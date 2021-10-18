using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace rozetka_daemon_1
{
    class Device
    {
        private const int DataWaitingInSeconds = 15000;
        //private static int id, quantity = 0;
        private float amperageSum = 0, refValue = 0.72f;
        private int recievedDataWhileOn = 0;
        private bool isOn, isPowermonOn = true;
        private readonly TimerCallback timeCallback;
        private readonly Timer time;
        public int Id { get; private set; }
        static public int Quantity { get; private set; }

        public Device(int id)
        {
            Id = id;
            Quantity++;
            timeCallback = new TimerCallback(Device_State);
            time = new Timer(timeCallback, null, DataWaitingInSeconds, DataWaitingInSeconds);
        }

        public void DataProcessing(float value)
        {
            var db = new DB();
            if (!isPowermonOn)
            {
                isPowermonOn = true;
                db.InsertIntoEvents_PowermonIsOnOrOff(Id, isPowermonOn, isOn, 0);
            }

            time.Change(DataWaitingInSeconds, DataWaitingInSeconds);
            
            if (isOn)
            {
                amperageSum += value;
                recievedDataWhileOn++;
            }

            db.InsertIntoData(Id, value);

            if (value >= refValue && !isOn)
                DeviceTurninOn();

            else if (value < refValue && isOn)
                DeviceTurningOff();
        }
        public void Device_State(object obj)
        {
            if (isPowermonOn)
            {
                isPowermonOn = false;
                var db = new DB();
                db.InsertIntoEvents_PowermonIsOnOrOff(Id, isPowermonOn, isOn, GetAverageAmperage());
                amperageSum = 0;
                recievedDataWhileOn = 0;
                isOn = false;
            }
        }

        public void DeviceTurninOn()
        {
            isOn = true;
            var db = new DB();
            db.InsertIntoEvents_DeviceIsOnOrOff(Id, isOn, 0);
        }

        public void DeviceTurningOff()
        {
            isOn = false;
            var db = new DB();
            db.InsertIntoEvents_DeviceIsOnOrOff(Id, isOn, GetAverageAmperage());
            amperageSum = 0;
            recievedDataWhileOn = 0;
        }

        float GetAverageAmperage()
        {
            return amperageSum / recievedDataWhileOn;
        }
    }
}
