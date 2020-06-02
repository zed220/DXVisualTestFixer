using System.Windows;
using DevExpress.Xpf.Core;
using Prism.Interactivity;

namespace DXVisualTestFixer.UI.PrismCommon {
	public class PopupDXDialogWindowAction : PopupWindowAction {
		protected override Window CreateWindow() {
			return new ThemedWindow();
		}
	}

	public class PopupDXMessageBoxAction : PopupWindowAction {
		protected override Window CreateWindow() {
			return new ThemedMessageBoxWindow() { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen };
		}
	}
}