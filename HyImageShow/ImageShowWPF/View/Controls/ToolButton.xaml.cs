using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HyImageShow.ImageShowWPF.View.Controls
{
    /// <summary>
    /// 按鈕類型
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// 功能按鈕（檔案、檢視操作）
        /// </summary>
        Function,
        /// <summary>
        /// 工具按鈕（ROI 工具，有啟用狀態）
        /// </summary>
        Tool
    }

    /// <summary>
    /// 自定義工具按鈕控制項，支援功能按鈕和工具按鈕兩種模式
    /// </summary>
    public partial class ToolButton : UserControl
    {
        /// <summary>
        /// ButtonType 依賴屬性
        /// </summary>
        public static readonly DependencyProperty ButtonTypeProperty =
            DependencyProperty.Register(
                "ButtonType",
                typeof(ButtonType),
                typeof(ToolButton),
                new PropertyMetadata(ButtonType.Function));

        /// <summary>
        /// IsActive 依賴屬性
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                "IsActive",
                typeof(bool),
                typeof(ToolButton),
                new PropertyMetadata(false));

        /// <summary>
        /// Command 依賴屬性
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(ToolButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Content 依賴屬性
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                "Content",
                typeof(object),
                typeof(ToolButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Width 依賴屬性
        /// </summary>
        public static readonly DependencyProperty ButtonWidthProperty =
            DependencyProperty.Register(
                "ButtonWidth",
                typeof(double),
                typeof(ToolButton),
                new PropertyMetadata(double.NaN));

        public ToolButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 按鈕類型
        /// </summary>
        public ButtonType ButtonType
        {
            get { return (ButtonType)GetValue(ButtonTypeProperty); }
            set { SetValue(ButtonTypeProperty, value); }
        }

        /// <summary>
        /// 是否為啟用狀態（僅工具按鈕有效）
        /// </summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        /// <summary>
        /// 按鈕命令
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// 按鈕內容
        /// </summary>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// 按鈕寬度
        /// </summary>
        public double ButtonWidth
        {
            get { return (double)GetValue(ButtonWidthProperty); }
            set { SetValue(ButtonWidthProperty, value); }
        }
    }
} 