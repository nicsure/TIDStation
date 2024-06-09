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
        private static readonly byte[] writePkt = [0x57, 0, 0, 0x20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                      0, 0, 0, 0];
        private static readonly object sync = new(), sync2 = new();
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
            string s = $"{eeprom[addr + 1]&0x3f:X2}{eeprom[addr]:X2}";
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
                        temp.Close();
                        Context.Instance.ComPort.Value = "Offline";
                        return false;
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

        private static void SendEepromUpdate(int start, int end)
        {
            if (port != null)
            {
                for (int j = start; j < end; j += 0x20)
                {
                    if (rwProgress != null)
                    {
                        double pg = (j / (double)end) * 100.0;
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
                if (modOverride < 0)
                {
                    modOverride = -modOverride;
                    port.Send([0x52, (byte)(modOverride - 1), 0, 0x10]);
                }
                else
                    port.Send(endSeq);
            }
        }

        private static int modOverride = 1;
        public static void SetModulationOverride(int mod) => modOverride = -mod;

        private static bool skip = false;
        private static bool UpdatePending => modOverride < 0 || (cstart <= 0x2000 && cend > -1);
        public static async Task Commit()
        {
            if (UpdatePending && port != null && Context.Instance.LiveMode.Value)
            {
                if (skip) return;
                skip = true;
                int start = cstart;
                int end = cend;
                cstart = 0x2001;
                cend = -1;
                using Task task = Task.Run(() =>
                {
                    lock (sync2)
                    {
                        lock (sync)
                        {
                            port.Send(compId);
                            if (!sync.Wait()) return;
                            port.Send(radioIdReq);
                            if (!sync.Wait()) return;
                            port.Send(okayAck);
                            if (!sync.Wait()) return;
                            SendEepromUpdate(start, end);
                            if (!sync.Wait()) return;
                            port.Send(radioIdReq);
                            if (!sync.Wait()) return;
                        }
                    }
                });
                await task;
                skip = false;
            }
        }

        private static int rssi = 0;
        private static void Received(byte[] data)
        {
            foreach (byte b in data)
            {
                switch (state)
                {
                    case 8: // rssi first (least sig) rssi level
                        rssi = b;
                        state = 9;
                        break;
                    case 9: // rssi last (most sig) rssi level
                        rssi |= (b << 8);
                        Context.Instance.Rssi.Value = rssi / 2.0;
                        state = 0;
                        break;
                    case 0: // idle
                        switch (b)
                        {
                            case 0xa4:
                                state = 8;
                                break;
                            case 0x06: // okay, ack?
                                sync.SyncSignal();
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
            using Task<string> task = Task.Run(() =>
            {
                byte[] firmware = new byte[65536];
                string err = ProcessFirmware(file, firmware, out int fmLength);
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
