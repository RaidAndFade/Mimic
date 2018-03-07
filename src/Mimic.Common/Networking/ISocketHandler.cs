using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mimic.Common
{
    public interface ISocketHandler
    {
        Task RunAsync(TcpClient client);
    }
}