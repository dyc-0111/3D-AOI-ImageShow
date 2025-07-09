using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HyImageShow.ImageShowWPF.Models;
using HyImageShow.ImageShowWPF.ViewModels;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 圖像顯示 API 實現類別，提供程式化操作圖像和工具的功能
    /// </summary>
    public class ImageShowAPI : IImageShowAPI
    {
        private readonly ImageShowViewModel _viewModel;

        public ImageShowAPI(ImageShowViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            SubscribeToViewModelEvents();
        }

        /// <summary>
        /// 建立預設服務的 API 實例
        /// </summary>
        /// <returns>API 實例</returns>
        public static ImageShowAPI CreateDefault()
        {
            var viewModel = ImageShowViewModel.CreateDefaultServices();
            return new ImageShowAPI(viewModel);
        }

        #region 屬性實現
        public ImageSource ImageSource => _viewModel.ImageSource;
        public ObservableCollection<RoiItem> RoiItems => _viewModel.RoiItems;
        public RoiItem SelectedRoiItem
        {
            get => _viewModel.SelectedRoiItem;
            set => _viewModel.SelectedRoiItem = value;
        }
        public bool ShowCrossLines
        {
            get => _viewModel.ShowCrossLines;
            set => _viewModel.ShowCrossLines = value;
        }
        public bool ShowLabels
        {
            get => _viewModel.ShowLabels;
            set => _viewModel.ShowLabels = value;
        }
        #endregion

        #region 檔案操作實現
        public void LoadImage(string filePath)
        {
            _viewModel.LoadImage(filePath);
        }

        public void LoadImage(BitmapSource bitmapSource)
        {
            _viewModel.LoadImage(bitmapSource);
        }

        public void LoadImage(System.IO.Stream imageStream)
        {
            _viewModel.LoadImage(imageStream);
        }

        public void SaveFile()
        {
            if (_viewModel.SaveFileCommand.CanExecute(null))
            {
                _viewModel.SaveFileCommand.Execute(null);
            }
        }

        public void NewFile()
        {
            if (_viewModel.NewFileCommand.CanExecute(null))
            {
                _viewModel.NewFileCommand.Execute(null);
            }
        }
        #endregion

        #region 檢視操作實現
        public void FitImage()
        {
            if (_viewModel.FitImageCommand.CanExecute(null))
            {
                _viewModel.FitImageCommand.Execute(null);
            }
        }

        public void ToggleCrossLines()
        {
            if (_viewModel.ToggleCrossLinesCommand.CanExecute(null))
            {
                _viewModel.ToggleCrossLinesCommand.Execute(null);
            }
        }

        public void ClearAll()
        {
            if (_viewModel.ClearAllCommand.CanExecute(null))
            {
                _viewModel.ClearAllCommand.Execute(null);
            }
        }

        public void ZoomTo(double scale)
        {
            _viewModel.ZoomTo(scale);
        }

        public void PanTo(Point position)
        {
            _viewModel.PanTo(position);
        }

        public void ZoomAndPanTo(double scale, Point position)
        {
            _viewModel.ZoomAndPanTo(scale, position);
        }
        #endregion

        #region ROI 工具模式實現
        public void EnableDrawPointMode()
        {
            if (_viewModel.DrawPointCommand.CanExecute(null))
            {
                _viewModel.DrawPointCommand.Execute(null);
            }
        }

        public void EnableDrawLineMode()
        {
            if (_viewModel.DrawLineCommand.CanExecute(null))
            {
                _viewModel.DrawLineCommand.Execute(null);
            }
        }

        public void EnableRulerMode()
        {
            if (_viewModel.RulerCommand.CanExecute(null))
            {
                _viewModel.RulerCommand.Execute(null);
            }
        }

        public void EnableRotRectRoiMode()
        {
            if (_viewModel.RotRectRoiCommand.CanExecute(null))
            {
                _viewModel.RotRectRoiCommand.Execute(null);
            }
        }

        public void EnableEllipseRoiMode()
        {
            if (_viewModel.EllipseRoiCommand.CanExecute(null))
            {
                _viewModel.EllipseRoiCommand.Execute(null);
            }
        }

        public void EnablePolygonRoiMode()
        {
            if (_viewModel.PolygonRoiCommand.CanExecute(null))
            {
                _viewModel.PolygonRoiCommand.Execute(null);
            }
        }

        public void EnableBezierArcRoiMode()
        {
            if (_viewModel.BezierArcRoiCommand.CanExecute(null))
            {
                _viewModel.BezierArcRoiCommand.Execute(null);
            }
        }

        public void EnableCircularArcRoiMode()
        {
            if (_viewModel.CircularArcRoiCommand.CanExecute(null))
            {
                _viewModel.CircularArcRoiCommand.Execute(null);
            }
        }

        public void DisableAllModes()
        {
            // 透過 ViewModel 的內部方法停用所有模式
            // 需要存取 ViewModel 的 DisableAllModes 方法
            _viewModel.GetType().GetMethod("DisableAllModes", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_viewModel, null);
        }
        #endregion

        #region ROI 操作實現
        public void DeleteSelectedRoi()
        {
            if (_viewModel.DeleteSelectedRoiCommand.CanExecute(null))
            {
                _viewModel.DeleteSelectedRoiCommand.Execute(null);
            }
        }

        public void DeleteRoi(RoiItem roiItem)
        {
            if (roiItem != null && _viewModel.RoiItems.Contains(roiItem))
            {
                _viewModel.SelectedRoiItem = roiItem;
                DeleteSelectedRoi();
            }
        }

        public void AddRoi(RoiItem roiItem)
        {
            if (roiItem != null && !_viewModel.RoiItems.Contains(roiItem))
            {
                _viewModel.RoiItems.Add(roiItem);
            }
        }
        #endregion

        #region 滑鼠事件處理實現
        public void HandleMouseDown(Point position, Canvas canvas, bool isShift = false)
        {
            _viewModel.HandleMouseDown(position, canvas, isShift);
        }

        public void HandleMouseMove(Point position, Canvas canvas)
        {
            _viewModel.HandleMouseMove(position, canvas);
        }

        public void HandleMouseUp(Point position, Canvas canvas)
        {
            _viewModel.HandleMouseUp(position, canvas);
        }

        public void HandleRightMouseDown(Point position, Canvas canvas)
        {
            _viewModel.HandleRightMouseDown(position, canvas);
        }
        #endregion

        #region 事件實現
        public event Action<RoiItem> RoiCompleted;
        public event Action<RoiItem> RoiUpdated;
        public event Action<RoiItem> RoiDeleted;

        private void SubscribeToViewModelEvents()
        {
            // 暫時保留事件定義，可以在需要時實現
        }
        #endregion

        #region 初始化設定實現
        public void SetCanvas(Canvas canvas)
        {
            _viewModel.SetCanvas(canvas);
        }

        public void SetTransformActions(Action<double> zoomAction, Action<Point> panAction, Action<double, Point> zoomAndPanAction = null, Func<Size> getDisplaySizeAction = null)
        {
            _viewModel.SetTransformActions(zoomAction, panAction, zoomAndPanAction, getDisplaySizeAction);
        }
        #endregion

        #region 額外的便利方法
        /// <summary>
        /// 取得底層的 ViewModel
        /// </summary>
        /// <returns>ImageShowViewModel 實例</returns>
        public ImageShowViewModel GetViewModel()
        {
            return _viewModel;
        }

        /// <summary>
        /// 檢查是否有任何工具模式處於啟用狀態
        /// </summary>
        /// <returns>是否有工具模式啟用</returns>
        public bool IsAnyToolModeActive()
        {
            return _viewModel.IsDrawPointActive ||
                   _viewModel.IsDrawLineActive ||
                   _viewModel.IsRulerActive ||
                   _viewModel.IsRotRectRoiActive ||
                   _viewModel.IsEllipseRoiActive ||
                   _viewModel.IsPolygonRoiActive ||
                   _viewModel.IsBezierArcRoiActive ||
                   _viewModel.IsCircularArcRoiActive;
        }

        /// <summary>
        /// 取得目前啟用的工具模式名稱
        /// </summary>
        /// <returns>工具模式名稱，如果沒有啟用則返回空字串</returns>
        public string GetActiveToolMode()
        {
            if (_viewModel.IsDrawPointActive) return "DrawPoint";
            if (_viewModel.IsDrawLineActive) return "DrawLine";
            if (_viewModel.IsRulerActive) return "Ruler";
            if (_viewModel.IsRotRectRoiActive) return "RotRect";
            if (_viewModel.IsEllipseRoiActive) return "Ellipse";
            if (_viewModel.IsPolygonRoiActive) return "Polygon";
            if (_viewModel.IsBezierArcRoiActive) return "BezierArc";
            if (_viewModel.IsCircularArcRoiActive) return "CircularArc";
            return string.Empty;
        }
        #endregion
    }
} 