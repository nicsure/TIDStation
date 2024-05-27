using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIDStation.General;
using TIDStation.Serial;

namespace TIDStation.View
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        protected static readonly Dictionary<string, string> configStore = [];
        public static Dictionary<string, string> ConfigStore => configStore;
        public string? ConfigName { get; protected set; } = null;
        public event PropertyChangedEventHandler? PropertyChanged;
        private static readonly PropertyChangedEventArgs valueArgs = new("Value");
        public int ForceUpdate
        {
            get => 0;
            set => OnPropertyChanged();
        }
        protected void OnPropertyChanged()
        {
            (_ = PropertyChanged)?.Invoke(this, valueArgs);
            if (ConfigName != null)
            {
                if(IsDefault)
                    configStore.Remove(ConfigName);
                else
                    configStore[ConfigName] = Serialization.Serialize(ObjValue);
            }
        }
        public abstract object ObjValue { get; set; }
        public abstract bool IsDefault { get; }
        static ViewModel()
        {
            try
            {
                if (File.Exists(User.ConfigFile))
                {
                    string[] lines = File.ReadAllLines(User.ConfigFile);
                    for (int i = 0; i < lines.Length; i += 2)
                    {
                        ConfigStore[lines[i]] = lines[i + 1];
                    }
                }
            }
            catch { }
        }
        public static void Save()
        {
            List<string> lines = [];
            foreach (string key in ConfigStore.Keys)
            {
                lines.Add(key);
                lines.Add(ConfigStore[key]);
            }
            try
            {
                File.WriteAllLines(User.ConfigFile, lines);
            }
            catch { }
        }       
    }

    public class ViewModel<T> : ViewModel
    {
        public T Default { get; private set; }
        public T Value
        {
            get => val;
            set
            {
                if (!val?.Equals(value) ?? false)
                {
                    val = value;
                    OnPropertyChanged();
                }
            }
        }
        private T val;
        public override object ObjValue { get => val!; set => Value = (T)value; }
        public override bool IsDefault => val?.Equals(Default) ?? true;
        public ViewModel(T defaultValue, string? saveName = null)
        {
            Default = defaultValue;
            if (saveName != null && configStore.TryGetValue(saveName, out string? sText) && sText != null)
                val = (T)Serialization.Deserialize<T>(sText);
            else
                val = defaultValue;
            ConfigName = saveName;
        }
    }

    public class Converter<T> : ViewModel
    {
        public T Value
        {
            get => Convert(Parents);
            set
            {
                if (ConvertBack != null)
                {
                    ConvertBack(value, Parents);
                    OnPropertyChanged();
                }
            }
        }
        public ViewModel[] Parents { get; private set; }
        public override object ObjValue { get => Value!; set => Value = (T)value; }
        public override bool IsDefault => true;
        private readonly Func<ViewModel[], T> Convert;
        private readonly Action<T, ViewModel[]>? ConvertBack;
        public Converter(Func<ViewModel[], T> convert, Action<T, ViewModel[]>? convertBack, params ViewModel[] parents)
        {
            Convert = convert;
            ConvertBack = convertBack;
            Parents = parents;
            foreach(var parent in Parents)
                parent.PropertyChanged += Parent_PropertyChanged;
        }
        private void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged();
        }
    }

    public class FreqModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (double)value; }

        public override bool IsDefault => true;
        private readonly int address;

        public int AltAddress { get; set; } = -1;
        public int Address => AltAddress > -1 ? AltAddress : address;

        public double Value
        {
            get => Comms.GetBcdAt(Address) / 100000.0;
            set
            {
                Comms.SetBcdAt(Address, (int)Math.Round(value * 100000.0));
                OnPropertyChanged();
            }
        }

        public FreqModel(int addr) 
        {
            address = addr;
        }
    }

    public class ToneModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (string)value; }

        public override bool IsDefault => true;
        private readonly int address;

        public int AltAddress { get; set; } = -1;
        public int Address => AltAddress > -1 ? AltAddress : address;

        public string Value
        {
            get
            {
                int i = Comms.GetDcsAt(Address);
                if (i == -1) return "NT";
                if (i > 10000) return $"{(i - 10000) / 10.0:F1}";
                if (i < 0) return $"{-i:D3}I";
                return $"{i:D3}N";
            }
            set
            {
                string s = value.Replace(".", string.Empty);
                if (int.TryParse(s, out int i))
                    Comms.SetDcsAt(Address, i + 10000);
                else
                {
                    if (
                        (s.EndsWith('N') || s.EndsWith('I')) &&
                        int.TryParse(s[..^1], out i)
                       )
                        Comms.SetDcsAt(Address, s.EndsWith('N') ? i : -i);
                    else
                        Comms.SetDcsAt(Address, -1);
                }
                OnPropertyChanged();
            }
        }

        public ToneModel(int addr)
        {
            address = addr;
        }
    }


    public class BcdModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (int)value; }

        public override bool IsDefault => true;
        private readonly int address, count;

        public int Value
        {
            get => Comms.GetBcdAt(address, count);
            set
            {
                Comms.SetBcdAt(address, value, count);
                OnPropertyChanged();
            }
        }

        public BcdModel(int addr, int count)
        {
            address = addr;
            this.count = count;
        }
    }

    public class BitModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (bool)value; }

        public override bool IsDefault => true;
        private readonly int address;
        private readonly byte bit, ibit;

        public int AltAddress { get; set; } = -1;
        public int Address => AltAddress > -1 ? AltAddress : address;

        public bool Value
        {
            get => (Comms.EEPROM[Address] & bit) != 0;
            set
            {
                byte b = Comms.EEPROM[Address];
                if (value) b |= bit; else b &= ibit;
                Comms.Write(Address, b);
                OnPropertyChanged();
            }
        }

        public BitModel(int addr, int bit)
        {
            address = addr;
            this.bit = (byte)(1 << bit);
            ibit = (byte)~this.bit;
        }
    }

    public class BitsModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (int)value; }

        public override bool IsDefault => true;

        public int AltAddress { get; set; } = -1;
        public int Address => AltAddress > -1 ? AltAddress : address;

        private readonly int address;
        private readonly int shift = 0;
        private readonly byte mask;

        public int Value
        {
            get => (Comms.EEPROM[Address] & mask) >> shift;
            set
            {
                int b = Comms.EEPROM[Address] & ~mask;
                b |= ((value << shift) & mask);
                Comms.Write(Address, (byte)b);
                OnPropertyChanged();
            }
        }

        public BitsModel(int addr, int mask)
        {
            address = addr;
            this.mask = (byte)mask;
            while ((mask & 1) == 0) { mask >>= 1; shift++; }
       }
    }

    public class ByteModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (int)value; }

        public override bool IsDefault => true;
        private readonly int address;

        public int Value
        {
            get => Comms.EEPROM[address];
            set
            {
                Comms.Write(address, (byte)value);
                OnPropertyChanged();
            }
        }

        public ByteModel(int addr)
        {
            address = addr;
        }
    }

    public class DtmfModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (string)value; }
        public override bool IsDefault => true;

        private readonly int address;
        private readonly int length;
        private readonly int end;

        public DtmfModel(int addr, int length)
        {
            address = addr;
            this.length = length;
            end = addr + length;
        }

        public string Value
        {
            get
            {
                StringBuilder sb = new();
                for (int i = address; i < end; i++)
                {
                    byte b = Comms.EEPROM[i];
                    switch (b)
                    {
                        case <= 9: sb.Append(b); break;
                        case >= 10 and <= 13: sb.Append((char)('A' + b - 10)); break;
                        case 14: sb.Append('*'); break;
                        case 15: sb.Append('#'); break;
                        case 0xff: i = int.MaxValue - 1; break;
                    }
                }
                return sb.ToString();
            }
            set
            {
                Array.Fill<byte>(Comms.EEPROM, 0xff, address, length);
                int i = address;
                foreach(char c in value)
                {
                    if (i >= end) break;
                    switch(c)
                    {
                        case >= '0' and <= '9': Comms.EEPROM[i++] = (byte)(c - '0'); break;
                        case >= 'a' and <= 'd': Comms.EEPROM[i++] = (byte)(c - 'a' + 10); break;
                        case >= 'A' and <= 'D': Comms.EEPROM[i++] = (byte)(c - 'A' + 10); break;
                        case '*': Comms.EEPROM[i++] = 14; break;
                        case '#': Comms.EEPROM[i++] = 15; break;
                    }
                }
                Comms.PreCommit(address, length);
            }
        }
    }

    public class StringModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (string)value; }

        public override bool IsDefault => true;

        private readonly int address;
        private readonly int length;

        public StringModel(int addr, int chars)
        {
            address = addr;
            length = chars;
        }

        public string Value
        {
            get => Encoding.ASCII.GetString(Comms.EEPROM, address, length).Trim('\0');
            set
            {
                byte[] b = new byte[length];
                Encoding.ASCII.GetBytes(value.Length > length ? value[..length] : value).CopyTo(b, 0);
                Comms.Write(address, b);
                OnPropertyChanged();
            }
        }
    }

}
