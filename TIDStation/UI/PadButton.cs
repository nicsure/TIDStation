using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TIDStation.Data;

namespace TIDStation.UI
{
    public class PadButton : Border
    {
        private Brush? norm = null;
        public PadButton() : base() 
        {
            
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Mouse.Capture(this,CaptureMode.SubTree);
            base.OnMouseDown(e);
            norm ??= Background;
            Background = Brushes.DarkGray;
            Context.Instance.KeyPad.Value = (Key)PadKey;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            ReleaseMouseCapture();
            if (norm != null) Background = norm;
            Context.Instance.KeyPad.Value = Key.None;
        }

        public int PadKey
        {
            get { return (int)GetValue(PadKeyProperty); }
            set { SetValue(PadKeyProperty, value); }
        }
        public static readonly DependencyProperty PadKeyProperty =
            DependencyProperty.Register("PadKey", typeof(int), typeof(PadButton), new PropertyMetadata(18));
    }

    public class PadLabel : TextBlock
    {
        public PadLabel() : base() { }
    }
}
