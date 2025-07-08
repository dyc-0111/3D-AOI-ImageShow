using MainFromParameter_WPF.Modules;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MainFromParameter_WPF.Display
{
    public partial class uc2DViewer : UserControl
    {
        #region =====================  EventHandler  =====================

        public event EventHandler<LineDrawnEventArgs> LineDrawn;
        public event EventHandler<LineDrawnEventArgs> LineDrawingPreview;
        public event EventHandler LevelingPointsChanged;
        public event EventHandler DrawROIRequested_Done;

        #endregion

        #region=====================  內部變數  =====================

        private double _zoom = 1.0;
        private const double ZOOM_FACTOR = 1.1;     //可改 值越小越慢
        private const double MIN_ZOOM = 0.1;
        private const double MAX_ZOOM = 10.0;
        private const double DRAG_SPEED_FACTOR = 2; //可改 值越小拖曳越慢

        private bool _isDraggingImage = false;
        private Point _lastDragPoint;
        private Point _startPoint;
        private bool _isOutsideImage = false;
        private bool _enableMouseEvents = true;

        // 線條樣式列舉
        public enum LineStyle
        {
            Horizontal,  // 水平線
            Vertical,    // 垂直線
            Free        // 任意線
        }

        private LineStyle _currentLineStyle = LineStyle.Free;

        // 基準值（針對 1920x1080 的圖片）
        private const double BASE_STROKE_THICKNESS = 4.0;
        private const double BASE_FONT_SIZE = 30.0;
        private const double BASE_NUMBER_OFFSET = 5.0;

        private Line _currentLine = null;
        private bool _isDrawingLine = false;

        private bool _isSelectingROI = false;
        private Rectangle _currentROIBox = null;
        private bool _isLevelingMode = false;  // 新增變數來區分是否為 Leveling 模式

        public List<Rect> lst_ROI = new List<Rect>();
        public List<Rect> lst_LevelingROI = new List<Rect>();  // 新增 Leveling 模式的 ROI 列表

        private bool _isDraggingROI = false;
        private int _draggedROIIndex = -1;
        private Point _dragStartPoint;

        private int _currentEditROIIndex = -1;  // 追蹤當前正在編輯的 ROI 索引

        // ====== Leveling 三點三角形 ======
        private List<Point> LevelingPoints = new List<Point>();
        private List<Ellipse> LevelingPointEllipses = new List<Ellipse>();
        private Polyline LevelingTriangle = null;
        private int DraggingPointIndex = -1;
        public IReadOnlyList<Point> LevelingPointsReadOnly => LevelingPoints.AsReadOnly();

        // ====== 三點三角形 ROI 點選互動 ======
        private List<Point> RoiTrianglePoints = new List<Point>();
        private List<Rectangle> RoiTriangleRects = new List<Rectangle>();
        private Polyline RoiTriangleLines = null;

        private int draggingTrianglePointIndex = -1;

        public uc2DViewer()
        {
            InitializeComponent();
            this.ShowRightButtons = false;
            NewFileButton.Visibility = Visibility.Hidden;
            OpenButton.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
            DeleteButton.Visibility = Visibility.Hidden;
            AddButton.Visibility = Visibility.Hidden;
            SaveButton.Visibility = Visibility.Hidden;
        }

        #endregion

        #region=====================  顏色函式  =====================

        private DropShadowEffect CreateGlowEffect(Color glowColor)
        {
            return new DropShadowEffect
            {
                Color = glowColor,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 15,
                Opacity = 0.9
            };
        }

        private void RgbToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            h = s = v = max;
            double d = max - min;
            s = max == 0 ? 0 : d / max;
            if (max == min)
                h = 0;
            else if (max == r)
                h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g)
                h = (b - r) / d + 2;
            else
                h = (r - g) / d + 4;
            h *= 60;
            h = h % 360;
        }

        private Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double r, g, b;
            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)
            );
        }

        // ====== 主色調分析與對比色選擇 ======
        private Color GetImageDominantColor()
        {
            var bmp = MainImage.Source as BitmapSource;
            if (bmp == null) return Colors.Lime;
            int stride = bmp.PixelWidth * 4;
            int sampleStep = Math.Max(1, Math.Min(bmp.PixelWidth, bmp.PixelHeight) / 32);
            long r = 0, g = 0, b = 0, count = 0;
            for (int y = 0; y < bmp.PixelHeight; y += sampleStep)
            {
                byte[] pixels = new byte[bmp.PixelWidth * 4];
                bmp.CopyPixels(new Int32Rect(0, y, bmp.PixelWidth, 1), pixels, stride, 0);
                for (int x = 0; x < bmp.PixelWidth; x += sampleStep)
                {
                    int idx = x * 4;
                    b += pixels[idx];
                    g += pixels[idx + 1];
                    r += pixels[idx + 2];
                    count++;
                }
            }
            if (count == 0) return Colors.Lime;
            return Color.FromRgb((byte)(r / count), (byte)(g / count), (byte)(b / count));
        }

        private Brush GetContrastBrushColor()
        {
            Color c = GetImageDominantColor();
            double h, s, v;
            RgbToHsv(c, out h, out s, out v);

            // 創建漸變畫筆
            LinearGradientBrush gradientBrush = new LinearGradientBrush();
            gradientBrush.MappingMode = BrushMappingMode.Absolute;
            gradientBrush.SpreadMethod = GradientSpreadMethod.Repeat;

            // 根據主色調選擇對比色
            if (s < 0.2) // 低飽和度（接近灰色）
            {
                if (v < 0.3) // 暗灰色
                {
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Orange, 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 1.0));
                }
                else if (v > 0.7) // 亮灰色
                {
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.DarkBlue, 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Blue, 1.0));
                }
                else // 中灰色
                {
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Red, 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.DarkRed, 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(Colors.Red, 1.0));
                }
            }
            else // 有明顯色調
            {
                // 計算互補色（色相相差180度）
                double complementaryH = (h + 180) % 360;

                // 根據主色調選擇對比色系
                if (h >= 0 && h < 60) // 紅色系
                {
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb((complementaryH + 30) % 360, 1.0, 1.0), 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 1.0));
                }
                else if (h >= 60 && h < 120) // 黃色系
                {
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb((complementaryH + 30) % 360, 1.0, 1.0), 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 1.0));
                }
                else if (h >= 120 && h < 180) // 綠色系
                {
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb((complementaryH + 30) % 360, 1.0, 1.0), 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 1.0));
                }
                else if (h >= 180 && h < 240) // 青色系
                {
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb((complementaryH + 30) % 360, 1.0, 1.0), 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 1.0));
                }
                else if (h >= 240 && h < 300) // 藍色系
                {
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb((complementaryH + 30) % 360, 1.0, 1.0), 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 1.0));
                }
                else // 紫色系
                {
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 0.0));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb((complementaryH + 30) % 360, 1.0, 1.0), 0.5));
                    gradientBrush.GradientStops.Add(new GradientStop(HsvToRgb(complementaryH, 1.0, 1.0), 1.0));
                }
            }

            return gradientBrush;
        }

        #endregion

        #region=====================  自訂屬性  =====================

        [Description("是否啟用滑鼠事件"), Category("自訂")]
        public bool EnableMouseEvents
        {
            get => _enableMouseEvents;
            set => _enableMouseEvents = value;
        }

        [Description("是否顯示右側按鈕"), Category("自訂")]
        public bool ShowRightButtons
        {
            get => RightButtonPanel.Visibility == Visibility.Visible;
            set
            {
                RightButtonPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                
                var mainGrid = (Grid)this.Content;
                var columnDefinitions = mainGrid.ColumnDefinitions;
                
                if (value)
                {
                    // 顯示按鈕時，恢復原始的19:1比例
                    columnDefinitions[0].Width = new GridLength(19, GridUnitType.Star);
                    columnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    // 隱藏按鈕時，讓左側佔據全部寬度
                    columnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    columnDefinitions[1].Width = new GridLength(0, GridUnitType.Star);
                }
            }
        }

        #endregion

        #region=====================  圖片處理  =====================

        private double CalculateScaleFactor(double imageWidth, double imageHeight)
        {
            double diagonal = Math.Sqrt(imageWidth * imageWidth + imageHeight * imageHeight);
            double baseDiagonal = Math.Sqrt(1920 * 1920 + 1080 * 1080);
            return diagonal / baseDiagonal;
        }

        public void LoadImage(string imagePath)
        {
            try
            {
                _zoom = 1.0;
                ImageScale.ScaleX = 1.0;
                ImageScale.ScaleY = 1.0;
                ImageTranslate.X = 0;
                ImageTranslate.Y = 0;
                SelectionBox.Visibility = Visibility.Collapsed;
                SelectionNumber.Visibility = Visibility.Collapsed;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                double scaleFactor = CalculateScaleFactor(bitmap.PixelWidth, bitmap.PixelHeight);

                SelectionBox.StrokeThickness = BASE_STROKE_THICKNESS * scaleFactor;
                SelectionNumber.FontSize = BASE_FONT_SIZE * scaleFactor;

                MainImage.Source = bitmap;
                bitmap.Freeze();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入圖片時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadImage(System.Drawing.Bitmap bitmap)
        {
            try
            {
                _zoom = 1.0;
                ImageScale.ScaleX = 1.0;
                ImageScale.ScaleY = 1.0;
                ImageTranslate.X = 0;
                ImageTranslate.Y = 0;
                SelectionBox.Visibility = Visibility.Collapsed;
                SelectionNumber.Visibility = Visibility.Collapsed;

                var bitmapImage = new BitmapImage();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }

                double scaleFactor = CalculateScaleFactor(bitmapImage.PixelWidth, bitmapImage.PixelHeight);

                SelectionBox.StrokeThickness = BASE_STROKE_THICKNESS * scaleFactor;
                SelectionNumber.FontSize = BASE_FONT_SIZE * scaleFactor;

                MainImage.Source = bitmapImage;
                bitmapImage.Freeze();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入圖片時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearImage()
        {
            MainImage.Source = null;

            _zoom = 1.0;
            ImageScale.ScaleX = 1.0;
            ImageScale.ScaleY = 1.0;
            ImageTranslate.X = 0;
            ImageTranslate.Y = 0;

            SelectionBox.Visibility = Visibility.Collapsed;
            SelectionNumber.Visibility = Visibility.Collapsed;

            _isDraggingImage = false;
            _isOutsideImage = false;
            _startPoint = new Point(0, 0);
            _lastDragPoint = new Point(0, 0);
        }

        public void ClearROI()
        {
            foreach (var child in SelectionCanvas.Children.OfType<Rectangle>().Where(r => r != SelectionBox).ToList())
            {
                SelectionCanvas.Children.Remove(child);
            }
            foreach (var child in SelectionCanvas.Children.OfType<Line>().ToList())
            {
                SelectionCanvas.Children.Remove(child);
            }
            lst_ROI.Clear();
            lst_LevelingROI.Clear();
        }

        #endregion

        #region=====================  滑鼠控制  =====================

        private void MainGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainImage.Source == null || !_enableMouseEvents) return;

            Point mousePosition = e.GetPosition(MainImage);

            double zoomFactor = e.Delta > 0 ? ZOOM_FACTOR : 1.0 / ZOOM_FACTOR;
            double newZoom = _zoom * zoomFactor;

            newZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, newZoom));

            double relativeX = mousePosition.X / _zoom;
            double relativeY = mousePosition.Y / _zoom;

            double newRelativeX = relativeX * newZoom;
            double newRelativeY = relativeY * newZoom;

            double deltaX = newRelativeX - mousePosition.X;
            double deltaY = newRelativeY - mousePosition.Y;

            _zoom = newZoom;
            ImageScale.ScaleX = _zoom;
            ImageScale.ScaleY = _zoom;
            ImageTranslate.X -= deltaX;
            ImageTranslate.Y -= deltaY;
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (MainImage.Source == null || !_enableMouseEvents || _isOutsideImage) return;

            if (_isDraggingROI && _draggedROIIndex >= 0 && _draggedROIIndex < lst_ROI.Count)
            {
                Point currPoint = e.GetPosition(MainImage);
                double deltaX = currPoint.X - _dragStartPoint.X;
                double deltaY = currPoint.Y - _dragStartPoint.Y;

                Rect roi = lst_ROI[_draggedROIIndex];
                double newX = roi.X + deltaX;
                double newY = roi.Y + deltaY;

                // 確保不會超出圖片邊界
                if (newX >= 0 && newX + roi.Width <= MainImage.ActualWidth &&
                    newY >= 0 && newY + roi.Height <= MainImage.ActualHeight)
                {
                    roi.X = newX;
                    roi.Y = newY;
                    lst_ROI[_draggedROIIndex] = roi;
                    _dragStartPoint = currPoint;
                    DrawROIRequested_Done?.Invoke(this, EventArgs.Empty);
                }
                return;
            }

            if (_isDraggingImage)
            {
                Point currPoint = e.GetPosition(MainGrid);
                double deltaX = currPoint.X - _lastDragPoint.X;
                double deltaY = currPoint.Y - _lastDragPoint.Y;

                // 根據影像尺寸和縮放比例動態調整拖曳速度
                var bitmap = MainImage.Source as BitmapSource;
                if (bitmap != null)
                {
                    // 計算影像對角線長度
                    double imageDiagonal = Math.Sqrt(bitmap.PixelWidth * bitmap.PixelWidth + bitmap.PixelHeight * bitmap.PixelHeight);
                    // 基準對角線（以1920x1080為基準）
                    double baseDiagonal = Math.Sqrt(1920 * 1920 + 1080 * 1080);
                    // 計算影像尺寸縮放因子，影像越大因子越大
                    double sizeScaleFactor = Math.Max(0.5, imageDiagonal / baseDiagonal);

                    // 計算縮放比例因子，縮放越大速度越慢
                    double zoomScaleFactor = 1.0 / Math.Max(0.1, _zoom);

                    double dynamicSpeedFactor = DRAG_SPEED_FACTOR *
                        Math.Sqrt(sizeScaleFactor * zoomScaleFactor);

                    deltaX *= dynamicSpeedFactor;
                    deltaY *= dynamicSpeedFactor;
                }

                ImageTranslate.X += deltaX;
                ImageTranslate.Y += deltaY;

                _lastDragPoint = currPoint;
                return;
            }

            Point currentPoint = e.GetPosition(MainImage);
            bool isInImage = currentPoint.X >= 0 && currentPoint.X <= MainImage.ActualWidth &&
                             currentPoint.Y >= 0 && currentPoint.Y <= MainImage.ActualHeight;
            if (!isInImage && !_isDrawingLine) return;

            // 如果從圖片內進入圖片外，重置所有狀態
            if (!_isOutsideImage && !isInImage)
            {
                _isOutsideImage = true;
                if (_isDrawingLine && _currentLine != null)
                {
                    SelectionCanvas.Children.Remove(_currentLine);
                    _currentLine = null;
                }
                MainGrid.ReleaseMouseCapture();
                return;
            }

            if (_isSelectingROI && _currentROIBox != null)
            {
                double left = Math.Min(_startPoint.X, currentPoint.X);
                double top = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);

                // 更新 ROI 框的位置和大小
                Canvas.SetLeft(_currentROIBox, left);
                Canvas.SetTop(_currentROIBox, top);
                _currentROIBox.Width = width;
                _currentROIBox.Height = height;

                // 動態更新發光效果
                if (_currentROIBox.Effect is DropShadowEffect effect)
                {
                    effect.BlurRadius = Math.Max(10, Math.Min(20, Math.Max(width, height) / 10));
                    effect.ShadowDepth = Math.Max(0, Math.Min(5, Math.Max(width, height) / 50));
                }
            }

            if (_isDrawingLine && _currentLine != null)
            {
                switch (_currentLineStyle)
                {
                    case LineStyle.Horizontal:
                        _currentLine.X2 = currentPoint.X;
                        _currentLine.Y2 = _currentLine.Y1; // 保持Y座標不變
                        break;
                    case LineStyle.Vertical:
                        _currentLine.X2 = _currentLine.X1; // 保持X座標不變
                        _currentLine.Y2 = currentPoint.Y;
                        break;
                    case LineStyle.Free:
                        _currentLine.X2 = currentPoint.X;
                        _currentLine.Y2 = currentPoint.Y;
                        break;
                }

                if (_currentLine.Stroke is LinearGradientBrush gradientBrush)
                {
                    double length = Math.Sqrt(
                        Math.Pow(_currentLine.X2 - _currentLine.X1, 2) +
                        Math.Pow(_currentLine.Y2 - _currentLine.Y1, 2));

                    gradientBrush.StartPoint = new Point(0, 0);
                    gradientBrush.EndPoint = new Point(50, 0);
                }
            }
        }

        private void MainGrid_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private void MainGrid_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MainImage.Source == null || !_enableMouseEvents) return;

            Point clickPoint = e.GetPosition(MainImage);

            // ROI 框選模式
            if (SelectROIButton.IsActive)
            {
                // 只允許一個ROI
                if (lst_ROI.Count >= 1)
                    return;
                // 框選矩形ROI
                _isSelectingROI = true;
                _startPoint = clickPoint;

                // 計算自適應線條粗細
                double scaleFactor = CalculateScaleFactor(
                    ((BitmapSource)MainImage.Source).PixelWidth,
                    ((BitmapSource)MainImage.Source).PixelHeight);
                double strokeThickness = Math.Max(2, BASE_STROKE_THICKNESS * scaleFactor);

                _currentROIBox = new Rectangle
                {
                    Stroke = GetContrastBrushColor(),
                    StrokeThickness = strokeThickness,
                    Fill = Brushes.Transparent,
                    Effect = CreateGlowEffect(Colors.Yellow) // 添加發光效果
                };
                Canvas.SetLeft(_currentROIBox, _startPoint.X);
                Canvas.SetTop(_currentROIBox, _startPoint.Y);
                SelectionCanvas.Children.Add(_currentROIBox);
                MainGrid.CaptureMouse();
                return;
            }

            // Leveling 三點三角形模式
            if (LevelingButton.IsActive)
            {
                // 只允許三個點
                if (RoiTrianglePoints.Count >= 3)
                    return;
                LevelingPoints.Add(clickPoint);
                RoiTrianglePoints.Add(clickPoint);
                RedrawRoiTriangle();
                return;
            }

            // Leveling 模式下拖曳 ROI
            if (_isLevelingMode)
            {
                // 檢查是否點擊到 ROI
                for (int i = 0; i < lst_LevelingROI.Count; i++)
                {
                    Rect roi = lst_LevelingROI[i];
                    if (clickPoint.X >= roi.X && clickPoint.X <= roi.X + roi.Width &&
                        clickPoint.Y >= roi.Y && clickPoint.Y <= roi.Y + roi.Height)
                    {
                        _isDraggingROI = true;
                        _draggedROIIndex = i;
                        _dragStartPoint = clickPoint;
                        MainGrid.CaptureMouse();
                        return;
                    }
                }
            }
            else if (LineButton.IsActive)
            {
                bool isInImage = clickPoint.X >= 0 && clickPoint.X <= MainImage.ActualWidth &&
                                clickPoint.Y >= 0 && clickPoint.Y <= MainImage.ActualHeight;

                if (!isInImage)
                {
                    return;
                }

                _isDrawingLine = true;
                _startPoint = clickPoint;
                _isOutsideImage = false;

                _currentLine = new Line
                {
                    Stroke = GetContrastBrushColor(),
                    StrokeThickness = BASE_STROKE_THICKNESS * CalculateScaleFactor(
                        ((BitmapSource)MainImage.Source).PixelWidth,
                        ((BitmapSource)MainImage.Source).PixelHeight),
                    X1 = _startPoint.X,
                    Y1 = _startPoint.Y,
                    X2 = _startPoint.X,
                    Y2 = _startPoint.Y
                };

                SelectionCanvas.Children.Add(_currentLine);
                MainGrid.CaptureMouse();
            }
            else if (EditButton.IsActive)
            {
                EditButton.IsActive = false;
                return;
            }
            else if (AddButton.IsActive)
            {
                MainGrid.CaptureMouse();
            }
        }

        private void MainGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_enableMouseEvents) return;

            if (_isOutsideImage)
            {
                if (_currentLine != null)
                {
                    SelectionCanvas.Children.Remove(_currentLine);
                    _currentLine = null;
                }
                _isDrawingLine = false;
                MainGrid.ReleaseMouseCapture();
                return;
            }

            if (_isDrawingLine && _currentLine != null)
            {
                _isDrawingLine = false;
                _currentLine = null;
                MainGrid.ReleaseMouseCapture();
                LineButton.IsActive = false;
                return;
            }

            if (_isDraggingROI)
            {
                _isDraggingROI = false;
                _draggedROIIndex = -1;
                MainGrid.ReleaseMouseCapture();
                return;
            }

            if (_isDrawingLine || !_enableMouseEvents) return;

            if (_isSelectingROI && _currentROIBox != null)
            {
                Point ptStart = new Point(Canvas.GetLeft(_currentROIBox), Canvas.GetTop(_currentROIBox));
                Point ptEnd = new Point(ptStart.X + _currentROIBox.Width, ptStart.Y + _currentROIBox.Height);
                var pt_Recte = new Rect(ptStart, ptEnd);

                if (_isLevelingMode)
                {
                    // Leveling 模式：更新或添加特定索引的 ROI
                    if (_currentEditROIIndex >= 0)
                    {
                        // 確保 ROI1 存在
                        if (_currentEditROIIndex > 0 && lst_LevelingROI.Count == 0)
                        {
                            MessageBox.Show("請先建立 ROI1", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            _isSelectingROI = false;
                            SelectROIButton.IsActive = false;
                            SelectionCanvas.Children.Remove(_currentROIBox);
                            MainGrid.ReleaseMouseCapture();
                            _currentEditROIIndex = -1;
                            return;
                        }

                        while (lst_LevelingROI.Count <= _currentEditROIIndex)
                        {
                            lst_LevelingROI.Add(new Rect());
                        }
                        lst_LevelingROI[_currentEditROIIndex] = pt_Recte;
                        // Leveling 模式下，只通知更新特定索引的 ROI
                        DrawROIRequested_Done?.Invoke(this, new ROIEventArgs(_currentEditROIIndex));
                    }
                }
                else
                {
                    // 單獨框取模式：清除所有 ROI 並添加新的
                    lst_ROI.Clear();
                    lst_ROI.Add(pt_Recte);

                    // 創建新的 ROI 框來顯示
                    var roiBox = new Rectangle
                    {
                        Stroke = GetContrastBrushColor(),
                        StrokeThickness = _currentROIBox.StrokeThickness,
                        Fill = Brushes.Transparent,
                        Width = _currentROIBox.Width,
                        Height = _currentROIBox.Height,
                        Effect = CreateGlowEffect(Colors.Yellow)
                    };

                    Canvas.SetLeft(roiBox, ptStart.X);
                    Canvas.SetTop(roiBox, ptStart.Y);
                    SelectionCanvas.Children.Add(roiBox);

                    // 單獨框取模式下，通知更新所有 ROI
                    DrawROIRequested_Done?.Invoke(this, new ROIEventArgs(-1));
                }

                _isSelectingROI = false;
                SelectROIButton.IsActive = false;
                SelectionCanvas.Children.Remove(_currentROIBox);
                MainGrid.ReleaseMouseCapture();
                _currentEditROIIndex = -1;
            }

        }

        private void MainGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MainImage.Source == null || !_enableMouseEvents) return;

            _isDraggingImage = true;
            _lastDragPoint = e.GetPosition(MainGrid);
            MainGrid.CaptureMouse();
        }

        private void MainGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingImage = false;
            MainGrid.ReleaseMouseCapture();
        }

        #endregion

        #region=====================  按鈕觸發事件  =====================

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainImage.Source == null) return;

            _zoom = 1.0;
            ImageScale.ScaleX = 1.0;
            ImageScale.ScaleY = 1.0;
            ImageTranslate.X = 0;
            ImageTranslate.Y = 0;
        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "選擇影像檔案",
                Filter = "圖片檔案|*.jpg;*.jpeg;*.png;*.bmp|所有檔案|*.*",
                RestoreDirectory = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadImage(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"載入圖片時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入圖片時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditButton.IsActive = !EditButton.IsActive;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteButton.IsActive = !DeleteButton.IsActive;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.IsActive = !AddButton.IsActive;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
        }

        public void LineButton_Click(object sender, RoutedEventArgs e)
        {
            var linesToRemove = SelectionCanvas.Children.OfType<Line>().ToList();
            foreach (var line in linesToRemove)
            {
                SelectionCanvas.Children.Remove(line);
            }
            _currentLine = null;

            EditButton.IsActive = false;
            AddButton.IsActive = false;
            LineButton.IsActive = !LineButton.IsActive;

            if (LineButton.IsActive)
            {
                // 創建線條樣式選擇選單
                ContextMenu menu = new ContextMenu();
                
                MenuItem horizontalItem = new MenuItem { Header = "水平線" };
                horizontalItem.Click += (s, args) => 
                { 
                    _currentLineStyle = LineStyle.Horizontal;
                    MainGrid.Cursor = Cursors.SizeWE; // 水平線使用水平調整游標
                };
                
                MenuItem verticalItem = new MenuItem { Header = "垂直線" };
                verticalItem.Click += (s, args) => 
                { 
                    _currentLineStyle = LineStyle.Vertical;
                    MainGrid.Cursor = Cursors.SizeNS; // 垂直線使用垂直調整游標
                };
                
                MenuItem freeItem = new MenuItem { Header = "任意線" };
                freeItem.Click += (s, args) => 
                { 
                    _currentLineStyle = LineStyle.Free;
                    MainGrid.Cursor = Cursors.Cross; // 任意線使用十字游標
                };

                menu.Items.Add(horizontalItem);
                menu.Items.Add(verticalItem);
                menu.Items.Add(freeItem);

                // 顯示選單
                menu.PlacementTarget = LineButton;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
            else
            {
                // 當關閉畫線模式時，恢復默認游標
                MainGrid.Cursor = Cursors.Arrow;
            }
        }

        private void SelectROIButton_Click(object sender, RoutedEventArgs e)
        {
            // 關閉 Leveling 模式
            LevelingButton.IsActive = false;
            ClearRoiTriangle();

            // 啟用/關閉 ROI 框選模式
            SelectROIButton.IsActive = !SelectROIButton.IsActive;
            if (!SelectROIButton.IsActive)
                ClearROI();
        }

        public void LevelingButton_Click(object sender, RoutedEventArgs e)
        {
            SelectROIButton.IsActive = false;
            ClearROI();

            // 啟用/關閉 Leveling 模式
            LevelingButton.IsActive = !LevelingButton.IsActive;
            if (!LevelingButton.IsActive)
            {
                LevelingPoints.Clear();
                ClearRoiTriangle();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearROI();
            ClearRoiTriangle();
            ClearLevelingTriangle();
            LevelingPoints.Clear();
        }

        #endregion

        #region=====================  獲取影像  =====================
        /// <summary>
        /// 獲取 MainGrid 的影像
        /// </summary>
        /// <returns>返回 MainGrid 的 BitmapSource</returns>
        public BitmapSource GetMainGridImage()
        {
            try
            {
                // 獲取 MainGrid 的實際大小
                double dpiX = 96.0;
                double dpiY = 96.0;

                // 創建 RenderTargetBitmap
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    (int)MainGrid.ActualWidth,
                    (int)MainGrid.ActualHeight,
                    dpiX,
                    dpiY,
                    PixelFormats.Pbgra32);

                // 渲染 MainGrid
                renderBitmap.Render(MainGrid);

                // 返回渲染後的影像
                return renderBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"獲取影像時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// 獲取 MainGrid 的影像並保存到檔案
        /// </summary>
        /// <param name="filePath">保存路徑</param>
        /// <returns>是否保存成功</returns>
        public bool SaveMainGridImage(string filePath)
        {
            try
            {
                BitmapSource bitmap = GetMainGridImage();
                if (bitmap == null) return false;

                // 創建編碼器
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                // 保存檔案
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存影像時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion

        #region=====================  框取ROI  =====================

        public void ToggleSelectROI(int roiIndex = -1)
        {
            // 關閉其他模式
            EditButton.IsActive = false;
            AddButton.IsActive = false;
            LineButton.IsActive = false;

            // 切換 ROI 模式
            _isSelectingROI = !_isSelectingROI;
            SelectROIButton.IsActive = _isSelectingROI;
            _currentEditROIIndex = roiIndex;
            _isLevelingMode = roiIndex >= 0;  // Leveling 模式

            if (!_isSelectingROI && _currentROIBox != null)
            {
                SelectionCanvas.Children.Remove(_currentROIBox);
                _currentROIBox = null;
            }

            // 清除當前的線
            if (_currentLine != null)
            {
                SelectionCanvas.Children.Remove(_currentLine);
                _currentLine = null;
            }
            _isDrawingLine = false;

            // ====== Leveling 三點三角形模式 ======
            if (_isLevelingMode && _isSelectingROI)
            {
                // 預設三點為三角形（可根據圖片大小調整）
                var img = MainImage.Source as BitmapSource;
                double w = img != null ? img.PixelWidth : 400;
                double h = img != null ? img.PixelHeight : 400;
                LevelingPoints = new List<Point>
                {
                    new Point(w * 0.3, h * 0.3),
                    new Point(w * 0.7, h * 0.3),
                    new Point(w * 0.5, h * 0.7)
                };
                ShowLevelingTriangle();
            }
            else
            {
                ClearLevelingTriangle();
            }
        }

        #endregion

        #region=====================  畫線  =====================

        public class LineDrawnEventArgs : EventArgs
        {
            public Point StartPoint { get; set; }
            public Point EndPoint { get; set; }

            public LineDrawnEventArgs(Point start, Point end)
            {
                StartPoint = start;
                EndPoint = end;
            }
        }

        public void StartDrawLineMode()
        {
            if (_currentLine != null)
            {
                SelectionCanvas.Children.Remove(_currentLine);
                _currentLine = null;
            }

            // 關閉其他模式
            EditButton.IsActive = false;
            AddButton.IsActive = false;

            // 啟用畫線模式
            LineButton.IsActive = true;
        }

        #endregion

        #region=====================  三點Leveling  =====================

        // ====== 計算自適應縮放因子 ======
        private double GetTriangleScaleFactor()
        {
            var img = MainImage.Source as BitmapSource;
            if (img == null) return 1.0;
            double diag = Math.Sqrt(img.PixelWidth * img.PixelWidth + img.PixelHeight * img.PixelHeight);
            double baseDiag = Math.Sqrt(800 * 800 + 600 * 600); // 以 800x600 為基準
            double scale = diag / baseDiag;
            return Math.Max(1.0, scale); // 最小為1
        }

        // ====== Leveling 三點三角形繪製 ======
        private void ShowLevelingTriangle()
        {
            // 清除舊的
            foreach (var ell in LevelingPointEllipses)
                SelectionCanvas.Children.Remove(ell);
            LevelingPointEllipses.Clear();
            if (LevelingTriangle != null)
                SelectionCanvas.Children.Remove(LevelingTriangle);

            if (LevelingPoints.Count != 3)
                return;

            // 畫三角形
            LevelingTriangle = new Polyline
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 8, 8 },
                Points = new PointCollection(LevelingPoints) { LevelingPoints[0] }
            };
            SelectionCanvas.Children.Add(LevelingTriangle);

            // 畫三個圓點
            for (int i = 0; i < 3; i++)
            {
                var ell = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Effect = CreateGlowEffect(Colors.Blue)
                };
                Canvas.SetLeft(ell, LevelingPoints[i].X - 9);
                Canvas.SetTop(ell, LevelingPoints[i].Y - 9);
                ell.Tag = i;
                ell.MouseLeftButtonDown += LevelingPoint_MouseLeftButtonDown;
                ell.MouseMove += LevelingPoint_MouseMove;
                ell.MouseLeftButtonUp += LevelingPoint_MouseLeftButtonUp;
                LevelingPointEllipses.Add(ell);
                SelectionCanvas.Children.Add(ell);
            }
        }
        private void ClearLevelingTriangle()
        {
            foreach (var ell in LevelingPointEllipses)
                SelectionCanvas.Children.Remove(ell);
            LevelingPointEllipses.Clear();
            if (LevelingTriangle != null)
            {
                SelectionCanvas.Children.Remove(LevelingTriangle);
                LevelingTriangle = null;
            }
        }
        private void LevelingPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ell = sender as Ellipse;
            DraggingPointIndex = (int)ell.Tag;
            ell.CaptureMouse();
            e.Handled = true;
        }
        private void LevelingPoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (DraggingPointIndex == -1) return;
            var pos = e.GetPosition(SelectionCanvas);
            LevelingPoints[DraggingPointIndex] = pos;
            ShowLevelingTriangle();
        }
        private void LevelingPoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DraggingPointIndex = -1;
            (sender as Ellipse).ReleaseMouseCapture();
        }

        // ====== 清除三點三角形 ROI ======
        private void ClearRoiTriangle()
        {
            foreach (var rect in RoiTriangleRects)
                SelectionCanvas.Children.Remove(rect);
            RoiTriangleRects.Clear();
            RoiTrianglePoints.Clear();
            if (RoiTriangleLines != null)
            {
                SelectionCanvas.Children.Remove(RoiTriangleLines);
                RoiTriangleLines = null;
            }
        }

        // ====== 拖曳三"點"事件 ======
        private void RoiTriangleRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ellipse = sender as Ellipse;
            if (ellipse == null) return;

            draggingTrianglePointIndex = (int)ellipse.Tag;
            ellipse.CaptureMouse();
            e.Handled = true;
        }

        private void RoiTriangleRect_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingTrianglePointIndex == -1) return;
            var ellipse = sender as Ellipse;
            if (ellipse == null) return;

            var pos = e.GetPosition(SelectionCanvas);
            UpdateTrianglePoint(draggingTrianglePointIndex, pos);
        }

        private void RoiTriangleRect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            draggingTrianglePointIndex = -1;
            ((UIElement)sender).ReleaseMouseCapture();
            LevelingPointsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTrianglePoint(int index, Point newPosition)
        {
            if (index < 0 || index >= RoiTrianglePoints.Count) return;

            RoiTrianglePoints[index] = newPosition;
            LevelingPoints[index] = newPosition;
            RedrawRoiTriangle();
        }

        // ====== 重繪三角形點與線 ======

        private class DrawingParameters
        {
            public double RectSize { get; set; }
            public double RectStroke { get; set; }
            public double LineStroke { get; set; }
            public double HitSize { get; set; }
        }

        private void RedrawRoiTriangle()
        {
            ClearTriangleElements();

            if (RoiTrianglePoints.Count == 0) return;

            double scaleFactor = GetTriangleScaleFactor();
            var drawingParams = GetDrawingParameters(scaleFactor);

            DrawTrianglePoints(drawingParams);
            DrawTriangleLines(drawingParams);
        }

        private void ClearTriangleElements()
        {
            foreach (var rect in RoiTriangleRects)
                SelectionCanvas.Children.Remove(rect);
            RoiTriangleRects.Clear();

            if (RoiTriangleLines != null)
            {
                SelectionCanvas.Children.Remove(RoiTriangleLines);
                RoiTriangleLines = null;
            }
        }

        private DrawingParameters GetDrawingParameters(double scaleFactor)
        {
            double rectSize = 8 * scaleFactor;
            double rectStroke = 2 * scaleFactor;
            double lineStroke = 3 * scaleFactor;
            double hitSize = Math.Max(24, rectSize * 2);

            return new DrawingParameters
            {
                RectSize = rectSize,
                RectStroke = rectStroke,
                LineStroke = lineStroke,
                HitSize = hitSize
            };
        }

        private void DrawTrianglePoints(DrawingParameters drawingParams)
        {
            for (int i = 0; i < RoiTrianglePoints.Count; i++)
            {
                var pt = RoiTrianglePoints[i];
                DrawHitArea(pt, i, drawingParams.HitSize);
                DrawVisualPoint(pt, i, drawingParams.RectSize, drawingParams.RectStroke);
            }
        }

        private void DrawHitArea(Point pt, int index, double hitSize)
        {
            var hitEllipse = new Ellipse
            {
                Width = hitSize,
                Height = hitSize,
                Fill = Brushes.Transparent,
                StrokeThickness = 0,
                Stroke = null,
                Cursor = Cursors.Hand,
                Tag = index
            };

            Canvas.SetLeft(hitEllipse, pt.X - hitSize / 2);
            Canvas.SetTop(hitEllipse, pt.Y - hitSize / 2);

            hitEllipse.MouseLeftButtonDown += RoiTriangleRect_MouseLeftButtonDown;
            hitEllipse.MouseMove += RoiTriangleRect_MouseMove;
            hitEllipse.MouseLeftButtonUp += RoiTriangleRect_MouseLeftButtonUp;

            SelectionCanvas.Children.Add(hitEllipse);
        }

        private void DrawVisualPoint(Point pt, int index, double rectSize, double rectStroke)
        {
            var rect = new Rectangle
            {
                Width = rectSize,
                Height = rectSize,
                Stroke = Brushes.White,
                StrokeThickness = rectStroke,
                Fill = Brushes.Transparent,
                Tag = index,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rect, pt.X - rectSize / 2);
            Canvas.SetTop(rect, pt.Y - rectSize / 2);

            SelectionCanvas.Children.Add(rect);
            RoiTriangleRects.Add(rect);
        }

        private void DrawTriangleLines(DrawingParameters drawingParams)
        {
            if (RoiTrianglePoints.Count <= 1) return;

            RoiTriangleLines = new Polyline
            {
                Stroke = GetContrastBrushColor(),
                StrokeThickness = drawingParams.LineStroke,
                StrokeDashArray = new DoubleCollection { 0.1 * drawingParams.RectSize, 0.1 * drawingParams.RectSize },
                Points = new PointCollection(RoiTrianglePoints)
            };

            if (RoiTrianglePoints.Count == 3)
                RoiTriangleLines.Points.Add(RoiTrianglePoints[0]);

            SelectionCanvas.Children.Add(RoiTriangleLines);
        }

        #endregion
    }
}