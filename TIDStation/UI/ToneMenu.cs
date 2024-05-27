﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TIDStation.UI
{
    public class ToneMenu : ContextMenu
    {
        public ToneMenu() : base()
        {
            Items.Add(new CtcssMenu());
            Items.Add(new DcsMenu("N", string.Empty));
            Items.Add(new DcsMenu("I", "R"));
            Items.Add("None");
        }
    }

    public class ToneSubMenu : MenuItem
    {
        public object? ClickedItem { get; private set; } = null;

        public void Add(object o)
        {
            MenuItem? mi = o switch
            {
                MenuItem menuItem => menuItem,
                string s => new MenuItem { Header = s },
                _ => null
            };
            if (mi != null)
            {
                Items.Add(mi);
                mi.Click += (s, e) => ClickedItem = mi;
            }
        }

        public ToneSubMenu() : base()
        {
            Add(new CtcssMenu());
            Add(new DcsMenu("N", string.Empty));
            Add(new DcsMenu("I", "R"));
            Add("None");
        }
    }    

    public interface IToneMenu
    {
        string ClickedOption { get; }
    }

    public class DcsMenu : MenuItem, IToneMenu
    {
        public static uint[] Dcs { get; } =
        [
            0x0013, 0x0015, 0x0016, 0x0019, 0x001A, 0x001E, 0x0023, 0x0027,
            0x0029, 0x002B, 0x002C, 0x0035, 0x0039, 0x003A, 0x003B, 0x003C,
            0x004C, 0x004D, 0x004E, 0x0052, 0x0055, 0x0059, 0x005A, 0x005C,
            0x0063, 0x0065, 0x006A, 0x006D, 0x006E, 0x0072, 0x0075, 0x007A,
            0x007C, 0x0085, 0x008A, 0x0093, 0x0095, 0x0096, 0x00A3, 0x00A4,
            0x00A5, 0x00A6, 0x00A9, 0x00AA, 0x00AD, 0x00B1, 0x00B3, 0x00B5,
            0x00B6, 0x00B9, 0x00BC, 0x00C6, 0x00C9, 0x00CD, 0x00D5, 0x00D9,
            0x00DA, 0x00E3, 0x00E6, 0x00E9, 0x00EE, 0x00F4, 0x00F5, 0x00F9,
            0x0109, 0x010A, 0x010B, 0x0113, 0x0119, 0x011A, 0x0125, 0x0126,
            0x012A, 0x012C, 0x012D, 0x0132, 0x0134, 0x0135, 0x0136, 0x0143,
            0x0146, 0x014E, 0x0153, 0x0156, 0x015A, 0x0166, 0x0175, 0x0186,
            0x018A, 0x0194, 0x0197, 0x0199, 0x019A, 0x01AC, 0x01B2, 0x01B4,
            0x01C3, 0x01CA, 0x01D3, 0x01D9, 0x01DA, 0x01DC, 0x01E3, 0x01EC,
        ];

        public string ClickedOption { get; private set; } = "None";

        public DcsMenu(string suffix, string type) : base()
        {
            Grid grid = new();
            for (int i = 0; i < 8; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < 13; i++)
                grid.RowDefinitions.Add(new RowDefinition());
            int cnt = 0;
            for (int y = 0; y < 13; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    string s = $"{Convert.ToString(Dcs[cnt], 8).PadLeft(3, '0')}{suffix}";
                    MenuItem mi = new()
                    {
                        Header = s,
                        Tag = cnt
                    };
                    mi.Click += (sender, e) => ClickedOption = s;
                    Grid.SetRow(mi, y);
                    Grid.SetColumn(mi, x);
                    grid.Children.Add(mi);
                    cnt++;
                }
            }
            Items.Add(grid);
            Header = $"{type}DCS";
        }

    }

    public class CtcssMenu : MenuItem, IToneMenu
    {
        public static uint[] Ctcss { get; } =
        [
             670,  693,  719,  744,  770,  797,  825,  854,  885,  915,
             948,  974, 1000, 1035, 1072, 1109, 1148, 1188, 1230, 1273,
            1318, 1365, 1413, 1462, 1514, 1567, 1598, 1622, 1655, 1679,
            1713, 1738, 1773, 1799, 1835, 1862, 1899, 1928, 1966, 1995,
            2035, 2065, 2107, 2181, 2257, 2291, 2336, 2418, 2503, 2541
        ];

        public string ClickedOption { get; private set; } = "None";

        public CtcssMenu() : base()
        {
            Grid grid = new();
            for (int i = 0; i < 5; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < 10; i++)
                grid.RowDefinitions.Add(new RowDefinition());
            int cnt = 0;
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    string s = $"{Ctcss[cnt]/10.0:F1}";
                    MenuItem mi = new()
                    {
                        Header = s,
                        Tag = cnt
                    };
                    mi.Click += (sender, e) => ClickedOption = s;
                    Grid.SetRow(mi, y);
                    Grid.SetColumn(mi, x);
                    grid.Children.Add(mi);
                    cnt++;
                }
            }
            Items.Add(grid);
            Header = "CTCSS";
        }

    }
}
