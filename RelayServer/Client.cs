using KingdomsSharedCode.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static KingdomsSharedCode.Generic.Logger;
using static RelayServer.Config;
using static RelayServer.Relay;

namespace RelayServer
{
    public class Client
    {
        public Socket tcp;
        public Thread thread;
        public Session session;
        public int lastHeartBeat = ((int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()) % int.MaxValue;
        public ushort clockBeat = 0;

        bool isPaused = false;
        uint internalId = 0;

        public Client(Socket tcp)
        {
            OnMessageReception += delegate { };

            thread = new Thread((ThreadStart)delegate {
                while (Receive()) { }
            });
            thread.Start();
            this.tcp = tcp;
        }

        public bool IsPaused()
        {
            return isPaused;
        }

        public void Pause()
        {
            if (!isPaused)
            {
                using (var stream = tcp.NewStream())
                {
                    stream.Write(new Message()
                    {
                        controller = (byte)KingdomsSharedCode.Networking.Controller.WAIT
                    });
                }
            }
            isPaused = true;
        }

        public void Unpause()
        {
            if (isPaused)
            {
                using (var stream = tcp.NewStream())
                {
                    stream.Write(new Message()
                    {
                        controller = (byte)KingdomsSharedCode.Networking.Controller.GO
                    });
                }
            }
            isPaused = false;
        }

        public uint GetId()
        {
            if (session == null)
                throw new MissingSessionException("Requested id of client " + this + " but doesnt belong to any session");

            return internalId;
        }

        public void SetId(uint id)
        {
            this.internalId = id;
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
