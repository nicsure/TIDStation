using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIDStation.Serial
{
    public class ComPort
    {
        public bool Active { get; private set; } = false;

        private readonly SerialPort port = null!;
        private readonly Task loopTask = null!;
        private readonly Action<byte[]> callBack;

        public ComPort(int number, int baud, Parity parity, int bits, StopBits stopbits, Action<byte[]> callBack)
        {
            this.callBack = callBack;
            try
            {
                port = new($"COM{number}", baud, parity, bits, stopbits);
                port.Open();
                Active = true;
                loopTask = Task.Run(Loop);
                return;
            }
            catch { }
            Close();
        }

        public void Close()
        {
            Active = false;
            try { port.ReadTimeout = 100; } catch { }
            try { port.WriteTimeout = 100; } catch { }
            try { port.Close(); } catch { }
            try { port.Dispose(); } catch { }
            using (loopTask)
                loopTask?.Wait();
        }

        public void Send(byte[] data) => Send(data, 0, data.Length);
        public void Send(byte[] data, int len) => Send(data, 0, len);
        public void Send(byte[] data, int offset, int len)
        {
            try
            {
                port.Write(data, offset, len);
            }
            catch { }
        }

        public void Send(byte byt)
        {
            Send([byt], 0, 1);
        }

        private void Loop()
        {
            while (Active)
            {
                byte[] bytes = new byte[32768];
                int br;
                try { br = port.Read(bytes, 0, 32768); } catch { br = -1; }
                if (br <= 0)
                    break;
                callBack(bytes[..br]);
            }
        }
    }
}
