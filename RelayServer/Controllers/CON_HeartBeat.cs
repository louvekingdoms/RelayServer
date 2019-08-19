
using KingdomsSharedCode.Networking;
using static KingdomsSharedCode.Generic.Logger;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

using static RelayServer.Relay;

namespace RelayServer.Controllers
{
    class CON_HeartBeat : Controller
    {
        public override void Execute(Relay server, Client client, Session session, Message message)
        {
            ushort remoteBeat = Convert.ToUInt16(message.body);

            client.lastHeartBeat = ((int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()) % int.MaxValue;
            client.clockBeat = remoteBeat;

            // Below are Synchron operations - only valid when in a session
            if (session == null) return;
            
            // Pausing client if too advanced
            if (remoteBeat > session.lowestClock + Config.MAXIMUM_BEAT_DIFFERENCE && !client.IsPaused())
            {
                logger.Debug("Paused client from session " + session + " because too advanced (" + remoteBeat + " > " + session.lowestClock + ")");
                client.Pause();
            }

            // Checking if we can resume clients now that the others have caught back
            foreach (var cl in session.GetClients())
            {
                if (cl.IsPaused() && cl.clockBeat < session.lowestClock + Config.MAXIMUM_BEAT_DIFFERENCE)
                {
                    logger.Debug("Resumed client from session " + session + "(" + cl.clockBeat + " ~ " + session.lowestClock + ")");
                    cl.Unpause();
                }
            }
        }
    }
}
