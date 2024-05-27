using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using TIDStation.General;
using TIDStation.Serial;

namespace TIDStation.Radio
{
    public class Channel : INotifyPropertyChanged
    {
        private static readonly byte[] newChBytes = [0x00, 0x00, 0x40, 0x14, 0x00, 0x00, 0x40, 0x14,
                                                     0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x10, 0x00];

        public static Channel[] Mem { get; } = Enumerable.Range(1, 199).Select(i => new Channel(i)).ToArray();
        public static List<string> PttIDOptions { get; } = ["Off", "BoT", "EoT", "Both"];
        public static List<string> OnOffOptions { get; } = ["Off", "On" ];
        public static List<string> PowerOptions { get; } = ["Low", "High"];
        public static List<string> BandwidthOptions { get; } = ["Wide", "Narrow"];
        public static List<string> ScanOptions { get; } = ["Del", "Add"];
        public static List<string> ReverseOptions { get; } = ["No", "Yes"];

        private static int FreqInt(string s, int def)
        {
            if (double.TryParse(s, out double d))
            {
                int i = (int)Math.Round(d * 100000.0);
                return i.Clamp(1800000, 66000000);
            }
            return def;
        }
        private static string FreqStr(int i)
        {
            return $"{i.Clamp(1800000, 66000000) / 100000.0:F5}";
        }

        private static string OptionGet(int addr, int mask, int shift, List<string> options)
        {
            int i = (Comms.EEPROM[addr] & mask) >> shift;
            return options[i];
        }

        private static void OptionSet(int addr, int mask, int shift, List<string> options, string value)
        {
            int i = options.IndexOf(value);
            if (i < 0) i = 0;
            i <<= shift;
            int b = Comms.EEPROM[addr] & ~mask;
            Comms.Write(addr, (byte)(b | i));
        }

        public int Number => num;
        private readonly int num, num0;
        private readonly int addr;

        private bool Enabled 
        {
            get => (EnabledByte & EnabledBit) != 0;
            set
            {
                Comms.EEPROM[EnabledAddr] &= (byte)~EnabledBit;
                Comms.Write(EnabledAddr, (byte)(EnabledByte | (value ? EnabledBit : 0)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            (_ = PropertyChanged)?.Invoke(this, new(name));
            TD.Update();
        }

        private int NameAddr => 0xd38 + num * 8;
        private int ScanAddr => 0x1920 + (num0 / 8);
        private int ScanByte => Comms.EEPROM[ScanAddr];
        private int ScanBit => 1 << (num0 % 8);

        private int EnabledAddr => 0x1900 + (num0 / 8);
        private int EnabledByte => Comms.EEPROM[EnabledAddr];
        private int EnabledBit => 1 << (num0 % 8);

        public Channel(int number)
        {
            num = number;
            num0 = num - 1;
            addr = num * 0x10;
            _ = RX;
        }

        private void FullUpdate()
        {
            OnPropertyChanged(nameof(Enabled));
            OnPropertyChanged(nameof(RX));
            OnPropertyChanged(nameof(TX));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Reverse));
            OnPropertyChanged(nameof(Power));
            OnPropertyChanged(nameof(Bandwidth));
            OnPropertyChanged(nameof(BusyLock));
            OnPropertyChanged(nameof(ToneRX));
            OnPropertyChanged(nameof(ToneTX));
            OnPropertyChanged(nameof(Scan));
            OnPropertyChanged(nameof(Hop));
            OnPropertyChanged(nameof(Scramble));
            OnPropertyChanged(nameof(PttID));
        }

        private void Enable(bool init, string rx)
        {
            if (!Enabled)
            {
                Enabled = true;
                if (init)
                {
                    Comms.Write(addr, newChBytes);
                    TX = rx;
                    Scan = ScanOptions[0];
                    Name = string.Empty;
                    FullUpdate();
                }
            }
        }

        private void Disable(bool init)
        {
            if (Enabled)
            {
                if (init)
                {
                    Array.Fill<byte>(Comms.EEPROM, 0xFF, addr, 16);
                    Comms.PreCommit(addr, 16);
                    Name = string.Empty;
                    FullUpdate();
                }
                Enabled = false;
            }
        }

        public void SetProperty(string propertyName, string value)
        {
            PropertyInfo? propInfo = GetType().GetProperty(propertyName);
            if (propInfo?.PropertyType == typeof(string))
            {
                propInfo.SetValue(this, value);
            }
        }

        private int Differential
        {
            get
            {
                return (Comms.EEPROM[addr + 0xe] & 0x3) switch
                {
                    0 => 0,
                    1 => -1,
                    _ => 1,
                };
            }
            set
            {
                int r = Comms.EEPROM[addr + 0xe] & 0xfc;
                switch (value)
                {
                    case 0: break;
                    case var n when n < 0: r |= 1; break;
                    default: r |= 2; break;
                }
                Comms.Write(addr + 0xe, (byte)r);
            }
        }

        public string RX
        {
            get
            {
                int r = Comms.GetBcdAt(addr);
                if (r <= 0)
                {
                    Disable(false);
                    return string.Empty;
                }
                Enable(false, string.Empty);
                return FreqStr(r);
            }
            set
            {
                if (value.Length == 0)
                    Disable(true);
                else
                {
                    Enable(true, value);
                    int def = Comms.GetBcdAt(addr);
                    if (def < 18000000 || def > 66000000) def = 14400000;
                    Comms.SetBcdAt(addr, FreqInt(value, def));
                }
                OnPropertyChanged(nameof(RX));
            }
        }

        public string TX
        {
            get
            {
                if (!Enabled) return string.Empty;
                return FreqStr(Comms.GetBcdAt(addr + 4));
            }
            set
            {
                int rx = Comms.GetBcdAt(addr);
                int tx = FreqInt(value, Comms.GetBcdAt(addr + 4));
                Comms.SetBcdAt(addr + 4, tx);
                Differential = Math.Sign(tx - rx);
                OnPropertyChanged(nameof(TX));
            }
        }

        public string Name
        {
            get
            {
                if (!Enabled) return string.Empty;
                return Encoding.ASCII.GetString(Comms.EEPROM, NameAddr, 8).Trim('\0');
            }
            set
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(value.Truncate(8));
                Array.Clear(Comms.EEPROM, NameAddr, 8);
                Comms.Write(NameAddr, nameBytes);
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Reverse
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xe, 0x80, 7, ReverseOptions);
            }
            set
            {
                OptionSet(addr + 0xe, 0x80, 7, ReverseOptions, value);
                OnPropertyChanged(nameof(Reverse));
            }

        }

        public string Power
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xe, 0x10, 4, PowerOptions);
            }
            set
            {
                OptionSet(addr + 0xe, 0x10, 4, PowerOptions, value);
                OnPropertyChanged(nameof(Power));
            }
        }

        public string Bandwidth
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xe, 0x08, 3, BandwidthOptions);
            }
            set
            {
                OptionSet(addr + 0xe, 0x08, 3, BandwidthOptions, value);
                OnPropertyChanged(nameof(Bandwidth));
            }
        }

        public string BusyLock
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xd, 0x04, 2, OnOffOptions);
            }
            set
            {
                OptionSet(addr + 0xd, 0x04, 2, OnOffOptions, value);
                OnPropertyChanged(nameof(BusyLock));
            }
        }

        public string ToneRX
        {
            get
            {
                if (!Enabled) return string.Empty;
                int i = Comms.GetDcsAt(addr + 8);
                if (i == -1) return "None";
                if (i > 10000) return $"{(i - 10000) / 10.0:F1}";
                if (i < 0) return $"{-i:D3}I";
                return $"{i:D3}N";
            }
            set
            {
                if (double.TryParse(value, out double d)) Comms.SetBcdAt(addr + 8, (int)Math.Round(10000 + d * 10));
                else if (value.EndsWith('N') && int.TryParse(value.Replace("N", ""), out int i)) Comms.SetDcsAt(addr + 8, i);
                else if (value.EndsWith('I') && int.TryParse(value.Replace("I", ""), out i)) Comms.SetDcsAt(addr + 8, -i);
                else
                {
                    Comms.Write(addr + 8, 0xFF);
                    Comms.Write(addr + 9, 0xFF);
                }
                OnPropertyChanged(nameof(ToneRX));
            }
        }

        public string ToneTX
        {
            get
            {
                if (!Enabled) return string.Empty;
                int i = Comms.GetDcsAt(addr + 0xa);
                if (i == -1) return "None";
                if (i > 10000) return $"{(i - 10000) / 10.0:F1}";
                if (i < 0) return $"{i:D3}I";
                return $"{i:D3}N";
            }
            set
            {
                if (double.TryParse(value, out double d)) Comms.SetBcdAt(addr + 0xa, (int)Math.Round(10000 + d * 10));
                else if (value.EndsWith('N') && int.TryParse(value.Replace("N", ""), out int i)) Comms.SetDcsAt(addr + 0xa, i);
                else if (value.EndsWith('I') && int.TryParse(value.Replace("I", ""), out i)) Comms.SetDcsAt(addr + 0xa, -i);
                else
                {
                    Comms.Write(addr + 0xa, 0xFF);
                    Comms.Write(addr + 0xb, 0xFF);
                }
                OnPropertyChanged(nameof(ToneTX));
            }
        }

        public string Scan
        {
            get
            {
                if (!Enabled) return string.Empty;
                return (ScanByte & ScanBit) == 0 ? ScanOptions[0] : ScanOptions[1];
            }
            set
            {
                Comms.EEPROM[ScanAddr] &= (byte)~ScanBit;
                Comms.Write(ScanAddr, (byte)(ScanByte | (value.Equals(ScanOptions[1]) ? ScanBit : 0)));
                OnPropertyChanged(nameof(Scan));
            }
        }

        public string Hop
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xd, 0x20, 5, OnOffOptions);
            }
            set
            {
                OptionSet(addr + 0xd, 0x20, 5, OnOffOptions, value);
                OnPropertyChanged(nameof(Hop));
            }
        }

        public string Scramble
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xe, 0x40, 6, OnOffOptions);
            }
            set
            {
                OptionSet(addr + 0xe, 0x40, 6, OnOffOptions, value);
                OnPropertyChanged(nameof(Scramble));
            }
        }

        public string PttID
        {
            get
            {
                if (!Enabled) return string.Empty;
                return OptionGet(addr + 0xd, 0xC0, 6, PttIDOptions);
            }
            set
            {
                OptionSet(addr + 0xd, 0xC0, 6, PttIDOptions, value);
                OnPropertyChanged(nameof(PttID));
            }
        }

    }
}
