using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestCase.Helpers
{
    internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

        public ScriptedHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No scripted HTTP response remains.");
            }

            return Task.FromResult(_responses.Dequeue()(request));
        }
    }

    internal sealed class ThrowAfterReadStream : Stream
    {
        private readonly byte[] _content;
        private readonly int _bytesBeforeFailure;
        private int _position;

        public ThrowAfterReadStream(byte[] content, int bytesBeforeFailure)
        {
            _content = content;
            _bytesBeforeFailure = bytesBeforeFailure;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _content.Length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _bytesBeforeFailure)
            {
                throw new IOException("The test connection was interrupted.");
            }

            var bytesToRead = Math.Min(count, _bytesBeforeFailure - _position);
            Buffer.BlockCopy(_content, _position, buffer, offset, bytesToRead);
            _position += bytesToRead;
            return bytesToRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(Read(buffer, offset, count));
            }
            catch (Exception exception)
            {
                var completion = new TaskCompletionSource<int>();
                completion.SetException(exception);
                return completion.Task;
            }
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }

    internal sealed class UnknownLengthContent : HttpContent
    {
        private readonly byte[] _content;

        public UnknownLengthContent(byte[] content)
        {
            _content = content;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return stream.WriteAsync(_content, 0, _content.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
