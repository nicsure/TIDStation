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
                Header = "AM/USB/FM Override: 240530",
                Hex = ":03A6DE0002EFD0B8\r\n:03B31E0002EFD566\r\n:03BD1B007581CE61\r\n:04E6380002EFF900F4\r\n:10EFD000F1DA02A6E1F1DA02B32190047CE0B45246\r\n:10EFE0001790047FE0B4100D90047DE0F5CD7445DA\r\n:10EFF00090047CF02290047C228F4DBF4706E5CD23\r\n:10F0000054037013BF3D0BE5CD5403B402047D00DF\r\n:10F010007B008D4E02E63CB401047D678017B4028C\r\n:10F0200012754D3D754E007B0012E63C7D65754DB9\r\n:09F030004780027D617B4080D91C\r\n:00000001FF"
            },
            new()
            {
                Header = "S-Meter: 240530",
                Hex = ":035DE70002F04087\r\n:10F040009002F5E05401F5F0A3E07FA412EAA9FFD5\r\n:0EF0500012EAA9AFF012EAA99002F5025DEAF9\r\n:00000001FF"
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
        }
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            Active = true;
        }
    }
}
