#region 文件描述

// 开发者：陈柏宇
// 解决方案：Lamp
// 工程：Lamp.Agent
// 文件名：SessionManager.cs
// 创建日期：2017-08-28

#endregion

using System.Collections.Concurrent;
using System.Net;
using Lamp.Agent.Server;

namespace Lamp.Agent
{
    internal class SessionManager
    {
        private readonly ConcurrentQueue<int> mSessionIdQueue = new ConcurrentQueue<int>();

        private readonly ConcurrentDictionary<EndPoint, Session> mSessions =
            new ConcurrentDictionary<EndPoint, Session>();

        private SessionManager()
        {
            for (var sessionId = 0; sessionId <= 100; sessionId++)
                mSessionIdQueue.Enqueue(sessionId);
        }

        public static SessionManager Instance { get; } = new SessionManager();

        public bool AddSession(EndPoint endpoint, Session session)
        {
            return mSessions.TryAdd(endpoint, session);
        }

        public Session FindSession(EndPoint endpoint)
        {
            if (mSessions.TryGetValue(endpoint, out var value))
                return value;

            return null;
        }

        /// <summary>
        ///     获得一个新的会话标识，如果获取失败则返回<see cref="NetworkOperationCode.MAX_CONN_EXCEED" />
        /// </summary>
        /// <returns></returns>
        public int GetNewSessionId()
        {
            if (mSessionIdQueue.IsEmpty)
                return NetworkOperationCode.MAX_CONN_EXCEED;

            return mSessionIdQueue.TryDequeue(out var newPeerId)
                ? newPeerId
                : NetworkOperationCode.MAX_CONN_EXCEED;
        }

        /// <summary>
        ///     回收一个会话标识以备复用
        /// </summary>
        /// <param name="sessionId">会话标识</param>
        public void RecycleSessionId(int sessionId)
        {
            mSessionIdQueue.Enqueue(sessionId);
        }
    }
}