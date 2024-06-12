using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Text;
using System.Windows.Controls;
using System.Windows.Input;
using TIDStation.General;
using TIDStation.View;

namespace TIDStation.UI
{
    public class DecimalEntry : Label
    {
        public EventHandler? EntryComplete = null;
        private string Text 
        { 
            get => ((string)Content).Trim();
            set => Content = value;
        }
        private bool inputMode = false;
        private long lastInput;
        private CancellationTokenSource? cts = null;

        public DecimalEntry() : base()
        {
            HorizontalContentAlignment = HorizontalAlignment.Right;
        }

        public int MaxTextLength => WholeDigits + DecimalPlaces + DecimalPlaces.Sign();

        public void KeyIn(Key k)
        {
            lastInput = DateTime.Now.Ticks;
            if (k >= Key.NumPad0 && k <= Key.NumPad9)
                k -= 40;
            if (!inputMode)
            {
                if (k >= Key.D0 && k <= Key.D9)
                {
                    inputMode = true;
                    Text = string.Empty;
                    KeyIn(k);
                    using (cts)
                        cts = new();
                    Tasks.Watch = TimeOutTicker(cts.Token);
                }
            }
            else
            {
                switch (k)
                {
                    case Key.Escape:
                        Refresh(this, Value);
                        break;
                    case >= Key.D0 and <= Key.D9:
                        Text += (k - Key.D0).ToString();
                        if (!Text.Contains('.') && Text.Length >= WholeDigits && DecimalPlaces > 0)
                            Text += '.';
                        if (Text.Length >= MaxTextLength)
                            Update(this, Text);
                        break;
                    case Key.Delete:
                    case Key.Back:
                        Text = Text[..^1];
                        if (Text.EndsWith('.'))
                            Text = Text[..^1];
                        if (Text.Length == 0)
                            Refresh(this, Value);
                        break;
                    case Key.OemPeriod:
                    case Key.Decimal:
                    case Key.OemComma:
                        if (!Text.Contains('.') && DecimalPlaces > 0)
                        {
                            while (Text.Length < WholeDigits)
                                Text = "0" + Text;                                    
                            Text += '.';
                        }
                        break;
                    case Key.Enter:
                        Update(this, Text);
                        break;
                }
            }
        }
        private async Task TimeOutTicker(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using Task task = Task.Delay(100, token);
                    await task;
                }
                catch { break; }
                long now = DateTime.Now.Ticks;
                if (now - lastInput > TimeOut * 10000)
                {
                    Refresh(this, Value);
                    break;
                }
            }
        }

        private static void Update(DecimalEntry dec, string s)
        {
            if (double.TryParse(s, out double d))
                dec.Value = d.Clamp(dec.Minimum, dec.Maximum);
            Refresh(dec, dec.Value);
        }
        private static void Refresh(DecimalEntry dec, double val)
        {
            string format = $"{new string('0', dec.WholeDigits)}.{new string('0', dec.DecimalPlaces)}";
            string what = val.ToString(format);
            dec.Text = what;
            dec.inputMode = false;
            try { dec.cts?.Cancel(); } catch { }
            (_ = dec.EntryComplete)?.Invoke(dec, EventArgs.Empty);
        }
        public double TimeOut
        {
            get { return (double)GetValue(TimeOutProperty); }
            set { SetValue(TimeOutProperty, value); }
        }
        public static readonly DependencyProperty TimeOutProperty =
            DependencyProperty.Register("TimeOut", typeof(double), typeof(DecimalEntry), new PropertyMetadata(3000.0));
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(DecimalEntry), new PropertyMetadata(0.0));
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(DecimalEntry), new PropertyMetadata(1000000.0));
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(DecimalEntry), new PropertyMetadata(0.0, ValueSet));
        private static void ValueSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DecimalEntry dec && e.NewValue is double newv)
            {
                Refresh(dec, newv);
            }
        }
        public int DecimalPlaces
        {
            get { return (int)GetValue(DecimalPlacesProperty); }
            set { SetValue(DecimalPlacesProperty, value); }
        }
        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register("DecimalPlaces", typeof(int), typeof(DecimalEntry), new PropertyMetadata(0));
        public int WholeDigits
        {
            get { return (int)GetValue(WholeDigitsProperty); }
            set { SetValue(WholeDigitsProperty, value); }
        }
        public static readonly DependencyProperty WholeDigitsProperty =
            DependencyProperty.Register("WholeDigits", typeof(int), typeof(DecimalEntry), new PropertyMetadata(4));





    }
}
