using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIDStation.Radio
{
    public class PowerLevel : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs lowArgs = new(nameof(Low));
        private static readonly PropertyChangedEventArgs midArgs = new(nameof(Mid));
        private static readonly PropertyChangedEventArgs highArgs = new(nameof(High));
        private void OnLowPropertyChanged() { (_ = PropertyChanged)?.Invoke(this, lowArgs); }
        private void OnMidPropertyChanged() { (_ = PropertyChanged)?.Invoke(this, midArgs); }
        private void OnHighPropertyChanged() { (_ = PropertyChanged)?.Invoke(this, highArgs); }
        public byte Low
        {
            get => low;
            set
            {
                low = value;
                OnLowPropertyChanged();
            }
        }
        private byte low;
        public byte Mid
        {
            get => mid;
            set
            {
                mid = value;
                OnMidPropertyChanged();
            }
        }
        private byte mid;
        public byte High
        {
            get => high;
            set
            {
                high = value;
                OnHighPropertyChanged();
            }
        }
        private byte high;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
