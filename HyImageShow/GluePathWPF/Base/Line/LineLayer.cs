using Hyperbrid.UIX.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace HyImageShow
{
    public class LineLayer : INotifyPropertyChanged
    {
        public LineLayer(LineLayerService lineLayerService)
        {
            Lines = new ObservableCollection<LinePoint>();
            Lines.CollectionChanged += OnLinesCollectionChanged;

            LineLayerService = lineLayerService;

            InitializeCommands();
        }

        public LineLayerService LineLayerService { get; private set; } = null;

        private void InitializeCommands()
        {
            AddLineCommand = new RelayCommand(_ => LineLayerService.AddLine(this));
            DeleteLineCommand = new RelayCommand(_ => LineLayerService.DeleteLine(this),
                _ => _lines.Any(x => x.IsSelected));

            MoveLineUpCommand = new RelayCommand(
                () => LineLayerService.MoveLineUp(this),
                () => LineLayerService.CanMoveLineUp(this));

            MoveLineDownCommand = new RelayCommand(
                () => LineLayerService.MoveLineDown(this),
                () => LineLayerService.CanMoveLineDown(this));
        }

        public void CopyFrom(LineLayer layer)
        {
            _lines.Clear();

            foreach (LinePoint point in layer._lines)
            {
                _lines.CollectionChanged -= OnLinesCollectionChanged;
                _lines.Add(point);
                _lines.CollectionChanged += OnLinesCollectionChanged;
            }

            _name = layer._name;
            _isVisible = layer._isVisible;
            _isActive = layer._isActive;

            this.LineLayerService = layer.LineLayerService;

            AddLineCommand = layer.AddLineCommand;
            DeleteLineCommand = layer.DeleteLineCommand;
            MoveLineUpCommand = layer.MoveLineUpCommand;
            MoveLineDownCommand = layer.MoveLineDownCommand;

            DoShowScale = layer.DoShowScale;
            DrawVisibleLine = layer.DrawVisibleLine;
            RemoveInvisibleLine = layer.RemoveInvisibleLine;
            AfterLineChangeIndexAction = layer.AfterLineChangeIndexAction;
            GetDrawingMode = layer.GetDrawingMode;
            AddUndoHistory = layer.AddUndoHistory;
        }

        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand MoveLineUpCommand { get; set; }
        public ICommand MoveLineDownCommand { get; set; }

        public Action<LinePoint> DoShowScale { get; set; }
        public Action<LinePoint> DrawVisibleLine
        {
            get;
            set;
        }

        public Action<LinePoint> RemoveInvisibleLine { get; set; }
        public Action AfterLineChangeIndexAction { get; set; }
        public Func<EDrawingMode> GetDrawingMode { get; set; }
        public Action<IEditCommand, bool> AddUndoHistory { get; set; }

        private void VisibleChanged(bool isVisible)
        {
            if (isVisible && !IsActive)
            {
                foreach (var lp in _lines)
                {
                    DrawVisibleLine.Invoke(lp);
                }
            }
            else
            {
                foreach (var lp in _lines)
                {
                    RemoveInvisibleLine.Invoke(lp);
                }
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;

                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;

                    OnPropertyChanged(nameof(IsVisible));

                    VisibleChanged(_isVisible);
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;

                    OnPropertyChanged(nameof(_isActive));

                    VisibleChanged(_isVisible);
                }
            }
        }

        private ObservableCollection<LinePoint> _lines;
        public ObservableCollection<LinePoint> Lines
        {
            get => _lines;
            set
            {
                if (_lines != value)
                {
                    _lines = value;
                    OnPropertyChanged(nameof(Lines));
                }
            }
        }

        public List<LinePoint> SelectedLines => Lines.Where(x => x.IsSelected).ToList();

        private void OnLinesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (GluePathEditorViewModel.IsLoadingFile)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (var item in e.NewItems)
                {
                    HandleAddOrReplaceAction();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
            }
            else
            {
            }
        }

        private void HandleAddOrReplaceAction()
        {
            var allLines = Lines.OrderBy(l => l.LineIndex).ToList();
            LinePoint latestAlign = null;

            foreach (var line in allLines)
            {
                if (line.LinePointType == ELinePointType.Align)
                {
                    latestAlign = line;
                }
                else if (line.LinePointType == ELinePointType.Line
                        || line.LinePointType == ELinePointType.Point)
                {
                    if (latestAlign != null)
                    {
                        line.UpdateRelative(latestAlign);
                    }
                    else
                    {
                        line.ResetRelative();
                    }
                }
            }
        }

        public void GenerateByPitchParallelLines(
            EOrientation orientationType,
            EDirection directionType,
            double leftTopX,
            double leftTopY,
            double width,
            double height,
            double pitchY)
        {
            int lineCount = (int)(height / pitchY);

            GenerateParallelLines(
                orientationType,
                directionType,
                leftTopX,
                leftTopY,
                width,
                height,
                lineCount,
                pitchY);
        }

        public void GenerateByCountParallelLines(
                EOrientation orientationType,
                EDirection directionType,
                double leftTopX,
                double leftTopY,
                double width,
                double height,
                double lineCount)
        {
            int pitchY = (int)(height / lineCount);

            GenerateParallelLines(
                orientationType,
                directionType,
                leftTopX,
                leftTopY,
                width,
                height,
                lineCount,
                pitchY);
        }

        private void GenerateParallelLines(
                EOrientation orientationType,
                EDirection directionType,
                double leftTopX,
                double leftTopY,
                double width,
                double height,
                double lineCount,
                double pitchY)
        {
            for (int i = 0; i < lineCount; i++)
            {
                double offsetY;

                if (orientationType == EOrientation.LeftTop
                    || orientationType == EOrientation.RightTop)
                {
                    offsetY = i * pitchY;
                }
                else // LeftBottom or RightBottom
                {
                    offsetY = -i * pitchY;
                }

                // 根據起始點方向調整起始座標
                double startX = leftTopX;
                double startY = leftTopY + offsetY;
                double endX = leftTopX + width;
                double endY = leftTopY + offsetY;

                switch (orientationType)
                {
                    case EOrientation.LeftTop:
                        startX = leftTopX;
                        startY = leftTopY + offsetY;
                        endX = leftTopX + width;
                        endY = leftTopY + offsetY;
                        break;

                    case EOrientation.RightTop:
                        startX = leftTopX + width;
                        startY = leftTopY + offsetY;
                        endX = leftTopX;
                        endY = leftTopY + offsetY;
                        break;

                    case EOrientation.LeftBottom:
                        startX = leftTopX;
                        startY = leftTopY + height + offsetY;
                        endX = leftTopX + width;
                        endY = leftTopY + height + offsetY;
                        break;

                    case EOrientation.RightBottom:
                        startX = leftTopX + width;
                        startY = leftTopY + height + offsetY;
                        endX = leftTopX;
                        endY = leftTopY + height + offsetY;
                        break;
                }

                if (directionType == EDirection.S)
                {
                    if (Math.Abs(i % 2 - 1) < double.Epsilon)
                    {
                        var swapX = startX;
                        startX = endX;
                        endX = swapX;
                    }
                }

                var linePoint = new LinePoint
                {
                    ParentLayer = this,
                    LinePointType = ELinePointType.Line,
                    LineIndex = Lines.Count,

                    StartX = startX,
                    StartY = startY,
                    EndX = endX,
                    EndY = endY,
                };

                Lines.Add(linePoint);
            }
        }

    

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
