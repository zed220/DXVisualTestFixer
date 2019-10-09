using System.Windows;
using Prism.Mvvm;

namespace DXVisualTestFixer.UI.PrismCommon {
	public static class PrismExtensions {
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.RegisterAttached(
			"ViewModel", typeof(BindableBase), typeof(PrismExtensions), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

		public static void SetViewModel(DependencyObject element, BindableBase value) => element.SetValue(ViewModelProperty, value);

		public static BindableBase GetViewModel(DependencyObject element) => (BindableBase) element.GetValue(ViewModelProperty);
	}
}