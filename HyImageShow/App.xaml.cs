using System.Windows;
using HyImageShow.ImageShowWPF.Services;
using HyImageShow.ImageShowWPF.ViewModels;

namespace HyImageShow
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 簡單的服務定位器模式
            var services = CreateServices();
            var imageShowViewModel = CreateImageShowViewModel(services);
            var mainWindowViewModel = new MainWindowViewModel(imageShowViewModel);
            var mainWindow = new MainWindow(mainWindowViewModel);
            
            mainWindow.Show();
        }

        private static object[] CreateServices()
        {
            // 創建所有服務實例
            var roiManagementService = new RoiManagementService();
            var lineService = new DrawLineService();
            var lineDrawingService = new DrawLineDrawingService();
            var rulerDrawingService = new RulerDrawingService();
            var rulerService = new RulerService();
            var rotRectRoiService = new RotRectRoiService();
            var rotRectRoiDrawingService = new RotRectRoiDrawingService();
            var ellipseRoiService = new EllipseRoiService();
            var ellipseRoiDrawingService = new EllipseRoiDrawingService();
            var polygonRoiService = new PolygonRoiService();
            var polygonRoiDrawingService = new PolygonRoiDrawingService();
            var bezierArcRoiService = new BezierArcRoiService();
            var bezierArcRoiDrawingService = new BezierArcRoiDrawingService();
            var circularArcRoiService = new CircularArcRoiService();
            var circularArcRoiDrawingService = new CircularArcRoiDrawingService();
            var crossLinesService = new CrossLinesService();
            var crossLinesDrawingService = new CrossLinesDrawingService();
            var pointRoiService = new PointRoiService();
            var pointRoiDrawingService = new PointRoiDrawingService();

            return new object[]
            {
                roiManagementService,
                lineService,
                lineDrawingService,
                rulerService,
                rulerDrawingService,
                rotRectRoiService,
                rotRectRoiDrawingService,
                ellipseRoiService,
                ellipseRoiDrawingService,
                polygonRoiService,
                polygonRoiDrawingService,
                bezierArcRoiService,
                bezierArcRoiDrawingService,
                circularArcRoiService,
                circularArcRoiDrawingService,
                crossLinesService,
                crossLinesDrawingService,
                pointRoiService,
                pointRoiDrawingService
            };
        }

        private static ImageShowViewModel CreateImageShowViewModel(object[] services)
        {
            return new ImageShowViewModel(
                (RoiManagementService)services[0],
                (DrawLineService)services[1],
                (RulerService)services[3],
                (RotRectRoiService)services[5],
                (EllipseRoiService)services[7],
                (PolygonRoiService)services[9],
                (BezierArcRoiService)services[11],
                (CircularArcRoiService)services[13],
                (CrossLinesService)services[15],
                (RulerDrawingService)services[4],
                (RotRectRoiDrawingService)services[6],
                (EllipseRoiDrawingService)services[8],
                (PolygonRoiDrawingService)services[10],
                (BezierArcRoiDrawingService)services[12],
                (CircularArcRoiDrawingService)services[14],
                (CrossLinesDrawingService)services[16],
                (DrawLineDrawingService)services[2],
                (PointRoiService)services[17],
                (PointRoiDrawingService)services[18]
            );
        }
    }
}
