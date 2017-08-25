using DotNetty.Buffers;

namespace Lamp.Network.KcpLib
{
    class Segment
    {
        public int Conv;
        public byte Cmd;
        public int Frg;
        public int Wnd;
        public int Ts;
        public int Sn;
        public int Una;
        public int Resendts;
        public int Rto;
        public int Fastack;
        public int Xmit;
        public IByteBuffer Data;

        public Segment(int size)
        {
            if (size > 0)
            {
                Data = PooledByteBufferAllocator.Default.Buffer(size);
            }
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
            {
                Data.Release(Data.ReferenceCount);
            }
        }
    }
}

