using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JollyCamperVanPlanner
{
    public partial class MainWindow : Window
    {
        private const double FloorLeft = 40;
        private const double FloorTop = 40;
        private const double MaxFloorPixelsWide = 800;
        private const double MaxFloorPixelsHigh = 450;
        private const double GridMm = 50;

        private double _scale = 0.25;

        private Border? _vanFloor;
        private Border? _selectedObject;
        private Border? _draggedObject;

        private Point _dragStart;
        private double _objectStartX;
        private double _objectStartY;
        private bool _isDragging;

        private Border? _resizeHandle;
        private Border? _resizingObject;
        private Point _resizeStart;
        private double _resizeStartLengthMm;
        private double _resizeStartWidthMm;

        public MainViewModel ViewModel { get; } = new(); 
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.FurnitureDeleted += OnFurnitureDeleted;
            ViewModel.FurnitureRotated += OnFurnitureRotated;
            ViewModel.FurnitureAdded += OnFurnitureAdded;
            ViewModel.FurnitureTooLarge += OnFurnitureTooLarge;
            ViewModel.FurnitureChangesApplied += OnFurnitureChangesApplied;
            ViewModel.FurnitureChangesRejected += OnFurnitureChangesRejected;
            ViewModel.PrintFloorPlanRequested += OnPrintFloorPlanRequested;

            VanLengthBox.Text = ViewModel.VanLengthMm.ToString("0");
            VanWidthBox.Text = ViewModel.VanWidthMm.ToString("0");
            CabLengthBox.Text = ViewModel.CabLengthMm.ToString("0");
            WheelbaseBox.Text = ViewModel.WheelbaseMm.ToString("0");
            FrontArchOffsetBox.Text = ViewModel.FrontWheelArchOffsetMm.ToString("0");
            RearArchOffsetBox.Text = ViewModel.RearWheelArchOffsetMm.ToString("0");
            WheelArchLengthBox.Text = ViewModel.WheelArchLengthMm.ToString("0");
            WheelArchWidthBox.Text = ViewModel.WheelArchWidthMm.ToString("0");
            SlidingDoorStartBox.Text = ViewModel.SlidingDoorStartMm.ToString("0");
            SlidingDoorLengthBox.Text = ViewModel.SlidingDoorLengthMm.ToString("0");

            StateChanged += MainWindow_StateChanged;
            SizeChanged += MainWindow_SizeChanged;
            Loaded += MainWindow_Loaded;
            RebuildFloor();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer? scrollViewer = FindVisualAncestor<ScrollViewer>(PlannerCanvas);

            if (scrollViewer != null)
            {
                scrollViewer.SizeChanged -= PlannerHost_SizeChanged;
                scrollViewer.SizeChanged += PlannerHost_SizeChanged;
            }

            ResizeFloorToViewport();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MaximizeButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(ResizeFloorToViewport), DispatcherPriority.Loaded);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void PlannerHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(ResizeFloorToViewport), DispatcherPriority.Loaded);
        }

        private void ResizeFloorToViewport()
        {
            if (!IsLoaded || PlannerCanvas.ActualWidth <= 0 || PlannerCanvas.ActualHeight <= 0)
            {
                return;
            }

            FurnitureData? selectedFurniture = ViewModel.SelectedFurniture;
            List<FurnitureData> furniture = ViewModel.Furniture.ToList();

            RebuildFloor();

            foreach (FurnitureData data in furniture)
            {
                Border item = CreateFurniture(data.Name, data.LengthMm, data.WidthMm, GetObjectColour(data.Name));
                item.Tag = data;
                SetObjectPosition(item, data.XMm, data.YMm);
                item.RenderTransform = Transform.Identity;
                PlannerCanvas.Children.Add(item);

                if (ReferenceEquals(data, selectedFurniture))
                {
                    SelectObject(item);
                }
            }
        }

        private void GetAvailablePlannerSize(out double width, out double height)
        {
            ScrollViewer? scrollViewer = FindVisualAncestor<ScrollViewer>(PlannerCanvas);

            if (scrollViewer != null && scrollViewer.ViewportWidth > 0 && scrollViewer.ViewportHeight > 0)
            {
                width = scrollViewer.ViewportWidth;
                height = scrollViewer.ViewportHeight;
                return;
            }

            width = ActualWidth > 0 ? Math.Max(MaxFloorPixelsWide + FloorLeft * 2, ActualWidth - 420) : MaxFloorPixelsWide + FloorLeft * 2;
            height = ActualHeight > 0 ? Math.Max(MaxFloorPixelsHigh + FloorTop * 2, ActualHeight - 180) : MaxFloorPixelsHigh + FloorTop * 2;
        }

        private static T? FindVisualAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            DependencyObject? current = VisualTreeHelper.GetParent(element);

            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private double FloorPixelWidth => ViewModel.VanLengthMm * _scale;
        private double FloorPixelHeight => ViewModel.VanWidthMm * _scale;

        private void RebuildFloor()
        {
            GetAvailablePlannerSize(out double availableWidth, out double availableHeight);
            _scale = Math.Min(Math.Max(100, availableWidth - FloorLeft * 2) / ViewModel.VanLengthMm, Math.Max(100, availableHeight - FloorTop * 2) / ViewModel.VanWidthMm);

            double floorWidth = FloorPixelWidth;
            double floorHeight = FloorPixelHeight;

            PlannerCanvas.Children.Clear();
            _vanFloor = null;

            PlannerCanvas.Width = Math.Max(availableWidth, FloorLeft * 2 + floorWidth);
            PlannerCanvas.Height = Math.Max(availableHeight, FloorTop * 2 + floorHeight);

            _vanFloor = new Border
            {
                Width = floorWidth,
                Height = floorHeight,
                Background = Brushes.WhiteSmoke,
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(5),
                CornerRadius = new CornerRadius(18),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(_vanFloor, FloorLeft);
            Canvas.SetTop(_vanFloor, FloorTop);
            PlannerCanvas.Children.Add(_vanFloor);

            DrawFloorGrid();
            DrawVanDetails();
            ClearSelection();
            StatusText.Text = $"Van: {ViewModel.VanLengthMm:0} × {ViewModel.VanWidthMm:0} mm | Scale: {_scale:0.000} pixels/mm";
        }

        private void DrawFloorGrid()
        {
            if (_vanFloor is null)
            {
                return;
            }

            double spacing = GridMm * _scale;
            DrawingGroup group = new();
            group.Children.Add(new GeometryDrawing(Brushes.WhiteSmoke, null, new RectangleGeometry(new Rect(0, 0, spacing, spacing))));
            Pen gridPen = new(new SolidColorBrush(Color.FromRgb(215, 215, 215)), 0.6);

            group.Children.Add(new GeometryDrawing(null, gridPen, new LineGeometry(new Point(0, 0), new Point(spacing, 0))));
            group.Children.Add(new GeometryDrawing(null, gridPen, new LineGeometry(new Point(0, 0), new Point(0, spacing))));

            DrawingBrush brush = new(group)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, spacing, spacing),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };

            _vanFloor.Background = brush;

            Pen metrePen = new(new SolidColorBrush(Color.FromRgb(135, 135, 135)), 1.2);

            for (double xMm = 1000; xMm < ViewModel.VanLengthMm; xMm += 1000)
            {
                double x = FloorLeft + xMm * _scale;
                AddVanLine(x, FloorTop, x, FloorTop + FloorPixelHeight, metrePen.Brush, metrePen.Thickness);
            }

            for (double yMm = 1000; yMm < ViewModel.VanWidthMm; yMm += 1000)
            {
                double y = FloorTop + yMm * _scale;
                AddVanLine(FloorLeft, y, FloorLeft + FloorPixelWidth, y, metrePen.Brush, metrePen.Thickness);
            }
        }

        private void DrawVanDetails()
        {
            double s = _scale;
            double length = ViewModel.VanLengthMm;
            double width = ViewModel.VanWidthMm;

            // Proportional stretching relative to the Crafter L1H1 reference dimensions.
            // X follows overall length; Y follows overall width. The front remains on the left.
            double sx = ViewModel.VanLengthMm / 5986.0;
            double sy = ViewModel.VanWidthMm / 2427.0;
            double X(double mm) => mm * sx;
            double Y(double mm) => mm * sy;

            // Cab boundary: the reference load-space length is 3450 mm, so the cab/front section
            // ends approximately 2536 mm from the front of the 5986 mm van.
            double cabLength = X(ViewModel.CabLengthMm);
            AddVanLine(FloorLeft + cabLength * s, FloorTop + 5, FloorLeft + cabLength * s, FloorTop + width * s - 5, Brushes.SlateGray, 3);

            // Wheel arches. Offsets are centre positions measured from the front (left).
            double archLength = X(ViewModel.WheelArchLengthMm) * s;
            double archWidth = Y(ViewModel.WheelArchWidthMm) * s;
            double frontArchX = X(ViewModel.FrontWheelArchOffsetMm - ViewModel.WheelArchLengthMm / 2.0) * s;
            double rearArchX = X(ViewModel.RearWheelArchOffsetMm - ViewModel.WheelArchLengthMm / 2.0) * s;
            AddVanRectangle(frontArchX, 0, archLength, archWidth, Brushes.DimGray, Brushes.Black, 2, 12);
            AddVanRectangle(frontArchX, FloorPixelHeight - archWidth, archLength, archWidth, Brushes.DimGray, Brushes.Black, 2, 12);
            AddVanRectangle(rearArchX, 0, archLength, archWidth, Brushes.DimGray, Brushes.Black, 2, 12);
            AddVanRectangle(rearArchX, FloorPixelHeight - archWidth, archLength, archWidth, Brushes.DimGray, Brushes.Black, 2, 12);

            // Windscreen: immediately behind the dashboard and before the seats.
            // It spans 1000 mm across the width and is 120 mm deep along the van.
            double windscreenX = X(900) * s;
            double windscreenY = Y(100) * s;
            double windscreenLength = X(900) * s;
            double windscreenWidth = Y(2427 - 200) * s;
            AddVanRectangle(windscreenX, windscreenY, windscreenLength, windscreenWidth, Brushes.LightSteelBlue, Brushes.SlateGray, 2, 12);

            // Dashboard: transverse panel, set back from the front bumper.
            // X is distance from the front (left); Y spans the van width.
            double dashboardX = X(950) * s;
            double dashboardY = Y(100) * s;
            double dashboardLength = X(300) * s;
            double dashboardWidth = Y(2427 - 200) * s;
            AddVanRectangle(dashboardX, dashboardY, dashboardLength, dashboardWidth, Brushes.Gray, Brushes.DimGray, 2, 5);

            // Front seats sit side-by-side across the van, behind the windscreen.
            double seatX = (cabLength - 650.0) * s;
            double seatLength = X(600) * s;
            double seatWidth = Y(600) * s;
            double seatGap = Y(250) * s;
            double driverSeatY = Y(200) * s;
            double passengerSeatY = driverSeatY + seatWidth + seatGap;
            AddVanRectangle(seatX, driverSeatY, seatLength, seatWidth, Brushes.DarkSeaGreen, Brushes.DarkSlateGray, 2, 12);
            AddVanRectangle(seatX, passengerSeatY, seatLength, (seatWidth*2), Brushes.DarkSeaGreen, Brushes.DarkSlateGray, 2, 12);

            // Sliding door opening along the right-hand side wall in the load area.
            double doorStart = X(ViewModel.SlidingDoorStartMm) * s;
            double doorLength = X(ViewModel.SlidingDoorLengthMm) * s;
            double doorY = FloorPixelHeight - 3;
            AddVanLine(FloorLeft + doorStart, FloorTop + doorY, FloorLeft + doorStart + doorLength, FloorTop + doorY, Brushes.WhiteSmoke, 7);
            AddVanLine(FloorLeft + doorStart + X(80) * s, FloorTop + FloorPixelHeight - Y(35) * s, FloorLeft + doorStart + doorLength - X(80) * s, FloorTop + FloorPixelHeight - Y(35) * s, Brushes.SlateGray, 1.5);

            // Rear doors and centre split at the back (right-hand end).
            double rearX = (length - X(45)) * s;
            AddVanLine(FloorLeft + rearX, FloorTop + FloorPixelHeight * 0.5, FloorLeft + FloorPixelWidth, FloorTop + FloorPixelHeight * 0.5, Brushes.SlateGray, 2);
            AddVanLine(FloorLeft + length * s - X(15) * s, FloorTop + Y(15) * s, FloorLeft + length * s - X(15) * s, FloorTop + FloorPixelHeight - Y(15) * s, Brushes.SlateGray, 1);

            // Rear door windows.
            AddVanRectangle(X(5986 - 33) * s, FloorPixelHeight * 0.18, X(15) * s, FloorPixelHeight * 0.22, Brushes.LightSteelBlue, Brushes.SlateGray, 1, 3);
            AddVanRectangle(X(5986 - 33) * s, FloorPixelHeight * 0.60, X(15) * s, FloorPixelHeight * 0.22, Brushes.LightSteelBlue, Brushes.SlateGray, 1, 3);

            // Side wall details.
            AddVanLine(FloorLeft + X(700) * s, FloorTop + Y(12) * s, FloorLeft + (length - X(30)) * s, FloorTop + Y(12) * s, Brushes.SlateGray, 1);
            AddVanLine(FloorLeft + X(700) * s, FloorTop + FloorPixelHeight - Y(12) * s, FloorLeft + (length - X(30)) * s, FloorTop + FloorPixelHeight - Y(12) * s, Brushes.SlateGray, 1);

            // Labels are centred on the same stretched geometry as the drawn features.
            // Keep labels out of the main seat area so they do not obscure the cab layout.
            AddVanLabel("Windscreen", X(1450), Y(300), X(120), Y(1000));
            AddVanLabel("Dashboard", X(1025), Y(200), X(150), Y(2427 - 400));
            AddVanLabel("Driver Seat", X(1950), Y(200), X(500), Y(550));
            AddVanLabel("Passenger Seat", X(1950), Y(1350), X(500), Y(550));
            AddVanLabel("Front Wheel Arch", X(ViewModel.FrontWheelArchOffsetMm - ViewModel.WheelArchLengthMm / 2.0), -150, X(ViewModel.WheelArchLengthMm), Y(ViewModel.WheelArchWidthMm));
            AddVanLabel("Rear Wheel Arch", X(ViewModel.RearWheelArchOffsetMm - ViewModel.WheelArchLengthMm / 2.0), -150, X(ViewModel.WheelArchLengthMm), Y(ViewModel.WheelArchWidthMm));
            AddVanLabel("Sliding Door", X(ViewModel.SlidingDoorStartMm), Y(width - 150), X(ViewModel.SlidingDoorLengthMm), Y(150));
            AddVanLabel("Rear Doors", length - X(100), 0, X(100), width);
        }

        private void AddVanRectangle(double x, double y, double width, double height, Brush fill, Brush outline, double outlineThickness = 1, double cornerRadius = 0)
        {
            Border shape = new()
            {
                Tag = "VanDetail",
                Width = width,
                Height = height,
                Background = fill,
                BorderBrush = outline,
                BorderThickness = new Thickness(outlineThickness),
                CornerRadius = new CornerRadius(cornerRadius),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(shape, FloorLeft + x);
            Canvas.SetTop(shape, FloorTop + y);

            PlannerCanvas.Children.Add(shape);
        }

        private void AddVanLine(double x1, double y1, double x2, double y2, Brush colour, double thickness)
        {
            Line line = new()
            {
                Tag = "VanDetail",
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = colour,
                StrokeThickness = thickness,
                IsHitTestVisible = false
            };

            PlannerCanvas.Children.Add(line);
        }

        private void AddVanLabel(string text, double xMm, double yMm, double widthMm, double heightMm)
        {
            TextBlock label = new()
            {
                Tag = "VanDetail",
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkSlateGray,
                Background = Brushes.Transparent,
                Padding = new Thickness(2),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                IsHitTestVisible = false
            };

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(label, FloorLeft + (xMm + widthMm / 2) * _scale - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, FloorTop + (yMm + heightMm / 2) * _scale - label.DesiredSize.Height / 2);

            PlannerCanvas.Children.Add(label);
        }

        private void ApplyVanSize_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(VanLengthBox.Text, out double length) || !double.TryParse(VanWidthBox.Text, out double width) ||
                !double.IsFinite(length) || !double.IsFinite(width) ||
                length < 1000 || length > 10000 || width < 800 || width > 3000)
            {
                MessageBox.Show("Enter a valid internal length between 1000 and 10000 mm and width between 800 and 3000 mm.", "Invalid van dimensions", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ViewModel.Furniture.Count > 0)
            {
                MessageBoxResult answer = MessageBox.Show("Changing the van size clears the current furniture layout. Save your layout first if you want to keep it. Continue?", "Change van dimensions", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ViewModel.Furniture.Clear();
            ViewModel.SelectedFurniture = null;

            ViewModel.VanLengthMm = length;
            ViewModel.VanWidthMm = width;

            RebuildFloor();
        }

        private Border CreateFurniture(string name, double lengthMm, double widthMm, Brush colour)
        {
            Border item = new()
            {
                Width = lengthMm * _scale,
                Height = widthMm * _scale,
                Background = colour,
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.SizeAll,
                Tag = new FurnitureData
                {
                    Name = name,
                    LengthMm = lengthMm,
                    WidthMm = widthMm,
                    XMm = 0,
                    YMm = 0,
                    Angle = 0
                },
                Child = new TextBlock
                {
                    Text = $"{name}\n{lengthMm:0} × {widthMm:0}",
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    IsHitTestVisible = false
                }
            };

            item.MouseLeftButtonDown += Object_MouseLeftButtonDown;
            item.MouseMove += Object_MouseMove;
            item.MouseLeftButtonUp += Object_MouseLeftButtonUp;

            return item;
        }

        private void SetObjectPosition(Border item, double xMm, double yMm)
        {
            FurnitureData data = (FurnitureData)item.Tag;

            data.XMm = xMm;
            data.YMm = yMm;

            Canvas.SetLeft(item, FloorLeft + xMm * _scale);
            Canvas.SetTop(item, FloorTop + yMm * _scale);
        }

        private void Object_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border item)
            {
                return;
            }

            SelectObject(item);

            _draggedObject = item;
            _dragStart = e.GetPosition(PlannerCanvas);
            _objectStartX = Canvas.GetLeft(item);
            _objectStartY = Canvas.GetTop(item);
            _isDragging = false;

            item.CaptureMouse();
            e.Handled = true;
        }

        private void Object_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedObject is null || !_draggedObject.IsMouseCaptured)
            {
                return;
            }

            Point current = e.GetPosition(PlannerCanvas);

            double deltaX = current.X - _dragStart.X;
            double deltaY = current.Y - _dragStart.Y;

            if (Math.Abs(deltaX) > 2 || Math.Abs(deltaY) > 2)
            {
                _isDragging = true;
            }

            if (!_isDragging)
            {
                return;
            }

            FurnitureData data = (FurnitureData)_draggedObject.Tag;
            double xMm = Snap((_objectStartX - FloorLeft + deltaX) / _scale);
            double yMm = Snap((_objectStartY - FloorTop + deltaY) / _scale);

            xMm = Math.Clamp(xMm, 0, ViewModel.VanLengthMm - data.LengthMm);
            yMm = Math.Clamp(yMm, 0, ViewModel.VanWidthMm - data.WidthMm);

            SetObjectPosition(_draggedObject, xMm, yMm);
            PositionResizeHandle();
            UpdateObjectLabel(_draggedObject);

            StatusText.Text = $"{data.Name}: X {data.XMm:0} mm, Y {data.YMm:0} mm";
        }

        private void Object_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedObject is null)
            {
                return;
            }

            _draggedObject.ReleaseMouseCapture();
            _draggedObject = null;
            _isDragging = false;
            e.Handled = true;
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedObject?.Tag is not FurnitureData data || sender is not Border handle)
            {
                return;
            }

            _resizingObject = _selectedObject;
            _resizeStart = e.GetPosition(PlannerCanvas);
            _resizeStartLengthMm = data.LengthMm;
            _resizeStartWidthMm = data.WidthMm;

            handle.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_resizingObject?.Tag is not FurnitureData data || sender is not Border handle || !handle.IsMouseCaptured)
            {
                return;
            }

            Point current = e.GetPosition(PlannerCanvas);

            double deltaX = current.X - _resizeStart.X;
            double deltaY = current.Y - _resizeStart.Y;

            int angle = ((int)data.Angle % 360 + 360) % 360;

            double lengthChange;
            double widthChange;

            lengthChange = deltaX / _scale;
            widthChange = deltaY / _scale;

            double maxLength = ViewModel.VanLengthMm - data.XMm;
            double maxWidth = ViewModel.VanWidthMm - data.YMm;
            double length = Snap(Math.Clamp(_resizeStartLengthMm + lengthChange, 100, Math.Max(100, maxLength)));
            double width = Snap(Math.Clamp(_resizeStartWidthMm + widthChange, 100, Math.Max(100, maxWidth)));

            data.LengthMm = length;
            data.WidthMm = width;

            _resizingObject.Width = length * _scale;
            _resizingObject.Height = width * _scale;

            _resizingObject.RenderTransform = Transform.Identity;

            UpdateObjectLabel(_resizingObject);
            PositionResizeHandle();

            StatusText.Text = $"{data.Name}: {length:0} × {width:0} mm";
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border handle && handle.IsMouseCaptured)
            {
                handle.ReleaseMouseCapture();
            }

            _resizingObject = null;
            e.Handled = true;
        }

        private double Snap(double value)
        {
            return Math.Round(value / GridMm) * GridMm;
        }

        private void SelectObject(Border item)
        {
            if (_selectedObject is not null)
            {
                _selectedObject.BorderBrush = Brushes.DimGray;
                _selectedObject.BorderThickness = new Thickness(2);
            }

            _selectedObject = item;
            ViewModel.SelectedFurniture = item.Tag as FurnitureData;
            _selectedObject.BorderBrush = Brushes.DodgerBlue;
            _selectedObject.BorderThickness = new Thickness(3);

            UpdateResizeHandle();
        }

        private void UpdateResizeHandle()
        {
            if (_resizeHandle != null)
            {
                PlannerCanvas.Children.Remove(_resizeHandle);
                _resizeHandle = null;
            }

            if (_selectedObject?.Tag is not FurnitureData)
            {
                return;
            }

            _resizeHandle = new Border
            {
                Width = 14,
                Height = 14,
                Background = Brushes.DodgerBlue,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.SizeNWSE,
                ToolTip = "Drag to resize",
                Tag = "ResizeHandle"
            };

            _resizeHandle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;
            _resizeHandle.MouseMove += ResizeHandle_MouseMove;
            _resizeHandle.MouseLeftButtonUp += ResizeHandle_MouseLeftButtonUp;

            PlannerCanvas.Children.Add(_resizeHandle);
            PositionResizeHandle();
        }

        private void PositionResizeHandle()
        {
            if (_resizeHandle == null || _selectedObject == null)
            {
                return;
            }

            Point corner = new Point(_selectedObject.Width, _selectedObject.Height);

            if (_selectedObject.RenderTransform is RotateTransform rotation)
            {
                corner = rotation.Transform(corner);
            }

            double left = Canvas.GetLeft(_selectedObject);
            double top = Canvas.GetTop(_selectedObject);

            Canvas.SetLeft(_resizeHandle, left + corner.X - _resizeHandle.Width / 2);
            Canvas.SetTop(_resizeHandle, top + corner.Y - _resizeHandle.Height / 2);
        }

        private void UpdateObjectLabel(Border item)
        {
            if (item.Tag is not FurnitureData data || item.Child is not TextBlock label)
            {
                return;
            }

            label.Text = $"{data.Name}\n{data.LengthMm:0} × {data.WidthMm:0}";
        }

        private void ApplyGeometry_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadGeometry(out string error))
            {
                MessageBox.Show(error, "Invalid geometry", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DrawUpdatedVanOutline();
        }

        private void DrawUpdatedVanOutline()
        {
            List<UIElement> oldDetails = PlannerCanvas.Children.Cast<UIElement>().Where(element => element is FrameworkElement item && Equals(item.Tag, "VanDetail")).ToList();
            foreach (UIElement detail in oldDetails)
            {
                PlannerCanvas.Children.Remove(detail);
            }

            DrawVanDetails();

            List<UIElement> newDetails = PlannerCanvas.Children.Cast<UIElement>().Where(element => element is FrameworkElement item && Equals(item.Tag, "VanDetail")).ToList();
            foreach (UIElement detail in newDetails)
            {
                PlannerCanvas.Children.Remove(detail);
            }

            int insertIndex = _vanFloor != null ? PlannerCanvas.Children.IndexOf(_vanFloor) + 1 : 0;
            foreach (UIElement detail in newDetails)
            {
                PlannerCanvas.Children.Insert(insertIndex++, detail);
            }

            StatusText.Text = "Van geometry updated.";
        }

        private bool TryReadGeometry(out string error)
        {
            error = "Enter valid positive measurements for all geometry fields.";

            if (!double.TryParse(CabLengthBox.Text, out double cab) ||
                !double.TryParse(WheelbaseBox.Text, out double wheelbase) ||
                !double.TryParse(FrontArchOffsetBox.Text, out double frontArch) ||
                !double.TryParse(RearArchOffsetBox.Text, out double rearArch) ||
                !double.TryParse(WheelArchLengthBox.Text, out double archLength) ||
                !double.TryParse(WheelArchWidthBox.Text, out double archWidth) ||
                !double.TryParse(SlidingDoorStartBox.Text, out double doorStart) ||
                !double.TryParse(SlidingDoorLengthBox.Text, out double doorLength))
            {
                return false;
            }

            if (!double.IsFinite(cab) ||
                !double.IsFinite(wheelbase) ||
                !double.IsFinite(frontArch) ||
                !double.IsFinite(rearArch) ||
                !double.IsFinite(archLength) ||
                !double.IsFinite(archWidth) ||
                !double.IsFinite(doorStart) ||
                !double.IsFinite(doorLength))
            {
                return false;
            }

            if (cab < 400 || cab > ViewModel.VanLengthMm * 0.5 ||
                wheelbase <= 0 || wheelbase >= ViewModel.VanLengthMm ||
                archLength <= 0 || archWidth <= 0 ||
                frontArch - archLength / 2.0 < 0 ||
                frontArch + archLength / 2.0 > ViewModel.VanLengthMm ||
                rearArch - archLength / 2.0 < 0 ||
                rearArch + archLength / 2.0 > ViewModel.VanLengthMm ||
                archWidth > ViewModel.VanWidthMm * 0.4 ||
                doorStart < 0 || doorLength <= 0 ||
                doorStart + doorLength > ViewModel.VanLengthMm)
            {
                error = "One or more measurements fall outside the allowed range.";
                return false;
            }

            ViewModel.CabLengthMm = cab;
            ViewModel.WheelbaseMm = wheelbase;
            ViewModel.FrontWheelArchOffsetMm = frontArch;
            ViewModel.RearWheelArchOffsetMm = rearArch;
            ViewModel.WheelArchLengthMm = archLength;
            ViewModel.WheelArchWidthMm = archWidth;
            ViewModel.SlidingDoorStartMm = doorStart;
            ViewModel.SlidingDoorLengthMm = doorLength;

            return true;
        }

        private void OnFurnitureRotated(FurnitureData data)
        {
            if (_selectedObject?.Tag is not FurnitureData selected || selected != data)
            {
                return;
            }

            _selectedObject.Width = data.LengthMm * _scale;
            _selectedObject.Height = data.WidthMm * _scale;
            _selectedObject.RenderTransform = Transform.Identity;

            UpdateObjectLabel(_selectedObject);
            PositionResizeHandle();

            StatusText.Text = $"{data.Name} dimensions swapped.";
        }

        private void OnFurnitureDeleted(FurnitureData data)
        {
            if (_selectedObject?.Tag is not FurnitureData selected || selected != data)
            {
                return;
            }

            PlannerCanvas.Children.Remove(_resizeHandle);
            _resizeHandle = null;

            DeleteSelectedFurnitureVisual();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Save Campervan Layout",
                Filter = "Campervan Layout (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "MyCampervan.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }


            CamperLayout layout = ViewModel.CreateLayout();            

            try
            {
                string json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(dialog.FileName, json);
                StatusText.Text = "Layout saved.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Load Campervan Layout",
                Filter = "Campervan Layout (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(dialog.FileName);
                CamperLayout layout = JsonSerializer.Deserialize<CamperLayout>(json) ?? throw new InvalidDataException("Invalid layout file.");
                ValidateLayout(layout);
                ViewModel.LoadLayout(layout);
                VanLengthBox.Text = ViewModel.VanLengthMm.ToString("0");
                VanWidthBox.Text = ViewModel.VanWidthMm.ToString("0");
                CabLengthBox.Text = ViewModel.CabLengthMm.ToString("0");
                WheelbaseBox.Text = ViewModel.WheelbaseMm.ToString("0");
                FrontArchOffsetBox.Text = ViewModel.FrontWheelArchOffsetMm.ToString("0");
                RearArchOffsetBox.Text = ViewModel.RearWheelArchOffsetMm.ToString("0");
                WheelArchLengthBox.Text = ViewModel.WheelArchLengthMm.ToString("0");
                WheelArchWidthBox.Text = ViewModel.WheelArchWidthMm.ToString("0");
                SlidingDoorStartBox.Text = ViewModel.SlidingDoorStartMm.ToString("0");
                SlidingDoorLengthBox.Text = ViewModel.SlidingDoorLengthMm.ToString("0");

                ResizeFloorToViewport(); StatusText.Text = "Layout loaded successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ValidateLayout(CamperLayout layout)
        {
            if (!double.IsFinite(layout.VanLengthMm) ||
                !double.IsFinite(layout.VanWidthMm) ||
                layout.VanLengthMm < 1000 ||
                layout.VanLengthMm > 10000 ||
                layout.VanWidthMm < 800 ||
                layout.VanWidthMm > 3000 ||
                layout.Objects is null ||
                layout.Objects.Count > 500)
            {
                throw new InvalidDataException("Invalid van dimensions or object list.");
            }

            foreach (FurnitureData item in layout.Objects)
            {
                if (string.IsNullOrWhiteSpace(item.Name) ||
                    item.Name.Length > 100 ||
                    !double.IsFinite(item.XMm) ||
                    !double.IsFinite(item.YMm) ||
                    !double.IsFinite(item.LengthMm) ||
                    !double.IsFinite(item.WidthMm) ||
                    !double.IsFinite(item.Angle) ||
                    item.LengthMm <= 0 ||
                    item.WidthMm <= 0 ||
                    item.LengthMm > layout.VanLengthMm ||
                    item.WidthMm > layout.VanWidthMm ||
                    item.XMm < 0 ||
                    item.YMm < 0 ||
                    item.XMm + item.LengthMm > layout.VanLengthMm ||
                    item.YMm + item.WidthMm > layout.VanWidthMm ||
                    item.Angle % 90 != 0)
                {
                    throw new InvalidDataException("The layout contains invalid furniture data.");
                }
            }
        }
        
        private Brush GetObjectColour(string name)
        {
            return name switch
            {
                "Bed" => Brushes.LightSteelBlue,
                "Kitchen" => Brushes.PeachPuff,
                "Fridge" => Brushes.LightGray,
                "Sink" => Brushes.LightCyan,
                "Cupboard" => Brushes.Tan,
                "Table" => Brushes.PaleGoldenrod,
                "Seat" => Brushes.DarkSeaGreen,
                "Wheel Arch" => Brushes.DarkGray,
                _ => Brushes.BurlyWood
            };
        }

        private void DeleteSelectedFurnitureVisual()
        {
            if (_selectedObject == null)
            {
                return;
            }

            string name = (_selectedObject.Tag as FurnitureData)?.Name ?? "Object";

            PlannerCanvas.Children.Remove(_selectedObject);
            _draggedObject = null;

            ClearSelection();
            StatusText.Text = $"{name} deleted.";
        }

        private void OnFurnitureAdded(FurnitureData data)
        {
            Border item = CreateFurniture(data.Name, data.LengthMm, data.WidthMm, GetObjectColour(data.Name));
            item.Tag = data;

            SetObjectPosition(item, data.XMm, data.YMm);

            PlannerCanvas.Children.Add(item);
            SelectObject(item);

            StatusText.Text = $"{data.Name} added: {data.LengthMm:0} × {data.WidthMm:0} mm.";
        }

        private void OnFurnitureTooLarge(string name)
        {
            MessageBox.Show($"{name} is larger than the current van interior.", "Object too large", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnFurnitureChangesApplied(FurnitureData data)
        {
            if (_selectedObject?.Tag is not FurnitureData selectedData || selectedData != data)
            {
                return;
            }

            _selectedObject.Width = data.LengthMm * _scale;
            _selectedObject.Height = data.WidthMm * _scale;
            _selectedObject.RenderTransform = Transform.Identity;

            SetObjectPosition(_selectedObject, data.XMm, data.YMm);
            UpdateObjectLabel(_selectedObject);
            PositionResizeHandle();

            StatusText.Text = $"{data.Name} updated.";
        }

        private void OnFurnitureChangesRejected()
        {
            MessageBox.Show("Enter valid positions and dimensions in millimetres. The object must fit inside the van.", "Invalid object dimensions", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ClearSelection()
        {
            if (_selectedObject is not null)
            {
                _selectedObject.BorderBrush = Brushes.DimGray;
                _selectedObject.BorderThickness = new Thickness(2);
            }

            _selectedObject = null;
            _draggedObject = null;

            ViewModel.SelectedFurniture = null;

            if (_resizeHandle is not null)
            {
                PlannerCanvas.Children.Remove(_resizeHandle);
                _resizeHandle = null;
            }
        }

        private void OnPrintFloorPlanRequested()
        {
            if (PlannerCanvas.ActualWidth <= 0 || PlannerCanvas.ActualHeight <= 0)
            {
                return;
            }

            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            double pageWidth = printDialog.PrintableAreaWidth;
            double pageHeight = printDialog.PrintableAreaHeight;
            double margin = 30.0;
            double titleHeight = 55.0;

            double availableWidth = pageWidth - margin * 2.0;
            double availableHeight = pageHeight - margin * 2.0 - titleHeight;

            double scale = Math.Min(availableWidth / PlannerCanvas.ActualWidth, availableHeight / PlannerCanvas.ActualHeight);

            double drawingWidth = PlannerCanvas.ActualWidth * scale;
            double drawingHeight = PlannerCanvas.ActualHeight * scale;
            double drawingX = (pageWidth - drawingWidth) / 2.0;
            double drawingY = margin + titleHeight;

            DrawingVisual printVisual = new DrawingVisual();

            using (DrawingContext dc = printVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageWidth, pageHeight));

                FormattedText title = new FormattedText("Campervan Floor Plan", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 20.0, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(title, new Point(margin, margin));

                FormattedText dimensions = new FormattedText($"Van: {ViewModel.VanLengthMm:0} × {ViewModel.VanWidthMm:0} mm", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 10.0, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(dimensions, new Point(margin, margin + 28.0));

                VisualBrush canvasBrush = new VisualBrush(PlannerCanvas)
                {
                    Stretch = Stretch.Fill,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    Viewbox = new Rect(0, 0, PlannerCanvas.ActualWidth, PlannerCanvas.ActualHeight), ViewboxUnits = BrushMappingMode.Absolute
                };

                dc.DrawRectangle(Brushes.White, null, new Rect(drawingX, drawingY, drawingWidth, drawingHeight));
                dc.PushClip(new RectangleGeometry(new Rect(drawingX, drawingY, drawingWidth, drawingHeight)));
                dc.PushTransform(new TranslateTransform(drawingX, drawingY));
                dc.PushTransform(new ScaleTransform(scale, scale));

                foreach (UIElement child in PlannerCanvas.Children)
                {
                    if (child is Border border && border == _vanFloor)
                    {
                        continue;
                    }

                    child.UpdateLayout();

                    VisualBrush childBrush = new VisualBrush(child);
                    Rect bounds = VisualTreeHelper.GetDescendantBounds(child);

                    if (bounds.IsEmpty)
                    {
                        bounds = new Rect(child.RenderSize);
                    }

                    dc.DrawRectangle(childBrush, null, bounds);
                }

                dc.Pop();
                dc.Pop();
                dc.Pop();
            }

            printDialog.PrintVisual(printVisual, "Campervan Floor Plan");
        }
    }
}
