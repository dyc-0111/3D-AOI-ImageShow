using HyImageShow.ImageShowWPF.View;
using HyImageShow.ImageShowWPF.ViewModels;
using HyImageShow.ImageShowWPF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            mainImageShow.DataContext = ImageShowViewModel.CreateDefaultServices();

            var roiDataList = new List<RoiData>
            {
                new RoiData
                {
                    Type = RoiType.RotRect,
                    CenterX = 300,
                    CenterY = 200,
                    Width = 120,
                    Height = 80,
                    Angle = 15
                },
                new RoiData
                {
                    Type = RoiType.Polygon,
                    Points = new List<Point> { new Point(100,100), new Point(200,120), new Point(180,200), new Point(120,180) },
                    Angle = 0
                },
                new RoiData
                {
                    Type = RoiType.Ellipse,
                    CenterX = 500,
                    CenterY = 300,
                    Width = 100,
                    Height = 100
                },
                new RoiData
                {
                    Type = RoiType.Line,
                    Points = new List<Point> { new Point(400, 400), new Point(600, 420) }
                },
                new RoiData
                {
                    Type = RoiType.Ruler,
                    Points = new List<Point> { new Point(450, 400), new Point(600, 420) }
                },
                new RoiData
                {
                    Type = RoiType.BezierArc,
                    CenterX = 600,
                    CenterY = 200,
                    Width = 120,
                    Height = 120,
                    Angle = 0,
                    Points = new List<Point> { new Point(660, 200), new Point(600, 260), new Point(540, 200) }
                },
                new RoiData
                {
                    Type = RoiType.CircularArc,
                    CenterX = 600,
                    CenterY = 200,
                    Width = 120,
                    Height = 120,
                    Angle = 0,
                    Points = new List<Point> { new Point(60, 200), new Point(60, 260), new Point(540, 200) }
                }
            };
            mainImageShow.SetAllRoiData(roiDataList);

            var result = mainImageShow.GetAllRoiData();
        }
    }
}
