using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace Server_Hue
{
    internal class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager instance { get { return _session; } }
        List<GameSession> _sessions = new List<GameSession>();
        object _lock = new object();

        public GameSession Generate(IPEndPoint RemoteEndPoint)
        {
            lock (_lock)
            {
                GameSession session = new GameSession();
                _sessions.Add(session);
                return session;
            }
        }

        public GameSession Find(int id)
        {
            lock (_lock)
            {
                GameSession session = null;
                // _sessions.TryGetValue(id, out session);
                Console.WriteLine($"Session egist{id}");
                return session;
            }
        }

        public void Remove(GameSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
            }
        }
    }
}
