using MySql.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rozetka_daemon_1
{
    class DB
    {
        private readonly string connectionInfo = "Database=;Data Source=;User Id=;Password=;";

        public void InsertIntoEvents_DeviceIsOnOrOff(int id, bool isOn, float value)
        {
            using (var connection = new MySqlConnection(connectionInfo))
            {
                connection.Open();
                var command = new MySqlCommand("INSERT INTO `events` (`id_event`, `id_device`, `time_event`, `id_type`, `amperage`) VALUES (NULL, @device, @datetime, @type, @amper);", connection);
                command.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                command.Parameters.Add("@device", MySqlDbType.Int32).Value = id;
                if (isOn)
                {
                    command.Parameters.Add("@type", MySqlDbType.Int32).Value = EventTypes.deviceIsOn;
                    command.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                }
                else
                {
                    command.Parameters.Add("@type", MySqlDbType.Int32).Value = EventTypes.deviceIsOff;
                    command.Parameters.Add("@amper", MySqlDbType.Float).Value = value;
                }
                command.ExecuteNonQuery();
                connection.Close();
            }

        }

        public void InsertIntoData(int id, float value)
        {
            using (var connection = new MySqlConnection(connectionInfo))
            {
                connection.Open();
                var command = new MySqlCommand("INSERT INTO `data` (`id_data`, `id_device`, `date_data`, `value_data`) VALUES (NULL, @device, @date, @value);", connection);
                command.Parameters.Add("@date", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                command.Parameters.Add("@device", MySqlDbType.Int32).Value = id;
                command.Parameters.Add("@value", MySqlDbType.Float).Value = value;
                command.ExecuteNonQuery();
                connection.Close();
            }
        }


        public void InsertIntoEvents_PowermonIsOnOrOff(int id, bool isPowermonOn, bool isOn, float value)
        {
            using (var connection = new MySqlConnection(connectionInfo))
            {
                connection.Open();
                var command = new MySqlCommand("INSERT INTO `events` (`id_event`, `id_device`, `time_event`, `id_type`, `amperage`) VALUES (NULL, @device, @datetime, @type, @amper);", connection);
                command.Parameters.Add("@device", MySqlDbType.Int32).Value = id;
                command.Parameters.Add("@datetime", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (isPowermonOn)
                {
                    command.Parameters.Add("@type", MySqlDbType.Int32).Value = EventTypes.powerMonIsOn;
                    command.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                }
                else
                {
                    command.Parameters.Add("@type", MySqlDbType.Int32).Value = EventTypes.powerMonIfOff;
                    if (isOn)
                        command.Parameters.Add("@amper", MySqlDbType.Float).Value = value;
                    else
                        command.Parameters.Add("@amper", MySqlDbType.Float).Value = null;
                }
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}
