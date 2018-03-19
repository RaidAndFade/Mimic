using Mimic.Common;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Mimic.WorldServer
{
    class Program
    {
        public static AuthDatabase authDatabase;
        public static WorldDatabase worldDatabase;

        public static World world;

        static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            
            //Starting
            string host = "127.0.0.1", database = "mimic_auth", user = "root", pass = "root";
            var builder = new DbContextOptionsBuilder().UseMySQL($"Server={host}; database={database}; UID={user}; password={pass}");
            authDatabase = new AuthDatabase(builder.Options);
            authDatabase.Database.EnsureCreated();

            database = "mimic_world";
            builder = new DbContextOptionsBuilder().UseMySQL($"Server={host}; database={database}; UID={user}; password={pass}");
            worldDatabase = new WorldDatabase(builder.Options);
            worldDatabase.Database.EnsureCreated();

            world = new World();

            string ip = "0.0.0.0";
            ushort port = 8085;
            var socketManager = new SocketManager<WorldHandler>(ip,port);

            await socketManager.StartAsync();
            Console.Out.WriteLine("Started on " + ip + ":" + port);
            
            //Started. Looping
            InitWorldLoop();
            //Stopping
        }

        static void InitWorldLoop(){
            long curTime = 0;
            long prevTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            while(world.isRunning){
                //++World::m_worldLoopCounter;
                curTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                long tickDiff = curTime - prevTime;

                world.Update(tickDiff);
                prevTime = curTime;

                long tickTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - curTime;

                if(tickTime < Consts.WORLD_TICK_TIME_MS)
                    Task.Delay(Consts.WORLD_TICK_TIME_MS-(int)tickTime);
            }
        }
    }
}
