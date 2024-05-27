using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TIDStation.View;

namespace TIDStation.Data
{
    public static class Conv
    {
        public static bool BoolInvFW(ViewModel[] vms)
        {
            return !(bool)vms[0].ObjValue;
        }

        public static void BoolInvBK(bool b, ViewModel[] vms)
        {
            vms[0].ObjValue = !b;
        }

        public static Brush Color2Brush(ViewModel[] vms) => new SolidColorBrush((Color)vms[0].ObjValue);
    }
}
