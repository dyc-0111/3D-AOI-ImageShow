using Hyperbrid.UIX.Interactivity;
using netDxf;
using System;
using System.ComponentModel;

namespace HyImageShow
{
    public class LinePoint : INotifyPropertyChanged
    {
        public LinePoint(
            LineLayer parentLayer,
            Vector3 start,
            Vector3 end,
            int lineIndex,
            ELinePointType type)
        {
            ParentLayer = parentLayer;

            StartXInMm = start.X;
            StartYInMm = start.Y;

            EndXInMm = end.X;
            EndYInMm = end.Y;

            LineIndex = lineIndex;

            LinePointType = type;
        }

        public LinePoint(
            double startXInMm,
            double startYInMm,
            double endXInMm,
            double endYInMm,
            int lineIndex,
            ELinePointType type)
        {
            StartXInMm = startXInMm;
            StartYInMm = startYInMm;

            EndXInMm = endXInMm;
            EndYInMm = endYInMm;

            LineIndex = lineIndex;

            LinePointType = type;
        }

        public LinePoint(LinePoint p)
        {
            CopyFrom(p);
        }

        public LinePoint()
        {

        }

        public LineLayer ParentLayer
        {
            get;
            set;
        }
        //= new LineLayer();

        private ELinePointType _linePointType = ELinePointType.Line;
        private int _lineIndex;
        private double _startX;
        private double _startY;
        private double _relativeStartX;
        private double _relativeStartY;
        private double _startXDist;
        private double _startYDist;
        private double _startXEdge;
        private double _startYEdge;
        private double _relativeEndX;
        private double _relativeEndY;
        private double _endX;
        private double _endY;
        private double _endXDist;
        private double _endYDist;
        private double _endXEdge;
        private double _endYEdge;

        private bool _editX = false;
        private bool _editY = false;

        private bool _isUpdateScale = false;

        private void OnUpdateScale()
        {
            if (_isUpdateScale)
                return;

            _isUpdateScale = true;

            ParentLayer.DoShowScale?.Invoke(this);

            _isUpdateScale = false;
        }


        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set 
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));

                    ((RelayCommand)ParentLayer.DeleteLineCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)ParentLayer.MoveLineUpCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)ParentLayer.MoveLineDownCommand).NotifyCanExecuteChanged();
                }
            }
        }



        private bool _showScale;
        public bool ShowScale 
        { 
            get => _showScale;
            set
            {
                if (_showScale != value)
                {
                    _showScale = value;
                    OnPropertyChanged(nameof(ShowScale));
                    OnUpdateScale();
                }
            }
        }

        public void CopyFrom(LinePoint p)
        {
            if (ParentLayer == null)
            {
                ParentLayer = new LineLayer(p.ParentLayer.LineLayerService);
            }
            ParentLayer.CopyFrom(p.ParentLayer);

            _lineIndex = p._lineIndex;
            _linePointType = p._linePointType;
            _startX = p._startX;
            _startY = p._startY;
            _relativeStartX = p._relativeStartX;
            _relativeStartY = p._relativeStartY;
            _startXDist = p._startXDist;
            _startYDist = p._startYDist;
            _startXEdge = p._startXEdge;
            _startYEdge = p._startYEdge;
            _relativeEndX = p._relativeEndX;
            _relativeEndY = p._relativeEndY;
            _endX = p._endX;
            _endY = p._endY;
            _endXDist = p._endXDist;
            _endYDist = p._endYDist;
            _endXEdge = p._endXEdge;
            _endYEdge = p._endYEdge;

            _editX = p._editX;
            _editY = p._editY;

            _isSelected = p._isSelected;
        }

        public void UpdateRelative(LinePoint alignLinePoint)
        {
            if ((LinePointType == ELinePointType.Line
                || LinePointType == ELinePointType.Point)
                && alignLinePoint != null)
            {
                RelativeStartX = StartX - alignLinePoint.StartX;
                RelativeStartY = StartY - alignLinePoint.StartY;
                RelativeEndX = EndX - alignLinePoint.EndX;
                RelativeEndY = EndY - alignLinePoint.EndY;
            }
        }

        public void ResetRelative()
        {
            if (LinePointType == ELinePointType.Line
                || LinePointType == ELinePointType.Point)
            {
                RelativeStartX = 0;
                RelativeStartY = 0;
                RelativeEndX = 0;
                RelativeEndY = 0;
            }
        }

        public int LineIndex
        {
            get => _lineIndex;
            set
            {
                if (_lineIndex != value)
                {
                    _lineIndex = value;
                    OnPropertyChanged(nameof(LineIndex));
                }
            }
        }

        public ELinePointType LinePointType
        {
            get => _linePointType;
            set
            {
                if (_linePointType != value)
                {
                    _linePointType = value;
                    OnPropertyChanged(nameof(LinePointType));
                }
            }
        }

        public double StartX
        {
            get => _startX;
            set
            {
                if (_startX != value)
                {
                    _startX = value;
                    OnPropertyChanged(nameof(StartX));
                    OnPropertyChanged(nameof(StartXInMm));
                    OnUpdateScale();

                    // 當類型為 Point 時，確保 End 與 Start 相同
                    if (LinePointType == ELinePointType.Point ||
                        LinePointType == ELinePointType.Align)
                    {
                        if (_editX)
                        {
                            _editX = false;
                            return;
                        }

                        _editX = true;

                        EndX = value;
                    }
                }
            }
        }

        public double StartY
        {
            get => _startY;
            set
            {
                if (_startY != value)
                {
                    _startY = value;
                    OnPropertyChanged(nameof(StartY));
                    OnPropertyChanged(nameof(StartYInMm));
                    OnUpdateScale();

                    // 當類型為 Point 時，確保 End 與 Start 相同
                    if (LinePointType == ELinePointType.Point ||
                        LinePointType == ELinePointType.Align)
                    {
                        if (_editY)
                        {
                            _editY = false;
                            return;
                        }

                        _editY = true;

                        EndY = value;
                    }
                }
            }
        }

        public double RelativeStartX
        {
            get => _relativeStartX;
            set
            {
                if (_relativeStartX != value)
                {
                    _relativeStartX = value;
                    OnPropertyChanged(nameof(RelativeStartX));
                    OnPropertyChanged(nameof(RelativeStartXInMm));
                }
            }
        }

        public double RelativeStartY
        {
            get => _relativeStartY;
            set
            {
                if (_relativeStartY != value)
                {
                    _relativeStartY = value;
                    OnPropertyChanged(nameof(RelativeStartY));
                    OnPropertyChanged(nameof(RelativeStartYInMm));
                }
            }
        }

        public double RelativeEndX
        {
            get => _relativeEndX;
            set
            {
                if (_relativeEndX != value)
                {
                    _relativeEndX = value;
                    OnPropertyChanged(nameof(RelativeEndX));
                    OnPropertyChanged(nameof(RelativeEndXInMm));
                }
            }
        }

        public double RelativeEndY
        {
            get => _relativeEndY;
            set
            {
                if (_relativeEndY != value)
                {
                    _relativeEndY = value;
                    OnPropertyChanged(nameof(RelativeEndY));
                    OnPropertyChanged(nameof(RelativeEndYInMm));
                }
            }
        }

        public double StartXDist
        {
            get => _startXDist;
            set
            {
                if (_startXDist != value)
                {
                    _startXDist = value;
                    OnPropertyChanged(nameof(StartXDist));
                }
            }
        }

        public double StartYDist
        {
            get => _startYDist;
            set
            {
                if (_startYDist != value)
                {
                    _startYDist = value;
                    OnPropertyChanged(nameof(StartYDist));
                }
            }
        }

        public double StartXEdge
        {
            get => _startXEdge;
            set
            {
                if (_startXEdge != value)
                {
                    _startXEdge = value;
                    OnPropertyChanged(nameof(StartXEdge));
                }
            }
        }

        public double StartYEdge
        {
            get => _startYEdge;
            set
            {
                if (_startYEdge != value)
                {
                    _startYEdge = value;
                    OnPropertyChanged(nameof(StartYEdge));
                }
            }
        }

        public double EndX
        {
            get => _endX;
            set
            {
                if (_endX != value)
                {
                    _endX = value;
                    OnPropertyChanged(nameof(EndX));
                    OnPropertyChanged(nameof(EndXInMm));
                    OnUpdateScale();

                    // 當類型為 Point 時，確保 End 與 Start 相同
                    if (LinePointType == ELinePointType.Point ||
                        LinePointType == ELinePointType.Align)
                    {
                        if (_editX)
                        {
                            _editX = false;
                            return;
                        }

                        _editX = true;

                        StartX = value;
                    }
                }
            }
        }

        public double EndY
        {
            get => _endY;
            set
            {
                if (_endY != value)
                {
                    _endY = value;
                    OnPropertyChanged(nameof(EndY));
                    OnPropertyChanged(nameof(EndYInMm));
                    OnUpdateScale();

                    // 當類型為 Point 時，確保 End 與 Start 相同
                    if (LinePointType == ELinePointType.Point ||
                        LinePointType == ELinePointType.Align)
                    {
                        if (_editY)
                        {
                            _editY = false;
                            return;
                        }

                        _editY = true;

                        StartY = value;
                    }
                }
            }
        }

        public double EndXDist
        {
            get => _endXDist;
            set
            {
                if (_endXDist != value)
                {
                    _endXDist = value;
                    OnPropertyChanged(nameof(EndXDist));
                }
            }
        }

        public double EndYDist
        {
            get => _endYDist;
            set
            {
                if (_endYDist != value)
                {
                    _endYDist = value;
                    OnPropertyChanged(nameof(EndYDist));
                }
            }
        }

        public double EndXEdge
        {
            get => _endXEdge;
            set
            {
                if (_endXEdge != value)
                {
                    _endXEdge = value;
                    OnPropertyChanged(nameof(EndXEdge));
                }
            }
        }

        public double EndYEdge
        {
            get => _endYEdge;
            set
            {
                if (_endYEdge != value)
                {
                    _endYEdge = value;
                    OnPropertyChanged(nameof(EndYEdge));
                }
            }
        }

        public enuEdgeType p1_XEdgeType = enuEdgeType.Unknow;
        public enuEdgeType p1_YEdgeType = enuEdgeType.Unknow;


        public double StartXInMm
        {
            get => Math.Round(ConvertPixelToMm(StartX), 3);
            set => StartX = ConvertMmToPixel(value);
        }

        public double RelativeStartXInMm
        {
            get => Math.Round(ConvertPixelToMm(RelativeStartX), 3);
            set => RelativeStartX = ConvertMmToPixel(value);
        }

        public double RelativeStartYInMm
        {
            get => Math.Round(ConvertPixelToMm(RelativeStartY), 3);
            set => RelativeStartY = ConvertMmToPixel(value);
        }

        public double RelativeEndXInMm
        {
            get => Math.Round(ConvertPixelToMm(RelativeEndX), 3);
            set => RelativeEndX = ConvertMmToPixel(value);
        }

        public double RelativeEndYInMm
        {
            get => Math.Round(ConvertPixelToMm(RelativeEndY), 3);
            set => RelativeEndY = ConvertMmToPixel(value);
        }


        public double StartYInMm
        {
            get => Math.Round(ConvertPixelToMm(StartY), 3);
            set => StartY = ConvertMmToPixel(value);
        }

        public double EndXInMm
        {
            get => Math.Round(ConvertPixelToMm(EndX), 3);
            set => EndX = ConvertMmToPixel(value);
        }

        public double EndYInMm
        {
            get => Math.Round(ConvertPixelToMm(EndY), 3);
            set => EndY = ConvertMmToPixel(value);
        }

        private double ConvertPixelToMm(double pixel)
        {
            const double pixelToMm = 1.666667 / 1000.0; // 假設每像素1.666667微米
            return pixel * pixelToMm * GetRatio();
        }

        private float MaxWidthPixel = 720f;
         
        private float GetRatio()
        {
            return 200;
            //return 5120f / MaxWidthPixel;
        }

        private double ConvertMmToPixel(double mm)
        {
            const double mmToPixel = 1000.0 / 1.666667; // 假設每像素1.666667微米
            return mm * mmToPixel / GetRatio();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
