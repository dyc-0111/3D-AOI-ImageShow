using Hyperbrid.UIX.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow
{
    /// <summary>
    /// GluePathEditorView.xaml 的互動邏輯
    /// </summary>
    public partial class GluePathEditorView : UserControl
    {
        public GluePathEditorView()
        {
            InitializeComponent();

            InitColorPicker();

            this.KeyDown += GluePathEditorView_KeyDown;

            RenderOptions.SetEdgeMode(GluePathCanvas, EdgeMode.Aliased);

            //int renderTier = RenderCapability.Tier >> 16;
            //Console.WriteLine($"Render Tier: {renderTier}");
        }

        public GluePathEditorViewModel ViewModel => DataContext as GluePathEditorViewModel;

        private readonly DrawingService drawingService = new DrawingService();

        private readonly Dictionary<LinePoint, Line> lineDictionary =
            new Dictionary<LinePoint, Line>();
        private readonly Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary =
            new Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)>();

        private readonly Dictionary<LinePoint, (TextBlock, Line, Line)> lineScaleDictionary =
            new Dictionary<LinePoint, (TextBlock, Line, Line)>();

        private readonly Dictionary<LinePoint, Line> visibleLineDictionary =
            new Dictionary<LinePoint, Line>();
        private readonly Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> visiblePointDictionary =
            new Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)>();

        private List<LinePoint> CurrentSelectedLines = new List<LinePoint>();

        private (LinePoint Line, Ellipse StartPoint, Ellipse EndPoint) LastShowPoints;

        private bool isDraggingCanvas = false;
        private Point lastMousePosition;

        private bool isDragging;
        private Point clickPosition;

        private Ellipse tempStartPoint;
        private Line tempLine;
        private Line tempPointLine;

        private bool isDraggingSelectionRectangle;
        private Rectangle selectionRectangle;
        private Point selectionStartPoint;

        //正方形用
        private Line tempLine1, tempLine2, tempLine3, tempLine4;

        private bool needRemove = false;

        private Point _dragLayerStartPoint;

        private PopupWindow popupWindow;
        private ColorPicker colorPicker;

        private void AnimationSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 拖動完成後更新動畫位置
            ViewModel.AnimationService.HoldStop();
        }

        private void AnimationSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 拖動完成後更新動畫位置
            ViewModel.AnimationService.Seek(
                AnimationSlider.Value,
                ViewModel.CurrentLayer.Lines, lineDictionary);
        }


        #region 存讀檔
        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "圖片檔案 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                ViewModel?.LoadImage(dialog.FileName);
            }
        }

        private void SaveDxfButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "DXF 檔案 (*.dxf)|*.dxf" };

            if (dialog.ShowDialog() == true)
            {
                ViewModel?.SaveDxf(dialog.FileName);

                Hyperbrid.UIX.Controls.MessageBox.Show(
                    "DXF 檔案已成功儲存！", "儲存完成",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadDxfButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "DXF 檔案 (*.dxf)|*.dxf" };
            if (dialog.ShowDialog() == true)
            {
                ViewModel?.LoadDxf(dialog.FileName);

                Hyperbrid.UIX.Controls.MessageBox.Show(
                    "DXF 檔案已完成讀取！", "讀取完畢",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Call DrawingService Methods 
        private void ResetDrawings(bool isTempChange = false) =>
            drawingService.ResetDrawings(
                () => ViewModel.SelectedSolidColorBrush,
                () => ViewModel.TempSelectedSolidColorBrush,
                GluePathCanvas, isTempChange);

        private void DrawLine(LinePoint linePoint) =>
            drawingService.DrawLine(
                lineDictionary,
                pointDictionary,
                lineScaleDictionary,
                linePoint,
                () => ViewModel.SelectedSolidColorBrush,
                Line_MouseLeftButtonDown,
                Line_MouseMove,
                Line_MouseLeftButtonUp,
                Point_MouseLeftButtonDown,
                Point_MouseMove,
                Point_MouseLeftButtonUp,
                GluePathCanvas,
                ref LastShowPoints);

        private void RemoveLine(LinePoint linePoint) =>
            drawingService.RemoveLine(
                lineDictionary,
                pointDictionary,
                lineScaleDictionary,
                linePoint,
                GluePathCanvas);

        private void UpdateLine(LinePoint linePoint) =>
            drawingService.UpdateLine(
                lineDictionary,
                pointDictionary,
                linePoint);

        private void ShowPoint(LinePoint linePoint, bool isLine) =>
            drawingService.ShowPoint(
                pointDictionary,
                linePoint,
                () => ViewModel.SelectedSolidColorBrush,
                Point_MouseLeftButtonDown,
                Point_MouseMove,
                Point_MouseLeftButtonUp,
                isLine,
                GluePathCanvas,
                ref LastShowPoints);

        private void RemoveShowPoints() =>
            drawingService.RemoveShowPoints(
                pointDictionary, LastShowPoints, GluePathCanvas);

        private void SetDrawingAppearance(LinePoint linePoint, bool isSelected) =>
            drawingService.SetDrawingAppearance(
                lineDictionary, pointDictionary, linePoint,
                () => ViewModel.SelectedSolidColorBrush, isSelected);

        private void UpdateLineAppearance(Line line, Brush stroke) =>
            drawingService.UpdateLineAppearance(line, stroke);

        private void SetPointAppearance(LinePoint linePoint, bool isSelected) =>
            drawingService.SetPointAppearance(pointDictionary, linePoint,
                () => ViewModel.SelectedSolidColorBrush, isSelected);

        private void RefreshScale(LinePoint linePoint) =>
            drawingService.RefreshScale(lineScaleDictionary, linePoint, GluePathCanvas);

        private void DrawVisibleLine(LinePoint linePoint) =>
            drawingService.DrawVisibleLine(
                visibleLineDictionary, visiblePointDictionary, linePoint,
                () => ViewModel.SelectedSolidColorBrush,
                GluePathCanvas);

        private void RemoveInvisibleLine(LinePoint linePoint) =>
            drawingService.RemoveInvisibleLine(
                visibleLineDictionary,
                visiblePointDictionary,
                linePoint,
                GluePathCanvas);

        private void UpdateRelativeLines() =>
            drawingService.UpdateRelativeLines(ViewModel.CurrentLayer.Lines);
        #endregion

        #region Some Events
        public void InitViewModelEvent()
        {
            BindViewModelEvents();

            ViewModel.AddLayer();
            ViewModel.EditCmdHistory.ClearAll();

            InitLineAnimationService();
        }

        private void BindViewModelEvents()
        {
            ViewModel.DoShowScale = RefreshScale;
            ViewModel.DrawVisibleLine = DrawVisibleLine;
            ViewModel.RemoveInvisibleLine = RemoveInvisibleLine;
            ViewModel.AfterLineChangeIndexAction = UpdateRelativeLines;
            ViewModel.RemoveLineDrawing = RemoveLine;
            ViewModel.OnLinePointPropertyChanged = OnLinePointPropertyChanged;
            ViewModel.OnLinesCollectionChanged = OnLinesCollectionChanged;
        }

        private void InitLineAnimationService()
        {
            ViewModel.InitLineAnimationService(PauseResumeButton, lineDictionary);
        }

        private void GluePathEditorView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete
                && ViewModel?.CurrentLayer.DeleteLineCommand?.CanExecute(null) == true)
            {
                ViewModel.CurrentLayer.DeleteLineCommand.Execute(null);
            }
        }

        private void GluePathCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? 1.1 : 0.9;
            Point mousePosition = e.GetPosition(GluePathCanvasGrid); // 获取鼠标相对 DragCanvas 的坐标

            Zoom(zoomFactor, mousePosition);
        }

        private void Zoom(double zoomFactor, Point mousePosition)
        {
            // 计算缩放前鼠标的相对位置
            double prevX = (mousePosition.X - GluePathCanvasTranslateTransform.X) / GluePathCanvasScaleTransform.ScaleX;
            double prevY = (mousePosition.Y - GluePathCanvasTranslateTransform.Y) / GluePathCanvasScaleTransform.ScaleY;

            // 执行缩放
            double newScaleX = GluePathCanvasScaleTransform.ScaleX * zoomFactor;
            double newScaleY = GluePathCanvasScaleTransform.ScaleY * zoomFactor;

            // 限制缩放比例在合理范围内
            if (newScaleX < 0.01) newScaleX = 0.01;
            if (newScaleX > 100) newScaleX = 100;
            if (newScaleY < 0.01) newScaleY = 0.01;
            if (newScaleY > 100) newScaleY = 100;

            GluePathCanvasScaleTransform.ScaleX = newScaleX;
            GluePathCanvasScaleTransform.ScaleY = newScaleY;

            // 计算缩放后鼠标的相对位置
            double newX = prevX * GluePathCanvasScaleTransform.ScaleX + GluePathCanvasTranslateTransform.X;
            double newY = prevY * GluePathCanvasScaleTransform.ScaleY + GluePathCanvasTranslateTransform.Y;

            // 通过 TranslateTransform 使缩放后的鼠标位置与缩放前一致
            GluePathCanvasTranslateTransform.X -= (newX - mousePosition.X);
            GluePathCanvasTranslateTransform.Y -= (newY - mousePosition.Y);
        }

        private void OnLinesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (LinePoint newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += OnLinePointPropertyChanged;

                        DrawLine(newItem);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (LinePoint oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= OnLinePointPropertyChanged;

                        RemoveLine(oldItem);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                foreach (LinePoint movedItem in e.NewItems)
                {
                    UpdateLine(movedItem);
                }
            }
        }

        private void OnLinePointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is LinePoint linePoint)
            {
                UpdateLine(linePoint);
            }
        }

        private void DGPathLineEditter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 處理新增的選中項目
            foreach (var addedItem in e.AddedItems)
            {
                if (addedItem is LinePoint linePoint)
                {
                    linePoint.IsSelected = true;

                    SetDrawingAppearance(linePoint, true);
                }
            }

            // 處理移除的選中項目
            foreach (var removedItem in e.RemovedItems)
            {
                if (removedItem is LinePoint linePoint)
                {
                    linePoint.IsSelected = false;

                    SetDrawingAppearance(linePoint, false);
                }
            }
        }

        private void DGPathLineEditter_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 獲取右鍵點擊的 DataGridRow
            var dataGrid = sender as DataGrid;
            var hitTest = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
            if (hitTest == null) return;

            var cell = FindParent<DataGridCell>(hitTest.VisualHit);
            if (cell == null) return;

            // 確認是否為 DataGridCheckBoxColumn
            if (cell.Column is DataGridCheckBoxColumn checkBoxColumn
                && checkBoxColumn.Binding is Binding binding
                && binding.Path.Path == "ShowScale")
            {
                // 創建 ContextMenu
                var contextMenu = new ContextMenu();

                var showAllScaleMenuItem = new MenuItem { Header = "顯示全部尺標" };
                showAllScaleMenuItem.Click += (s, args) =>
                {
                    foreach (var item in ViewModel.CurrentLayer.Lines)
                    {
                        item.ShowScale = true;
                    }
                };

                var unShowAllScaleMenuItem = new MenuItem { Header = "隱藏全部尺標" };
                unShowAllScaleMenuItem.Click += (s, args) =>
                {
                    foreach (var item in ViewModel.CurrentLayer.Lines)
                    {
                        item.ShowScale = false;
                    }
                };

                var showSelectedScaleMenuItem = new MenuItem { Header = "顯示選擇尺標" };
                showSelectedScaleMenuItem.Click += (s, args) =>
                {
                    foreach (var item in ViewModel.CurrentLayer.Lines)
                    {
                        if (item.IsSelected)
                            item.ShowScale = true;
                    }
                };

                var unshowSelectedScaleMenuItem = new MenuItem { Header = "隱藏選擇尺標" };
                unshowSelectedScaleMenuItem.Click += (s, args) =>
                {
                    foreach (var item in ViewModel.CurrentLayer.Lines)
                    {
                        if (item.IsSelected)
                            item.ShowScale = false;
                    }
                };

                contextMenu.Items.Add(showAllScaleMenuItem);
                contextMenu.Items.Add(unShowAllScaleMenuItem);
                contextMenu.Items.Add(showSelectedScaleMenuItem);
                contextMenu.Items.Add(unshowSelectedScaleMenuItem);

                // 顯示 ContextMenu
                contextMenu.IsOpen = true;

                e.Handled = true;
            }
            else if (cell.Column is DataGridTextColumn textBoxColumn
                    && textBoxColumn.Binding is Binding tbinding
                    && (tbinding.Path.Path == "StartXInMm"
                         || tbinding.Path.Path == "StartYInMm"))
            {
                // 創建 ContextMenu
                var contextMenu = new ContextMenu();

                var alignLeftMenuItem = new MenuItem { Header = "向左靠齊" };
                alignLeftMenuItem.Click += (s, args) =>
                {
                    drawingService.AlignSelectedLines(
                        lineDictionary, pointDictionary, ViewModel.CurrentLayer, EDrawingAlignType.Left);
                };

                var alignRightMenuItem = new MenuItem { Header = "向右靠齊" };
                alignRightMenuItem.Click += (s, args) =>
                {
                    drawingService.AlignSelectedLines(
                        lineDictionary, pointDictionary, ViewModel.CurrentLayer, EDrawingAlignType.Right);
                };

                var alignTopMenuItem = new MenuItem { Header = "向上靠齊" };
                alignTopMenuItem.Click += (s, args) =>
                {
                    drawingService.AlignSelectedLines(
                        lineDictionary, pointDictionary, ViewModel.CurrentLayer, EDrawingAlignType.Top);
                };

                var alignDownMenuItem = new MenuItem { Header = "向下靠齊" };
                alignDownMenuItem.Click += (s, args) =>
                {
                    drawingService.AlignSelectedLines(
                        lineDictionary, pointDictionary, ViewModel.CurrentLayer, EDrawingAlignType.Down);
                };


                contextMenu.Items.Add(alignLeftMenuItem);
                contextMenu.Items.Add(alignRightMenuItem);
                contextMenu.Items.Add(alignTopMenuItem);
                contextMenu.Items.Add(alignDownMenuItem);

                // 顯示 ContextMenu
                contextMenu.IsOpen = true;

                e.Handled = true;
            }                       
        }


        // Helper 方法：查找父控件
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;

            if (parent is T parentAsT)
                return parentAsT;

            return FindParent<T>(parent);
        }

        #endregion

        #region Color Picker
        private void InitColorPicker()
        {
            colorPicker = new ColorPicker();
            colorPicker.SelectedColorChanged += ColorPicker_SelectedColorChanged;
            colorPicker.Confirmed += ColorPicker_Confirmed;
            colorPicker.Canceled += ColorPicker_Canceled;
        }

        private void ColorPicker_SelectedColorChanged(object sender, Hyperbrid.UIX.Data.FunctionEventArgs<Color> e)
        {
            var selectedColor = e.Info;

            ViewModel.TempSelectedSolidColorBrush = new SolidColorBrush(selectedColor);

            ResetDrawings(true);
        }

        private void ColorPicker_Canceled(object sender, System.EventArgs e)
        {
            // 關閉包含 ColorPicker 的 PopupWindow
            ResetDrawings();

            popupWindow?.Close();
        }

        private void ColorPicker_Confirmed(object sender, Hyperbrid.UIX.Data.FunctionEventArgs<Color> e)
        {
            ViewModel.SelectedSolidColorBrush = ViewModel.TempSelectedSolidColorBrush;

            ResetDrawings();

            popupWindow?.Close();
        }

        private void OpenColorPickerPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            popupWindow = new PopupWindow
            {
                Title = "選擇顏色",
                ShowTitle = true,
                ShowBorder = false,
                PopupElement = colorPicker,
            };

            popupWindow.ShowWindowStart(popupWindow.PopupElement);
        }
        #endregion

        #region LayerList
        private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GluePathEditorViewModel.IsLoadingFile)
                return;


            if (ViewModel.CurrentLayer != null)
            {
                foreach (var linePoint in ViewModel.CurrentLayer.Lines)
                {
                    linePoint.PropertyChanged += OnLinePointPropertyChanged;

                    DrawLine(linePoint);
                }
            }

            if (ViewModel.LastLayer != null)
            {
                foreach (var linePoint in ViewModel.LastLayer.Lines)
                {
                    linePoint.PropertyChanged -= OnLinePointPropertyChanged;

                    RemoveLine(linePoint);
                }
            }
        }


        private void LayerList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragLayerStartPoint = e.GetPosition(null);
        }

        private void LayerList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(null);

            Vector diff = _dragLayerStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed
                && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is ListView
                    && GetListViewItemUnderMouse(e) is ListViewItem item
                    && item.DataContext is LineLayer layers)
                {
                    DragDrop.DoDragDrop(item, layers, DragDropEffects.Move);
                }
            }
        }

        private void LayerList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(LineLayer)))
            {
                if (e.Data.GetData(typeof(LineLayer)) is LineLayer droppedData
                    && GetListViewItemUnderMouse(e)?.DataContext is LineLayer target
                    && !ReferenceEquals(droppedData, target))
                {
                    ObservableCollection<LineLayer> list = (ObservableCollection<LineLayer>)
                                    LayerList.ItemsSource;

                    int oldIndex = list.IndexOf(droppedData);
                    int newIndex = list.IndexOf(target);

                    if (oldIndex != newIndex
                        && oldIndex >= 0
                        && newIndex >= 0)
                    {
                        list.Move(oldIndex, newIndex);
                        UpdateIndex(list);
                    }
                }
            }
        }

        private void UpdateIndex(ObservableCollection<LineLayer> lineLayers)
        {
            for (int index = 0; index < lineLayers.Count; index++)
            {
                lineLayers[index].Name = index.ToString();
            }
        }

        private ListViewItem GetListViewItemUnderMouse(MouseEventArgs e)
        {
            DependencyObject obj = e.OriginalSource as DependencyObject;

            while (obj != null
                   && !(obj is ListViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as ListViewItem;
        }

        private ListViewItem GetListViewItemUnderMouse(DragEventArgs e)
        {
            Point p = e.GetPosition(LayerList);

            var hitTestResult = VisualTreeHelper.HitTest(LayerList, p);

            DependencyObject obj = hitTestResult?.VisualHit;

            while (obj != null
                   && !(obj is ListViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as ListViewItem;
        }
        #endregion

        #region Fit
        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            Fit();
        }

        private void Fit()
        {
            // 重置縮放和位移
            GluePathCanvasScaleTransform.ScaleX = 1;
            GluePathCanvasScaleTransform.ScaleY = 1;
            GluePathCanvasTranslateTransform.X = 0;
            GluePathCanvasTranslateTransform.Y = 0;

            // 置中
            GluePathCanvas.UpdateLayout();
        }
        #endregion

        #region Line
        private void Line_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ViewModel.IsEnableSelected)
            {
                return;
            }

            if (sender is Line line)
            {
                needRemove = false;

                if (line.Tag is LinePoint linePoint)
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        // 多選模式，添加到選中集合中
                        if (ViewModel.CurrentLayer.SelectedLines.Contains(linePoint))
                        {
                            linePoint.IsSelected = false;

                            UpdateLineAppearance(line, ViewModel.SelectedSolidColorBrush);

                            RemoveShowPoints();
                        }
                        else
                        {
                            linePoint.IsSelected = true;

                            UpdateLineAppearance(line, Brushes.Red);

                            RemoveShowPoints();
                        }
                    }
                    else
                    {
                        if (ViewModel.CurrentLayer.SelectedLines != null)
                        {
                            if (!ViewModel.CurrentLayer.SelectedLines.Contains(linePoint))
                            {
                                // 單選模式，清除其他選中項目
                                ResetDrawings();

                                foreach (var _line in ViewModel.CurrentLayer.Lines)
                                {
                                    if (!_line.Equals(linePoint))
                                        _line.IsSelected = false;
                                }

                                linePoint.IsSelected = true;

                                UpdateLineAppearance(line, Brushes.Red);

                                RemoveShowPoints();
                                ShowPoint(linePoint, true);
                            }
                            else
                            {
                                needRemove = true;

                                RemoveShowPoints();
                            }
                        }
                    }
                }

                CurrentSelectedLines = new List<LinePoint>();

                int i = 0;

                foreach (var selectedLine in ViewModel.CurrentLayer.SelectedLines)
                {
                    CurrentSelectedLines.Add(new LinePoint());

                    CurrentSelectedLines[i].CopyFrom(selectedLine);

                    i++;
                }

                isDragging = true;
                clickPosition = e.GetPosition(GluePathCanvas);
                line.CaptureMouse();
            }
        }

        private void Line_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ViewModel.IsEnableSelected)
            {
                return;
            }

            if (isDragging)
            {
                var currentPosition = e.GetPosition(GluePathCanvas);
                var offsetX = currentPosition.X - clickPosition.X;
                var offsetY = currentPosition.Y - clickPosition.Y;

                foreach (var linePoint in ViewModel.CurrentLayer.SelectedLines)
                {
                    linePoint.StartX += offsetX;
                    linePoint.StartY += offsetY;

                    if (linePoint.LinePointType == ELinePointType.Point
                        || linePoint.LinePointType == ELinePointType.Align)
                    {
                        linePoint.EndX = linePoint.StartX;
                        linePoint.EndY = linePoint.StartY;
                    }
                    else
                    {
                        linePoint.EndX += offsetX;
                        linePoint.EndY += offsetY;
                    }
                }


                clickPosition = currentPosition;

                needRemove = false;
            }
        }

        private void Line_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ViewModel.IsEnableSelected)
            {
                return;
            }

            var line = sender as Line;

            if (needRemove)
            {
                needRemove = false;

                if (line != null)
                {
                    var linePoint = line.Tag as LinePoint;
                    if (linePoint != null)
                    {
                        if (ViewModel.CurrentLayer.SelectedLines.Contains(linePoint))
                        {
                            // 單選模式，清除其他選中項目
                            ResetDrawings();

                            foreach (var _line in ViewModel.CurrentLayer.Lines)
                            {
                                _line.IsSelected = false;
                            }

                            linePoint.IsSelected = true;

                            UpdateLineAppearance(line, Brushes.Red);
                        }
                    }
                }
            }
            else
            {
                foreach (var selectLine in ViewModel.CurrentLayer.SelectedLines)
                {
                    UpdateLine(selectLine);
                }

                UpdateRelativeLines();

                ViewModel.EditCmdHistory.AddUndoHistory(
                    new PointEditCommand(ViewModel.CurrentLayer, CurrentSelectedLines,
                        ViewModel.CurrentLayer.SelectedLines, UpdateLine), true);
            }

            if (line != null)
            {
                isDragging = false;
                line.ReleaseMouseCapture();
            }
        }
        #endregion

        #region Point
        private void Point_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ViewModel.IsEnableSelected)
            {
                return;
            }

            if (sender is Ellipse point)
            {
                ResetDrawings();

                var linePoint = point.Tag as LinePoint;
                if (linePoint != null)
                {
                    if (linePoint.LinePointType == ELinePointType.Line)
                    {
                        point.Fill = Brushes.Red;
                    }
                    else
                    {
                        needRemove = false;

                        if (Keyboard.IsKeyDown(Key.LeftCtrl)
                            || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            // 多選模式，添加到選中集合中
                            if (ViewModel.CurrentLayer.SelectedLines.Contains(linePoint))
                            {
                                linePoint.IsSelected = false;

                                foreach (var p in ViewModel.CurrentLayer.SelectedLines)
                                {
                                    SetPointAppearance(p, true);
                                }
                            }
                            else
                            {
                                linePoint.IsSelected = true;

                                foreach (var p in ViewModel.CurrentLayer.SelectedLines)
                                {
                                    SetPointAppearance(p, true);
                                }
                            }
                        }
                        else
                        {
                            if (!ViewModel.CurrentLayer.SelectedLines.Contains(linePoint))
                            {
                                // 單選模式，清除其他選中項目
                                ResetDrawings();

                                foreach (var _line in ViewModel.CurrentLayer.Lines)
                                {
                                    _line.IsSelected = false;
                                }

                                linePoint.IsSelected = true;
                            }
                            else
                            {
                                foreach (var p in ViewModel.CurrentLayer.SelectedLines)
                                {
                                    SetDrawingAppearance(p, true);
                                }

                                needRemove = true;
                            }
                        }
                    }
                }

                CurrentSelectedLines = new List<LinePoint>();

                var lp = new LinePoint(linePoint);
                CurrentSelectedLines.Add(lp);

                isDragging = true;
                clickPosition = e.GetPosition(GluePathCanvas);
                point.CaptureMouse();
            }
        }

        private void Point_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ViewModel.IsEnableSelected)
            {
                return;
            }

            if (isDragging)
            {
                if (sender is Ellipse point)
                {
                    var linePoint = point.Tag as LinePoint;
                    if (linePoint != null)
                    {
                        var currentPosition = e.GetPosition(GluePathCanvas);
                        var offsetX = currentPosition.X - clickPosition.X;
                        var offsetY = currentPosition.Y - clickPosition.Y;

                        if (linePoint.LinePointType == ELinePointType.Line)
                        {
                            if (point.Stroke == Brushes.Blue) // Start point
                            {
                                linePoint.StartX += offsetX;
                                linePoint.StartY += offsetY;
                            }
                            else if (point.Stroke == Brushes.Green) // End point
                            {
                                linePoint.EndX += offsetX;
                                linePoint.EndY += offsetY;
                            }

                            clickPosition = currentPosition;
                        }
                        else if (linePoint.LinePointType == ELinePointType.Point
                                || linePoint.LinePointType == ELinePointType.Align)
                        {
                            foreach (var _linePoint in ViewModel.CurrentLayer.SelectedLines)
                            {
                                _linePoint.StartX += offsetX;
                                _linePoint.StartY += offsetY;

                                if (_linePoint.LinePointType == ELinePointType.Point
                                    || _linePoint.LinePointType == ELinePointType.Align)
                                {
                                    _linePoint.EndX = _linePoint.StartX;
                                    _linePoint.EndY = _linePoint.StartY;
                                }
                                else
                                {
                                    _linePoint.EndX += offsetX;
                                    _linePoint.EndY += offsetY;
                                }
                            }

                            clickPosition = e.GetPosition(GluePathCanvas);

                            needRemove = false;
                        }
                    }
                }
            }
        }

        private void Point_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ViewModel.IsEnableSelected)
            {
                return;
            }

            if (sender is Ellipse point)
            {
                if (needRemove)
                {
                    needRemove = false;

                    if (point.Tag is LinePoint linePoint
                        && (linePoint.LinePointType == ELinePointType.Point
                            || linePoint.LinePointType == ELinePointType.Align))
                    {
                        if (ViewModel.CurrentLayer.SelectedLines.Contains(linePoint))
                        {
                            // 單選模式，清除其他選中項目
                            ResetDrawings();

                            foreach (var _line in ViewModel.CurrentLayer.Lines)
                            {
                                _line.IsSelected = false;
                            }

                            linePoint.IsSelected = true;

                            foreach (var p in ViewModel.CurrentLayer.SelectedLines)
                            {
                                SetPointAppearance(p, true);
                            }
                        }
                    }
                }
                else
                {
                    UpdateRelativeLines();

                    ViewModel.EditCmdHistory.AddUndoHistory(
                        new PointEditCommand(ViewModel.CurrentLayer, CurrentSelectedLines[0],
                            new LinePoint(point.Tag as LinePoint), UpdateLine), true);
                }

                isDragging = false;
                point.ReleaseMouseCapture();
            }
        }
        #endregion

        #region Canvas Mouse
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = true;
                lastMousePosition = e.GetPosition(GluePathCanvasGrid);

                GluePathCanvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingCanvas)
            {
                var currentPosition = e.GetPosition(GluePathCanvasGrid);
                var offset = currentPosition - lastMousePosition;

                GluePathCanvasTranslateTransform.X += offset.X;
                GluePathCanvasTranslateTransform.Y += offset.Y;

                lastMousePosition = currentPosition;
            }

            if (ViewModel.IsEnableSelected)
            {
                if (isDraggingSelectionRectangle && selectionRectangle != null)
                {
                    // 更新選擇框的大小和位置
                    var currentPoint = e.GetPosition(GluePathCanvas);
                    var x = Math.Min(currentPoint.X, selectionStartPoint.X);
                    var y = Math.Min(currentPoint.Y, selectionStartPoint.Y);
                    var width = Math.Abs(currentPoint.X - selectionStartPoint.X);
                    var height = Math.Abs(currentPoint.Y - selectionStartPoint.Y);

                    Canvas.SetLeft(selectionRectangle, x);
                    Canvas.SetTop(selectionRectangle, y);
                    selectionRectangle.Width = width;
                    selectionRectangle.Height = height;
                }

                return;
            }

            switch (ViewModel.DrawingMode)
            {
                case EDrawingMode.AlignmentPoint:
                case EDrawingMode.Point:
                    break;

                case EDrawingMode.Line:
                    {
                        if (isDragging && tempLine != null)
                        {
                            var position = e.GetPosition(GluePathCanvas);
                            tempLine.X2 = position.X;
                            tempLine.Y2 = position.Y;
                        }
                    }
                    break;

                case EDrawingMode.Rectangle:
                case EDrawingMode.MultiLines:
                    {
                        if (isDragging
                            && tempLine1 != null
                            && tempLine2 != null
                            && tempLine3 != null
                            && tempLine4 != null)
                        {
                            var position = e.GetPosition(GluePathCanvas);
                            var width = position.X - clickPosition.X;
                            var height = position.Y - clickPosition.Y;

                            // 更新四條線的位置
                            tempLine1.X2 = tempLine1.X1 + width;
                            tempLine1.Y2 = tempLine1.Y1;

                            tempLine2.X1 = tempLine1.X2;
                            tempLine2.Y1 = tempLine1.Y2;
                            tempLine2.X2 = tempLine2.X1;
                            tempLine2.Y2 = tempLine2.Y1 + height;

                            tempLine3.X1 = tempLine2.X2;
                            tempLine3.Y1 = tempLine2.Y2;
                            tempLine3.X2 = tempLine3.X1 - width;
                            tempLine3.Y2 = tempLine3.Y1;

                            tempLine4.X1 = tempLine3.X2;
                            tempLine4.Y1 = tempLine3.Y2;
                            tempLine4.X2 = tempLine4.X1;
                            tempLine4.Y2 = tempLine4.Y1 - height;
                        }
                    }
                    break;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = false;
            }
            else
            {
                GluePathCanvas.Children.Remove(selectionRectangle);
                selectionRectangle = null;
                isDraggingSelectionRectangle = false;
            }

            GluePathCanvas.ReleaseMouseCapture(); // 釋放滑鼠

            if (!this.IsKeyboardFocused)
            {
                Keyboard.Focus(this); // 焦點丟失時重新設置
            }
        }

        #endregion

        #region Canvas Left Mouse
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsEnableSelected)
            {
                if (!isDragging)
                {
                    // 初始化選擇框
                    selectionStartPoint = e.GetPosition(GluePathCanvas);
                    selectionRectangle = new Rectangle
                    {
                        Stroke = Brushes.Blue,
                        StrokeThickness = 1,
                        Fill = new SolidColorBrush(Color.FromArgb(50, 0, 0, 255)) // 半透明填充
                    };
                    Canvas.SetLeft(selectionRectangle, selectionStartPoint.X);
                    Canvas.SetTop(selectionRectangle, selectionStartPoint.Y);
                    GluePathCanvas.Children.Add(selectionRectangle);

                    isDraggingSelectionRectangle = true;

                    GluePathCanvas.CaptureMouse();
                }

                return;
            }

            ResetDrawings();

            var position = e.GetPosition(GluePathCanvas);

            if (tempStartPoint == null)
            {
                // 新增起始點
                tempStartPoint = DrawPath.CreatePoint(
                    ELinePointType.Point,
                    EPointType.Start,
                    position,
                    Brushes.Blue,
                    1.5);

                GluePathCanvas.Children.Add(tempStartPoint);

                switch (ViewModel.DrawingMode)
                {
                    case EDrawingMode.Point:
                    case EDrawingMode.AlignmentPoint:
                        {
                            //新增臨時線條
                            tempPointLine = DrawPath.CreateLine(
                                position, position, ViewModel.SelectedSolidColorBrush);

                            GluePathCanvas.Children.Add(tempPointLine);
                        }
                        break;

                    case EDrawingMode.Line:
                        {
                            //新增臨時線條
                            tempLine = DrawPath.CreateLine(
                                position, position, ViewModel.SelectedSolidColorBrush);

                            GluePathCanvas.Children.Add(tempLine);
                        }
                        break;

                    case EDrawingMode.Rectangle:
                    case EDrawingMode.MultiLines:
                        {
                            //新增臨時矩形線條
                            tempLine1 = DrawPath.CreateLine(position, position, ViewModel.SelectedSolidColorBrush);
                            tempLine2 = DrawPath.CreateLine(position, position, ViewModel.SelectedSolidColorBrush);
                            tempLine3 = DrawPath.CreateLine(position, position, ViewModel.SelectedSolidColorBrush);
                            tempLine4 = DrawPath.CreateLine(position, position, ViewModel.SelectedSolidColorBrush);

                            GluePathCanvas.Children.Add(tempLine1);
                            GluePathCanvas.Children.Add(tempLine2);
                            GluePathCanvas.Children.Add(tempLine3);
                            GluePathCanvas.Children.Add(tempLine4);
                        }
                        break;
                }

                isDragging = true;
                clickPosition = position;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsEnableSelected)
            {
                if (isDraggingSelectionRectangle && selectionRectangle != null)
                {
                    // 獲取選擇框的範圍
                    var rectX = Canvas.GetLeft(selectionRectangle);
                    var rectY = Canvas.GetTop(selectionRectangle);
                    var rectWidth = selectionRectangle.Width;
                    var rectHeight = selectionRectangle.Height;
                    var selectionBounds = new Rect(rectX, rectY, rectWidth, rectHeight);

                    foreach (var linePoint in ViewModel.CurrentLayer.Lines)
                    {
                        linePoint.PropertyChanged -= OnLinePointPropertyChanged;

                        var startPoint = new Point(linePoint.StartX, linePoint.StartY);
                        var endPoint = new Point(linePoint.EndX, linePoint.EndY);

                        bool inSelection = selectionBounds.Contains(startPoint) || selectionBounds.Contains(endPoint);
                        linePoint.IsSelected = inSelection;

                        if (inSelection)
                        {
                            SetDrawingAppearance(linePoint, true);
                        }
                        else
                        {
                            SetDrawingAppearance(linePoint, false);
                        }

                        linePoint.PropertyChanged += OnLinePointPropertyChanged;
                    }

                    // 移除選擇框
                    GluePathCanvas.Children.Remove(selectionRectangle);
                    selectionRectangle = null;
                    isDraggingSelectionRectangle = false;
                }
                else
                {

                }

                if (!this.IsKeyboardFocused)
                {
                    Keyboard.Focus(this); // 焦點丟失時重新設置
                }

                return;
            }

            switch (ViewModel.DrawingMode)
            {
                case EDrawingMode.AlignmentPoint:
                    {
                        if (isDragging && tempStartPoint != null)
                        {
                            // 如果需要，將點的資訊加入到 ViewModel 中
                            var linePoint = new LinePoint
                            {
                                ParentLayer = ViewModel.CurrentLayer,

                                LinePointType = ELinePointType.Align,
                                LineIndex = ViewModel.CurrentLayer.Lines.Count, // 分配唯一的 LineIndex
                                StartX = tempPointLine.X1,
                                StartY = tempPointLine.Y1,
                                EndX = tempPointLine.X2, // 點的終點與起點相同
                                EndY = tempPointLine.Y2
                            };
                            ViewModel.LineLayerService.AddLine(ViewModel.CurrentLayer, linePoint);

                            GluePathCanvas.Children.Remove(tempStartPoint);
                            GluePathCanvas.Children.Remove(tempPointLine);

                            tempStartPoint = null;

                            isDragging = false;
                        }
                    }
                    break;

                case EDrawingMode.Point:
                    {
                        if (isDragging && tempStartPoint != null)
                        {
                            // 如果需要，將點的資訊加入到 ViewModel 中
                            var linePoint = new LinePoint
                            {
                                ParentLayer = ViewModel.CurrentLayer,

                                LinePointType = ELinePointType.Point,
                                LineIndex = ViewModel.CurrentLayer.Lines.Count, // 分配唯一的 LineIndex
                                StartX = tempPointLine.X1,
                                StartY = tempPointLine.Y1,
                                EndX = tempPointLine.X2, // 點的終點與起點相同
                                EndY = tempPointLine.Y2
                            };
                            ViewModel.LineLayerService.AddLine(ViewModel.CurrentLayer, linePoint);

                            GluePathCanvas.Children.Remove(tempStartPoint);
                            GluePathCanvas.Children.Remove(tempPointLine);

                            tempStartPoint = null;

                            isDragging = false;
                        }
                    }
                    break;

                case EDrawingMode.Line:
                    {
                        if (isDragging && tempStartPoint != null && tempLine != null)
                        {
                            // 新增線條到 ViewModel
                            var linePoint = new LinePoint
                            {
                                ParentLayer = ViewModel.CurrentLayer,

                                LinePointType = ELinePointType.Line,
                                LineIndex = ViewModel.CurrentLayer.Lines.Count, // 分配唯一的 LineIndex
                                StartX = tempLine.X1,
                                StartY = tempLine.Y1,
                                EndX = tempLine.X2,
                                EndY = tempLine.Y2
                            };
                            ViewModel.LineLayerService.AddLine(ViewModel.CurrentLayer, linePoint);

                            GluePathCanvas.Children.Remove(tempStartPoint);
                            GluePathCanvas.Children.Remove(tempLine);

                            // 清除臨時變數
                            tempStartPoint = null;
                            tempLine = null;
                            isDragging = false;
                        }
                    }
                    break;

                case EDrawingMode.Rectangle:
                    {
                        if (isDragging
                            && tempStartPoint != null
                            && tempLine1 != null
                            && tempLine2 != null
                            && tempLine3 != null
                            && tempLine4 != null)
                        {
                            // 新增四條線到 ViewModel
                            var (linePoint1, linePoint2, linePoint3, linePoint4) = CreateRectangleLines(
                                tempLine1, tempLine2, tempLine3, tempLine4);

                            ViewModel.LineLayerService.AddLines(ViewModel.CurrentLayer, linePoint1, linePoint2, linePoint3, linePoint4);

                            GluePathCanvas.Children.Remove(tempStartPoint);
                            GluePathCanvas.Children.Remove(tempLine1);
                            GluePathCanvas.Children.Remove(tempLine2);
                            GluePathCanvas.Children.Remove(tempLine3);
                            GluePathCanvas.Children.Remove(tempLine4);

                            // 清除臨時變數
                            tempStartPoint = null;
                            tempLine1 = null;
                            tempLine2 = null;
                            tempLine3 = null;
                            tempLine4 = null;
                            isDragging = false;
                        }
                    }
                    break;

                case EDrawingMode.MultiLines:
                    {
                        if (isDragging
                            && tempStartPoint != null
                            && tempLine1 != null
                            && tempLine2 != null
                            && tempLine3 != null
                            && tempLine4 != null)
                        {
                            ViewModel.CurrentLayer.GenerateByCountParallelLines(
                                ViewModel.MultiLinesOrientation,
                                ViewModel.MultiLinesDirection,
                                tempLine1.X1,
                                tempLine1.Y1,
                                tempLine1.X2 - tempLine1.X1,
                                tempLine2.Y2 - tempLine2.Y1,
                                10);

                            GluePathCanvas.Children.Remove(tempStartPoint);
                            GluePathCanvas.Children.Remove(tempLine1);
                            GluePathCanvas.Children.Remove(tempLine2);
                            GluePathCanvas.Children.Remove(tempLine3);
                            GluePathCanvas.Children.Remove(tempLine4);

                            // 清除臨時變數
                            tempStartPoint = null;
                            tempLine1 = null;
                            tempLine2 = null;
                            tempLine3 = null;
                            tempLine4 = null;
                            isDragging = false;
                        }
                    }
                    break;
            }

            if (!this.IsKeyboardFocused)
            {
                Keyboard.Focus(this); // 焦點丟失時重新設置
            }
        }
        #endregion


        private (LinePoint l1, LinePoint l2, LinePoint l3, LinePoint l4) CreateRectangleLines(
                Line line1, Line line2, Line line3, Line line4)
        {
            var linePoint1 = new LinePoint
            {
                ParentLayer = ViewModel.CurrentLayer,
                LinePointType = ELinePointType.Line,
                LineIndex = ViewModel.CurrentLayer.Lines.Count,
                StartX = line1.X1,
                StartY = line1.Y1,
                EndX = line1.X2,
                EndY = line1.Y2
            };
            var linePoint2 = new LinePoint
            {
                ParentLayer = ViewModel.CurrentLayer,
                LinePointType = ELinePointType.Line,
                LineIndex = ViewModel.CurrentLayer.Lines.Count + 1,
                StartX = line2.X1,
                StartY = line2.Y1,
                EndX = line2.X2,
                EndY = line2.Y2
            };
            var linePoint3 = new LinePoint
            {
                ParentLayer = ViewModel.CurrentLayer,
                LinePointType = ELinePointType.Line,
                LineIndex = ViewModel.CurrentLayer.Lines.Count + 2,
                StartX = line3.X1,
                StartY = line3.Y1,
                EndX = line3.X2,
                EndY = line3.Y2
            };
            var linePoint4 = new LinePoint
            {
                ParentLayer = ViewModel.CurrentLayer,
                LinePointType = ELinePointType.Line,
                LineIndex = ViewModel.CurrentLayer.Lines.Count + 3,
                StartX = line4.X1,
                StartY = line4.Y1,
                EndX = line4.X2,
                EndY = line4.Y2
            };
            return (linePoint1, linePoint2, linePoint3, linePoint4);
        }
    }
}