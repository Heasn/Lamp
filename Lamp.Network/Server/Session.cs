using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Lamp.Network.KcpLib;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Network.Server
{
    class Session
    {
        private Kcp m_Kcp;
        private IChannel m_IChannel;
        private EndPoint m_LocalAddress;

        public Session(BedRockUdpServer s)
        {
            m_Kcp = new Kcp(s.Out, null);
        }
    }
}
