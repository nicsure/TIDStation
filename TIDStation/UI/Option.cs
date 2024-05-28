using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TIDStation.Radio;
using TIDStation.View;

namespace TIDStation.UI
{
    public class Option : Label
    {
        private readonly ContextMenu cMenu = new();
        public Option() : base()
        {
            ContextMenu = cMenu;            
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach(var obj in cMenu.Items) 
            {
                if(obj is TextBox tb)
                {
                    switch(Type)
                    {
                        case "string":
                            Content = tb.Text = Model.ObjValue.ToString();
                            break;
                        case "int":
                            Content = tb.Text = ((int)Model.ObjValue).ToString();
                            break;
                        case "bool":
                            Content = tb.Text = ((bool)Model.ObjValue).ToString();
                            break;
                    }
                }
                else
                if(obj is MenuItem mi && mi.Tag is string tag)
                {
                    switch(Type)
                    {
                        case "string":
                            if (tag.Equals(Model.ObjValue.ToString()))
                            {
                                Content = mi.Header;
                                return;
                            }
                            break;
                        case "bool":
                            if (bool.Parse(tag) == (bool)Model.ObjValue)
                            {
                                Content = mi.Header;
                                return;
                            }
                            break;
                        case "int":
                            if (int.Parse(tag) == (int)Model.ObjValue)
                            {
                                Content = mi.Header;
                                return;
                            }
                            break;
                    }
                }
            }
        }

        private  void Tb_Enter(TextBox tb)
        {
            switch(Type)
            {
                case "string":
                    Model.ObjValue = tb.Text;
                    break;
                case "int":
                    Model.ObjValue = int.TryParse(tb.Text, out int i) ? i : Model.ObjValue;
                    break;
                case "bool":
                    Model.ObjValue = bool.TryParse(tb.Text, out bool b) ? b : Model.ObjValue;
                    break;
            }
        }

        private void Mi_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string tag)
            {
                Content = mi.Header;
                switch (Type)
                {
                    case "string":
                        Model.ObjValue = tag;
                        break;
                    case "bool":
                        Model.ObjValue = bool.Parse(tag);
                        break;
                    case "int":
                        Model.ObjValue = int.Parse(tag);
                        break;
                    default:
                        return;
                }
            }
            else 
                return;
            TD.Update();
        }

        public string Type
        {
            get { return (string)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(string), typeof(Option), new PropertyMetadata("bool"));


        public ViewModel Model
        {
            get { return (ViewModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(ViewModel), typeof(Option), new PropertyMetadata(null!, OnModelChanged));
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is Option opt)
            {
                if(e.NewValue is ViewModel vm)
                {
                    if(e.OldValue is ViewModel oldVM)
                        oldVM.PropertyChanged -= opt.Vm_PropertyChanged;
                    vm.PropertyChanged += opt.Vm_PropertyChanged;
                }
            }
        }

        public string Options
        {
            get { return (string)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(string), typeof(Option), new PropertyMetadata(string.Empty, OnOptionsChanged));
        private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is Option opt && e.NewValue is string options)
            {
                if(opt.Tag is string head)
                {
                    opt.cMenu.Items.Add(new MenuItem() { Header = head, IsEnabled = false });
                    opt.cMenu.Items.Add(new Separator());
                }
                if(options.StartsWith("input"))
                {                    
                    TextBox tb = new()
                    {
                        Width = 200
                    };
                    if (int.TryParse(options[5..], out int max))
                        tb.MaxLength = Math.Abs(max);
                    opt.cMenu.Items.Add(tb);
                    tb.KeyDown += (s, e) =>
                    {
                        if (e.Key == Key.Enter)
                        {
                            e.Handled = true;
                            opt.Tb_Enter((TextBox)s);
                            opt.cMenu.IsOpen = false;
                        }
                        if (max < 0)
                        {
                            switch(Keyboard.Modifiers)
                            {
                                case ModifierKeys.Shift:
                                    switch (e.Key)
                                    {
                                        case >= Key.A and <= Key.D: break;
                                        case Key.D8: break;
                                        case Key.D3: break;
                                        default: e.Handled = true; break;
                                    }
                                    break;
                                case ModifierKeys.None:
                                    switch (e.Key)
                                    {
                                        case >= Key.A and <= Key.D: break;
                                        case >= Key.D0 and <= Key.D9: break;
                                        case >= Key.NumPad0 and <= Key.NumPad9: break;
                                        case Key.Back: break;
                                        case Key.Delete: break;
                                        case Key.Left: break;
                                        case Key.Right: break;
                                        case Key.Multiply: break;
                                        case Key.Oem7: break;
                                        default: e.Handled = true; break;
                                    }
                                    break;
                                default: e.Handled = true; break;
                            }


                            switch (e.Key)
                            {
                                case >= Key.A and <= Key.D: break;
                                case >= Key.D0 and <= Key.D9: break;
                                case >= Key.NumPad0 and <= Key.NumPad9: break;
                                case Key.Back: break;
                                case Key.Delete: break;
                                case Key.Left: break;
                                case Key.Right: break;
                                case Key.Multiply: break;
                                case Key.Oem7: break;
                                default: e.Handled = true; break;
                            }
                        }
                    };
                }
                else
                foreach(var item in options.Split(','))
                {
                    string[] p = item.Split(";");
                    MenuItem mi;
                    if (p.Length == 2)
                    {
                        mi = new()
                        {
                            Header = p[0].Trim(),
                            Tag = p[1].Trim()
                        };
                    }
                    else
                    if (p.Length == 1)
                    {
                        mi = new();
                        mi.Tag = mi.Header = p[0].Trim();
                    }
                    else continue;
                    opt.cMenu.Items.Add(mi);
                    mi.Click += opt.Mi_Click;
                }
            }
        }


    }
}
