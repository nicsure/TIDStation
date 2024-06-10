using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TIDStation.Firmware
{
    
    public static class Patches
    {
        public static ObservableCollection<Patch> List { get; } = 
        [
            new()
            {
                ID = 0,
                Dependants = [1, 3],
                Header = "240530: COM Handler",
                Hex = ":03A6DE0002EFD0B8\r\n:03B31E0002EFD566\r\n:10EFD000F1DA02A6E1F1DA02B32190047CE0B45246\r\n:10EFE0000A90047FE0B4200490047C22000000001A\r\n:10EFF0000000000000000000000000000000000011\r\n:10F000000000000000000000000000000000000000\r\n:10F0100000000000000000000000000000000000F0\r\n:10F02000000000000000000090047DE4F0A3F0A3C5\r\n:07F030007420F090047C2223\r\n:00000001FF"
            },
            new()
            {
                ID = 1,
                Prerequisites = [0],
                Header = "240530: AM/USB/FM Override (Requires COM Handler)",
                Hex = ":03BD1B007581CE61\r\n:04E6380002F10000EB\r\n:09EFEC00B4100690047DE0F5CD9F\r\n:10F100008F4DBF4706E5CD54037013BF3D0BE5CDD2\r\n:10F110005403B402047D007B008D4E02E63CB40132\r\n:10F12000047D678017B40212754D3D754E007B005B\r\n:10F1300012E63C7D65754D4780027D617B4080D93C\r\n:00000001FF"
            },
            new()
            {
                ID = 2,
                Header = "240530: S-Meter",
                Hex = ":035DE70002F200C5\r\n:10F200009002F5E05401F5F0A3E07FA412EAA9FF13\r\n:0EF2100012EAA9AFF012EAA99002F5025DEA37\r\n:00000001FF"
            },
            new()
            {
                ID = 3,
                Prerequisites = [0],
                Header = "240530: Frequency Analyser (Requires COM Handler)",
                Hex = ":10EFF500B4110512F3008016B4120512F311800E38\r\n:0EF00500B4130512F3228006B4140312F33381\r\n:10F3000090047DE09004F0F090047EE09004F1F031\r\n:10F310002290047DE09004F2F090047EE09004F3EB\r\n:10F32000F02290047DE09004F4F090047EE09004DC\r\n:10F33000F5F0227FB412EAA990047DE0FF12EAA959\r\n:10F34000801B71CF7F6712C8009002F5E054010363\r\n:10F35000F5F0A3E054FE0345F0FF12EAA990047D06\r\n:10F36000E0C39401400DF071997174719971B77196\r\n:10F37000A880CF227F39E8FDE9FB12E63871997F3A\r\n:10F3800038EAFD12E6387F307D007B0012E6387FD8\r\n:10F39000307DBF7BF112E638229004F0E0F8A3E064\r\n:10F3A000F9A3E0FAA3E0FB229004F0E8F0A3E9F06F\r\n:10F3B000A3EAF0A3EBF0229004F4E0FCA3E0FDC389\r\n:10F3C000EB2DFBEA3CFAE93400F9E83400F8227846\r\n:0CF3D00040C374FF00940150FBD8F622EB\r\n:00000001FF"
            }
        ];

        static Patches()
        {
            Patch p;
            foreach (string file in Directory.GetFiles("."))
            {
                if(file.ToLower().EndsWith(".hex"))
                {
                    try
                    {
                        p = new()
                        {
                            Header = $"File: {Path.GetFileName(file)}",
                            Hex = File.ReadAllText(file)
                        };
                        List.Add(p);
                    }
                    catch { }
                }
            }
        }

        public static int Apply(byte[] firmware, int fwLength, out string error)
        {
            foreach (var element in List)
            {
                if (element is Patch patch && patch.Active)
                {
                    foreach (string hexLine in patch.Hex.Split('\n'))
                    {
                        string ihex = hexLine.Replace('\t', ' ').Replace('\r', ' ').Trim();
                        try
                        {
                            if (ihex.StartsWith(':'))
                            {
                                int cnt = Convert.ToInt32(ihex.Substring(1, 2), 16);
                                int addh = Convert.ToInt32(ihex.Substring(3, 2), 16);
                                int addl = Convert.ToInt32(ihex.Substring(5, 2), 16);
                                int add = (addh << 8) | addl;
                                int type = Convert.ToInt32(ihex.Substring(7, 2), 16);
                                if (type != 0) continue;
                                int cs = cnt + addh + addl + type;
                                int i = 0;
                                for (; i < cnt; i++)
                                {
                                    string s = ihex.Substring(9 + (2 * i), 2);
                                    int b = Convert.ToInt32(s, 16);
                                    cs += b;
                                    firmware[add] = (byte)b;
                                    if (add > fwLength - 1) fwLength = add + 1;
                                    add++;
                                }
                                int cs1 = Convert.ToInt32(ihex.Substring(9 + (2 * i), 2), 16);
                                cs &= 0xff;
                                cs = (0x100 - cs) & 0xff;
                                if (cs != cs1) { error= "Bad checksum in iHex"; return 0; }
                            }
                        }
                        catch { error = "Error processing iHex"; return 0; }
                    }
                }
            }
            error = string.Empty;
            return fwLength;
        }
    }

    public class Patch : MenuItem
    {
        public int ID { get; set; } = -1;
        public int[] Prerequisites { get; set; } = [];
        public int[] Dependants { get; set; } = [];
        public string Hex { get; set; } = string.Empty;
        public bool Active { get; private set; } = false;
        public Patch()
        {
            IsCheckable = true;            
        }
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            Active = false;
            foreach (int i in Dependants)
            {
                Patches.List[i].IsChecked = false;
            }
        }
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            Active = true;
            foreach (int i in Prerequisites)
            {
                Patches.List[i].IsChecked = true;
            }
        }
    }
}
