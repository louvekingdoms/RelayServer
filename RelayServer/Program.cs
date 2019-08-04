using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static RelayServer.Config;

namespace RelayServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Relay server booting");

            new Relay(LOCAL_ADDR, PORT).WaitUntilDeath().Wait();
            Console.WriteLine("Relay server task terminated.");
            Console.ReadKey();
        }
    }
}
