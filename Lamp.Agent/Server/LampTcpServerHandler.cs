using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Lamp.Agent.Server
{
    sealed class LampTcpServerHandler: ChannelHandlerAdapter
    {
        public override void ChannelActive(IChannelHandlerContext context)
        {
            var sessionAttribute = context.GetAttribute(Session.SessionIdentity);
            var session = sessionAttribute.Get();
            var buffer = context.Allocator.Buffer().WithOrder(ByteOrder.LittleEndian);

            //新客户连入
            if (session == null)
            {
                var sessionId = SessionManager.Instance.GetNewSessionId();
                var newsession = Session.Create(context.Channel, context.Channel.RemoteAddress, sessionId);
                var randomKey = new Random().Next(int.MaxValue);

                //用户连入数已达到最大
                if (sessionId == NetworkOperationCode.MAX_CONN_EXCEED)
                {
                    SimplePacketCreator.HandshakeRefuse(buffer);
                }
                else
                {
                    sessionAttribute.Set(newsession);
                    SessionManager.Instance.AddSession(context.Channel.RemoteAddress, newsession);

                    //SimplePacketCreator.HandshakeAccept(buffer, sessionId, randomKey);
                }

                context.WriteAndFlushAsync(buffer);
            }
            else
            {
                //TODO:重连
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            base.ChannelRead(context, message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
        }
    }
}
