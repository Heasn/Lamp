using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using Lamp.Network.Server;

namespace Lamp.Agent
{
    class AgentServer : Lamp.Network.Server.BedRockUdpServer
    {
        protected override void SessionConnectAccept(Session session)
        {
            throw new NotImplementedException();
        }

        protected override void SessionConnectRefuse(Session session)
        {
            throw new NotImplementedException();
        }

        protected override void SessionDisconnected(Session session)
        {
            throw new NotImplementedException();
        }

        protected override void PacketReceived(IByteBuffer buffer, Session session)
        {
            throw new NotImplementedException();
        }


    }
}
