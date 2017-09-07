#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：LampTcpServerHandler.cs
// 创建日期：2017-08-28

#endregion

using System;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Lamp.Utilities;
using Lamp.Utilities.Crypto.RSA;
using Microsoft.Extensions.Logging;

namespace Lamp.Agent.Server
{
    internal sealed class LampTcpServerHandler : ChannelHandlerAdapter
    {
        private static readonly ILogger logger = ApplicationLogging.CreateLogger<Program>();

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

                //var randomKeyBytes = new byte[sizeof(int)];
                //Randomizer.GetBytes(randomKeyBytes);
                //var randomKey = BitConverter.ToInt32(randomKeyBytes, 0);

                ////用户连入数已达到最大
                //if (sessionId == NetworkOperationCode.MAX_CONN_EXCEED)
                //{
                //    SimplePacketCreator.HandshakeRefuse(buffer);
                //}
                //else
                //{
                sessionAttribute.Set(newsession);
                SessionManager.Instance.AddSession(context.Channel.RemoteAddress, newsession);

                //    SimplePacketCreator.HandshakeAccept(buffer, sessionId, randomKey, 0, 1);
                //}

                //context.WriteAndFlushAsync(buffer);
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is IByteBuffer byteBuffer))
                return;

            var content = new byte[byteBuffer.ReadableBytes];
            byteBuffer.GetBytes(byteBuffer.ReaderIndex, content);
            var plain = RsaCryptor.Decrypt(content);
            Console.WriteLine($"[Recv]{BitConverter.ToString(plain)}");
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            logger.LogWarning(exception, $"在{nameof(LampTcpServerHandler)}中捕获到一个错误");
        }
    }
}