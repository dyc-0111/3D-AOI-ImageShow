using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 圖像顯示 API 介面，提供程式化操作圖像和工具的功能
    /// </summary>
    public interface IImageShowAPI
    {
        #region 屬性
        /// <summary>
        /// 目前顯示的圖像
        /// </summary>
        ImageSource ImageSource { get; }
        
        /// <summary>
        /// 所有 ROI 項目集合
        /// </summary>
        ObservableCollection<RoiItem> RoiItems { get; }
        
        /// <summary>
        /// 目前選中的 ROI 項目
        /// </summary>
        RoiItem SelectedRoiItem { get; set; }
        
        /// <summary>
        /// 是否顯示十字線
        /// </summary>
        bool ShowCrossLines { get; set; }
        
        /// <summary>
        /// 是否顯示標籤
        /// </summary>
        bool ShowLabels { get; set; }
        #endregion

        #region 檔案操作
        /// <summary>
        /// 載入圖像檔案
        /// </summary>
        /// <param name="filePath">圖像檔案路徑</param>
        void LoadImage(string filePath);
        
        /// <summary>
        /// 載入圖像 BitmapSource
        /// </summary>
        /// <param name="bitmapSource">圖像 BitmapSource</param>
        void LoadImage(BitmapSource bitmapSource);
        
        /// <summary>
        /// 載入圖像串流
        /// </summary>
        /// <param name="imageStream">圖像串流</param>
        void LoadImage(System.IO.Stream imageStream);
        
        /// <summary>
        /// 儲存檔案
        /// </summary>
        void SaveFile();
        
        /// <summary>
        /// 新建檔案
        /// </summary>
        void NewFile();
        #endregion

        #region 檢視操作
        /// <summary>
        /// 適應視窗大小
        /// </summary>
        void FitImage();
        
        /// <summary>
        /// 切換十字線顯示
        /// </summary>
        void ToggleCrossLines();
        
        /// <summary>
        /// 清除所有 ROI
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// 縮放到指定比例
        /// </summary>
        /// <param name="scale">縮放比例</param>
        void ZoomTo(double scale);
        
        /// <summary>
        /// 平移到指定位置
        /// </summary>
        /// <param name="position">位置</param>
        void PanTo(Point position);
        
        /// <summary>
        /// 同時縮放和平移
        /// </summary>
        /// <param name="scale">縮放比例</param>
        /// <param name="position">位置</param>
        void ZoomAndPanTo(double scale, Point position);
        #endregion

        #region ROI 工具模式
        /// <summary>
        /// 啟用標點模式
        /// </summary>
        void EnableDrawPointMode();
        
        /// <summary>
        /// 啟用畫線模式
        /// </summary>
        void EnableDrawLineMode();
        
        /// <summary>
        /// 啟用量尺模式
        /// </summary>
        void EnableRulerMode();
        
        /// <summary>
        /// 啟用旋轉矩形模式
        /// </summary>
        void EnableRotRectRoiMode();
        
        /// <summary>
        /// 啟用橢圓模式
        /// </summary>
        void EnableEllipseRoiMode();
        
        /// <summary>
        /// 啟用多邊形模式
        /// </summary>
        void EnablePolygonRoiMode();
        
        /// <summary>
        /// 啟用貝茲弧模式
        /// </summary>
        void EnableBezierArcRoiMode();
        
        /// <summary>
        /// 啟用圓弧模式
        /// </summary>
        void EnableCircularArcRoiMode();
        
        /// <summary>
        /// 停用所有工具模式
        /// </summary>
        void DisableAllModes();
        #endregion

        #region ROI 操作
        /// <summary>
        /// 刪除選中的 ROI
        /// </summary>
        void DeleteSelectedRoi();
        
        /// <summary>
        /// 刪除指定的 ROI
        /// </summary>
        /// <param name="roiItem">要刪除的 ROI 項目</param>
        void DeleteRoi(RoiItem roiItem);
        
        /// <summary>
        /// 加入 ROI 項目
        /// </summary>
        /// <param name="roiItem">ROI 項目</param>
        void AddRoi(RoiItem roiItem);
        #endregion

        #region 滑鼠事件處理
        /// <summary>
        /// 處理滑鼠按下事件
        /// </summary>
        /// <param name="position">滑鼠位置</param>
        /// <param name="canvas">畫布</param>
        /// <param name="isShift">是否按下 Shift 鍵</param>
        void HandleMouseDown(Point position, Canvas canvas, bool isShift = false);
        
        /// <summary>
        /// 處理滑鼠移動事件
        /// </summary>
        /// <param name="position">滑鼠位置</param>
        /// <param name="canvas">畫布</param>
        void HandleMouseMove(Point position, Canvas canvas);
        
        /// <summary>
        /// 處理滑鼠釋放事件
        /// </summary>
        /// <param name="position">滑鼠位置</param>
        /// <param name="canvas">畫布</param>
        void HandleMouseUp(Point position, Canvas canvas);
        
        /// <summary>
        /// 處理滑鼠右鍵按下事件
        /// </summary>
        /// <param name="position">滑鼠位置</param>
        /// <param name="canvas">畫布</param>
        void HandleRightMouseDown(Point position, Canvas canvas);
        #endregion

        #region 事件
        /// <summary>
        /// ROI 項目完成事件
        /// </summary>
        event Action<RoiItem> RoiCompleted;
        
        /// <summary>
        /// ROI 項目更新事件
        /// </summary>
        event Action<RoiItem> RoiUpdated;
        
        /// <summary>
        /// ROI 項目刪除事件
        /// </summary>
        event Action<RoiItem> RoiDeleted;
        #endregion

        #region 初始化設定
        /// <summary>
        /// 設定畫布
        /// </summary>
        /// <param name="canvas">畫布</param>
        void SetCanvas(Canvas canvas);
        
        /// <summary>
        /// 設定轉換動作
        /// </summary>
        /// <param name="zoomAction">縮放動作</param>
        /// <param name="panAction">平移動作</param>
        /// <param name="zoomAndPanAction">縮放和平移動作</param>
        /// <param name="getDisplaySizeAction">取得顯示大小動作</param>
        void SetTransformActions(Action<double> zoomAction, Action<Point> panAction, Action<double, Point> zoomAndPanAction = null, Func<Size> getDisplaySizeAction = null);
        #endregion
    }
} 