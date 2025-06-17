using Hyperbrid.UIX.Interactivity;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HyImageShow
{
    public class GluePathEditorViewModel : INotifyPropertyChanged
    {
        public GluePathEditorViewModel()
        {
            EditCmdHistory = new EditCommandHistory();

            Layers = new ObservableCollection<LineLayer>();
            Layers.CollectionChanged += OnLayersCollectionChanged;

            AddLayerCommand = new RelayCommand(_ => AddLayer());
            RemoveLayerCommand = new RelayCommand(_ => RemoveLayer());

            RedoCommand = new RelayCommand(_ => EditCmdHistory.Redo());
            UndoCommand = new RelayCommand(_ => EditCmdHistory.Undo());

            PlayCommand = new RelayCommand(OnPlay);
            ResumePauseCommand = new RelayCommand(OnResumePause);

            LoadImage(DEFAULT_CANVAS_IMAGE_PATH);
        }

        public LineLayerService LineLayerService { get; private set; } = new LineLayerService();

        public ILineAnimationService AnimationService { get; private set; }

        private const string DEFAULT_CANVAS_IMAGE_PATH = @"D:\ART\Test\fa disp.jpg";

        public static bool IsLoadingFile { get; private set; } = false;

        public void InitLineAnimationService(
            Button pauseResumeButton, 
            Dictionary<LinePoint, System.Windows.Shapes.Line> lineDictionary)
        {
            AnimationService = new LineAnimationService(
                pauseResumeButton, 
                () => SelectedSolidColorBrush);

            PlayRequested += () =>
            AnimationService.Start(CurrentLayer.Lines, lineDictionary);

            ResumePauseRequested += () => AnimationService.ResumePause();
        }

        #region Canvas 背景圖
        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

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
                ImageSource = bitmap;
            }
            catch
            {
                ImageSource = null;
            }
        }
        #endregion

        public EditCommandHistory EditCmdHistory { get; set; }

        public ICommand AddLayerCommand { get; }
        public ICommand RemoveLayerCommand { get; }

        public ICommand RedoCommand { get; }
        public ICommand UndoCommand { get; }

        public ICommand PlayCommand { get; }
        public ICommand ResumePauseCommand { get; }

        public Action<LinePoint> DoShowScale { get; set; }
        public Action<LinePoint> DrawVisibleLine { get; set; }
        public Action<LinePoint> RemoveInvisibleLine { get; set; }
        public Action AfterLineChangeIndexAction { get; set; }
        public Action<LinePoint> RemoveLineDrawing { get; set; }
        public PropertyChangedEventHandler OnLinePointPropertyChanged { get; set; }
        public NotifyCollectionChangedEventHandler OnLinesCollectionChanged { get; set; }

        public event Action PlayRequested;
        public event Action ResumePauseRequested;

        private void OnPlay() => PlayRequested?.Invoke();
        private void OnResumePause() => ResumePauseRequested?.Invoke();

        public double AnimationSpeed
        {
            get => AnimationService.Speed;
            set
            {
                const double Tolerance = 0.0001; 
                // Define a small tolerance for floating-point comparison
                
                if (Math.Abs(AnimationService.Speed - value) > Tolerance)
                {
                    AnimationService.Speed = value;
                    OnPropertyChanged(nameof(AnimationSpeed));
                }
            }
        }

        #region Layer 圖層
        public void AddLayer()
        {
            var layer = new LineLayer(LineLayerService);
            layer.Name = Layers.Count.ToString();

            Layers.Add(layer);

            SetLayerEvents();

            EditCmdHistory.AddUndoHistory(new AddLayerEditCommand(this, layer), true);
        }

        private void AddLayerLoadFile()
        {
            var layer = new LineLayer(LineLayerService);
            layer.Name = Layers.Count.ToString();

            Layers.Add(layer);
        }

        private void RemoveLayer()
        {
            var cmd = new RemoveLayerEditCommand(this, CurrentLayer);

            Layers.Remove(CurrentLayer);

            var indexChanges = UpdateLayerIndex(Layers);
            cmd.SetIndexChanges(indexChanges);

            EditCmdHistory.AddUndoHistory(cmd, true);
        }

        private List<IndexChange> UpdateLayerIndex(ObservableCollection<LineLayer> lineLayers)
        {
            List<IndexChange> indexChanges = new List<IndexChange>();

            for (int index = 0; index < lineLayers.Count; index++)
            {
                if (!lineLayers[index].Name.Equals(index.ToString()))
                {
                    indexChanges.Add(new IndexChange(int.Parse(lineLayers[index].Name), index));
                }

                lineLayers[index].Name = index.ToString();
            }

            return indexChanges;
        }


        private void _setLayerEvents(LineLayer layer)
        {
            layer.Lines.CollectionChanged += OnLinesCollectionChanged;

            foreach (var l in layer.Lines)
            {
                l.ParentLayer = layer;
            }

            layer.DoShowScale = DoShowScale;
            layer.DrawVisibleLine = DrawVisibleLine;
            layer.RemoveInvisibleLine = RemoveInvisibleLine;
            layer.AfterLineChangeIndexAction = AfterLineChangeIndexAction;
            layer.GetDrawingMode = () => DrawingMode;
            layer.AddUndoHistory = EditCmdHistory.AddUndoHistory;
        }

        private void SetLayerEvents()
        {
            _setLayerEvents(Layers[Layers.Count - 1]);
        }

        private void SetAllLayersEvents()
        {
            foreach (var layer in Layers)
            {
                _setLayerEvents(layer);
            }
        }

        private void InitLayer()
        {
            Layers = new ObservableCollection<LineLayer>();
            Layers.CollectionChanged += OnLayersCollectionChanged;
        }

        private bool CheckNewLayer(EntityObject entity, ref int layerIndex, List<string> distinctNames)
        {
            if (entity.Layer.Name.Equals(distinctNames[layerIndex]))
            {
                return false;
            }
            else
            {
                //不同層 換層
                layerIndex++;

                //最後再設 不然好像顯示會怪怪的
                SetLayerEvents();

                AddLayerLoadFile();

                return true;
            }
        }

        private void SetLayerLine(Line line, ref int layerIndex, ref int lineIndex, List<string> distinctNames)
        {
            if (line == null)
                return;

            if (!CheckNewLayer(line, ref layerIndex, distinctNames))
            {
                Layers[layerIndex].Name = layerIndex.ToString();

                LineLayerService.AddLine(
                    Layers[layerIndex],
                    new LinePoint(
                        Layers[layerIndex],
                        line.StartPoint,
                        line.EndPoint,
                        lineIndex,
                        ELinePointType.Line));

                lineIndex++;
            }
        }

        private void SetLayerPoint(Point point, ref int layerIndex, ref int lineIndex, List<string> distinctNames)
        {
            if (point == null)
                return;

            if (!CheckNewLayer(point, ref layerIndex, distinctNames))
            {
                Vector3 vector3 = new Vector3(point.Position.X, point.Position.Y, 0);

                bool isAlign = !point.IsVisible;

                if (isAlign)
                {
                    Layers[layerIndex].Name = layerIndex.ToString();

                    LineLayerService.AddLine(
                        Layers[layerIndex],
                        new LinePoint(
                            Layers[layerIndex],
                            vector3,
                            vector3,
                            lineIndex,
                            ELinePointType.Align));
                }
                else
                {
                    Layers[layerIndex].Name = layerIndex.ToString();

                    LineLayerService.AddLine(
                        Layers[layerIndex],
                        new LinePoint(
                            Layers[layerIndex],
                            vector3,
                            vector3,
                            lineIndex,
                            ELinePointType.Point));
                }

                lineIndex++;
            }
        }
        #endregion

        public void SaveDxf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            DXFConverter.SaveDxfFile(filePath, Layers.ToList());
        }

        public void LoadDxf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var entityObjects = DXFConverter.LoadDxfFile(filePath);

            if (entityObjects.Count == 0)
                return;

            IsLoadingFile = true;

            if (CurrentLayer != null)
            {
                foreach (var linePoint in CurrentLayer.Lines)
                {
                    linePoint.PropertyChanged -= OnLinePointPropertyChanged;
                    RemoveLineDrawing.Invoke(linePoint);
                }
            }

            if (LastLayer != null)
            {
                foreach (var linePoint in LastLayer.Lines)
                {
                    linePoint.PropertyChanged -= OnLinePointPropertyChanged;
                    RemoveLineDrawing.Invoke(linePoint);
                }
            }

            InitLayer();
            AddLayerLoadFile();

            int maxLayerIndex = entityObjects.Select(x => x.Layer.Name).Distinct().Count();
            List<string> distinctNames = entityObjects.Select(x => x.Layer.Name).Distinct().ToList();

            int lineIndex = 0;
            int layerIndex = 0;

            int i = 0;
            lineIndex = 0;

            for (i = 0; i < entityObjects.Count; i++)
            {
                var entity = entityObjects[i];

                switch (entity.Type)
                {
                    case EntityType.Line:
                        SetLayerLine(entity as Line, ref layerIndex, ref lineIndex, distinctNames);
                        break;

                    case EntityType.Point:
                        SetLayerPoint(entity as Point, ref layerIndex, ref lineIndex, distinctNames);
                        break;

                    case EntityType.Insert:
                        SetInsertObject(entity as Insert);
                        break;
                }
            }

            SetAllLayersEvents();

            OnPropertyChanged(nameof(CurrentLayer));

            IsLoadingFile = false;
        }


        private void SetInsertObject(Insert insert)
        {
            if (insert == null)
                return;

            int maxLayerIndex = insert.Block.Entities.Select(x => x.Layer.Name).Distinct().Count();
            List<string> insertDistinctNames = insert.Block.Entities.Select(x => x.Layer.Name).Distinct().ToList();

            int lineIndex = 0;
            int layerIndex = 0;

            foreach (var entity in insert.Block.Entities)
            {
                if (entity.Type == EntityType.Line)
                {
                    SetLayerLine(entity as Line, ref layerIndex, ref lineIndex, insertDistinctNames);
                }
                else if (entity.Type == EntityType.Point)
                {
                    SetLayerPoint(entity as Point, ref layerIndex, ref lineIndex, insertDistinctNames);
                }
                else if (entity.Type == EntityType.Insert)
                {
                    SetInsertObject(entity as Insert);
                }
                else if (entity.Type == EntityType.Circle)
                {
                    Console.WriteLine("EntityCircle Lost!");
                }
                else if (entity.Type == EntityType.Arc)
                {
                    Console.WriteLine("EntityArc Lost!");
                }
                else
                {

                }
            }
        }

        private void OnLayersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (LineLayer newLineLayer in e.NewItems)
                {
                    _setLayerEvents(newLineLayer);
                }
            }
        }

        private ObservableCollection<LineLayer> _layers;
        public ObservableCollection<LineLayer> Layers
        {
            get => _layers;
            set
            {
                if (_layers != value)
                {
                    _layers = value;
                    OnPropertyChanged(nameof(Layers));
                }
            }
        }


        public LineLayer LastLayer { get; set; }

        private LineLayer _currentLayer;
        public LineLayer CurrentLayer
        {
            get
            {
                if (_currentLayer == null)
                {
                    if (Layers.Count != 0)
                        _currentLayer = Layers[0];
                    else
                        return null;
                }

                return _currentLayer;
            }
            set
            {
                if (_currentLayer != value)
                {
                    if (_currentLayer == null)
                        _currentLayer = Layers[0];

                    LastLayer = _currentLayer;
                    LastLayer.IsActive = false;

                    _currentLayer = value;

                    if (_currentLayer != null)
                        _currentLayer.IsActive = true;

                    OnPropertyChanged(nameof(CurrentLayer));
                }
            }
        }

        private bool _isEnableSelected;
        public bool IsEnableSelected
        {
            get => _isEnableSelected;
            set
            {
                if (_isEnableSelected != value)
                {
                    _isEnableSelected = value;
                    OnPropertyChanged(nameof(IsEnableSelected));
                }
            }
        }

        public Array EDrawingModeItems { get; } = Enum.GetValues(typeof(EDrawingMode));

        public Array EMultiLinesDirection { get; } = Enum.GetValues(typeof(EDirection));

        public Array EMultiLinesOrientation { get; } = Enum.GetValues(typeof(EOrientation));

        private EDrawingMode _drawingMode;
        public EDrawingMode DrawingMode
        {
            get => _drawingMode;
            set
            {
                if (_drawingMode != value)
                {
                    _drawingMode = value;
                    OnPropertyChanged(nameof(DrawingMode));
                }
            }
        }

        private EDirection _multiLinesDirection;
        public EDirection MultiLinesDirection
        {
            get => _multiLinesDirection;
            set
            {
                if (_multiLinesDirection != value)
                {
                    _multiLinesDirection = value;
                    OnPropertyChanged(nameof(MultiLinesDirection));
                }
            }
        }

        private EOrientation _multiLinesOrientation;
        public EOrientation MultiLinesOrientation
        {
            get => _multiLinesOrientation;
            set
            {
                if (_multiLinesOrientation != value)
                {
                    _multiLinesOrientation = value;
                    OnPropertyChanged(nameof(MultiLinesOrientation));
                }
            }
        }

        private SolidColorBrush _selectedSolidColorBrush = Brushes.Yellow;
        public SolidColorBrush SelectedSolidColorBrush
        {
            get => _selectedSolidColorBrush;
            set
            {
                if (_selectedSolidColorBrush != value)
                {
                    _selectedSolidColorBrush = value;
                    OnPropertyChanged(nameof(SelectedSolidColorBrush));
                }
            }
        }

        private SolidColorBrush _tempSelectedSolidColorBrush = Brushes.Yellow;
        public SolidColorBrush TempSelectedSolidColorBrush
        {
            get => _tempSelectedSolidColorBrush;
            set
            {
                if (_tempSelectedSolidColorBrush != value)
                {
                    _tempSelectedSolidColorBrush = value;
                    OnPropertyChanged(nameof(TempSelectedSolidColorBrush));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
