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
            
            // 綁定滑鼠事件到 MainCanvas
            this.Loaded += (s, e) => {
                if (MainCanvas != null)
                {
                    MainCanvas.MouseWheel += OnMouseWheel;
                    MainCanvas.MouseDown += OnMouseDown;
                    MainCanvas.MouseUp += OnMouseUp;
                    MainCanvas.MouseMove += OnMouseMove;
                }
            };
            
            // 監聽圖片載入事件
            this.Loaded += (s, e) => {
                if (MainImage != null)
                {
                    MainImage.Loaded += OnMainImageLoaded;
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

        private void OnMainImageLoaded(object sender, RoutedEventArgs e)
        {
            // 圖片載入完成後，延遲執行適應視窗
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FitImageToWindow();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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
                
                if (MainImage != null)
                {
                    MainImage.Loaded -= OnMainImageLoaded;
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
                
                // 清除座標轉換器（如果當前實例是活動的轉換器）
                if (Models.BaseItem.CoordinateConverter != null &&
                    Models.BaseItem.CoordinateConverter.Target == (object)this)
                {
                    Models.BaseItem.SetActiveCoordinateConverter(null);
                }
                
                _disposed = true;
            }
        }

        private ImageShowViewModel ViewModel => DataContext as ImageShowViewModel;

        /// <summary>
        /// 將顯示座標轉換為原始圖片座標（僅用於顯示給用戶）
        /// </summary>
        /// <param name="displayPos">顯示座標</param>
        /// <returns>原始圖片座標</returns>
        public Point ConvertDisplayToOriginalCoordinates(Point displayPos)
        {
            System.Windows.Media.Imaging.BitmapSource imageSource = null;
            
            // 優先使用 MainImage.Source
            if (MainImage?.Source != null)
            {
                imageSource = MainImage.Source as System.Windows.Media.Imaging.BitmapSource;
                System.Diagnostics.Debug.WriteLine($"[MainImageShow.ConvertDisplayToOriginalCoordinates] 使用 MainImage.Source");
            }
            
            // 如果 MainImage.Source 為 null，嘗試使用 ViewModel.ImageSource
            if (imageSource == null && ViewModel?.ImageSource != null)
            {
                imageSource = ViewModel.ImageSource as System.Windows.Media.Imaging.BitmapSource;
                System.Diagnostics.Debug.WriteLine($"[MainImageShow.ConvertDisplayToOriginalCoordinates] MainImage.Source為null，回退使用 ViewModel.ImageSource");
            }
            
            if (imageSource == null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainImageShow.ConvertDisplayToOriginalCoordinates] 尚未載入圖片，無法轉換座標！");
                return new Point(double.NaN, double.NaN);
            }

            // 獲取原始圖片尺寸和顯示尺寸
            double originalWidth = imageSource.PixelWidth;
            double originalHeight = imageSource.PixelHeight;
            double displayWidth = MainImage.ActualWidth;
            double displayHeight = MainImage.ActualHeight;


            if (displayWidth <= 0 || displayHeight <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MainImageShow.ConvertDisplayToOriginalCoordinates] 顯示尺寸無效，返回原座標");
                return displayPos;
            }

            // 簡單的比例轉換：顯示座標 → 原始圖片座標
            double scaleX = originalWidth / displayWidth;
            double scaleY = originalHeight / displayHeight;

            double imageX = displayPos.X * scaleX;
            double imageY = displayPos.Y * scaleY;

            // 確保座標在範圍內
            imageX = Math.Max(0, Math.Min(originalWidth, imageX));
            imageY = Math.Max(0, Math.Min(originalHeight, imageY));

            return new Point(imageX, imageY);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);

            // 新增：判斷是否超出圖片邊界
            if (pos.X < 0 || pos.X > MainCanvas.ActualWidth || pos.Y < 0 || pos.Y > MainCanvas.ActualHeight)
            {
                if (MainCanvas.IsMouseCaptured)
                    MainCanvas.ReleaseMouseCapture();
                ViewModel?.HandleMouseUp(pos, MainCanvas);
                return;
            }

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
                // 檢查是否有可用的圖像源（MainImage.Source 或 ViewModel.ImageSource）
                bool hasImageSource = (MainImage?.Source != null) || (ViewModel?.ImageSource != null);
                bool hasValidDimensions = MainImage.ActualWidth > 0 && MainImage.ActualHeight > 0;
                
                if (hasImageSource && hasValidDimensions)
                {
                    ViewModel?.HandleMouseMoveWithImageBounds(pos, MainCanvas, 0, 0, MainImage.ActualWidth, MainImage.ActualHeight);
                }
                else
                {
                    ViewModel?.HandleMouseMove(pos, MainCanvas);
                }
            }

            // 顯示原始圖片座標給用戶
            Point displayCoord = ConvertDisplayToOriginalCoordinates(pos);
            MousePositionText.Text = $"X: {displayCoord.X:F0}, Y: {displayCoord.Y:F0}";
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {

            Point pos = e.GetPosition(MainCanvas);
            
            if (e.ChangedButton == MouseButton.Right)
            {
                isDraggingCanvas = true;
                lastMousePosition = e.GetPosition(MainCanvasGrid);
                MainCanvas.CaptureMouse();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                if (!MainCanvas.IsMouseCaptured)
                    MainCanvas.CaptureMouse();
                // 處理 ROI 互動 - 使用顯示座標
                bool multiDragRoi = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                
                // 檢查是否有可用的圖像源（MainImage.Source 或 ViewModel.ImageSource）
                bool hasImageSource = (MainImage?.Source != null) || (ViewModel?.ImageSource != null);
                bool hasValidDimensions = MainImage.ActualWidth > 0 && MainImage.ActualHeight > 0;
                
                if (hasImageSource && hasValidDimensions)
                {
                    // 使用顯示尺寸作為邊界
                    ViewModel.HandleMouseDownWithImageBounds(pos, MainCanvas, multiDragRoi, 0, 0, MainImage.ActualWidth, MainImage.ActualHeight);
                }
                else
                {
                    ViewModel.HandleMouseDown(pos, MainCanvas, multiDragRoi);
                }
            }
            //else if (e.ChangedButton == MouseButton.Right)
            //{
            //    // 處理右鍵事件
            //    ViewModel?.HandleRightMouseDown(pos, MainCanvas);
            //}
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainCanvas);
            
            if (e.ChangedButton == MouseButton.Right)
            {
                isDraggingCanvas = false;
                MainCanvas.ReleaseMouseCapture();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                // 處理 ROI 互動 - 使用顯示座標
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

            e.Handled = true;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SetCanvas(MainCanvas);
                
                // 設置MainImageShow引用以進行座標轉換
                ViewModel.SetMainImageShow(this);
                
                // 設置為當前活動的座標轉換器（避免多實例覆蓋問題）
                Models.BaseItem.SetActiveCoordinateConverter(this);
                
                // 初始化變換物件
                _mainCanvasScaleTransform = MainCanvasScaleTransform;
                _mainCanvasTranslateTransform = MainCanvasTranslateTransform;

                // 設置變換更新Action
                ViewModel.SetTransformActions(
                    zoom => _mainCanvasScaleTransform.ScaleX = _mainCanvasScaleTransform.ScaleY = zoom,
                    pan => { _mainCanvasTranslateTransform.X = pan.X; _mainCanvasTranslateTransform.Y = pan.Y; },
                    null, // zoomAndPanAction
                    () => new Size(MainCanvasGrid.ActualWidth, MainCanvasGrid.ActualHeight), // getDisplaySizeAction
                    () => FitImageToWindow() // fitImageToWindowAction
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
            else if (e.PropertyName == nameof(ImageShowViewModel.ImageSource))
            {
                // 當圖片來源改變時，更新Canvas尺寸和十字線
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
                if (MainImage?.Source != null && MainCanvas != null && MainCanvas.ActualWidth > 0 && MainCanvas.ActualHeight > 0)
                {
                    // CrossLine 畫在 MainCanvas 上，使用Canvas的實際尺寸（與Image顯示尺寸相同）
                    _crossLinesDrawingService.DrawCrossLines(MainCanvas, MainCanvas.ActualWidth, MainCanvas.ActualHeight);
                }
            }
            else
            {
                _crossLinesDrawingService.ClearCrossLines(MainCanvas);
            }
        }

        public void FitImageToWindow()
        {
            // 只要重置縮放和平移，讓 WPF 的 Stretch="Uniform" 自動處理
            if (MainCanvasScaleTransform == null || MainCanvasTranslateTransform == null)
                return;

            MainCanvasScaleTransform.ScaleX = 1.0;
            MainCanvasScaleTransform.ScaleY = 1.0;
            MainCanvasTranslateTransform.X = 0;
            MainCanvasTranslateTransform.Y = 0;
        }

        /// <summary>
        /// 設定圖片縮放比例
        /// </summary>
        /// <param name="scale">縮放比例</param>
        public void SetImageScale(double scale)
        {
            if (MainCanvasScaleTransform == null) return;

            const double minZoom = 0.1;
            const double maxZoom = 10.0;
            
            scale = Math.Max(minZoom, Math.Min(maxZoom, scale));
            
            MainCanvasScaleTransform.ScaleX = scale;
            MainCanvasScaleTransform.ScaleY = scale;
        }

        /// <summary>
        /// 獲取目前圖片縮放比例
        /// </summary>
        /// <returns>縮放比例</returns>
        public double GetImageScale()
        {
            return MainCanvasScaleTransform?.ScaleX ?? 1.0;
        }

        /// <summary>
        /// 重置圖片位置和縮放
        /// </summary>
        public void ResetImageTransform()
        {
            if (MainCanvasScaleTransform == null || MainCanvasTranslateTransform == null) return;

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

            // 取得圖片來源與顯示資訊
            var imageSource = MainImage?.Source as System.Windows.Media.Imaging.BitmapSource ?? ViewModel?.ImageSource as System.Windows.Media.Imaging.BitmapSource;
            if (imageSource == null) return result;
            double displayWidth = MainImage.ActualWidth;
            double displayHeight = MainImage.ActualHeight;
            var scaleTransform = MainCanvasScaleTransform;
            var translateTransform = MainCanvasTranslateTransform;

            // 建立座標轉換器
            var converter = new HyImageShow.ImageShowWPF.Data.CoordinateConverter(imageSource, displayWidth, displayHeight, scaleTransform, translateTransform);

            // RotRect
            var rotRectRoiServiceField = typeof(ImageShowViewModel).GetField("_rotRectRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rotRectRoiService = rotRectRoiServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RotRectRoiService;
            if (rotRectRoiService != null)
            {
                foreach (var item in rotRectRoiService.RotRectRois)
                    result.Add(RoiDataConverter.FromRectRoiItem(item, converter.ToImage, len => converter.ToImageLength(len, true), len => converter.ToImageLength(len, false)));
            }
            // Polygon
            var polygonServiceField = typeof(ImageShowViewModel).GetField("_polygonRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var polygonService = polygonServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PolygonRoiService;
            if (polygonService != null)
            {
                foreach (var item in polygonService.PolygonRois)
                    result.Add(RoiDataConverter.FromPolygonRoiItem(item, converter.ToImage));
            }
            // Ellipse
            var ellipseServiceField = typeof(ImageShowViewModel).GetField("_ellipseRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ellipseService = ellipseServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.EllipseRoiService;
            if (ellipseService != null)
            {
                foreach (var item in ellipseService.EllipseRois)
                    result.Add(RoiDataConverter.FromEllipseRoiItem(item, converter.ToImage, len => converter.ToImageLength(len, true)));
            }
            // CircularArc
            var circularArcServiceField = typeof(ImageShowViewModel).GetField("_circularArcRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var circularArcService = circularArcServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.CircularArcRoiService;
            if (circularArcService != null)
            {
                foreach (var item in circularArcService.CircularArcRois)
                    result.Add(RoiDataConverter.FromCircularArcRoiItem(item, converter.ToImage, len => converter.ToImageLength(len, true)));
            }
            // BezierArc
            var bezierArcServiceField = typeof(ImageShowViewModel).GetField("_bezierArcRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bezierArcService = bezierArcServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.BezierArcRoiService;
            if (bezierArcService != null)
            {
                foreach (var item in bezierArcService.BezierArcRois)
                    result.Add(RoiDataConverter.FromBezierArcRoiItem(item, converter.ToImage));
            }
            // Ruler
            var rulerServiceField = typeof(ImageShowViewModel).GetField("_rulerService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rulerService = rulerServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.RulerService;
            if (rulerService != null)
            {
                foreach (var item in rulerService.RulerItems)
                    result.Add(RoiDataConverter.FromRulerItem(item, converter.ToImage));
            }
            // DrawLine
            var lineServiceField = typeof(ImageShowViewModel).GetField("_lineService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lineService = lineServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.DrawLineService;
            if (lineService != null)
            {
                foreach (var item in lineService.DrawLines)
                    result.Add(RoiDataConverter.FromLineItem(item, converter.ToImage));
            }
            // Point
            var pointServiceField = typeof(ImageShowViewModel).GetField("_pointRoiService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pointService = lineServiceField?.GetValue(ViewModel) as HyImageShow.ImageShowWPF.Services.PointRoiService;
            if (pointService != null)
            {
                foreach (var item in pointService.Points)
                    result.Add(RoiDataConverter.FromPointItem(item, converter.ToImage));
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