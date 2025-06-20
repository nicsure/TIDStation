﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TIDStation.Data;
using TIDStation.General;
using TIDStation.Radio;
using TIDStation.Serial;
using TIDStation.UI;
using TIDStation.View;

namespace TIDStation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; set; }
        private CancellationTokenSource? flashCancel = null;

        public MainWindow()
        {
            Context.Activator++;
            Instance = this;
            DataContext = Context.Instance;
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;
            IsEnabledChanged += MainWindow_IsEnabledChanged;
            MainBorder.IsEnabledChanged += MainWindow_IsEnabledChanged;
            StepsEntry.EntryComplete += StepsEntry_EntryComplete;
            ScanGraph.BarClicked += Scanner_BarClicked;
            Context.Instance.KeyPad.PropertyChanged += KeyPad_PropertyChanged;
            Frequency.Default = VfoRxA;
            Frequency.Current = VfoRxA;
            Context.Instance.SyncToRadio();
            _ = Comms.RssiWatcher();
        }

        private void KeyPad_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ProcessKey(Context.Instance.KeyPad.Value);
        }

        private void MainWindow_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsEnabled) Focus();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double ld = Math.Min(ActualHeight, ActualWidth);
            Context.Instance.FontSize.Value[0] = ld / 100.0;
            Context.Instance.FontSize.ForceUpdate++;
        }

        private void ScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseLeftButtonDown(e);
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs? e)
        {
            if (e != null)
            {
                base.OnMouseLeftButtonDown(e);
                if (Mouse.DirectlyOver is Grid || Mouse.DirectlyOver is Border)
                    DragMove();
            }
            if (Mouse.DirectlyOver is FrameworkElement fe)
            {
                if (!Context.Instance.UiReady.Value) return;
                while (fe != null)
                {
                    if (fe.ContextMenu is ContextMenu cm && !"NLC".Equals(cm.Tag))
                    {
                        if (e != null) e.Handled = true;
                        cm.PlacementTarget = fe;
                        cm.IsOpen = true;
                        break;
                    }
                    fe = (fe.TemplatedParent is FrameworkElement fe2) ? fe2 : null!;
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if(Keyboard.FocusedElement is not System.Windows.Controls.Primitives.TextBoxBase)
                ProcessKey(e.Key);
            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if(e.Key == Key.Space)
                ProcessKey(Key.None);
            base.OnPreviewKeyUp(e);
        }

        private static bool AllowEdit => Context.Instance.SelectedVfoA.Value ? Context.Instance.AllowEditA : Context.Instance.AllowEditB;
        private static ByteModel VfoChNum => Context.Instance.SelectedVfoA.Value ? Context.Instance.VfoChNumA : Context.Instance.VfoChNumB;
        private static double Step => Context.Instance.SelectedVfoA.Value ? Context.Instance.StepA : Context.Instance.StepB;
        private static FreqModel VfoRx => Context.Instance.SelectedVfoA.Value ? Context.Instance.VfoRxA : Context.Instance.VfoRxB;
        private NumEntry ChannelEntry => Context.Instance.SelectedVfoA.Value ? ChannelEntryA : ChannelEntryB;
        private Frequency RX => Context.Instance.SelectedVfoA.Value ? VfoRxA : VfoRxB;
        private Frequency TX => Context.Instance.SelectedVfoA.Value ? VfoTxA : VfoTxB;

        private void ProcessKey(Key k)
        {
            if(!Context.Instance.UiReady.Value)
            {
                try { flashCancel?.Cancel(); } catch { }
                return;
            }
            if(stepEntry != null)
            {
                StepsEntry.KeyIn(k);
                return;
            }
            switch (k)
            {
                case Key.None:
                    Comms.SimulateKey(0);
                    break;
                case Key.Space:
                    Comms.SimulateKey(Context.Instance.SelectedVfo.Value ? 0x1a : 0x13);
                    break;
                case Key.Tab:
                    if(!Context.Instance.AnalyserMode.Value)
                        Context.Instance.SelectedVfo.Value = !Context.Instance.SelectedVfo.Value;
                    break;
                case Key.Up:
                case Key.Down:
                    int p = k == Key.Up ? 1 : -1;
                    if (!AllowEdit)
                    {
                        int cn = VfoChNum.Value, cs = cn;
                        if (cn > 0)
                        {
                            while (true)
                            {
                                cs += p;
                                if (cs > 199) cs = 1;
                                if (cs < 1) cs = 199;
                                if (Comms.EEPROM[cs * 0x10] != 0xff)
                                {
                                    VfoChNum.Value = cs;
                                    cs = cn;
                                }
                                if (cs == cn)
                                    return;
                            }
                        }
                    }
                    else
                    {
                        double s = Step / 1000.0;
                        double rx = VfoRx.Value + (s * p);
                        if (Comms.ShiftMode) rx -= 0.45568;
                        rx = Math.Round(rx / s) * s;
                        if (Comms.ShiftMode) rx += 0.45568;
                        VfoRx.Value = rx;
                    }
                    break;
                default:
                    if (AllowEdit)
                    {
                        if(TX.Equals(Frequency.Current))
                            TX.KeyIn(k);
                        else
                            RX.KeyIn(k);
                    }
                    else
                        ChannelEntry.KeyIn(k);
                    break;
            }

        }

        private void ComMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                Context.Instance.ComPort.Value = (string)mi.Header;
            }
        }

        private void VfoTxA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Context.Instance.SelectedVfoA.Value) return;
            Context.Instance.SplitTxA.Value = true;
            Frequency.Current = VfoTxA;
            VfoTxA.Select();
        }

        private void SpDrA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.VfoTxA.Value = Context.Instance.VfoRxA.Value;
        }

        private void BwLabA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.BandwidthA.Value = !Context.Instance.BandwidthA.Value;
        }

        private void PwrLabA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.PowerA.Value = Context.Instance.PowerA.Value == 0 ? 2 : 0;
        }

        private void FqStpLabA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.FreqStepA.Value = (Context.Instance.FreqStepA.Value + 1) % 7;
            Context.Instance.FreqStepOverrideA.Value = 0;
        }

        private void ScLabA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.ScrambleA.Value = !Context.Instance.ScrambleA.Value;
        }


        private void ToneMenuItemTxA_Click(object sender, RoutedEventArgs e)
        {
            if (sender is IToneMenu toneMenu)
                Context.Instance.ToneTxA.Value = toneMenu.ClickedOption;
            else
                Context.Instance.ToneTxA.Value = string.Empty;
        }

        private void ToneMenuItemRxA_Click(object sender, RoutedEventArgs e)
        {
            if (sender is IToneMenu toneMenu)
                Context.Instance.ToneRxA.Value = toneMenu.ClickedOption;
            else
                Context.Instance.ToneRxA.Value = string.Empty;
        }

        private void VfoChASel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int i = Context.Instance.VfoChA.Value == 0 ? 1 : 0;
            Context.Instance.VfoChA.Value = i;
        }









        private void DwLabA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.DualWatch.Value = !Context.Instance.DualWatch.Value;
        }

        private void RgLabA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.Roger.Value = Context.Instance.Roger.Value switch
            {
                0 or 1 => 2,
                2 or 3 => 4,
                _ => 0,
            };
        }











        private void VfoTxB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Context.Instance.SelectedVfoB.Value) return;
            Context.Instance.SplitTxB.Value = true;
            Frequency.Current = VfoTxB; // TODO
            VfoTxB.Select();
        }

        private void SpDrB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.VfoTxB.Value = Context.Instance.VfoRxB.Value;
        }

        private void BwLabB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.BandwidthB.Value = !Context.Instance.BandwidthB.Value;
        }

        private void PwrLabB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.PowerB.Value = Context.Instance.PowerB.Value == 0 ? 2 : 0;
        }

        private void FqStpLabB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.FreqStepB.Value = (Context.Instance.FreqStepB.Value + 1) % 7;
            Context.Instance.FreqStepOverrideB.Value = 0;
        }

        private void ScLabB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.ScrambleB.Value = !Context.Instance.ScrambleB.Value;
        }

        private void ToneMenuItemTxB_Click(object sender, RoutedEventArgs e)
        {
            if (sender is IToneMenu toneMenu)
                Context.Instance.ToneTxB.Value = toneMenu.ClickedOption;
            else
                Context.Instance.ToneTxB.Value = string.Empty;
        }

        private void ToneMenuItemRxB_Click(object sender, RoutedEventArgs e)
        {
            if (sender is IToneMenu toneMenu)
                Context.Instance.ToneRxB.Value = toneMenu.ClickedOption;
            else
                Context.Instance.ToneRxB.Value = string.Empty;
        }

        private void VfoChBSel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int i = Context.Instance.VfoChB.Value == 0 ? 1 : 0;
            Context.Instance.VfoChB.Value = i;
        }

        private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void MaximizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void MinimizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void VfoLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Context.Instance.SelectedVfo.Value = VfoLabelB.Equals(sender);
        }

        private void BusyLab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (BusyLabA.Equals(sender))
                Context.Instance.BusyLockA.Value = !Context.Instance.BusyLockA.Value;
            else
                Context.Instance.BusyLockB.Value = !Context.Instance.BusyLockB.Value;
        }

        private void PttLab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BitsModel bits = PttLabA.Equals(sender) ? Context.Instance.PttIdA : Context.Instance.PttIdB;
            bits.Value = (bits.Value + 1) % 4;
        }


        private void AppMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HaltFA();
            Context.Instance.AnalyserMode.Value = false;
            Context.Instance.ChannelModeVis.Value = ChannelMode.Equals(sender) ? Visibility.Visible : Visibility.Hidden;
            Context.Instance.PowerModeVis.Value = PowerMode.Equals(sender) ? Visibility.Visible : Visibility.Hidden;
            Context.Instance.FlashModeVis.Value = FlashMode.Equals(sender) ? Visibility.Visible : Visibility.Hidden;
            Context.Instance.TunerModeVis.Value = TunerMode.Equals(sender) ? Visibility.Visible : Visibility.Hidden;
            Context.Instance.RadioOpc.Value = RadioMode.Equals(sender) ? 1.0 : 0.5;
            Context.Instance.ChannelOpc.Value = ChannelMode.Equals(sender) ? 1.0 : 0.5;
            Context.Instance.PowerOpc.Value = PowerMode.Equals(sender) ? 1.0 : 0.5;
            Context.Instance.FlashOpc.Value = FlashMode.Equals(sender) ? 1.0 : 0.5;
            Context.Instance.TunerOpc.Value = TunerMode.Equals(sender) ? 1.0 : 0.5;
        }

        private static readonly string[] bands = [
            "136-140 ",
            "140-150 ",
            "150-160 ",
            "160-170 ",
            "170- ",
            "400-410 ",
            "410-420 ",
            "420-430 ",
            "430-440 ",
            "440-450 ",
            "450-460 ",
            "460-470 ",
            "470- ",
            "245 ",
        ];
        private static int rCnt = 0;
        private void PowerGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if(rCnt<14)
                e.Row.Header = bands[rCnt++];
        }

        public void FixDataDrid()
        {
            foreach (var col in PowerGrid.Columns)
            {
                if (col == null) continue;
                col.Width = 0;
                col.Width = DataGridLength.Auto;
            }            
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FixDataDrid();
        }

        private void PowerReset_Click(object sender, RoutedEventArgs e)
        {
            Context.Instance.ResetPowerLevels();
        }

        private void PowerRevert_Click(object sender, RoutedEventArgs e)
        {
            Context.Instance.ReadPowerLevels();
        }

        private void PowerApply_Click(object sender, RoutedEventArgs e)
        {
            Context.Instance.WritePowerLevels();
        }

        private void PowerGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction == DataGridEditAction.Commit && e.EditingElement is TextBox tb)
            {
                int val = (int.TryParse(tb.Text, out int i) ? i : -1);
                if (val < 0 || val > 255)
                {
                    tb.Text = val.Clamp(0, 255).ToString();
                    e.Cancel = true;
                }
            }
        }

        private void PowerFrame_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FixDataDrid();
        }

        private void FlashBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Title = "Choose firmware binary",
                Filter = "Bin Files|*.bin|All Files|*.*"
            };
            if(ofd.ShowDialog() ?? false)
            {
                Context.Instance.FlashFile.Value = ofd.FileName;                               
            }
        }

        private void FlashSave_Click(object sender, RoutedEventArgs e)
        {
            byte[] firmware = new byte[65536];
            string err = Comms.ProcessFirmware(Context.Instance.FlashFile.Value, firmware, out int fmLength);
            if (fmLength > 0)
            {
                var sfd = new SaveFileDialog()
                {
                    Title = "Name of patched firmware binary to save",
                    Filter = "Bin Files|*.bin"
                };
                if (sfd.ShowDialog() ?? false)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, firmware[..fmLength]);
                        return;
                    }
                    catch { err = $"Unable to write to file: {sfd.FileName}"; }
                }
            }
            if(err.Length>0) MessageBox.Show(err);
        }

        private void FlashStart_Click(object sender, RoutedEventArgs e)
        {
            Context.Instance.FlashError.Value = "Press ESC to abort";
            Context.Instance.UiReady.Value = false;
            flashCancel = new CancellationTokenSource();
            Tasks.Watch = Comms.FlashFirmware(Context.Instance.FlashFile.Value, FlashBar, result =>
            {
                Context.Instance.UiReady.Value = true;
                Context.Instance.FlashError.Value = result;
                flashCancel.Dispose();
            }, flashCancel.Token);
        }

        private void SaveState_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SaveFileDialog sfd = new()
            {
                Title = "Save Radio Config File",
                Filter = "CFG Files|*.cfg|All Files|*.*"
            };
            if (sfd.ShowDialog() ?? false)
            {
                try
                {
                    File.WriteAllBytes(sfd.FileName, Comms.EEPROM);
                    Comms.UnCommit();
                }
                catch { }
            }
        }

        private void LoadState_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Comms.Ready)
            {
                var res = MessageBox.Show("You are currently connected to a radio.\rLoading a saved configuration will overwrite\rthe radio with the configuration you are loading in\r\rAre you sure?", "Confirmation", MessageBoxButton.OKCancel);
                if (res.Equals(MessageBoxResult.Cancel))
                {
                    return;
                }
            }
            OpenFileDialog ofd = new()
            {
                Title = "Load Radio Config File",
                Filter = "CFG Files|*.cfg|All Files|*.*"
            };
            if (ofd.ShowDialog() ?? false)
            {
                try
                {
                    FileInfo info = new(ofd.FileName);
                    if (info.Length == 0x2000)
                    {
                        byte[] b = File.ReadAllBytes(ofd.FileName);
                        Array.Copy(b, Comms.EEPROM, 0x2000);
                    }
                    else
                        return;
                }
                catch { return; }
                Context.Instance.SyncToRadio();
                if (!Context.Instance.FirstRun)
                {
                    Comms.PreCommit(0, 0x2000);
                    TD.Update();
                }
                else
                    Comms.UnCommit();
                Context.Instance.FirstRun = false;
            }
        }

        private void LiveMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(Context.Instance.LiveMode.Value = !Context.Instance.LiveMode.Value)
            {
                TD.Update();
            }
        }

        private void ChannelEditMenu_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem mi && mi.Header is string val)
            {
                if(mi is ToneSubMenu tsm)
                {
                    // && tsm.ClickedItem is IToneMenu itm
                    string co = tsm.ClickedItem is IToneMenu itm ? itm.ClickedOption : "None";
                    foreach (var item in ChannelGrid.SelectedItems)
                    {
                        if (item is Channel channel)
                            channel.SetProperty(val, co);
                    }
                }
                else
                if (mi.Parent is MenuItem pa && pa.Header is string key)
                {
                    foreach (var item in ChannelGrid.SelectedItems)
                    {
                        if (item is Channel channel)
                            channel.SetProperty(key, val);
                        if (key.Equals("Presets"))
                            break;
                    }
                }
                else
                {
                    foreach (var item in ChannelGrid.SelectedItems)
                    {
                        if (item is Channel channel)
                            channel.SetProperty("Action", val);
                    }

                }
            }
        }

        private void FrequencyMenu_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter && double.TryParse(MenuFrequency.Text, out double freq)) 
            {
                foreach (var item in ChannelGrid.SelectedItems)
                {
                    if (item is Channel channel)
                    {
                        channel.RX = freq.ToString();
                        freq += Context.Instance.StepA / 1000.0;
                    }
                }
            }
        }

        private void ReverseALab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.ReverseA.Value = !Context.Instance.ReverseA.Value;
        }

        private void ReverseBLab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Context.Instance.ReverseB.Value = !Context.Instance.ReverseB.Value;
        }

        private void RadioDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                Context.Instance.UiReady.Value = false;
                Comms.SetPort((string)mi.Header, b => 
                {
                    if (b)
                    {
                        Comms.SetPort("Offline", _ => { });
                        Context.Instance.SyncToRadio();
                        RadioRWPBar.Value = 0;
                    }
                    Context.Instance.UiReady.Value = true;
                }, RadioRWPBar);
            }
        }

        private void RadioUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                Context.Instance.UiReady.Value = false;
                Comms.SetPort("!" + (string)mi.Header, async b =>
                {
                    if (b)
                    {
                        Comms.PreCommit(0, 0x2000);
                        Context.Instance.LiveMode.Value = true;
                        await Comms.Commit();
                        RadioRWPBar.Value = 0;
                        Context.Instance.LiveMode.Value = false;
                        Comms.SetPort("Offline", _ => { });
                        Context.Instance.UiReady.Value = true;
                    }
                    else
                        Context.Instance.UiReady.Value = true;
                }, RadioRWPBar);
            }
        }

        private void ScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseLeftButtonDown(null);
        }

        private void CustomStepA_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
                CustomStep_Click(Context.Instance.FreqStepOverrideA, mi);
        }

        private void CustomStepB_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem mi)
                CustomStep_Click(Context.Instance.FreqStepOverrideB, mi);
        }

        private static void CustomStep_Click(ViewModel<double> vm, MenuItem mi)
        {
            if(mi.Header is string s && double.TryParse(s, out double d))            
                vm.Value = d == 8.33 ? 8.33333333333333333333333 : d;
            else
                vm.Value = 0;
        }

        private void ModulationSelect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch(Context.Instance.ModulationOverride.Value)
            {
                case "〜":
                    Context.Instance.ModulationOverride.Value = "AM";
                    Comms.SetModulationOverride(2);
                    break;
                case "AM":
                    Context.Instance.ModulationOverride.Value = "USB";
                    Comms.SetModulationOverride(3);
                    break;
                case "USB":
                    Context.Instance.ModulationOverride.Value = "FM";
                    Comms.SetModulationOverride(4);
                    break;
                case "FM":
                    Context.Instance.ModulationOverride.Value = "〜";
                    Comms.SetModulationOverride(1);
                    break;
            }
            TD.Update();
        }

        private void AnalyzerMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Context.Instance.ChannelModeVis.Value == Visibility.Visible ||
                Context.Instance.PowerModeVis.Value == Visibility.Visible ||
                Context.Instance.FlashModeVis.Value == Visibility.Visible ||
                Context.Instance.TunerModeVis.Value == Visibility.Visible)
                return;
            Context.Instance.AnalyserMode.Value = !Context.Instance.AnalyserMode.Value;
            if (Context.Instance.AnalyserMode.Value)
            {
                Context.Instance.SelectedVfo.Value = false;
            }
            else
            {
                StepsEntry_EntryComplete(sender, e);
                HaltFA();
            }
        }

        private CancellationTokenSource? faCts = null;

        //private byte[] lastExtMem = [];
        
        /*
        private void ExtMemScan()
        {
            byte[]? b = Comms.GetExtMem();
            if (b != null)
            {
                if (lastExtMem.Length > 0)
                {
                    for (int i = 0; i < b.Length; i++)
                    {
                        if (lastExtMem[i] != b[i])
                        {
                            Debug.WriteLine($"{i:X4} - OLD:{lastExtMem[i]:X2} NEW:{b[i]:X2}");
                        }
                    }
                }
                lastExtMem = b;
            }
            else
                Debug.WriteLine("Read Mem Failed.");
        }
        */

        private void SetFreqLabels()
        {
            double rx = RX.Value;
            double step = Step / 1000.0;
            int steps = (int)Context.Instance.AnalyserSteps.Value;
            double offset = step * Math.Abs(steps / 2);
            rx -= offset;
            for (int i = 0; i < steps; i++)
            {
                if(ScanGraph.Children[i] is Grid grid)
                {
                    grid.Tag = rx.ToString("F5");
                    rx += step;
                }
            }
        }
        // ⎇
        private void StartFA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            using (faCts)
            {
                faCts = new CancellationTokenSource();
                SetFreqLabels();
                Comms.Scan(RX.Value, Step, (int)Context.Instance.AnalyserSteps.Value, ScanGraph, faCts.Token);
                //Context.Instance.AnalyserRun.Value = true;
            }
        }

        private void HaltFA()
        {
            try { faCts?.Cancel(); } catch { }
        }

        private void StopFA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HaltFA();
        }

        private void JetScan_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Comms.JetScan(ScanGraph);
        }

        private void OnceFA_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Comms.JetScan(ScanGraph);
            SetFreqLabels();
            Comms.Scan(RX.Value, Step, (int)Context.Instance.AnalyserSteps.Value, ScanGraph, null);
        }

        private DecimalEntry? stepEntry = null;
        private void StepsEntry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StepsEntry.Foreground = Brushes.Yellow;
            stepEntry = StepsEntry;
        }
        private void StepsEntry_EntryComplete(object? sender, EventArgs e)
        {
            StepsEntry.Foreground = Brushes.Green;
            stepEntry = null;
        }
        private void Scanner_BarClicked(object? sender, EventArgs e)
        {
            try { faCts?.Cancel(); } catch { }
            if (sender is Grid grid)
            {
                if (grid.Tag is string s)
                {
                    if (double.TryParse(s, out double d))
                    {
                        TD.Suspend();
                        Context.Instance.VfoChA.Value = 0;
                        TD.Resume();
                        RX.Value = d;
                    }
                }
                else
                if (grid.Tag is Channel chan)
                {
                    TD.Suspend();
                    Context.Instance.VfoChA.Value = 1;
                    TD.Resume();
                    Context.Instance.VfoChNumEnterA.Value = chan.Number;
                }
            }
        }

        private void ShiftMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Comms.ReadExtMem();
            Context.Instance.ShiftMode.Value = !Context.Instance.ShiftMode.Value;
            Comms.SetShiftMode(Context.Instance.ShiftMode.Value);
            VfoRxA.Refresh();
            VfoTxA.Refresh();
            VfoRxB.Refresh();
            VfoTxB.Refresh();
        }

        private void PatchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PatchBox.SelectedIndex = -1;
        }
    }
}