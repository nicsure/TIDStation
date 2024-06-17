using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TIDStation.Data;
using TIDStation.General;

namespace TIDStation.UI
{
    public class BarGraph : Grid
    {
        private int barCount = 2;
        public EventHandler? BarClicked = null;

        public BarGraph() : base() 
        {
            MouseMove += Mouse_Moved;
            MouseLeftButtonDown += BarGraph_MouseLeftButtonDown;
            MouseLeave += BarGraph_MouseLeave;
        }

        private void BarGraph_MouseLeave(object sender, MouseEventArgs e)
        {
            Context.Instance.AnalyserFLabel.Value = string.Empty;
        }

        private void BarGraph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GetMouseOverGrid() is Grid grid)
            {
                (_ = BarClicked)?.Invoke(grid, EventArgs.Empty);
            }
        }

        private Grid? GetMouseOverGrid()
        {
            if (Mouse.DirectlyOver is Border border)
            {
                if (border.TemplatedParent is Frame frame)
                {
                    if (frame.Parent is Grid grid)
                    {
                        return grid;
                    }
                }
            }
            return null;
        }

        private void Mouse_Moved(object sender, MouseEventArgs e)
        {
            if(GetMouseOverGrid() is Grid grid)
            {
                Context.Instance.AnalyserFLabel.Value = grid.Tag?.ToString() ?? string.Empty;
            }
        }

        private void SetColumns(int cc)
        {
            if (barCount != cc)
            {
                barCount = cc;
                Children.Clear();
                ColumnDefinitions.Clear();
                for (int i = 0; i < cc; i++)
                {
                    ColumnDefinitions.Add(new ColumnDefinition());
                    Grid bar = new();
                    bar.RowDefinitions.Add(new RowDefinition() { Height = new(1, GridUnitType.Star) });
                    bar.RowDefinitions.Add(new RowDefinition() { Height = new(0, GridUnitType.Star) });
                    Frame fr = new() { Background = Brushes.Cyan, BorderThickness = new(0.5), BorderBrush = Brushes.Blue };
                    bar.Children.Add(fr);
                    SetRow(fr, 1);
                    fr = new() { Background = Brushes.Black };
                    bar.Children.Add(fr);
                    SetRow(fr, 0);
                    SetColumn(bar, i);
                    Children.Add(bar);
                }
            }
        }

        public void SetValues(int count, int[] values)
        {
            int hi = -10000, lo = 10000;
            for (int i = 0; i < count && i < barCount; i++)
            {
                int v = values[i];
                if (v > hi) hi = v;
                if (v < lo) lo = v;
            }
            int dif = hi - lo;
            for (int i = 0; i < count && i < barCount; i++)
            {
                int a, b;
                a = values[i] - lo;
                b = dif - a;
                if (Children[i] is Grid bar)
                {
                    bar.RowDefinitions[0].Height = new(b, GridUnitType.Star);
                    bar.RowDefinitions[1].Height = new(a, GridUnitType.Star);
                    if (values[i] == hi && bar.Tag is string frq)
                    {
                        Context.Instance.AnalyserHLabel.Value = frq;
                    }
                }
            }
        }

        public double BarCount
        {
            get { return (double)GetValue(BarCountProperty); }
            set { SetValue(BarCountProperty, value); }
        }
        public static readonly DependencyProperty BarCountProperty =
            DependencyProperty.Register("BarCount", typeof(double), typeof(BarGraph), new PropertyMetadata(2.0, BarCountChanged));

        private static void BarCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is BarGraph bg)
            {
                bg.SetColumns((int)(double)e.NewValue);
            }
        }
    }
}
