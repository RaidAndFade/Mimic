using Mimic.Common;
using System;
using System.Threading.Tasks;

namespace Mimic.RealmServer
{
    class Program
    {
        public static IDatabase authDatabase;
        
        static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            authDatabase = new MySQLDatabase(pass:"root",database:"mimic_auth");
            authDatabase.createIfNonexistant();
            var socketManager = new SocketManager<AuthHandler>();


            await socketManager.StartAsync();
            await authDatabase.init();
            await Task.Delay(-1);
        }
    }
}
