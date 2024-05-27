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
        public static void Update()
        {
            Tasks.Watch = Comms.Commit();
        }
    }
}
