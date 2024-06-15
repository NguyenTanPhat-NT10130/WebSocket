using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocket
{
    public static class LoginManager
    {
        private static Dictionary<string, Student> sessions = new Dictionary<string, Student>();

        public static void AddSession(string sessionId, Student student)
        {
            sessions[sessionId] = student;
        }

        public static Student GetStudent(string sessionId)
        {
            if (sessions.ContainsKey(sessionId))
                return sessions[sessionId];
            return null;
        }
    }
}
