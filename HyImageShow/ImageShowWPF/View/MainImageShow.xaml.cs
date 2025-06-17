using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShow
{
    /// <summary>
    /// ImageShow.xaml 的互動邏輯
    /// </summary>
    public partial class MainImageShow : UserControl
    {
        public MainImageShow()
        {
            InitializeComponent();
        }

        public ImageShowViewModel ViewModel => DataContext as ImageShowViewModel;

        private bool isDraggingCanvas = false;
        private Point lastMousePosition;

        private bool showCrossLines = false;

        #region Fit
        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            Fit();
        }

        private void Fit()
        {
            // 重置縮放和位移
            MainCanvasScaleTransform.ScaleX = 1;
            MainCanvasScaleTransform.ScaleY = 1;
            MainCanvasTranslateTransform.X = 0;
            MainCanvasTranslateTransform.Y = 0;

            // 置中
            MainCanvas.UpdateLayout();
        }
        #endregion

        private void ShowCrossLinesButton_Click(object sender, RoutedEventArgs e)
        {
            showCrossLines = !showCrossLines;
            

        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? 1.1 : 0.9;
            Point mousePosition = e.GetPosition(MainCanvasGrid); // 获取鼠标相对 DragCanvas 的坐标

            Zoom(zoomFactor, mousePosition);
        }

        private void Zoom(double zoomFactor, Point mousePosition)
        {
            // 计算缩放前鼠标的相对位置
            double prevX = (mousePosition.X - MainCanvasTranslateTransform.X) / MainCanvasScaleTransform.ScaleX;
            double prevY = (mousePosition.Y - MainCanvasTranslateTransform.Y) / MainCanvasScaleTransform.ScaleY;

            // 执行缩放
            double newScaleX = MainCanvasScaleTransform.ScaleX * zoomFactor;
            double newScaleY = MainCanvasScaleTransform.ScaleY * zoomFactor;

            // 限制缩放比例在合理范围内
            if (newScaleX < 0.01) newScaleX = 0.01;
            if (newScaleX > 100) newScaleX = 100;
            if (newScaleY < 0.01) newScaleY = 0.01;
            if (newScaleY > 100) newScaleY = 100;

            MainCanvasScaleTransform.ScaleX = newScaleX;
            MainCanvasScaleTransform.ScaleY = newScaleY;

            // 计算缩放后鼠标的相对位置
            double newX = prevX * MainCanvasScaleTransform.ScaleX + MainCanvasTranslateTransform.X;
            double newY = prevY * MainCanvasScaleTransform.ScaleY + MainCanvasTranslateTransform.Y;

            // 通过 TranslateTransform 使缩放后的鼠标位置与缩放前一致
            MainCanvasTranslateTransform.X -= (newX - mousePosition.X);
            MainCanvasTranslateTransform.Y -= (newY - mousePosition.Y);
        }



        #region Canvas Left Mouse
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
        #endregion

        #region Canvas Mouse
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = true;
                lastMousePosition = e.GetPosition(MainCanvasGrid);

                MainCanvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingCanvas)
            {
                var currentPosition = e.GetPosition(MainCanvasGrid);
                var offset = currentPosition - lastMousePosition;

                MainCanvasTranslateTransform.X += offset.X;
                MainCanvasTranslateTransform.Y += offset.Y;

                lastMousePosition = currentPosition;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                isDraggingCanvas = false;
            }
        }

        #endregion

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "圖片檔案 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                ViewModel?.LoadImage(dialog.FileName);

                Fit();
            }
        }
    }
}
