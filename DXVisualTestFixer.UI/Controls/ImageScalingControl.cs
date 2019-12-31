using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DXVisualTestFixer.UI.Controls.Native;
using DXVisualTestFixer.UI.ViewModels;
using JetBrains.Annotations;

namespace DXVisualTestFixer.UI.Controls {
	public class ImageScalingControl : Control {
		public static readonly DependencyProperty ViewModeProperty;
		public static readonly DependencyProperty MergedViewTypeProperty;
		public static readonly DependencyProperty ShowGridLinesProperty;
		public static readonly DependencyProperty ScrollModeProperty;
		public static readonly DependencyProperty TestInfoModelProperty;
		public static readonly DependencyProperty ImageScalingControlProperty;

		public static readonly DependencyProperty ScrollViewerTypeProperty;
		readonly FocusedPixelSynchronizer focusedPixelSynchronizer = new FocusedPixelSynchronizer();
		readonly ImageScaleSynchronizer imageScaleSynchronizer = new ImageScaleSynchronizer();

		readonly ScrollViewerScrollSynchronizer scrollViewerScrollSynchronizer = new ScrollViewerScrollSynchronizer();

		static ImageScalingControl() {
			var ownerType = typeof(ImageScalingControl);
			TestInfoModelProperty = DependencyProperty.Register("TestInfoModel", typeof(TestInfoModel), ownerType, new PropertyMetadata(null));
			ViewModeProperty = DependencyProperty.Register("ViewMode", typeof(TestViewType), ownerType, new PropertyMetadata(TestViewType.Split));
			MergedViewTypeProperty = DependencyProperty.Register("MergedViewType", typeof(MergedTestViewType), ownerType, new PropertyMetadata(MergedTestViewType.Before));
			ShowGridLinesProperty = DependencyProperty.Register("ShowGridLines", typeof(bool), ownerType, new PropertyMetadata(false, OnShowGridLinesChanged));
			ScrollModeProperty = DependencyProperty.Register("ScrollMode", typeof(ScrollMode), ownerType, new PropertyMetadata(ScrollMode.Draggable));
			ImageScalingControlProperty = DependencyProperty.RegisterAttached("ImageScalingControl", ownerType, ownerType, new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

			ScrollViewerTypeProperty = DependencyProperty.RegisterAttached("ScrollViewerType", typeof(MergedTestViewType?), ownerType, new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

			DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
		}

		public TestInfoModel TestInfoModel {
			get => (TestInfoModel) GetValue(TestInfoModelProperty);
			set => SetValue(TestInfoModelProperty, value);
		}

		public TestViewType ViewMode {
			get => (TestViewType) GetValue(ViewModeProperty);
			set => SetValue(ViewModeProperty, value);
		}

		public MergedTestViewType MergedViewType {
			get => (MergedTestViewType) GetValue(MergedViewTypeProperty);
			set => SetValue(MergedViewTypeProperty, value);
		}

		public bool ShowGridLines {
			get => (bool) GetValue(ShowGridLinesProperty);
			set => SetValue(ShowGridLinesProperty, value);
		}

		public ScrollMode ScrollMode {
			get => (ScrollMode) GetValue(ScrollModeProperty);
			set => SetValue(ScrollModeProperty, value);
		}

		static void OnShowGridLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((ImageScalingControl) d).OnShowGridLinesChanged();
		}

		public static ImageScalingControl GetImageScalingControl(DependencyObject obj) {
			return (ImageScalingControl) obj.GetValue(ImageScalingControlProperty);
		}

		public static void SetImageScalingControl(DependencyObject obj, ImageScalingControl value) {
			obj.SetValue(ImageScalingControlProperty, value);
		}

		public static MergedTestViewType GetScrollViewerType(DependencyObject obj) {
			return (MergedTestViewType) obj.GetValue(ScrollViewerTypeProperty);
		}

		public static void SetScrollViewerType(DependencyObject obj, MergedTestViewType value) {
			obj.SetValue(ScrollViewerTypeProperty, value);
		}

		public void StartTrackingScrollViewer(DraggableScrollViewer scrollViewer, MergedTestViewType scrollViewerType) {
			scrollViewerScrollSynchronizer.Register(scrollViewerType, scrollViewer);
			imageScaleSynchronizer.Register(scrollViewerType, scrollViewer);
			focusedPixelSynchronizer.Register(scrollViewerType, scrollViewer);
			scrollViewer.PreviewMouseWheel += previewMouseWheel;
		}

		void previewMouseWheel(object sender, MouseWheelEventArgs e) {
			e.Handled = true;
			if(e.Delta < 0)
				ZoomOut();
			else
				ZoomIn();
		}

		[UsedImplicitly]
		public void ChangeView(bool reverse) {
			MergedViewType = GetNextMergedTestViewType(reverse);
		}
		[UsedImplicitly]
		public void ZoomIn() {
			imageScaleSynchronizer.ZoomIn();
			UpdateFocusedPixel();
		}
		[UsedImplicitly]
		public void ZoomOut() {
			imageScaleSynchronizer.ZoomOut();
			UpdateFocusedPixel();
		}
		[UsedImplicitly]
		public void Zoom100() {
			imageScaleSynchronizer.Zoom100();
			UpdateFocusedPixel();
		}

		void UpdateFocusedPixel() {
			focusedPixelSynchronizer.IsEnabled = imageScaleSynchronizer.Scale > 100;
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
			switch((TestViewType) item) {
				case TestViewType.Split:
					return SplitTemplate;
				case TestViewType.Merged:
					return MergedTemplate;
			}

			return base.SelectTemplate(item, container);
		}
	}

	public enum ScrollMode {
		Legacy,
		Draggable
	}

	public enum TestViewType {
		Split,
		Merged
	}

	public enum MergedTestViewType {
		Diff,
		Before,
		Current
	}
}