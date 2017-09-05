#region 文件描述

// 开发者：陈柏宇
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：Kcp.cs
// 创建日期：2017-08-27

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using DotNetty.Buffers;

namespace Lamp.Agent.KcpLib
{
    internal class Kcp
    {
        public const int IKCP_ACK_FAST = 3;
        public const int IKCP_ASK_SEND = 1; // need to send IKCP_CMD_WASK
        public const int IKCP_ASK_TELL = 2; // need to send IKCP_CMD_WINS
        public const int IKCP_CMD_ACK = 82; // cmd: ack
        public const int IKCP_CMD_PUSH = 81; // cmd: push data
        public const int IKCP_CMD_WASK = 83; // cmd: window probe (ask)
        public const int IKCP_CMD_WINS = 84; // cmd: window size (tell)
        public const int IKCP_DEADLINK = 10;
        public const int IKCP_INTERVAL = 100;
        public const int IKCP_MTU_DEF = 1400;
        public const int IKCP_OVERHEAD = 24;
        public const int IKCP_PROBE_INIT = 7000; // 7 secs to probe window size
        public const int IKCP_PROBE_LIMIT = 120000; // up to 120 secs to probe window
        public const int IKCP_RTO_DEF = 200;
        public const int IKCP_RTO_MAX = 60000;
        public const int IKCP_RTO_MIN = 100; // normal min rto
        public const int IKCP_RTO_NDL = 30; // no delay min rto
        public const int IKCP_THRESH_INIT = 2;
        public const int IKCP_THRESH_MIN = 2;
        public const int IKCP_WND_RCV = 32;
        public const int IKCP_WND_SND = 32;
        private readonly LinkedList<int> mAcklist = new LinkedList<int>();
        private readonly OutPutDelegate mOutPut;
        private readonly LinkedList<Segment> mRcvBuf = new LinkedList<Segment>();
        private readonly LinkedList<Segment> mRcvQueue = new LinkedList<Segment>();
        private readonly LinkedList<Segment> mSndBuf = new LinkedList<Segment>();
        private readonly LinkedList<Segment> mSndQueue = new LinkedList<Segment>();
        private readonly EndPoint mUser; //远端地址
        private IByteBuffer mBuffer;

        private int mConv;
        private int mCurrent;
        private int mCwnd;
        private int mFastresend;
        private int mIncr;
        private int mInterval;
        private int mMss;
        private int mMtu;
        private int mNextUpdate; //the next update time.
        private int mNocwnd;
        private int mNodelay;
        private int mProbe;
        private int mProbeWait;
        private int mRcvNxt;
        private int mRcvWnd;
        private int mRmtWnd;
        private int mRxMinrto;
        private int mRxRto;
        private int mRxRttval;
        private int mRxSrtt;
        private int mSndNxt;
        private int mSndUna;
        private int mSndWnd;
        private int mSsthresh;
        private bool mStream; //流模式
        private int mTsFlush;
        private int mTsProbe;
        private int mUpdated;

        public Kcp(OutPutDelegate output, EndPoint user)
        {
            mSndWnd = IKCP_WND_SND;
            mRcvWnd = IKCP_WND_RCV;
            mRmtWnd = IKCP_WND_RCV;
            mMtu = IKCP_MTU_DEF;
            mMss = mMtu - IKCP_OVERHEAD;
            mRxRto = IKCP_RTO_DEF;
            mRxMinrto = IKCP_RTO_MIN;
            mInterval = IKCP_INTERVAL;
            mTsFlush = IKCP_INTERVAL;
            mSsthresh = IKCP_THRESH_INIT;
            mBuffer = PooledByteBufferAllocator.Default.Buffer((mMtu + IKCP_OVERHEAD) * 3);
            mOutPut = output;
            mUser = user;
        }

        public delegate void OutPutDelegate(IByteBuffer buf, Kcp kcp, EndPoint user);

        public int Check(long curr)
        {
            var cur = (int) curr;
            if (mUpdated == 0)
                return cur;
            var tsFlushTemp = mTsFlush;
            var tmPacket = 0x7fffffff;
            if (TimeDiff(cur, tsFlushTemp) >= 10000 || TimeDiff(cur, tsFlushTemp) < -10000)
                tsFlushTemp = cur;
            if (TimeDiff(cur, tsFlushTemp) >= 0)
                return cur;
            var tmFlush = TimeDiff(tsFlushTemp, cur);
            foreach (var seg in mSndBuf)
            {
                var diff = TimeDiff(seg.Resendts, cur);
                if (diff <= 0)
                    return cur;
                if (diff < tmPacket)
                    tmPacket = diff;
            }
            var minimal = tmPacket < tmFlush ? tmPacket : tmFlush;
            if (minimal >= mInterval)
                minimal = mInterval;
            return cur + minimal;
        }

        public int GetConv()
        {
            return mConv;
        }

        public int GetNextUpdate()
        {
            return mNextUpdate;
        }

        public object GetUser()
        {
            return mUser;
        }

        public int Input(IByteBuffer data)
        {
            var unaTemp = mSndUna;
            int flag = 0, maxack = 0;
            if (data == null || data.ReadableBytes < IKCP_OVERHEAD)
                return -1;
            while (true)
            {
                var readed = false;
                int ts;
                int sn;
                int len;
                int una;
                int convTemp;
                int wnd;
                byte cmd;
                byte frg;
                if (data.ReadableBytes < IKCP_OVERHEAD)
                    break;
                convTemp = data.ReadInt();
                if (mConv != convTemp)
                    return -1;
                cmd = data.ReadByte();
                frg = data.ReadByte();
                wnd = data.ReadShort();
                ts = data.ReadInt();
                sn = data.ReadInt();
                una = data.ReadInt();
                len = data.ReadInt();
                if (data.ReadableBytes < len)
                    return -2;
                switch ((int) cmd)
                {
                    case IKCP_CMD_PUSH:
                    case IKCP_CMD_ACK:
                    case IKCP_CMD_WASK:
                    case IKCP_CMD_WINS:
                        break;
                    default:
                        return -3;
                }
                mRmtWnd = wnd & 0x0000ffff;
                Parse_una(una);
                Shrink_buf();
                switch (cmd)
                {
                    case IKCP_CMD_ACK:
                        if (TimeDiff(mCurrent, ts) >= 0)
                            Update_ack(TimeDiff(mCurrent, ts));
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
                        if (TimeDiff(sn, mRcvNxt + mRcvWnd) < 0)
                        {
                            Ack_push(sn, ts);
                            if (TimeDiff(sn, mRcvNxt) >= 0)
                            {
                                var seg = new Segment(len)
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
                        mProbe |= IKCP_ASK_TELL;
                        break;
                    case IKCP_CMD_WINS:
                        // do nothing
                        break;
                    default:
                        return -3;
                }
                if (!readed)
                    data.SkipBytes(len);
            }
            if (flag != 0)
                Parse_fastack(maxack);
            if (TimeDiff(mSndUna, unaTemp) > 0)
                if (mCwnd < mRmtWnd)
                {
                    if (mCwnd < mSsthresh)
                    {
                        mCwnd++;
                        mIncr += mMss;
                    }
                    else
                    {
                        if (mIncr < mMss)
                            mIncr = mMss;
                        mIncr += mMss * mMss / mIncr + mMss / 16;
                        if ((mCwnd + 1) * mMss <= mIncr)
                            mCwnd++;
                    }
                    if (mCwnd > mRmtWnd)
                    {
                        mCwnd = mRmtWnd;
                        mIncr = mRmtWnd * mMss;
                    }
                }
            return 0;
        }

        public int Interval(int interval)
        {
            if (interval > 5000)
                interval = 5000;
            else if (interval < 10)
                interval = 10;
            mInterval = interval;
            return 0;
        }

        public bool IsStream()
        {
            return mStream;
        }

        public int NoDelay(int nodelay, int interval, int resend, int nc)
        {
            if (nodelay >= 0)
            {
                mNodelay = nodelay;
                mRxMinrto = nodelay != 0 ? IKCP_RTO_NDL : IKCP_RTO_MIN;
            }
            if (interval >= 0)
            {
                if (interval > 5000)
                    interval = 5000;
                else if (interval < 10)
                    interval = 10;
                mInterval = interval;
            }
            if (resend >= 0)
                mFastresend = resend;
            if (nc >= 0)
                mNocwnd = nc;
            return 0;
        }

        public int PeekSize()
        {
            if (!mRcvQueue.Any())
                return -1;

            var seq = mRcvQueue.First();

            if (seq.Frg == 0)
                return seq.Data.ReadableBytes;
            if (mRcvQueue.Count < seq.Frg + 1)
                return -1;
            var length = 0;
            foreach (var item in mRcvQueue)
            {
                length += item.Data.ReadableBytes;
                if (item.Frg == 0)
                    break;
            }
            return length;
        }

        public int Receive(IByteBuffer buf)
        {
            if (!mRcvQueue.Any())
                return -1;
            var peekSize = PeekSize();
            if (peekSize < 0)
                return -2;
            var recover = mRcvQueue.Count >= mRcvWnd;
            // merge fragment.
            var c = 0;
            var len = 0;
            foreach (var seg in mRcvQueue)
            {
                len += seg.Data.ReadableBytes;
                buf.WriteBytes(seg.Data);
                c++;
                if (seg.Frg == 0)
                    break;
            }

            if (c > 0)
                for (var i = 0; i < c; i++)
                {
                    var first = mRcvQueue.First();
                    first.Data.Release();
                    mRcvQueue.RemoveFirst();
                }

            if (len != peekSize)
                throw new Exception("数据异常.");

            // move available data from rcv_buf -> rcv_queue
            c = 0;
            foreach (var seg in mRcvBuf)
                if (seg.Sn == mRcvNxt && mRcvQueue.Count < mRcvWnd)
                {
                    mRcvQueue.AddLast(seg);
                    mRcvNxt++;
                    c++;
                }
                else
                {
                    break;
                }
            if (c > 0)
                for (var i = 0; i < c; i++)
                    mRcvBuf.RemoveFirst();
            // fast recover
            if (mRcvQueue.Count < mRcvWnd && recover)
                mProbe |= IKCP_ASK_TELL;
            return len;
        }

        public void Release()
        {
            if (mBuffer.ReferenceCount > 0)
                mBuffer.Release(mBuffer.ReferenceCount);
            foreach (var seg in mRcvBuf)
                seg.Release();
            foreach (var seg in mRcvQueue)
                seg.Release();
            foreach (var seg in mSndBuf)
                seg.Release();
            foreach (var seg in mSndQueue)
                seg.Release();
        }

        public int Send(IByteBuffer buf)
        {
            if (buf.ReadableBytes == 0)
                return -1;
            // append to previous segment in streaming mode (if possible)
            if (mStream && mSndQueue.Any())
            {
                var seg = mSndQueue.Last();
                if (seg.Data != null && seg.Data.ReadableBytes < mMss)
                {
                    var capacity = mMss - seg.Data.ReadableBytes;
                    var extend = buf.ReadableBytes < capacity ? buf.ReadableBytes : capacity;
                    seg.Data.WriteBytes(buf, extend);
                    if (buf.ReadableBytes == 0)
                        return 0;
                }
            }
            int count;
            if (buf.ReadableBytes <= mMss)
                count = 1;
            else
                count = (buf.ReadableBytes + mMss - 1) / mMss;
            if (count > 255)
                return -2;
            if (count == 0)
                count = 1;
            //fragment
            for (var i = 0; i < count; i++)
            {
                var size = buf.ReadableBytes > mMss ? mMss : buf.ReadableBytes;
                var seg = new Segment(size);
                seg.Data.WriteBytes(buf, size);
                seg.Frg = mStream ? 0 : count - i - 1;
                mSndQueue.AddLast(seg);
            }
            buf.Release();
            return 0;
        }

        public void SetConv(int conv)
        {
            mConv = conv;
        }

        public void SetMinRto(int min)
        {
            mRxMinrto = min;
        }

        public int SetMtu(int mtu)
        {
            if (mtu < 50 || mtu < IKCP_OVERHEAD)
                return -1;
            var buf = PooledByteBufferAllocator.Default.Buffer((mtu + IKCP_OVERHEAD) * 3);
            mMtu = mtu;
            mMss = mtu - IKCP_OVERHEAD;
            mBuffer?.Release();
            mBuffer = buf;
            return 0;
        }

        public void SetNextUpdate(int nextUpdate)
        {
            mNextUpdate = nextUpdate;
        }

        public void SetStream(bool stream)
        {
            mStream = stream;
        }

        public void Update(long current)
        {
            mCurrent = (int) current;
            if (mUpdated == 0)
            {
                mUpdated = 1;
                mTsFlush = mCurrent;
            }
            var slap = TimeDiff(mCurrent, mTsFlush);
            if (slap >= 10000 || slap < -10000)
            {
                mTsFlush = mCurrent;
                slap = 0;
            }
            if (slap >= 0)
            {
                mTsFlush += mInterval;
                if (TimeDiff(mCurrent, mTsFlush) >= 0)
                    mTsFlush = mCurrent + mInterval;
                Flush();
            }
        }

        public int WaitSnd()
        {
            return mSndBuf.Count + mSndQueue.Count;
        }

        public int WndSize(int sndwnd, int rcvwnd)
        {
            if (sndwnd > 0)
                mSndWnd = sndwnd;
            if (rcvwnd > 0)
                mRcvWnd = rcvwnd;
            return 0;
        }

        private static int Bound(int lower, int middle, int upper)
        {
            return Math.Min(Math.Max(lower, middle), upper);
        }

        private static int TimeDiff(int later, int earlier)
        {
            return later - earlier;
        }

        private (int sn, int ts) Ack_get(int p)
        {
            var ptr = IntPtr.Zero;
            Marshal.StructureToPtr(mAcklist, ptr, false);
            var sn = Marshal.PtrToStructure<int>(ptr + p * 2 + 0);
            var ts = Marshal.PtrToStructure<int>(ptr + p * 2 + 1);
            return (sn, ts);
        }

        private void Ack_push(int sn, int ts)
        {
            mAcklist.AddLast(sn);
            mAcklist.AddLast(ts);
        }

        private void Flush()
        {
            var cur = mCurrent;
            var change = 0;
            var lost = 0;
            if (mUpdated == 0)
                return;
            var seg = new Segment(0)
            {
                Conv = mConv,
                Cmd = IKCP_CMD_ACK,
                Wnd = Wnd_unused(),
                Una = mRcvNxt
            };
            // flush acknowledges
            var c = mAcklist.Count / 2;
            for (var i = 0; i < c; i++)
            {
                if (mBuffer.ReadableBytes + IKCP_OVERHEAD > mMtu)
                {
                    mOutPut(mBuffer, this, mUser);
                    mBuffer = PooledByteBufferAllocator.Default.Buffer((mMtu + IKCP_OVERHEAD) * 3);
                }

                var tuple = Ack_get(i);
                seg.Sn = tuple.sn;
                seg.Ts = tuple.ts;

                seg.Encode(mBuffer);
            }
            mAcklist.Clear();
            // probe window size (if remote window size equals zero)
            if (mRmtWnd == 0)
            {
                if (mProbeWait == 0)
                {
                    mProbeWait = IKCP_PROBE_INIT;
                    mTsProbe = mCurrent + mProbeWait;
                }
                else if (TimeDiff(mCurrent, mTsProbe) >= 0)
                {
                    if (mProbeWait < IKCP_PROBE_INIT)
                        mProbeWait = IKCP_PROBE_INIT;
                    mProbeWait += mProbeWait / 2;
                    if (mProbeWait > IKCP_PROBE_LIMIT)
                        mProbeWait = IKCP_PROBE_LIMIT;
                    mTsProbe = mCurrent + mProbeWait;
                    mProbe |= IKCP_ASK_SEND;
                }
            }
            else
            {
                mTsProbe = 0;
                mProbeWait = 0;
            }
            // flush window probing commands
            if ((mProbe & IKCP_ASK_SEND) != 0)
            {
                seg.Cmd = IKCP_CMD_WASK;
                if (mBuffer.ReadableBytes + IKCP_OVERHEAD > mMtu)
                {
                    mOutPut(mBuffer, this, mUser);
                    mBuffer = PooledByteBufferAllocator.Default.Buffer((mMtu + IKCP_OVERHEAD) * 3);
                }
                seg.Encode(mBuffer);
            }
            // flush window probing commands
            if ((mProbe & IKCP_ASK_TELL) != 0)
            {
                seg.Cmd = IKCP_CMD_WINS;
                if (mBuffer.ReadableBytes + IKCP_OVERHEAD > mMtu)
                {
                    mOutPut(mBuffer, this, mUser);
                    mBuffer = PooledByteBufferAllocator.Default.Buffer((mMtu + IKCP_OVERHEAD) * 3);
                }
                seg.Encode(mBuffer);
            }
            mProbe = 0;
            // calculate window size
            var cwndTemp = Math.Min(mSndWnd, mRmtWnd);
            if (mNocwnd == 0)
                cwndTemp = Math.Min(mCwnd, cwndTemp);
            // move data from snd_queue to snd_buf
            c = 0;
            foreach (var item in mSndQueue)
            {
                if (TimeDiff(mSndNxt, mSndUna + cwndTemp) >= 0)
                    break;
                var newseg = item;
                newseg.Conv = mConv;
                newseg.Cmd = IKCP_CMD_PUSH;
                newseg.Wnd = seg.Wnd;
                newseg.Ts = cur;
                newseg.Sn = mSndNxt++;
                newseg.Una = mRcvNxt;
                newseg.Resendts = cur;
                newseg.Rto = mRxRto;
                newseg.Fastack = 0;
                newseg.Xmit = 0;
                mSndBuf.AddLast(newseg);
                c++;
            }
            if (c > 0)
                for (var i = 0; i < c; i++)
                    mSndQueue.RemoveFirst();
            // calculate resent
            var resent = mFastresend > 0 ? mFastresend : int.MaxValue;
            var rtomin = mNodelay == 0 ? mRxRto >> 3 : 0;
            // flush data segments
            foreach (var segment in mSndBuf)
            {
                var needsend = false;
                if (segment.Xmit == 0)
                {
                    needsend = true;
                    segment.Xmit++;
                    segment.Rto = mRxRto;
                    segment.Resendts = cur + segment.Rto + rtomin;
                }
                else if (TimeDiff(cur, segment.Resendts) >= 0)
                {
                    needsend = true;
                    segment.Xmit++;
                    if (mNodelay == 0)
                        segment.Rto += mRxRto;
                    else
                        segment.Rto += mRxRto / 2;
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
                    segment.Una = mRcvNxt;
                    var need = IKCP_OVERHEAD + segment.Data.ReadableBytes;
                    if (mBuffer.ReadableBytes + need > mMtu)
                    {
                        mOutPut(mBuffer, this, mUser);
                        mBuffer = PooledByteBufferAllocator.Default.Buffer((mMtu + IKCP_OVERHEAD) * 3);
                    }
                    segment.Encode(mBuffer);
                    if (segment.Data.ReadableBytes > 0)
                        mBuffer.WriteBytes(segment.Data.Duplicate());
                    //if (segment.Xmit >= _deadLink)
                    //{
                    //    m_State = -1;
                    //}
                }
            }
            // flash remain segments
            if (mBuffer.ReadableBytes > 0)
            {
                mOutPut(mBuffer, this, mUser);
                mBuffer = PooledByteBufferAllocator.Default.Buffer((mMtu + IKCP_OVERHEAD) * 3);
            }
            // update ssthresh
            if (change != 0)
            {
                var inflight = mSndNxt - mSndUna;
                mSsthresh = inflight / 2;
                if (mSsthresh < IKCP_THRESH_MIN)
                    mSsthresh = IKCP_THRESH_MIN;
                mCwnd = mSsthresh + resent;
                mIncr = mCwnd * mMss;
            }
            if (lost != 0)
            {
                mSsthresh = mCwnd / 2;
                if (mSsthresh < IKCP_THRESH_MIN)
                    mSsthresh = IKCP_THRESH_MIN;
                mCwnd = 1;
                mIncr = mMss;
            }
            if (mCwnd < 1)
            {
                mCwnd = 1;
                mIncr = mMss;
            }
        }

        private void Parse_ack(int sn)
        {
            if (TimeDiff(sn, mSndUna) < 0 || TimeDiff(sn, mSndNxt) >= 0)
                return;

            foreach (var seg in mSndBuf)
            {
                if (sn == seg.Sn)
                {
                    mSndBuf.Remove(seg);
                    seg.Data.Release(seg.Data.ReferenceCount);
                    break;
                }

                if (TimeDiff(sn, seg.Sn) < 0)
                    break;
            }
        }

        private void Parse_data(Segment newseg)
        {
            var sn = newseg.Sn;
            if (TimeDiff(sn, mRcvNxt + mRcvWnd) >= 0 || TimeDiff(sn, mRcvNxt) < 0)
            {
                newseg.Release();
                return;
            }

            var repeat = false;

            LinkedListNode<Segment> p, prev;
            for (p = mRcvBuf.Last; p != mRcvBuf.First; p = prev)
            {
                var seg = p.Value;
                prev = p.Previous;
                if (seg.Sn == sn)
                {
                    repeat = true;
                    break;
                }

                if (TimeDiff(sn, seg.Sn) > 0)
                    break;
            }

            if (repeat == false)
                if (p == null)
                    mRcvBuf.AddFirst(newseg);
                else
                    mRcvBuf.AddBefore(p, newseg);
            else
                newseg.Release();


            // move available data from rcv_buf -> rcv_queue
            var c = 0;
            foreach (var seg in mRcvBuf)
                if (seg.Sn == mRcvNxt && mRcvQueue.Count < mRcvWnd)
                {
                    mRcvQueue.AddLast(seg);
                    mRcvNxt++;
                    c++;
                }
                else
                {
                    break;
                }
            if (0 < c)
                for (var i = 0; i < c; i++)
                    mRcvBuf.RemoveFirst();
        }

        private void Parse_fastack(int sn)
        {
            if (TimeDiff(sn, mSndUna) < 0 || TimeDiff(sn, mSndNxt) >= 0)
                return;
            foreach (var seg in mSndBuf)
            {
                if (TimeDiff(sn, seg.Sn) < 0)
                    break;

                if (sn != seg.Sn)
                    seg.Fastack++;
            }
        }

        private void Parse_una(int una)
        {
            var c = 0;
            foreach (var seg in mSndBuf)
                if (TimeDiff(una, seg.Sn) > 0)
                    c++;
                else
                    break;
            if (c > 0)
                for (var i = 0; i < c; i++)
                {
                    var seg = mSndBuf.First();
                    mSndBuf.Remove(seg);
                    seg.Data.Release(seg.Data.ReferenceCount);
                }
        }

        private void Shrink_buf()
        {
            mSndUna = mSndBuf.Any() ? mSndBuf.First().Sn : mSndNxt;
        }

        private void Update_ack(int rtt)
        {
            if (mRxSrtt == 0)
            {
                mRxSrtt = rtt;
                mRxRttval = rtt / 2;
            }
            else
            {
                var delta = rtt - mRxSrtt;
                if (delta < 0)
                    delta = -delta;
                mRxRttval = (3 * mRxRttval + delta) / 4;
                mRxSrtt = (7 * mRxSrtt + rtt) / 8;
                if (mRxSrtt < 1)
                    mRxSrtt = 1;
            }
            var rto = mRxSrtt + Math.Max(mInterval, 4 * mRxRttval);
            mRxRto = Bound(mRxMinrto, rto, IKCP_RTO_MAX);
        }

        private int Wnd_unused()
        {
            if (mRcvQueue.Count < mRcvWnd)
                return mRcvWnd - mRcvQueue.Count;
            return 0;
        }
    }
}