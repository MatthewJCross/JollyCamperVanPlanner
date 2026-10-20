
using JollyCamperVanPlanner.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace JollyCamperVanPlanner
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private double _vanLengthMm = 5986;
        private double _vanWidthMm = 1832;
        private double _cabLengthMm = 2536;
        private double _wheelbaseMm = 3640;
        private double _frontWheelArchOffsetMm = 1000;
        private double _rearWheelArchOffsetMm = 4640;
        private double _wheelArchLengthMm = 500;
        private double _wheelArchWidthMm = 225;
        private double _slidingDoorStartMm = 2600;
        private double _slidingDoorLengthMm = 1500;

        public double VanLengthMm
        {
            get => _vanLengthMm;
            set => SetProperty(ref _vanLengthMm, value);
        }

        public double VanWidthMm
        {
            get => _vanWidthMm;
            set => SetProperty(ref _vanWidthMm, value);
        }

        public double CabLengthMm
        {
            get => _cabLengthMm;
            set => SetProperty(ref _cabLengthMm, value);
        }

        public double WheelbaseMm
        {
            get => _wheelbaseMm;
            set => SetProperty(ref _wheelbaseMm, value);
        }

        public double FrontWheelArchOffsetMm
        {
            get => _frontWheelArchOffsetMm;
            set => SetProperty(ref _frontWheelArchOffsetMm, value);
        }

        public double RearWheelArchOffsetMm
        {
            get => _rearWheelArchOffsetMm;
            set => SetProperty(ref _rearWheelArchOffsetMm, value);
        }

        public double WheelArchLengthMm
        {
            get => _wheelArchLengthMm;
            set => SetProperty(ref _wheelArchLengthMm, value);
        }

        public double WheelArchWidthMm
        {
            get => _wheelArchWidthMm;
            set => SetProperty(ref _wheelArchWidthMm, value);
        }

        public double SlidingDoorStartMm
        {
            get => _slidingDoorStartMm;
            set => SetProperty(ref _slidingDoorStartMm, value);
        }

        public double SlidingDoorLengthMm
        {
            get => _slidingDoorLengthMm;
            set => SetProperty(ref _slidingDoorLengthMm, value);
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

        public ObservableCollection<FurnitureData> Furniture { get; } = new();

        private FurnitureData? _selectedFurniture;
        public FurnitureData? SelectedFurniture
        {
            get => _selectedFurniture;

            set
            {
                if (_selectedFurniture == value)
                {
                    return;
                }

                if (_selectedFurniture != null)
                {
                    _selectedFurniture.PropertyChanged -= SelectedFurniture_PropertyChanged;
                }

                if (!SetProperty(ref _selectedFurniture, value))
                {
                    return;
                }

                if (value != null)
                {
                    value.PropertyChanged += SelectedFurniture_PropertyChanged;

                    SelectedFurnitureName = value.Name;
                    SelectedFurnitureXmm = value.XMm;
                    SelectedFurnitureYmm = value.YMm;
                    SelectedFurnitureLengthMm = value.LengthMm;
                    SelectedFurnitureWidthMm = value.WidthMm;
                }
                else
                {
                    SelectedFurnitureName = string.Empty;
                    SelectedFurnitureXmm = 0;
                    SelectedFurnitureYmm = 0;
                    SelectedFurnitureLengthMm = 0;
                    SelectedFurnitureWidthMm = 0;
                }
            }
        }

        private string _selectedFurnitureName = string.Empty;
        public string SelectedFurnitureName
        {
            get => _selectedFurnitureName;
            set => SetProperty(ref _selectedFurnitureName, value);
        }

        private double _selectedFurnitureLengthMm;
        public double SelectedFurnitureLengthMm
        {
            get => _selectedFurnitureLengthMm;
            set => SetProperty(ref _selectedFurnitureLengthMm, value);
        }

        private double _selectedFurnitureWidthMm;
        public double SelectedFurnitureWidthMm
        {
            get => _selectedFurnitureWidthMm;
            set => SetProperty(ref _selectedFurnitureWidthMm, value);
        }

        private double _selectedFurnitureXmm;
        public double SelectedFurnitureXmm
        {
            get => _selectedFurnitureXmm;
            set => SetProperty(ref _selectedFurnitureXmm, value);
        }

        private double _selectedFurnitureYmm;
        public double SelectedFurnitureYmm
        {
            get => _selectedFurnitureYmm;
            set => SetProperty(ref _selectedFurnitureYmm, value);
        }


        public ICommand DeleteFurnitureCommand { get; }
        private void DeleteFurniture(object? parameter)
        {
            if (SelectedFurniture == null)
            {
                return;
            }

            FurnitureData data = SelectedFurniture;

            FurnitureDeleted?.Invoke(data);
            Furniture.Remove(data);
            SelectedFurniture = null;
        }

        public ICommand RotateFurnitureCommand { get; }
        private void RotateFurniture()
        {
            if (SelectedFurniture == null)
            {
                return;
            }

            FurnitureData data = SelectedFurniture;

            double oldLength = data.LengthMm;
            data.LengthMm = data.WidthMm;
            data.WidthMm = oldLength;

            SelectedFurnitureLengthMm = data.LengthMm;
            SelectedFurnitureWidthMm = data.WidthMm;

            FurnitureRotated?.Invoke(data);
        }

        public ICommand AddFurnitureCommand { get; }
        private void AddFurniture(object? parameter)
        {
            if (parameter is not string name)
            {
                return;
            }

            (double length, double width) = name switch
            {
                "Bed" => (1900, 1200),
                "Kitchen" => (1000, 600),
                "Fridge" => (500, 500),
                "Sink" => (400, 400),
                "Cupboard" => (600, 400),
                "Table" => (700, 600),
                "Seat" => (500, 500),
                "Wheel Arch" => (700, 250),
                _ => (500, 500)
            };

            if (length > VanLengthMm || width > VanWidthMm)
            {
                FurnitureTooLarge?.Invoke(name);
                return;
            }

            FurnitureData data = new()
            {
                Name = name,
                LengthMm = length,
                WidthMm = width,
                XMm = Math.Min(100, VanLengthMm - length),
                YMm = Math.Min(100, VanWidthMm - width)
            };

            Furniture.Add(data);
            SelectedFurniture = data;
            FurnitureAdded?.Invoke(data);
        }

        public ICommand ApplyFurnitureChangesCommand { get; }

        public ICommand PrintFloorPlanCommand { get; }

        public event Action<string>? FurnitureTooLarge;
        public event Action<FurnitureData>? FurnitureDeleted;
        public event Action<FurnitureData>? FurnitureRotated;
        public event Action<FurnitureData>? FurnitureAdded;
        public event Action<FurnitureData>? FurnitureChangesApplied;
        public event Action? FurnitureChangesRejected;
        public event Action? PrintFloorPlanRequested;

        public MainViewModel()
        {
            DeleteFurnitureCommand = new RelayCommand(DeleteFurniture);
            RotateFurnitureCommand = new RelayCommand(_ => RotateFurniture());
            AddFurnitureCommand = new RelayCommand(AddFurniture);
            ApplyFurnitureChangesCommand = new RelayCommand(_ => ApplySelectedFurnitureChangesFromEditor());
            ApplyFurnitureChangesCommand = new RelayCommand(_ => ApplyFurnitureChanges());
            PrintFloorPlanCommand = new RelayCommand(_ => PrintFloorPlanRequested?.Invoke());
        }

        private void SelectedFurniture_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not FurnitureData data || data != SelectedFurniture)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(FurnitureData.Name):
                    SelectedFurnitureName = data.Name;
                    break;

                case nameof(FurnitureData.XMm):
                    SelectedFurnitureXmm = data.XMm;
                    break;

                case nameof(FurnitureData.YMm):
                    SelectedFurnitureYmm = data.YMm;
                    break;

                case nameof(FurnitureData.LengthMm):
                    SelectedFurnitureLengthMm = data.LengthMm;
                    break;

                case nameof(FurnitureData.WidthMm):
                    SelectedFurnitureWidthMm = data.WidthMm;
                    break;
            }
        }


        public bool ApplySelectedFurnitureChanges(string name, double x, double y, double length, double width)
        {
            if (SelectedFurniture == null || !double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(length) || !double.IsFinite(width) || length <= 0 || width <= 0 || length > VanLengthMm || width > VanWidthMm || x < 0 || y < 0 || x + length > VanLengthMm || y + width > VanWidthMm)
            {
                return false;
            }

            FurnitureData data = SelectedFurniture;

            data.Name = string.IsNullOrWhiteSpace(name) ? "Object" : name.Trim();
            data.XMm = x;
            data.YMm = y;
            data.LengthMm = length;
            data.WidthMm = width;

            return true;
        }

        private void ApplySelectedFurnitureChangesFromEditor()
        {
            ApplySelectedFurnitureChanges(SelectedFurnitureName, SelectedFurnitureXmm, SelectedFurnitureYmm, SelectedFurnitureLengthMm, SelectedFurnitureWidthMm);
        }

        private void ApplyFurnitureChanges()
        {
            if (!ApplySelectedFurnitureChanges(SelectedFurnitureName, SelectedFurnitureXmm, SelectedFurnitureYmm, SelectedFurnitureLengthMm, SelectedFurnitureWidthMm))
            {
                FurnitureChangesRejected?.Invoke();
                return;
            }

            if (SelectedFurniture != null)
            {
                FurnitureChangesApplied?.Invoke(SelectedFurniture);
            }
        }

        public CamperLayout CreateLayout()
        {
            return new CamperLayout
            {     
                VanLengthMm = VanLengthMm,
                VanWidthMm = VanWidthMm,
                Objects = Furniture.Select(data => new FurnitureData
                {
                    Name = data.Name,
                    XMm = data.XMm,
                    YMm = data.YMm,
                    LengthMm = data.LengthMm,
                    WidthMm = data.WidthMm,
                    Angle = data.Angle
                }).ToList()
            };
        }

        public void LoadLayout(CamperLayout layout)
        {
            VanLengthMm = layout.VanLengthMm;
            VanWidthMm = layout.VanWidthMm;

            SelectedFurniture = null;
            Furniture.Clear();

            foreach (FurnitureData data in layout.Objects)
            {
                Furniture.Add(data);
            }
        }
    }
}

