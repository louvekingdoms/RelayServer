using KingdomsSharedCode.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static KingdomsSharedCode.Generic.Logger;
using static RelayServer.Config;

namespace RelayServer
{


    public class Client
    {
        public Socket tcp;
        public Thread thread;
        public Session session;
        public int lastHeartBeat = ((int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()) % int.MaxValue;

        public Client(Socket tcp)
        {
            OnMessageReception += delegate { };

            thread = new Thread((ThreadStart)delegate {
                while (Receive()) { }
            });
            thread.Start();
            this.tcp = tcp;
        }

        public void Die()
        {
            Debug("Client " + tcp + " died");
            tcp.Dispose();
            session = null;
        }

        public bool HasTimedOut(int newTime)
        {
            return (newTime - lastHeartBeat > HEARTBEAT_TIMEOUT);
        }

        bool Receive()
        {
            try
            {
                using (NetworkStream clientStream = tcp.NewStream())
                    if (clientStream.DataAvailable)
                        using (BinaryReader reader = new BinaryReader(clientStream, Encoding.UTF8, leaveOpen: true))
                        {
                            var msg = new Message(reader);
                            OnMessageReception(msg);
                        }

                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public Action<Message> OnMessageReception;
    }

}
