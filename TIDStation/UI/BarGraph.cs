using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TIDStation.UI
{
    public class BarGraph : Grid
    {
        private int barCount = 0;



        private void SetColumns(int cc)
        {
            if (barCount != cc)
            {
                barCount = cc;
                Children.Clear();
                ColumnDefinitions.Clear();
                for (int i = 0; i < cc, i++)
                    ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        public int BarCount
        {
            get { return (int)GetValue(BarCountProperty); }
            set { SetValue(BarCountProperty, value); }
        }
        public static readonly DependencyProperty BarCountProperty =
            DependencyProperty.Register("BarCount", typeof(int), typeof(BarGraph), new PropertyMetadata(0, BarCountChanged));

        private static void BarCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is BarGraph bg)
            {
                bg.SetColumns((int)e.NewValue);
            }
        }
    }
}
