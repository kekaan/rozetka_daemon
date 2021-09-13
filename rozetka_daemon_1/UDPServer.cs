using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
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
        private float data_float_amper = 0, amperage_sum = 0;
        private int device_on_data_recieved = 0;
        private bool isOn;
        private DB database;
        private MySqlCommand command1, command2;
        private byte[] _buffer_recv;
        private ArraySegment<byte> _buffer_recv_segment;

        public void Initialize()
        {
            database = new DB();
            try
            {
                database.openConnection();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            _ep = new IPEndPoint(IPAddress.Any, PORT);
            command1 = new MySqlCommand("INSERT INTO `events` (`id_event`, `id_device`, `time_event`, `id_type`, `amperage`) VALUES (NULL, '1', @datetime, @type, @amper);", database.getConnection());
            command2 = new MySqlCommand("INSERT INTO `data` (`id_data`, `date_data`, `value_data`) VALUES (NULL, @date, @value);", database.getConnection());
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            _socket.Bind(_ep);
        }

        public void StartMessageLoop()
        {
            _ = Task.Run(async () =>
            {
                SocketReceiveMessageFromResult res;
                while (true)
                {
                    var data = new StringBuilder();
                    _buffer_recv = new byte[256];
                    _buffer_recv_segment = new ArraySegment<byte>(_buffer_recv);
                    
                    res = await _socket.ReceiveMessageFromAsync(_buffer_recv_segment, SocketFlags.None, _ep);
                    try
                    {
                        data.Append(Encoding.UTF8.GetString(_buffer_recv_segment.Array));
                        data_float_amper = float.Parse(data.ToString(), CultureInfo.InvariantCulture.NumberFormat) * 10;
                    }
                    catch {};
                    command2.Parameters.Add("@date", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    command2.Parameters.Add("@value", MySqlDbType.Float).Value = data_float_amper;
                    command2.ExecuteNonQuery();
                    if (isOn)
                    {
                        amperage_sum += data_float_amper;
                        device_on_data_recieved++;
                    }

                    //Включение прибора
                    if (data_float_amper >= 0.72 && isOn == false)
                    {
                        isOn = true;
                        command1.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        command1.Parameters.Add("@type", MySqlDbType.Int32).Value = 1;
                        command1.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                        command1.ExecuteNonQuery();
                    }

                    //Выключение прибора
                    else if (data_float_amper < 0.72 && isOn == true)
                    {
                        isOn = false;
                        command1.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        command1.Parameters.Add("@type", MySqlDbType.Int32).Value = 2;
                        command1.Parameters.Add("@amper", MySqlDbType.Float).Value = amperage_sum / device_on_data_recieved;
                        amperage_sum = 0;
                        device_on_data_recieved = 0;
                        command1.ExecuteNonQuery();
                    }
                    command1.Parameters.Clear();
                    command2.Parameters.Clear();
                }
            });
        }

        public async Task SendTo(EndPoint recipient, byte[] data)
        {
            var s = new ArraySegment<byte>(data);
            await _socket.SendToAsync(s, SocketFlags.None, recipient);
        }
    }
}
