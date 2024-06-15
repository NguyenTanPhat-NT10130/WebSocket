using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WebSocket
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }

        public Student(int id, string name, string password)
        {
            Id = id;
            Name = name;
            Password = password;
        }

        public override string ToString()
        {
            return $"ID: {Id}, Name: {Name}, Password: {Password}";
        }
    }

}
