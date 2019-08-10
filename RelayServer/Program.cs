using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static RelayServer.Config;
using static KingdomsSharedCode.Generic.Logger;

namespace RelayServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Info("Relay server booting");

            new Relay(LOCAL_ADDR, PORT, directMode:true).WaitUntilDeath().Wait();

            Info("Relay server task terminated.");
            Console.ReadKey();
        }
    }
}
