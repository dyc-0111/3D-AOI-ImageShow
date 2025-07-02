# Models 資料夾重構指南

## 概述

經過分析，發現 Models 資料夾中的所有 Item 類別都有許多共同特性，可以提取出一個基礎類別 `BaseItem` 來減少程式碼重複並提高維護性。

## 重構完成狀態

✅ **已完成重構的類別：**
- ✅ `LineItem` (DrawLineItem.cs)
- ✅ `EllipseRoiItem`
- ✅ `RectRoiItem`
- ✅ `PolygonRoiItem`
- ✅ `RulerItem`
- ✅ `BezierArcRoiItem`
- ✅ `CircularArcRoiItem`

## 命名衝突修復

在重構過程中發現並修復了以下命名衝突：

### 1. Center 屬性衝突
- **BezierArcRoiItem**: `Center` → `CenterPoint`
- **EllipseRoiItem**: `Center` → `CenterPoint`
- **PolygonRoiItem**: `Center` → `CenterPoint`
- **RectRoiItem**: `Center` → `CenterPoint`

### 2. Size 屬性衝突
- **PolygonRoiItem**: `Size` → `SizeValue`

### 3. Angle 屬性衝突
- **PolygonRoiItem**: `Angle` → `angle` (私有變數)
- **RectRoiItem**: `Angle` → `RotationAngle`
- **RulerItem**: `Angle` → `RulerAngle`

### 修復策略
- 將原本的屬性重新命名為更具描述性的名稱
- 保留 BaseItem 的抽象成員名稱不變
- 在抽象成員實作中引用重新命名的屬性

## 共同特性分析

### 所有 Item 類別都有的屬性：
1. **Id** - 唯一識別碼
2. **IsCompleted** - 是否完成
3. **LastDragPos** - 最後拖曳位置
4. **IsDetailsVisible** - 詳細資訊是否可見（在 RoiItem 中）

### 所有 Item 類別都有的方法：
1. **OnPropertyChanged** - 屬性變更事件處理
2. **ClearUIElements** - 清除UI元素
3. **ResetDragState** - 重置拖曳狀態

### 所有 Item 類別都實作：
1. **INotifyPropertyChanged** 介面

## 基礎類別設計

已創建 `BaseItem` 抽象類別，包含：

### 共同屬性
- `Id` - 唯一識別碼
- `IsCompleted` - 是否完成
- `LastDragPos` - 最後拖曳位置
- `IsDetailsVisible` - 詳細資訊是否可見

### 抽象成員（子類別必須實作）
- `ItemType` - 項目類型
- `Center` - 中心點
- `Size` - 大小
- `Angle` - 角度
- `DisplayText` - 顯示文字
- `SimpleDisplayText` - 簡化顯示文字
- `ClearUIElements()` - 清除UI元素
- `ResetDragState()` - 重置拖曳狀態
- `CalculateProperties()` - 計算屬性

### 實用方法
- `OnPropertyChanged()` - 觸發屬性變更事件
- `OnPropertiesChanged()` - 觸發多個屬性變更事件
- `CalculateDistance()` - 計算兩點距離
- `CalculateAngle()` - 計算兩點角度
- `CalculateMidPoint()` - 計算中點
- `FormatPointText()` - 格式化點座標文字
- `FormatValueText()` - 格式化數值文字

## 重構步驟

### 1. 修改類別宣告
```csharp
// 原本
public class LineItem : INotifyPropertyChanged

// 重構後
public class LineItem : BaseItem
```

### 2. 移除重複屬性
移除以下屬性（已在 BaseItem 中定義）：
- `id`
- `isCompleted`
- `lastDragPos`
- `isDetailsVisible`

### 3. 移除重複方法
移除以下方法（已在 BaseItem 中定義）：
- `OnPropertyChanged()`
- `PropertyChanged` 事件

### 4. 處理命名衝突
如果原本的類別已有與 BaseItem 抽象成員同名的屬性，需要重新命名：
```csharp
// 原本
public Point Center { get; set; }

// 重構後
public Point CenterPoint { get; set; }
public override Point Center => CenterPoint;
```

### 5. 實作抽象成員
為每個抽象成員提供實作：

```csharp
public override string ItemType => "線條";
public override Point Center => MidPoint;
public override Size Size => new Size(Math.Abs(P2.X - P1.X), Math.Abs(P2.Y - P1.Y));
public override double Angle => CalculateAngle(P1, P2);
public override string DisplayText => $"起點: {FormatPointText(P1)} 終點: {FormatPointText(P2)}";
public override string SimpleDisplayText => $"{FormatPointText(P1)} → {FormatPointText(P2)}";
public override void ClearUIElements() { /* 實作 */ }
public override void ResetDragState() { /* 實作 */ }
public override void CalculateProperties() { /* 實作 */ }
```

### 6. 使用基礎類別方法
將原本的計算邏輯替換為基礎類別的方法：
```csharp
// 原本
public Point MidPoint => new Point((P1.X + P2.X) / 2, (P1.Y + P2.Y) / 2);
public double Length => (P2 - P1).Length;

// 重構後
public Point MidPoint => CalculateMidPoint(P1, P2);
public double Length => CalculateDistance(P1, P2);
```

## 各類別重構重點

### LineItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 實作所有抽象成員
- 使用基礎類別的計算方法

### EllipseRoiItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 修復命名衝突：`Center` → `CenterPoint`
- 實作：Center, Size, Angle, DisplayText, SimpleDisplayText
- 使用基礎類別的計算方法
- 橢圓角度固定為 0

### RectRoiItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 修復命名衝突：`Center` → `CenterPoint`, `Angle` → `RotationAngle`
- 實作：Center, Size, Angle, DisplayText, SimpleDisplayText
- 保留旋轉相關的特殊屬性
- 使用基礎類別的格式化方法

### PolygonRoiItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 修復命名衝突：`Center` → `CenterPoint`, `Size` → `SizeValue`
- 實作：Center, Size, Angle, DisplayText, SimpleDisplayText
- 保留多邊形特有的頂點管理
- 使用基礎類別的計算方法

### RulerItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 修復命名衝突：`Angle` → `RulerAngle`
- 實作：Center, Size, Angle, DisplayText, SimpleDisplayText
- 保留測量相關的特殊屬性
- 使用基礎類別的計算和格式化方法

### BezierArcRoiItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 修復命名衝突：`Center` → `CenterPoint`
- 實作：Center, Size, Angle, DisplayText, SimpleDisplayText
- 保留貝塞爾曲線特有的控制點
- 使用基礎類別的計算方法

### CircularArcRoiItem（已完成）
- 繼承 BaseItem
- 移除重複屬性：id, isCompleted, lastDragPos
- 實作：Center, Size, Angle, DisplayText, SimpleDisplayText
- 保留圓弧特有的角度計算
- 使用基礎類別的計算和格式化方法

## 重構效益

1. **減少程式碼重複** - 共同功能只需維護一份
2. **提高一致性** - 所有 Item 類別都有相同的介面
3. **易於維護** - 修改共同功能只需修改 BaseItem
4. **提高可讀性** - 子類別專注於特有功能
5. **便於擴展** - 新增 Item 類型時有明確的實作指南
6. **統一格式化** - 所有文字格式化都使用基礎類別的方法
7. **統一計算** - 所有幾何計算都使用基礎類別的方法

## 重構統計

### 程式碼減少
- **移除重複屬性**：每個類別約減少 20-30 行程式碼
- **移除重複方法**：每個類別約減少 10-15 行程式碼
- **統一計算方法**：使用基礎類別的計算方法，提高一致性

### 功能增強
- **統一介面**：所有 Item 類別都有相同的抽象成員
- **統一格式化**：所有文字顯示都使用相同的格式化方法
- **統一計算**：所有幾何計算都使用相同的計算方法

## 注意事項

1. **向後相容性** - 重構後不應影響現有功能
2. **測試** - 重構後需要充分測試
3. **文檔更新** - 更新相關的 API 文檔
4. **命名衝突處理** - 注意原本屬性與 BaseItem 抽象成員的命名衝突

## 下一步

1. ✅ 所有 Item 類別重構完成
2. ✅ 命名衝突修復完成
3. 更新相關的 ViewModel 和 Service 類別（如有需要）
4. 進行充分測試
5. 更新專案文檔
6. 考慮將 RoiItem 也重構為繼承 BaseItem（如果適用）

## 重構完成總結

所有 Models 資料夾中的 Item 類別已成功重構為繼承 `BaseItem`，並修復了所有命名衝突，實現了：

- **程式碼重複大幅減少**
- **介面統一性提高**
- **維護性顯著改善**
- **擴展性增強**
- **程式碼品質提升**
- **命名衝突完全解決**

這次重構為專案建立了良好的基礎架構，為未來的功能擴展和維護奠定了堅實的基礎。

## 重構目標
將多個 ROI 項目類別重構為繼承一個基礎類別，以提取共同特性並減少重複程式碼。

## 重構架構

### 1. BaseItem 抽象基礎類別
- **位置**: `HyImageShow/ImageShowWPF/Models/BaseItem.cs`
- **目的**: 提供所有 ROI 項目的共同基礎
- **特性**:
  - 抽象屬性：`ItemType`, `Center`, `Size`, `Angle`, `DisplayText`, `SimpleDisplayText`
  - 文字屬性：`Point1Text`, `Point2Text`, `Point3Text`, `SizeText`, `AngleText`, `CenterText`
  - 實用方法：`ClearUIElements()`, `ResetDragState()`, `CalculateProperties()`
  - 實作 `INotifyPropertyChanged` 介面

### 2. 具體 ROI 類別
所有具體的 ROI 類別都繼承 `BaseItem`：

- **LineItem** (DrawLineItem)
- **EllipseRoiItem**
- **RectRoiItem**
- **PolygonRoiItem**
- **RulerItem**
- **BezierArcRoiItem**
- **CircularArcRoiItem**

### 3. RoiItem 包裝類別（優化後）
- **位置**: `HyImageShow/ImageShowWPF/Models/RoiItem.cs`
- **目的**: 作為 UI 清單顯示的包裝類別
- **架構**: 繼承 `BaseItem` 並使用委託模式
- **特性**:
  - 繼承 `BaseItem` 以減少重複程式碼
  - 使用委託模式從原始物件獲取資料
  - 監聽原始物件的屬性變更事件
  - 提供統一的清單顯示介面
  - 格式化顯示文字以適合清單顯示

## 重構優點

### 1. 程式碼重用
- 所有 ROI 類別共享相同的基礎屬性和方法
- 減少重複程式碼，提高維護性

### 2. 統一介面
- 所有 ROI 類別都實作相同的介面
- 便於統一處理和管理

### 3. 即時更新
- 所有 ROI 類別都支援屬性變更通知
- UI 可以即時反映資料變更

### 4. 擴展性
- 新增 ROI 類型時只需繼承 `BaseItem`
- 保持架構的一致性

## 重構後的架構圖

```
BaseItem (抽象基礎類別)
├── LineItem
├── EllipseRoiItem
├── RectRoiItem
├── PolygonRoiItem
├── RulerItem
├── BezierArcRoiItem
├── CircularArcRoiItem
└── RoiItem (包裝類別，繼承 BaseItem)
    ├── 委託給原始物件
    ├── 監聽屬性變更
    └── 格式化顯示文字
```

## 使用方式

### 1. 建立具體 ROI 物件
```csharp
var ellipse = new EllipseRoiItem
{
    CenterPoint = new Point(100, 100),
    Radius = 50
};
```

### 2. 建立 RoiItem 包裝
```csharp
var roiItem = new RoiItem
{
    OriginalObject = ellipse
};
```

### 3. UI 綁定
```xml
<TextBlock Text="{Binding Point1Text}" />
<TextBlock Text="{Binding SizeText}" />
<TextBlock Text="{Binding AngleText}" />
```

## 注意事項

### 1. 屬性變更通知
- 所有屬性變更都必須觸發 `PropertyChanged` 事件
- 計算屬性變更時要觸發相關文字屬性的變更通知

### 2. 文字屬性格式化
- 每個 ROI 類型都有適合的文字顯示格式
- 座標顯示使用整數格式 `{X:F0}`
- 尺寸和角度顯示使用小數格式 `{Value:F1}`

### 3. 事件監聽
- `RoiItem` 會自動監聽原始物件的屬性變更
- 當原始物件變更時，`RoiItem` 會自動更新顯示

## 維護指南

### 1. 新增 ROI 類型
1. 建立新的類別繼承 `BaseItem`
2. 實作所有抽象成員
3. 在 `RoiItem` 中添加對應的格式化方法
4. 更新 `RoiManagementService` 的處理邏輯

### 2. 修改顯示格式
- 修改 `RoiItem` 中的格式化方法
- 確保格式適合清單顯示

### 3. 除錯
- 檢查屬性變更事件是否正確觸發
- 確認 UI 綁定路徑是否正確
- 驗證委託模式是否正常工作 