using System.Collections.Concurrent;

namespace Lamp.Network
{
    class SessionManager
    {
        private readonly ConcurrentQueue<int> m_SessionIdQueue = new ConcurrentQueue<int>();

        /// <summary>
        /// 创建一个<see cref="SessionManager"/>
        /// </summary>
        /// <param name="peerCapcity">连接点容量</param>
        /// <returns></returns>
        public static SessionManager Create(uint peerCapcity)
        {
            var sm = new SessionManager();
            
            for (int sessionId = 1; sessionId <= peerCapcity; sessionId++)
            {
                sm.m_SessionIdQueue.Enqueue(sessionId);
            }

            return sm;
        }

        /// <summary>
        /// 获得一个新的会话标识，如果获取失败则返回<see cref="NetworkOperationCode.MAX_CONN_EXCEED"/>
        /// </summary>
        /// <returns></returns>
        public int GetNewSessionId()
        {
            if (m_SessionIdQueue.IsEmpty)
            {
                return NetworkOperationCode.MAX_CONN_EXCEED;
            }

            return m_SessionIdQueue.TryDequeue(out var newPeerId) ? newPeerId : NetworkOperationCode.MAX_CONN_EXCEED;
        }

        /// <summary>
        /// 回收一个会话标识以备复用
        /// </summary>
        /// <param name="sessionId">会话标识</param>
        public void RecycleSessionId(int sessionId)
        {
            m_SessionIdQueue.Enqueue(sessionId);
        }
    }
}
