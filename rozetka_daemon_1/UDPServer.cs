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
        private TimerCallback timeCb;
        private Timer time;
        private float data_float_amper = 0, amperage_sum = 0;
        private int device_on_data_recieved = 0;
        private bool isOn, isPowermonOn = true;
        private DB database;
        private MySqlCommand command_insert_events_OnOrOff, command_insert_data, command_insert_events_isPowerMonOn;
        private byte[] _buffer_recv;
        private ArraySegment<byte> _buffer_recv_segment;
        

        public void Initialize()
        {
            database = new DB();
            timeCb = new TimerCallback(Device_State);
            time = new Timer(timeCb, null, 15000, 15000);


            try
            {
                database.openConnection();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            _ep = new IPEndPoint(IPAddress.Any, PORT);
            command_insert_events_OnOrOff = new MySqlCommand("INSERT INTO `events` (`id_event`, `id_device`, `time_event`, `id_type`, `amperage`) VALUES (NULL, '1', @datetime, @type, @amper);", database.getConnection());
            command_insert_data = new MySqlCommand("INSERT INTO `data` (`id_data`, `date_data`, `value_data`) VALUES (NULL, @date, @value);", database.getConnection());
            command_insert_events_isPowerMonOn = new MySqlCommand("INSERT INTO `events` (`id_event`, `id_device`, `time_event`, `id_type`, `amperage`) VALUES (NULL, '1', @datetime, @type, @amper);", database.getConnection());
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            _socket.Bind(_ep);
        }

        
        /// <summary>
        /// Recieving data from PowerMon
        /// </summary>
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

                    
                    // Recieving data
                    res = await _socket.ReceiveMessageFromAsync(_buffer_recv_segment, SocketFlags.None, _ep);


                    // Checking if PowerMon turned on or it already is. Timer reset
                    if (isPowermonOn == false)
                    {
                        isPowermonOn = true;
                        command_insert_events_isPowerMonOn.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        command_insert_events_isPowerMonOn.Parameters.Add("@type", MySqlDbType.Int32).Value = 3;
                        command_insert_events_isPowerMonOn.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                        command_insert_events_isPowerMonOn.ExecuteNonQuery();
                        command_insert_events_isPowerMonOn.Parameters.Clear();
                        Console.WriteLine("PowerMon is On");
                    }
                    time.Change(15000, 15000);


                    // Converting recieved data
                    try
                    {
                        data.Append(Encoding.UTF8.GetString(_buffer_recv_segment.Array));
                        data_float_amper = float.Parse(data.ToString(), CultureInfo.InvariantCulture.NumberFormat) * 10;
                    }
                    catch {};


                    // Data for calculating the average amperage
                    if (isOn)
                    {
                        amperage_sum += data_float_amper;
                        device_on_data_recieved++;
                    }


                    // Adding parameters and making SQL query for sending soft data
                    command_insert_data.Parameters.Add("@date", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    command_insert_data.Parameters.Add("@value", MySqlDbType.Float).Value = data_float_amper;
                    command_insert_data.ExecuteNonQuery();


                    // Device is turned on. Adding parameters and making SQL query for sending info about it
                    if (data_float_amper >= 0.72 && isOn == false)
                    {
                        isOn = true;
                        command_insert_events_OnOrOff.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        command_insert_events_OnOrOff.Parameters.Add("@type", MySqlDbType.Int32).Value = 1;
                        command_insert_events_OnOrOff.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                        command_insert_events_OnOrOff.ExecuteNonQuery();
                        Console.WriteLine("Device is On");
                    }


                    // Device is turned off. Adding parameters and making SQL query for sending info about it + average amperage
                    else if (data_float_amper < 0.72 && isOn == true)
                    {
                        isOn = false;
                        command_insert_events_OnOrOff.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        command_insert_events_OnOrOff.Parameters.Add("@type", MySqlDbType.Int32).Value = 2;
                        command_insert_events_OnOrOff.Parameters.Add("@amper", MySqlDbType.Float).Value = amperage_sum / device_on_data_recieved;
                        Console.WriteLine("Device is Off: " + (amperage_sum / device_on_data_recieved).ToString());
                        amperage_sum = 0;
                        device_on_data_recieved = 0;
                        command_insert_events_OnOrOff.ExecuteNonQuery();
                    }

                    // Clearing parameters for SQL queries
                    command_insert_events_OnOrOff.Parameters.Clear();
                    command_insert_data.Parameters.Clear();
                }
            });
        }

        /// <summary>
        /// Fires if data does not arrive for more than 15 seconds
        /// </summary>
        /// <param name="obj"></param>
        public void Device_State(object obj)
        {
            if (isPowermonOn == true)
            {
                isPowermonOn = false;
                command_insert_events_isPowerMonOn.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                command_insert_events_isPowerMonOn.Parameters.Add("@type", MySqlDbType.Int32).Value = 4;
                if (isOn)
                {
                    command_insert_events_isPowerMonOn.Parameters.Add("@amper", MySqlDbType.Float).Value = amperage_sum / device_on_data_recieved;
                    Console.WriteLine("PowerMon is Off: " + (amperage_sum / device_on_data_recieved).ToString());
                    amperage_sum = 0;
                    device_on_data_recieved = 0;
                    isOn = false;
                }
                else
                {
                    command_insert_events_isPowerMonOn.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                    Console.WriteLine("PowerMon is Off: " + (amperage_sum / device_on_data_recieved).ToString());
                }
                    command_insert_events_isPowerMonOn.ExecuteNonQuery();
                command_insert_events_isPowerMonOn.Parameters.Clear();
            }
        }
    }
}
