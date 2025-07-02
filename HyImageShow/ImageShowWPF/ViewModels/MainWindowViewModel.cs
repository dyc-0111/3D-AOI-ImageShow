using System;
using System.ComponentModel;
using System.Windows;
using HyImageShow.ImageShowWPF.Commands;

namespace HyImageShow.ImageShowWPF.ViewModels
{
    /// <summary>
    /// 主視窗的ViewModel
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ImageShowViewModel _imageShowViewModel;

        public MainWindowViewModel(ImageShowViewModel imageShowViewModel)
        {
            _imageShowViewModel = imageShowViewModel ?? throw new ArgumentNullException(nameof(imageShowViewModel));
            
            // 初始化命令
            InitializeCommands();
        }

        public ImageShowViewModel ImageShowViewModel => _imageShowViewModel;

        public string WindowTitle => "3D-AOI Image Show";

        #region Commands

        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand MinimizeCommand { get; private set; }
        public RelayCommand MaximizeCommand { get; private set; }

        private void InitializeCommands()
        {
            CloseCommand = new RelayCommand(CloseWindow);
            MinimizeCommand = new RelayCommand(MinimizeWindow);
            MaximizeCommand = new RelayCommand(MaximizeWindow);
        }

        private void CloseWindow()
        {
            Application.Current.MainWindow?.Close();
        }

        private void MinimizeWindow()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow()
        {
            if (Application.Current.MainWindow != null)
            {
                var currentState = Application.Current.MainWindow.WindowState;
                Application.Current.MainWindow.WindowState = 
                    currentState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 