using System;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Buffers;

namespace Lamp.Network.Server
{
    class BedRockUdpServerHandler : ChannelHandlerAdapter
    {
        private SessionManager mSessionManager;

        private readonly Action<Session> mConnectAcceptAction;
        private Action<Session> mDisconnectAction;
        private Action<IByteBuffer,Session> mReceiveAction;
        private Action<Session> mConnectRefuseAction;

        public BedRockUdpServerHandler(SessionManager sessionManager, Action<Session> connectAcceptAction,
            Action<Session> disconnectAction, Action<IByteBuffer, Session> receiveAction,Action<Session> connectRefuseAction)
        {
            mSessionManager = sessionManager;

            mConnectAcceptAction = connectAcceptAction;       
            mConnectRefuseAction = connectRefuseAction;

            mDisconnectAction = disconnectAction;
            mReceiveAction = receiveAction;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var packet = (DatagramPacket)message;

            if(packet.Content.ReadableBytes <= 0)
            {
                //丢弃
            }

            var session = mSessionManager.FindSession(packet.Sender);
            
            //新用户连入
            if (session == null)
            {
                var sessionId = mSessionManager.GetNewSessionId();
                session = Session.Create(context.Channel, packet.Sender, sessionId);

                //用户连入数已达到最大
                if (sessionId == NetworkOperationCode.MAX_CONN_EXCEED)
                {
                    mConnectRefuseAction(session);
                }
                else
                {
                    
                    if (mSessionManager.AddSession(packet.Sender, session))
                    {
                        mConnectAcceptAction(session);
                    }
                    else
                    {
                        mConnectRefuseAction(session);
                    }

                }
            }
            else
            {
                session.RecvData(packet.Content);
            }


        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            
        }
    }
}
