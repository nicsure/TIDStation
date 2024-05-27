using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TIDStation.General;

namespace TIDStation.UI
{
    public class NumEntry : Label
    {
        private bool inputMode = false;

        private string Text
        {
            get => (Content is string s) ? s.Trim() : string.Empty;
            set => Content = value;
        }

        private void EndInput()
        {
            int i = (int.TryParse(Text, out int d) ? d : Value);
            Text = $"{i:D3}";
            inputMode = false;
            Value = i;
            if (Value != i)
                Text = $"{Value:D3}";
        }

        public void KeyIn(Key k)
        {
            if (inputMode)
            {
                switch (k)
                {
                    case Key.Escape:
                        Text = string.Empty;
                        EndInput();
                        break;
                    case Key.Back:
                    case Key.Delete:
                        Text = Text[..^1];
                        if (Text.Length == 0)
                            EndInput();
                        break;
                    case Key.Enter:
                        EndInput();
                        break;
                    case Key.NumPad0: KeyIn(Key.D0); break;
                    case Key.NumPad1: KeyIn(Key.D1); break;
                    case Key.NumPad2: KeyIn(Key.D2); break;
                    case Key.NumPad3: KeyIn(Key.D3); break;
                    case Key.NumPad4: KeyIn(Key.D4); break;
                    case Key.NumPad5: KeyIn(Key.D5); break;
                    case Key.NumPad6: KeyIn(Key.D6); break;
                    case Key.NumPad7: KeyIn(Key.D7); break;
                    case Key.NumPad8: KeyIn(Key.D8); break;
                    case Key.NumPad9: KeyIn(Key.D9); break;
                    case Key.D0:
                    case Key.D1:
                    case Key.D2:
                    case Key.D3:
                    case Key.D4:
                    case Key.D5:
                    case Key.D6:
                    case Key.D7:
                    case Key.D8:
                    case Key.D9:
                        Text += k - Key.D0;
                        if (Text.Length >= 3)
                            EndInput();
                        break;
                }
            }
            else
            {
                if ((k >= Key.D0 && k <= Key.D9) || (k >= Key.NumPad0 && k <= Key.NumPad9))
                {
                    inputMode = true;
                    Text = string.Empty;
                    KeyIn(k);
                }
            }
        }

        public int InputTimeout
        {
            get { return (int)GetValue(InputTimeoutProperty); }
            set { SetValue(InputTimeoutProperty, value); }
        }
        public static readonly DependencyProperty InputTimeoutProperty =
            DependencyProperty.Register("InputTimeout", typeof(int), typeof(NumEntry), new PropertyMetadata(3000));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumEntry), new PropertyMetadata(0));


    }
}
