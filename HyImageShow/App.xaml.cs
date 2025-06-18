using HyImageShow.ImageShow;
using Hyperbrid.UIX.Tools.Extension;
using Hyperbrid.UIX.WinForms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

            var mainWindow = new Window
            {
                Title = "Main",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            var view = new MainImageShow();
            view.DataContext = new ImageShowViewModel();

            mainWindow.Content = view;
            mainWindow.Show();
        }
    }
}
