#region 文件描述

// 开发者：陈柏宇
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：Segment.cs
// 创建日期：2017-08-27

#endregion

using DotNetty.Buffers;

namespace Lamp.Agent.KcpLib
{
    internal class Segment
    {
        public byte Cmd;
        public int Conv;
        public IByteBuffer Data;
        public int Fastack;
        public int Frg;
        public int Resendts;
        public int Rto;
        public int Sn;
        public int Ts;
        public int Una;
        public int Wnd;
        public int Xmit;

        public Segment(int size)
        {
            if (size > 0)
                Data = PooledByteBufferAllocator.Default.Buffer(size);
        }

        public int Encode(IByteBuffer buf)
        {
            var startIndex = buf.WriterIndex;

            buf.WriteInt(Conv);
            buf.WriteByte(Cmd);
            buf.WriteByte(Frg);
            buf.WriteShort(Wnd);
            buf.WriteInt(Ts);
            buf.WriteInt(Sn);
            buf.WriteInt(Una);
            buf.WriteInt(Data?.ReadableBytes ?? 0);

            return buf.WriterIndex - startIndex;
        }

        public void Release()
        {
            if (Data != null && Data.ReferenceCount > 0)
                Data.Release(Data.ReferenceCount);
        }
    }
}