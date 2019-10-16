using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;

namespace DXVisualTestFixer.UI.Behaviors {
	public class BarButtonIndicatorBehavior : Behavior<DXImage> {
		UIElement AdornerElement { get; set; }

		protected override void OnAttached() {
			base.OnAttached();
			AdornerElement = LayoutHelper.GetTopContainerWithAdornerLayer(AssociatedObject);
		}
	}
}