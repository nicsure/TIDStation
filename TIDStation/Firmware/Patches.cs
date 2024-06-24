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
                Header = "240606: TIDStation Patch 0.37.1b",
                Hex=":0A69010044474A4D505356595C5F5D\r\n:017F7100E926\r\n:017F73007D90\r\n:027C3B008006C1\r\n:027C3200800FC1\r\n:035DE70002F29E27\r\n:036DCA0002EFE0F5\r\n:03A7680012F0A547\r\n:03B3850012F0A51E\r\n:03C06E0012F02DA0\r\n:03C4780012F02D92\r\n:04E6470012F1F600D6\r\n:10EFE0005380F0126DCD9004FEE09004786017F02D\r\n:10EFF000E5EA30E01153EAFE7F0712EAB8E4900434\r\n:10F00000FEF0900478F0E0B413028013B41A02808A\r\n:10F010000E9004FCE06015E4F07FF012EAB8229054\r\n:10F0200004FCE0700704F07FF112EAB82290047D3E\r\n:10F03000E0B4560122D083D082605C146024146056\r\n:10F040003B146001227F9A12EAB8C283C282E0A315\r\n:10F05000C082C083FF12EAB8D083D082E583B405B2\r\n:10F06000ED227F9912EAB890047EE0F5F090047FDB\r\n:10F07000E085F083F582E0FF12EAB82290047EE09A\r\n:10F08000F5F090047FE0F8900480E085F0838882BA\r\n:10F09000F07F0712EAB82290047EE09004FEF07F31\r\n:10F0A0000712EAB82212E91D9004A0E06F04600183\r\n:10F0B00022D083D082900480E0600A14600914603A\r\n:10F0C0000814600722214221582131127CEB7FB5C0\r\n:10F0D00012EAB8900480A3E582B49D0122E0FDA36A\r\n:10F0E000E0FB7F39C083C08212E647D082D083A381\r\n:10F0F000E0FDA3E0FB7F38C083C08212E6477F308B\r\n:10F100007D007B0012E6477F307DBF7BF112E64732\r\n:10F1100078FFC374FF00940150FBD8F67F6512C8D6\r\n:10F120000F9002F6E0547FFF12EAB8D082D08380BD\r\n:10F13000A5A3E05480F5F09004FFE0547F45F0F083\r\n:10F14000800FA3E05403F5F09004FFE054FC45F079\r\n:10F15000F07F0712EAB82222127CEB7FB412EAB8E1\r\n:10F16000900487E0FF12EAB812E647900487E06057\r\n:10F17000E614F0900483E0FDA3E0FB7F3812E6473D\r\n:10F18000900481E0FDA3E0FB7F3912E6477F307DEC\r\n:10F19000007B0012E6477F307DBF7BF112E64778A7\r\n:10F1A00040C374FF00940150FBD8F67F6712C80F6C\r\n:10F1B0009002F5E0540103F5F0A3E054FE0345F09E\r\n:10F1C000FF12EAB8C3900481E0F8A3E0F9A3E0FAE3\r\n:10F1D000A3E0FBA3E0FCA3E0C33BFBEC3AFAE93479\r\n:10F1E00000F9E83400F8900481E8F0A3E9F0A3EA1C\r\n:10F1F000F0A3EBF0216B8F4DBF47028041BF3D0272\r\n:10F20000802EBF3902801DBF7302800280389004B7\r\n:10F21000FFE054036030B40305EB54EF8003EB448C\r\n:10F2200010FB80229004FFE05480601A0D0D8016C0\r\n:10F230009004FFE05403B4020DE4FBFD8008900449\r\n:10F24000FFE0540370038D4E22C0E0B401047D67DB\r\n:10F250008009B402047D6580027D617B408D4E1281\r\n:10F26000E64BD0E0C0E0B402067D007B0080047D68\r\n:10F270002A7BAB7F3D8F4D8D4E12E64B7F7312C8BC\r\n:10F280000F9002F5E0FDA3E0FBD0E0B40305EB54E2\r\n:10F29000EF8003EB4410FB7F738F4D8D4E227FA4D4\r\n:10F2A00012EAB89002F6E0FF12EAB89002F5E054D4\r\n:10F2B00001FF12EAB87F6512C80F9002F6E0547F92\r\n:0FF2C000FF12EAB87F6712C80F9002F5025DEAED\r\n:00000001FF"
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
