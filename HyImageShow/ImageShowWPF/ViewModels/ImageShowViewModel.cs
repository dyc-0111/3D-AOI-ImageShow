using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using HyImageShow.ImageShowWPF.Commands;
using HyImageShow.ImageShowWPF.Models;
using HyImageShow.ImageShowWPF.Services;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace HyImageShow.ImageShowWPF.ViewModels
{
    /// <summary>
    /// 圖片顯示的ViewModel
    /// </summary>
    public class ImageShowViewModel : INotifyPropertyChanged
    {
        private readonly RoiManagementService _roiManagementService;
        private readonly DrawLineService _lineService;
        private readonly RulerService _rulerService;
        private readonly RotRectRoiService _rotRectRoiService;
        private readonly EllipseRoiService _ellipseRoiService;
        private readonly PolygonRoiService _polygonRoiService;
        private readonly BezierArcRoiService _bezierArcRoiService;
        private readonly CircularArcRoiService _circularArcRoiService;
        private readonly CrossLinesService _crossLinesService;
        private readonly RulerDrawingService _rulerDrawingService;
        private readonly RotRectRoiDrawingService _rotRectRoiDrawingService;
        private readonly EllipseRoiDrawingService _ellipseRoiDrawingService;
        private readonly PolygonRoiDrawingService _polygonRoiDrawingService;
        private readonly BezierArcRoiDrawingService _bezierArcRoiDrawingService;
        private readonly CircularArcRoiDrawingService _circularArcRoiDrawingService;
        private readonly CrossLinesDrawingService _crossLinesDrawingService;
        private readonly DrawLineDrawingService _drawLineDrawingService;
        private readonly PointRoiService _pointRoiService;
        private readonly PointRoiDrawingService _pointRoiDrawingService;
        private Canvas _mainCanvas;

        private ImageSource _imageSource;
        private bool _showLabels = true;
        private bool _isRoiPanelVisible = true;
        private bool _isToolbarVisible = true;
        
        // Transform Actions - 用於間接控制 View 層的 Transform
        private Action<double> _zoomAction;
        private Action<Point> _panAction;
        private Action<double, Point> _zoomAndPanAction;
        private Func<Size> _getDisplaySizeAction;

        public ImageShowViewModel(
            RoiManagementService roiManagementService,
            DrawLineService lineService,
            RulerService rulerService,
            RotRectRoiService rotRectRoiService,
            EllipseRoiService ellipseRoiService,
            PolygonRoiService polygonRoiService,
            BezierArcRoiService bezierArcRoiService,
            CircularArcRoiService circularArcRoiService,
            CrossLinesService crossLinesService,
            RulerDrawingService rulerDrawingService,
            RotRectRoiDrawingService rotRectRoiDrawingService,
            EllipseRoiDrawingService ellipseRoiDrawingService,
            PolygonRoiDrawingService polygonRoiDrawingService,
            BezierArcRoiDrawingService bezierArcRoiDrawingService,
            CircularArcRoiDrawingService circularArcRoiDrawingService,
            CrossLinesDrawingService crossLinesDrawingService,
            DrawLineDrawingService drawLineDrawingService,
            PointRoiService pointRoiService,
            PointRoiDrawingService pointRoiDrawingService)
        {
            _roiManagementService = roiManagementService ?? throw new ArgumentNullException(nameof(roiManagementService));
            _lineService = lineService ?? throw new ArgumentNullException(nameof(lineService));
            _rulerService = rulerService ?? throw new ArgumentNullException(nameof(rulerService));
            _rotRectRoiService = rotRectRoiService ?? throw new ArgumentNullException(nameof(rotRectRoiService));
            _ellipseRoiService = ellipseRoiService ?? throw new ArgumentNullException(nameof(ellipseRoiService));
            _polygonRoiService = polygonRoiService ?? throw new ArgumentNullException(nameof(polygonRoiService));
            _bezierArcRoiService = bezierArcRoiService ?? throw new ArgumentNullException(nameof(bezierArcRoiService));
            _circularArcRoiService = circularArcRoiService ?? throw new ArgumentNullException(nameof(circularArcRoiService));
            _crossLinesService = crossLinesService ?? throw new ArgumentNullException(nameof(crossLinesService));
            _rulerDrawingService = rulerDrawingService ?? throw new ArgumentNullException(nameof(rulerDrawingService));
            _rotRectRoiDrawingService = rotRectRoiDrawingService ?? throw new ArgumentNullException(nameof(rotRectRoiDrawingService));
            _ellipseRoiDrawingService = ellipseRoiDrawingService ?? throw new ArgumentNullException(nameof(ellipseRoiDrawingService));
            _polygonRoiDrawingService = polygonRoiDrawingService ?? throw new ArgumentNullException(nameof(polygonRoiDrawingService));
            _bezierArcRoiDrawingService = bezierArcRoiDrawingService ?? throw new ArgumentNullException(nameof(bezierArcRoiDrawingService));
            _circularArcRoiDrawingService = circularArcRoiDrawingService ?? throw new ArgumentNullException(nameof(circularArcRoiDrawingService));
            _crossLinesDrawingService = crossLinesDrawingService ?? throw new ArgumentNullException(nameof(crossLinesDrawingService));
            _drawLineDrawingService = drawLineDrawingService ?? throw new ArgumentNullException(nameof(drawLineDrawingService));
            _pointRoiService = pointRoiService ?? throw new ArgumentNullException(nameof(pointRoiService));
            _pointRoiDrawingService = pointRoiDrawingService ?? throw new ArgumentNullException(nameof(pointRoiDrawingService));

            InitializeCommands();
            SubscribeToEvents();
        }

        public static ImageShowViewModel CreateDefaultServices()
        {
            return new ImageShowViewModel(
                new RoiManagementService(),
                new DrawLineService(),
                new RulerService(),
                new RotRectRoiService(),
                new EllipseRoiService(),
                new PolygonRoiService(),
                new BezierArcRoiService(),
                new CircularArcRoiService(),
                new CrossLinesService(),
                new RulerDrawingService(),
                new RotRectRoiDrawingService(),
                new EllipseRoiDrawingService(),
                new PolygonRoiDrawingService(),
                new BezierArcRoiDrawingService(),
                new CircularArcRoiDrawingService(),
                new CrossLinesDrawingService(),
                new DrawLineDrawingService(),
                new PointRoiService(),
                new PointRoiDrawingService()
            );
        }

        #region Properties

        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public ObservableCollection<RoiItem> RoiItems => _roiManagementService.RoiItems;

        public RoiItem SelectedRoiItem
        {
            get => _roiManagementService.SelectedRoiItem;
            set
            {
                if (_roiManagementService.SelectedRoiItem != value)
                {
                    _roiManagementService.SelectedRoiItem = value;
                    OnPropertyChanged(nameof(SelectedRoiItem));
                }
            }
        }

        public bool ShowCrossLines
        {
            get => _crossLinesService.ShowCrossLines;
            set
            {
                if (_crossLinesService.ShowCrossLines != value)
                {
                    _crossLinesService.ShowCrossLines = value;
                    OnPropertyChanged(nameof(ShowCrossLines));
                }
            }
        }

        public bool ShowLabels
        {
            get => _showLabels;
            set
            {
                _showLabels = value;
                OnPropertyChanged(nameof(ShowLabels));
            }
        }

        public bool IsRoiPanelVisible
        {
            get => _isRoiPanelVisible;
            set { _isRoiPanelVisible = value; OnPropertyChanged(nameof(IsRoiPanelVisible)); }
        }

        public bool IsToolbarVisible
        {
            get => _isToolbarVisible;
            set { _isToolbarVisible = value; OnPropertyChanged(nameof(IsToolbarVisible)); }
        }

        // 工具按鈕的 Active 狀態
        public bool IsDrawPointActive => _pointRoiService.IsDrawPointMode;
        public bool IsDrawLineActive => _lineService.IsDrawLineMode;
        public bool IsRulerActive => _rulerService.IsRulerMode;
        public bool IsRotRectRoiActive => _rotRectRoiService.IsRotRectRoiMode;
        public bool IsEllipseRoiActive => _ellipseRoiService.IsEllipseRoiMode;
        public bool IsPolygonRoiActive => _polygonRoiService.IsPolygonRoiMode;
        public bool IsBezierArcRoiActive => _bezierArcRoiService.IsBezierArcRoiMode;
        public bool IsCircularArcRoiActive => _circularArcRoiService.IsCircularArcRoiMode;

        public ObservableCollection<BaseItem> AllRoiItems { get; } = new ObservableCollection<BaseItem>();

        #endregion

        #region Commands

        public RelayCommand OpenFileCommand { get; private set; }
        public RelayCommand SaveFileCommand { get; private set; }
        public RelayCommand NewFileCommand { get; private set; }
        public RelayCommand FitImageCommand { get; private set; }
        public RelayCommand ToggleCrossLinesCommand { get; private set; }
        public RelayCommand ClearAllCommand { get; private set; }
        public RelayCommand DrawLineCommand { get; private set; }
        public RelayCommand RulerCommand { get; private set; }
        public RelayCommand RotRectRoiCommand { get; private set; }
        public RelayCommand EllipseRoiCommand { get; private set; }
        public RelayCommand PolygonRoiCommand { get; private set; }
        public RelayCommand BezierArcRoiCommand { get; private set; }
        public RelayCommand CircularArcRoiCommand { get; private set; }
        public RelayCommand DrawPointCommand { get; private set; }
        
        // Transform 控制命令
        public RelayCommand ZoomInCommand { get; private set; }
        public RelayCommand ZoomOutCommand { get; private set; }
        public RelayCommand ZoomToOriginalCommand { get; private set; }
        public RelayCommand ResetToCenterCommand { get; private set; }

        public RelayCommand ToggleRoiPanelCommand { get; private set; }
        public RelayCommand ToggleToolbarCommand { get; private set; }
        public RelayCommand DeleteSelectedRoiCommand { get; private set; }

        private void InitializeCommands()
        {
            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileCommand = new RelayCommand(SaveFile, CanSaveFile);
            NewFileCommand = new RelayCommand(NewFile);
            FitImageCommand = new RelayCommand(FitImage);
            ToggleCrossLinesCommand = new RelayCommand(ToggleCrossLines);
            ClearAllCommand = new RelayCommand(ClearAll);
            DrawLineCommand = new RelayCommand(EnableDrawLineMode);
            RulerCommand = new RelayCommand(EnableRulerMode);
            RotRectRoiCommand = new RelayCommand(EnableRotRectRoiMode);
            EllipseRoiCommand = new RelayCommand(EnableEllipseRoiMode);
            PolygonRoiCommand = new RelayCommand(EnablePolygonRoiMode);
            BezierArcRoiCommand = new RelayCommand(EnableBezierArcRoiMode);
            CircularArcRoiCommand = new RelayCommand(EnableCircularArcRoiMode);
            DrawPointCommand = new RelayCommand(EnableDrawPointMode);
            ToggleRoiPanelCommand = new RelayCommand(_ => IsRoiPanelVisible = !IsRoiPanelVisible);
            ToggleToolbarCommand = new RelayCommand(_ => IsToolbarVisible = !IsToolbarVisible);
            DeleteSelectedRoiCommand = new RelayCommand(_ => DeleteSelectedRoi(), _ => CanDeleteSelectedRoi());
        }

        #endregion

        #region Command Implementations

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|All files (*.*)|*.*",
                Title = "選擇圖片檔案"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // 清除所有 ROI
                ClearAll();
                LoadImage(openFileDialog.FileName);
            }
        }

        private void SaveFile()
        {
            try
            {
                if (ImageSource == null)
                {
                    MessageBox.Show("沒有圖片可以儲存", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 顯示儲存選項對話框
                var saveOption = ShowSaveOptionsDialog();
                if (!saveOption.HasValue) return;

                // 顯示儲存對話框
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG 圖片 (*.png)|*.png|JPEG 圖片 (*.jpg)|*.jpg|BMP 圖片 (*.bmp)|*.bmp|TIFF 圖片 (*.tiff)|*.tiff",
                    DefaultExt = "png",
                    Title = "儲存圖片檔案"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    BitmapSource bitmapToSave;
                    
                    if (saveOption.Value)
                    {
                        // 儲存包含 ROI 的完整畫面
                        bitmapToSave = RenderCanvasWithRoi();
                    }
                    else
                    {
                        // 只儲存原始圖片
                        bitmapToSave = ImageSource as BitmapSource;
                    }

                    if (bitmapToSave != null)
                    {
                        // 根據檔案副檔名選擇編碼器
                        BitmapEncoder encoder = GetEncoder(saveFileDialog.FileName);
                        encoder.Frames.Add(BitmapFrame.Create(bitmapToSave));

                        // 儲存檔案
                        using (var fileStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                        {
                            encoder.Save(fileStream);
                        }

                        MessageBox.Show($"圖片已成功儲存至：\n{saveFileDialog.FileName}", "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("無法生成圖片", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存檔案失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 顯示儲存選項對話框
        /// </summary>
        /// <returns>true = 包含 ROI，false = 只儲存原始圖片，null = 取消</returns>
        private bool? ShowSaveOptionsDialog()
        {
            var dialog = new Window
            {
                Title = "儲存選項",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 標題
            var titleLabel = new TextBlock
            {
                Text = "請選擇儲存方式：",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(titleLabel, 0);
            grid.Children.Add(titleLabel);

            // 選項
            var originalImageRadio = new RadioButton
            {
                Content = "只儲存原始圖片（不含 ROI）",
                Margin = new Thickness(10, 5, 10, 5),
                IsChecked = true
            };
            Grid.SetRow(originalImageRadio, 1);
            grid.Children.Add(originalImageRadio);

            var withRoiRadio = new RadioButton
            {
                Content = "儲存完整畫面（包含 ROI）",
                Margin = new Thickness(10, 5, 10, 5),
                IsChecked = false
            };
            Grid.SetRow(withRoiRadio, 2);
            grid.Children.Add(withRoiRadio);

            // 按鈕
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            var okButton = new Button
            {
                Content = "確定",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            // 事件處理
            bool? result = null;

            okButton.Click += (sender, e) =>
            {
                result = withRoiRadio.IsChecked == true;
                dialog.Close();
            };

            cancelButton.Click += (sender, e) => dialog.Close();

            dialog.Content = grid;
            dialog.ShowDialog();

            return result;
        }

        /// <summary>
        /// 渲染包含 ROI 的完整 Canvas
        /// </summary>
        /// <returns>渲染後的 BitmapSource</returns>
        private BitmapSource RenderCanvasWithRoi()
        {
            if (_mainCanvas == null || ImageSource == null)
                return null;

            try
            {
                // 獲取 Canvas 的實際尺寸
                double canvasWidth = _mainCanvas.ActualWidth;
                double canvasHeight = _mainCanvas.ActualHeight;

                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    // 如果 Canvas 尺寸為 0，使用 ImageSource 的尺寸
                    var bitmapSource = ImageSource as BitmapSource;
                    if (bitmapSource != null)
                    {
                        canvasWidth = bitmapSource.PixelWidth;
                        canvasHeight = bitmapSource.PixelHeight;
                    }
                    else
                    {
                        return null;
                    }
                }

                // 創建 RenderTargetBitmap
                var renderTarget = new RenderTargetBitmap(
                    (int)canvasWidth,
                    (int)canvasHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                // 渲染 Canvas
                renderTarget.Render(_mainCanvas);

                return renderTarget;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 根據檔案名稱獲取對應的圖片編碼器
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>對應的編碼器</returns>
        private BitmapEncoder GetEncoder(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLower();
            
            switch (extension)
            {
                case ".png":
                    return new PngBitmapEncoder();
                case ".jpg":
                case ".jpeg":
                    var jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.QualityLevel = 95; // 高品質
                    return jpegEncoder;
                case ".bmp":
                    return new BmpBitmapEncoder();
                case ".tiff":
                    return new TiffBitmapEncoder();
                default:
                    return new PngBitmapEncoder(); // 預設使用 PNG
            }
        }

        private bool CanSaveFile()
        {
            return ImageSource != null;
        }

        private void NewFile()
        {
            try
            {
                // 創建一張白色底的空白圖片，填滿整個 Canvas
                CreateWhiteCanvas();
                
                // 清除所有 ROI
                ClearAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"創建空白檔案失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateWhiteCanvas()
        {
            // 獲取當前顯示區域的尺寸
            Size displaySize = _getDisplaySizeAction?.Invoke() ?? new Size(1920, 1080);
            
            int width = (int)displaySize.Width;
            int height = (int)displaySize.Height;
            
            // 確保最小尺寸
            if (width <= 0) width = 1920;
            if (height <= 0) height = 1080;
            
            // 創建 WriteableBitmap
            var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            
            // 鎖定位圖以進行像素操作
            writeableBitmap.Lock();
            
            try
            {
                // 獲取像素資料
                IntPtr backBuffer = writeableBitmap.BackBuffer;
                int stride = writeableBitmap.BackBufferStride;
                
                // 創建白色像素資料 (BGRA: 255, 255, 255, 255)
                byte[] whitePixels = new byte[stride * height];
                for (int i = 0; i < whitePixels.Length; i += 4)
                {
                    whitePixels[i] = 255;     // Blue
                    whitePixels[i + 1] = 255; // Green
                    whitePixels[i + 2] = 255; // Red
                    whitePixels[i + 3] = 255; // Alpha
                }
                
                // 複製像素資料到位圖
                Marshal.Copy(whitePixels, 0, backBuffer, whitePixels.Length);
                
                // 更新整個位圖區域
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                // 解鎖位圖
                writeableBitmap.Unlock();
            }
            
            // 設置為 ImageSource
            ImageSource = writeableBitmap;
            
            // 自動適應視窗（縮放比例設為 1.0，因為畫布已經填滿視窗）
            ZoomTo(1.0);
            PanTo(new Point(0, 0));
        }

        private void FitImage()
        {
            // 使用 Transform Actions 執行適應視窗
            FitImageToWindow();
        }

        private void ToggleCrossLines()
        {
            _crossLinesService.ToggleCrossLines();
        }

        private void ClearAll()
        {
            if (_mainCanvas != null)
            {
                // 先清 Canvas
                _drawLineDrawingService.ClearRois(_mainCanvas, _lineService.DrawLines, _lineService.CurrentDrawLine);
                _rulerDrawingService.ClearRois(_mainCanvas, _rulerService.RulerItems);
                _rotRectRoiDrawingService.ClearRois(_mainCanvas, _rotRectRoiService.RotRectRois);
                _ellipseRoiDrawingService.ClearRois(_mainCanvas, _ellipseRoiService.EllipseRois);
                _polygonRoiDrawingService.ClearRois(_mainCanvas, _polygonRoiService.PolygonRois);
                _bezierArcRoiDrawingService.ClearRois(_mainCanvas, _bezierArcRoiService.BezierArcRois, _bezierArcRoiService.CurrentBezierArcRoi);
                _circularArcRoiDrawingService.ClearRois(_mainCanvas, _circularArcRoiService.CircularArcRois, _circularArcRoiService.CurrentCircularArcRoi);
                _pointRoiDrawingService.ClearRois(_mainCanvas, _pointRoiService.Points, _pointRoiService.CurrentPoint);

                // 再清資料層
                _roiManagementService.ClearRoiItems();
                _lineService.DrawLines.Clear();
                _rulerService.RulerItems.Clear();
                _rotRectRoiService.RotRectRois.Clear();
                _ellipseRoiService.EllipseRois.Clear();
                _polygonRoiService.PolygonRois.Clear();
                _bezierArcRoiService.BezierArcRois.Clear();
                _circularArcRoiService.CircularArcRois.Clear();
                _pointRoiService.Points.Clear();
            }
        }

        private void EnableDrawLineMode()
        {
            DisableAllModes();
            _lineService.EnableDrawLineMode();
            OnPropertyChanged(nameof(IsDrawLineActive));
        }

        private void EnableRulerMode()
        {
            DisableAllModes();
            _rulerService.EnableRulerMode();
            OnPropertyChanged(nameof(IsRulerActive));
        }

        private void EnableRotRectRoiMode()
        {
            DisableAllModes();
            _rotRectRoiService.EnableRotRectRoiMode();
            OnPropertyChanged(nameof(IsRotRectRoiActive));
        }

        private void EnableEllipseRoiMode()
        {
            DisableAllModes();
            _ellipseRoiService.EnableEllipseRoiMode();
            OnPropertyChanged(nameof(IsEllipseRoiActive));
        }

        private void EnablePolygonRoiMode()
        {
            DisableAllModes();
            _polygonRoiService.EnablePolygonRoiMode();
            OnPropertyChanged(nameof(IsPolygonRoiActive));
        }

        private void EnableBezierArcRoiMode()
        {
            DisableAllModes();
            _bezierArcRoiService.EnableBezierArcRoiMode();
            OnPropertyChanged(nameof(IsBezierArcRoiActive));
        }

        private void EnableCircularArcRoiMode()
        {
            DisableAllModes();
            _circularArcRoiService.EnableCircularArcRoiMode();
            OnPropertyChanged(nameof(IsCircularArcRoiActive));
        }

        private void EnableDrawPointMode()
        {
            DisableAllModes();
            _pointRoiService.EnableDrawPointMode();
            OnPropertyChanged(nameof(IsDrawPointActive));
        }

        private void DisableAllModes()
        {
            _lineService.DisableDrawLineMode();
            _rulerService.DisableRulerMode();
            _rotRectRoiService.DisableRotRectRoiMode();
            _ellipseRoiService.DisableEllipseRoiMode();
            _polygonRoiService.DisablePolygonRoiMode();
            _bezierArcRoiService.DisableBezierArcRoiMode();
            _circularArcRoiService.DisableCircularArcRoiMode();
            _pointRoiService.DisableDrawPointMode();
            OnPropertyChanged(nameof(IsDrawLineActive));
            OnPropertyChanged(nameof(IsRulerActive));
            OnPropertyChanged(nameof(IsRotRectRoiActive));
            OnPropertyChanged(nameof(IsEllipseRoiActive));
            OnPropertyChanged(nameof(IsPolygonRoiActive));
            OnPropertyChanged(nameof(IsBezierArcRoiActive));
            OnPropertyChanged(nameof(IsCircularArcRoiActive));
            OnPropertyChanged(nameof(IsDrawPointActive));
        }

        #endregion

        #region Event Handlers

        private void SubscribeToEvents()
        {
            // 訂閱各種服務的事件
            _lineService.LineCompleted += OnLineCompleted;
            _rulerService.RulerCompleted += OnRulerCompleted;
            _rotRectRoiService.RotRectRoiCompleted += OnRotRectRoiCompleted;
            _ellipseRoiService.EllipseRoiCompleted += OnEllipseRoiCompleted;
            _polygonRoiService.PolygonRoiCompleted += OnPolygonRoiCompleted;
            _polygonRoiService.PolygonRoiUpdated += OnPolygonRoiUpdated;
            _bezierArcRoiService.BezierArcRoiCompleted += OnBezierArcRoiCompleted;
            _bezierArcRoiService.BezierArcRoiUpdated += OnBezierArcRoiUpdated;
            _circularArcRoiService.CircularArcRoiCompleted += OnCircularArcRoiCompleted;
            _circularArcRoiService.CircularArcRoiUpdated += OnCircularArcRoiUpdated;
            _pointRoiService.PointCompleted += OnPointCompleted;

            // 訂閱十字線狀態變更事件
            _crossLinesService.CrossLinesStateChanged += OnCrossLinesStateChanged;
        }

        private void OnLineCompleted(LineItem lineItem)
        {
            // 處理線條完成事件
            _lineService.DisableDrawLineMode();
            
            // 通知 UI 更新畫線按鈕狀態
            OnPropertyChanged(nameof(IsDrawLineActive));
            
            // 將完成的線條加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(lineItem);
        }

        private void OnRulerCompleted(RulerItem rulerItem)
        {
            // 處理量尺完成事件
            _rulerService.DisableRulerMode();
            
            // 通知 UI 更新量尺按鈕狀態
            OnPropertyChanged(nameof(IsRulerActive));
            
            // 將完成的量尺加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(rulerItem);
        }

        private void OnRotRectRoiCompleted(RectRoiItem rectRoiItem)
        {
            // 處理旋轉矩形ROI完成事件
            _rotRectRoiService.DisableRotRectRoiMode();
            
            // 通知 UI 更新旋轉矩形按鈕狀態
            OnPropertyChanged(nameof(IsRotRectRoiActive));
            
            // 將完成的旋轉矩形ROI加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(rectRoiItem);
        }

        private void OnEllipseRoiCompleted(EllipseRoiItem ellipseRoiItem)
        {
            // 處理橢圓ROI完成事件
            _ellipseRoiService.DisableEllipseRoiMode();
            
            // 通知 UI 更新橢圓按鈕狀態
            OnPropertyChanged(nameof(IsEllipseRoiActive));
            
            // 將完成的橢圓ROI加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(ellipseRoiItem);
        }

        private void OnPolygonRoiCompleted(PolygonRoiItem polygonRoiItem)
        {
            // 處理多邊形ROI完成事件
            _polygonRoiService.DisablePolygonRoiMode();
            
            // 通知 UI 更新多邊形按鈕狀態
            OnPropertyChanged(nameof(IsPolygonRoiActive));
            
            // 將完成的多邊形ROI加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(polygonRoiItem);
        }

        private void OnPolygonRoiUpdated(PolygonRoiItem polygonRoiItem)
        {
            // 處理多邊形ROI更新事件 - 重繪Canvas
            if (_mainCanvas != null && _polygonRoiDrawingService != null)
            {
                _polygonRoiDrawingService.DrawRois(_mainCanvas, _polygonRoiService.PolygonRois.ToList(), polygonRoiItem, ShowLabels);
            }
        }

        private void OnBezierArcRoiCompleted(BezierArcRoiItem bezierArcRoiItem)
        {
            // 處理貝茲弧ROI完成事件
            _bezierArcRoiService.DisableBezierArcRoiMode();
            
            // 通知 UI 更新貝茲弧按鈕狀態
            OnPropertyChanged(nameof(IsBezierArcRoiActive));
            
            // 將完成的貝茲弧ROI加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(bezierArcRoiItem);

            // Log: 新增時
            var roiItem = _roiManagementService.RoiItems.FirstOrDefault(x => x.OriginalObject == bezierArcRoiItem);
            System.Diagnostics.Debug.WriteLine($"[ViewModel] Add: bezierArcRoiItem Hash={bezierArcRoiItem.GetHashCode()}, In RoiItems={(roiItem != null ? "YES" : "NO")}");
        }

        private void OnBezierArcRoiUpdated(BezierArcRoiItem bezierArcRoiItem)
        {
            // 處理貝茲弧ROI更新事件 - 重繪Canvas
            if (_mainCanvas != null && _bezierArcRoiDrawingService != null)
            {
                _bezierArcRoiDrawingService.DrawRois(_mainCanvas, _bezierArcRoiService.BezierArcRois.ToList(), bezierArcRoiItem, ShowLabels);
            }
        }

        private void OnCircularArcRoiCompleted(CircularArcRoiItem circularArcRoiItem)
        {
            // 處理圓弧ROI完成事件
            _circularArcRoiService.DisableCircularArcRoiMode();
            
            // 通知 UI 更新圓弧按鈕狀態
            OnPropertyChanged(nameof(IsCircularArcRoiActive));
            
            // 將完成的圓弧ROI加入到 ROI 管理服務中
            _roiManagementService.AddRoiItem(circularArcRoiItem);
        }

        private void OnCircularArcRoiUpdated(CircularArcRoiItem circularArcRoiItem)
        {
            // 處理圓弧ROI更新事件 - 重繪Canvas
            if (_mainCanvas != null && _circularArcRoiDrawingService != null)
            {
                _circularArcRoiDrawingService.DrawRois(_mainCanvas, _circularArcRoiService.CircularArcRois.ToList(), _circularArcRoiService.CurrentCircularArcRoi, ShowLabels);
            }
        }

        private void OnCrossLinesStateChanged(bool showCrossLines)
        {
            // 當十字線狀態改變時，通知UI更新
            OnPropertyChanged(nameof(ShowCrossLines));
        }

        private void OnPointCompleted(PointItem pointItem)
        {
            _pointRoiService.DisableDrawPointMode();
            OnPropertyChanged(nameof(IsDrawPointActive));
            _roiManagementService.AddRoiItem(pointItem);
        }

        #endregion

        #region Public Methods

        public void LoadImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var writable = new WriteableBitmap(bitmap);
                ImageSource = writable;
            }
            catch (Exception ex)
            {
                ImageSource = null;
                MessageBox.Show($"載入圖片失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadImage(BitmapSource bitmapSource)
        {
            if (bitmapSource == null)
                return;

            try
            {
                ImageSource = new WriteableBitmap(bitmapSource);
            }
            catch (Exception ex)
            {
                ImageSource = null;
                MessageBox.Show($"載入圖片失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadImage(System.IO.Stream imageStream)
        {
            if (imageStream == null)
                return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = imageStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var writable = new WriteableBitmap(bitmap);
                ImageSource = writable;
            }
            catch (Exception ex)
            {
                ImageSource = null;
                MessageBox.Show($"載入圖片失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SetCanvas(Canvas canvas)
        {
            // 只設置各服務的畫布引用
            _lineService.SetMainCanvas(canvas);
            _rulerService.SetMainCanvas(canvas);
            _rotRectRoiService.SetMainCanvas(canvas);
            _ellipseRoiService.SetMainCanvas(canvas);
            _polygonRoiService.SetMainCanvas(canvas);
            _bezierArcRoiService.SetMainCanvas(canvas);
            _circularArcRoiService.SetMainCanvas(canvas);
            _drawLineDrawingService.SetMainCanvas(canvas);
            _rulerDrawingService.SetMainCanvas(canvas);
            _rotRectRoiDrawingService.SetMainCanvas(canvas);
            _ellipseRoiDrawingService.SetMainCanvas(canvas);
            _polygonRoiDrawingService.SetMainCanvas(canvas);
            _bezierArcRoiDrawingService.SetMainCanvas(canvas);
            _circularArcRoiDrawingService.SetMainCanvas(canvas);
            _pointRoiDrawingService.SetMainCanvas(canvas);
            // 設置服務的繪製服務引用
            _lineService.SetDrawingService(_drawLineDrawingService);
            _rulerService.SetDrawingService(_rulerDrawingService);
            _rotRectRoiService.SetDrawingService(_rotRectRoiDrawingService);
            _ellipseRoiService.SetDrawingService(_ellipseRoiDrawingService);
            _polygonRoiService.SetDrawingService(_polygonRoiDrawingService);
            _bezierArcRoiService.SetDrawingService(_bezierArcRoiDrawingService);
            _circularArcRoiService.SetDrawingService(_circularArcRoiDrawingService);
            _pointRoiService.SetDrawingService(_pointRoiDrawingService);

            _roiManagementService.SetCanvas(canvas);
            _mainCanvas = canvas;
        }

        /// <summary>
        /// 設置 OverlayCanvas，用於繪製十字線等固定元素
        /// </summary>
        public void SetOverlayCanvas(Canvas overlayCanvas)
        {
            // ROI 服務應該使用 MainCanvas，這樣會跟著圖片縮放
            // 只有十字線使用 OverlayCanvas，保持固定位置
        }

        /// <summary>
        /// 設置 Transform 更新 Actions，讓 ViewModel 能夠間接控制 View 層的 Transform
        /// </summary>
        /// <param name="zoomAction">縮放 Action</param>
        /// <param name="panAction">平移 Action</param>
        /// <param name="zoomAndPanAction">縮放和平移 Action</param>
        /// <param name="getDisplaySizeAction">獲取顯示區域尺寸的 Action</param>
        public void SetTransformActions(Action<double> zoomAction, Action<Point> panAction, Action<double, Point> zoomAndPanAction = null, Func<Size> getDisplaySizeAction = null)
        {
            _zoomAction = zoomAction;
            _panAction = panAction;
            _zoomAndPanAction = zoomAndPanAction;
            _getDisplaySizeAction = getDisplaySizeAction;
        }

        /// <summary>
        /// 縮放到指定比例
        /// </summary>
        /// <param name="scale">縮放比例</param>
        public void ZoomTo(double scale)
        {
            _zoomAction?.Invoke(scale);
        }

        /// <summary>
        /// 平移到指定位置
        /// </summary>
        /// <param name="position">目標位置</param>
        public void PanTo(Point position)
        {
            _panAction?.Invoke(position);
        }

        /// <summary>
        /// 縮放並平移到指定位置
        /// </summary>
        /// <param name="scale">縮放比例</param>
        /// <param name="position">目標位置</param>
        public void ZoomAndPanTo(double scale, Point position)
        {
            if (_zoomAndPanAction != null)
            {
                _zoomAndPanAction.Invoke(scale, position);
            }
            else
            {
                // 如果沒有組合 Action，分別執行
                _zoomAction?.Invoke(scale);
                _panAction?.Invoke(position);
            }
        }

        /// <summary>
        /// 適應視窗 - 計算最佳縮放比例和位置
        /// </summary>
        public void FitImageToWindow()
        {
            if (ImageSource == null) return;

            // 重置縮放和平移，讓 Viewbox 自動處理置中
            ZoomAndPanTo(1.0, new Point(0, 0));
        }

        /// <summary>
        /// 處理滑鼠按下事件（使用自定義邊界）
        /// </summary>
        public void HandleMouseDownWithImageBounds(Point position, Canvas canvas, bool isShift, double minX, double minY, double maxX, double maxY)
        {
            // 暫存邊界資訊供後續拖拽使用
            System.Diagnostics.Debug.WriteLine($"[ViewModel] MouseDown with bounds: ({minX}, {minY}) to ({maxX}, {maxY})");
            HandleMouseDown(position, canvas, isShift);
        }

        /// <summary>
        /// 處理滑鼠按下事件
        /// </summary>
        public void HandleMouseDown(Point position, Canvas canvas, bool isShift = false)
        {
            RoiMouseDown?.Invoke();
            var hits = new List<(BaseItem roi, Action<Point, Canvas, bool> handle)>();
            var rulerHit = _rulerService.GetHitRoiItem(position);
            if (rulerHit != null) hits.Add((rulerHit, (p, c, s) => _rulerService.HandleMouseDown(p, c, s)));
            var lineHit = _lineService.GetHitRoiItem(position);
            if (lineHit != null) hits.Add((lineHit, (p, c, s) => _lineService.HandleMouseDown(p, c, s)));
            var rotRectHit = _rotRectRoiService.GetHitRoiItem(position);
            if (rotRectHit != null) hits.Add((rotRectHit, (p, c, s) => _rotRectRoiService.HandleMouseDown(p, c, s)));
            var ellipseHit = _ellipseRoiService.GetHitRoiItem(position);
            if (ellipseHit != null) hits.Add((ellipseHit, (p, c, s) => _ellipseRoiService.HandleMouseDown(p, c, s)));
            var polygonHit = _polygonRoiService.GetHitRoiItem(position);
            if (polygonHit != null) hits.Add((polygonHit, (p, c, s) => _polygonRoiService.HandleMouseDown(p, c, s)));
            var bezierHit = _bezierArcRoiService.GetHitRoiItem(position);
            if (bezierHit != null) hits.Add((bezierHit, (p, c, s) => _bezierArcRoiService.HandleMouseDown(p, c, s)));
            var circularHit = _circularArcRoiService.GetHitRoiItem(position);
            if (circularHit != null) hits.Add((circularHit, (p, c, s) => _circularArcRoiService.HandleMouseDown(p, c, s)));
            var pointHit = _pointRoiService.GetHitPointItem(position);
            if (pointHit != null) hits.Add((pointHit, (p, c, s) => _pointRoiService.HandleMouseDown(p, c, s)));
            if (hits.Any())
            {
                var top = hits.OrderByDescending(h => h.roi.ZIndex).First();
                
                // 總是調用ROI的handle方法來處理拖曳等操作
                top.handle(position, canvas, isShift);
                
                // 如果沒有啟用任何工具模式，同時設置選中狀態
                if (!IsAnyToolModeActive())
                {
                    SetSelectedRoiFromBaseItem(top.roi);
                }
            }
            else
            {
                // 若無命中，根據目前啟用的模式創建新 ROI
                if (IsDrawLineActive)
                    _lineService.HandleMouseDown(position, canvas, isShift);
                else if (IsRulerActive)
                    _rulerService.HandleMouseDown(position, canvas, isShift);
                else if (IsRotRectRoiActive)
                    _rotRectRoiService.HandleMouseDown(position, canvas, isShift);
                else if (IsEllipseRoiActive)
                    _ellipseRoiService.HandleMouseDown(position, canvas, isShift);
                else if (IsPolygonRoiActive)
                    _polygonRoiService.HandleMouseDown(position, canvas, isShift);
                else if (IsBezierArcRoiActive)
                    _bezierArcRoiService.HandleMouseDown(position, canvas, isShift);
                else if (IsCircularArcRoiActive)
                    _circularArcRoiService.HandleMouseDown(position, canvas, isShift);
                else if (IsDrawPointActive)
                    _pointRoiService.HandleMouseDown(position, canvas, isShift);
                else
                {
                    // 如果沒有工具模式啟用且點擊空白區域，清除選中狀態
                    SelectedRoiItem = null;
                }
            }
        }

        /// <summary>
        /// 處理滑鼠移動事件
        /// </summary>
        public void HandleMouseMove(Point position, Canvas canvas)
        {
            // 將事件傳遞給各個服務
            _lineService.HandleMouseMove(position, canvas);
            _rulerService.HandleMouseMove(position, canvas);
            _rotRectRoiService.HandleMouseMove(position, canvas);
            _ellipseRoiService.HandleMouseMove(position, canvas);
            _polygonRoiService.HandleMouseMove(position, canvas);
            _bezierArcRoiService.HandleMouseMove(position, canvas);
            _circularArcRoiService.HandleMouseMove(position, canvas);
            _pointRoiService.HandleMouseMove(position, canvas);
        }

        /// <summary>
        /// 處理滑鼠移動事件（使用自定義邊界）
        /// </summary>
        public void HandleMouseMoveWithImageBounds(Point position, Canvas canvas, double minX, double minY, double maxX, double maxY)
        {
            // 將事件傳遞給各個服務，對支援自定義邊界的服務使用自定義邊界
            _lineService.HandleMouseMove(position, canvas);
            _rulerService.HandleMouseMove(position, canvas);
            _rotRectRoiService.HandleMouseMove(position, canvas);
            _ellipseRoiService.HandleMouseMove(position, canvas, minX, minY, maxX, maxY);
            _polygonRoiService.HandleMouseMove(position, canvas);
            _bezierArcRoiService.HandleMouseMove(position, canvas);
            _circularArcRoiService.HandleMouseMove(position, canvas);
            _pointRoiService.HandleMouseMove(position, canvas);
        }

        /// <summary>
        /// 處理滑鼠放開事件
        /// </summary>
        public void HandleMouseUp(Point position, Canvas canvas)
        {
            RoiMouseUp?.Invoke();
            // 將事件傳遞給各個服務
            _lineService.HandleMouseUp(position, canvas);
            _rulerService.HandleMouseUp(position, canvas);
            _rotRectRoiService.HandleMouseUp(position, canvas);
            _ellipseRoiService.HandleMouseUp(position, canvas);
            _polygonRoiService.HandleMouseUp(position, canvas);
            _bezierArcRoiService.HandleMouseUp(position, canvas);
            _circularArcRoiService.HandleMouseUp(position, canvas);
            _pointRoiService.HandleMouseUp(position, canvas);
        }

        /// <summary>
        /// 處理滑鼠右鍵按下事件
        /// </summary>
        public void HandleRightMouseDown(Point position, Canvas canvas)
        {
            // 處理各ROI服務的右鍵事件
            _lineService.HandleRightMouseDown(canvas);
            _rulerService.HandleRightMouseDown(canvas);
            _rotRectRoiService.HandleRightMouseDown(canvas);
            _ellipseRoiService.HandleRightMouseDown(canvas);
            _polygonRoiService.HandleRightMouseDown(position, canvas);
            _bezierArcRoiService.HandleRightMouseDown(position, canvas);
            _circularArcRoiService.HandleRightMouseDown(position, canvas);
        }

        public void HandleKeyDown(Key key)
        {
            try
            {
                switch (key)
                {
                    case Key.F3:
                        // F3鍵切換Canvas Label顯示
                        ToggleLabels();
                        break;
                    case Key.Escape:
                        // ESC鍵取消當前操作
                        HandleRightMouseDown(new Point(), _mainCanvas);
                        break;
                    case Key.Delete:
                        // Delete鍵刪除選中的ROI
                        if (CanDeleteSelectedRoi())
                        {
                            DeleteSelectedRoi();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ToggleLabels()
        {
            try
            {
                // 切換ShowLabels狀態
                ShowLabels = !ShowLabels;
                
                // 重新繪製所有ROI以更新標籤顯示狀態
                if (_mainCanvas != null)
                {
                    // 重新繪製量尺
                    _rulerDrawingService.DrawRois(_mainCanvas, _rulerService.RulerItems.ToList(), null, ShowLabels);

                    // 重新繪製畫線
                    _drawLineDrawingService.DrawRois(_mainCanvas, _lineService.DrawLines.ToList(), _lineService.CurrentDrawLine, ShowLabels);

                    // 重新繪製旋轉矩形ROI
                    _rotRectRoiDrawingService.DrawRois(_mainCanvas, _rotRectRoiService.RotRectRois.ToList(), _rotRectRoiService.CurrentRotRectRoi, ShowLabels);

                    // 重新繪製橢圓ROI 
                    _ellipseRoiDrawingService.DrawRois(_mainCanvas, _ellipseRoiService.EllipseRois.ToList(), _ellipseRoiService.CurrentEllipseRoi, ShowLabels);
                    
                    // 重新繪製多邊形ROI
                    _polygonRoiDrawingService.DrawRois(_mainCanvas, _polygonRoiService.PolygonRois.ToList(), _polygonRoiService.CurrentPolygonRoi, ShowLabels);
                    
                    // 重新繪製貝塞爾弧線ROI
                    _bezierArcRoiDrawingService.DrawRois(_mainCanvas, _bezierArcRoiService.BezierArcRois.ToList(), _bezierArcRoiService.CurrentBezierArcRoi, ShowLabels);
                    
                    // 重新繪製圓弧ROI
                    _circularArcRoiDrawingService.DrawRois(_mainCanvas, _circularArcRoiService.CircularArcRois.ToList(), _circularArcRoiService.CurrentCircularArcRoi, ShowLabels);

                    // 重新繪製點ROI
                    _pointRoiDrawingService.DrawRois(_mainCanvas, _pointRoiService.Points.ToList(), _pointRoiService.CurrentPoint, ShowLabels);
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 刪除選中的ROI
        /// </summary>
        private void DeleteSelectedRoi()
        {
            if (SelectedRoiItem?.OriginalObject == null) return;

            try
            {
                var originalObject = SelectedRoiItem.OriginalObject;

                // 根據原始物件類型從對應的服務中刪除
                if (originalObject is LineItem lineItem)
                {
                    _lineService.RemoveRoi(lineItem, _mainCanvas);
                }
                else if (originalObject is RulerItem rulerItem)
                {
                    _rulerService.RemoveRoi(rulerItem, _mainCanvas);
                }
                else if (originalObject is RectRoiItem rectRoiItem)
                {
                    _rotRectRoiService.RemoveRoi(rectRoiItem, _mainCanvas);
                }
                else if (originalObject is EllipseRoiItem ellipseRoiItem)
                {
                    _ellipseRoiService.RemoveRoi(ellipseRoiItem, _mainCanvas);
                }
                else if (originalObject is PolygonRoiItem polygonRoiItem)
                {
                    _polygonRoiService.RemoveRoi(polygonRoiItem, _mainCanvas);
                }
                else if (originalObject is BezierArcRoiItem bezierArcRoiItem)
                {
                    _bezierArcRoiService.RemoveRoi(bezierArcRoiItem, _mainCanvas);
                }
                else if (originalObject is CircularArcRoiItem circularArcRoiItem)
                {
                    _circularArcRoiService.RemoveRoi(circularArcRoiItem, _mainCanvas);
                }
                else if (originalObject is PointItem pointItem)
                {
                    _pointRoiService.RemoveRoi(pointItem, _mainCanvas);
                }

                // 從ROI管理服務中移除
                _roiManagementService.RemoveRoiItem(SelectedRoiItem);
                
                // 清除選中狀態
                SelectedRoiItem = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刪除ROI時發生錯誤：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 檢查是否可以刪除選中的ROI
        /// </summary>
        private bool CanDeleteSelectedRoi()
        {
            return SelectedRoiItem?.OriginalObject != null;
        }

        /// <summary>
        /// 檢查是否有任何工具模式啟用
        /// </summary>
        private bool IsAnyToolModeActive()
        {
            return IsDrawPointActive || IsDrawLineActive || IsRulerActive || 
                   IsRotRectRoiActive || IsEllipseRoiActive || IsPolygonRoiActive || 
                   IsBezierArcRoiActive || IsCircularArcRoiActive;
        }

        /// <summary>
        /// 根據BaseItem設置選中的ROI
        /// </summary>
        private void SetSelectedRoiFromBaseItem(BaseItem baseItem)
        {
            if (baseItem == null) return;

            // 在ROI清單中找到對應的RoiItem
            var roiItem = RoiItems.FirstOrDefault(r => r.OriginalObject == baseItem);
            if (roiItem != null)
            {
                SelectedRoiItem = roiItem;
            }
        }

        public void RefreshAllRoiItems()
        {
            AllRoiItems.Clear();
            // RotRect
            foreach (var item in _rotRectRoiService.RotRectRois)
                AllRoiItems.Add(item);
            // Polygon
            foreach (var item in _polygonRoiService.PolygonRois)
                AllRoiItems.Add(item);
            // Ellipse
            foreach (var item in _ellipseRoiService.EllipseRois)
                AllRoiItems.Add(item);
            // BezierArc
            foreach (var item in _bezierArcRoiService.BezierArcRois)
                AllRoiItems.Add(item);
            // CircularArc
            foreach (var item in _circularArcRoiService.CircularArcRois)
                AllRoiItems.Add(item);
            // Ruler
            foreach (var item in _rulerService.RulerItems)
                AllRoiItems.Add(item);
            // DrawLine
            foreach (var item in _lineService.DrawLines)
                AllRoiItems.Add(item);
            // Point
            foreach (var item in _pointRoiService.Points)
                AllRoiItems.Add(item);
        }

        // 新增：ROI 框選滑鼠事件
        public event Action RoiMouseDown;
        public event Action RoiMouseUp;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 