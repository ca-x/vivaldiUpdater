using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VivaldiUpdater.Helpers;
using VivaldiUpdater.Model;

namespace TestCase.Helpers
{
    [TestFixture]
    public class MetadataSocksProxyTest
    {
        [TestCase("vivaldi.czyt.tech", 443, false, (byte)1, false)]
        [TestCase("vivaldi.czyt.eu.org", 443, true, (byte)3, false)]
        [TestCase("vivaldi.czyt.tech", 443, false, (byte)4, false)]
        [TestCase("release-assets.githubusercontent.com", 8443, false, (byte)1, true)]
        [TestCase("github.com", 443, false, (byte)1, true)]
        public async Task ConnectNegotiatesSocksAndRelaysTheTlsClientHello(
            string host, int port, bool authenticate, byte addressType, bool forDownloads)
        {
            var listener = StartListener();
            try
            {
                var config = Configure(listener);
                if (authenticate)
                {
                    config.ProxyUsername = "user";
                    config.ProxyPassword = "pass";
                }

                using (var client = MetadataSocksProxy.CreateClient(config, forDownloads))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var request = client.GetAsync(new UriBuilder(Uri.UriSchemeHttps, host, port, "/metadata").Uri);
                    using (var peer = await AcceptAsync(listener))
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    using (timeout.Token.Register(peer.Close))
                    {
                        var stream = peer.GetStream();
                        var method = authenticate ? (byte)2 : (byte)0;
                        Assert.That(await ReadAsync(stream, 3), Is.EqualTo(new byte[] { 5, 1, method }));
                        await stream.WriteAsync(new byte[] { 5, method }, 0, 2);

                        if (authenticate)
                        {
                            Assert.That(await ReadAsync(stream, 11),
                                Is.EqualTo(new byte[] { 1, 4, 117, 115, 101, 114, 4, 112, 97, 115, 115 }));
                            await stream.WriteAsync(new byte[] { 1, 0 }, 0, 2);
                        }

                        Assert.That(await ReadAsync(stream, 5), Is.EqualTo(new byte[] { 5, 1, 0, 3, (byte)host.Length }));
                        Assert.That(Encoding.ASCII.GetString(await ReadAsync(stream, host.Length)), Is.EqualTo(host));
                        Assert.That(await ReadAsync(stream, 2), Is.EqualTo(new byte[] { (byte)(port >> 8), (byte)port }));

                        var address = addressType == 3 ? new byte[] { 2, 97, 98 } : new byte[addressType == 1 ? 4 : 16];
                        await stream.WriteAsync(new byte[] { 5, 0, 0, addressType }, 0, 4);
                        await stream.WriteAsync(address, 0, address.Length);
                        await stream.WriteAsync(new byte[2], 0, 2);

                        var tls = await ReadAsync(stream, 6);
                        Assert.That(tls[0], Is.EqualTo(22), "TLS handshake record");
                        Assert.That(tls[5], Is.EqualTo(1), "TLS ClientHello");
                        // Return a TLS fatal handshake_failure instead of EOF, which HttpWebRequest can retry.
                        await ReadAsync(stream, (tls[3] << 8) + tls[4] - 1);
                        var alert = new byte[] { 21, 3, 3, 0, 2, 2, 40 };
                        await stream.WriteAsync(alert, 0, alert.Length);
                        Assert.ThrowsAsync<HttpRequestException>(async () => await request);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [TestCase("http://vivaldi.czyt.tech/", false)]
        [TestCase("https://vivaldi.czyt.tech:444/", false)]
        [TestCase("https://vivaldi.czyt.tech.example/", false)]
        [TestCase("https://vivaldi.czyt.tech./", false)]
        [TestCase("http://github.com/", true)]
        [TestCase("https://github.com.example/", true)]
        [TestCase("https://127.0.0.1:8443/", true)]
        [TestCase("https://github.com:0/", true)]
        public void ClientRejectsInvalidDestinations(string url, bool forDownloads)
        {
            var listener = StartListener();
            try
            {
                using (var client = MetadataSocksProxy.CreateClient(Configure(listener), forDownloads))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync(url));
                    Assert.That(listener.Pending(), Is.False);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DisposalOrHandshakeDeadlineClosesAnUpstreamSocketWaitingForSocksNegotiation(bool dispose)
        {
            var listener = StartListener();
            try
            {
                using (var client = MetadataSocksProxy.CreateClient(Configure(listener)))
                using (var cancellation = new CancellationTokenSource())
                {
                    var request = client.GetAsync("https://vivaldi.czyt.tech/", cancellation.Token);
                    using (var peer = await AcceptAsync(listener))
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    using (timeout.Token.Register(peer.Close))
                    {
                        var stream = peer.GetStream();
                        await ReadAsync(stream, 3);
                        timeout.CancelAfter(TimeSpan.FromSeconds(dispose ? 3 : 8));
                        if (dispose) client.Dispose();
                        else
                        {
                            cancellation.Cancel();
                            Assert.That(await Task.WhenAny(request, Task.Delay(TimeSpan.FromSeconds(3))), Is.SameAs(request),
                                "The caller should not wait for the stalled SOCKS negotiation.");
                        }
                        try
                        {
                            Assert.That(await stream.ReadAsync(new byte[1], 0, 1), Is.Zero);
                        }
                        catch (IOException ex) when (ex.InnerException is SocketException socket &&
                                                    socket.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            // Windows may reset a socket that is closed during a pending receive.
                        }
                    }
                    try
                    {
                        await request;
                        Assert.Fail("The disposed client's request unexpectedly succeeded.");
                    }
                    catch (OperationCanceledException) { }
                    catch (HttpRequestException) { }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static TcpListener StartListener()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return listener;
        }

        private static ProxyConfig Configure(TcpListener listener)
        {
            return new ProxyConfig
            {
                UseProxy = true,
                ProxyType = ProxyType.Socks5,
                ProxyHost = IPAddress.Loopback.ToString(),
                ProxyPort = ((IPEndPoint)listener.LocalEndpoint).Port
            };
        }

        private static async Task<TcpClient> AcceptAsync(TcpListener listener)
        {
            var accept = listener.AcceptTcpClientAsync();
            Assert.That(await Task.WhenAny(accept, Task.Delay(TimeSpan.FromSeconds(10))), Is.SameAs(accept),
                "The HTTP handler never reached the SOCKS fixture.");
            return await accept;
        }

        private static async Task<byte[]> ReadAsync(Stream stream, int length)
        {
            var buffer = new byte[length];
            for (var offset = 0; offset < length;)
            {
                var read = await stream.ReadAsync(buffer, offset, length - offset);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
            }
            return buffer;
        }
    }
}
