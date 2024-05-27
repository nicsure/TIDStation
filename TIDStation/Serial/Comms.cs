using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TIDStation.Data;
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
        private static readonly byte[] eeprom = new byte[8192];
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

        private static int cstart = 0x2001, cend = -1;

        static Comms()
        {
            eeprom[0x1958] = 0xff;
            eeprom[0x1959] = 0xff;
            eeprom[0x195a] = 0xff;
            eeprom[0x195b] = 0xff;
            eeprom[0x1968] = 0xff;
            eeprom[0x1969] = 0xff;
            eeprom[0x196a] = 0xff;
            eeprom[0x196b] = 0xff;
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
            string s = i.ToString($"D8");
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

        public static void SetPort(string portName, Action<bool> finished)
        {
            if (int.TryParse(portName.Replace("COM", string.Empty), out int i))
            {
                Tasks.Watch = SetPort(i, finished);
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
            Context.Instance.FlashComPort.Value = $"COM{portNumber}";
            if (!Context.Instance.FirstRun && (cstart <= 0x2000 || cend > -1))
            {
                var res = MessageBox.Show("Connecting to a radio will overwrite the current configuration\rwith the configuration of the radio you are connecting to\r\rAre you sure?", "Confirmation", MessageBoxButton.OKCancel);
                if (res.Equals(MessageBoxResult.Cancel))
                {
                    finished(false);
                    return;
                }
            }
            using Task<bool> task = Task<bool>.Run(() =>
            {
                lock(sync2) lock (sync)
                {
                    port?.Close();                        
                    port = null;
                    ComPort temp = new(portNumber, 38400, Parity.None, 8, StopBits.One, Received);
                    if (temp.Active)
                    {
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

        private static bool skip = false;
        public static async Task Commit()
        {
            if (cstart <= 0x2000 && cend > -1 && port != null && Context.Instance.LiveMode.Value)
            {
                if (skip) return;
                skip = true;
                int start = cstart;
                int end = cend;
                cstart = 0x2001;
                cend = -1;
                using Task task = Task.Run(() =>
                {
                    lock(sync2) lock (sync)
                    {
                        port.Send(compId);
                        if (!sync.Wait()) return;
                        port.Send(radioIdReq);
                        if (!sync.Wait()) return;
                        port.Send(okayAck);
                        if (!sync.Wait()) return;
                        for (int j = start; j < end; j += 0x20)
                        {
                            j.Write16BE(writePkt, 1);
                            Array.Copy(eeprom, j, writePkt, 4, 0x20);
                            writePkt[0x24] = 0;
                            for (int i = 4; i < 0x24; i++)
                                writePkt[0x24] += writePkt[i];
                            port.Send(writePkt);
                            if (!sync.Wait()) return;
                        }
                        port.Send(endSeq);
                        if (!sync.Wait()) return;
                        port.Send(radioIdReq);
                        if (!sync.Wait()) return;
                    }
                });
                await task;
                skip = false;
            }
        }

        private static void Received(byte[] data)
        {
            foreach (byte b in data)
            {
                switch (state)
                {
                    case 0: // idle
                        switch (b)
                        {
                            case 0x06: // okay, ack?
                                sync.SyncSignal();
                                break;
                            case 0x50: // Radio Id?
                                state = 1;
                                cnt = 0;
                                break;
                            case 0x57: // eeprom data
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

        public static async Task FlashFirmware(string file, ProgressBar bar, Action<string> finished, CancellationToken token)
        {
            using Task<string> task = Task.Run(() =>
            {
                FileInfo? info = null; try { info = new FileInfo(file); } catch { }
                if (info == null) return "File info error";
                if (info.Length > 65535) return "File too big";
                if (info.Length < 10000) return "File too small";
                byte[] firmware; try { firmware = File.ReadAllBytes(file); } catch { return "File open error"; }
                port?.Close(); port = null;
                SerialPort? com = null;
                try
                {
                    com = new(Context.Instance.FlashComPort.Value, 115200, Parity.None, 8, StopBits.One) { ReadTimeout = 500 };
                    com.Open();
                }
                catch { }
                if (com == null) return "Error opening COM";
                int b;
                using (com)
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested) return "Aborted @ discovery";
                        try { b = com.ReadByte(); }
                        catch (TimeoutException) { continue; }
                        catch { b = -1; }
                        if (b == -1) return "Comms error, discovery";
                        if (b == 0xa5) break;
                    }
                    try { com.Write(firmId, 0, firmId.Length); } catch { return "Comms error, handshake"; }
                    while (true)
                    {
                        try
                        {
                            b = com.ReadByte();
                            if (b == -1) return "Handshake eof";
                        }
                        catch (TimeoutException) { break; }
                        catch { return "Comms error hs ack"; }
                    }
                    if (token.IsCancellationRequested) return "Aborted @ handshake";
                    for (int i = 0, j = 0; i < firmware.Length; i += 0x20, j++)
                    {
                        if (token.IsCancellationRequested) return "Aborted @ data xfer";
                        byte[] block = new byte[0x24];
                        Array.Copy(firmware, i, block, 4, i + 0x20 > firmware.Length ? firmware.Length - i : 0x20);
                        byte cs = 0; for (int k = 4; k < 0x24; k++) cs += block[k];
                        block[3] = cs;
                        block[0] = (byte)(i + 0x20 >= firmware.Length ? 0xa2 : 0xa1);
                        block[1] = (byte)((j >> 8) & 0xff);
                        block[2] = (byte)(j & 0xff);
                        try { com.Write(block, 0, block.Length); } catch { return "Data xfer error"; }
                        try { while (com.ReadByte() != 0xa3) { } } catch { return "Data ack timeout"; }
                        double f = ((double)i / firmware.Length) * 100;
                        bar.Dispatcher.Invoke(() => bar.Value = f);
                    }
                }
                return "Complete";
            });
            finished(await task);
        }

        private static readonly byte[] firmId = [ 0xA0, 0xEE, 0x74, 0x71, 0x07, 0x74, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55,
                                                  0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55,
                                                  0x55, 0x55, 0x55, 0x55 ];

    }
}
