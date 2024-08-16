using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TIDStation.Data;
using TIDStation.Firmware;
using TIDStation.General;
using TIDStation.Radio;
using TIDStation.UI;

namespace TIDStation.Serial
{
    public static class Comms
    {
        private static ComPort? port = null;
        private static int state = 0;
        private static int cnt = 0;
        private static int len = 0;
        private static int add = 0;
        private static byte checkSum = 0;
        private static bool checkSumOK;
        private static readonly byte[] radioId = new byte[7];
        private static readonly byte[] eeprom;
        private static readonly byte[] compId = [0x50, 0x56, 0x4F, 0x4A, 0x48, 0x5C, 0x14];
        private static readonly byte[] radioIdReq = [0x02];
        private static readonly byte[] endSeq = [0x45];
        private static readonly byte[] okayAck = [0x06];
        private static readonly byte[] readReq = [0x52, 0x00, 0x00, 0x20];
        private static readonly byte[] writePkt = [0x57, 0, 0, 0x20,
                                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                   0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                   0];
        private static readonly object sync = new(), sync2 = new(), sync3 = new();
        public static bool Ready => port != null;
        public static byte[] EEPROM => eeprom;
        public static byte[] BlankEEPROM { get; } = new byte[8192];

        private static int cstart = 0x2001, cend = -1;

        static Comms()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("TIDStation.Resources.BLANK.BIN") ?? throw new Exception($"Resource BLANK.BIN not found.");
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            eeprom = memoryStream.ToArray();
            Array.Copy(eeprom, 0, BlankEEPROM, 0, 8192);
        }

        public static int GetDcsAt(int addr)
        {
            if (eeprom[addr + 1] < 0x80) return 10000 + GetBcdAt(addr, 2);
            string s = $"{eeprom[addr + 1] & 0x3f:X2}{eeprom[addr]:X2}";
            if (int.TryParse(s, out int i))
                return eeprom[addr + 1] >= 0xc0 ? -i : i;
            else
                return -1;
        }

        public static void SetDcsAt(int addr, int dcs)
        {
            if (dcs == -1)
                eeprom[addr] = eeprom[addr + 1] = 0xff;
            else
            {
                if (dcs > 10000)
                    SetBcdAt(addr, dcs - 10000);
                else
                {
                    byte pre = (byte)(dcs < 0 ? 0xc0 : 0x80);
                    string s = Math.Abs(dcs).ToString($"D4");
                    for (int j = 2, k = 0; j >= 0; j -= 2, k++)
                        EEPROM[addr + k] = Convert.ToByte(s.Substring(j, 2), 16);
                    EEPROM[addr + 1] |= pre;
                }
            }
            PreCommit(addr, 2);
        }

        public static int GetBcdAt(int addr, int count)
        {
            StringBuilder s = new();
            for (int i = count - 1; i >= 0; i--)
                s.Append($"{eeprom[addr + i]:X2}");
            return int.TryParse(s.ToString(), out int j) ? j : -1;
        }

        public static void SetBcdAt(int addr, int i, int count)
        {
            string s = i.ToString($"D{count * 2}");
            for (int j = (count - 1) * 2, k = 0; j >= 0; j -= 2, k++)
                EEPROM[addr + k] = Convert.ToByte(s.Substring(j, 2), 16);
            PreCommit(addr, count);
        }

        public static int GetBcdAt(int addr)
        {
            return int.TryParse($"{eeprom[addr + 3]:X2}{eeprom[addr + 2]:X2}{eeprom[addr + 1]:X2}{eeprom[addr]:X2}", out int i) ? i : 0;
        }

        public static void SetBcdAt(int addr, int i)
        {
            string s = i.ToString("D8");
            for (int j = 6, k = 0; j >= 0; j -= 2, k++)
                EEPROM[addr + k] = Convert.ToByte(s.Substring(j, 2), 16);
            PreCommit(addr, 4);
        }

        public static int GetBcdrAt(int addr, int count)
        {
            StringBuilder s = new();
            for (int i = 0; i < count; i++)
                s.Append($"{eeprom[addr + i]:X2}");
            return int.TryParse(s.ToString(), out int j) ? j : -1;
        }

        public static void SetBcdrAt(int addr, int i, int count)
        {
            string s = i.ToString($"D{count * 2}");
            for (int j = 0, k = 0; k < count; j += 2, k++)
                EEPROM[addr + k] = Convert.ToByte(s.Substring(j, 2), 16);
            PreCommit(addr, count);
        }


        private static ProgressBar? rwProgress = null;

        public static void SetPort(string portName, Action<bool> finished, ProgressBar? pb = null)
        {
            rwProgress = pb;
            int pos = 1;
            if (portName.StartsWith('!'))
            {
                portName = portName[1..];
                pos = -1;
            }
            if (int.TryParse(portName.Replace("COM", string.Empty), out int i))
            {
                Tasks.Watch = SetPort(i * pos, finished);
            }
            else
            {
                port?.Close();
                port = null;
                finished(false);
            }
        }

        public static async Task SetPort(int portNumber, Action<bool> finished)
        {
            Context.Instance.FlashComPort.Value = $"COM{Math.Abs(portNumber)}";
            using Task<bool> task = Task<bool>.Run(() =>
            {
                lock (sync2)
                {
                    lock (sync)
                    {
                        port?.Close();
                        port = null;
                        ComPort temp = new(Math.Abs(portNumber), 38400, Parity.None, 8, StopBits.One, Received);
                        if (temp.Active)
                        {
                            if (portNumber < 0)
                            {
                                port = temp;
                                return true;
                            }
                            for (; true;)
                            {
                                checkSumOK = true;
                                temp.Send(compId);
                                if (!sync.Wait()) break;
                                temp.Send(radioIdReq);
                                if (!sync.Wait()) break;
                                temp.Send(okayAck);
                                if (!sync.Wait()) break;
                                for (int addr = 0; addr < 0x2000; addr += 0x20)
                                {
                                    if (rwProgress != null)
                                    {
                                        double pg = (addr / 8912.0) * 100.0;
                                        rwProgress.Dispatcher.Invoke(() => rwProgress.Value = pg);
                                    }
                                    addr.Write16BE(readReq, 1);
                                    temp.Send(readReq);
                                    if (!sync.Wait()) break;
                                }
                                temp.Send(endSeq);
                                if (!sync.Wait()) break;
                                temp.Send(radioIdReq);
                                if (!sync.Wait()) break;
                                if (!checkSumOK) break;
                                cstart = 0x2001;
                                cend = -1;
                                port = temp;
                                Context.Instance.FirstRun = false;
                                return true;
                            }
                        }
                        port = temp;
                        return true;
                        //temp.Close();
                        //Context.Instance.ComPort.Value = "Offline";
                        //return false;
                    }
                }
            });
            finished(await task);
        }

        public static void PreCommit(int addr, int length)
        {
            int e = addr + length;
            int s = addr & 0x7fffffe0;
            if (s < cstart)
                cstart = s;
            if (e > cend)
                cend = e;
        }

        public static void UnCommit()
        {
            cstart = 0x2001;
            cend = -1;
        }

        public static void Write(int addr, byte b)
        {
            eeprom[addr] = b;
            PreCommit(addr, 1);
        }

        public static void Write(int addr, byte[] data)
        {
            Array.Copy(data, 0, eeprom, addr, data.Length);
            PreCommit(addr, data.Length);
        }

        private static void SendEepromUpdate()
        {
            if (port != null)
            {
                for (int j = cstart; j < cend; j += 0x20)
                {
                    if (rwProgress != null)
                    {
                        double pg = (j / (double)cend) * 100.0;
                        rwProgress.Dispatcher.Invoke(() => rwProgress.Value = pg);
                    }
                    j.Write16BE(writePkt, 1);
                    Array.Copy(eeprom, j, writePkt, 4, 0x20);
                    writePkt[0x24] = 0;
                    for (int i = 4; i < 0x24; i++)
                        writePkt[0x24] += writePkt[i];
                    port.Send(writePkt);
                    if (!sync.Wait()) return;
                }
            }
            cstart = 0x2001;
            cend = -1;
        }

        private static byte[] CustomPkt(params byte[] data)
        {
            byte[] cpkt = new byte[0x25];
            cpkt[0] = 0x57;
            Array.Copy(data, 0, cpkt, 4, data.Length);
            for (int i = 4; i < 0x24; i++)
                cpkt[0x24] += cpkt[i];
            cpkt[0x24] ^= 0xff;
            //Debug.WriteLine($"CS:{cpkt[0x24]}");
            return cpkt;
        }

        public static void SetModulationOverride(int mod)
        {
            if (!LiveMode) return;
            modulationOverride = mod;
            changeModulation = true;
            TD.Update();
        }

        public static void SetShiftMode(bool on)
        {
            if (!LiveMode) return;
            shiftMode = on ? 0x80 : 0;
            changeShiftMode = true;
            TD.Update();
        }

        public static void SimulateKey(int key)
        {
            if (!LiveMode) return;
            if (keyCode != key)
            {
                keyCode = key;
                pressKey = true;
                TD.Update();
            }
        }

        private static bool UpdatePending => jetScan || changeShiftMode || startScan || changeModulation || (cstart <= 0x2000 && cend > -1);
        private static BarGraph? barGraph = null;
        private static byte scFrq0, scFrq1, scFrq2, scFrq3, scStp0, scStp1, scSteps;
        private static CancellationToken? scToken = null;
        public static bool LiveMode => Context.Instance.LiveMode.Value && port != null;

        public static void Scan(double midFreq, double step, int steps, BarGraph bg, CancellationToken? token)
        {
            if (!LiveMode) return;
            barGraph = bg;
            double offset = (step / 1000.0) * Math.Abs(steps / 2);
            midFreq -= offset;
            int sf = (int)Math.Round(midFreq * 100000.0);
            int st = (int)Math.Round(step * 100.0);
            scFrq3 = (byte)(sf >> 24);
            scFrq2 = (byte)((sf >> 16) & 0xff);
            scFrq1 = (byte)((sf >> 8) & 0xff);
            scFrq0 = (byte)(sf & 0xff);
            scStp1 = (byte)((st >> 8) & 0xff);
            scStp0 = (byte)(st & 0xff);
            scSteps = (byte)steps;
            scToken = token;
            startScan = true;
            TD.Update();
        }

        public static void JetScan(BarGraph bg)
        {
            barGraph = bg;
            jetScan = true;
            TD.Update();
        }

        private static bool jetScan = false, readExtMem = false, changeModulation = false, startScan = false, changeShiftMode = false, pressKey = false;
        private static int modulationOverride = 0, shiftMode = 0, keyCode = 0, extMemAddr = -1;
        public static bool ShiftMode => shiftMode != 0;

        private static int commitCount = 0;

        private static readonly byte[] extMem = new byte[0x500];
        //private static int extMemCnt = 0;
        public static byte[] ExtMem
        {
            get
            {
                byte[] b = new byte[0x500];
                Array.Copy(extMem, 0, b, 0, 0x500);
                return b;
            }
        }

        public static void ReadExtMem(int addr = -1)
        {
            extMemAddr = addr;
            readExtMem = true;
            TD.Update();
        }

        private static byte[]? prevExtMem = null;
        public static async Task Commit()
        {
            lock(sync3)
            {
                if (commitCount > 1) return;
                commitCount++;
            }
            using Task task = new(() =>
            {
                lock (sync2)
                {
                    //ignoreUnprompted = true;
                    bool okay = false;
                    for (bool once = true; once; once = false)
                    {
                        if (readExtMem)
                        {
                            readExtMem = false;
                            if (LiveMode)
                            {
                                lock (sync)
                                {
                                    if (extMemAddr > 0)
                                    {
                                        Debug.WriteLine($"Reading {extMemAddr:X4}");
                                        byte hi = (byte)((extMemAddr >> 8) & 0xff);
                                        byte lo = (byte)(extMemAddr & 0xff);
                                        port!.Send([0x50, 0x01, hi, lo, 0x00, 0x00, 0x00]);
                                        if (!sync.Wait()) break;
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"Reading All Extmem");
                                        port!.Send([0x50, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00]);
                                        if (!sync.Wait()) break;
                                        byte[] now = ExtMem;
                                        if (prevExtMem != null)
                                        {
                                            for (int i = 0; i < 0x500; i++)
                                            {
                                                if (now[i] != prevExtMem[i])
                                                {
                                                    Debug.WriteLine($"Addr:{i:X4} was:{prevExtMem[i]:X2} now:{now[i]:X2}");
                                                }
                                            }
                                        }
                                        prevExtMem = now;
                                    }
                                }
                            }
                        }
                        if (pressKey)
                        {
                            pressKey = false;
                            if (LiveMode)
                            {
                                lock (sync)
                                {
                                    if (keyCode != 0)
                                        port!.Send([0x50, 0x00, (byte)keyCode, 0x00, 0x00, 0x00, 0x00]);
                                    else
                                        port!.Send(0);
                                    if (!sync.Wait()) break;
                                }
                            }
                        }
                        if (!UpdatePending || !LiveMode)
                        {
                            changeModulation = false;
                            startScan = false;
                            changeShiftMode = false;
                            jetScan = false;
                            return;
                        }
                        lock (sync)
                        {
                            port!.Send(compId);
                            if (!sync.Wait()) break;
                            port!.Send(radioIdReq);
                            if (!sync.Wait()) break;
                            port!.Send(okayAck);
                            if (!sync.Wait()) break;
                            SendEepromUpdate();
                            if (changeModulation)
                            {
                                port!.Send(CustomPkt(0, (byte)(modulationOverride - 1)));
                                if (!sync.Wait()) break;
                                changeModulation = false;
                            }
                            if(jetScan)
                            {
                                jetScan = false;
                                Context.Instance.AnalyserRun.Value = true;
                                List<Channel> active = [];
                                foreach(var ch in Channel.Mem)
                                {
                                    if (ch.RX.Length == 0) continue;
                                    active.Add(ch);
                                }
                                for (int i = 0; i < active.Count; i += 7)
                                {
                                    stepCount = i;
                                    byte[] pckFr = Enumerable.Repeat((byte)0x03, 29).ToArray();
                                    for (int j = 0; j < 7; j++)
                                    {
                                        int chi = i + j;
                                        if (chi < active.Count)
                                        {
                                            double fr = double.TryParse(active[chi].RX, out double d) ? d : 996.0;
                                            int fri = (int)Math.Round(fr * 100000.0);
                                            int bi = (j * 4) + 1;
                                            pckFr[bi] = (byte)(fri >> 24);
                                            pckFr[bi + 1] = (byte)((fri >> 16) & 0xff);
                                            pckFr[bi + 2] = (byte)((fri >> 8) & 0xff);
                                            pckFr[bi + 3] = (byte)(fri & 0xff);
                                        }
                                    }
                                    port!.Send(CustomPkt(pckFr));
                                    if (!sync.Wait())
                                    {
                                        Context.Instance.AnalyserRun.Value = false;
                                        break;
                                    }
                                }
                                List<(Channel, int)> scanResult = [];
                                for (int i = 0; i < active.Count; i++)
                                {
                                    if (freqScans[i] < 0x30)
                                        scanResult.Add((active[i], freqScans[i]));
                                }
                                barGraph?.Dispatcher.Invoke(() => barGraph.SetJetScanValues(scanResult));
                                Context.Instance.AnalyserRun.Value = false;
                            }
                            if (startScan)
                            {
                                startScan = false;
                                Context.Instance.AnalyserRun.Value = true;
                                do
                                {
                                    port!.Send(CustomPkt(0x01, scFrq3, scFrq2, scFrq1, scFrq0, scStp1, scStp0, scSteps));
                                    if (!sync.Wait(2000))
                                    {
                                        continue;
                                    }
                                }
                                while (scToken != null && !scToken.Value.IsCancellationRequested);
                                Context.Instance.AnalyserRun.Value = false;
                            }
                            if (changeShiftMode)
                            {
                                port!.Send(CustomPkt(0x02, (byte)shiftMode));
                                if (!sync.Wait()) break;
                                changeShiftMode = false;
                            }
                            port!.Send(endSeq);
                            if (!sync.Wait()) break;
                            port!.Send(radioIdReq);
                            if (!sync.Wait()) break;
                        }
                        okay = true;
                        Thread.Sleep(250);
                    }
                    /*
                    if(!okay && LiveMode)
                    {
                        Debug.WriteLine("Desync");
                        lock (sync)
                        {
                            int to = 0;
                            bool sok;
                            do
                            {
                                port!.Send([0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
                            }
                            while (!(sok = sync.Wait(150)) && to++ < 10);
                            Debug.WriteLine(sok ? "Resynced" : "Unable to Resync");
                        }
                    }
                    */
                    //ignoreUnprompted = false;
                }
            });
            task.Start();
            await task;
            lock (sync3)
            {
                commitCount--;
            }
        }

        private static int rssi = 0, stepCount, stepCounter;
        private static readonly int[] freqScans = new int[256];
        private static long lastRssi = -1;

        public static async Task RssiWatcher()
        {
            while(true)
            {
                using Task task = Task.Delay(301);
                await task;
                if (lastRssi > -1)
                {
                    long now = DateTime.Now.Ticks;
                    long rssiTime = now - lastRssi;
                    if (rssiTime > 6000000)
                    {
                        lastRssi = -1;
                        Context.Instance.Rssi.Value = 30;
                        if (Context.Instance.State.Value != 2)
                            Context.Instance.State.Value = 0;
                    }
                }
            }
        }

        private static readonly int[] mbytes = new int[3];
        private static int byte3 = 0;
        private static int extMemCnt = 0;
        private static int lastdebug = 0;
        private static void Received(byte[] data)
        {
            foreach (byte b in data)
            {
                switch (state)
                {
                    case 91: // 3 byte debug
                        mbytes[byte3] = b;
                        if (++byte3 >= 3)
                        {
                            Debug.WriteLine($"0x{mbytes[0]:X2}, 0x{mbytes[1]:X2}, 0x{mbytes[2]:X2},");
                            state = 0;
                        }
                        break;
                    case 100: // ext mem data
                        extMem[extMemCnt++] = b;
                        if (extMemCnt >= 0x500)
                        {
                            Debug.WriteLine("Read All Ext Mem");
                            sync.SyncSignal();
                            state = 0;
                        }
                        break;
                    case 99: // radio debug
                        if (b != lastdebug)
                        {
                            Debug.WriteLine($"Radio Debug Byte: {b:X2}");
                            lastdebug = b;
                        }
                        state = 0;
                        break;
                    case 13: // jetscan
                        freqScans[stepCount + stepCounter++] = b;
                        if (stepCounter >= 7)
                        {
                            state = 0;
                            sync.SyncSignal();
                        }
                        break;
                    case 12: // freq scans
                        freqScans[stepCounter++] = b;
                        if (stepCounter >= stepCount)
                        {
                            state = 0;
                            sync.SyncSignal();
                            barGraph?.Dispatcher.Invoke(() => barGraph.SetValues(stepCount, freqScans));
                        }
                        break;
                    case 11: // freq scan step count
                        stepCount = b;
                        stepCounter = 0;
                        state = 12;
                        break;
                    case 8: // rssi first (least sig) rssi level
                        rssi = b;
                        state = 9;
                        break;
                    case 9: // rssi last (most sig) rssi level
                        rssi |= (b << 8);
                        state = 10;
                        break;
                    case 10: // rssi noise level
                        //Debug.WriteLine($"RSSI:{rssi}  Noise:{b}");
                        Context.Instance.Rssi.Value = (rssi - b).Clamp(30, 270);
                        lastRssi = DateTime.Now.Ticks;
                        Context.Instance.State.Value = 1;
                        state = 0;
                        break;
                    case 0: // idle
                        switch (b)
                        {
                            case 0xF0:
                                Context.Instance.State.Value = 0;
                                break;
                            case 0xF1:
                                Context.Instance.State.Value = 2;
                                break;
                            case 0xb4:
                                state = 11;
                                break;
                            case 0xb5:
                                stepCounter = 0;
                                state = 13;
                                break;
                            case 0xa4:
                                state = 8;
                                break;
                            case 0x06: // okay, ack?
                                sync.SyncSignal();
                                break;
                            case 0x07: // okay, ack? custom
                                //Debug.WriteLine("Custom Ack");
                                sync.SyncSignal();
                                break;
                            case 0x91: // radio debug 3 byte
                                state = 91;
                                byte3 = 0;
                                break;
                            case 0x99: // radio debug
                                state = 99;
                                break;
                            case 0x9a: // read all ext mem
                                state = 100;
                                extMemCnt = 0;
                                break;
                            case 0x50: // Radio Id?
                                state = 1;
                                cnt = 0;
                                break;
                            case 0x57: // eeprom data // temp change
                                state = 2;
                                cnt = 0;
                                add = 0;
                                len = 0;
                                checkSum = 0;
                                break;
                            default:
                                break;
                        }
                        break;
                    case 1: // Radio Id?
                        radioId[cnt++] = b;
                        if (cnt >= 7)
                        {
                            state = 0; // back to idle
                            sync.SyncSignal();
                        }
                        break;
                    case 2: // eeprom data length msB
                        add |= (b << 8);
                        state = 3; // data address lsB
                        break;
                    case 3: // eeprom data length lsB
                        add |= b;
                        state = 4; // data length
                        break;
                    case 4: // eeprom data read length
                        len = b;
                        state = 5; // eeprom data
                        break;
                    case 5: // eeprom data                        
                        eeprom[(add + cnt) & 0x1fff] = b;
                        checkSum += b;
                        if (++cnt >= len)
                            state = 6; // check byte
                        break;
                    case 6: // eeprom read check byte
                        if (checkSum != b)
                            checkSumOK = false;
                        state = 0; // back to idle
                        sync.SyncSignal();
                        break;
                }
            }
        }

        public static string ProcessFirmware(string file, byte[] firmware, out int fmLength)
        {
            fmLength = 0;
            FileInfo? info; try { info = new FileInfo(file); } catch { info = null; }
            if (info == null) return "File info err";
            if (info.Length > 65536) return "File too big";
            if (info.Length < 10000) return "File too small";
            byte[] tb;
            try { tb = File.ReadAllBytes(file); } catch { return "File open err"; }
            Array.Copy(tb, 0, firmware, 0, tb.Length);
            fmLength = Patches.Apply(firmware, tb.Length, out string err);
            if (fmLength == 0) return err;
            return string.Empty;
        }

        public static async Task FlashFirmware(string file, ProgressBar bar, Action<string> finished, CancellationToken token)
        {
            byte[] firmware = new byte[65536];
            string err = ProcessFirmware(file, firmware, out int fmLength);
            using Task<string> task = Task.Run(() =>
            {
                if (fmLength == 0) return err;
                int b;                
                port?.Close(); port = null;
                SerialPort? com = null;
                try
                {
                    com = new(Context.Instance.FlashComPort.Value, 115200, Parity.None, 8, StopBits.One) { ReadTimeout = 500 };
                    com.Open();
                }
                catch { }
                if (com == null) return $"Err open {Context.Instance.FlashComPort.Value}";                
                using (com)
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested) return "Aborted Disc";
                        try { b = com.ReadByte(); }
                        catch (TimeoutException) { continue; }
                        catch { b = -1; }
                        if (b == -1) return "Error Disc";
                        if (b == 0xa5) break;
                    }
                    firmId[3] = 0; for (int k = 4; k < 0x24; k++) firmId[3] += firmId[k];
                    try { com.Write(firmId, 0, firmId.Length); } catch { return "Error HS"; }
                    while (true)
                    {
                        try
                        {
                            b = com.ReadByte();
                            if (b == -1) return "HS eof";
                            if (b != 0xa5) return "Unexpect HS";
                        }
                        catch (TimeoutException) { break; }
                        catch { return "Err HS ack"; }
                    }
                    if (token.IsCancellationRequested) return "Aborted HS";
                    for (int i = 0x00, j = 0; i < fmLength; i += 0x20, j++)
                    {
                        if (token.IsCancellationRequested) return "Aborted xfer";
                        byte[] block = new byte[0x24];
                        Array.Copy(firmware, i, block, 4, 0x20);
                        for (int k = 4; k < 0x24; k++) block[3] += block[k];
                        block[0] = (byte)(i + 0x20 >= fmLength ? 0xa2 : 0xa1);
                        block[1] = (byte)((j >> 8) & 0xff);
                        block[2] = (byte)(j & 0xff);
                        try { com.Write(block, 0, block.Length); } catch { return $"xfer err, blk:{j} adr:{i:X4}"; }
                        try { while (com.ReadByte() != 0xa3) { } } catch { return $"ack TO blk:{j} adr:{i:X4}"; }
                        double f = ((double)i / fmLength) * 100;
                        bar.Dispatcher.Invoke(() => bar.Value = f);
                    }
                }
                return "Complete";
            });
            finished(await task);
        }

        private static readonly byte[] firmId = [ 0xA0, 0xee, 0x74, 0x00, 0x07, 0x74, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55,
                                                  0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55,
                                                  0x55, 0x55, 0x55, 0x55 ];

    }
}
