
using KingdomsSharedCode.Networking;
using static KingdomsSharedCode.Generic.Logger;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using KingdomsSharedCode.JSON;
using static RelayServer.Relay;

namespace RelayServer.Controllers
{
    class CON_HostSession : Controller
    {
        public override void Execute(Relay server, Client client, Session session, Message message)
        {
            if (session != null) throw new Relay.UnexpectedSessionException();

            uint id = server.CreateSession(client);

            var newJSON = new JSONObject();
            newJSON.Add("session", client.session.id);
            newJSON.Add("beat", client.session.GetLowestClock());

            using (var stream = client.tcp.NewStream())
            {
                var msg = new Message()
                {
                    controller = (byte)KingdomsSharedCode.Networking.Controller.SESSION_INFO,
                    body = newJSON.ToString()
                };
                logger.Trace(">> " + msg);
                stream.Write(msg);
            }

            logger.Trace("Client " + client + " is now in session " + client.session);
        }
    }
}
