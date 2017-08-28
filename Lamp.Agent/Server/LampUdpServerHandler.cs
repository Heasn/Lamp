using System;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Agent.Server
{
    sealed class LampUdpServerHandler : ChannelHandlerAdapter
    {

        public LampUdpServerHandler()
        {

        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var packet = (DatagramPacket)message;
            var packetContent = packet.Content;

            var ctxAttribute = context.GetAttribute(Session.SessionIdentity);

            var session = ctxAttribute.Get();

            if (session == null)
            {
                var sessionId = SessionManager.Instance.GetNewSessionId();
                var newsession = Session.Create(context.Channel, packet.Sender, sessionId);

                //用户连入数已达到最大
                if (sessionId == NetworkOperationCode.MAX_CONN_EXCEED)
                {
                   
                }
                else
                {
                    ctxAttribute.Set(newsession);
                    //mConnectAcceptAction(session);
                }
            }
            else
            {
                //session.RecvData(packet.Content);
            }
            
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            
        }
    }
}
