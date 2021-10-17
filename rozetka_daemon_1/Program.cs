using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using MySql.Data.MySqlClient;

namespace rozetka_daemon_1
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new UDPServer();
            server.Initialize();
            server.StartMessageLoop();
            Console.ReadLine();
        }
    }
}
