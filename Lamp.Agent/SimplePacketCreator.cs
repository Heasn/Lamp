#region 文件描述

// 开发者：CHENBAIYU
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：SimplePacketCreator.cs
// 创建日期：2017-08-28

#endregion

using DotNetty.Buffers;

namespace Lamp.Agent
{
    internal class SimplePacketCreator
    {
        /// <summary>
        ///     握手通过
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="sessionId">会话ID</param>
        /// <param name="randomKey">随机Key</param>
        /// <param name="majorVersion">主版本号</param>
        /// <param name="subVersion">副版本号</param>
        public static void HandshakeAccept(IByteBuffer buffer, int sessionId, int randomKey, byte majorVersion,
            byte subVersion)
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