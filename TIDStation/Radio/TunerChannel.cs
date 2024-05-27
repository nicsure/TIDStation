using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIDStation.General;
using TIDStation.Serial;

namespace TIDStation.Radio
{
    public class TunerChannel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => (_ = PropertyChanged)?.Invoke(this, new PropertyChangedEventArgs(name));

        public static TunerChannel[] Mem { get; } = Enumerable.Range(1, 25).Select(i => new TunerChannel(i)).ToArray();

        private int EnabledAddr => 0x1940 + (num0 / 8);
        private int EnabledByte => Comms.EEPROM[EnabledAddr];
        private int EnabledBit => 1 << (num0 % 8);

        public int Number => num;
        private int num, num0;
        private readonly int addr;

        private bool Enabled
        {
            get => (EnabledByte & EnabledBit) != 0;
            set
            {
                Comms.EEPROM[EnabledAddr] &= (byte)~EnabledBit;
                Comms.Write(EnabledAddr, (byte)(EnabledByte | (value ? EnabledBit : 0)));
                OnPropertyChanged(nameof(Enabled));
                OnPropertyChanged(nameof(Frequency));
            }
        }

        public TunerChannel(int number)
        {
            num = number;
            num0 = num - 1;
            addr = 0xcd0 + num0 * 4;
        }

        public string Frequency
        {
            get 
            {
                int r = Enabled ? Comms.GetBcdAt(addr) : -1;
                return r > 0 ? $"{r / 100000.0:F5}" : string.Empty;
            }
            set
            {
                if (double.TryParse(value, out double d))
                {
                    int i = (int)Math.Round(d * 100000.0);
                    i = i.Clamp(8800000, 10800000);
                    Comms.SetBcdAt(addr, i);
                    Enabled = true;
                }
                else
                {
                    Comms.EEPROM[addr] = 0xff;
                    Comms.EEPROM[addr + 1] = 0xff;
                    Comms.EEPROM[addr + 2] = 0xff;
                    Comms.EEPROM[addr + 3] = 0xff;
                    Enabled = false;
                }
                OnPropertyChanged(nameof(Frequency));
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }
}
