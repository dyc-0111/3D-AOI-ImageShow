﻿#pragma checksum "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "DFDE8F65FF28194F2F2A4F6A83E23916B70196D560FC1748626C2456B3D9E902"
//------------------------------------------------------------------------------
// <auto-generated>
//     這段程式碼是由工具產生的。
//     執行階段版本:4.0.30319.42000
//
//     對這個檔案所做的變更可能會造成錯誤的行為，而且如果重新產生程式碼，
//     變更將會遺失。
// </auto-generated>
//------------------------------------------------------------------------------

using HyImageShow;
using Hyperbrid.UIX.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace HyImageShow {
    
    
    /// <summary>
    /// GluePathEditorView
    /// </summary>
    public partial class GluePathEditorView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 16 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid GridGluePathEditorView;
        
        #line default
        #line hidden
        
        
        #line 60 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button PauseResumeButton;
        
        #line default
        #line hidden
        
        
        #line 83 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider AnimationSlider;
        
        #line default
        #line hidden
        
        
        #line 119 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListView LayerList;
        
        #line default
        #line hidden
        
        
        #line 280 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DataGrid DGPathLineEditter;
        
        #line default
        #line hidden
        
        
        #line 350 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid GluePathCanvasGrid;
        
        #line default
        #line hidden
        
        
        #line 356 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Canvas GluePathCanvas;
        
        #line default
        #line hidden
        
        
        #line 373 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Media.ScaleTransform GluePathCanvasScaleTransform;
        
        #line default
        #line hidden
        
        
        #line 374 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Media.TranslateTransform GluePathCanvasTranslateTransform;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/HyImageShow;component/gluepathwpf/view/gluepatheditorview.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.GridGluePathEditorView = ((System.Windows.Controls.Grid)(target));
            return;
            case 2:
            
            #line 43 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SelectImageButton_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.PauseResumeButton = ((System.Windows.Controls.Button)(target));
            return;
            case 4:
            this.AnimationSlider = ((System.Windows.Controls.Slider)(target));
            
            #line 92 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.AnimationSlider.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.AnimationSlider_PreviewMouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 93 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.AnimationSlider.PreviewMouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.AnimationSlider_PreviewMouseLeftButtonUp);
            
            #line default
            #line hidden
            return;
            case 5:
            this.LayerList = ((System.Windows.Controls.ListView)(target));
            
            #line 123 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.LayerList.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.LayerList_SelectionChanged);
            
            #line default
            #line hidden
            
            #line 124 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.LayerList.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.LayerList_PreviewMouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 125 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.LayerList.PreviewMouseMove += new System.Windows.Input.MouseEventHandler(this.LayerList_PreviewMouseMove);
            
            #line default
            #line hidden
            
            #line 127 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.LayerList.Drop += new System.Windows.DragEventHandler(this.LayerList_Drop);
            
            #line default
            #line hidden
            return;
            case 6:
            
            #line 186 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SaveDxfButton_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            
            #line 187 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.LoadDxfButton_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            
            #line 269 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            ((System.Windows.Controls.Border)(target)).MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.OpenColorPickerPanel_MouseLeftButtonDown);
            
            #line default
            #line hidden
            return;
            case 9:
            this.DGPathLineEditter = ((System.Windows.Controls.DataGrid)(target));
            
            #line 286 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.DGPathLineEditter.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.GluePathEditorView_KeyDown);
            
            #line default
            #line hidden
            
            #line 289 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.DGPathLineEditter.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.DGPathLineEditter_SelectionChanged);
            
            #line default
            #line hidden
            
            #line 290 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.DGPathLineEditter.PreviewMouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.DGPathLineEditter_PreviewMouseRightButtonDown);
            
            #line default
            #line hidden
            return;
            case 10:
            
            #line 347 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.FitButton_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.GluePathCanvasGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 12:
            this.GluePathCanvas = ((System.Windows.Controls.Canvas)(target));
            
            #line 357 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.GluePathCanvas.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.GluePathCanvas_MouseWheel);
            
            #line default
            #line hidden
            
            #line 358 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.GluePathCanvas.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.Canvas_MouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 359 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.GluePathCanvas.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.Canvas_MouseLeftButtonUp);
            
            #line default
            #line hidden
            
            #line 360 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.GluePathCanvas.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.Canvas_MouseDown);
            
            #line default
            #line hidden
            
            #line 361 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.GluePathCanvas.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.Canvas_MouseUp);
            
            #line default
            #line hidden
            
            #line 362 "..\..\..\..\GluePathWPF\View\GluePathEditorView.xaml"
            this.GluePathCanvas.MouseMove += new System.Windows.Input.MouseEventHandler(this.Canvas_MouseMove);
            
            #line default
            #line hidden
            return;
            case 13:
            this.GluePathCanvasScaleTransform = ((System.Windows.Media.ScaleTransform)(target));
            return;
            case 14:
            this.GluePathCanvasTranslateTransform = ((System.Windows.Media.TranslateTransform)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

