using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static RelayServer.Config;
using static RelayServer.Relay;
using KingdomsSharedCode.Generic;

namespace RelayServer
{
    class Program
    {
        static void Main(string[] args)
        {
            logger = new Logger("RELAY");

            logger.Info("Relay server booting");

            new Relay(LOCAL_ADDR, PORT, directMode: true).WaitUntilDeath().Wait();
            logger.Info("Relay server task terminated.");

            Console.ReadKey();
        }
    }
}
