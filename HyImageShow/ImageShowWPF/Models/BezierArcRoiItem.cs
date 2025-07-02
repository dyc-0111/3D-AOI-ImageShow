using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 貝塞爾弧線ROI項目
    /// </summary>
    public class BezierArcRoiItem : BaseItem
    {
        private Point startPoint;
        private Point middlePoint;
        private Point endPoint;
        private Path path;
        private Ellipse[] controlDots;
        private Border labelBorder;
        private bool isCompleted;
        private double arcLength;
        private double chordLength;
        private Point center;

        public BezierArcRoiItem()
        {
            controlDots = new Ellipse[3];
            isCompleted = false;
        }

        /// <summary>
        /// 起點
        /// </summary>
        public Point StartPoint
        {
            get => startPoint;
            set
            {
                startPoint = value;
                OnPropertyChanged(nameof(StartPoint));
                OnPropertyChanged(nameof(CenterPoint));
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(SimpleDisplayText));
                OnPropertyChanged(nameof(ChordLength));
                OnPropertyChanged(nameof(Point1Text));
                OnPropertyChanged(nameof(Point2Text));
                OnPropertyChanged(nameof(SizeText));
                UpdateCenter();
            }
        }

        /// <summary>
        /// 中點（控制點）
        /// </summary>
        public Point MiddlePoint
        {
            get => middlePoint;
            set
            {
                middlePoint = value;
                OnPropertyChanged(nameof(MiddlePoint));
                OnPropertyChanged(nameof(CenterPoint));
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(SimpleDisplayText));
                OnPropertyChanged(nameof(ChordLength));
                OnPropertyChanged(nameof(Point1Text));
                OnPropertyChanged(nameof(Point2Text));
                OnPropertyChanged(nameof(SizeText));
                UpdateCenter();
            }
        }

        /// <summary>
        /// 終點
        /// </summary>
        public Point EndPoint
        {
            get => endPoint;
            set
            {
                endPoint = value;
                OnPropertyChanged(nameof(EndPoint));
                OnPropertyChanged(nameof(CenterPoint));
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(SimpleDisplayText));
                OnPropertyChanged(nameof(ChordLength));
                OnPropertyChanged(nameof(Point1Text));
                OnPropertyChanged(nameof(Point2Text));
                OnPropertyChanged(nameof(SizeText));
                UpdateCenter();
            }
        }

        /// <summary>
        /// 路徑元素
        /// </summary>
        public Path Path
        {
            get => path;
            set => path = value;
        }

        /// <summary>
        /// 控制點陣列
        /// </summary>
        public Ellipse[] ControlDots
        {
            get => controlDots;
            set => controlDots = value;
        }

        /// <summary>
        /// 標籤邊框
        /// </summary>
        public Border LabelBorder
        {
            get => labelBorder;
            set => labelBorder = value;
        }

        /// <summary>
        /// 起點文字
        /// </summary>
        public override string Point1Text => $"起點: {FormatPointText(StartPoint)}";

        /// <summary>
        /// 終點文字
        /// </summary>
        public override string Point2Text => $"終點: {FormatPointText(EndPoint)}";

        /// <summary>
        /// 弧長文字
        /// </summary>
        public override string Point3Text => $"弧長: {FormatValueText(ArcLength)}";

        /// <summary>
        /// 弦長文字
        /// </summary>
        public override string SizeText => $"弦長: {FormatValueText(ChordLength)}";

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted
        {
            get => isCompleted;
            set
            {
                isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        /// <summary>
        /// 弧長
        /// </summary>
        public double ArcLength
        {
            get => arcLength;
            set
            {
                arcLength = value;
                OnPropertyChanged(nameof(ArcLength));
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(SimpleDisplayText));
            }
        }

        /// <summary>
        /// 弦長
        /// </summary>
        public double ChordLength => CalculateDistance(StartPoint, EndPoint);

        /// <summary>
        /// 中心點
        /// </summary>
        public Point CenterPoint
        {
            get => center;
            set
            {
                center = value;
                OnPropertyChanged(nameof(CenterPoint));
            }
        }

        // BaseItem 抽象成員實作
        public override string ItemType => "BezierArc";

        public override Point Center => CalculateMidPoint(StartPoint, EndPoint);

        public override Size Size => new Size(ChordLength, ArcLength);

        public override double Angle => CalculateAngle(StartPoint, EndPoint);

        public override string DisplayText => $"起點: {FormatPointText(StartPoint)} 終點: {FormatPointText(EndPoint)} 弧長: {FormatValueText(ArcLength)}";

        public override string SimpleDisplayText => $"{FormatPointText(StartPoint)} → {FormatPointText(EndPoint)} ({FormatValueText(ArcLength)})";

        public override void ClearUIElements()
        {
            Path = null;
            ControlDots = new Ellipse[3];
            LabelBorder = null;
        }

        public override void ResetDragState()
        {
            // 貝塞爾弧線沒有特定的拖曳狀態需要重置
        }

        public override void CalculateProperties()
        {
            if (IsCompleted)
            {
                CalculateArcLength();
            }
        }

        /// <summary>
        /// 更新中心點
        /// </summary>
        private void UpdateCenter()
        {
            if (IsCompleted)
            {
                // 計算三點的中心
                CenterPoint = new Point(
                    (StartPoint.X + MiddlePoint.X + EndPoint.X) / 3,
                    (StartPoint.Y + MiddlePoint.Y + EndPoint.Y) / 3
                );
            }
        }

        /// <summary>
        /// 計算弧長
        /// </summary>
        public void CalculateArcLength()
        {
            if (IsCompleted)
            {
                ArcLength = CalcQuadraticBezierLength(StartPoint, MiddlePoint, EndPoint);
                // ChordLength 現在是唯讀計算屬性，不需要手動賦值
            }
        }

        /// <summary>
        /// 計算二次貝塞爾曲線長度
        /// </summary>
        private double CalcQuadraticBezierLength(Point p0, Point p1, Point p2, int steps = 32)
        {
            double length = 0;
            Point prev = p0;
            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                Point pt = GetQuadraticBezierPoint(p0, p1, p2, t);
                length += CalculateDistance(prev, pt);
                prev = pt;
            }
            return length;
        }

        /// <summary>
        /// 獲取二次貝塞爾曲線上的點
        /// </summary>
        private Point GetQuadraticBezierPoint(Point p0, Point p1, Point p2, double t)
        {
            double x = (1 - t) * (1 - t) * p0.X + 2 * (1 - t) * t * p1.X + t * t * p2.X;
            double y = (1 - t) * (1 - t) * p0.Y + 2 * (1 - t) * t * p1.Y + t * t * p2.Y;
            return new Point(x, y);
        }

    }
} 