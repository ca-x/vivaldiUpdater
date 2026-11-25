using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VivaldiUpdater.Model;

namespace VivaldiUpdater.Helpers
{
    internal sealed class MetadataSocksProxy : IDisposable
    {
        private const int MaxConnections = 16;
        private const string ProxyUser = "vivaldi";
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly HashSet<Tunnel> tunnels = new HashSet<Tunnel>();
        private readonly string socksHost;
        private readonly int socksPort;
        private readonly byte[] socksCredentials;
        private readonly bool forDownloads;
        private readonly string password;
        private readonly string authorization;
        private bool disposed;

        private MetadataSocksProxy(ProxyConfig config, bool forDownloads)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            socksHost = config.ProxyHost?.Trim();
            socksPort = config.ProxyPort;
            if (string.IsNullOrEmpty(socksHost) || !IsValidPort(socksPort))
                throw new ArgumentException("A SOCKS5 host and valid port are required.", nameof(config));
            if (socksHost.StartsWith("[", StringComparison.Ordinal) && socksHost.EndsWith("]", StringComparison.Ordinal))
                socksHost = socksHost.Substring(1, socksHost.Length - 2);

            socksCredentials = EncodeCredentials(config.ProxyUsername, config.ProxyPassword);
            this.forDownloads = forDownloads;
            var secret = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(secret);
            password = Convert.ToBase64String(secret);
            authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(ProxyUser + ":" + password));
            listener.ExclusiveAddressUse = true;
            listener.Start(MaxConnections);
            _ = AcceptAsync();
        }

        public static HttpClient CreateClient(ProxyConfig config, bool forDownloads = false)
        {
            var bridge = new MetadataSocksProxy(config, forDownloads);
            try
            {
                return new HttpClient(new BridgeHandler(bridge));
            }
            catch
            {
                bridge.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            lock (tunnels)
            {
                if (disposed) return;
                disposed = true;
                listener.Stop();
                foreach (var tunnel in tunnels) tunnel.Dispose();
            }
        }

        private async Task AcceptAsync()
        {
            while (true)
            {
                Socket client;
                try
                {
                    client = await listener.AcceptSocketAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (IsSocketFailure(ex) || ex is InvalidOperationException)
                {
                    Dispose();
                    return;
                }

                var tunnel = new Tunnel(client);
                lock (tunnels)
                {
                    if (disposed || tunnels.Count == MaxConnections)
                    {
                        tunnel.Dispose();
                        continue;
                    }
                    tunnels.Add(tunnel);
                }
                _ = HandleClientAsync(tunnel);
            }
        }

        private async Task HandleClientAsync(Tunnel tunnel)
        {
            try
            {
                using (var local = new NetworkStream(tunnel.Client))
                    await HandleClientAsync(tunnel, local).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsSocketFailure(ex)) { }
            finally
            {
                tunnel.Dispose();
                lock (tunnels) tunnels.Remove(tunnel);
            }
        }

        private async Task HandleClientAsync(Tunnel tunnel, NetworkStream local)
        {
            NetworkStream remote = null;
            try
            {
                Socket upstream;
                // Framework aborts the request on cancellation, but its pending CONNECT can outlive it.
                // Closing both sockets enforces this deadline even for uncancellable Framework socket I/O.
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                using (timeout.Token.Register(tunnel.Dispose))
                {
                    var header = await ReadHeaderAsync(local).ConfigureAwait(false);
                    if (!TryReadDestination(header, out var destination))
                    {
                        await ReplyAsync(local, "403 Forbidden").ConfigureAwait(false);
                        return;
                    }
                    if (!IsAuthorized(header))
                    {
                        await ReplyAsync(local, "407 Proxy Authentication Required",
                            "Proxy-Authenticate: Basic realm=\"Vivaldi SOCKS5 bridge\"\r\n").ConfigureAwait(false);
                        return;
                    }

                    try
                    {
                        upstream = tunnel.OpenUpstream();
                        remote = await ConnectSocksAsync(upstream, destination).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (IsSocketFailure(ex))
                    {
                        await ReplyAsync(local, "502 Bad Gateway").ConfigureAwait(false);
                        return;
                    }
                    await ReplyAsync(local, "200 Connection Established").ConfigureAwait(false);
                }

                var outbound = RelayAsync(local, remote, upstream, tunnel);
                var inbound = RelayAsync(remote, local, tunnel.Client, tunnel);
                await Task.WhenAny(outbound, inbound).ConfigureAwait(false);
                var relays = Task.WhenAll(outbound, inbound);
                // Let a peer finish sending after a half-close, but don't retain abandoned slots indefinitely.
                if (!relays.IsCompleted &&
                    await Task.WhenAny(relays, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false) != relays)
                    tunnel.Dispose();
                await relays.ConfigureAwait(false);
            }
            finally { remote?.Dispose(); }
        }

        private static Socket OpenUpstreamSocket()
        {
            if (!Socket.OSSupportsIPv6)
                return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.DualMode = true;
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static async Task RelayAsync(NetworkStream from, NetworkStream to, Socket target, Tunnel tunnel)
        {
            try
            {
                await from.CopyToAsync(to, 16384).ConfigureAwait(false);
                target.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex) when (IsSocketFailure(ex)) { tunnel.Dispose(); }
        }

        private async Task<NetworkStream> ConnectSocksAsync(Socket upstream, Uri destination)
        {
            await Task.Factory.FromAsync(
                (callback, state) => upstream.BeginConnect(socksHost, socksPort, callback, state),
                upstream.EndConnect, null).ConfigureAwait(false);
            var stream = new NetworkStream(upstream);
            try
            {
                await NegotiateSocksAsync(stream, destination).ConfigureAwait(false);
                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private async Task NegotiateSocksAsync(NetworkStream stream, Uri destination)
        {
            var method = socksCredentials == null ? (byte)0 : (byte)2;
            await WriteAsync(stream, new byte[] { 5, 1, method }).ConfigureAwait(false);
            var selection = await ReadAsync(stream, 2).ConfigureAwait(false);
            if (selection[0] != 5 || selection[1] != method)
                throw new IOException("The SOCKS5 proxy rejected the configured authentication method.");
            if (socksCredentials != null)
            {
                await WriteAsync(stream, socksCredentials).ConfigureAwait(false);
                var result = await ReadAsync(stream, 2).ConfigureAwait(false);
                if (result[0] != 1 || result[1] != 0)
                    throw new IOException("The SOCKS5 proxy rejected the credentials.");
            }

            var host = Encoding.ASCII.GetBytes(destination.IdnHost);
            var request = new byte[7 + host.Length];
            request[0] = 5;
            request[1] = 1;
            request[3] = 3; // The SOCKS server resolves the destination, not the updater.
            request[4] = (byte)host.Length;
            host.CopyTo(request, 5);
            request[request.Length - 2] = (byte)(destination.Port >> 8);
            request[request.Length - 1] = (byte)destination.Port;
            await WriteAsync(stream, request).ConfigureAwait(false);

            var reply = await ReadAsync(stream, 4).ConfigureAwait(false);
            if (reply[0] != 5 || reply[1] != 0 || reply[2] != 0)
                throw new IOException("The SOCKS5 proxy rejected the CONNECT request.");
            int addressLength;
            switch (reply[3])
            {
                case 1: addressLength = 4; break;
                case 4: addressLength = 16; break;
                case 3: addressLength = (await ReadAsync(stream, 1).ConfigureAwait(false))[0]; break;
                default: throw new IOException("Invalid SOCKS5 bound address type.");
            }
            if (addressLength == 0) throw new IOException("Invalid SOCKS5 bound address length.");
            await ReadAsync(stream, addressLength + 2).ConfigureAwait(false);
        }

        private static byte[] EncodeCredentials(string username, string password)
        {
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password)) return null;
            var user = Encoding.UTF8.GetBytes(username ?? "");
            var pass = Encoding.UTF8.GetBytes(password ?? "");
            if (user.Length < 1 || user.Length > 255 || pass.Length < 1 || pass.Length > 255)
                throw new ArgumentException("SOCKS5 username and password must each encode to 1–255 bytes.");
            var result = new byte[3 + user.Length + pass.Length];
            result[0] = 1;
            result[1] = (byte)user.Length;
            user.CopyTo(result, 2);
            result[2 + user.Length] = (byte)pass.Length;
            pass.CopyTo(result, 3 + user.Length);
            return result;
        }

        private bool IsAllowed(Uri uri)
        {
            if (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0 ||
                uri.HostNameType != UriHostNameType.Dns || uri.IdnHost.Length > 253 || !IsValidPort(uri.Port))
                return false;
            if (!forDownloads)
                return uri.Port == 443 && (uri.IdnHost == "vivaldi.czyt.tech" || uri.IdnHost == "vivaldi.czyt.eu.org");
            try
            {
                DownloadUriPolicy.Validate(uri, enforceTrustedHosts: true);
                return true;
            }
            catch (SecurityException) { return false; }
        }

        private bool TryReadDestination(string[] header, out Uri destination)
        {
            destination = null;
            var line = header[0].Split(' ');
            if (line.Length != 3 || line[0] != "CONNECT" || (line[2] != "HTTP/1.1" && line[2] != "HTTP/1.0") ||
                !Uri.TryCreate("https://" + line[1], UriKind.Absolute, out var uri) || !IsAllowed(uri) ||
                !string.Equals(line[1], uri.IdnHost + ":" + uri.Port.ToString(CultureInfo.InvariantCulture),
                    StringComparison.OrdinalIgnoreCase))
                return false;
            destination = uri;
            return true;
        }

        private bool IsAuthorized(string[] header)
        {
            string value = null;
            for (var i = 1; i < header.Length; i++)
            {
                const string name = "Proxy-Authorization:";
                if (!header[i].StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
                if (value != null) return false;
                value = header[i].Substring(name.Length).Trim();
            }
            const string basic = "Basic ";
            if (value == null || value.Length != basic.Length + authorization.Length ||
                !value.StartsWith(basic, StringComparison.OrdinalIgnoreCase))
                return false;
            var differences = 0;
            for (var i = 0; i < authorization.Length; i++)
                differences |= value[basic.Length + i] ^ authorization[i];
            return differences == 0;
        }

        private static async Task<string[]> ReadHeaderAsync(NetworkStream stream)
        {
            // Do not consume bytes past CRLFCRLF: the rest of the stream belongs to the tunnel.
            var header = new byte[8192];
            for (var i = 0; i < header.Length; i++)
            {
                if (await stream.ReadAsync(header, i, 1).ConfigureAwait(false) == 0)
                    throw new EndOfStreamException();
                if (header[i] > 127) throw new IOException("Invalid CONNECT header.");
                if (i >= 3 && header[i - 3] == 13 && header[i - 2] == 10 && header[i - 1] == 13 && header[i] == 10)
                    return Encoding.ASCII.GetString(header, 0, i - 3)
                        .Split(new[] { "\r\n" }, StringSplitOptions.None);
            }
            throw new IOException("CONNECT header exceeds the size limit.");
        }

        private static async Task<byte[]> ReadAsync(NetworkStream stream, int count)
        {
            var bytes = new byte[count];
            for (var offset = 0; offset < count;)
            {
                var read = await stream.ReadAsync(bytes, offset, count - offset).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
            return bytes;
        }

        private static Task WriteAsync(NetworkStream stream, byte[] bytes) =>
            stream.WriteAsync(bytes, 0, bytes.Length);

        private static Task ReplyAsync(NetworkStream stream, string status, string extraHeaders = "") =>
            WriteAsync(stream, Encoding.ASCII.GetBytes("HTTP/1.1 " + status + "\r\n" +
                (status[0] == '2' ? "" : "Connection: close\r\nContent-Length: 0\r\n") + extraHeaders + "\r\n"));

        private static bool IsValidPort(int port) => port >= 1 && port <= 65535;

        private static bool IsSocketFailure(Exception ex) =>
            ex is IOException || ex is SocketException || ex is ObjectDisposedException;

        private sealed class Tunnel : IDisposable
        {
            private readonly object gate = new object();
            private Socket upstream;
            private bool closed;

            public Tunnel(Socket client) { Client = client; }
            public Socket Client { get; }

            public Socket OpenUpstream()
            {
                lock (gate)
                {
                    if (closed) throw new ObjectDisposedException(nameof(Tunnel));
                    return upstream = OpenUpstreamSocket();
                }
            }

            public void Dispose()
            {
                lock (gate)
                {
                    if (closed) return;
                    closed = true;
                    Client.Dispose();
                    upstream?.Dispose();
                }
            }
        }

        private sealed class BridgeHandler : DelegatingHandler
        {
            private readonly MetadataSocksProxy bridge;

            public BridgeHandler(MetadataSocksProxy bridge) : base(new HttpClientHandler
            {
                UseProxy = true,
                Proxy = new LoopbackProxy(bridge),
                AllowAutoRedirect = bridge.forDownloads
            })
            {
                this.bridge = bridge;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                if (!bridge.IsAllowed(request.RequestUri))
                    throw new HttpRequestException("The destination is not allowed by the SOCKS5 bridge.");
                return base.SendAsync(request, token);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) bridge.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class LoopbackProxy : IWebProxy
        {
            private readonly Uri endpoint;

            public LoopbackProxy(MetadataSocksProxy bridge)
            {
                endpoint = new Uri("http://127.0.0.1:" +
                    ((IPEndPoint)bridge.listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture));
                Credentials = new NetworkCredential(ProxyUser, bridge.password);
            }

            public ICredentials Credentials { get; set; }
            public Uri GetProxy(Uri destination) => endpoint;
            // WebProxy bypasses loopback destinations, which could otherwise escape redirect checks.
            public bool IsBypassed(Uri host) => false;
        }
    }
}
