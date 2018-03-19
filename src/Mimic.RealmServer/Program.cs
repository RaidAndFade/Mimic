using Mimic.Common;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Mimic.RealmServer
{
    class Program
    {
        public static AuthDatabase authDatabase;
        
        static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            string host = "127.0.0.1", database = "mimic_auth", user = "root", pass = "root";
            var builder = new DbContextOptionsBuilder();
            builder.UseMySQL($"Server={host}; database={database}; UID={user}; password={pass}");
            authDatabase = new AuthDatabase(builder.Options);
            authDatabase.Database.EnsureDeleted();
            try{
                authDatabase.Database.EnsureCreated();
            }catch(MySql.Data.MySqlClient.MySqlException mse){
                Debug.WriteLine(mse);
            }
            try
            {
                authDatabase.Accounts.Add(new AccountInfo { username = "RAID", pass_hash = "1848c99bb0beb9ccba402c3c1ca703095216c65d" });
                authDatabase.SaveChanges();
            }catch(Microsoft.EntityFrameworkCore.DbUpdateException due)
            {
                Debug.Write(due.GetBaseException());
            }

            var socketManager = new SocketManager<AuthHandler>();


            await socketManager.StartAsync();
            await Task.Delay(-1);
        }
    }
}
