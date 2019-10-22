using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
	public class LoadingButton : Button {
		public static readonly DependencyProperty LoadingStateProperty;

		static LoadingButton() {
			var ownerType = typeof(LoadingButton);
			LoadingStateProperty = DependencyProperty.Register("LoadingState", typeof(LoadingButtonState), ownerType, new PropertyMetadata(LoadingButtonState.Enabled, (d, e) => ((LoadingButton) d).OnLoadingStateChanged()));
		}

		public LoadingButton() {
			DefaultStyleKey = typeof(LoadingButton);
		}

		public LoadingButtonState LoadingState {
			get => (LoadingButtonState) GetValue(LoadingStateProperty);
			set => SetValue(LoadingStateProperty, value);
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			UpdateAnimations();
		}

		void OnLoadingStateChanged() {
			UpdateAnimations();
		}

		void UpdateAnimations() {
			VisualStateManager.GoToState(this, LoadingState.ToString(), false);
		}
	}

	public enum LoadingButtonState {
		Enabled,
		Loading,
		Loaded,
		Disabled
	}
}