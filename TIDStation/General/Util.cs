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
        public static bool SyncWait(this object obj, int timeOut = 1000)
        {
            lock (obj) return Monitor.Wait(obj, timeOut);
        }

        public static void SyncSignal(this object obj)
        {
            lock (obj) Monitor.PulseAll(obj);
        }

        public static bool Wait(this object obj, int timeOut = 1000)
        {
            return Monitor.Wait(obj, timeOut);
        }

        public static void Signal(this object obj)
        {
            Monitor.PulseAll(obj);
        }



        public static int Clamp(this int val, int min, int max)
        {
            return val < min ? min : val > max ? max : val;
        }

        public static double Clamp(this double val, double min, double max)
        {
            return val < min ? min : val > max ? max : val;
        }


        public static void Write16BE(this int shrt, byte[] array, int offset)
        {
            array[offset] = (byte)((shrt >> 8) & 0xff);
            array[offset + 1] = (byte)(shrt & 0xff);
        }

        public static void Write8(this int shrt, byte[] array, int offset)
        {
            array[offset] = (byte)(shrt & 0xff);
        }

        public static int Sign(this int d) => d > 0 ? 1 : d < 0 ? -1 : 0;
        public static string Truncate(this string str, int length) => str.Length > length ? str[..length] : str;
    }
}
