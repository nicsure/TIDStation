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

        private static readonly double[] gmrsFreq =
        [
            462.5625,
            462.5875,
            462.6125,
            462.6375,
            462.6625,
            462.6875,
            462.7125,
            467.5625,
            467.5875,
            467.6125,
            467.6375,
            467.6625,
            467.6875,
            467.7125,
            462.5500,
            462.5750,
            462.6000,
            462.6250,
            462.6500,
            462.6750,
            462.7000,
            462.7250,
            467.5500,
            467.5750,
            467.6000,
            467.6250,
            467.6500,
            467.6750,
            467.7000,
            467.7250
        ];

        private static readonly double[] noaaFreq =
        [
            162.550,
            162.400,
            162.475,
            162.425,
            162.450,
            162.500,
            162.525,
            161.650,
            161.775,
            161.750,
            162.000,
            163.275
        ];

        public void SetProperty(string propertyName, string value)
        {
            double f;
            switch(propertyName)
            {
                case "Action":
                    switch (value)
                    {
                        case "Clear":
                            Disable(true);
                            break;
                    }
                    break;
                case "Presets":
                    switch (value)
                    {
                        case "NOAA":
                            for (int i = num0, j = 1; i < num0 + 12 && i < 200; i++, j++)
                            {
                                Mem[i].Disable(true);
                                Mem[i].RX = $"{noaaFreq[j - 1]:F5}";
                                if (j < 10)
                                    Mem[i].Name = $"NOAA WX{j:D1}";
                                else
                                    Mem[i].Name = $"NOAA #{j:D2}";
                                Mem[i].Power = "Low";
                            }
                            break;
                        case "GMRS":
                            for (int i = num0, j = 1; i < num0 + 30 && i < 200; i++, j++)
                            {
                                Mem[i].Disable(true);
                                Mem[i].RX = $"{gmrsFreq[j-1]:F5}";
                                if(j<23) 
                                    Mem[i].Name = $"GMRS {j:D2}";
                                else
                                    Mem[i].Name = $"GMRP {j-8:D2}";
                            }
                            break;
                        case "CHND":
                            f = 425.225;
                            for (int i = num0, j = 1; i < num0 + 30 && i < 200; i++, j++)
                            {
                                Mem[i].Disable(true);
                                Mem[i].RX = $"{f:F5}";
                                double add = j switch
                                {
                                    24 => 1.1725,
                                    5 => 1,
                                    29 => 0.4725,
                                    25 => -305.7725,
                                    17 => 0.3,
                                    9 => 0.3,
                                    _ => 1.1,
                                };
                                f += add;
                            }
                            break;
                        case "CB CEPT":
                            f = 26.965;
                            for (int i = num0, j = 1; i < num0 + 40 && i < 200; i++, j++)
                            {
                                Mem[i].Disable(true);
                                Mem[i].RX = $"{f:F5}";
                                f += 0.01;
                                switch(j)
                                {
                                    case 23:
                                        f -= 0.03;
                                        break;
                                    case 22:
                                        f += 0.02;
                                        break;
                                    case 25:
                                    case 19:
                                    case 15:
                                    case 11:
                                    case 7:
                                    case 3:
                                        f += 0.01;
                                        break;
                                }
                                Mem[i].Name = $"CB {j:D2}";
                                Mem[i].Power = "Low";
                            }
                            break;
                        case "CB UK":
                            f = 27.60125;
                            for (int i = num0, j = 1; i < num0 + 40 && i < 200; i++, j++)
                            {
                                Mem[i].Disable(true);
                                Mem[i].RX = $"{f:F5}";
                                f += 0.01;
                                Mem[i].Name = $"CBUK {j:D2}";
                                Mem[i].Power = "Low";
                            }
                            break;                           
                        case "PMR446":
                            f = 446.00625;
                            for (int i = num0, j = 1; i < num0 + 16 && i < 200; i++, j++)
                            {
                                Mem[i].Disable(true);
                                Mem[i].RX = $"{f:F5}";
                                f += 0.0125;
                                Mem[i].Name = $"PMR {j:D2}";
                                Mem[i].Power = "Low";
                                switch(j)
                                {
                                    case 1: Mem[i].ToneTX = "94.8"; break;
                                    case 2: Mem[i].ToneTX = "88.5"; break;
                                    case 3: Mem[i].ToneTX = "103.5"; break;
                                    case 4: Mem[i].ToneTX = "79.7"; break;
                                    case 5: Mem[i].ToneTX = "118.8"; break;
                                    case 6: Mem[i].ToneTX = "123.0"; break;
                                    case 7: Mem[i].ToneTX = "127.3"; break;
                                    case 8: Mem[i].ToneTX = "85.4"; break;
                                }
                            }
                            break;
                    }
                    break;
                default:
                    PropertyInfo? propInfo = GetType().GetProperty(propertyName);
                    if (propInfo?.PropertyType == typeof(string))
                    {
                        propInfo.SetValue(this, value);
                    }
                    break;
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
                for (int i = NameAddr + 7; i >= NameAddr; i--)
                    if (Comms.EEPROM[i] == 0xff)
                        Comms.EEPROM[i] = 0x00;
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
