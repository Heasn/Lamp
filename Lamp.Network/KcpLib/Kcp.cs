using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DotNetty.Buffers;

namespace Lamp.Network.KcpLib
{
    class Kcp
    {
        public const int IKCP_RTO_NDL = 30;  // no delay min rto
        public const int IKCP_RTO_MIN = 100; // normal min rto
        public const int IKCP_RTO_DEF = 200;
        public const int IKCP_RTO_MAX = 60000;
        public const int IKCP_CMD_PUSH = 81; // cmd: push data
        public const int IKCP_CMD_ACK = 82; // cmd: ack
        public const int IKCP_CMD_WASK = 83; // cmd: window probe (ask)
        public const int IKCP_CMD_WINS = 84; // cmd: window size (tell)
        public const int IKCP_ASK_SEND = 1;  // need to send IKCP_CMD_WASK
        public const int IKCP_ASK_TELL = 2;  // need to send IKCP_CMD_WINS
        public const int IKCP_WND_SND = 32;
        public const int IKCP_WND_RCV = 32;
        public const int IKCP_MTU_DEF = 1400;
        public const int IKCP_ACK_FAST = 3;
        public const int IKCP_INTERVAL = 100;
        public const int IKCP_OVERHEAD = 24;
        public const int IKCP_DEADLINK = 10;
        public const int IKCP_THRESH_INIT = 2;
        public const int IKCP_THRESH_MIN = 2;
        public const int IKCP_PROBE_INIT = 7000;   // 7 secs to probe window size
        public const int IKCP_PROBE_LIMIT = 120000; // up to 120 secs to probe window

        private int m_Conv;
        private int m_Mtu;
        private int m_Mss;
        private int m_SndUna;
        private int m_SndNxt;
        private int m_RcvNxt;
        private int m_Ssthresh;
        private int m_RxRttval;
        private int m_RxSrtt;
        private int m_RxRto;
        private int m_RxMinrto;
        private int m_SndWnd;
        private int m_RcvWnd;
        private int m_RmtWnd;
        private int m_Cwnd;
        private int m_Probe;
        private int m_Current;
        private int m_Interval;
        private int m_TsFlush;
        private int m_Nodelay;
        private int m_Updated;
        private int m_TsProbe;
        private int m_ProbeWait;
        private int m_Incr;
        private readonly LinkedList<Segment> m_SndQueue = new LinkedList<Segment>();
        private readonly LinkedList<Segment> m_RcvQueue = new LinkedList<Segment>();
        private readonly LinkedList<Segment> m_SndBuf =  new LinkedList<Segment>();
        private readonly LinkedList<Segment> m_RcvBuf =  new LinkedList<Segment>();
        private readonly LinkedList<int> m_Acklist = new LinkedList<int>();
        private IByteBuffer m_Buffer;
        private int m_Fastresend;
        private int m_Nocwnd;
        private bool m_Stream;//流模式
        private readonly object m_User;//远端地址
        private int m_NextUpdate;//the next update time.
        private readonly OutPutDelegate m_OutPut;

        public delegate void OutPutDelegate(IByteBuffer buf, Kcp kcp, object user);

        public Kcp(OutPutDelegate output, object user)
        {
            m_SndWnd = IKCP_WND_SND;
            m_RcvWnd = IKCP_WND_RCV;
            m_RmtWnd = IKCP_WND_RCV;
            m_Mtu = IKCP_MTU_DEF;
            m_Mss = m_Mtu - IKCP_OVERHEAD;
            m_RxRto = IKCP_RTO_DEF;
            m_RxMinrto = IKCP_RTO_MIN;
            m_Interval = IKCP_INTERVAL;
            m_TsFlush = IKCP_INTERVAL;
            m_Ssthresh = IKCP_THRESH_INIT;
            m_Buffer = PooledByteBufferAllocator.Default.Buffer((m_Mtu + IKCP_OVERHEAD) * 3);
            m_OutPut = output;
            m_User = user;
        }

        private static int Bound(int lower, int middle, int upper)
        {
            return Math.Min(Math.Max(lower, middle), upper);
        }

        private static int TimeDiff(int later, int earlier)
        {
            return later - earlier;
        }

        public int PeekSize()
        {
            if (!m_RcvQueue.Any())
            {
                return -1;
            }

            Segment seq = m_RcvQueue.First();
            
            if (seq.Frg == 0)
            {
                return seq.Data.ReadableBytes;
            }
            if (m_RcvQueue.Count < seq.Frg + 1)
            {
                return -1;
            }
            int length = 0;
            foreach (Segment item in m_RcvQueue)
            {
                length += item.Data.ReadableBytes;
                if (item.Frg == 0)
                {
                    break;
                }
            }
            return length;
        }

        public int Receive(IByteBuffer buf)
        {
            if (!m_RcvQueue.Any())
            {
                return -1;
            }
            int peekSize = PeekSize();
            if (peekSize < 0)
            {
                return -2;
            }
            var recover = m_RcvQueue.Count >= m_RcvWnd;
            // merge fragment.
            int c = 0;
            int len = 0;
            foreach (Segment seg in m_RcvQueue)
            {
                len += seg.Data.ReadableBytes;
                buf.WriteBytes(seg.Data);
                c++;
                if (seg.Frg == 0)
                {
                    break;
                }
            }

            if (c > 0)
            {
                for (int i = 0; i < c; i++)
                {
                    var first = m_RcvQueue.First();
                    first.Data.Release();
                    m_RcvQueue.RemoveFirst();
                }
            }

            if (len != peekSize)
            {
                throw new Exception("数据异常.");
            }

            // move available data from rcv_buf -> rcv_queue
            c = 0;
            foreach (Segment seg in m_RcvBuf)
            {
                if (seg.Sn == m_RcvNxt && m_RcvQueue.Count < m_RcvWnd)
                {
                    m_RcvQueue.AddLast(seg);
                    m_RcvNxt++;
                    c++;
                }
                else
                {
                    break;
                }
            }
            if (c > 0)
            {
                for (int i = 0; i < c; i++)
                {
                    m_RcvBuf.RemoveFirst();
                }
            }
            // fast recover
            if (m_RcvQueue.Count < m_RcvWnd && recover)
            {
                // ready to send back IKCP_CMD_WINS in ikcp_flush
                // tell remote my window size
                m_Probe |= IKCP_ASK_TELL;
            }
            return len;
        }

        public int Send(IByteBuffer buf)
        {
            if (buf.ReadableBytes == 0)
            {
                return -1;
            }
            // append to previous segment in streaming mode (if possible)
            if (m_Stream && m_SndQueue.Any())
            {
                Segment seg = m_SndQueue.Last();
                if (seg.Data != null && seg.Data.ReadableBytes < m_Mss)
                {
                    int capacity = m_Mss - seg.Data.ReadableBytes;
                    int extend = buf.ReadableBytes < capacity ? buf.ReadableBytes : capacity;
                    seg.Data.WriteBytes(buf, extend);
                    if (buf.ReadableBytes == 0)
                    {
                        return 0;
                    }
                }
            }
            int count;
            if (buf.ReadableBytes <= m_Mss)
            {
                count = 1;
            }
            else
            {
                count = (buf.ReadableBytes + m_Mss - 1) / m_Mss;
            }
            if (count > 255)
            {
                return -2;
            }
            if (count == 0)
            {
                count = 1;
            }
            //fragment
            for (int i = 0; i < count; i++)
            {
                int size = buf.ReadableBytes > m_Mss ? m_Mss : buf.ReadableBytes;
                Segment seg = new Segment(size);
                seg.Data.WriteBytes(buf, size);
                seg.Frg = m_Stream ? 0 : count - i - 1;
                m_SndQueue.AddLast(seg);
            }
            buf.Release();
            return 0;
        }

        private void Update_ack(int rtt)
        {
            if (m_RxSrtt == 0)
            {
                m_RxSrtt = rtt;
                m_RxRttval = rtt / 2;
            }
            else
            {
                int delta = rtt - m_RxSrtt;
                if (delta < 0)
                {
                    delta = -delta;
                }
                m_RxRttval = (3 * m_RxRttval + delta) / 4;
                m_RxSrtt = (7 * m_RxSrtt + rtt) / 8;
                if (m_RxSrtt < 1)
                {
                    m_RxSrtt = 1;
                }
            }
            int rto = m_RxSrtt + Math.Max(m_Interval, 4 * m_RxRttval);
            m_RxRto = Bound(m_RxMinrto, rto, IKCP_RTO_MAX);
        }

        private void Shrink_buf()
        {
            m_SndUna = m_SndBuf.Any() ? m_SndBuf.First().Sn : m_SndNxt;
        }

        private void Parse_ack(int sn)
        {
            if (TimeDiff(sn, m_SndUna) < 0 || TimeDiff(sn, m_SndNxt) >= 0)
            {
                return;
            }

            foreach (var seg in m_SndBuf)
            {
                if (sn == seg.Sn)
                {
                    m_SndBuf.Remove(seg);
                    seg.Data.Release(seg.Data.ReferenceCount);
                    break;
                }

                if (TimeDiff(sn, seg.Sn) < 0)
                {
                    break;
                }
            }

        }

        private void Parse_una(int una)
        {
            int c = 0;
            foreach (Segment seg in m_SndBuf)
            {
                if (TimeDiff(una, seg.Sn) > 0)
                {
                    c++;
                }
                else
                {
                    break;
                }
            }
            if (c > 0)
            {
                for (int i = 0; i < c; i++)
                {
                    Segment seg = m_SndBuf.First();
                    m_SndBuf.Remove(seg);
                    seg.Data.Release(seg.Data.ReferenceCount);
                }
            }
        }

        private void Parse_fastack(int sn)
        {
            if (TimeDiff(sn, m_SndUna) < 0 || TimeDiff(sn, m_SndNxt) >= 0)
            {
                return;
            }
            foreach (Segment seg in m_SndBuf)
            {
                if (TimeDiff(sn, seg.Sn) < 0)
                {
                    break;
                }

                if (sn != seg.Sn)
                {
                    seg.Fastack++;
                }
            }
        }

        private void Ack_push(int sn, int ts)
        {
            m_Acklist.AddLast(sn);
            m_Acklist.AddLast(ts);
        }

        private (int sn,int ts) Ack_get(int p)
        {
            IntPtr ptr = IntPtr.Zero;
            Marshal.StructureToPtr(m_Acklist, ptr, false);
            var sn = Marshal.PtrToStructure<int>(ptr + p * 2 + 0);
            var ts = Marshal.PtrToStructure<int>(ptr + p * 2 + 1);
            return (sn, ts);
        }

        private void Parse_data(Segment newseg)
        {
            var sn = newseg.Sn;
            if (TimeDiff(sn, m_RcvNxt + m_RcvWnd) >= 0 || TimeDiff(sn, m_RcvNxt) < 0)
            {
                newseg.Release();
                return;
            }

            var repeat = false;

            LinkedListNode<Segment> p, prev;
            for (p = m_RcvBuf.Last; p != m_RcvBuf.First; p = prev)
            {
                var seg = p.Value;
                prev = p.Previous;
                if (seg.Sn == sn)
                {
                    repeat = true;
                    break;
                }

                if (TimeDiff(sn, seg.Sn) > 0)
                {
                    break;
                }
            }

            if (repeat == false)
            {
                if (p == null)
                {
                    m_RcvBuf.AddFirst(newseg);
                }
                else
                {
                    m_RcvBuf.AddBefore(p, newseg);
                }
            }
            else
            {
                newseg.Release();
            }


            // move available data from rcv_buf -> rcv_queue
            int c = 0;
            foreach (Segment seg in m_RcvBuf)
            {
                if (seg.Sn == m_RcvNxt && m_RcvQueue.Count < m_RcvWnd)
                {
                    m_RcvQueue.AddLast(seg);
                    m_RcvNxt++;
                    c++;
                }
                else
                {
                    break;
                }
            }
            if (0 < c)
            {
                for (int i = 0; i < c; i++)
                {
                    m_RcvBuf.RemoveFirst();
                }
            }
        }

        public int Input(IByteBuffer data)
        {
            int unaTemp = m_SndUna;
            int flag = 0, maxack = 0;
            if (data == null || data.ReadableBytes < IKCP_OVERHEAD)
            {
                return -1;
            }
            while (true)
            {
                bool readed = false;
                int ts;
                int sn;
                int len;
                int una;
                int convTemp;
                int wnd;
                byte cmd;
                byte frg;
                if (data.ReadableBytes < IKCP_OVERHEAD)
                {
                    break;
                }
                convTemp = data.ReadInt();
                if (m_Conv != convTemp)
                {
                    return -1;
                }
                cmd = data.ReadByte();
                frg = data.ReadByte();
                wnd = data.ReadShort();
                ts = data.ReadInt();
                sn = data.ReadInt();
                una = data.ReadInt();
                len = data.ReadInt();
                if (data.ReadableBytes < len)
                {
                    return -2;
                }
                switch ((int)cmd)
                {
                    case IKCP_CMD_PUSH:
                    case IKCP_CMD_ACK:
                    case IKCP_CMD_WASK:
                    case IKCP_CMD_WINS:
                        break;
                    default:
                        return -3;
                }
                m_RmtWnd = wnd & 0x0000ffff;
                Parse_una(una);
                Shrink_buf();
                switch (cmd)
                {
                    case IKCP_CMD_ACK:
                        if (TimeDiff(m_Current, ts) >= 0)
                        {
                            Update_ack(TimeDiff(m_Current, ts));
                        }
                        Parse_ack(sn);
                        Shrink_buf();
                        if (flag == 0)
                        {
                            flag = 1;
                            maxack = sn;
                        }
                        else if (TimeDiff(sn, maxack) > 0)
                        {
                            maxack = sn;
                        }
                        break;
                    case IKCP_CMD_PUSH:
                        if (TimeDiff(sn, m_RcvNxt + m_RcvWnd) < 0)
                        {
                            Ack_push(sn, ts);
                            if (TimeDiff(sn, m_RcvNxt) >= 0)
                            {
                                Segment seg = new Segment(len)
                                {
                                    Conv = convTemp,
                                    Cmd = cmd,
                                    Frg = frg & 0x000000ff,
                                    Wnd = wnd,
                                    Ts = ts,
                                    Sn = sn,
                                    Una = una
                                };
                                if (len > 0)
                                {
                                    seg.Data.WriteBytes(data, len);
                                    readed = true;
                                }
                                Parse_data(seg);
                            }
                        }
                        break;
                    case IKCP_CMD_WASK:
                        // ready to send back IKCP_CMD_WINS in Ikcp_flush
                        // tell remote my window size
                        m_Probe |= IKCP_ASK_TELL;
                        break;
                    case IKCP_CMD_WINS:
                        // do nothing
                        break;
                    default:
                        return -3;
                }
                if (!readed)
                {
                    data.SkipBytes(len);
                }
            }
            if (flag != 0)
            {
                Parse_fastack(maxack);
            }
            if (TimeDiff(m_SndUna, unaTemp) > 0)
            {
                if (m_Cwnd < m_RmtWnd)
                {
                    if (m_Cwnd < m_Ssthresh)
                    {
                        m_Cwnd++;
                        m_Incr += m_Mss;
                    }
                    else
                    {
                        if (m_Incr < m_Mss)
                        {
                            m_Incr = m_Mss;
                        }
                        m_Incr += m_Mss * m_Mss / m_Incr + m_Mss / 16;
                        if ((m_Cwnd + 1) * m_Mss <= m_Incr)
                        {
                            m_Cwnd++;
                        }
                    }
                    if (m_Cwnd > m_RmtWnd)
                    {
                        m_Cwnd = m_RmtWnd;
                        m_Incr = m_RmtWnd * m_Mss;
                    }
                }
            }
            return 0;
        }

        private int Wnd_unused()
        {
            if (m_RcvQueue.Count < m_RcvWnd)
            {
                return m_RcvWnd - m_RcvQueue.Count;
            }
            return 0;
        }

        private void Flush()
        {
            int cur = m_Current;
            int change = 0;
            int lost = 0;
            if (m_Updated == 0)
            {
                return;
            }
            Segment seg = new Segment(0)
            {
                Conv = m_Conv,
                Cmd = IKCP_CMD_ACK,
                Wnd = Wnd_unused(),
                Una = m_RcvNxt
            };
            // flush acknowledges
            int c = m_Acklist.Count / 2;
            for (int i = 0; i < c; i++)
            {
                if (m_Buffer.ReadableBytes + IKCP_OVERHEAD > m_Mtu)
                {
                    m_OutPut(m_Buffer, this, m_User);
                    m_Buffer = PooledByteBufferAllocator.Default.Buffer((m_Mtu + IKCP_OVERHEAD) * 3);
                }

                var tuple = Ack_get(i);
                seg.Sn = tuple.sn;
                seg.Ts = tuple.ts;

                seg.Encode(m_Buffer);
            }
            m_Acklist.Clear();
            // probe window size (if remote window size equals zero)
            if (m_RmtWnd == 0)
            {
                if (m_ProbeWait == 0)
                {
                    m_ProbeWait = IKCP_PROBE_INIT;
                    m_TsProbe = m_Current + m_ProbeWait;
                }
                else if (TimeDiff(m_Current, m_TsProbe) >= 0)
                {
                    if (m_ProbeWait < IKCP_PROBE_INIT)
                    {
                        m_ProbeWait = IKCP_PROBE_INIT;
                    }
                    m_ProbeWait += m_ProbeWait / 2;
                    if (m_ProbeWait > IKCP_PROBE_LIMIT)
                    {
                        m_ProbeWait = IKCP_PROBE_LIMIT;
                    }
                    m_TsProbe = m_Current + m_ProbeWait;
                    m_Probe |= IKCP_ASK_SEND;
                }
            }
            else
            {
                m_TsProbe = 0;
                m_ProbeWait = 0;
            }
            // flush window probing commands
            if ((m_Probe & IKCP_ASK_SEND) != 0)
            {
                seg.Cmd = IKCP_CMD_WASK;
                if (m_Buffer.ReadableBytes + IKCP_OVERHEAD > m_Mtu)
                {
                    m_OutPut(m_Buffer, this, m_User);
                    m_Buffer = PooledByteBufferAllocator.Default.Buffer((m_Mtu + IKCP_OVERHEAD) * 3);
                }
                seg.Encode(m_Buffer);
            }
            // flush window probing commands
            if ((m_Probe & IKCP_ASK_TELL) != 0)
            {
                seg.Cmd = IKCP_CMD_WINS;
                if (m_Buffer.ReadableBytes + IKCP_OVERHEAD > m_Mtu)
                {
                    m_OutPut(m_Buffer, this, m_User);
                    m_Buffer = PooledByteBufferAllocator.Default.Buffer((m_Mtu + IKCP_OVERHEAD) * 3);
                }
                seg.Encode(m_Buffer);
            }
            m_Probe = 0;
            // calculate window size
            int cwndTemp = Math.Min(m_SndWnd, m_RmtWnd);
            if (m_Nocwnd == 0)
            {
                cwndTemp = Math.Min(m_Cwnd, cwndTemp);
            }
            // move data from snd_queue to snd_buf
            c = 0;
            foreach (Segment item in m_SndQueue)
            {
                if (TimeDiff(m_SndNxt, m_SndUna + cwndTemp) >= 0)
                {
                    break;
                }
                Segment newseg = item;
                newseg.Conv = m_Conv;
                newseg.Cmd = IKCP_CMD_PUSH;
                newseg.Wnd = seg.Wnd;
                newseg.Ts = cur;
                newseg.Sn = m_SndNxt++;
                newseg.Una = m_RcvNxt;
                newseg.Resendts = cur;
                newseg.Rto = m_RxRto;
                newseg.Fastack = 0;
                newseg.Xmit = 0;
                m_SndBuf.AddLast(newseg);
                c++;
            }
            if (c > 0)
            {
                for (int i = 0; i < c; i++)
                {
                    m_SndQueue.RemoveFirst();
                }
            }
            // calculate resent
            int resent = m_Fastresend > 0 ? m_Fastresend : int.MaxValue;
            int rtomin = m_Nodelay == 0 ? m_RxRto >> 3 : 0;
            // flush data segments
            foreach (Segment segment in m_SndBuf)
            {
                bool needsend = false;
                if (segment.Xmit == 0)
                {
                    needsend = true;
                    segment.Xmit++;
                    segment.Rto = m_RxRto;
                    segment.Resendts = cur + segment.Rto + rtomin;
                }
                else if (TimeDiff(cur, segment.Resendts) >= 0)
                {
                    needsend = true;
                    segment.Xmit++;
                    if (m_Nodelay == 0)
                    {
                        segment.Rto += m_RxRto;
                    }
                    else
                    {
                        segment.Rto += m_RxRto / 2;
                    }
                    segment.Resendts = cur + segment.Rto;
                    lost = 1;
                }
                else if (segment.Fastack >= resent)
                {
                    needsend = true;
                    segment.Xmit++;
                    segment.Fastack = 0;
                    segment.Resendts = cur + segment.Rto;
                    change++;
                }
                if (needsend)
                {
                    segment.Ts = cur;
                    segment.Wnd = seg.Wnd;
                    segment.Una = m_RcvNxt;
                    int need = IKCP_OVERHEAD + segment.Data.ReadableBytes;
                    if (m_Buffer.ReadableBytes + need > m_Mtu)
                    {
                        m_OutPut(m_Buffer, this, m_User);
                        m_Buffer = PooledByteBufferAllocator.Default.Buffer((m_Mtu + IKCP_OVERHEAD) * 3);
                    }
                    segment.Encode(m_Buffer);
                    if (segment.Data.ReadableBytes > 0)
                    {
                        m_Buffer.WriteBytes(segment.Data.Duplicate());
                    }
                    //if (segment.Xmit >= _deadLink)
                    //{
                    //    m_State = -1;
                    //}
                }
            }
            // flash remain segments
            if (m_Buffer.ReadableBytes > 0)
            {
                m_OutPut(m_Buffer, this, m_User);
                m_Buffer = PooledByteBufferAllocator.Default.Buffer((m_Mtu + IKCP_OVERHEAD) * 3);
            }
            // update ssthresh
            if (change != 0)
            {
                int inflight = m_SndNxt - m_SndUna;
                m_Ssthresh = inflight / 2;
                if (m_Ssthresh < IKCP_THRESH_MIN)
                {
                    m_Ssthresh = IKCP_THRESH_MIN;
                }
                m_Cwnd = m_Ssthresh + resent;
                m_Incr = m_Cwnd * m_Mss;
            }
            if (lost != 0)
            {
                m_Ssthresh = m_Cwnd / 2;
                if (m_Ssthresh < IKCP_THRESH_MIN)
                {
                    m_Ssthresh = IKCP_THRESH_MIN;
                }
                m_Cwnd = 1;
                m_Incr = m_Mss;
            }
            if (m_Cwnd < 1)
            {
                m_Cwnd = 1;
                m_Incr = m_Mss;
            }
        }

        public void Update(long current)
        {
            m_Current = (int)current;
            if (m_Updated == 0)
            {
                m_Updated = 1;
                m_TsFlush = m_Current;
            }
            int slap = TimeDiff(m_Current, m_TsFlush);
            if (slap >= 10000 || slap < -10000)
            {
                m_TsFlush = m_Current;
                slap = 0;
            }
            if (slap >= 0)
            {
                m_TsFlush += m_Interval;
                if (TimeDiff(m_Current, m_TsFlush) >= 0)
                {
                    m_TsFlush = m_Current + m_Interval;
                }
                Flush();
            }
        }

        public int Check(long curr)
        {
            int cur = (int)curr;
            if (m_Updated == 0)
            {
                return cur;
            }
            int tsFlushTemp = m_TsFlush;
            int tmPacket = 0x7fffffff;
            if (TimeDiff(cur, tsFlushTemp) >= 10000 || TimeDiff(cur, tsFlushTemp) < -10000)
            {
                tsFlushTemp = cur;
            }
            if (TimeDiff(cur, tsFlushTemp) >= 0)
            {
                return cur;
            }
            int tmFlush = TimeDiff(tsFlushTemp, cur);
            foreach (Segment seg in m_SndBuf)
            {
                int diff = TimeDiff(seg.Resendts, cur);
                if (diff <= 0)
                {
                    return cur;
                }
                if (diff < tmPacket)
                {
                    tmPacket = diff;
                }
            }
            int minimal = tmPacket < tmFlush ? tmPacket : tmFlush;
            if (minimal >= m_Interval)
            {
                minimal = m_Interval;
            }
            return cur + minimal;
        }

        public int SetMtu(int mtu)
        {
            if (mtu < 50 || mtu < IKCP_OVERHEAD)
            {
                return -1;
            }
            IByteBuffer buf = PooledByteBufferAllocator.Default.Buffer((mtu + IKCP_OVERHEAD) * 3);
            m_Mtu = mtu;
            m_Mss = mtu - IKCP_OVERHEAD;
            m_Buffer?.Release();
            m_Buffer = buf;
            return 0;
        }

        public void SetConv(int conv)
        {
            m_Conv = conv;
        }

        public int GetConv()
        {
            return m_Conv;
        }

        public int Interval(int interval)
        {
            if (interval > 5000)
            {
                interval = 5000;
            }
            else if (interval < 10)
            {
                interval = 10;
            }
            m_Interval = interval;
            return 0;
        }

        public int NoDelay(int nodelay, int interval, int resend, int nc)
        {
            if (nodelay >= 0)
            {
                m_Nodelay = nodelay;
                m_RxMinrto = nodelay != 0 ? IKCP_RTO_NDL : IKCP_RTO_MIN;
            }
            if (interval >= 0)
            {
                if (interval > 5000)
                {
                    interval = 5000;
                }
                else if (interval < 10)
                {
                    interval = 10;
                }
                m_Interval = interval;
            }
            if (resend >= 0)
            {
                m_Fastresend = resend;
            }
            if (nc >= 0)
            {
                m_Nocwnd = nc;
            }
            return 0;
        }

        public int WndSize(int sndwnd, int rcvwnd)
        {
            if (sndwnd > 0)
            {
                m_SndWnd = sndwnd;
            }
            if (rcvwnd > 0)
            {
                m_RcvWnd = rcvwnd;
            }
            return 0;
        }

        public int WaitSnd()
        {
            return m_SndBuf.Count + m_SndQueue.Count;
        }

        public void SetNextUpdate(int nextUpdate)
        {
            m_NextUpdate = nextUpdate;
        }

        public int GetNextUpdate()
        {
            return m_NextUpdate;
        }

        public Object GetUser()
        {
            return m_User;
        }

        public bool IsStream()
        {
            return m_Stream;
        }

        public void SetStream(bool stream)
        {
            m_Stream = stream;
        }

        public void SetMinRto(int min)
        {
            m_RxMinrto = min;
        }

        public void Release()
        {
            if (m_Buffer.ReferenceCount > 0)
            {
                m_Buffer.Release(m_Buffer.ReferenceCount);
            }
            foreach (Segment seg in m_RcvBuf)
            {
                seg.Release();
            }
            foreach (Segment seg in m_RcvQueue)
            {
                seg.Release();
            }
            foreach (Segment seg in m_SndBuf)
            {
                seg.Release();
            }
            foreach (Segment seg in m_SndQueue)
            {
                seg.Release();
            }
        }
    }
}
