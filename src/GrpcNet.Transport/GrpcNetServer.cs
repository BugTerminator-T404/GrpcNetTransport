namespace GrpcNet.Transport
{
    using Grpc.Core;
    using GrpcNet;
    using GrpcNet.Abstractions;
    using GrpcNet.Transport.Impl;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading.Tasks;

    internal sealed class GrpcNetServer<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
    T> : IGrpcNetServer<T>, IAsyncDisposable
        where T : class
    {
        private readonly T _instance;
        private readonly ILogger<GrpcNetServer<T>> _logger;
        private GrpcServer? _grpcServer;
        private int _networkPort;
        private IPAddress _hostAdress;
        readonly ITransportListener _listener;
        public GrpcNetServer(
            ILogger<GrpcNetServer<T>> logger,
            T instance,
            ITransportListener listener,
            string host,
            int port) : this(logger, instance, listener, IPAddress.Parse(host), port)
        {

        }

        public GrpcNetServer(
            ILogger<GrpcNetServer<T>> logger,
            T instance,
            ITransportListener listener,
            bool loopbackOnly,
            int port) : this(logger, instance, listener, loopbackOnly ? IPAddress.Loopback : IPAddress.Any, port)
        {
        }

        public GrpcNetServer(
        ILogger<GrpcNetServer<T>> logger,
        T instance,
        ITransportListener listener,
        IPAddress address,
        int port)
        {
            _logger = logger;
            _instance = instance;
            _listener = listener;
            _grpcServer = null;
            _hostAdress = address;
            _networkPort = port;
        }

        public async Task StartAsync()
        {
            if (_grpcServer != null)
            {
                return;
            }

            do
            {
                GrpcServer? grpcServer = null;
                try
                {
                    //GrpcPipeLog.GrpcServerStarting(_logger);

                    var endpoint = new IPEndPoint(_hostAdress, _networkPort);
                    await _listener.ListenAsync(endpoint).ConfigureAwait(false);
                    grpcServer = new GrpcServer(_listener, _logger);
                    var binderAttr = typeof(T).GetCustomAttribute<BindServiceMethodAttribute>();
                    if (binderAttr == null)
                    {
                        throw new InvalidOperationException($"{typeof(T).FullName} does not have the grpc::BindServiceMethod attribute.");
                    }
                    var targetMethods = binderAttr.BindType.GetMethods(
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.FlattenHierarchy);
                    var binder = targetMethods
                        .Where(x => x.Name == binderAttr.BindMethodName && x.GetParameters().Length == 2)
                        .First();
#if !NET5_0_OR_GREATER
                    binder.Invoke(null, BindingFlags.DoNotWrapExceptions, null, new Object[] { grpcServer, _instance }, null);
#else
                    binder.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [grpcServer, _instance], null);
#endif

                    _grpcServer = grpcServer;
                    return;
                }
                catch (IOException ex) when ((
                    // Pointer file is open by another server.
                    ex.Message.Contains("used by another process", StringComparison.OrdinalIgnoreCase)))
                {

                    if (grpcServer != null)
                    {
                        await grpcServer.DisposeAsync().ConfigureAwait(false);
                        grpcServer = null;
                    }
                    continue;
                }
            } while (true);
        }

        public async Task StopAsync()
        {

            if (_grpcServer != null)
            {
                await _grpcServer.DisposeAsync().ConfigureAwait(false);
                _grpcServer = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public ServiceBinderBase? ServiceBinder => _grpcServer;

        public int NetworkPort
        {
            get
            {
                return _networkPort;
            }
        }
    }
}
