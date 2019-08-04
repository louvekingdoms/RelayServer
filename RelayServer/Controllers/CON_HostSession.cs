
using KingdomsSharedCode.Networking;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RelayServer.Controllers
{
    class CON_HostSession : Controller
    {
        const int C_SESSION_INFO = 0;

        public override void Execute(Relay server, Relay.Client client, Relay.Session session, Message message)
        {
            if (session != null) throw new Relay.UnexpectedSessionException();

            uint id = server.CreateSession(client);

            using (var stream = client.tcp.NewStream())
            {
                stream.Write(new Message()
                {
                    beat = 0,
                    controller = (byte)KingdomsSharedCode.Networking.Controller.SESSION_INFO,
                    body = id.ToString()
                });
            }
        }
    }
}
