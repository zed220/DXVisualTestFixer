using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DXVisualTestFixer.UI.Controls;

namespace DXVisualTestFixer.UI.Behaviors {
	public class ScrollViewerRegistrationBehavior : Behavior<DraggableScrollViewer> {
		public static readonly DependencyProperty ImageScalingControlProperty;
		public static readonly DependencyProperty ScrollViewerTypeProperty;

		bool isRegistered;

		static ScrollViewerRegistrationBehavior() {
			var ownerType = typeof(ScrollViewerRegistrationBehavior);
			ImageScalingControlProperty = DependencyProperty.Register("ImageScalingControl", typeof(ImageScalingControl), ownerType, new PropertyMetadata(null, Register));
			ScrollViewerTypeProperty = DependencyProperty.Register("ScrollViewerType", typeof(MergedTestViewType?), ownerType, new PropertyMetadata(null, Register));
		}

		public ImageScalingControl ImageScalingControl {
			get => (ImageScalingControl) GetValue(ImageScalingControlProperty);
			set => SetValue(ImageScalingControlProperty, value);
		}

		public MergedTestViewType? ScrollViewerType {
			get => (MergedTestViewType?) GetValue(ScrollViewerTypeProperty);
			set => SetValue(ScrollViewerTypeProperty, value);
		}

		static void Register(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((ScrollViewerRegistrationBehavior) d).Register();
		}

		protected override void OnAttached() {
			base.OnAttached();
			Register();
		}

		protected override void OnDetaching() {
			base.OnDetaching();
		}

		void Register() {
			if(isRegistered)
				return;
			if(ImageScalingControl == null || !ScrollViewerType.HasValue)
				return;
			ImageScalingControl.StartTrackingScrollViewer(AssociatedObject, ScrollViewerType.Value);
			isRegistered = true;
		}
	}
}