using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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

        private static readonly List<ViewModel> cycles = [];
        protected void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            lock (cycles)
            {
                if(cycles.Contains(this))
                    throw new Exception("Detected a cyclic property change, do not do this.");
                cycles.Add(this);
            }
            (_ = PropertyChanged)?.Invoke(this, args);
            lock(cycles)
                cycles.Remove(this);
        }

        protected void OnPropertyChanged()
        {
            OnPropertyChanged(valueArgs);
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

    public class BcdDouble : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (double)value; }
        public override bool IsDefault => true;

        private readonly int address, count;
        private readonly double magnitude;
        private readonly bool bigEndian;
        private readonly string format;
        private readonly static PropertyChangedEventArgs valueStringArgs = new(nameof(StringValue));
        public BcdDouble(int addr, int byteCount, int decimals, int wholeDigits, bool isBigEndian = false) 
        {
            this.address = addr;
            this.count = byteCount;
            this.magnitude = Math.Pow(10, decimals);
            this.bigEndian = isBigEndian;
            format = $"{new string('0', wholeDigits)}.{new string('0', decimals)}";
        }
        public double Value
        {
            get => (bigEndian ? Comms.GetBcdrAt(address, count) : Comms.GetBcdAt(address, count)) / magnitude;
            set
            {
                int val = (int)Math.Round(value * magnitude);
                if(bigEndian)
                    Comms.SetBcdrAt(address, val, count);
                else
                    Comms.SetBcdAt(address, val, count);
                OnPropertyChanged();
                OnPropertyChanged(valueStringArgs);
            }
        }
        public string StringValue
        {
            get => Value.ToString(format);
            set => Value = double.TryParse(value, out double d) ? d : Value;
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
                if (Value != value)
                {
                    Comms.SetBcdAt(Address, (int)Math.Round(value * 100000.0));
                    OnPropertyChanged();
                }
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

    public class BcdrModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (string)value; }

        public override bool IsDefault => true;
        private readonly int address;
        private readonly double min, max, warnlow, warnhigh;

        public string Value
        {
            get => $"{Comms.GetBcdrAt(address, 2) / 10.0:F1}";
            set
            {
                if (Value != value)
                {
                    if (double.TryParse(value, out double d))
                    {
                        d = d.Clamp(min, max);
                        if (d < warnlow || d > warnhigh)
                        {
                            MessageBox.Show(
                                "WARNING!\r\n\r\nThe entered frequency limit is outside the designed range\r\n" +
                                $"of frequencies for this band. {warnlow:F1} - {warnhigh:F1} MHz\r\n\r\n" +
                                "The radio will likely not function correctly at these frequencies and\r\n" +
                                "could result in damage to the radio and violation of operator regulations."
                            );
                        }
                        Comms.SetBcdrAt(address, (int)Math.Round(d * 10.0), 2);
                    }
                    OnPropertyChanged();
                }
            }
        }

        public BcdrModel(int addr, double min, double max, double warnlow, double warnhigh)
        {
            address = addr;
            this.min = min;
            this.max = max;
            this.warnlow = warnlow;
            this.warnhigh = warnhigh;
        }
    }

    public class BcdfModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (string)value; }

        public override bool IsDefault => true;
        private readonly int address;
        private readonly double min, max;

        public string Value
        {
            get => $"{Comms.GetBcdAt(address, 2) / 10.0:F1}";
            set
            {
                if (Value != value)
                {
                    if (double.TryParse(value, out double d))
                    {
                        d = d.Clamp(min, max);
                        Comms.SetBcdAt(address, (int)Math.Round(d * 10.0), 2);
                    }
                    OnPropertyChanged();
                }
            }
        }

        public BcdfModel(int addr, double min, double max)
        {
            address = addr;
            this.min = min; 
            this.max = max;
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
                if (Value != value)
                {
                    Comms.SetBcdAt(address, value, count);
                    OnPropertyChanged();
                }
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
        private static readonly PropertyChangedEventArgs OpcArgs = new(nameof(Opacity));

        public override bool IsDefault => true;
        private readonly int address;
        private readonly byte bit, ibit;

        public int AltAddress { get; set; } = -1;
        public int Address => AltAddress > -1 ? AltAddress : address;

        public double Opacity => Value ? 1.0 : 0.25;

        public bool Value
        {
            get => (Comms.EEPROM[Address] & bit) != 0;
            set
            {
                if (value != Value)
                {
                    byte b = Comms.EEPROM[Address];
                    if (value) b |= bit; else b &= ibit;
                    Comms.Write(Address, b);
                    OnPropertyChanged();
                    OnPropertyChanged(OpcArgs);
                }
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
        private static readonly PropertyChangedEventArgs OpcArgs = new(nameof(Opacity));

        public override bool IsDefault => true;

        public int AltAddress { get; set; } = -1;
        public int Address => AltAddress > -1 ? AltAddress : address;

        public double Opacity => Value == 0 ? 0.25 : 1.0;

        private readonly int address;
        private readonly int shift = 0;
        private readonly byte mask;

        public int Value
        {
            get => (Comms.EEPROM[Address] & mask) >> shift;
            set
            {
                if (value != Value)
                {
                    int b = Comms.EEPROM[Address] & ~mask;
                    b |= ((value << shift) & mask);
                    Comms.Write(Address, (byte)b);
                    OnPropertyChanged();
                    OnPropertyChanged(OpcArgs);
                }
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
                if (value != Value)
                {
                    Comms.Write(address, (byte)value);
                    OnPropertyChanged();
                }
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
            get
            {
                for (int i = address + length - 1; i >= address; i--)
                {
                    if (Comms.EEPROM[i] == 0xff)
                        Comms.EEPROM[i] = 0x00;
                }
                return Encoding.ASCII.GetString(Comms.EEPROM, address, length).Trim('\0');
            }
            set
            {
                if (!value.Equals(Value))
                {
                    byte[] b = new byte[length];
                    Encoding.ASCII.GetBytes(value.Length > length ? value[..length] : value).CopyTo(b, 0);
                    Comms.Write(address, b);
                    OnPropertyChanged();
                }
            }
        }
    }

    public class BoolModel : ViewModel
    {
        public override object ObjValue { get => Value; set => Value = (bool)value; }
        private static readonly PropertyChangedEventArgs OpcArgs = new(nameof(Opacity));
        private static readonly PropertyChangedEventArgs VisArgs = new(nameof(Visible));
        private static readonly PropertyChangedEventArgs ROpcArgs = new(nameof(RevOpacity));
        private static readonly PropertyChangedEventArgs RVisArgs = new(nameof(RevVisible));
        private static readonly PropertyChangedEventArgs NotArgs = new(nameof(Not));
        public override bool IsDefault => true;
        private bool val;

        private double opcTrue = 1.0, opcFalse = 0.4;

        public bool Value
        {
            get => val;
            set
            {
                val = value;
                OnPropertyChanged();
                OnPropertyChanged(OpcArgs);
                OnPropertyChanged(VisArgs);
                OnPropertyChanged(ROpcArgs);
                OnPropertyChanged(RVisArgs);
                OnPropertyChanged(NotArgs);
            }
        }

        public BoolModel(bool initValue)
        {
            Value = initValue;
        }

        public void SetOpacities(double whenTrue, double whenFalse)
        {
            opcTrue = whenTrue; 
            opcFalse = whenFalse;
            OnPropertyChanged(OpcArgs);
            OnPropertyChanged(ROpcArgs);
        }

        public bool Not => !val;
        public double Opacity => val ? opcTrue : opcFalse;
        public double RevOpacity => val ? opcFalse : opcTrue;
        public Visibility Visible => val ? Visibility.Visible : Visibility.Hidden;
        public Visibility RevVisible => val ? Visibility.Hidden : Visibility.Visible;
    }

}
