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
        protected void OnPropertyChanged(string name)
        {
            (_ = PropertyChanged)?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static TunerChannel[] Mem { get; } = Enumerable.Range(1, 25).Select(i => new TunerChannel(i)).ToArray();

        private int EnabledAddr => 0x1940 + (num0 / 8);
        private int EnabledByte => Comms.EEPROM[EnabledAddr];
        private int EnabledBit => 1 << (num0 % 8);

        public int Number => num;

        private readonly int num;
        private readonly int num0;
        private readonly int addr;

        private bool Enabled
        {
            get => (EnabledByte & EnabledBit) != 0;
            set
            {
                Comms.EEPROM[EnabledAddr] &= (byte)~EnabledBit;
                Comms.Write(EnabledAddr, (byte)(EnabledByte | (value ? EnabledBit : 0)));
                OnPropertyChanged(nameof(Enabled));
                TD.Update();
                OnPropertyChanged(nameof(Frequency));
                TD.Update();
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
                int r = Enabled ? Comms.GetBcdAt(addr, 2) : -1;
                return r > 0 ? $"{r / 10.0:F1}" : string.Empty;
            }
            set
            {
                if (double.TryParse(value, out double d))
                {
                    int i = (int)Math.Round(d * 10.0);
                    i = i.Clamp(760, 1080);
                    Comms.SetBcdAt(addr, i, 2);
                    Comms.EEPROM[addr + 2] = 0;
                    Comms.EEPROM[addr + 3] = 0;
                    Comms.PreCommit(addr, 4);
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
                TD.Update();
                OnPropertyChanged(nameof(Enabled));
                TD.Update();
            }
        }
    }
}
