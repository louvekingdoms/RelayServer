using KingdomsSharedCode.Networking;
using KingdomsSharedCode.JSON;

using static KingdomsSharedCode.Generic.Logger;
using static RelayServer.Relay;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RelayServer.Controllers
{
    class CON_JoinSession : Controller
    {
        public override void Execute(Relay server, Client client, Session session, Message message)
        {
            if (session != null) throw new Relay.UnexpectedSessionException();

            var sessionId = Convert.ToUInt32(message.body);
            var targetSession = server.GetSession(sessionId);

            if (targetSession == null) throw new Relay.UnknownSessionException(sessionId);

            var newJSON = new JSONObject();
            newJSON.Add("session", targetSession.id);
            newJSON.Add("beat", targetSession.GetLowestClock());

            server.AddToSession(sessionId, client);

            logger.Warn("Told client to start at beat " + client.session.GetLowestClock());
            
            using (var stream = client.tcp.NewStream())
            {
                var msg = new Message()
                {
                    beat = 0,
                    controller = (byte)KingdomsSharedCode.Networking.Controller.SESSION_INFO,
                    body = newJSON.ToString()
                };
                logger.Trace(">> " + msg);
                stream.Write(msg);
            }
        }
    }
}
