using HyImageShow.ImageShowWPF.View;
using HyImageShow.ImageShowWPF.ViewModels;
using HyImageShow.ImageShowWPF.Data;
using HyImageShow.ImageShowWPF.Services;
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
        private IImageShowAPI _imageShowAPI;

        public MainWindow()
        {
            InitializeComponent();

            // 建立 ViewModel 和 API
            var vm = ImageShowViewModel.CreateDefaultServices();
            _imageShowAPI = new ImageShowAPI(vm);
            mainImageShow.DataContext = vm;

            InitializeImageShow();
        }

        private void InitializeImageShow()
        {
            // 建立新檔案
            _imageShowAPI.NewFile();

            // 隱藏 ROI 面板和工具列（如果您不想顯示 UI）
            mainImageShow.ToggleRoiPanelVisible(false);
            mainImageShow.ToggleToolbarVisible(false);

            // 設定一些預設的 ROI 數據
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
                }
            };
            mainImageShow.SetAllRoiData(roiDataList);
        }

    }
}
