#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：LampUdpServerHandler.cs
// 创建日期：2017-08-28

#endregion

using System;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Lamp.Agent.Server
{
    internal sealed class LampUdpServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var packet = (DatagramPacket) message;
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
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
        }
    }
}