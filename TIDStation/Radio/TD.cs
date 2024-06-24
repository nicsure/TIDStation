using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIDStation.General;
using TIDStation.Serial;

namespace TIDStation.Radio
{
    public static class TD
    {
        private static bool suspend = false;

        public static void Suspend()
        {
            suspend = true;
        }

        public static void Resume()
        {
            suspend = false;
        }

        public static void Update()
        {
            if (!suspend)
                Tasks.Watch = Comms.Commit();
        }
    }
}
