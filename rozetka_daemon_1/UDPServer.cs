using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace rozetka_daemon_1
{
    class UDPServer
    {
        public const int PORT = 8081;

        private Socket _socket;
        private EndPoint _ep;
        private float dataInFloat = 0;
        private int numOfDevices = 5;
        private byte[] _buffer_recv;
        private ArraySegment<byte> _buffer_recv_segment;
        private Dictionary<int, Device> devices = new Dictionary<int, Device>();


        public void Initialize()
        {
            for (int i = 1; i < numOfDevices; i++)
                devices[i] = new Device(i);


            _ep = new IPEndPoint(IPAddress.Any, PORT);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            _socket.Bind(_ep);
        }

        public void StartMessageLoop()
        {
            var powermonId = 0;
            string[] words;
            Device powermon;
            _ = Task.Run(async () =>
            {
                SocketReceiveMessageFromResult res;
                while (true)
                {
                    var data = new StringBuilder();
                    _buffer_recv = new byte[256];
                    _buffer_recv_segment = new ArraySegment<byte>(_buffer_recv);

                    // Recieving data
                    res = await _socket.ReceiveMessageFromAsync(_buffer_recv_segment, SocketFlags.None, _ep);

                    try
                    {
                        data.Append(Encoding.UTF8.GetString(_buffer_recv_segment.Array));
                        words = data.ToString().Split(new char[] { ':' });
                        powermonId = Int32.Parse(words[0]);
                        dataInFloat = float.Parse(words[1], CultureInfo.InvariantCulture.NumberFormat) * 10;
                    }
                    catch { };

                    if (devices.TryGetValue(powermonId, out powermon))
                        powermon.DataProcessing(dataInFloat);
                }
            });
        
        }

        public void AddDevice(int id) 
        {
            //TODO
        }
    }
}
