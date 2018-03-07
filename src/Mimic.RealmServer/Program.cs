using Mimic.Common;
using System;
using System.Threading.Tasks;

namespace Mimic.RealmServer
{
    class Program
    {
        static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            var socketManager = new SocketManager<AuthHandler>();

            await socketManager.StartAsync();
            await Task.Delay(-1);
        }
    }
}
