using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
    public partial class ImageScalingControl : Control {
        public static readonly DependencyProperty ViewModeProperty;
        public static readonly DependencyProperty MergedViewTypeProperty;
        public static readonly DependencyProperty ScaleProperty;
        public static readonly DependencyProperty IsPerfectPixelProperty;
        public static readonly DependencyProperty ShowGridLinesProperty;
        public static readonly DependencyProperty ScrollModeProperty;
        public static readonly DependencyProperty TestInfoModelProperty;
        public static readonly DependencyProperty ImageScalingControlProperty;

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
        }



        public static ImageScalingControl GetImageScalingControl(DependencyObject obj) {
            return (ImageScalingControl)obj.GetValue(ImageScalingControlProperty);
        }
        public static void SetImageScalingControl(DependencyObject obj, ImageScalingControl value) {
            obj.SetValue(ImageScalingControlProperty, value);
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

        //ScrollViewer scrollViewerBefore, scrollViewerCurrent, scrollViewerDiff;
        //ScaleImageControl imageBefore, imageCurrent, imageDiff;

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            //scrollViewerBefore = GetTemplateChild<ScrollViewer>(GetTemplateChild, "PART_ScrollViewerBefore");
            //scrollViewerCurrent = GetTemplateChild<ScrollViewer>(GetTemplateChild, "PART_ScrollViewerCurrent");
            //scrollViewerDiff = GetTemplateChild<ScrollViewer>(GetTemplateChild, "PART_ScrollViewerDiff");
            //imageBefore = GetTemplateChild<ScaleImageControl>(GetTemplateChild, "PART_ImageBefore");
            //imageCurrent = GetTemplateChild<ScaleImageControl>(GetTemplateChild, "PART_ImageCurrent");
            //imageDiff = GetTemplateChild<ScaleImageControl>(GetTemplateChild, "PART_ImageDiff");
        }

        static T GetTemplateChild<T>(Func<string, DependencyObject> getChild, string name) where T : DependencyObject {
            var result = getChild(name) as T;
            if(result == null)
                throw new ArgumentNullException(name);
            return result;
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
