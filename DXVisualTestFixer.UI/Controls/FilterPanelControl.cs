using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
	public class FilterPanelControl : Control {
		public static readonly DependencyProperty HasFixedTestsProperty;

		static FilterPanelControl() {
			var ownerType = typeof(FilterPanelControl);
			HasFixedTestsProperty = DependencyProperty.Register("HasFixedTests", typeof(bool), ownerType, new PropertyMetadata(false));

			DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
		}

		public bool HasFixedTests {
			get => (bool) GetValue(HasFixedTestsProperty);
			set => SetValue(HasFixedTestsProperty, value);
		}
	}
}