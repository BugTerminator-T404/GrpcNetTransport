namespace GrpcNet.Transport.Impl
{
    using Grpc.Core;
    using System;
    using System.Threading;
    using Mutex = Concurrency.Mutex;
#if NET6_0_OR_GREATER
    internal readonly record struct GrpcServerIncomingCall
    {
        public required GrpcRequest Request { get; init; }
        public required GrpcTransportConnection Connection { get; init; }
        public required string Peer { get; init; }
        public required Action<string> LogTrace { get; init; }
#else    
    internal readonly struct GrpcServerIncomingCall
    {
        public GrpcRequest Request { get; init; }
        public GrpcTransportConnection Connection { get; init; }
        public string Peer { get; init; }
        public Action<string> LogTrace { get; init; }

#endif
        public GrpcServerCallContext CreateCallContext(
                string methodName,
                CancellationToken cancellationToken)
        {
            return new GrpcServerCallContext(
                methodName,
                string.Empty,
                Peer,
                Request.DeadlineUnixTimeMilliseconds == 0
                    ? null
                    : DateTimeOffset.FromUnixTimeMilliseconds(Request.DeadlineUnixTimeMilliseconds).UtcDateTime,
                Request.HasRequestHeaders
                    ? GrpcMetadataConverter.Convert(Request.RequestHeaders)
                    : new Metadata(),
                Connection,
                new Mutex(),
                cancellationToken);
        }
    }
}
