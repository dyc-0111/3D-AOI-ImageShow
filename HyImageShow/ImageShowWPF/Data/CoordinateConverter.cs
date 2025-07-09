using System;
using System.Windows;
using System.Windows.Media;

namespace HyImageShow.ImageShowWPF.Data
{
    public class CoordinateConverter
    {
        private readonly System.Windows.Media.Imaging.BitmapSource _imageSource;
        private readonly double _displayWidth;
        private readonly double _displayHeight;
        private readonly double _canvasScaleX;
        private readonly double _canvasScaleY;
        private readonly double _canvasTranslateX;
        private readonly double _canvasTranslateY;

        private double _drawWidth;
        private double _drawHeight;
        private double _offsetX;
        private double _offsetY;
        private double _scaleX;
        private double _scaleY;

        public CoordinateConverter(System.Windows.Media.Imaging.BitmapSource imageSource, double displayWidth, double displayHeight, ScaleTransform scaleTransform, TranslateTransform translateTransform)
        {
            _imageSource = imageSource;
            _displayWidth = displayWidth;
            _displayHeight = displayHeight;
            _canvasScaleX = scaleTransform?.ScaleX ?? 1.0;
            _canvasScaleY = scaleTransform?.ScaleY ?? 1.0;
            _canvasTranslateX = translateTransform?.X ?? 0.0;
            _canvasTranslateY = translateTransform?.Y ?? 0.0;
            Init();
        }

        private void Init()
        {
            double originalWidth = _imageSource.PixelWidth;
            double originalHeight = _imageSource.PixelHeight;
            double imgAspect = originalWidth / originalHeight;
            double ctrlAspect = _displayWidth / _displayHeight;
            if (imgAspect > ctrlAspect)
            {
                _drawWidth = _displayWidth;
                _drawHeight = _displayWidth / imgAspect;
                _offsetX = 0;
                _offsetY = (_displayHeight - _drawHeight) / 2;
            }
            else
            {
                _drawHeight = _displayHeight;
                _drawWidth = _displayHeight * imgAspect;
                _offsetX = (_displayWidth - _drawWidth) / 2;
                _offsetY = 0;
            }
            _scaleX = originalWidth / _drawWidth;
            _scaleY = originalHeight / _drawHeight;
        }

        // Canvas/顯示座標 → 原圖座標
        public Point ToImage(Point displayPoint)
        {
            // 先扣掉 Canvas 的平移
            double x = displayPoint.X - _canvasTranslateX;
            double y = displayPoint.Y - _canvasTranslateY;
            // 再除以 Canvas 的縮放
            x /= _canvasScaleX;
            y /= _canvasScaleY;
            // 再扣掉圖片在 MainImage 內的 offset
            x -= _offsetX;
            y -= _offsetY;
            // 轉換到原圖座標
            double imageX = x * _scaleX;
            double imageY = y * _scaleY;
            // 限制範圍
            imageX = Math.Max(0, Math.Min(_imageSource.PixelWidth, imageX));
            imageY = Math.Max(0, Math.Min(_imageSource.PixelHeight, imageY));
            return new Point(imageX, imageY);
        }

        // Canvas/顯示長度 → 原圖長度（只考慮縮放，不考慮 offset）
        public double ToImageLength(double displayLength, bool isX = true)
        {
            double scale = isX ? _scaleX : _scaleY;
            double canvasScale = isX ? _canvasScaleX : _canvasScaleY;
            return displayLength / canvasScale * scale;
        }
    }
} 