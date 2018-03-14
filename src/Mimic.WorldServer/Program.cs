using Mimic.Common;
using System;
using System.Threading.Tasks;

namespace Mimic.WorldServer
{
    class Program
    {
        public static IDatabase authDatabase;
        static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            string ip = "0.0.0.0";
            ushort port = 8085;
            
            authDatabase = new MySQLDatabase(pass:"root",database:"mimic_auth");
            authDatabase.createIfNonexistant();
            var socketManager = new SocketManager<WorldHandler>(ip,port);

            await socketManager.StartAsync();
            Console.Out.WriteLine("Started on " + ip + ":" + port);
            await authDatabase.init();
            await Task.Delay(-1);
        }
    }
}
