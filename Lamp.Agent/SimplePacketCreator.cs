using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;

namespace Lamp.Agent
{
    class SimplePacketCreator
    {
        /// <summary>
        /// 握手通过
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="sessionId">会话ID</param>
        /// <param name="randomKey">随机Key</param>
        /// <param name="majorVersion">主版本号</param>
        /// <param name="subVersion">副版本号</param>
        public static void HandshakeAccept(IByteBuffer buffer, int sessionId, int randomKey, byte majorVersion, byte subVersion)
        {
            buffer.WriteBoolean(true);
            buffer.WriteInt(sessionId);
            buffer.WriteInt(randomKey);
            buffer.WriteByte(majorVersion);
            buffer.WriteByte(subVersion);
        }

        public static void HandshakeRefuse(IByteBuffer buffer)
        {
            buffer.WriteBoolean(false);
        }
    }
}
