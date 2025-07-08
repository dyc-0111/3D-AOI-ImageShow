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
    public partial class MainImageShow : UserControl, IDisposable, INotifyPropertyChanged
    {
        private readonly CrossLinesDrawingService _crossLinesDrawingService;
        private ScaleTransform _mainCanvasScaleTransform;
        private TranslateTransform _mainCanvasTranslateTransform;
        private bool isDraggingCanvas = false;
        private Point lastMousePosition;
        private bool _disposed = false;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainImageShow()
        {
            InitializeComponent();
            _crossLinesDrawingService = new CrossLinesDrawingService();
            
            // 訂閱 DataContext 變更事件
            this.DataContextChanged += OnDataContextChanged;
            
            // 綁定滑鼠事件到 ImageViewGrid
            this.Loaded += (s, e) => {
                if (ImageViewGrid != null)
                {
                    ImageViewGrid.MouseWheel += OnMouseWheel;
                    ImageViewGrid.MouseDown += OnMouseDown;
                    ImageViewGrid.MouseUp += OnMouseUp;
                    ImageViewGrid.MouseMove += OnMouseMove;
                }
            };
            
            // 添加全局鍵盤事件處理器
            this.KeyDown += OnKeyDown;
            
            // 訂閱應用程式級別的鍵盤事件
            SubscribeToMainWindowEvents();
            
            // 為ListView添加鍵盤事件處理
            this.Loaded += (s, e) => {
                if (RoiListView != null)
                {
                    RoiListView.KeyDown += OnRoiListViewKeyDown;
                }
            };
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
                
                if (ImageViewGrid != null)
                {
                    ImageViewGrid.MouseWheel -= OnMouseWheel;
                    ImageViewGrid.MouseDown -= OnMouseDown;
                    ImageViewGrid.MouseUp -= OnMouseUp;
                    ImageViewGrid.MouseMove -= OnMouseMove;
                }
                
                if (MainCanvasGrid != null)
                {
                    MainCanvasGrid.SizeChanged -= OnMainCanvasGridSizeChanged;
                }
                
                if (ImageViewGrid != null)
                {
                    ImageViewGrid.SizeChanged -= OnImageViewGridSizeChanged;
                }
                
                if (MainImage != null)
                {
                    MainImage.SizeChanged -= OnMainImageSizeChanged;
                }
                
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }
                
                if (RoiListView != null)
                {
                    RoiListView.KeyDown -= OnRoiListViewKeyDown;
                }
                
                _disposed = true;
            }
        }

        private ImageShowViewModel ViewModel => DataContext as ImageShowViewModel;

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(ImageViewGrid);

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
                // 使用 MainImage 的實際邊界而不是 MainCanvas
                if (MainImage?.Source != null && MainImage.ActualWidth > 0 && MainImage.ActualHeight > 0)
                {
                    // 調試：輸出座標和邊界資訊
                    System.Diagnostics.Debug.WriteLine($"[MainImageShow] Mouse: ({pos.X:F1}, {pos.Y:F1}), MainImage: {MainImage.ActualWidth:F1}x{MainImage.ActualHeight:F1}, MainCanvas: {MainCanvas.ActualWidth:F1}x{MainCanvas.ActualHeight:F1}");
                    
                    // 檢查滑鼠座標是否與MainCanvas座標系匹配
                    Point canvasPos = pos; // ImageViewGrid 和 MainCanvas 應該是同一座標系
                    ViewModel?.HandleMouseMoveWithImageBounds(canvasPos, MainCanvas, 0, 0, MainImage.ActualWidth, MainImage.ActualHeight);
                }
                else
                {
                    ViewModel?.HandleMouseMove(pos, MainCanvas);
                }
            }

            // 顯示滑鼠座標
            MousePositionText.Text = $"X: {pos.X:F0}, Y: {pos.Y:F0}";
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(ImageViewGrid);
            
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = true;
                lastMousePosition = e.GetPosition(MainCanvasGrid);
                ImageViewGrid.CaptureMouse();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                // 處理 ROI 互動 - 使用 MainCanvas，這樣 ROI 會跟著圖片縮放
                bool multiDragRoi = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                
                // 同樣在MouseDown時也使用MainImage邊界
                if (MainImage?.Source != null && MainImage.ActualWidth > 0 && MainImage.ActualHeight > 0)
                {
                    ViewModel.HandleMouseDownWithImageBounds(pos, MainCanvas, multiDragRoi, 0, 0, MainImage.ActualWidth, MainImage.ActualHeight);
                }
                else
                {
                    ViewModel.HandleMouseDown(pos, MainCanvas, multiDragRoi);
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                // 處理右鍵事件
                ViewModel?.HandleRightMouseDown(pos, MainCanvas);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(ImageViewGrid);
            
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = false;
                ImageViewGrid.ReleaseMouseCapture();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                // 處理 ROI 互動 - 使用 MainCanvas，這樣 ROI 會跟著圖片縮放
                ViewModel?.HandleMouseUp(pos, MainCanvas);
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainImage?.Source == null) return;

            double zoom = e.Delta > 0 ? 1.1 : 0.9;
            double newScaleX = MainCanvasScaleTransform.ScaleX * zoom;
            double newScaleY = MainCanvasScaleTransform.ScaleY * zoom;

            // 限制縮放範圍
            const double minZoom = 0.1;
            const double maxZoom = 10.0;
            
            newScaleX = Math.Max(minZoom, Math.Min(maxZoom, newScaleX));
            newScaleY = Math.Max(minZoom, Math.Min(maxZoom, newScaleY));

            MainCanvasScaleTransform.ScaleX = newScaleX;
            MainCanvasScaleTransform.ScaleY = newScaleY;
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
                
                // 訂閱ImageViewGrid尺寸變更事件來同步Canvas尺寸
                if (ImageViewGrid != null)
                {
                    ImageViewGrid.SizeChanged += OnImageViewGridSizeChanged;
                }
                
                // 訂閱MainImage尺寸變更事件來重新繪製CrossLine
                if (MainImage != null)
                {
                    MainImage.SizeChanged += OnMainImageSizeChanged;
                }
                
                // 訂閱MainCanvasGrid尺寸變更事件
                MainCanvasGrid.SizeChanged += OnMainCanvasGridSizeChanged;
                
                // 初始化十字線
                UpdateCrossLines();

                // 監聽 ROI 清單收合/展開，動態調整欄寬
                AdjustRoiColumnWidth(ViewModel.IsRoiPanelVisible);
            }
        }

        private void OnMainCanvasGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 當MainCanvasGrid尺寸改變時，重新繪製十字線
            UpdateCrossLines();
        }

        private void OnImageViewGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Canvas 尺寸已通過綁定自動同步，這裡可以處理其他邏輯
            // 例如重新繪製十字線或通知 ViewModel
        }

        private void OnMainImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 當MainImage尺寸改變時，重新繪製十字線
            UpdateCrossLines();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageShowViewModel.ShowCrossLines))
            {
                UpdateCrossLines();
            }
            else if (e.PropertyName == nameof(ImageShowViewModel.IsRoiPanelVisible))
            {
                AdjustRoiColumnWidth(ViewModel.IsRoiPanelVisible);
                OnPropertyChanged(nameof(PopupPlacementTarget));
            }
            else if (e.PropertyName == nameof(ImageShowViewModel.IsToolbarVisible))
            {
                AdjustToolbarRowHeight(ViewModel.IsToolbarVisible);
            }
        }

        private void UpdateCrossLines()
        {
            if (ViewModel == null) 
            {
                return;
            }

            if (ViewModel.ShowCrossLines)
            {
                if (MainImage?.Source != null && MainImage.ActualWidth > 0 && MainImage.ActualHeight > 0)
                {
                    // CrossLine 畫在 MainCanvas 上，顯示在 MainImage 的正中央
                    // 使用 MainImage 的實際尺寸
                    _crossLinesDrawingService.DrawCrossLines(MainCanvas, MainImage.ActualWidth, MainImage.ActualHeight);
                }
            }
            else
            {
                _crossLinesDrawingService.ClearCrossLines(MainCanvas);
            }
        }

        public void FitImageToWindow()
        {
            if (MainImage?.Source == null || MainCanvasScaleTransform == null || MainCanvasTranslateTransform == null)
                return;

            // 重置縮放和平移，讓 Viewbox 自動處理置中
            MainCanvasScaleTransform.ScaleX = 1.0;
            MainCanvasScaleTransform.ScaleY = 1.0;
            MainCanvasTranslateTransform.X = 0;
            MainCanvasTranslateTransform.Y = 0;
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

        private void OnRoiListViewKeyDown(object sender, KeyEventArgs e)
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
                
                if (ImageViewGrid != null)
                {
                    ImageViewGrid.SizeChanged += OnImageViewGridSizeChanged;
                }
                
                if (MainImage != null)
                {
                    MainImage.SizeChanged += OnMainImageSizeChanged;
                }
                
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
            // Point
            var pointServiceField = typeof(ImageShowViewModel).GetField("_pointRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pointService = pointServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PointRoiService;
            var pointDrawingServiceField = typeof(ImageShowViewModel).GetField("_pointRoiDrawingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pointDrawingService = pointDrawingServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PointRoiDrawingService;
            if (pointService != null)
            {
                pointService.Points.Clear();
                foreach (var data in roiDataList)
                {
                    if (data.Type == RoiType.Point)
                    {
                        var roi = RoiDataConverter.ToPointItem(data);
                        pointService.Points.Add(roi);
                        roiManagementService?.AddRoiItem(roi);
                    }
                }
                pointDrawingService?.DrawRois(MainCanvas, pointService.Points.ToList(), null, true);
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
            // Point
            var pointServiceField = typeof(ImageShowViewModel).GetField("_pointRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pointService = lineServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PointRoiService;
            if (pointService != null)
            {
                foreach (var item in pointService.Points)
                    result.Add(RoiDataConverter.FromPointItem(item));
            }
            return result;
        }

        private void AdjustRoiColumnWidth(bool isVisible)
        {
            var storyboard = (System.Windows.Media.Animation.Storyboard)FindResource(isVisible ? "ExpandRoiPanel" : "CollapseRoiPanel");
            storyboard.Begin();
        }

        private void AdjustToolbarRowHeight(bool isVisible)
        {
            var storyboard = (System.Windows.Media.Animation.Storyboard)FindResource(isVisible ? "ExpandToolbar" : "CollapseToolbar");
            storyboard.Begin();
        }

        public void ToggleToolbarVisible(bool isVisible = true)
        {
            if (ViewModel != null)
            {
                ViewModel.IsToolbarVisible = isVisible;
            }
        }

        public void ToggleRoiPanelVisible(bool isVisible = true)
        {
            if (ViewModel != null)
            {
                ViewModel.IsRoiPanelVisible = isVisible;
            }
        }
        public UIElement PopupPlacementTarget
        {
            get
            {
                return ViewModel != null && ViewModel.IsRoiPanelVisible ? (UIElement)RoiPanel : (UIElement)MainCanvasGrid;
            }
        }
    }
}