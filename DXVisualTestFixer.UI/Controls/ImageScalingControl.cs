using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Bars.Native;
using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
    public class ImageScalingControl : Control {

        #region Inner Classes

        class ScrollViewerScrollSynchronizer {
            DraggableScrollViewer before, current, diff;
                        
            public void Register(MergedTestViewType scrollViewerType, DraggableScrollViewer scrollViewer) {
                switch(scrollViewerType) {
                    case MergedTestViewType.Diff:
                        Unregister(diff);
                        diff = scrollViewer;
                        break;
                    case MergedTestViewType.Before:
                        Unregister(before);
                        before = scrollViewer;
                        break;
                    case MergedTestViewType.Current:
                        Unregister(current);
                        current = scrollViewer;
                        break;
                }
                scrollViewer.ScrollChanged += scrollChanged;
            }
            public void Unregister(DraggableScrollViewer scrollViewer) {
                if(scrollViewer == null)
                    return;
                scrollViewer.ScrollChanged -= scrollChanged;
            }

            void scrollChanged(object sender, ScrollChangedEventArgs e) {
                foreach(DraggableScrollViewer sv in new List<DraggableScrollViewer>() { before, current, diff }.Where(x => x != null)) {
                    sv.ScrollToHorizontalOffset(e.HorizontalOffset);
                    sv.ScrollToVerticalOffset(e.VerticalOffset);
                }
            }
        }

        #endregion

        public static readonly DependencyProperty ViewModeProperty;
        public static readonly DependencyProperty MergedViewTypeProperty;
        public static readonly DependencyProperty ScaleProperty;
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
            ScaleProperty = DependencyProperty.Register("Scale", typeof(double), ownerType, new PropertyMetadata(1d));
            IsPerfectPixelProperty = DependencyProperty.Register("IsPerfectPixel", typeof(bool), ownerType, new PropertyMetadata(false));
            ShowGridLinesProperty = DependencyProperty.Register("ShowGridLines", typeof(bool), ownerType, new PropertyMetadata(false));
            ScrollModeProperty = DependencyProperty.Register("ScrollMode", typeof(ScrollMode), ownerType, new PropertyMetadata(ScrollMode.Draggable));
            ImageScalingControlProperty = DependencyProperty.RegisterAttached("ImageScalingControl", typeof(ImageScalingControl), ownerType, new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

            ScrollViewerTypeProperty = DependencyProperty.RegisterAttached("ScrollViewerType", typeof(MergedTestViewType), ownerType, new FrameworkPropertyMetadata(MergedTestViewType.Before, FrameworkPropertyMetadataOptions.Inherits));

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
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
        public double Scale {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
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

        public void StartTrackingScrollViewer(DraggableScrollViewer scrollViewer, MergedTestViewType scrollViewerType) {
            scrollViewerScrollSynchronizer.Register(scrollViewerType, scrollViewer);
        }

        public void ChangeView(bool reverse) {
            MergedViewType = GetNextMergedTestViewType(reverse);
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
