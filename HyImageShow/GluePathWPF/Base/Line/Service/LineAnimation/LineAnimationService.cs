using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HyImageShow
{
    public class LineAnimationService : ILineAnimationService, INotifyPropertyChanged
    {
        private readonly Queue<Storyboard> _lineAnimations = new Queue<Storyboard>();
        private Storyboard _currentStoryboard;
        private bool _isPaused = false;
        private Button _pauseResumeButton;

        private readonly SolidColorBrush _playedColorBrush = new SolidColorBrush(Colors.Orange);
        public bool IsPaused => _isPaused;

        public double Speed { get; set; } = 1.0; // Default speed

        private double durationInSeconds => 1 / Speed;

        private List<Line> PlayedLines = new List<Line>();

        private Func<SolidColorBrush> GetOriginColorBrush { get; set; }

        private double currentProgress = 0.0;
        public double CurrentProgress
        {
            get => currentProgress;
            set
            {
                if (currentProgress != value)
                {
                    currentProgress = value;

                    OnPropertyChanged(nameof(CurrentProgress));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LineAnimationService(
            Button pauseResumeButton, 
            Func<SolidColorBrush> getOriginColorBrush)
        {
            _pauseResumeButton = pauseResumeButton;
            GetOriginColorBrush = getOriginColorBrush;
        }


        private bool playFromBeginning = false;

        public void Start(IEnumerable<LinePoint> lines, Dictionary<LinePoint, Line> lineDictionary)
        {
            if (_currentStoryboard != null)
            {
                _currentStoryboard.Stop();
                _currentStoryboard = null;
            }

            _isPaused = false;
            _pauseResumeButton.Content = "Pause";
            _lineAnimations.Clear();
            ClearLineAnimationControl();

            playFromBeginning = true;

            PlayedLines = new List<Line>();

            foreach (var linePoint in lines.OrderBy(l => l.LineIndex))
            {
                if (lineDictionary.ContainsKey(linePoint))
                {
                    var items = DrawPath.CreateLineAnimation(
                        lineDictionary[linePoint],
                        new Point(linePoint.StartX, linePoint.StartY),
                        new Point(linePoint.EndX, linePoint.EndY),
                        durationInSeconds);

                    PlayedLines.Add(items.line);

                    items.storyboard.Completed += (_, __) =>
                    {
                        PlayNextLine();
                    };


                    _lineAnimations.Enqueue(items.storyboard);
                }
            }

            PlayNextLine();
        }

        private void ResetPlayLines(IEnumerable<LinePoint> lines, Dictionary<LinePoint, Line> lineDictionary)
        {
            if (_currentStoryboard != null)
            {
                _currentStoryboard.Stop();
                _currentStoryboard = null;
            }

            _isPaused = false;
            _pauseResumeButton.Content = "Pause";
            _lineAnimations.Clear();
            ClearLineAnimationControl();

            Line line = null;
            PlayedLines = new List<Line>();

            foreach (var linePoint in lines.OrderBy(l => l.LineIndex))
            {
                if (lineDictionary.ContainsKey(linePoint))
                {
                    var items = DrawPath.CreateLineAnimation(
                        lineDictionary[linePoint],
                        new Point(linePoint.StartX, linePoint.StartY),
                        new Point(linePoint.EndX, linePoint.EndY),
                        durationInSeconds);

                    line = items.line;

                    PlayedLines.Add(line);
                }
            }
        }

        public void HoldStop()
        {
            _currentStoryboard?.Pause();
            _isPaused = true;
            playFromBeginning = false;
            _pauseResumeButton.Content = "Resume";
        }

        public void Seek(
            double progress, 
            IEnumerable<LinePoint> lines, Dictionary<LinePoint, Line> lineDictionary)
        {
            if (playFromBeginning)
                return;

            ResetPlayLines(lines, lineDictionary);

            if (progress < 0 || progress > 1 || PlayedLines.Count == 0)
                return;

            // 計算目標線條索引
            int targetIndex = (int)(progress * PlayedLines.Count);

            // 清除所有動畫
            ClearLineAnimationControl();

            // 重置動畫隊列
            _lineAnimations.Clear();

            for (int i = 0; i < targetIndex; i++)
            {
                var line = PlayedLines[i];
                line.Stroke = _playedColorBrush;
            }

            for (int i = targetIndex; i < PlayedLines.Count; i++)
            {
                var line = PlayedLines[i];
                var items = DrawPath.CreateLineAnimation(
                    line,
                    new Point(line.X1, line.Y1),
                    new Point(line.X2, line.Y2),
                    durationInSeconds);

                items.storyboard.Completed += (_, __) => PlayNextLine();
                _lineAnimations.Enqueue(items.storyboard);
            }

            // 更新進度
            CurrentProgress = progress;

            // 播放下一條線
            PlayNextLine();
        }


        public void ResumePause()
        {
            if (!_isPaused)
            {
                _currentStoryboard?.Pause();
                _isPaused = true;
                playFromBeginning = false;
                _pauseResumeButton.Content = "Resume";
            }
            else
            {
                _currentStoryboard?.Resume();
                _isPaused = false;
                playFromBeginning = false;
                _pauseResumeButton.Content = "Pause";
            }
        }

        private void PlayNextLine()
        {
            if (_lineAnimations.Count > 0)
            {
                _currentStoryboard = _lineAnimations.Dequeue();
                _currentStoryboard.Begin();

                var line = PlayedLines[PlayedLines.Count - _lineAnimations.Count - 1];
                line.Stroke = _playedColorBrush;

                // 更新進度
                CurrentProgress = (double)(PlayedLines.Count - _lineAnimations.Count) / PlayedLines.Count;

                _isPaused = false;
                _pauseResumeButton.Content = "Pause";
            }
            else
            {
                ClearLineAnimationControl();
                //CurrentProgress = 1.0; // 動畫完成
            }
        }

        private void ClearLineAnimationControl()
        {
            foreach (var line in PlayedLines)
            {
                line.BeginAnimation(Line.X1Property, null);
                line.BeginAnimation(Line.Y1Property, null);
                line.BeginAnimation(Line.X2Property, null);
                line.BeginAnimation(Line.Y2Property, null);

                line.Stroke = GetOriginColorBrush.Invoke();
            }

            playFromBeginning = false;
        }
    }
}
