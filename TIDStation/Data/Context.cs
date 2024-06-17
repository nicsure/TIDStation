using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TIDStation.Firmware;
using TIDStation.Radio;
using TIDStation.Serial;
using TIDStation.UI;
using TIDStation.View;

namespace TIDStation.Data
{
    public class VariableFontSize
    {
        private double baseSize = 4.5;
        public double this[double m]
        {
            get => baseSize * m;
            set => baseSize = value;
        }
    }

    public class Context : INotifyPropertyChanged
    {
        public static readonly Brush darkBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x55, 0x55, 0x55));
        public static int Activator
        {
            get => 0;
            set
            {
                _ = vfoRxB;
                _ = vfoTxB;
            }
        }
        public static Context Instance 
        { 
            get 
            {
                _ = Patches.List;
                return instance;
            }
        }
        private static readonly Context instance = new();
        private bool suspend = false;
        private bool Ready => UiReady.Value && !suspend;
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string name) => (_ = PropertyChanged)?.Invoke(this, new PropertyChangedEventArgs(name));

        public ViewModel<VariableFontSize> FontSize { get; } = new(new());
        public ViewModel<string> ComPort { get; } = new("Offline", nameof(ComPort));
        public ViewModel<bool> UiReady { get; } = new(true);
        public ViewModel<Visibility> ChannelModeVis { get; } = new(Visibility.Hidden);
        public ViewModel<Visibility> PowerModeVis { get; } = new(Visibility.Hidden);
        public ViewModel<Visibility> FlashModeVis { get; } = new(Visibility.Hidden);
        public ViewModel<Visibility> TunerModeVis { get; } = new(Visibility.Hidden);
        public ViewModel<string> ModulationOverride { get; } = new("〜");
        public ViewModel<double> Rssi { get; } = new(0.0);
        public BoolModel AnalyserMode { get; } = new(false);
        public BoolModel AnalyserRun { get; } = new(false);
        public BoolModel ShiftMode { get; } = new(false);
        public ViewModel<double> AnalyserSteps { get; } = new(20.0);
        public ViewModel<string> AnalyserFLabel { get; } = new(string.Empty);
        public ViewModel<string> AnalyserHLabel { get; } = new(string.Empty);

        public ViewModel<Channel[]> TestStuff { get; } = new(Channel.Mem);
        public ViewModel<TunerChannel[]> TunerStuff { get; } = new(TunerChannel.Mem);

        public ViewModel<double> RadioOpc { get; } = new(1.0);
        public ViewModel<double> ChannelOpc { get; } = new(0.5);
        public ViewModel<double> PowerOpc { get; } = new(0.5);
        public ViewModel<double> TunerOpc { get; } = new(0.5);
        public ViewModel<double> FlashOpc { get; } = new(0.5);
        public ViewModel<ObservableCollection<PowerLevel>> Powers { get; } = new([
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel(),
                new PowerLevel()
            ]);
        public List<string> FreqAdj { get; } = Enumerable.Range(-125, 256).Select(x => x == 0 ? "0 Hz" : x > 0 ? $"+{x}00 Hz" : $"{x}00 Hz").ToList();
        public ViewModel<double> FlashProgress { get; } = new(0.0);
        public ViewModel<string> FlashFile { get; } = new(string.Empty);
        public ViewModel<string> FlashError { get; } = new(string.Empty);
        public ViewModel<string> FlashComPort { get; } = new(string.Empty);
        public ViewModel<int> State { get; } = new(0);
        public Brush StateBrush => State.Value switch
        {
            0 => Brushes.LightGray,
            1 => Brushes.LimeGreen,
            _ => Brushes.OrangeRed,
        };


    public bool AllowEditA
        {
            get => allowEditA;
            private set
            {
                allowEditA = value;
                OnPropertyChanged(nameof(AllowEditA));
                OnPropertyChanged(nameof(DenyEditA));
            }
        }
        public Visibility DenyEditA => allowEditA ? Visibility.Hidden : Visibility.Visible;
        private bool allowEditA = true;
        public FreqModel VfoRxA { get; private set; } = (vfoRxA = new(0x1950));
        public FreqModel VfoTxA { get; private set; } = (vfoTxA = new(0x1954));
        private static FreqModel? vfoRxA;
        private static FreqModel? vfoTxA;
        public ViewModel<bool> SplitTxA { get; } = new(false);
        public ViewModel<string> SplitDirA { get; } = new(string.Empty); // ▲▼
        public BitModel BandwidthA { get; } = new(0x195e, 3);
        public ViewModel<string> BwLabelA { get; } = new("N");
        public BitsModel PowerA { get; } = new(0x195e, 0x18);
        public ViewModel<string> PwrLabelA { get; } = new("L");
        public BitsModel FreqStepA { get; } = new(0x0ca8, 0x70);
        public ViewModel<double> FreqStepOverrideA { get; } = new(0.0);
        public ViewModel<string> FrqStpLabelA { get; } = new("12.5k");
        public ToneModel ToneTxA { get; } = new(0x195a);
        public ToneModel ToneRxA { get; } = new(0x1958);
        public BitsModel VfoChA { get; } = new(0x0ca2, 3);
        public ViewModel<string> VfoChALabel { get; } = new("VFO-A");
        public ByteModel VfoChNumA { get; } = new(0x0ca4);
        public ViewModel<int> VfoChNumEnterA { get; } = new(0);
        public ViewModel<string> VfoChNumALabel { get; } = new("---");
        public ViewModel<bool> SelectedVfoA { get; } = new(true);
        public ViewModel<double> VfoOpacityA { get; } = new(1.0);
        public ViewModel<Brush> VfoBorderA { get; } = new(new SolidColorBrush(Color.FromArgb(0xff, 0xee, 0xee, 0xee)));
        public ViewModel<string> VfoMarkerA { get; } = new("•");
        public BitModel BusyLockA { get; } = new(0x195d, 2);
        //public ViewModel<string> BusyLockLabelA { get; } = new("B");
        public BitsModel DiffA { get; } = new(0x195e, 0x3);
        public BitModel ScrambleA { get; } = new(0x195e, 6);
        //public ViewModel<string> ScrambleALabel { get; } = new("S");
        public BitModel ReverseA { get; } = new(0x195e, 7);
        //public ViewModel<string> ReverseALabel { get; } = new("R");
        public BitsModel PttIdA { get; } = new(0x195d, 0xc0);
        public ViewModel<string> PttIdALab { get; } = new("PID");


        public bool AllowEditB
        {
            get => allowEditB;
            private set
            {
                allowEditB = value;
                OnPropertyChanged(nameof(AllowEditB));
                OnPropertyChanged(nameof(DenyEditB));
            }
        }
        public Visibility DenyEditB => allowEditB ? Visibility.Hidden : Visibility.Visible;
        private bool allowEditB = true;
        public FreqModel VfoRxB { get; private set; } = (vfoRxB = new(0x1960));
        public FreqModel VfoTxB { get; private set; } = (vfoTxB = new(0x1964));
        private static FreqModel? vfoRxB;
        private static FreqModel? vfoTxB;
        public ViewModel<bool> SplitTxB { get; } = new(false);
        public ViewModel<string> SplitDirB { get; } = new(string.Empty); // ▲▼
        public BitModel BandwidthB { get; } = new(0x196e, 3);
        public ViewModel<string> BwLabelB { get; } = new("N");
        public BitsModel PowerB { get; } = new(0x196e, 0x18);
        public ViewModel<string> PwrLabelB { get; } = new("L");
        public BitsModel FreqStepB { get; } = new(0x0ca8, 0x7);
        public ViewModel<double> FreqStepOverrideB { get; } = new(0.0);
        public ViewModel<string> FrqStpLabelB { get; } = new("12.5k");
        public ToneModel ToneTxB { get; } = new(0x196a);
        public ToneModel ToneRxB { get; } = new(0x1968);
        public BitsModel VfoChB { get; } = new(0x0ca3, 3);
        public ViewModel<string> VfoChBLabel { get; } = new("VFO-B");
        public ByteModel VfoChNumB { get; } = new(0x0ca5);
        public ViewModel<int> VfoChNumEnterB { get; } = new(0);
        public ViewModel<string> VfoChNumBLabel { get; } = new("---");
        public ViewModel<bool> SelectedVfoB { get; } = new(false);
        public ViewModel<double> VfoOpacityB { get; } = new(0.6);
        public ViewModel<Brush> VfoBorderB { get; } = new(new SolidColorBrush(Color.FromArgb(0xff, 0x55, 0x55, 0x55)));
        public ViewModel<string> VfoMarkerB { get; } = new(string.Empty);
        public BitModel BusyLockB { get; } = new(0x196d, 2);
        //public ViewModel<string> BusyLockLabelB { get; } = new("B");
        public BitsModel DiffB { get; } = new(0x195e, 0x3);
        public BitModel ScrambleB { get; } = new(0x196e, 6);
        //public ViewModel<string> ScrambleBLabel { get; } = new("S");
        public BitModel ReverseB { get; } = new(0x196e, 7);
        //public ViewModel<string> ReverseBLabel { get; } = new("R");
        public BitsModel PttIdB { get; } = new(0x196d, 0xc0);
        public ViewModel<string> PttIdBLab { get; } = new("PID");











        public BitModel DualWatch { get; } = new(0x0ca3, 2);
        public ViewModel<string> DwLabel { get; } = new("🗘");
        public BitsModel Roger { get; } = new(0x0cab, 0xe0);
        public ViewModel<string> RgrLabel { get; } = new("𝄽");
        public BitModel SelectedVfo { get; } = new(0x1f00, 0);
        public BitsModel Squelch { get; } = new(0x0ca9, 0xf);
        public BitsModel Backlight { get; } = new(0x0cad, 0x7);
        public BitsModel BattSave { get; } = new(0x0cac, 0x7);
        public BitModel RadioLang { get; } = new(0x0cab, 1);
        public BitModel VoicePrompt { get; } = new(0x0ca1, 0);
        public BitModel AutoLock { get; } = new(0x0ca1, 4);
        public BitsModel Timeout { get; } = new(0x0caa, 0x7);
        public BitModel ScanAdd { get; } = new(0x1920, 0);
        public BitsModel Scanmode { get; } = new(0x0ca1, 0xc0);
        public BitModel PriorityTx { get; } = new(0x0ca0, 0);
        public BitModel Sync { get; } = new(0x0ca2, 6);
        public BitsModel MicGain { get; } = new(0x1f20, 0x1f);
        public BitsModel BreathLed { get; } = new(0x0caf, 0xf0);
        public BitModel Beep { get; } = new(0x0ca1, 2);
        public BitsModel DispLcd { get; } = new(0x0ca0, 0xc0);
        public BitModel ChOnly { get; } = new(0x0caf, 7);
        public BitsModel VoxGain { get; } = new(0x0ca7, 0x7);
        public BitsModel VoxDelay { get; } = new(0x0cae, 0x3);
        public BitsModel Pf1Short { get; } = new(0x0c91, 0x7);
        public BitsModel Pf1Long { get; } = new(0x0c94, 0x7);
        public BitsModel PonMode { get; } = new(0x0ca3, 0xc0);
        public ByteModel UhfAdj { get; } = new(0x1f5f);
        public ByteModel VhfAdj { get; } = new(0x1f5e);
        public BitModel BlueTooth { get; } = new(0x1f30, 0);
        public BitModel SegmentA { get; } = new(0x0ca2, 2);
        public BitModel SegmentB { get; } = new(0x0ca3, 4);
        public BitModel ToneMonitor { get; } = new(0x0ca2, 3);
        public BitsModel RepTone { get; } = new(0x0ca2, 0x30);
        public BitsModel RadioMode { get; } = new(0x0ca0, 0xc);
        public BitModel TailToneCancel { get; } = new(0x0ca7, 6);
        public BitModel RemoteKill { get; } = new(0x0ca7, 4);
        public BitModel RemoteHalo { get; } = new(0x0ca7, 3);
        public BitsModel SoundControl { get; } = new(0x0ca7, 0x7);
        public BitModel DDCD { get; } = new(0x0c98, 0);
        public BitsModel DtmfReset { get; } = new(0x0c99, 0x7);
        public BitsModel AutoAnswer { get; } = new(0x0c9a, 0x7);
        public BitModel AmBand { get; } = new(0x0caf, 2);
        public BitModel Tx200 { get; } = new(0x0cab, 4);
        public BitModel Tx350 { get; } = new(0x0cab, 3);
        public BitModel Tx500 { get; } = new(0x0cab, 2);
        public StringModel PonMsg1 { get; } = new(0x1c00, 16);
        public StringModel PonMsg2 { get; } = new(0x1c10, 16);
        public StringModel PonMsg3 { get; } = new(0x1c20, 16);
        public DtmfModel SelfId { get; } = new(0x1820, 3);
        public DtmfModel GroupCode { get; } = new(0x1829, 1);
        public BitModel DtmfSidetone { get; } = new(0xca0, 2);
        public DtmfModel PttIdStart { get; } = new(0x18c0, 7);
        public DtmfModel PttIdEnd { get; } = new(0x18d0, 7);
        public DtmfModel HaloCode { get; } = new(0x1800, 8);
        public DtmfModel KillCode { get; } = new(0x1810, 8);
        public DtmfModel CallCode1 { get; } = new(0x1830, 16);
        public DtmfModel CallCode2 { get; } = new(0x1840, 16);
        public DtmfModel CallCode3 { get; } = new(0x1850, 16);
        public DtmfModel CallCode4 { get; } = new(0x1860, 16);
        public DtmfModel CallCode5 { get; } = new(0x1870, 16);
        public DtmfModel CallCode6 { get; } = new(0x1880, 16);
        public DtmfModel CallCode7 { get; } = new(0x1890, 16);
        public DtmfModel CallCode8 { get; } = new(0x18a0, 16);
        public BitModel TunerMode { get; } = new(0x0ca2, 7);
        public BitModel ForbidRec { get; } = new(0x0ca2, 3);
        public BitsModel TunerChan { get; } = new(0x0ca6, 0x1f);
        public BcdrModel VhfLow { get; } = new(0x0cc0, 18.0, 350.0, 136.0, 174.0);
        public BcdrModel VhfHigh { get; } = new(0x0cc2, 18.0, 350.0, 136.0, 174.0);
        public BcdrModel UhfLow { get; } = new(0x0cc4, 350.0, 660.0, 400.0, 520.0);
        public BcdrModel UhfHigh { get; } = new(0x0cc6, 350.0, 660.0, 400.0, 520.0);
        public BcdfModel FmVfoFreq { get; } = new(0x1970, 76.0, 108.0);
        public BitsModel Brightness { get; } = new(0x0c9d, 0x7);

        public ViewModel<string> UhfAdjLab { get; } = new("0 Hz");
        public ViewModel<string> VhfAdjLab { get; } = new("0 Hz");
        public ViewModel<bool> LiveMode { get; } = new(false);
        public ViewModel<double> LiveModeOpacity { get; } = new(1.0);
        public ViewModel<bool> OfflineMode { get; } = new(true);
        public ViewModel<double> OfflineModeOpacity { get; } = new(1.0);
        public ToneMenu Tones { get; } = new();

        public bool FirstRun { get; set; } = true;

        public ViewModel<Key> KeyPad { get; } = new(Key.None);


        public static double Steps(int i) => i switch
        {
            0 => 2.5,
            1 => 5.0,
            2 => 6.25,
            3 => 10.0,
            4 => 12.5,
            5 => 25.0,
            _ => 50.0,
        };
        public double StepA => FreqStepOverrideA.Value > 0 ? FreqStepOverrideA.Value : Steps(FreqStepA.Value);
        public double StepB => FreqStepOverrideB.Value > 0 ? FreqStepOverrideB.Value : Steps(FreqStepB.Value);

        public static ObservableCollection<Patch> PatchList => Patches.List;
        public static string[] PortsAvailable => SerialPort.GetPortNames();
        public static string[] AvailPorts => PortsAvailable.Prepend("Offline").ToArray();
        public static object[] AvailPortsDownload =>
            PortsAvailable
            .Select(s => new MenuItem() { Header = s })
            .Prepend(new MenuItem() { Header = "", IsEnabled = false })
            .Prepend(new MenuItem() { Header = "Read from Radio", IsEnabled = false })
            .ToArray();

        public static object[] AvailPortsUpload =>
            PortsAvailable
            .Select(s => new MenuItem() { Header = s })
            .Prepend(new MenuItem() { Header = "", IsEnabled = false })
            .Prepend(new MenuItem() { Header = "Write to Radio", IsEnabled = false })
            .ToArray();

        private void SetSplitDir(bool A)
        {
            if (A)
                SplitDirA.Value = VfoTxA.Value > VfoRxA.Value ? "▲" : VfoTxA.Value < VfoRxA.Value ? "▼" : string.Empty;
            else
                SplitDirB.Value = VfoTxB.Value > VfoRxB.Value ? "▲" : VfoTxB.Value < VfoRxB.Value ? "▼" : string.Empty;
        }

        public Context()
        {
            VfoRxA.PropertyChanged += (s, e) =>
            {
                if (Ready)
                {
                    if (!SplitTxA.Value)
                        VfoTxA.Value = VfoRxA.Value;
                    else
                        TD.Update();
                    SetSplitDir(true);
                }
            };
            VfoTxA.PropertyChanged += (s, e) =>
            {
                if (Ready)
                {
                    SetSplitDir(true);
                    if (VfoTxA.Value == VfoRxA.Value)
                        SplitTxA.Value = false;
                    TD.Update();
                }
            };
            BandwidthA.PropertyChanged += (s, e) =>
            {
                BwLabelA.Value = BandwidthA.Value ? "N" : "W";
                if (Ready)
                {
                    TD.Update();
                }
            };
            PowerA.PropertyChanged += (s, e) =>
            {
                PwrLabelA.Value = PowerA.Value == 0 ? "L" : "H";
                if (Ready)
                {
                    TD.Update();
                }
            };
            FreqStepA.PropertyChanged += FreqStep_PropertyChanged;
            FreqStepOverrideA.PropertyChanged += FreqStep_PropertyChanged;
            VfoChA.PropertyChanged += (s, e) =>
            {
                suspend = true;
                if (VfoChA.Value == 0)
                    ApplyChannelA(0);
                else
                    ApplyChannelA(VfoChNumA.Value);
                suspend = false;
                if (Ready) TD.Update();
            };
            VfoChNumA.PropertyChanged += (s, e) =>
            {
                suspend = true;
                if (VfoChA.Value == 0)
                    ApplyChannelA(0);
                else
                    ApplyChannelA(VfoChNumA.Value);
                suspend = false;
                if (Ready) TD.Update();
            };
            VfoChNumEnterA.PropertyChanged += (s, e) => 
            {
                int num = VfoChNumEnterA.Value;
                if (num > 0 && num < 200 && Comms.EEPROM[num * 0x10] != 0xff)
                    VfoChNumA.Value = num;
                else
                    VfoChNumEnterA.Value = VfoChNumA.Value;
            };
            ToneRxA.PropertyChanged += (s, e) => { if (Ready) ToneTxA.Value = ToneRxA.Value; };
            ToneTxA.PropertyChanged += (s, e) => { if (Ready) TD.Update(); };
            BusyLockA.PropertyChanged += (s, e) =>
            {
                //BusyLockLabelA.Value = BusyLockA.Value ? "B" : "-";
                if (Ready) TD.Update();
            };
            ScrambleA.PropertyChanged += (s, e) =>
            {
                //ScrambleALabel.Value = ScrambleA.Value ? "S" : "-";
                if (Ready) TD.Update();
            };
            ReverseA.PropertyChanged += (s, e) =>
            {
                //ReverseALabel.Value = ReverseA.Value ? "R" : "-";
                VfoRxA = ReverseA.Value ? vfoTxA : vfoRxA;
                VfoTxA = ReverseA.Value ? vfoRxA : vfoTxA;
                OnPropertyChanged(nameof(VfoRxA));
                OnPropertyChanged(nameof(VfoTxA));
                SetSplitDir(true);
                if (Ready) TD.Update();
            };
            PttIdA.PropertyChanged += (s, e) =>
            {
                PttIdALab.Value = PttIdA.Value switch
                {
                    1 => "BOT",
                    2 => "EOT",
                    3 => "BTH",
                    _ => "PID",
                };
                if (Ready) TD.Update();
            };




            VfoRxB.PropertyChanged += (s, e) =>
            {
                if (Ready)
                {
                    if (!SplitTxB.Value)
                        VfoTxB.Value = VfoRxB.Value;
                    else
                        TD.Update();
                    SetSplitDir(false);
                }
            };
            VfoTxB.PropertyChanged += (s, e) =>
            {
                if (Ready)
                {
                    SetSplitDir(false);
                    if (VfoTxB.Value == VfoRxB.Value)
                        SplitTxB.Value = false;
                    TD.Update();
                }
            };
            BandwidthB.PropertyChanged += (s, e) =>
            {
                BwLabelB.Value = BandwidthB.Value ? "N" : "W";
                if (Ready)
                {
                    TD.Update();
                }
            };
            PowerB.PropertyChanged += (s, e) =>
            {
                PwrLabelB.Value = PowerB.Value == 0 ? "L" : "H";
                if (Ready)
                {
                    TD.Update();
                }
            };
            FreqStepB.PropertyChanged += FreqStep_PropertyChanged;
            FreqStepOverrideB.PropertyChanged += FreqStep_PropertyChanged;
            VfoChB.PropertyChanged += (s, e) =>
            {
                suspend = true;
                if (VfoChB.Value == 0)
                    ApplyChannelB(0);
                else
                    ApplyChannelB(VfoChNumB.Value);
                suspend = false;
                if (Ready) TD.Update();
            };
            VfoChNumB.PropertyChanged += (s, e) =>
            {
                suspend = true;
                if (VfoChB.Value == 0)
                    ApplyChannelB(0);
                else
                    ApplyChannelB(VfoChNumB.Value);
                suspend = false;
                if (Ready) TD.Update();
            };
            VfoChNumEnterB.PropertyChanged += (s, e) =>
            {
                int num = VfoChNumEnterB.Value;
                if (num > 0 && num < 200 && Comms.EEPROM[num * 0x10] != 0xff)
                    VfoChNumB.Value = num;
                else
                    VfoChNumEnterB.Value = VfoChNumB.Value;
            };
            ToneRxB.PropertyChanged += (s, e) => { if (Ready) ToneTxB.Value = ToneRxB.Value; };
            ToneTxB.PropertyChanged += (s, e) => { if (Ready) TD.Update(); };
            BusyLockB.PropertyChanged += (s, e) =>
            {
                //BusyLockLabelB.Value = BusyLockB.Value ? "B" : "-";
                if (Ready) TD.Update();
            };
            ScrambleB.PropertyChanged += (s, e) =>
            {
                //ScrambleBLabel.Value = ScrambleB.Value ? "S" : "-";
                if (Ready) TD.Update();
            };
            ReverseB.PropertyChanged += (s, e) =>
            {
                //ReverseBLabel.Value = ReverseB.Value ? "R" : "-";
                VfoRxB = ReverseB.Value ? vfoTxB : vfoRxB;
                VfoTxB = ReverseB.Value ? vfoRxB : vfoTxB;
                OnPropertyChanged(nameof(VfoRxB));
                OnPropertyChanged(nameof(VfoTxB));
                SetSplitDir(false);
                if (Ready) TD.Update();
            };
            PttIdB.PropertyChanged += (s, e) =>
            {
                PttIdBLab.Value = PttIdB.Value switch
                {
                    1 => "BOT",
                    2 => "EOT",
                    3 => "BTH",
                    _ => "PID",
                };
                if (Ready) TD.Update();
            };

            DualWatch.PropertyChanged += (s, e) =>
            {
                DwLabel.Value = DualWatch.Value ? "🗘" : "⛛";
                if (Ready)
                {
                    TD.Update();
                }
            };
            Roger.PropertyChanged += (s, e) =>
            {
                RgrLabel.Value = Roger.Value switch
                {
                    0 or 1 => "𝄽",
                    2 or 3 => "♪",
                    _ => "♫",
                };
                if (Ready)
                {
                    TD.Update();
                }
            };
            SelectedVfo.PropertyChanged += (s, e) => 
            {
                SelectedVfoA.Value = !SelectedVfo.Value;
                SelectedVfoB.Value = SelectedVfo.Value;
                VfoOpacityA.Value = SelectedVfoA.Value ? 1.0 : 0.75;
                VfoOpacityB.Value = SelectedVfoB.Value ? 1.0 : 0.75;
                VfoMarkerA.Value = SelectedVfoA.Value ? "•" : string.Empty;
                VfoMarkerB.Value = SelectedVfoB.Value ? "•" : string.Empty;
                VfoBorderA.Value = SelectedVfoA.Value ? StateBrush : darkBrush;
                VfoBorderB.Value = SelectedVfoB.Value ? StateBrush : darkBrush;
                if (Ready)
                    TD.Update();
            };
            LiveMode.PropertyChanged += (s, e) =>
            {
                LiveModeOpacity.Value = LiveMode.Value ? 1.0 : 0.5;
                OfflineMode.Value = !LiveMode.Value;
                OfflineModeOpacity.Value = LiveMode.Value ? 0.5 : 1.0;
            };
            State.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(StateBrush));
                SelectedVfo.ForceUpdate++;
            };
            ComPort.PropertyChanged += (s, e) => SetComPort();
        }

        private void FreqStep_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            FrqStpLabelA.Value = $"{StepA:F2}k";
            FrqStpLabelB.Value = $"{StepB:F2}k";
            if (Ready)
            {
                TD.Update();
            }
        }

        private const string zero7 = "0000000";
        private const string zero8 = "00000000";
        private const string zero16 = "0000000000000000";
        public void CleanDTMF()
        {
            if
            (
               PttIdStart.Value.Equals(zero7) &&
               PttIdEnd.Value.Equals(zero7) &&
               HaloCode.Value.Equals(zero8) &&
               KillCode.Value.Equals(zero8) &&
               CallCode1.Value.Equals(zero16) &&
               CallCode2.Value.Equals(zero16) &&
               CallCode3.Value.Equals(zero16) &&
               CallCode4.Value.Equals(zero16) &&
               CallCode5.Value.Equals(zero16) &&
               CallCode6.Value.Equals(zero16) &&
               CallCode7.Value.Equals(zero16) &&
               CallCode8.Value.Equals(zero16)
            )
            {
                PttIdStart.Value =
                PttIdEnd.Value =
                HaloCode.Value =
                KillCode.Value =
                CallCode1.Value =
                CallCode2.Value =
                CallCode3.Value =
                CallCode4.Value =
                CallCode5.Value =
                CallCode6.Value =
                CallCode7.Value =
                CallCode8.Value =
                    string.Empty;
            }
        }

        private void ApplyChannelA(int num)
        {
            if (num <= 0 || num > 199)
            {
                VfoChNumALabel.Value = string.Empty;
                VfoChALabel.Value = "VFO-A";
                VfoRxA.AltAddress = -1;
                VfoTxA.AltAddress = -1;
                ToneTxA.AltAddress = -1;
                ToneRxA.AltAddress = -1;
                PowerA.AltAddress = -1;
                BusyLockA.AltAddress = -1;
                BandwidthA.AltAddress = -1;
                ScrambleA.AltAddress = -1;
                ReverseA.AltAddress = -1;
                AllowEditA = true;
            }            
            else
            {
                AllowEditA = false;
                VfoChNumALabel.Value = $"{num:D3}";
                string name = Encoding.ASCII.GetString(Comms.EEPROM, 0xd38 + num * 8, 8).Trim('\0');
                if (name.Length == 0) name = $"CH-{VfoChNumALabel.Value}";
                VfoChALabel.Value = name;
                num *= 0x10;
                VfoRxA.AltAddress = num;
                VfoTxA.AltAddress = num + 0x4;
                ToneTxA.AltAddress = num + 0xa;
                ToneRxA.AltAddress = num + 0x8;
                PowerA.AltAddress = num + 0xe;
                BusyLockA.AltAddress = num + 0xd;
                BandwidthA.AltAddress = num + 0xe;
                ScrambleA.AltAddress = num + 0xe;
                ReverseA.AltAddress = num + 0xe;
            }
            SplitDirA.Value = VfoTxA.Value > VfoRxA.Value ? "▲" : VfoTxA.Value < VfoRxA.Value ? "▼" : string.Empty;
            SplitTxA.Value = VfoTxA.Value != VfoRxA.Value;
            VfoRxA.ForceUpdate++;
            VfoTxA.ForceUpdate++;
            ToneTxA.ForceUpdate++;
            ToneRxA.ForceUpdate++;
            PowerA.ForceUpdate++;
            BusyLockA.ForceUpdate++;
            BandwidthA.ForceUpdate++;
            ScrambleA.ForceUpdate++;
            ReverseA.ForceUpdate++;
            SelectedVfo.ForceUpdate++;            
        }

        private void ApplyChannelB(int num)
        {
            if (num <= 0 || num > 199)
            {
                VfoChNumBLabel.Value = string.Empty;
                VfoChBLabel.Value = "VFO-B";
                VfoRxB.AltAddress = -1;
                VfoTxB.AltAddress = -1;
                ToneTxB.AltAddress = -1;
                ToneRxB.AltAddress = -1;
                PowerB.AltAddress = -1;
                BusyLockA.AltAddress = -1;
                BandwidthB.AltAddress = -1;
                ScrambleB.AltAddress = -1;
                ReverseB.AltAddress = -1;
                AllowEditB = true;
            }
            else
            {
                AllowEditB = false;
                VfoChNumBLabel.Value = $"{num:D3}";
                string name = Encoding.ASCII.GetString(Comms.EEPROM, 0xd38 + num * 8, 8).Trim('\0');
                if (name.Length == 0) name = $"CH-{VfoChNumBLabel.Value}";
                VfoChBLabel.Value = name;
                num *= 0x10;
                VfoRxB.AltAddress = num;
                VfoTxB.AltAddress = num + 0x4;
                ToneTxB.AltAddress = num + 0xa;
                ToneRxB.AltAddress = num + 0x8;
                PowerB.AltAddress = num + 0xe;
                BusyLockB.AltAddress = num + 0xd;
                BandwidthB.AltAddress = num + 0xe;
                ScrambleB.AltAddress = num + 0xe;
                ReverseB.AltAddress = num + 0xe;
            }
            SplitDirB.Value = VfoTxB.Value > VfoRxB.Value ? "▲" : VfoTxB.Value < VfoRxB.Value ? "▼" : string.Empty;
            SplitTxB.Value = VfoTxB.Value != VfoRxB.Value;
            VfoRxB.ForceUpdate++;
            VfoTxB.ForceUpdate++;
            ToneTxB.ForceUpdate++;
            ToneRxB.ForceUpdate++;
            PowerB.ForceUpdate++;
            BusyLockB.ForceUpdate++;
            BandwidthB.ForceUpdate++;
            ScrambleB.ForceUpdate++;
            ReverseB.ForceUpdate++;
        }



        private void SetComPort()
        {
            UiReady.Value = false;
            Comms.SetPort(ComPort.Value, ok => 
            {
                if (ok)
                {
                    LiveMode.Value = true;
                    SyncToRadio();
                }
                else
                    LiveMode.Value = false;
                UiReady.Value = true;
            });
        }

        public void SyncToRadio()
        {
            CleanDTMF();
            SplitTxA.Value = VfoRxA.Value != VfoTxA.Value;
            VfoRxA.ForceUpdate++;
            VfoTxA.ForceUpdate++;
            BandwidthA.ForceUpdate++;
            FreqStepA.ForceUpdate++;
            ToneTxA.ForceUpdate++;
            ToneRxA.ForceUpdate++;
            VfoChA.ForceUpdate++;
            VfoChNumA.ForceUpdate++;
            BusyLockA.ForceUpdate++;
            ScrambleA.ForceUpdate++;
            ReverseA.ForceUpdate++;

            SplitTxB.Value = VfoRxB.Value != VfoTxB.Value;
            VfoRxB.ForceUpdate++;
            VfoTxB.ForceUpdate++;
            BandwidthB.ForceUpdate++;
            FreqStepB.ForceUpdate++;
            ToneTxB.ForceUpdate++;
            ToneRxB.ForceUpdate++;
            VfoChB.ForceUpdate++;
            VfoChNumB.ForceUpdate++;
            BusyLockB.ForceUpdate++;
            ScrambleB.ForceUpdate++;
            ReverseB.ForceUpdate++;

            DualWatch.ForceUpdate++;
            Roger.ForceUpdate++;
            Squelch.ForceUpdate++;
            Backlight.ForceUpdate++;
            BattSave.ForceUpdate++;
            RadioLang.ForceUpdate++;
            VoicePrompt.ForceUpdate++;
            AutoLock.ForceUpdate++;
            Timeout.ForceUpdate++;
            Scanmode.ForceUpdate++;
            ScanAdd.ForceUpdate++;
            PriorityTx.ForceUpdate++;
            Sync.ForceUpdate++;
            MicGain.ForceUpdate++;
            BreathLed.ForceUpdate++;
            Beep.ForceUpdate++;
            DispLcd.ForceUpdate++;
            ChOnly.ForceUpdate++;
            VoxGain.ForceUpdate++;
            VoxDelay.ForceUpdate++;
            Pf1Long.ForceUpdate++;
            Pf1Short.ForceUpdate++;
            PonMode.ForceUpdate++;
            UhfAdj.ForceUpdate++;
            VhfAdj.ForceUpdate++;
            BlueTooth.ForceUpdate++;
            SegmentA.ForceUpdate++;
            SegmentB.ForceUpdate++;
            ToneMonitor.ForceUpdate++;
            RepTone.ForceUpdate++;
            RadioMode.ForceUpdate++;
            TailToneCancel.ForceUpdate++;
            RemoteKill.ForceUpdate++;
            RemoteHalo.ForceUpdate++;
            SoundControl.ForceUpdate++;
            DDCD.ForceUpdate++;
            DtmfReset.ForceUpdate++;
            AutoAnswer.ForceUpdate++;
            Tx200.ForceUpdate++;
            Tx350.ForceUpdate++;
            Tx500.ForceUpdate++;
            AmBand.ForceUpdate++;
            PonMsg1.ForceUpdate++;
            PonMsg2.ForceUpdate++;
            PonMsg3.ForceUpdate++;
            SelfId.ForceUpdate++;
            GroupCode.ForceUpdate++;
            DtmfSidetone.ForceUpdate++;
            PttIdStart.ForceUpdate++;
            PttIdEnd.ForceUpdate++;
            HaloCode.ForceUpdate++;
            KillCode.ForceUpdate++;
            CallCode1.ForceUpdate++;
            CallCode2.ForceUpdate++;
            CallCode3.ForceUpdate++;
            CallCode4.ForceUpdate++;
            CallCode5.ForceUpdate++;
            CallCode6.ForceUpdate++;
            CallCode7.ForceUpdate++;
            CallCode8.ForceUpdate++;
            TunerMode.ForceUpdate++;
            ForbidRec.ForceUpdate++;
            TunerChan.ForceUpdate++;
            VhfLow.ForceUpdate++;
            VhfHigh.ForceUpdate++;
            UhfLow.ForceUpdate++;
            UhfHigh.ForceUpdate++;
            FmVfoFreq.ForceUpdate++;
            Brightness.ForceUpdate++;
            PttIdA.ForceUpdate++;
            PttIdB.ForceUpdate++;
            TunerStuff.Value = [];
            TunerStuff.Value = TunerChannel.Mem;
            TestStuff.Value = [];
            TestStuff.Value = Channel.Mem;
            ReadPowerLevels();
        }

        public void ResetPowerLevels()
        {
            for (int i = 0x1f50, k = 0; i < 0x1f7e; i++)
            {
                if (i == 0x1f6e) { i = 0x1f70; k++; }
                Comms.Write(i, Comms.BlankEEPROM[i]);
            }
            SyncToRadio();
        }

        public void ReadPowerLevels()
        {
            for (int i = 0x1f50, j = 0, k = 0; i < 0x1f7e; i++, j++)
            {
                if (i == 0x1f5e) { i = 0x1f60; k++; }
                if (i == 0x1f6e) { i = 0x1f70; k++; }
                if (k == 0) Powers.Value[j % 14].Low = Comms.EEPROM[i];
                if (k == 1) Powers.Value[j % 14].Mid = Comms.EEPROM[i];
                if (k == 2) Powers.Value[j % 14].High = Comms.EEPROM[i];
            }
            Powers.ForceUpdate++;
            VhfAdjLab.Value = FreqAdj[VhfAdj.Value];
            UhfAdjLab.Value = FreqAdj[UhfAdj.Value];
        }

        public void WritePowerLevels()
        {
            for (int i = 0x1f50, j = 0, k = 0; i < 0x1f7e; i++, j++)
            {
                if (i == 0x1f5e) { i = 0x1f60; k++; }
                if (i == 0x1f6e) { i = 0x1f70; k++; }
                if (k == 0) Comms.Write(i, Powers.Value[j % 14].Low);
                if (k == 1) Comms.Write(i, Powers.Value[j % 14].Mid);
                if (k == 2) Comms.Write(i, Powers.Value[j % 14].High);
            }
            Comms.Write(0x1f5e, (byte)FreqAdj.IndexOf(VhfAdjLab.Value));
            Comms.Write(0x1f5f, (byte)FreqAdj.IndexOf(UhfAdjLab.Value));
            TD.Update();
        }
    }
}
