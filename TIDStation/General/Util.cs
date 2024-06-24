using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIDStation.General
{
    public static class Util
    {

        public static void SyncSignal(this object obj)
        {
            lock (obj) Monitor.PulseAll(obj);
        }

        public static bool Wait(this object obj, int timeOut = 1000)
        {
            return Monitor.Wait(obj, timeOut);
        }

        public static int Clamp(this int val, int min, int max)
        {
            return val < min ? min : val > max ? max : val;
        }

        public static double Clamp(this double val, double min, double max)
        {
            return val < min ? min : val > max ? max : val;
        }


        public static void Write16BE(this int ushrt, byte[] array, int offset)
        {
            // don't handle IOoB, leave default exception to occur
            array[offset] = (byte)((ushrt >> 8) & 0xff); // move high byte into offset
            array[offset + 1] = (byte)(ushrt & 0xff); // move low byte after offset
        }

        public static int Sign(this int d) => d > 0 ? 1 : d < 0 ? -1 : 0;
        public static string Truncate(this string str, int length) => str.Length > length ? str[..length] : str;
    }
}
