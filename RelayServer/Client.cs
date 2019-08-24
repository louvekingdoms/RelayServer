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

        // WILL LEAK
        public Dictionary<ushort, string> sumAtBeat = new Dictionary<ushort, string>(); 

        bool isPaused = false;
        uint internalId = 0;

        public Client(Socket tcp)
        {
            OnMessageReception += delegate { };

            thread = new Thread((ThreadStart)delegate {
                while (Receive())
                {
                }
                logger.Trace("Client " + tcp.GetHashCode()+" is no longer listening.");
            });
            thread.Start();
            this.tcp = tcp;
            logger.Debug("Client thread started for " + tcp.GetHashCode());
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
            logger.Debug("Client " + tcp + " died");
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
                {
                    logger.Trace("Receiving message from client " + GetHashCode());
                    var msg = new Message(clientStream.ReadMessageData());
                    logger.Trace("<< " + msg);
                    OnMessageReception(msg);
                }
                return true;
            }
            catch (ObjectDisposedException)
            {
                logger.Trace("Client " + GetHashCode() + " has been disposed");
                return false;
            }
            catch (Exception e)
            {
                logger.Error("Fatal error for client " + GetHashCode() + ": "+e.ToString());
                return false;
            }
        }

        public void AddSumAtBeat(string sum)
        {
            sumAtBeat[clockBeat] = sum;
        }

        public Action<Message> OnMessageReception;
    }

}
