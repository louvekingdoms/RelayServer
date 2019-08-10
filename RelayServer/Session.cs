using System;
using System.Collections.Generic;
using System.Text;

using static KingdomsSharedCode.Generic.Logger;

namespace RelayServer
{
    public class Session
    {
        public uint id;
        public ushort lowestClock { get { return GetLowestClock() ; } }

        List<Client> clients = new List<Client>();

        public Session(uint id, Client client)
        {
            this.id = id;
            Add(client);
        }

        public ushort GetLowestClock()
        {
            ushort lowest = ushort.MaxValue;
            foreach(var client in clients)
            {
                if (client.clockBeat < lowest)
                {
                    lowest = client.clockBeat;
                }
            }
            return lowest;
        }

        public bool Contains(Client client)
        {
            return clients.Contains(client);
        }

        public void Add(Client client)
        {
            clients.Add(client);
            client.session = this;
            uint id = 0;
            foreach(var cl in clients)
            {
                if (cl.GetId() > id)
                {
                    id = cl.GetId() + 1;
                }
            }
            client.SetId(id);
        }

        public Client[] GetClients()
        {
            return clients.ToArray();
        }

        public bool IsEmpty()
        {
            return clients.Count <= 0;
        }

        public void Remove(Client client)
        {
            clients.RemoveAll(o => o == client);
            client.session = null;
        }

        public void Kill(Client client)
        {
            Remove(client);
            client.Die();
        }

        public override string ToString()
        {
            return string.Format("Session.{0}", id.ToString("X"));
        }
    }
}
