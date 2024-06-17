using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TIDStation.General;

namespace TIDStation.Radio
{
    public static class Status
    {
        public static int State
        {
            get => state;
            set
            {
                state = value.Clamp(0,2);
            }
        }
        private static int state = 0;

        public static Brush VFOBorderBrush => state switch
        {
            0 => Brushes.DarkGray,
            1 => Brushes.LimeGreen,
            _ => Brushes.OrangeRed,
        };
            
        

    }
}
