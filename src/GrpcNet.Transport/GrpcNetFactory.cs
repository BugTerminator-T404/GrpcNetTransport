namespace GrpcNet.Transport
{
    using Grpc.Core;
    using Grpc.Net.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using GrpcNet.Transport.Impl;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using GrpcNet;
    using GrpcNet.Abstractions;

    /// <summary>
    /// Provides a gRPC pipe factory which uses TCP for transport.
    /// </summary>
    public sealed class GrpcNetFactory : IGrpcNetFactory
    {
        private readonly IServiceProvider? _serviceProvider;

        /// <inheritdoc />
        public static IGrpcNetFactory CreateFactoryWithoutInjection()
        {
            return new GrpcNetFactory(null);
        }

        /// <summary>
        /// Construct a new gRPC pipe factory which uses TCP for transport.
        /// </summary>
        /// <param name="serviceProvider">The service provider, which is optional when creating clients.</param>
        public GrpcNetFactory(IServiceProvider? serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        IGrpcNetServer<T> IGrpcNetFactory.CreateNetworkServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] 
#endif
            T>(
            T instance, ITransportListener listener, bool loopbackOnly = false, int networkPort = 0) where T : class
        {
            return new GrpcNetServer<T>(
                _serviceProvider?.GetRequiredService<ILogger<GrpcNetServer<T>>>(),
                instance,
                listener,
                loopbackOnly,
                networkPort);
        }

        IGrpcNetServer<T> IGrpcNetFactory.CreateNetworkServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] 
#endif
            T>(
            T instance, ITransportListener listener, string host, int networkPort = 0) where T : class
        {
            return new GrpcNetServer<T>(
                _serviceProvider?.GetRequiredService<ILogger<GrpcNetServer<T>>>(),
                instance,
                listener,
                host,
                networkPort);
        }

        IGrpcNetServer<T> IGrpcNetFactory.CreateNetworkServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] 
#endif
            T>(
            T instance, ITransportListener listener, IPAddress host, int networkPort = 0) where T : class
        {
            return new GrpcNetServer<T>(
                _serviceProvider?.GetRequiredService<ILogger<GrpcNetServer<T>>>(),
                instance,
                listener,
                host,
                networkPort);
        }

        T IGrpcNetFactory.CreateNetworkClient<T>(
            IPEndPoint endpoint,
            ITransportFactory transportFactory,
            Func<CallInvoker, T> constructor,
            GrpcChannelOptions? grpcChannelOptions)
        {
            var logger = _serviceProvider?.GetService<ILogger<GrpcNetFactory>>();

            var options = grpcChannelOptions ?? new GrpcChannelOptions();
            options.Credentials = ChannelCredentials.Insecure;

            // Allow unlimited message sizes.
            options.MaxReceiveMessageSize = null;
            options.MaxSendMessageSize = null;

            return constructor(new GrpcClientCallInvoker(endpoint, transportFactory, logger));
        }
    }
}
