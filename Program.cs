using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebSocket
{
    internal class Program
    {
        public const int PORT = 6789;
        public const int BUFFSIZE = 1024;
        public const int CHAT_ROOM_TIMEOUT = 3 * 60 * 1000; // 3 phút

        static List<Student> students = new List<Student>();
        static string dssv;
        static Dictionary<int, ChatRoom> chatRooms = new Dictionary<int, ChatRoom>();
        static Dictionary<string, Student> activeSessions = new Dictionary<string, Student>();

        static void Main(string[] args)
        {
            string[] studentData = File.ReadAllLines("students.txt");
            foreach (var line in studentData)
            {
                var parts = line.Split(',');
                if (parts.Length == 3)
                {
                    int id = int.Parse(parts[0]);
                    string name = parts[1];
                    string password = parts[2];
                    students.Add(new Student(id, name, password));
                }
            }
                // Thêm các phòng chat mẫu
                chatRooms[1] = new ChatRoom(1);
            chatRooms[2] = new ChatRoom(2);

            TcpListener server = new TcpListener(IPAddress.Any, PORT);
            server.Start();

            Console.WriteLine("Server bat dau ! " + server.LocalEndpoint);

            int i = 0;
            while (i < 1000)
            {
                Socket client = server.AcceptSocket();
                Thread thread = new Thread(new ParameterizedThreadStart(ServeClient));
                thread.Start(client);
                i++;
            }

            server.Stop();
        }

        static void ServeClient(object xclient)
        {
            Socket client = xclient as Socket;

            byte[] buffer = new byte[BUFFSIZE];
            int receivedBytes;
            StringBuilder requestBuilder = new StringBuilder();
            while ((receivedBytes = client.Receive(buffer)) > 0)
            {
                requestBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receivedBytes));
                if (requestBuilder.ToString().Contains("\r\n\r\n"))
                    break;
            }

            string str = requestBuilder.ToString();

            Console.WriteLine("\n---------------\n... nhan ket noi ! "
                + client.RemoteEndPoint
                + "\n DATA: \t"
                + str
                + "---------------------\n\n"
            );

            string[] strlist = str.Split("\n");
            if (strlist != null && strlist.Length > 1)
            {
                string[] requestLineParts = strlist[0].Split(' ');
                string requestedPath = requestLineParts[1];
                string method = requestLineParts[0];
                string response;
                string sessionId = GetSessionId(strlist);

                if (requestedPath == "/contact")
                {
                    response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n  <p>Hello, world!</p>\r\n" + "</p><p> Local: "
                    + client.LocalEndPoint
                    + "</p><p> Client - Remote: " + client.RemoteEndPoint
                    + "</p><p> HTTP request line 0: " + strlist[0]
                    + "</html>";
                }
                else if (requestedPath == "/about")
                {
                    response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n  <p>About: Nguyen Tan Phat</p>\r\n</html>";
                }
                else if (requestedPath == "/login" && method == "GET")
                {
                    response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n  <h2>Login Page</h2>\r\n"
                    + "<form action='/login' method='post'>"
                    + "<label for='username'>Username:</label><br>"
                    + "<input type='text' id='username' name='username'><br>"
                    + "<label for='password'>Password:</label><br>"
                    + "<input type='password' id='password' name='password'><br><br>"
                    + "<input type='submit' value='Submit'>"
                    + "</form>"
                    + "</body></html>";
                }
                else if (requestedPath == "/login" && method == "POST")
                {
                    string[] requestBody = str.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);
                    string[] formDataArray = requestBody.Last().Split('&');
                    var formData = new Dictionary<string, string>();
                    foreach (var item in formDataArray)
                    {
                        var keyValue = item.Split('=');
                        if (keyValue.Length == 2)
                        {
                            formData[keyValue[0]] = WebUtility.UrlDecode(keyValue[1]);
                        }
                    }

                    string username = formData.ContainsKey("username") ? formData["username"] : "";
                    string password = formData.ContainsKey("password") ? formData["password"] : "";

                    var student = students.FirstOrDefault(s => s.Name == username && s.Password == password);
                    if (student != null)
                    {
                        sessionId = Guid.NewGuid().ToString();
                        LoginManager.AddSession(sessionId, student);
                        response = "HTTP/1.1 302 Found\r\nSet-Cookie: sessionId=" + sessionId + "\r\nLocation: /chat\r\n\r\n";
                    }
                    else
                    {
                        response = "HTTP/1.1 401 Unauthorized\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n  <h2>Login Failed</h2>\r\n"
                        + "<p>Invalid username or password. Please try again.</p>"
                        + "<a href='/login'>Go back to Login Page</a>"
                        + "</body></html>";
                    }
                }
                else if (requestedPath == "/")
                {
                    StringBuilder studentInfo = new StringBuilder();
                    students.ForEach(student =>
                    {
                        studentInfo.Append("<p>Student: " + student.ToString() + "</p>");
                    });
                    response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n " + "</p><p> Local: "
                    + client.LocalEndPoint
                    + "</p><p> Client - Remote: " + client.RemoteEndPoint
                    + "</p><p> Sinh Vien " + dssv
                    + studentInfo.ToString()
                    + "</p><p><a href='/login'>Go to Login Page</a></p>"
                    + "<body></html>";
                }
                else if (requestedPath == "/chat" && method == "GET")
                {
                    StringBuilder chatRoomsInfo = new StringBuilder();
                    chatRoomsInfo.Append("<h2>Chat Rooms</h2>");
                    foreach (var room in chatRooms)
                    {
                        chatRoomsInfo.Append($"<p>Room ID: {room.Key} - {(room.Value.IsActive() ? "Online" : "Offline")}</p>");
                    }
                    response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n" + chatRoomsInfo.ToString() + "</body></html>";
                }
                else if (requestedPath.StartsWith("/chat/") && method == "GET")
                {
                    string[] pathSegments = requestedPath.Split('/');
                    if (pathSegments.Length == 3 && int.TryParse(pathSegments.Last(), out int roomId))
                    {
                        if (chatRooms.ContainsKey(roomId))
                        {
                            var chatRoom = chatRooms[roomId];
                            StringBuilder chatHistory = new StringBuilder();
                            chatHistory.Append("<h2>Chat Room</h2>");
                            chatHistory.Append("<div id='chatMessages'>");
                            foreach (var message in chatRoom.Messages)
                            {
                                chatHistory.Append($"<p>{message.Timestamp} - {message.Username}: {message.Content}</p>");
                            }
                            chatHistory.Append("</div>");
                            chatHistory.Append(
                                "<form action='/chat/" + roomId + "' method='post'>" +
                                "<label for='message'>Message:</label><br>" +
                                "<input type='text' id='message' name='content'><br><br>" +
                                "<input type='submit' value='Send'>" +
                                "</form>");

                            response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n  <meta charset=\"utf-8\">\r\n</head>\r\n<body>\r\n" + chatHistory.ToString() + "</body></html>";
                        }
                        else
                        {
                            response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nChat Room not found";
                        }
                    }
                    else
                    {
                        response = "HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nInvalid Chat Room ID";
                    }
                }
                else if (requestedPath.StartsWith("/chat/") && method == "POST")
                {
                    string[] pathSegments = requestedPath.Split('/');
                    if (pathSegments.Length == 3 && int.TryParse(pathSegments.Last(), out int roomId))
                    {
                        if (chatRooms.ContainsKey(roomId))
                        {
                            string[] requestBody = str.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);
                            string[] formDataArray = requestBody.Last().Split('&');
                            var formData = new Dictionary<string, string>();
                            foreach (var item in formDataArray)
                            {
                                var keyValue = item.Split('=');
                                if (keyValue.Length == 2)
                                {
                                    formData[keyValue[0]] = WebUtility.UrlDecode(keyValue[1]);
                                }
                            }

                            Student student = LoginManager.GetStudent(sessionId);
                            string content = formData.ContainsKey("content") ? formData["content"] : "";

                            if (student != null)
                            {
                                chatRooms[roomId].AddMessage(new ChatMessage { Username = student.Name, Content = content, Timestamp = DateTime.Now });
                                response = "HTTP/1.1 302 Found\r\nLocation: /chat/" + roomId + "\r\n\r\n";
                            }
                            else
                            {
                                response = "HTTP/1.1 401 Unauthorized\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nUnauthorized";
                            }
                        }
                        else
                        {
                            response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nChat Room not found";
                        }
                    }
                    else
                    {
                        response = "HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nInvalid Chat Room ID";
                    }
                }
                else
                {
                    response = "HTTP/1.1 404 Not Found\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n404 Not Found";
                }

                client.Send(Encoding.UTF8.GetBytes(response));
            }
            client.Close();
        }

        static string GetSessionId(string[] headers)
        {
            foreach (var header in headers)
            {
                if (header.StartsWith("Cookie:"))
                {
                    string[] cookies = header.Substring(7).Split(';');
                    foreach (var cookie in cookies)
                    {
                        string[] keyValue = cookie.Trim().Split('=');
                        if (keyValue[0] == "sessionId")
                        {
                            return keyValue[1];
                        }
                    }
                }
            }
            return null;
        }
    }
}
