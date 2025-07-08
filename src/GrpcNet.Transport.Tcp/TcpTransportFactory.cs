using GrpcNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GrpcNet.Transport.Tcp
{
    public class TcpTransportFactory : ITransportFactory
    {
        public async Task<ITransportAdapter> ConnectAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var client = new TcpClient();
            await client.ConnectAsync(
#if NET6_0_OR_GREATER
                endpoint
                , cancellationToken
#else
                endpoint.Address,
                endpoint.Port
#endif
                ).ConfigureAwait(false);
            return new TcpTransportAdapter(client);
        }
    }
}
