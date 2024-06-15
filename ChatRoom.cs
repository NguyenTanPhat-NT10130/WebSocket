using System;
using System.Collections.Generic;

namespace WebSocket
{
    public class ChatRoom
    {
        public int Id { get; }
        public List<ChatMessage> Messages { get; }
        public DateTime LastActive { get; private set; }

        public ChatRoom(int id)
        {
            Id = id;
            Messages = new List<ChatMessage>();
            LastActive = DateTime.Now;
        }

        public void AddMessage(ChatMessage message)
        {
            Messages.Add(message);
            LastActive = DateTime.Now;
        }

        public bool IsActive()
        {
            return (DateTime.Now - LastActive).TotalMilliseconds <= Program.CHAT_ROOM_TIMEOUT;
        }
    }
}
