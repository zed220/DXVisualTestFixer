using DevExpress.Xpf.Bars.Native;
using DXVisualTestFixer.UI.Controls.Native;
using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
    public class ImageScalingControl : Control {
        public static readonly DependencyProperty ViewModeProperty;
        public static readonly DependencyProperty MergedViewTypeProperty;
        public static readonly DependencyProperty IsPerfectPixelProperty;
        public static readonly DependencyProperty ShowGridLinesProperty;
        public static readonly DependencyProperty ScrollModeProperty;
        public static readonly DependencyProperty TestInfoModelProperty;
        public static readonly DependencyProperty ImageScalingControlProperty;

        public static readonly DependencyProperty ScrollViewerTypeProperty;

        static ImageScalingControl() {
            Type ownerType = typeof(ImageScalingControl);
            TestInfoModelProperty = DependencyProperty.Register("TestInfoModel", typeof(TestInfoModel), ownerType, new PropertyMetadata(null));
            ViewModeProperty = DependencyProperty.Register("ViewMode", typeof(TestViewType), ownerType, new PropertyMetadata(TestViewType.Split));
            MergedViewTypeProperty = DependencyProperty.Register("MergedViewType", typeof(MergedTestViewType), ownerType, new PropertyMetadata(MergedTestViewType.Before));
            IsPerfectPixelProperty = DependencyProperty.Register("IsPerfectPixel", typeof(bool), ownerType, new PropertyMetadata(false, OnIsPerfectPixelChanged));
            ShowGridLinesProperty = DependencyProperty.Register("ShowGridLines", typeof(bool), ownerType, new PropertyMetadata(false, OnShowGridLinesChanged));
            ScrollModeProperty = DependencyProperty.Register("ScrollMode", typeof(ScrollMode), ownerType, new PropertyMetadata(ScrollMode.Draggable));
            ImageScalingControlProperty = DependencyProperty.RegisterAttached("ImageScalingControl", ownerType, ownerType, new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

            ScrollViewerTypeProperty = DependencyProperty.RegisterAttached("ScrollViewerType", typeof(MergedTestViewType), ownerType, new FrameworkPropertyMetadata(MergedTestViewType.Before, FrameworkPropertyMetadataOptions.Inherits));

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
        }

        static void OnIsPerfectPixelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ImageScalingControl)d).OnIsPerfectPixelChanged();
        }
        static void OnShowGridLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ImageScalingControl)d).OnShowGridLinesChanged();
        }

        public static ImageScalingControl GetImageScalingControl(DependencyObject obj) {
            return (ImageScalingControl)obj.GetValue(ImageScalingControlProperty);
        }
        public static void SetImageScalingControl(DependencyObject obj, ImageScalingControl value) {
            obj.SetValue(ImageScalingControlProperty, value);
        }
        public static MergedTestViewType GetScrollViewerType(DependencyObject obj) {
            return (MergedTestViewType)obj.GetValue(ScrollViewerTypeProperty);
        }
        public static void SetScrollViewerType(DependencyObject obj, MergedTestViewType value) {
            obj.SetValue(ScrollViewerTypeProperty, value);
        }

        public TestInfoModel TestInfoModel {
            get { return (TestInfoModel)GetValue(TestInfoModelProperty); }
            set { SetValue(TestInfoModelProperty, value); }
        }
        public TestViewType ViewMode {
            get { return (TestViewType)GetValue(ViewModeProperty); }
            set { SetValue(ViewModeProperty, value); }
        }
        public MergedTestViewType MergedViewType {
            get { return (MergedTestViewType)GetValue(MergedViewTypeProperty); }
            set { SetValue(MergedViewTypeProperty, value); }
        }
        public bool IsPerfectPixel {
            get { return (bool)GetValue(IsPerfectPixelProperty); }
            set { SetValue(IsPerfectPixelProperty, value); }
        }
        public bool ShowGridLines {
            get { return (bool)GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }
        public ScrollMode ScrollMode {
            get { return (ScrollMode)GetValue(ScrollModeProperty); }
            set { SetValue(ScrollModeProperty, value); }
        }

        ScrollViewerScrollSynchronizer scrollViewerScrollSynchronizer = new ScrollViewerScrollSynchronizer();
        ImageScaleSynchronizer imageScaleSynchronizer = new ImageScaleSynchronizer();

        public void StartTrackingScrollViewer(DraggableScrollViewer scrollViewer, MergedTestViewType scrollViewerType) {
            scrollViewerScrollSynchronizer.Register(scrollViewerType, scrollViewer);
            imageScaleSynchronizer.Register(scrollViewerType, scrollViewer);
        }

        public void ChangeView(bool reverse) {
            MergedViewType = GetNextMergedTestViewType(reverse);
        }
        public void ZoomIn() {
            imageScaleSynchronizer.ZoomIn();
        }
        public void ZoomOut() {
            imageScaleSynchronizer.ZoomOut();
        }
        public void Zoom100() {
            imageScaleSynchronizer.Zoom100();
        }

        void OnIsPerfectPixelChanged() {
            imageScaleSynchronizer.IsPerfectPixel = IsPerfectPixel;
        }
        void OnShowGridLinesChanged() {
            imageScaleSynchronizer.ShowGridLines = ShowGridLines;
        }
        MergedTestViewType GetNextMergedTestViewType(bool reverse) {
            switch(MergedViewType) {
                case MergedTestViewType.Diff:
                    return reverse ? MergedTestViewType.Current : MergedTestViewType.Before;
                case MergedTestViewType.Before:
                    return reverse ? MergedTestViewType.Diff : MergedTestViewType.Current;
                case MergedTestViewType.Current:
                    return reverse ? MergedTestViewType.Before : MergedTestViewType.Diff;
                default:
                    return MergedViewType;
            }
        }
    }

    public class ImageScalingControlTemplateSelector : DataTemplateSelector {
        public DataTemplate SplitTemplate { get; set; }
        public DataTemplate MergedTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            switch((TestViewType)item) {
                case TestViewType.Split:
                    return SplitTemplate;
                case TestViewType.Merged:
                    return MergedTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }

    public enum ScrollMode { Legacy, Draggable }
    public enum TestViewType { Split, Merged }
    public enum MergedTestViewType {
        Diff,
        Before,
        Current,
    }
}
