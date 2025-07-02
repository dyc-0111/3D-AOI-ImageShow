using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using HyImageShow.ImageShowWPF.ViewModels;
using System.ComponentModel;
using System.Windows.Media.Effects;
using HyImageShow.ImageShowWPF.Services;
using System.Linq;
using System.Collections.Generic;
using HyImageShow.ImageShowWPF.Models;
using System.Windows.Shapes;
using HyImageShow.ImageShowWPF.Data;

namespace HyImageShow.ImageShowWPF.View
{
    /// <summary>
    /// MainImageShow.xaml 的互動邏輯
    /// </summary>
    public partial class MainImageShow : UserControl, IDisposable
    {
        private readonly CrossLinesDrawingService _crossLinesDrawingService;
        private ScaleTransform _mainCanvasScaleTransform;
        private TranslateTransform _mainCanvasTranslateTransform;
        private bool isDraggingCanvas = false;
        private Point lastMousePosition;
        private bool _disposed = false;

        public MainImageShow()
        {
            InitializeComponent();
            _crossLinesDrawingService = new CrossLinesDrawingService();
            
            // 訂閱 DataContext 變更事件
            this.DataContextChanged += OnDataContextChanged;
            
            MainCanvas.MouseWheel += OnMouseWheel;
            MainCanvas.MouseDown += OnMouseDown;
            MainCanvas.MouseUp += OnMouseUp;
            MainCanvas.MouseMove += OnMouseMove;
            
            // 添加全局鍵盤事件處理器
            this.KeyDown += OnKeyDown;
            
            // 訂閱應用程式級別的鍵盤事件
            SubscribeToMainWindowEvents();
        }

        private void SubscribeToMainWindowEvents()
        {
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.KeyDown += OnMainWindowKeyDown;
            }
            else
            {
                // 如果MainWindow還沒準備好，等待它加載完成
                Application.Current.MainWindow.Loaded += OnMainWindowChanged;
            }
        }

        private void OnMainWindowChanged(object sender, EventArgs e)
        {
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.KeyDown += OnMainWindowKeyDown;
                Application.Current.MainWindow.Loaded -= OnMainWindowChanged;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 移除事件訂閱
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.KeyDown -= OnMainWindowKeyDown;
                }
                
                // 移除 MainWindowChanged 事件訂閱
                Application.Current.MainWindow.Loaded -= OnMainWindowChanged;
                
                this.KeyDown -= OnKeyDown;
                this.DataContextChanged -= OnDataContextChanged;
                
                if (MainCanvas != null)
                {
                    MainCanvas.MouseWheel -= OnMouseWheel;
                    MainCanvas.MouseDown -= OnMouseDown;
                    MainCanvas.MouseUp -= OnMouseUp;
                    MainCanvas.MouseMove -= OnMouseMove;
                }
                
                if (MainCanvasGrid != null)
                {
                    MainCanvasGrid.SizeChanged -= OnMainCanvasGridSizeChanged;
                }
                
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }
                
                _disposed = true;
            }
        }

        private ImageShowViewModel ViewModel => DataContext as ImageShowViewModel;

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);

            if (isDraggingCanvas)
            {
                var currentPosition = e.GetPosition(MainCanvasGrid);
                var offset = currentPosition - lastMousePosition;
                MainCanvasTranslateTransform.X += offset.X;
                MainCanvasTranslateTransform.Y += offset.Y;
                lastMousePosition = currentPosition;
            }
            else
            {
                // 處理 ROI 互動 - 使用 MainCanvas，這樣 ROI 會跟著圖片縮放
                ViewModel?.HandleMouseMove(pos, MainCanvas);
            }

            // 顯示滑鼠座標
            MousePositionText.Text = $"X: {pos.X:F0}, Y: {pos.Y:F0}";
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = true;
                lastMousePosition = e.GetPosition(MainCanvasGrid);
                MainCanvas.CaptureMouse();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                // 處理 ROI 互動 - 使用 MainCanvas，這樣 ROI 會跟著圖片縮放
                bool multiDragRoi = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                ViewModel.HandleMouseDown(pos, MainCanvas, multiDragRoi);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                // 處理右鍵事件
                ViewModel?.HandleRightMouseDown(pos, MainCanvas);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = false;
                MainCanvas.ReleaseMouseCapture();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                // 處理 ROI 互動 - 使用 MainCanvas，這樣 ROI 會跟著圖片縮放
                ViewModel?.HandleMouseUp(pos, MainCanvas);
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? 1.1 : 0.9;
            MainCanvasScaleTransform.ScaleX *= zoom;
            MainCanvasScaleTransform.ScaleY *= zoom;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SetCanvas(MainCanvas);
                ViewModel.SetOverlayCanvas(OverlayCanvas);
                
                // 初始化變換物件
                _mainCanvasScaleTransform = MainCanvasScaleTransform;
                _mainCanvasTranslateTransform = MainCanvasTranslateTransform;

                // 設置變換更新Action
                ViewModel.SetTransformActions(
                    zoom => _mainCanvasScaleTransform.ScaleX = _mainCanvasScaleTransform.ScaleY = zoom,
                    pan => { _mainCanvasTranslateTransform.X = pan.X; _mainCanvasTranslateTransform.Y = pan.Y; },
                    null, // zoomAndPanAction
                    () => new Size(MainCanvasGrid.ActualWidth, MainCanvasGrid.ActualHeight) // getDisplaySizeAction
                );

                // 訂閱ShowCrossLines屬性變更事件
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // 訂閱MainCanvasGrid尺寸變更事件
                MainCanvasGrid.SizeChanged += OnMainCanvasGridSizeChanged;
                
                // 初始化十字線
                UpdateCrossLines();
            }
        }

        private void OnMainCanvasGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 當MainCanvasGrid尺寸改變時，重新繪製十字線
            UpdateCrossLines();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageShowViewModel.ShowCrossLines))
            {
                UpdateCrossLines();
            }
        }

        private void UpdateCrossLines()
        {
            if (ViewModel == null) 
            {
                return;
            }

            // 使用MainCanvasGrid的尺寸，因為CrossLinesOverlay現在在MainCanvasGrid內部
            double canvasWidth = MainCanvasGrid.ActualWidth;
            double canvasHeight = MainCanvasGrid.ActualHeight;

            if (ViewModel.ShowCrossLines)
            {
                // 檢查Canvas尺寸，如果為0則等待SizeChanged事件
                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    return;
                }
                
                _crossLinesDrawingService.DrawCrossLines(OverlayCanvas, canvasWidth, canvasHeight);
            }
            else
            {
                _crossLinesDrawingService.ClearCrossLines(OverlayCanvas);
            }
        }

        public void FitImageToWindow()
        {
            if (MainCanvas == null || MainCanvasScaleTransform == null || MainCanvasTranslateTransform == null)
                return;

            var image = MainCanvas.Children.OfType<Image>().FirstOrDefault();
            if (image?.Source == null)
                return;

            double imgWidth = image.Source.Width;
            double imgHeight = image.Source.Height;
            double gridWidth = MainCanvasGrid.ActualWidth;
            double gridHeight = MainCanvasGrid.ActualHeight;

            if (imgWidth <= 0 || imgHeight <= 0 || gridWidth <= 0 || gridHeight <= 0)
                return;

            double scale = Math.Min(gridWidth / imgWidth, gridHeight / imgHeight);
            double offsetX = (gridWidth - imgWidth * scale) / 2;
            double offsetY = (gridHeight - imgHeight * scale) / 2;

            MainCanvasScaleTransform.ScaleX = scale;
            MainCanvasScaleTransform.ScaleY = scale;
            MainCanvasTranslateTransform.X = offsetX;
            MainCanvasTranslateTransform.Y = offsetY;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel != null)
            {
                // 將鍵盤事件傳遞給ViewModel處理
                ViewModel.HandleKeyDown(e.Key);
                e.Handled = true;
            }
        }

        private void OnMainWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel != null)
            {
                // 將鍵盤事件傳遞給ViewModel處理
                ViewModel.HandleKeyDown(e.Key);
                e.Handled = true;
            }
        }

        private void MainImageShow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null)
                this.DataContext = HyImageShow.ImageShowWPF.ViewModels.ImageShowViewModel.CreateDefaultServices();

            if (ViewModel != null)
            {
                ViewModel.SetCanvas(MainCanvas);
                ViewModel.SetOverlayCanvas(OverlayCanvas);
                ViewModel.SetTransformActions(
                    zoom => MainCanvasScaleTransform.ScaleX = MainCanvasScaleTransform.ScaleY = zoom,
                    pan => { MainCanvasTranslateTransform.X = pan.X; MainCanvasTranslateTransform.Y = pan.Y; },
                    null,
                    () => new Size(MainCanvasGrid.ActualWidth, MainCanvasGrid.ActualHeight)
                );
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                MainCanvasGrid.SizeChanged += OnMainCanvasGridSizeChanged;
                UpdateCrossLines();
            }
        }


        public void SetAllRoiData(List<RoiData> roiDataList)
        {
            if (ViewModel == null) return;
            // 先清空所有 ROI
            var roiManagementServiceField = typeof(ImageShowViewModel).GetField("_roiManagementService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var roiManagementService = roiManagementServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RoiManagementService;
            roiManagementService?.ClearRoiItems();

            // RotRect
            var rotRectRoiServiceField = typeof(ImageShowViewModel).GetField("_rotRectRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rotRectRoiService = rotRectRoiServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RotRectRoiService;
            var rotRectRoiDrawingServiceField = typeof(ImageShowViewModel).GetField("_rotRectRoiDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rotRectRoiDrawingService = rotRectRoiDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RotRectRoiDrawingService;
            if (rotRectRoiService != null)
            {
                rotRectRoiService.RotRectRois.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.RotRect)
                    {
                        var roi = RoiDataConverter.ToRectRoiItem(data);
                        rotRectRoiService.RotRectRois.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                rotRectRoiDrawingService?.DrawRois(MainCanvas, rotRectRoiService.RotRectRois.ToList(), null, true);
            }
            // Polygon
            var polygonServiceField = typeof(ImageShowViewModel).GetField("_polygonRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var polygonService = polygonServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PolygonRoiService;
            var polygonDrawingServiceField = typeof(ImageShowViewModel).GetField("_polygonRoiDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var polygonDrawingService = polygonDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PolygonRoiDrawingService;
            if (polygonService != null)
            {
                polygonService.PolygonRois.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.Polygon)
                    {
                        var roi = RoiDataConverter.ToPolygonRoiItem(data);
                        polygonService.PolygonRois.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                polygonDrawingService?.DrawRois(MainCanvas, polygonService.PolygonRois.ToList(), null, true);
            }
            // Ellipse
            var ellipseServiceField = typeof(ImageShowViewModel).GetField("_ellipseRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ellipseService = ellipseServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.EllipseRoiService;
            var ellipseDrawingServiceField = typeof(ImageShowViewModel).GetField("_ellipseRoiDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ellipseDrawingService = ellipseDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.EllipseRoiDrawingService;
            if (ellipseService != null)
            {
                ellipseService.EllipseRois.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.Ellipse)
                    {
                        var roi = RoiDataConverter.ToEllipseRoiItem(data);
                        ellipseService.EllipseRois.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                ellipseDrawingService?.DrawRois(MainCanvas, ellipseService.EllipseRois.ToList(), null, true);
            }
            // CircularArc
            var circularArcServiceField = typeof(ImageShowViewModel).GetField("_circularArcRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var circularArcService = circularArcServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.CircularArcRoiService;
            var circularArcDrawingServiceField = typeof(ImageShowViewModel).GetField("_circularArcRoiDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var circularArcDrawingService = circularArcDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.CircularArcRoiDrawingService;
            if (circularArcService != null)
            {
                circularArcService.CircularArcRois.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.CircularArc)
                    {
                        var roi = RoiDataConverter.ToCircularArcRoiItem(data);
                        circularArcService.CircularArcRois.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                circularArcDrawingService?.DrawRois(MainCanvas, circularArcService.CircularArcRois.ToList(), null, true);
            }
            // BezierArc
            var bezierArcServiceField = typeof(ImageShowViewModel).GetField("_bezierArcRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bezierArcService = bezierArcServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.BezierArcRoiService;
            var bezierArcDrawingServiceField = typeof(ImageShowViewModel).GetField("_bezierArcRoiDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bezierArcDrawingService = bezierArcDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.BezierArcRoiDrawingService;
            if (bezierArcService != null)
            {
                bezierArcService.BezierArcRois.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.BezierArc)
                    {
                        var roi = RoiDataConverter.ToBezierArcRoiItem(data);
                        bezierArcService.BezierArcRois.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                bezierArcDrawingService?.DrawRois(MainCanvas, bezierArcService.BezierArcRois.ToList(), null, true);
            }
            // Ruler
            var rulerServiceField = typeof(ImageShowViewModel).GetField("_rulerService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rulerService = rulerServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RulerService;
            var rulerDrawingServiceField = typeof(ImageShowViewModel).GetField("_rulerDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rulerDrawingService = rulerDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RulerDrawingService;
            if (rulerService != null)
            {
                rulerService.RulerItems.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.Ruler)
                    {
                        var roi = RoiDataConverter.ToRulerItem(data);
                        rulerService.RulerItems.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                rulerDrawingService?.DrawRois(MainCanvas, rulerService.RulerItems.ToList(), null, true);
            }
            // DrawLine
            var lineServiceField = typeof(ImageShowViewModel).GetField("_lineService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lineService = lineServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.DrawLineService;
            var lineDrawingServiceField = typeof(ImageShowViewModel).GetField("_drawLineDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lineDrawingService = lineDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.DrawLineDrawingService;
            if (lineService != null)
            {
                lineService.DrawLines.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.Line)
                    {
                        var roi = RoiDataConverter.ToLineItem(data);
                        lineService.DrawLines.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                lineDrawingService?.DrawRois(MainCanvas, lineService.DrawLines.ToList(), null, true);
            }
            // 同步 ListView
            (ViewModel as HyImageShow.ImageShowWPF.ViewModels.ImageShowViewModel)?.RefreshAllRoiItems();
        }

        // 批次傳出所有支援型別 ROI
        public List<RoiData> GetAllRoiData()
        {
            var result = new List<RoiData>();
            if (ViewModel == null) return result;
            // RotRect
            var rotRectRoiServiceField = typeof(ImageShowViewModel).GetField("_rotRectRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rotRectRoiService = rotRectRoiServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RotRectRoiService;
            if (rotRectRoiService != null)
            {
                foreach (var item in rotRectRoiService.RotRectRois)
                    result.Add(RoiDataConverter.FromRectRoiItem(item));
            }
            // Polygon
            var polygonServiceField = typeof(ImageShowViewModel).GetField("_polygonRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var polygonService = polygonServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PolygonRoiService;
            if (polygonService != null)
            {
                foreach (var item in polygonService.PolygonRois)
                    result.Add(RoiDataConverter.FromPolygonRoiItem(item));
            }
            // Ellipse
            var ellipseServiceField = typeof(ImageShowViewModel).GetField("_ellipseRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ellipseService = ellipseServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.EllipseRoiService;
            if (ellipseService != null)
            {
                foreach (var item in ellipseService.EllipseRois)
                    result.Add(RoiDataConverter.FromEllipseRoiItem(item));
            }
            // CircularArc
            var circularArcServiceField = typeof(ImageShowViewModel).GetField("_circularArcRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var circularArcService = circularArcServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.CircularArcRoiService;
            if (circularArcService != null)
            {
                foreach (var item in circularArcService.CircularArcRois)
                    result.Add(RoiDataConverter.FromCircularArcRoiItem(item));
            }
            // BezierArc
            var bezierArcServiceField = typeof(ImageShowViewModel).GetField("_bezierArcRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bezierArcService = bezierArcServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.BezierArcRoiService;
            if (bezierArcService != null)
            {
                foreach (var item in bezierArcService.BezierArcRois)
                    result.Add(RoiDataConverter.FromBezierArcRoiItem(item));
            }
            // Ruler
            var rulerServiceField = typeof(ImageShowViewModel).GetField("_rulerService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rulerService = rulerServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RulerService;
            if (rulerService != null)
            {
                foreach (var item in rulerService.RulerItems)
                    result.Add(RoiDataConverter.FromRulerItem(item));
            }
            // DrawLine
            var lineServiceField = typeof(ImageShowViewModel).GetField("_lineService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lineService = lineServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.DrawLineService;
            if (lineService != null)
            {
                foreach (var item in lineService.DrawLines)
                    result.Add(RoiDataConverter.FromLineItem(item));
            }
            return result;
        }
    }
}