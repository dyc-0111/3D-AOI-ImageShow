using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HyImageShow.ImageShowWPF.Models;
using HyImageShow.ImageShowWPF.Services;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// ROI 管理服務介面
    /// </summary>
    public interface IRoiManagementService
    {
        /// <summary>
        /// ROI 項目集合
        /// </summary>
        ObservableCollection<RoiItem> RoiItems { get; }

        /// <summary>
        /// 選中的 ROI 項目
        /// </summary>
        RoiItem SelectedRoiItem { get; set; }

        /// <summary>
        /// 添加 ROI 項目到清單
        /// </summary>
        /// <param name="originalObject">原始 ROI 物件</param>
        void AddRoiItem(object originalObject);

        /// <summary>
        /// 移除 ROI 項目
        /// </summary>
        /// <param name="roiItem">要移除的 ROI 項目</param>
        void RemoveRoiItem(RoiItem roiItem);

        /// <summary>
        /// 清除所有 ROI 項目
        /// </summary>
        void ClearRoiItems();

        /// <summary>
        /// 更新 ROI 清單視圖
        /// </summary>
        void UpdateRoiListView();

        /// <summary>
        /// 開始 ROI 高亮動畫
        /// </summary>
        /// <param name="roiItem">要高亮的 ROI 項目</param>
        void StartRoiHighlightAnimation(RoiItem roiItem);

        /// <summary>
        /// 停止 ROI 高亮動畫
        /// </summary>
        /// <param name="roiItem">要停止高亮的 ROI 項目</param>
        void StopRoiHighlightAnimation(RoiItem roiItem);

        /// <summary>
        /// 停止所有 ROI 高亮動畫
        /// </summary>
        void StopAllRoiHighlightAnimations();

        /// <summary>
        /// 設置選中的 ROI 項目
        /// </summary>
        /// <param name="roiItem">要選中的 ROI 項目</param>
        void SetSelectedRoiItem(RoiItem roiItem);

        /// <summary>
        /// 清除選中的 ROI 項目
        /// </summary>
        void ClearSelectedRoiItem();

        /// <summary>
        /// ROI 項目添加事件
        /// </summary>
        event Action<RoiItem> RoiItemAdded;

        /// <summary>
        /// ROI 項目移除事件
        /// </summary>
        event Action<RoiItem> RoiItemRemoved;

        /// <summary>
        /// ROI 項目清除事件
        /// </summary>
        event Action RoiItemsCleared;

        /// <summary>
        /// ROI 項目選擇變更事件
        /// </summary>
        event Action<RoiItem> RoiItemSelectionChanged;
    }
} 