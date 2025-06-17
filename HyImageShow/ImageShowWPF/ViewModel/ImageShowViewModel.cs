using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HyImageShow
{
    public class ImageShowViewModel : INotifyPropertyChanged
    {
        public ImageShowViewModel()
        {
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
                // 先載入成 BitmapImage（做為來源）
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // 轉成 WriteableBitmap 以供後續像素修改
                var writable = new WriteableBitmap(bitmap);
                ImageSource = writable;

                MessageBox.Show("Load Success!");
            }
            catch
            {
                ImageSource = null;

                MessageBox.Show("Load Fail!");
            }
        }
        #endregion


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
