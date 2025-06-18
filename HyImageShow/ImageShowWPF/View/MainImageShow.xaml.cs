using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Linq;

namespace HyImageShow.ImageShow
{
    /// <summary>
    /// ImageShow.xaml 的互動邏輯
    /// </summary>
    public partial class MainImageShow : UserControl
    {
        public MainImageShow()
        {
            InitializeComponent();
            MainCanvas.SizeChanged += MainCanvas_SizeChanged;
            MainCanvasGrid.SizeChanged += MainCanvasGrid_SizeChanged;
            this.KeyDown += MainImageShow_KeyDown;
        }

        public ImageShowViewModel ViewModel => DataContext as ImageShowViewModel;

        private bool isDraggingCanvas = false;
        private Point lastMousePosition;

        private bool showCrossLines = false;

        private Line crossLineHorizontal = null;
        private Line crossLineVertical = null;

        private Ellipse overlayCenterDot = null;
        private Storyboard centerDotStoryboard = null;

        private bool isRulerMode = false;
        private Ellipse rulerPoint1 = null, rulerPoint2 = null;
        private Line rulerLine = null;
        private TextBlock rulerLabel = null;
        private bool isDraggingRulerPoint1 = false, isDraggingRulerPoint2 = false;
        private Point rulerP1, rulerP2;
        private int rulerClickCount = 0;

        private bool isDraggingRulerLine = false;
        private Point lastRulerDragPos;

        private ScaleTransform rulerPoint1Scale = new ScaleTransform(1, 1);
        private ScaleTransform rulerPoint2Scale = new ScaleTransform(1, 1);
        private Polygon rulerArrow = null;
        private DropShadowEffect rulerGlow = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 };
        private DropShadowEffect rulerDotGlow = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 };
        private DropShadowEffect rulerLabelShadow = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 };

        private Border rulerLabelBorder = null;

        private Ellipse rulerArrowDot = null;

        private Border rulerPoint1Label = null;
        private Border rulerPoint2Label = null;

        // 旋轉ROI相關
        private bool isRotRectRoiMode = false;
        private Polygon rotRectRoi = null;
        private Ellipse[] rotRectRoiCornerDots = new Ellipse[4];
        private Ellipse rotRectRoiRotateDot = null;
        private Point rotRectCenter;
        private double rotRectWidth = 120, rotRectHeight = 80, rotRectAngle = 0;
        private bool isDraggingRotRect = false;
        private bool isDraggingRotRectCorner = false;
        private int draggingCornerIndex = -1;
        private bool isDraggingRotRectRotate = false;
        private Point lastRotRectDragPos;
        private int rotRectRoiClickCount = 0;

        // 圓形ROI相關
        private bool isEllipseRoiMode = false;
        private Ellipse ellipseRoi = null;
        private Ellipse ellipseRoiCenterDot = null;
        private Ellipse ellipseRoiRadiusDot = null;
        private Point ellipseRoiCenter;
        private double ellipseRoiRadius = 60;
        private bool isDraggingEllipseRoiCenter = false;
        private bool isDraggingEllipseRoiRadius = false;
        private Point ellipseRoiDragStart;
        private Point ellipseRoiDragStartCenter;
        private double ellipseRoiDragStartRadius;
        private int ellipseRoiClickCount = 0;

        // 畫線功能
        private bool isDrawLineMode = false;
        private Line drawLine = null;
        private Ellipse drawLinePoint1 = null, drawLinePoint2 = null;
        private Point drawLineP1, drawLineP2;
        private bool isDraggingDrawLineP1 = false, isDraggingDrawLineP2 = false;
        private bool isDraggingDrawLine = false;
        private Point lastDrawLineDragPos;
        private int drawLineClickCount = 0;
        private bool isDrawingLine = false;
        private TextBlock drawLineLabel = null;
        private Border drawLineLabelBorder = null;
        private Border drawLinePoint1Label = null;
        private Border drawLinePoint2Label = null;
        private ScaleTransform drawLinePoint1Scale = new ScaleTransform(1, 1);
        private ScaleTransform drawLinePoint2Scale = new ScaleTransform(1, 1);

        private bool isDrawingRulerLine = false;

        // 多邊形ROI功能
        private bool isPolygonRoiMode = false;
        private Polyline polygonRoi = null;
        private List<Ellipse> polygonRoiDots = new List<Ellipse>();
        private List<Point> polygonRoiPoints = new List<Point>();
        private int draggingPolygonDotIndex = -1;
        private bool isDraggingPolygonDot = false;
        private bool isPolygonClosed = false;
        private bool isDraggingPolygonBody = false;
        private Point polygonDragStart;
        private List<Point> polygonDragStartPoints = null;

        // 弧形ROI功能
        private bool isArcRoiMode = false;
        private Path arcRoi = null;
        private Ellipse[] arcRoiDots = new Ellipse[3];  // 三個控制點
        private Point[] arcRoiPoints = new Point[3];    // 三個點的座標
        private int arcRoiClickCount = 0;               // 點擊計數
        private bool isDraggingArcDot = false;          // 是否正在拖曳控制點
        private int draggingArcDotIndex = -1;           // 正在拖曳的控制點索引
        private Line arcRoiPreviewLine = null;          // 預覽線

        
        private bool IsPointInPolygon(Point p, Point[] poly)
        {
            int n = poly.Length;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                    (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                    inside = !inside;
            }
            return inside;
        }
        private Point rotRectDragStart;
        private Point rotRectDragStartCenter;
        private double rotRectDragStartWidth, rotRectDragStartHeight, rotRectDragStartAngle;

        private bool showLabels = true; 

        // ROI 統一樣式字典
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict =
            new Dictionary<string, (Color, Color, Color, Color)>
        {
            {"ruler",   (Color.FromRgb(255, 82, 82), Color.FromRgb(211, 47, 47), Color.FromArgb(220, 255, 205, 210), Color.FromRgb(183, 28, 28))}, // 紅
            {"rotrect", (Color.FromRgb(33, 150, 243), Color.FromRgb(25, 118, 210), Color.FromArgb(220, 187, 222, 251), Color.FromRgb(13, 71, 161))}, // 藍
            {"ellipse", (Color.FromRgb(140, 90, 220), Color.FromRgb(106, 58, 177), Color.FromArgb(220, 210, 180, 255), Color.FromRgb(106, 58, 177))}, // 紫
            {"polygon", (Color.FromRgb(220, 180, 40), Color.FromRgb(191, 160, 0), Color.FromArgb(220, 240, 220, 120), Color.FromRgb(106, 74, 20))}, // 黃
            {"arc",     (Color.FromRgb(40, 170, 110), Color.FromRgb(30, 122, 74), Color.FromArgb(220, 180, 240, 200), Color.FromRgb(30, 122, 74))}, // 綠
            {"line",    (Color.FromRgb(0, 188, 212), Color.FromRgb(0, 150, 167), Color.FromArgb(220, 178, 235, 242), Color.FromRgb(0, 105, 120))}, // 青
        };

        private void MainImageShow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3 && (isRulerMode || isRotRectRoiMode || isDrawLineMode ||
                isEllipseRoiMode || isPolygonRoiMode || isArcRoiMode)) // 所有ROI模式下都可切換
            {
                showLabels = !showLabels;
                if (isRulerMode) DrawRuler();
                if (isRotRectRoiMode) DrawRotRectRoi();
                if (isDrawLineMode) DrawLineOnCanvas();
                if (isEllipseRoiMode) DrawEllipseRoi();
                if (isPolygonRoiMode) DrawPolygonRoi();
                if (isArcRoiMode) DrawArcRoi();
            }
        }

        // 1. 旋轉ROI
        private ScaleTransform[] rotRectRoiCornerScales = { new ScaleTransform(1, 1), new ScaleTransform(1, 1), new ScaleTransform(1, 1), new ScaleTransform(1, 1) };
        private ScaleTransform rotRectRoiRotateScale = new ScaleTransform(1, 1);
        // 2. 圓形ROI
        private ScaleTransform ellipseRoiCenterScale = new ScaleTransform(1, 1);
        private ScaleTransform ellipseRoiRadiusScale = new ScaleTransform(1, 1);
        // 3. 多邊形ROI
        private List<ScaleTransform> polygonRoiDotScales = new List<ScaleTransform>();
        // 4. 弧形ROI
        private ScaleTransform[] arcRoiDotScales = { new ScaleTransform(1, 1), new ScaleTransform(1, 1), new ScaleTransform(1, 1) };

        #region Canvas Mouse Wheel

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? 1.1 : 0.9;
            Point mousePosition = e.GetPosition(MainCanvasGrid); // 获取鼠标相对 DragCanvas 的坐标

            Zoom(zoomFactor, mousePosition);
        }

        private void Zoom(double zoomFactor, Point mousePosition)
        {
            // 计算缩放前鼠标的相对位置
            double prevX = (mousePosition.X - MainCanvasTranslateTransform.X) / MainCanvasScaleTransform.ScaleX;
            double prevY = (mousePosition.Y - MainCanvasTranslateTransform.Y) / MainCanvasScaleTransform.ScaleY;

            // 执行缩放
            double newScaleX = MainCanvasScaleTransform.ScaleX * zoomFactor;
            double newScaleY = MainCanvasScaleTransform.ScaleY * zoomFactor;

            // 限制缩放比例在合理范围内
            if (newScaleX < 0.01) newScaleX = 0.01;
            if (newScaleX > 100) newScaleX = 100;
            if (newScaleY < 0.01) newScaleY = 0.01;
            if (newScaleY > 100) newScaleY = 100;

            MainCanvasScaleTransform.ScaleX = newScaleX;
            MainCanvasScaleTransform.ScaleY = newScaleY;

            // 计算缩放后鼠标的相对位置
            double newX = prevX * MainCanvasScaleTransform.ScaleX + MainCanvasTranslateTransform.X;
            double newY = prevY * MainCanvasScaleTransform.ScaleY + MainCanvasTranslateTransform.Y;

            // 通过 TranslateTransform 使缩放后的鼠标位置与缩放前一致
            MainCanvasTranslateTransform.X -= (newX - mousePosition.X);
            MainCanvasTranslateTransform.Y -= (newY - mousePosition.Y);
        }

        #endregion

        #region Canvas Left Mouse

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingPolygonDot)
            {
                Point p = e.GetPosition(MainCanvas);
                if (polygonRoiPoints.Count > 0)
                {
                    // 檢查是否點擊到第一個點（允許5像素的誤差）
                    Point firstPoint = polygonRoiPoints[0];
                    if (Math.Abs(p.X - firstPoint.X) < 5 && Math.Abs(p.Y - firstPoint.Y) < 5)
                    {
                        isPolygonClosed = true;
                        DrawPolygonRoi();
                        return;
                    }
                }
                polygonRoiPoints.Add(p);
                DrawPolygonRoi();
            }
            else if (isDrawingRulerLine)
            {
                Point p = e.GetPosition(MainCanvas);
                rulerP1 = p;
                rulerP2 = p;
                DrawRuler();
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        #endregion

        #region Canvas Mouse

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = true;
                lastMousePosition = e.GetPosition(MainCanvasGrid);

                MainCanvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingCanvas)
            {
                var currentPosition = e.GetPosition(MainCanvasGrid);
                var offset = currentPosition - lastMousePosition;

                MainCanvasTranslateTransform.X += offset.X;
                MainCanvasTranslateTransform.Y += offset.Y;

                lastMousePosition = currentPosition;
            }
            // 顯示滑鼠座標
            Point p = e.GetPosition(MainCanvas);
            MousePositionText.Text = $"X: {p.X:F0}, Y: {p.Y:F0}";
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = false;
                MainCanvas.ReleaseMouseCapture();
            }
        }

        #endregion

        #region File

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            // 建立一個 1920x1080 白色空白圖
            int width = 1920;
            int height = 1080;
            var wb = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = wb.BackBufferStride;
            byte[] pixels = new byte[height * stride];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;     // B
                pixels[i + 1] = 255; // G
                pixels[i + 2] = 255; // R
                pixels[i + 3] = 255; // A
            }
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            ViewModel.ImageSource = wb;
            Fit();
        }
        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG 圖片 (*.png)|*.png|JPEG 圖片 (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP 圖片 (*.bmp)|*.bmp",
                FileName = "image.png"
            };
            if (dialog.ShowDialog() == true)
            {
                double areaW = MainCanvasGrid.ActualWidth;
                double areaH = MainCanvasGrid.ActualHeight;
                var rtb = new RenderTargetBitmap((int)areaW, (int)areaH, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(MainCanvasGrid);

                using (var fileStream = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                {
                    System.Windows.Media.Imaging.BitmapEncoder encoder;
                    if (dialog.FilterIndex == 2)
                        encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                    else if (dialog.FilterIndex == 3)
                        encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                    else
                        encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    encoder.Save(fileStream);
                }
                MessageBox.Show("儲存成功！");
            }
        }
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "圖片檔案 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                ViewModel?.LoadImage(dialog.FileName);
                Fit();
            }
        }

        #endregion

        #region Fit
        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            Fit();
        }

        private void Fit()
        {
            if (ViewModel?.ImageSource == null)
            {
                return;
            }

            // 獲取圖片和視窗的尺寸
            double imageWidth = ViewModel.ImageSource.Width;
            double imageHeight = ViewModel.ImageSource.Height;
            double windowWidth = MainCanvasGrid.ActualWidth;
            double windowHeight = MainCanvasGrid.ActualHeight;

            // 計算縮放比例，使圖片完整顯示在視窗中
            double scaleX = windowWidth / imageWidth;
            double scaleY = windowHeight / imageHeight;
            double scale = Math.Min(scaleX, scaleY);

            // 設置縮放
            MainCanvasScaleTransform.ScaleX = scale;
            MainCanvasScaleTransform.ScaleY = scale;

            // 計算置中所需的位移
            double scaledWidth = imageWidth * scale;
            double scaledHeight = imageHeight * scale;
            double offsetX = (windowWidth - scaledWidth) / 2;
            double offsetY = (windowHeight - scaledHeight) / 2;

            // 設置位移
            MainCanvasTranslateTransform.X = offsetX;
            MainCanvasTranslateTransform.Y = offsetY;
        }

        #endregion

        #region CrossLines

        private void ShowCrossLinesButton_Click(object sender, RoutedEventArgs e)
        {
            showCrossLines = !showCrossLines;
            if (showCrossLines)
            {
                DrawOverlayCrossLines();
            }
            else
            {
                RemoveOverlayCrossLines();
            }
            SetButtonActiveState(sender as Button, showCrossLines);
        }

        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (showCrossLines)
                DrawCrossLines();
        }

        private void MainCanvasGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (showCrossLines)
                DrawOverlayCrossLines();
        }

        private void DrawCrossLines()
        {
            RemoveCrossLines();
            double width = MainCanvasGrid.ActualWidth;
            double height = MainCanvasGrid.ActualHeight;
            if (width == 0 || height == 0)
            {
                MainCanvasGrid.Loaded += MainCanvasGrid_LoadedForCross;
                return;
            }
            crossLineHorizontal = new Line
            {
                X1 = 0,
                Y1 = height / 2,
                X2 = width,
                Y2 = height / 2,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            crossLineVertical = new Line
            {
                X1 = width / 2,
                Y1 = 0,
                X2 = width / 2,
                Y2 = height,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            MainCanvas.Children.Add(crossLineHorizontal);
            MainCanvas.Children.Add(crossLineVertical);
        }

        private void RemoveCrossLines()
        {
            if (crossLineHorizontal != null)
            {
                MainCanvas.Children.Remove(crossLineHorizontal);
                crossLineHorizontal = null;
            }
            if (crossLineVertical != null)
            {
                MainCanvas.Children.Remove(crossLineVertical);
                crossLineVertical = null;
            }
            showCrossLines = false;
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "十字中心")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }

        private void MainCanvasGrid_LoadedForCross(object sender, RoutedEventArgs e)
        {
            MainCanvasGrid.Loaded -= MainCanvasGrid_LoadedForCross;
            if (showCrossLines)
                DrawCrossLines();
        }

        private void DrawOverlayCrossLines()
        {
            RemoveOverlayCrossLines();
            double width = MainCanvasGrid.ActualWidth;
            double height = MainCanvasGrid.ActualHeight;
            if (width == 0 || height == 0)
            {
                MainCanvasGrid.Loaded += MainCanvasGrid_LoadedForOverlayCross;
                return;
            }
            double gap = 18; // 缺口長度
            double centerX = width / 2;
            double centerY = height / 2;
            // 水平線（左）
            var hLineLeft = new Line
            {
                X1 = 0,
                Y1 = centerY,
                X2 = centerX - gap,
                Y2 = centerY,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };
            // 水平線（右）
            var hLineRight = new Line
            {
                X1 = centerX + gap,
                Y1 = centerY,
                X2 = width,
                Y2 = centerY,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };
            // 垂直線（上）
            var vLineTop = new Line
            {
                X1 = centerX,
                Y1 = 0,
                X2 = centerX,
                Y2 = centerY - gap,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };
            // 垂直線（下）
            var vLineBottom = new Line
            {
                X1 = centerX,
                Y1 = centerY + gap,
                X2 = centerX,
                Y2 = height,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };
            // 中心圓點
            overlayCenterDot = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.White,
                Stroke = Brushes.Red,
                StrokeThickness = 2.5,
                Opacity = 0.9,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.8 }
            };
            Canvas.SetLeft(overlayCenterDot, centerX - 8);
            Canvas.SetTop(overlayCenterDot, centerY - 8);
            OverlayCanvas.Width = width;
            OverlayCanvas.Height = height;
            OverlayCanvas.Children.Add(hLineLeft);
            OverlayCanvas.Children.Add(hLineRight);
            OverlayCanvas.Children.Add(vLineTop);
            OverlayCanvas.Children.Add(vLineBottom);
            OverlayCanvas.Children.Add(overlayCenterDot);
            // 呼吸動畫
            DoubleAnimation breathAnim = new DoubleAnimation
            {
                From = 0.7,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.7),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            centerDotStoryboard = new Storyboard();
            centerDotStoryboard.Children.Add(breathAnim);
            Storyboard.SetTarget(breathAnim, overlayCenterDot);
            Storyboard.SetTargetProperty(breathAnim, new PropertyPath(UIElement.OpacityProperty));
            centerDotStoryboard.Begin();
        }

        private void RemoveOverlayCrossLines()
        {
            OverlayCanvas.Children.Clear();
            if (centerDotStoryboard != null)
            {
                centerDotStoryboard.Stop();
                centerDotStoryboard = null;
            }
            overlayCenterDot = null;
        }

        private void MainCanvasGrid_LoadedForOverlayCross(object sender, RoutedEventArgs e)
        {
            MainCanvasGrid.Loaded -= MainCanvasGrid_LoadedForOverlayCross;
            if (showCrossLines)
                DrawOverlayCrossLines();
        }

        #endregion

        #region Ruler

        private void RulerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRulerMode)
            {
                isRulerMode = true;
                EnableRulerMode();
                SetButtonActiveState(sender as Button, isRulerMode);
            }
            else
            {
                isRulerMode = false;
                DisableRulerMode();
                SetButtonActiveState(sender as Button, isRulerMode);
            }
        }
        private void EnableRulerMode()
        {
            MainCanvas.MouseLeftButtonDown += MainCanvas_Ruler_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_Ruler_MouseMove;
            MainCanvas.MouseLeftButtonUp += MainCanvas_Ruler_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Cross;
        }
        private void DisableRulerMode()
        {
            MainCanvas.MouseLeftButtonDown -= MainCanvas_Ruler_MouseLeftButtonDown;
            MainCanvas.MouseMove -= MainCanvas_Ruler_MouseMove;
            MainCanvas.MouseLeftButtonUp -= MainCanvas_Ruler_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Arrow;
            isRulerMode = false;
            RemoveRuler();
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "量尺")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }
        private void RemoveRuler()
        {
            if (rulerPoint1 != null) MainCanvas.Children.Remove(rulerPoint1);
            if (rulerPoint2 != null) MainCanvas.Children.Remove(rulerPoint2);
            if (rulerLine != null) MainCanvas.Children.Remove(rulerLine);
            if (rulerLabel != null) MainCanvas.Children.Remove(rulerLabel);
            if (rulerArrow != null) MainCanvas.Children.Remove(rulerArrow);
            if (rulerLabelBorder != null) MainCanvas.Children.Remove(rulerLabelBorder);
            if (rulerArrowDot != null) MainCanvas.Children.Remove(rulerArrowDot);
            if (rulerPoint1Label != null) MainCanvas.Children.Remove(rulerPoint1Label);
            if (rulerPoint2Label != null) MainCanvas.Children.Remove(rulerPoint2Label);
            rulerPoint1 = null; rulerPoint2 = null; rulerLine = null; rulerLabel = null; rulerArrow = null; rulerLabelBorder = null; rulerArrowDot = null;
            rulerPoint1Label = null;
            rulerPoint2Label = null;
            rulerClickCount = 0;
        }
        private void MainCanvas_Ruler_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (rulerClickCount == 0)
            {
                rulerP1 = pos;
                rulerP2 = pos;
                isDrawingRulerLine = true;
                DrawRuler();
                rulerClickCount = 1;
            }
            else if (rulerClickCount == 2)
            {
                if (IsPointNear(pos, rulerP1))
                {
                    isDraggingRulerPoint1 = true;
                }
                else if (IsPointNear(pos, rulerP2))
                {
                    isDraggingRulerPoint2 = true;
                }
                else if (IsPointNearLine(pos, rulerP1, rulerP2, 10))
                {
                    isDraggingRulerLine = true;
                    lastRulerDragPos = pos;
                }
            }
        }
        private void MainCanvas_Ruler_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (rulerClickCount == 1 && isDrawingRulerLine)
            {
                rulerP2 = pos;
                DrawRuler();
            }
            else if (rulerClickCount == 2)
            {
                if (isDraggingRulerPoint1)
                {
                    rulerP1 = pos;
                    DrawRuler();
                }
                else if (isDraggingRulerPoint2)
                {
                    rulerP2 = pos;
                    DrawRuler();
                }
                else if (isDraggingRulerLine)
                {
                    Vector delta = pos - lastRulerDragPos;
                    rulerP1 += delta;
                    rulerP2 += delta;
                    lastRulerDragPos = pos;
                    DrawRuler();
                }
            }
        }
        private void MainCanvas_Ruler_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (rulerClickCount == 1 && isDrawingRulerLine)
            {
                isDrawingRulerLine = false;
                rulerClickCount = 2;
            }
            isDraggingRulerPoint1 = false;
            isDraggingRulerPoint2 = false;
            isDraggingRulerLine = false;
            AnimateRulerPointScale();
        }
        private bool IsPointNear(Point a, Point b, double tol = 12)
        {
            return (a - b).Length < tol;
        }
        private bool IsPointNearLine(Point p, Point a, Point b, double tol)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            if (dx == 0 && dy == 0) return false;
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            double dist = Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
            return dist < tol;
        }
        private void DrawRuler()
        {
            // Remove old
            if (rulerPoint1 != null) MainCanvas.Children.Remove(rulerPoint1);
            if (rulerPoint2 != null) MainCanvas.Children.Remove(rulerPoint2);
            if (rulerLine != null) MainCanvas.Children.Remove(rulerLine);
            if (rulerLabel != null) MainCanvas.Children.Remove(rulerLabel);
            if (rulerArrow != null) MainCanvas.Children.Remove(rulerArrow);
            if (rulerLabelBorder != null) MainCanvas.Children.Remove(rulerLabelBorder);
            if (rulerPoint1Label != null) MainCanvas.Children.Remove(rulerPoint1Label);
            if (rulerPoint2Label != null) MainCanvas.Children.Remove(rulerPoint2Label);
            rulerPoint1 = null;
            rulerPoint2 = null;
            rulerLine = null;
            rulerLabel = null;
            rulerArrow = null;
            rulerLabelBorder = null;
            rulerPoint1Label = null;
            rulerPoint2Label = null;
            // Draw line
            var newLine = new Line
            {
                X1 = rulerP1.X,
                Y1 = rulerP1.Y,
                X2 = rulerP2.X,
                Y2 = rulerP2.Y,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.7,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            MainCanvas.Children.Add(newLine);
            rulerLine = newLine;
            // Draw endpoints
            var newPoint1 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = rulerPoint1Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(newPoint1, rulerP1.X - 9);
            Canvas.SetTop(newPoint1, rulerP1.Y - 9);
            MainCanvas.Children.Add(newPoint1);
            rulerPoint1 = newPoint1;
            var newPoint2 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = rulerPoint2Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(newPoint2, rulerP2.X - 9);
            Canvas.SetTop(newPoint2, rulerP2.Y - 9);
            MainCanvas.Children.Add(newPoint2);
            rulerPoint2 = newPoint2;
            // Draw arrow in the middle
            double dx = rulerP2.X - rulerP1.X;
            double dy = rulerP2.Y - rulerP1.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double midX = (rulerP1.X + rulerP2.X) / 2;
            double midY = (rulerP1.Y + rulerP2.Y) / 2;
            double arrowLen = 36;
            double arrowWidth = 18;
            if (dist > 30)
            {
                double angle = Math.Atan2(dy, dx);
                // 尖端在 rulerP2
                Point tip = new Point(rulerP2.X, rulerP2.Y);
                // 底部兩點
                Point base1 = new Point(
                    tip.X - arrowLen * Math.Cos(angle) + arrowWidth * Math.Sin(angle) / 2,
                    tip.Y - arrowLen * Math.Sin(angle) - arrowWidth * Math.Cos(angle) / 2);
                Point base2 = new Point(
                    tip.X - arrowLen * Math.Cos(angle) - arrowWidth * Math.Sin(angle) / 2,
                    tip.Y - arrowLen * Math.Sin(angle) + arrowWidth * Math.Cos(angle) / 2);
                var newArrow = new Polygon
                {
                    Points = new PointCollection { base1, tip, base2 },
                    Fill = new SolidColorBrush(Color.FromRgb(255, 60, 60)), // 橙紅色
                    Stroke = Brushes.White, // 白色描邊
                    StrokeThickness = 2.5,
                    Opacity = 0.6,
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
                };
                MainCanvas.Children.Add(newArrow);
                rulerArrow = newArrow;
            }

            // 只有在 showLabels 為 true 時才繪製標籤
            if (showLabels)
            {
                // Draw label (with Border for rounded corners)
                var newLabel = new TextBlock
                {
                    Text = $"長度: {dist:F1} (ΔX: {dx:F0}, ΔY: {dy:F0})",
                    Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 18,
                    Background = Brushes.Transparent,
                    Opacity = 1.0,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var newLabelBorder = new Border
                {
                    Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                    CornerRadius = new CornerRadius(12),
                    Child = newLabel,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(10, 4, 10, 4)
                };
                double labelX = midX;
                double labelY = midY - 32;
                Canvas.SetLeft(newLabelBorder, labelX - 80);
                Canvas.SetTop(newLabelBorder, labelY);
                MainCanvas.Children.Add(newLabelBorder);
                rulerLabel = newLabel;
                rulerLabelBorder = newLabelBorder;

                // Draw端點座標標籤
                var p1Text = new TextBlock
                {
                    Text = $"({rulerP1.X:F0}, {rulerP1.Y:F0})",
                    Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var p1Border = new Border
                {
                    Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = p1Text,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2)
                };
                Canvas.SetLeft(p1Border, rulerP1.X + 12);
                Canvas.SetTop(p1Border, rulerP1.Y - 8);
                MainCanvas.Children.Add(p1Border);
                rulerPoint1Label = p1Border;

                var p2Text = new TextBlock
                {
                    Text = $"({rulerP2.X:F0}, {rulerP2.Y:F0})",
                    Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var p2Border = new Border
                {
                    Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = p2Text,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2)
                };
                Canvas.SetLeft(p2Border, rulerP2.X + 12);
                Canvas.SetTop(p2Border, rulerP2.Y - 8);
                MainCanvas.Children.Add(p2Border);
                rulerPoint2Label = p2Border;
            }

            // 拖曳動畫
            AnimateRulerPointScale();
        }
        private void AnimateRulerPointScale()
        {
            double scale1 = isDraggingRulerPoint1 ? 1.4 : 1.0;
            double scale2 = isDraggingRulerPoint2 ? 1.4 : 1.0;
            var anim1 = new DoubleAnimation(scale1, TimeSpan.FromMilliseconds(120));
            var anim2 = new DoubleAnimation(scale2, TimeSpan.FromMilliseconds(120));
            rulerPoint1Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim1);
            rulerPoint1Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim1);
            rulerPoint2Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim2);
            rulerPoint2Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim2);
        }

        #endregion

        #region Rectangle ROI

        private void RotatableRectRoiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRotRectRoiMode)
            {
                isRotRectRoiMode = true;
                EnableRotRectRoiMode();
                SetButtonActiveState(sender as Button, isRotRectRoiMode);
            }
            else
            {
                isRotRectRoiMode = false;
                DisableRotRectRoiMode();
                SetButtonActiveState(sender as Button, isRotRectRoiMode);
            }
        }
        private void EnableRotRectRoiMode()
        {
            MainCanvas.MouseLeftButtonDown += MainCanvas_RotRectRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_RotRectRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp += MainCanvas_RotRectRoi_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Cross;
        }
        private void MainCanvas_RotRectRoi_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (rotRectRoiClickCount < 2)
            {
                if (rotRectRoiClickCount == 0)
                {
                    rotRectCenter = pos;
                    rotRectWidth = 120;
                    rotRectHeight = 80;
                    rotRectAngle = 0;
                    DrawRotRectRoi();
                    rotRectRoiClickCount = 2; // 直接進入可拖曳狀態
                }
                return;
            }
            int hit = HitTestRotRectRoi(pos);
            if (hit >= 0 && hit <= 3)
            {
                isDraggingRotRectCorner = true;
                draggingCornerIndex = hit;
                rotRectDragStart = pos;
                rotRectDragStartCenter = rotRectCenter;
                rotRectDragStartWidth = rotRectWidth;
                rotRectDragStartHeight = rotRectHeight;
                rotRectDragStartAngle = rotRectAngle;
                AnimateScale(rotRectRoiCornerScales[hit], 1.4); // 補動畫
            }
            else if (hit == 4)
            {
                isDraggingRotRectRotate = true;
                rotRectDragStart = pos;
                rotRectDragStartCenter = rotRectCenter;
                rotRectDragStartAngle = rotRectAngle;
                AnimateScale(rotRectRoiRotateScale, 1.4); // 補動畫
            }
            else if (hit == 5)
            {
                isDraggingRotRect = true;
                rotRectDragStart = pos;
                rotRectDragStartCenter = rotRectCenter;
            }
        }
        private void MainCanvas_RotRectRoi_MouseMove(object sender, MouseEventArgs e)
        {
            if (rotRectRoiClickCount < 2) return;
            Point pos = e.GetPosition(MainCanvas);
            if (isDraggingRotRect)
            {
                Vector delta = pos - rotRectDragStart;
                rotRectCenter = rotRectDragStartCenter + delta;
                DrawRotRectRoi();
            }
            else if (isDraggingRotRectCorner && draggingCornerIndex >= 0)
            {
                // 拖曳角錨點，滑鼠位置即為該角
                double rad = rotRectAngle * Math.PI / 180.0;
                double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
                // 找到對角點
                int oppIdx = (draggingCornerIndex + 2) % 4;
                Point[] corners = new Point[4];
                double hw = rotRectDragStartWidth / 2, hh = rotRectDragStartHeight / 2;
                corners[0] = rotRectDragStartCenter + new Vector(-hw * cosA + hh * sinA, -hw * sinA - hh * cosA); // 左上
                corners[1] = rotRectDragStartCenter + new Vector(hw * cosA + hh * sinA, hw * sinA - hh * cosA);  // 右上
                corners[2] = rotRectDragStartCenter + new Vector(hw * cosA - hh * sinA, hw * sinA + hh * cosA);  // 右下
                corners[3] = rotRectDragStartCenter + new Vector(-hw * cosA - hh * sinA, -hw * sinA + hh * cosA);// 左下
                Point opp = corners[oppIdx];
                Point mouse = pos;
                // 新中心
                rotRectCenter = new Point((mouse.X + opp.X) / 2, (mouse.Y + opp.Y) / 2);
                // 轉回本地座標
                Vector v = mouse - rotRectCenter;
                double localX = v.X * cosA + v.Y * sinA;
                double localY = -v.X * sinA + v.Y * cosA;
                rotRectWidth = Math.Max(Math.Abs(localX) * 2, 20);
                rotRectHeight = Math.Max(Math.Abs(localY) * 2, 20);
                DrawRotRectRoi();
            }
            else if (isDraggingRotRectRotate)
            {
                Vector v1 = rotRectDragStart - rotRectCenter;
                Vector v2 = pos - rotRectCenter;
                double a1 = Math.Atan2(v1.Y, v1.X);
                double a2 = Math.Atan2(v2.Y, v2.X);
                rotRectAngle = rotRectDragStartAngle + (a2 - a1) * 180.0 / Math.PI;
                DrawRotRectRoi();
            }
        }
        private void MainCanvas_RotRectRoi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingRotRect = false;
            isDraggingRotRectCorner = false;
            isDraggingRotRectRotate = false;
            draggingCornerIndex = -1;
            for (int i = 0; i < 4; i++) AnimateScale(rotRectRoiCornerScales[i], 1.0);
            AnimateScale(rotRectRoiRotateScale, 1.0);
        }
        private void DisableRotRectRoiMode()
        {
            MainCanvas.MouseLeftButtonDown -= MainCanvas_RotRectRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove -= MainCanvas_RotRectRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp -= MainCanvas_RotRectRoi_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Arrow;
            isRotRectRoiMode = false;
            RemoveRotRectRoi();
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "旋轉ROI")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }
        private int HitTestRotRectRoi(Point pos)
        {
            // 回傳：0-3=角錨點，4=旋轉錨點，5=本體，-1=無
            double tol = 12;
            // 角錨點
            double rad = rotRectAngle * Math.PI / 180.0;
            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
            double hw = rotRectWidth / 2, hh = rotRectHeight / 2;
            Point[] corners = new Point[4];
            corners[0] = new Point(rotRectCenter.X - hw * cosA + hh * sinA, rotRectCenter.Y - hw * sinA - hh * cosA); // 左上
            corners[1] = new Point(rotRectCenter.X + hw * cosA + hh * sinA, rotRectCenter.Y + hw * sinA - hh * cosA); // 右上
            corners[2] = new Point(rotRectCenter.X + hw * cosA - hh * sinA, rotRectCenter.Y + hw * sinA + hh * cosA); // 右下
            corners[3] = new Point(rotRectCenter.X - hw * cosA - hh * sinA, rotRectCenter.Y - hw * sinA + hh * cosA); // 左下
            for (int i = 0; i < 4; i++)
                if ((pos - corners[i]).Length < tol) return i;
            // 旋轉錨點
            double rotDotDist = 32;
            double rotDotX = (corners[0].X + corners[1].X) / 2 + rotDotDist * -sinA;
            double rotDotY = (corners[0].Y + corners[1].Y) / 2 + rotDotDist * cosA;
            if ((pos - new Point(rotDotX, rotDotY)).Length < tol) return 4;
            // 本體（多邊形內）
            if (IsPointInPolygon(pos, corners)) return 5;
            return -1;
        }
        private void RemoveRotRectRoi()
        {
            if (rotRectRoi != null) MainCanvas.Children.Remove(rotRectRoi);
            for (int i = 0; i < rotRectRoiCornerDots.Length; i++)
                if (rotRectRoiCornerDots[i] != null) MainCanvas.Children.Remove(rotRectRoiCornerDots[i]);
            if (rotRectRoiRotateDot != null) MainCanvas.Children.Remove(rotRectRoiRotateDot);
            if (rotRectRoiLabelBorder != null) MainCanvas.Children.Remove(rotRectRoiLabelBorder); // 新增：移除標籤
            rotRectRoi = null;
            for (int i = 0; i < rotRectRoiCornerDots.Length; i++) rotRectRoiCornerDots[i] = null;
            rotRectRoiRotateDot = null;
            rotRectRoiLabelBorder = null; // 新增：標籤設為 null
            rotRectRoiClickCount = 0;
        }
        private void DrawRotRectRoi()
        {
            // 先移除舊的
            if (rotRectRoi != null) MainCanvas.Children.Remove(rotRectRoi);
            for (int i = 0; i < rotRectRoiCornerDots.Length; i++)
                if (rotRectRoiCornerDots[i] != null) MainCanvas.Children.Remove(rotRectRoiCornerDots[i]);
            if (rotRectRoiRotateDot != null) MainCanvas.Children.Remove(rotRectRoiRotateDot);
            if (rotRectRoiLabelBorder != null) MainCanvas.Children.Remove(rotRectRoiLabelBorder); // 新增：移除舊標籤
            rotRectRoiLabelBorder = null;
            // 計算四個角座標
            double rad = rotRectAngle * Math.PI / 180.0;
            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
            Point[] corners = new Point[4];
            double hw = rotRectWidth / 2, hh = rotRectHeight / 2;
            corners[0] = new Point(rotRectCenter.X - hw * cosA + hh * sinA, rotRectCenter.Y - hw * sinA - hh * cosA); // 左上
            corners[1] = new Point(rotRectCenter.X + hw * cosA + hh * sinA, rotRectCenter.Y + hw * sinA - hh * cosA); // 右上
            corners[2] = new Point(rotRectCenter.X + hw * cosA - hh * sinA, rotRectCenter.Y + hw * sinA + hh * cosA); // 右下
            corners[3] = new Point(rotRectCenter.X - hw * cosA - hh * sinA, rotRectCenter.Y - hw * sinA + hh * cosA); // 左下
            // 畫多邊形
            rotRectRoi = new Polygon
            {
                Points = new PointCollection(corners),
                Stroke = new SolidColorBrush(styleDict["rotrect"].line),
                StrokeThickness = 2.5,
                Fill = new SolidColorBrush(Color.FromArgb(40, 0, 191, 255)),
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["rotrect"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            MainCanvas.Children.Add(rotRectRoi);
            // 畫四個角錨點
            for (int i = 0; i < 4; i++)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(styleDict["rotrect"].line),
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = styleDict["rotrect"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = rotRectRoiCornerScales[i],
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(dot, corners[i].X - 9);
                Canvas.SetTop(dot, corners[i].Y - 9);
                MainCanvas.Children.Add(dot);
                rotRectRoiCornerDots[i] = dot;
            }
            // 畫旋轉錨點（在上方中點外一段距離）
            double rotDotDist = 32;
            double rotDotX = (corners[0].X + corners[1].X) / 2 + rotDotDist * -sinA;
            double rotDotY = (corners[0].Y + corners[1].Y) / 2 + rotDotDist * cosA;
            rotRectRoiRotateDot = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.Yellow,
                Stroke = new SolidColorBrush(styleDict["rotrect"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["rotrect"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = rotRectRoiRotateScale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rotRectRoiRotateDot, rotDotX - 9);
            Canvas.SetTop(rotRectRoiRotateDot, rotDotY - 9);
            MainCanvas.Children.Add(rotRectRoiRotateDot);

            // 新增標籤顯示長、寬和角度
            if (showLabels)
            {
                var labelText = new TextBlock
                {
                    Text = $"寬: {rotRectWidth:F1}  高: {rotRectHeight:F1}  角度: {rotRectAngle:F1}°",
                    Foreground = new SolidColorBrush(styleDict["rotrect"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Background = Brushes.Transparent,
                    Opacity = 1.0,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var labelBorder = new Border
                {
                    Background = new SolidColorBrush(styleDict["rotrect"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = labelText,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                double labelX = rotRectCenter.X;
                double labelY = rotRectCenter.Y - rotRectHeight / 2 - 40;
                Canvas.SetLeft(labelBorder, labelX - 100);
                Canvas.SetTop(labelBorder, labelY);
                MainCanvas.Children.Add(labelBorder);
                rotRectRoiLabelBorder = labelBorder;
            }
        }

        #endregion

        #region Ellipse ROI
        private void EllipseRoiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEllipseRoiMode)
            {
                isEllipseRoiMode = true;
                EnableEllipseRoiMode();
                SetButtonActiveState(sender as Button, isEllipseRoiMode);
            }
            else
            {
                isEllipseRoiMode = false;
                DisableEllipseRoiMode();
                SetButtonActiveState(sender as Button, isEllipseRoiMode);
            }
        }
        private void EnableEllipseRoiMode()
        {
            MainCanvas.MouseLeftButtonDown += MainCanvas_EllipseRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_EllipseRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp += MainCanvas_EllipseRoi_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Cross;
        }
        private void DisableEllipseRoiMode()
        {
            MainCanvas.MouseLeftButtonDown -= MainCanvas_EllipseRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove -= MainCanvas_EllipseRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp -= MainCanvas_EllipseRoi_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Arrow;
            isEllipseRoiMode = false;
            RemoveEllipseRoi();
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "圓形ROI")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }
        private void RemoveEllipseRoi()
        {
            if (ellipseRoi != null) MainCanvas.Children.Remove(ellipseRoi);
            if (ellipseRoiCenterDot != null) MainCanvas.Children.Remove(ellipseRoiCenterDot);
            if (ellipseRoiRadiusDot != null) MainCanvas.Children.Remove(ellipseRoiRadiusDot);
            if (ellipseRoiLabelBorder != null) MainCanvas.Children.Remove(ellipseRoiLabelBorder); // 新增：移除標籤
            ellipseRoi = null;
            ellipseRoiCenterDot = null;
            ellipseRoiRadiusDot = null;
            ellipseRoiLabelBorder = null; // 新增：設為 null
            ellipseRoiClickCount = 0;
        }
        private void MainCanvas_EllipseRoi_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (ellipseRoiClickCount < 2)
            {
                if (ellipseRoiClickCount == 0)
                {
                    ellipseRoiCenter = pos;
                    ellipseRoiRadius = 60;
                    DrawEllipseRoi();
                    ellipseRoiClickCount = 2; // 直接進入可拖曳狀態
                }
                return;
            }
            if ((pos - ellipseRoiCenter).Length < 14)
            {
                isDraggingEllipseRoiCenter = true;
                ellipseRoiDragStart = pos;
                ellipseRoiDragStartCenter = ellipseRoiCenter;
                AnimateScale(ellipseRoiCenterScale, 1.4); // 補動畫
            }
            else if (Math.Abs((pos - (ellipseRoiCenter + new Vector(ellipseRoiRadius, 0))).Length) < 14)
            {
                isDraggingEllipseRoiRadius = true;
                ellipseRoiDragStart = pos;
                ellipseRoiDragStartRadius = ellipseRoiRadius;
                AnimateScale(ellipseRoiRadiusScale, 1.4); // 補動畫
            }
        }
        private void MainCanvas_EllipseRoi_MouseMove(object sender, MouseEventArgs e)
        {
            if (ellipseRoiClickCount < 2) return;
            Point pos = e.GetPosition(MainCanvas);
            if (isDraggingEllipseRoiCenter)
            {
                Vector delta = pos - ellipseRoiDragStart;
                ellipseRoiCenter = ellipseRoiDragStartCenter + delta;
                DrawEllipseRoi();
            }
            else if (isDraggingEllipseRoiRadius)
            {
                double newRadius = ellipseRoiDragStartRadius + (pos - ellipseRoiDragStart).X;
                newRadius = Math.Max(newRadius, 10);
                ellipseRoiRadius = newRadius;
                DrawEllipseRoi();
            }
        }
        private void MainCanvas_EllipseRoi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingEllipseRoiCenter = false;
            isDraggingEllipseRoiRadius = false;
            AnimateScale(ellipseRoiCenterScale, 1.0);
            AnimateScale(ellipseRoiRadiusScale, 1.0);
        }
        private void DrawEllipseRoi()
        {
            if (ellipseRoi != null) MainCanvas.Children.Remove(ellipseRoi);
            if (ellipseRoiCenterDot != null) MainCanvas.Children.Remove(ellipseRoiCenterDot);
            if (ellipseRoiRadiusDot != null) MainCanvas.Children.Remove(ellipseRoiRadiusDot);
            if (ellipseRoiLabelBorder != null) MainCanvas.Children.Remove(ellipseRoiLabelBorder);
            ellipseRoiLabelBorder = null;

            ellipseRoi = new Ellipse
            {
                Width = ellipseRoiRadius * 2,
                Height = ellipseRoiRadius * 2,
                Stroke = new SolidColorBrush(styleDict["ellipse"].line),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(60, 140, 90, 220)), // 深一點的紫色半透明
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["ellipse"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            Canvas.SetLeft(ellipseRoi, ellipseRoiCenter.X - ellipseRoiRadius);
            Canvas.SetTop(ellipseRoi, ellipseRoiCenter.Y - ellipseRoiRadius);
            MainCanvas.Children.Add(ellipseRoi);

            ellipseRoiCenterDot = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ellipse"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ellipse"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = ellipseRoiCenterScale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ellipseRoiCenterDot, ellipseRoiCenter.X - 9);
            Canvas.SetTop(ellipseRoiCenterDot, ellipseRoiCenter.Y - 9);
            MainCanvas.Children.Add(ellipseRoiCenterDot);

            ellipseRoiRadiusDot = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ellipse"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ellipse"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = ellipseRoiRadiusScale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ellipseRoiRadiusDot, ellipseRoiCenter.X + ellipseRoiRadius - 9);
            Canvas.SetTop(ellipseRoiRadiusDot, ellipseRoiCenter.Y - 9);
            MainCanvas.Children.Add(ellipseRoiRadiusDot);

            if (showLabels)
            {
                var labelText = new TextBlock
                {
                    Text = $"圓心: ({ellipseRoiCenter.X:F0}, {ellipseRoiCenter.Y:F0})\n半徑: {ellipseRoiRadius:F1}",
                    Foreground = new SolidColorBrush(styleDict["ellipse"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Opacity = 1.0,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var labelBorder = new Border
                {
                    Background = new SolidColorBrush(styleDict["ellipse"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = labelText,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                // 計算標註位置，避開圓右側端點
                double offsetX = -ellipseRoiRadius - 80; // 向左偏移半徑+80
                double offsetY = -ellipseRoiRadius / 2;  // 向上偏移半徑一半
                Canvas.SetLeft(labelBorder, ellipseRoiCenter.X + offsetX);
                Canvas.SetTop(labelBorder, ellipseRoiCenter.Y + offsetY);
                MainCanvas.Children.Add(labelBorder);
                ellipseRoiLabelBorder = labelBorder;
            }
        }

        #endregion

        #region Line

        private void DrawLineButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isDrawLineMode)
            {
                isDrawLineMode = true;
                EnableDrawLineMode();
                SetButtonActiveState(sender as Button, isDrawLineMode);
            }
            else
            {
                isDrawLineMode = false;
                DisableDrawLineMode();
                SetButtonActiveState(sender as Button, isDrawLineMode);
            }
        }
        private void EnableDrawLineMode()
        {
            MainCanvas.MouseLeftButtonDown += MainCanvas_DrawLine_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_DrawLine_MouseMove;
            MainCanvas.MouseLeftButtonUp += MainCanvas_DrawLine_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Cross;
        }
        private void DisableDrawLineMode()
        {
            MainCanvas.MouseLeftButtonDown -= MainCanvas_DrawLine_MouseLeftButtonDown;
            MainCanvas.MouseMove -= MainCanvas_DrawLine_MouseMove;
            MainCanvas.MouseLeftButtonUp -= MainCanvas_DrawLine_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Arrow;
            isDrawLineMode = false;
            RemoveDrawLine();
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "畫線")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }
        private void RemoveDrawLine()
        {
            // 移除所有畫線相關的元素
            if (drawLine != null) MainCanvas.Children.Remove(drawLine);
            if (drawLinePoint1 != null) MainCanvas.Children.Remove(drawLinePoint1);
            if (drawLinePoint2 != null) MainCanvas.Children.Remove(drawLinePoint2);
            if (drawLineLabel != null) MainCanvas.Children.Remove(drawLineLabel);
            if (drawLineLabelBorder != null) MainCanvas.Children.Remove(drawLineLabelBorder);
            if (drawLinePoint1Label != null) MainCanvas.Children.Remove(drawLinePoint1Label);
            if (drawLinePoint2Label != null) MainCanvas.Children.Remove(drawLinePoint2Label);

            // 重置所有變數
            drawLine = null;
            drawLinePoint1 = null;
            drawLinePoint2 = null;
            drawLineLabel = null;
            drawLineLabelBorder = null;
            drawLinePoint1Label = null;
            drawLinePoint2Label = null;
            drawLineClickCount = 0;
            isDraggingDrawLineP1 = false;
            isDraggingDrawLineP2 = false;
            isDraggingDrawLine = false;
            isDrawingLine = false;
        }
        private void MainCanvas_DrawLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (drawLineClickCount == 0)
            {
                drawLineP1 = pos;
                drawLineP2 = pos;
                isDrawingLine = true;
                DrawLineOnCanvas();
                drawLineClickCount = 1;
            }
            else if (drawLineClickCount == 2)
            {
                if (IsPointNear(pos, drawLineP1))
                {
                    isDraggingDrawLineP1 = true;
                }
                else if (IsPointNear(pos, drawLineP2))
                {
                    isDraggingDrawLineP2 = true;
                }
                else if (IsPointNearLine(pos, drawLineP1, drawLineP2, 10))
                {
                    isDraggingDrawLine = true;
                    lastDrawLineDragPos = pos;
                }
            }
        }
        private void MainCanvas_DrawLine_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (drawLineClickCount == 1 && isDrawingLine)
            {
                drawLineP2 = pos;
                DrawLineOnCanvas();
            }
            else if (drawLineClickCount == 2)
            {
                if (isDraggingDrawLineP1)
                {
                    drawLineP1 = pos;
                    DrawLineOnCanvas();
                }
                else if (isDraggingDrawLineP2)
                {
                    drawLineP2 = pos;
                    DrawLineOnCanvas();
                }
                else if (isDraggingDrawLine)
                {
                    Vector delta = pos - lastDrawLineDragPos;
                    drawLineP1 += delta;
                    drawLineP2 += delta;
                    lastDrawLineDragPos = pos;
                    DrawLineOnCanvas();
                }
            }
        }
        private void MainCanvas_DrawLine_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (drawLineClickCount == 1 && isDrawingLine)
            {
                isDrawingLine = false;
                drawLineClickCount = 2;
            }
            isDraggingDrawLineP1 = false;
            isDraggingDrawLineP2 = false;
            isDraggingDrawLine = false;
            AnimateDrawLinePointScale();
        }
        private void DrawLineOnCanvas()
        {
            // Remove old
            if (drawLine != null) MainCanvas.Children.Remove(drawLine);
            if (drawLinePoint1 != null) MainCanvas.Children.Remove(drawLinePoint1);
            if (drawLinePoint2 != null) MainCanvas.Children.Remove(drawLinePoint2);
            if (drawLineLabel != null) MainCanvas.Children.Remove(drawLineLabel);
            if (drawLineLabelBorder != null) MainCanvas.Children.Remove(drawLineLabelBorder);
            if (drawLinePoint1Label != null) MainCanvas.Children.Remove(drawLinePoint1Label);
            if (drawLinePoint2Label != null) MainCanvas.Children.Remove(drawLinePoint2Label);
            drawLine = null;
            drawLinePoint1 = null;
            drawLinePoint2 = null;
            drawLineLabel = null;
            drawLineLabelBorder = null;
            drawLinePoint1Label = null;
            drawLinePoint2Label = null;

            // Draw line
            var newLine = new Line
            {
                X1 = drawLineP1.X,
                Y1 = drawLineP1.Y,
                X2 = drawLineP2.X,
                Y2 = drawLineP2.Y,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 3,
                Opacity = 0.7,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            MainCanvas.Children.Add(newLine);
            drawLine = newLine;

            // Draw endpoints
            var newPoint1 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = drawLinePoint1Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(newPoint1, drawLineP1.X - 9);
            Canvas.SetTop(newPoint1, drawLineP1.Y - 9);
            MainCanvas.Children.Add(newPoint1);
            drawLinePoint1 = newPoint1;

            var newPoint2 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = drawLinePoint2Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(newPoint2, drawLineP2.X - 9);
            Canvas.SetTop(newPoint2, drawLineP2.Y - 9);
            MainCanvas.Children.Add(newPoint2);
            drawLinePoint2 = newPoint2;

            // 計算距離和角度
            double dx = drawLineP2.X - drawLineP1.X;
            double dy = drawLineP2.Y - drawLineP1.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            // 顯示端點座標
            if (showLabels)
            {
                var p1Text = new TextBlock
                {
                    Text = $"({drawLineP1.X:F0}, {drawLineP1.Y:F0})",
                    Foreground = new SolidColorBrush(styleDict["line"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var p1Border = new Border
                {
                    Background = new SolidColorBrush(styleDict["line"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = p1Text,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2)
                };
                Canvas.SetLeft(p1Border, drawLineP1.X + 12);
                Canvas.SetTop(p1Border, drawLineP1.Y - 8);
                MainCanvas.Children.Add(p1Border);
                drawLinePoint1Label = p1Border;

                var p2Text = new TextBlock
                {
                    Text = $"({drawLineP2.X:F0}, {drawLineP2.Y:F0})",
                    Foreground = new SolidColorBrush(styleDict["line"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var p2Border = new Border
                {
                    Background = new SolidColorBrush(styleDict["line"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = p2Text,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2)
                };
                Canvas.SetLeft(p2Border, drawLineP2.X + 12);
                Canvas.SetTop(p2Border, drawLineP2.Y - 8);
                MainCanvas.Children.Add(p2Border);
                drawLinePoint2Label = p2Border;
            }

            // 拖曳動畫
            AnimateDrawLinePointScale();
        }

        private void AnimateDrawLinePointScale()
        {
            double scale1 = isDraggingDrawLineP1 ? 1.4 : 1.0;
            double scale2 = isDraggingDrawLineP2 ? 1.4 : 1.0;
            var anim1 = new DoubleAnimation(scale1, TimeSpan.FromMilliseconds(120));
            var anim2 = new DoubleAnimation(scale2, TimeSpan.FromMilliseconds(120));
            drawLinePoint1Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim1);
            drawLinePoint1Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim1);
            drawLinePoint2Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim2);
            drawLinePoint2Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim2);
        }

        #endregion

        #region Polygon ROI

        private void PolygonRoiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPolygonRoiMode)
            {
                isPolygonRoiMode = true;
                EnablePolygonRoiMode();
                SetButtonActiveState(sender as Button, isPolygonRoiMode);
            }
            else
            {
                isPolygonRoiMode = false;
                DisablePolygonRoiMode();
                SetButtonActiveState(sender as Button, isPolygonRoiMode);
            }
        }

        private void EnablePolygonRoiMode()
        {
            MainCanvas.MouseLeftButtonDown += MainCanvas_PolygonRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_PolygonRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp += MainCanvas_PolygonRoi_MouseLeftButtonUp;
            MainCanvas.MouseRightButtonDown += MainCanvas_PolygonRoi_MouseRightButtonDown;
            MainCanvas.Cursor = Cursors.Cross;
            polygonRoiPoints.Clear();
            isPolygonClosed = false;
            DrawPolygonRoi();
        }
        private void DisablePolygonRoiMode()
        {
            MainCanvas.MouseLeftButtonDown -= MainCanvas_PolygonRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove -= MainCanvas_PolygonRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp -= MainCanvas_PolygonRoi_MouseLeftButtonUp;
            MainCanvas.MouseRightButtonDown -= MainCanvas_PolygonRoi_MouseRightButtonDown;
            MainCanvas.Cursor = Cursors.Arrow;
            isPolygonRoiMode = false;
            RemovePolygonRoi();
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "多邊形ROI")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }
        private void RemovePolygonRoi()
        {
            if (polygonRoi != null) MainCanvas.Children.Remove(polygonRoi);
            foreach (var dot in polygonRoiDots) MainCanvas.Children.Remove(dot);
            foreach (var label in polygonRoiLabelBorders) MainCanvas.Children.Remove(label);
            polygonRoi = null;
            polygonRoiDots.Clear();
            polygonRoiLabelBorders.Clear();
        }
        private bool IsPointInPolygonRoi(Point p)
        {
            int n = polygonRoiPoints.Count;
            if (n < 3) return false;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((polygonRoiPoints[i].Y > p.Y) != (polygonRoiPoints[j].Y > p.Y)) &&
                    (p.X < (polygonRoiPoints[j].X - polygonRoiPoints[i].X) * (p.Y - polygonRoiPoints[i].Y) / (polygonRoiPoints[j].Y - polygonRoiPoints[i].Y) + polygonRoiPoints[i].X))
                    inside = !inside;
            }
            return inside && isPolygonClosed; // 只有在多邊形封閉時才判斷點是否在多邊形內
        }
        private void MainCanvas_PolygonRoi_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (!isPolygonClosed)
            {
                // 檢查是否點擊到第一個點（允許12像素的誤差）
                if (polygonRoiPoints.Count >= 3)
                {
                    Point firstPoint = polygonRoiPoints[0];
                    if (Math.Abs(pos.X - firstPoint.X) < 12 && Math.Abs(pos.Y - firstPoint.Y) < 12)
                    {
                        isPolygonClosed = true;
                        DrawPolygonRoi();
                        return;
                    }
                }
                // 新增頂點
                polygonRoiPoints.Add(pos);
                DrawPolygonRoi();
            }
            else
            {
                // 拖曳頂點
                for (int i = 0; i < polygonRoiPoints.Count; i++)
                {
                    if ((pos - polygonRoiPoints[i]).Length < 12)
                    {
                        isDraggingPolygonDot = true;
                        draggingPolygonDotIndex = i;
                        AnimateScale(polygonRoiDotScales[i], 1.4); // 補動畫
                        return;
                    }
                }
                // 拖曳整個多邊形
                if (IsPointInPolygonRoi(pos))
                {
                    isDraggingPolygonBody = true;
                    polygonDragStart = pos;
                    polygonDragStartPoints = new List<Point>(polygonRoiPoints);
                }
            }
        }
        private void MainCanvas_PolygonRoi_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isPolygonClosed) return;
            Point pos = e.GetPosition(MainCanvas);
            if (isDraggingPolygonDot && draggingPolygonDotIndex >= 0 && draggingPolygonDotIndex < polygonRoiPoints.Count)
            {
                polygonRoiPoints[draggingPolygonDotIndex] = pos;
                DrawPolygonRoi();
            }
            else if (isDraggingPolygonBody && polygonDragStartPoints != null)
            {
                Vector delta = pos - polygonDragStart;
                for (int i = 0; i < polygonRoiPoints.Count; i++)
                {
                    polygonRoiPoints[i] = polygonDragStartPoints[i] + delta;
                }
                DrawPolygonRoi();
            }
        }
        private void MainCanvas_PolygonRoi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingPolygonDot = false;
            draggingPolygonDotIndex = -1;
            isDraggingPolygonBody = false;
            polygonDragStartPoints = null;
            for (int i = 0; i < polygonRoiDotScales.Count; i++) AnimateScale(polygonRoiDotScales[i], 1.0);
        }
        private void MainCanvas_PolygonRoi_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 右鍵可刪除最後一個點（未關閉時）
            if (!isPolygonClosed && polygonRoiPoints.Count > 0)
            {
                polygonRoiPoints.RemoveAt(polygonRoiPoints.Count - 1);
                DrawPolygonRoi();
            }
        }
        private void DrawPolygonRoi()
        {
            // 先移除舊的
            if (polygonRoi != null) MainCanvas.Children.Remove(polygonRoi);
            foreach (var dot in polygonRoiDots) MainCanvas.Children.Remove(dot);
            foreach (var label in polygonRoiLabelBorders) MainCanvas.Children.Remove(label);
            polygonRoi = null;
            polygonRoiDots.Clear();
            polygonRoiLabelBorders.Clear();

            // 新增：初始化 polygonRoiDotScales
            polygonRoiDotScales = new List<ScaleTransform>();
            for (int i = 0; i < polygonRoiPoints.Count; i++)
                polygonRoiDotScales.Add(new ScaleTransform(1, 1));

            if (polygonRoiPoints.Count < 2) return;

            var points = new PointCollection(polygonRoiPoints);
            if (isPolygonClosed)
            {
                points.Add(polygonRoiPoints[0]);
            }

            polygonRoi = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(styleDict["polygon"].line),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(80, 220, 180, 40)), // 更深的淺黃色半透明
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            MainCanvas.Children.Add(polygonRoi);

            for (int i = 0; i < polygonRoiPoints.Count; i++)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(styleDict["polygon"].line),
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = polygonRoiDotScales[i],
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(dot, polygonRoiPoints[i].X - 9);
                Canvas.SetTop(dot, polygonRoiPoints[i].Y - 9);
                MainCanvas.Children.Add(dot);
                polygonRoiDots.Add(dot);

                if (showLabels)
                {
                    var labelText = new TextBlock
                    {
                        Text = $"({polygonRoiPoints[i].X:F0}, {polygonRoiPoints[i].Y:F0})",
                        Foreground = new SolidColorBrush(styleDict["polygon"].labelFg),
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        FontFamily = new FontFamily("Segoe UI"),
                        Background = Brushes.Transparent,
                        Padding = new Thickness(0),
                        TextAlignment = TextAlignment.Center
                    };
                    var labelBorder = new Border
                    {
                        Background = new SolidColorBrush(styleDict["polygon"].labelBg),
                        CornerRadius = new CornerRadius(8),
                        Child = labelText,
                        Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                        Opacity = 0.98,
                        Padding = new Thickness(6, 2, 6, 2)
                    };
                    var (offsetX, offsetY) = GetLabelOffset(polygonRoiPoints[i], MainCanvas.ActualWidth, MainCanvas.ActualHeight);
                    Canvas.SetLeft(labelBorder, polygonRoiPoints[i].X + offsetX);
                    Canvas.SetTop(labelBorder, polygonRoiPoints[i].Y + offsetY);
                    MainCanvas.Children.Add(labelBorder);
                    polygonRoiLabelBorders.Add(labelBorder);
                }
            }
        }

        #endregion

        #region Arc ROI

        private void ArcRoiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isArcRoiMode)
            {
                isArcRoiMode = true;
                EnableArcRoiMode();
                SetButtonActiveState(sender as Button, isArcRoiMode);
            }
            else
            {
                isArcRoiMode = false;
                DisableArcRoiMode();
                SetButtonActiveState(sender as Button, isArcRoiMode);
            }
        }

        private void EnableArcRoiMode()
        {
            MainCanvas.MouseLeftButtonDown += MainCanvas_ArcRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_ArcRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp += MainCanvas_ArcRoi_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Cross;
            arcRoiClickCount = 0;
            RemoveArcRoi();
        }

        private void DisableArcRoiMode()
        {
            MainCanvas.MouseLeftButtonDown -= MainCanvas_ArcRoi_MouseLeftButtonDown;
            MainCanvas.MouseMove -= MainCanvas_ArcRoi_MouseMove;
            MainCanvas.MouseLeftButtonUp -= MainCanvas_ArcRoi_MouseLeftButtonUp;
            MainCanvas.Cursor = Cursors.Arrow;
            isArcRoiMode = false;
            RemoveArcRoi();
            // 重置按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Content.ToString() == "弧形ROI")
                {
                    SetButtonActiveState(button, false);
                    break;
                }
            }
        }

        private void RemoveArcRoi()
        {
            if (arcRoi != null) MainCanvas.Children.Remove(arcRoi);
            if (arcRoiPreviewLine != null) MainCanvas.Children.Remove(arcRoiPreviewLine);
            foreach (var dot in arcRoiDots)
            {
                if (dot != null) MainCanvas.Children.Remove(dot);
            }
            if (arcRoiLabelBorder != null) MainCanvas.Children.Remove(arcRoiLabelBorder); // 新增：移除標籤
            arcRoi = null;
            arcRoiPreviewLine = null;
            arcRoiDots = new Ellipse[3];
            arcRoiPoints = new Point[3];
            arcRoiLabelBorder = null; // 新增：設為 null
            arcRoiClickCount = 0;
            isDraggingArcDot = false;
            draggingArcDotIndex = -1;
        }

        private void MainCanvas_ArcRoi_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (arcRoiClickCount < 3)
            {
                arcRoiPoints[arcRoiClickCount] = pos;
                arcRoiClickCount++;
                DrawArcRoi();
            }
            else
            {
                // 檢查是否點擊到控制點
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - arcRoiPoints[i]).Length < 12)
                    {
                        isDraggingArcDot = true;
                        draggingArcDotIndex = i;
                        AnimateScale(arcRoiDotScales[i], 1.4); // 補動畫
                        return;
                    }
                }
            }
        }

        private void MainCanvas_ArcRoi_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            if (isDraggingArcDot && draggingArcDotIndex >= 0 && draggingArcDotIndex < 3)
            {
                arcRoiPoints[draggingArcDotIndex] = pos;
                DrawArcRoi();
            }
        }

        private void MainCanvas_ArcRoi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingArcDot = false;
            draggingArcDotIndex = -1;
            for (int i = 0; i < arcRoiDotScales.Length; i++) AnimateScale(arcRoiDotScales[i], 1.0);
        }

        private void DrawArcRoi()
        {
            if (arcRoi != null) MainCanvas.Children.Remove(arcRoi);
            foreach (var dot in arcRoiDots) MainCanvas.Children.Remove(dot);
            if (arcRoiLabelBorder != null) MainCanvas.Children.Remove(arcRoiLabelBorder);
            arcRoi = null;
            arcRoiDots = new Ellipse[3];
            arcRoiLabelBorder = null;

            if (arcRoiClickCount < 2) return;

            // 建立弧形路徑
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = arcRoiPoints[0];

            if (arcRoiClickCount == 2)
            {
                // 只有兩個點時，畫直線
                LineSegment lineSegment = new LineSegment(arcRoiPoints[1], true);
                pathFigure.Segments.Add(lineSegment);
            }
            else if (arcRoiClickCount == 3)
            {
                // 有三個點時，畫二次貝塞爾曲線
                QuadraticBezierSegment bezierSegment = new QuadraticBezierSegment(arcRoiPoints[1], arcRoiPoints[2], true);
                pathFigure.Segments.Add(bezierSegment);
            }

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            arcRoi = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(styleDict["arc"].line),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(60, 40, 170, 110)), // 深一點的綠色半透明
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["arc"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            MainCanvas.Children.Add(arcRoi);

            // 畫控制點
            for (int i = 0; i < arcRoiClickCount; i++)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = (i == 1) ? new SolidColorBrush(Color.FromRgb(255, 235, 59)) : Brushes.White, // arcRoiPoints[1]黃色，其餘白色
                    Stroke = (i == 1) ? new SolidColorBrush(Color.FromRgb(255, 87, 34)) : new SolidColorBrush(styleDict["arc"].line), // arcRoiPoints[1]亮橘色，其餘深綠色
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = (i == 1) ? new DropShadowEffect { Color = Color.FromRgb(255, 60, 0), BlurRadius = 18, ShadowDepth = 0, Opacity = 0.8 } : new DropShadowEffect { Color = styleDict["arc"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = arcRoiDotScales[i],
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(dot, arcRoiPoints[i].X - 9);
                Canvas.SetTop(dot, arcRoiPoints[i].Y - 9);
                MainCanvas.Children.Add(dot);
                arcRoiDots[i] = dot;
            }

            if (showLabels && arcRoiClickCount == 3)
            {
                double arcLength = CalcQuadraticBezierLength(arcRoiPoints[0], arcRoiPoints[1], arcRoiPoints[2]);
                double chordLength = (arcRoiPoints[2] - arcRoiPoints[0]).Length;
                string p0 = $"({arcRoiPoints[0].X:F0}, {arcRoiPoints[0].Y:F0})";
                string p1 = $"({arcRoiPoints[1].X:F0}, {arcRoiPoints[1].Y:F0})";
                string p2 = $"({arcRoiPoints[2].X:F0}, {arcRoiPoints[2].Y:F0})";
                var labelText = new TextBlock
                {
                    Text = $"P0: {p0}\nP1: {p1}\nP2: {p2}\n弧長: {arcLength:F1}\n弦長: {chordLength:F1}",
                    Foreground = new SolidColorBrush(styleDict["arc"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Opacity = 1.0,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var labelBorder = new Border
                {
                    Background = new SolidColorBrush(styleDict["arc"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = labelText,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2)
                };
                // 計算弧線中點與法線方向
                Point mid = GetQuadraticBezierPoint(arcRoiPoints[0], arcRoiPoints[1], arcRoiPoints[2], 0.5);
                Vector tangent = 2 * (1 - 0.5) * (arcRoiPoints[1] - arcRoiPoints[0]) + 2 * 0.5 * (arcRoiPoints[2] - arcRoiPoints[1]);
                Vector normal = new Vector(-tangent.Y, tangent.X);
                if (normal.Length > 0.1) normal.Normalize();
                double offset = 40;
                Point labelPos = mid + normal * offset;
                // 先加到Canvas再取寬高
                MainCanvas.Children.Add(labelBorder);
                labelBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = labelBorder.DesiredSize;
                Canvas.SetLeft(labelBorder, labelPos.X - size.Width / 2);
                Canvas.SetTop(labelBorder, labelPos.Y - size.Height / 2);
                arcRoiLabelBorder = labelBorder;
            }
        }

        #endregion

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // 清除所有功能
            if (isRulerMode) DisableRulerMode();
            if (isRotRectRoiMode) DisableRotRectRoiMode();
            if (isEllipseRoiMode) DisableEllipseRoiMode();
            if (isDrawLineMode) DisableDrawLineMode();
            if (isPolygonRoiMode) DisablePolygonRoiMode();
            if (isArcRoiMode) DisableArcRoiMode();
            if (showCrossLines) RemoveCrossLines();

            // 重置所有按鈕狀態
            var buttons = FindVisualChildren<Button>(this);
            foreach (var button in buttons)
            {
                SetButtonActiveState(button, false);
            }
        }



        // 在類別內變數區加入：
        private Border rotRectRoiLabelBorder = null;
        private Border ellipseRoiLabelBorder = null;
        private List<Border> polygonRoiLabelBorders = new List<Border>();
        private Border arcRoiLabelBorder = null;

        // 新增：二次貝塞爾曲線長度計算與中點計算輔助方法
        private double CalcQuadraticBezierLength(Point p0, Point p1, Point p2, int steps = 32)
        {
            double length = 0;
            Point prev = p0;
            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                Point pt = GetQuadraticBezierPoint(p0, p1, p2, t);
                length += (pt - prev).Length;
                prev = pt;
            }
            return length;
        }
        private Point GetQuadraticBezierPoint(Point p0, Point p1, Point p2, double t)
        {
            double x = (1 - t) * (1 - t) * p0.X + 2 * (1 - t) * t * p1.X + t * t * p2.X;
            double y = (1 - t) * (1 - t) * p0.Y + 2 * (1 - t) * t * p1.Y + t * t * p2.Y;
            return new Point(x, y);
        }

        private void AnimateScale(ScaleTransform scaleTransform, double scaleFactor)
        {
            var scaleAnimation = new DoubleAnimation
            {
                To = scaleFactor,
                Duration = TimeSpan.FromMilliseconds(200),
                FillBehavior = FillBehavior.Stop
            };
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }

        private void SetButtonActiveState(Button button, bool isActive)
        {
            if (button != null)
            {
                button.Tag = isActive ? "Active" : null;
            }
        }

        // 輔助方法：查找視覺樹中的所有指定類型的元素
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        // 輔助方法：根據端點位置決定標註偏移
        private (double offsetX, double offsetY) GetLabelOffset(Point pt, double canvasWidth, double canvasHeight)
        {
            double offsetX = 16, offsetY = -16;
            // 右下象限
            if (pt.X > canvasWidth * 0.66 && pt.Y > canvasHeight * 0.66) { offsetX = -80; offsetY = -32; }
            // 右上象限
            else if (pt.X > canvasWidth * 0.66 && pt.Y < canvasHeight * 0.33) { offsetX = -80; offsetY = 16; }
            // 左下象限
            else if (pt.X < canvasWidth * 0.33 && pt.Y > canvasHeight * 0.66) { offsetX = 16; offsetY = -32; }
            // 左上象限
            else if (pt.X < canvasWidth * 0.33 && pt.Y < canvasHeight * 0.33) { offsetX = 16; offsetY = 16; }
            // 右側
            else if (pt.X > canvasWidth * 0.66) { offsetX = -80; }
            // 左側
            else if (pt.X < canvasWidth * 0.33) { offsetX = 16; }
            // 下方
            else if (pt.Y > canvasHeight * 0.66) { offsetY = -32; }
            // 上方
            else if (pt.Y < canvasHeight * 0.33) { offsetY = 16; }
            return (offsetX, offsetY);
        }
    }
}

