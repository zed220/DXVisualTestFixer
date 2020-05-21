using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
	public class ErrorTestControl : Control {
		static ErrorTestControl() {
			var ownerType = typeof(ErrorTestControl);
			
			DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
		}
	}
}