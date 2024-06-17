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
        private static int idCnt = 0;
        public static ObservableCollection<Patch> List { get; } = 
        [
            new()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                IsChecked = true,
                ID = idCnt++,
                Header = "240530: TIDStation Patch 0.36b",
                Hex=":027C31008006CB\r\n:027C2800800FCB\r\n:035DE70002F13E88\r\n:036DC00002EFD00F\r\n:03A7590012F014E7\r\n:03B3760012F014BE\r\n:03C05F0012EFF7E6\r\n:03C4690012EFF7D8\r\n:04E6380012F0F600E6\r\n:10EFD0005380F0126DC39004FEE0601A900478F044\r\n:10EFE000E5EA30E01153EAFE7F0712EAA9E4900453\r\n:10EFF000FEF0900478F02290047DE0B4560122D017\r\n:10F0000083D08260012290047EE09004FEF07F07AE\r\n:10F0100012EAA92212E90E9004A0E06F0460012216\r\n:10F02000D083D082900480E0601814602B146001BB\r\n:10F0300022A3E05480F5F09004FFE0547F45F0F007\r\n:10F04000800FA3E05403F5F09004FFE054FC45F07A\r\n:10F05000F07F0712EAA92222127CE17FB412EAA90A\r\n:10F06000900487E0FF12EAA912E638900487E06076\r\n:10F07000E614F0900483E0FDA3E0FB7F3812E6384D\r\n:10F08000900481E0FDA3E0FB7F3912E6387F307DFC\r\n:10F09000007B0012E6387F307DBF7BF112E63878C6\r\n:10F0A00040C374FF00940150FBD8F67F6712C8007C\r\n:10F0B0009002F5E0540103F5F0A3E054FE0345F09F\r\n:10F0C000FF12EAA9C3900481E0F8A3E0F9A3E0FAF3\r\n:10F0D000A3E0FBA3E0FCA3E0C33BFBEC3AFAE9347A\r\n:10F0E00000F9E83400F8900481E8F0A3E9F0A3EA1D\r\n:10F0F000F0A3EBF0016B8F4DBF47028022BF3D02B2\r\n:10F10000800FBF39229004FFE05480601A0D0D80FB\r\n:10F11000169004FFE05403B4020DE4FBFD80089058\r\n:10F1200004FFE0540370038D4E227B40B401047D44\r\n:10F130006780F4B402047D6580ED7D6180E97FA481\r\n:10F1400012EAA99002F6E0FF12EAA99002F5E05453\r\n:10F1500001FF12EAA97F6512C8009002F6E0547F11\r\n:0FF16000FF12EAA97F6712C8009002F5025DEA6C\r\n:00000001FF"
            }
            /*
            new()
            {
                IsChecked = true,
                ID = idCnt++,
                Dependants = [1, 3],
                Header = "240530: COM Handler",
                Hex = ":03A6DE0002EFD0B8\r\n:03B31E0002EFD566\r\n:10EFD000F1DA02A6E1F1DA02B32190047CE0B45246\r\n:10EFE0000A90047FE0B4200490047C22000000001A\r\n:10EFF0000000000000000000000000000000000011\r\n:10F000000000000000000000000000000000000000\r\n:10F0100000000000000000000000000000000000F0\r\n:10F02000000000000000000090047DE4F0A3F0A3C5\r\n:07F030007420F090047C2223\r\n:00000001FF"
            },
            new()
            {
                IsChecked = true,
                ID = idCnt++,
                Prerequisites = [0],
                Header = "240530: AM/USB/FM Override (Requires COM Handler)",
                Hex = ":03BD1B007581CE61\r\n:04E6380002F10000EB\r\n:09EFEC00B4100690047DE0F5CD9F\r\n:10F100008F4DBF4706E5CD54037013BF3D0BE5CDD2\r\n:10F110005403B402047D007B008D4E02E63CB40132\r\n:10F12000047D678025B40212754D3D754E007B004D\r\n:10F1300012E63C7D65754D478010754D3D754E2A34\r\n:0EF140007BAB12E63C7D61754D477B4080CB7A\r\n:00000001FF"
            },
            new()
            {
                IsChecked = true,
                ID = idCnt++,
                Header = "240530: S-Meter",
                Hex = ":035DE70002F200C5\r\n:10F200007FA412EAA99002F6E0FF12EAA99002F5A3\r\n:10F21000E05401FF12EAA97F6512C8009002F6E0EF\r\n:10F22000547FFF12EAA97F6712C8009002F5025DC1\r\n:01F23000EAF3\r\n:00000001FF"
            },
            new()
            {
                IsChecked = true,
                ID = idCnt++,
                Prerequisites = [0],
                Header = "240530: Spectrum Scope (Requires COM Handler)",
                Hex = ":10EFF500B4110512F3008016B4120512F311800E38\r\n:0EF00500B4130512F3228006B4140312F33381\r\n:10F3000090047DE09004F0F090047EE09004F1F031\r\n:10F310002290047DE09004F2F090047EE09004F3EB\r\n:10F32000F02290047DE09004F4F090047EE09004DC\r\n:10F33000F5F0227FB412EAA990047DE0FF12EAA959\r\n:10F34000719B801B71F27F6712C8009002F5E05438\r\n:10F350000103F5F0A3E054FE0345F0FF12EAA99083\r\n:10F36000047DE0C39401400DF071BC717671BC71F5\r\n:10F37000DA71CB80CF227F39E8FDE9FB12E63871E4\r\n:10F38000BC7F38EAFD12E6387F307D007B0012E654\r\n:10F39000387F307DBF7BF112E638229004F0E09098\r\n:10F3A00004F6F09004F1E09004F7F09004F2E0909D\r\n:10F3B00004F8F09004F3E09004F9F0229004F6E0F1\r\n:10F3C000F8A3E0F9A3E0FAA3E0FB229004F6E8F04A\r\n:10F3D000A3E9F0A3EAF0A3EBF0229004F4E0FCA38D\r\n:10F3E000E0FDC3EB2DFBEA3CFAE93400F9E8340018\r\n:0FF3F000F8227840C374FF00940150FBD8F62236\r\n:00000001FF"
            }
            */
        ];

        static Patches()
        {
            Patch p;            
            foreach (string file in Directory.GetFiles(@"..\..\..\.."))
            {
                if(file.ToLower().EndsWith(".hex"))
                {
                    try
                    {
                        p = new()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            ID = idCnt++,
                            IsChecked = false,
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
        public bool Active => IsChecked;
        public Patch()
        {
            IsCheckable = true;            
        }
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            foreach (int i in Dependants)
            {
                Patches.List[i].IsChecked = false;
            }
        }
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            foreach (int i in Prerequisites)
            {
                Patches.List[i].IsChecked = true;
            }
        }
    }
}
