using System;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 十字線服務實作
    /// </summary>
    public class CrossLinesService
    {
        private bool _showCrossLines;

        public CrossLinesService()
        {
            _showCrossLines = false;
        }

        /// <summary>
        /// 是否顯示十字線
        /// </summary>
        public bool ShowCrossLines
        {
            get => _showCrossLines;
            set
            {
                if (_showCrossLines != value)
                {
                    _showCrossLines = value;
                    CrossLinesStateChanged?.Invoke(_showCrossLines);
                }
            }
        }

        /// <summary>
        /// 十字線狀態變更事件
        /// </summary>
        public event Action<bool> CrossLinesStateChanged;

        /// <summary>
        /// 切換十字線顯示狀態
        /// </summary>
        public void ToggleCrossLines()
        {
            ShowCrossLines = !ShowCrossLines;
        }

        /// <summary>
        /// 啟用十字線
        /// </summary>
        public void EnableCrossLines()
        {
            ShowCrossLines = true;
        }

        /// <summary>
        /// 停用十字線
        /// </summary>
        public void DisableCrossLines()
        {
            ShowCrossLines = false;
        }
    }
} 