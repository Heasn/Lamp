using System;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Network.Server
{
    class BedRockUdpServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            //var datagramPacket = (DatagramPacket) message;
            //kcp.Receive(datagramPacket.Content);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            
        }
    }
}
