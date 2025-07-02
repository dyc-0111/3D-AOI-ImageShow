using System.Collections.Generic;
using System.Linq;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 全域 Z-Index 管理器，確保所有 ROI 的 Z-Index 唯一且自動遞增
    /// </summary>
    public class ZIndexManager
    {
        private static ZIndexManager _instance;
        public static ZIndexManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ZIndexManager();
                return _instance;
            }
        }

        private int _currentMaxZIndex = 0;
        private readonly List<BaseItem> _allItems = new List<BaseItem>();

        public void Register(BaseItem item)
        {
            if (!_allItems.Contains(item))
            {
                item.ZIndex = ++_currentMaxZIndex;
                _allItems.Add(item);
            }
        }

        public void Unregister(BaseItem item)
        {
            _allItems.Remove(item);
        }

        public void BringToFront(BaseItem item)
        {
            item.ZIndex = ++_currentMaxZIndex;
        }

        public IEnumerable<BaseItem> GetAllItemsByZIndex()
        {
            return _allItems.OrderBy(i => i.ZIndex);
        }
    }
} 