using System.Collections.Generic;

namespace Mimic.WorldServer
{
    public class World
    {
        public bool isRunning=true;

        public Queue<WorldSession> _wsAddQueue;
        public List<WorldSession> _sessions;

        public void Update(long diff){
            UpdateSessions(diff);
        }

        public void UpdateSessions(long diff){
            while(_wsAddQueue.Count > 0)
                _AddSession(_wsAddQueue.Dequeue());
        
            foreach(var session in _sessions){
                session.Update(diff);
            }
        }

        public void AddSession(WorldSession ws){
            _wsAddQueue.Enqueue(ws);
        }

        private void _AddSession(WorldSession ws){
            _sessions.Add(ws);
            var _ = ws.InitSession();
        }


    }
}
