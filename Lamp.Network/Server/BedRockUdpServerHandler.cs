using System;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Network.Server
{
    class BedRockUdpServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {

        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            
        }
    }
}
