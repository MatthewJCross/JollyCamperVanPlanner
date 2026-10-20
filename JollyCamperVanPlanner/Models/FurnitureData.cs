using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JollyCamperVanPlanner
{
    public class FurnitureData : INotifyPropertyChanged
    {
        private string _name = "Object";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private double _xMm;
        public double XMm
        {
            get => _xMm;
            set => SetProperty(ref _xMm, value);
        }

        private double _yMm;
        public double YMm
        {
            get => _yMm;
            set => SetProperty(ref _yMm, value);
        }

        private double _lengthMm;
        public double LengthMm
        {
            get => _lengthMm;
            set => SetProperty(ref _lengthMm, value);
        }

        private double _widthMm;
        public double WidthMm
        {
            get => _widthMm;
            set => SetProperty(ref _widthMm, value);
        }

        private double _angle;
        public double Angle
        {
            get => _angle;
            set => SetProperty(ref _angle, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
