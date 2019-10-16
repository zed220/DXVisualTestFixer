using System.Linq;
using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Dialogs;
using DevExpress.Xpf.Editors;

namespace DXVisualTestFixer.UI.Behaviors {
	public class BrowseDirBehavior : Behavior<ButtonEdit> {
		protected override void OnAttached() {
			base.OnAttached();
			AssociatedObject.DefaultButtonClick += AssociatedObject_DefaultButtonClick;
		}

		protected override void OnDetaching() {
			AssociatedObject.DefaultButtonClick -= AssociatedObject_DefaultButtonClick;
			base.OnDetaching();
		}


		void AssociatedObject_DefaultButtonClick(object sender, RoutedEventArgs e) {
			var dialog = new DXFolderBrowserDialog {ShowNewFolderButton = false};
			var result = dialog.ShowDialog(Application.Current.Windows.Cast<Window>().Last());
			if(result.HasValue && (bool) result)
				AssociatedObject.EditValue = dialog.SelectedPath;
		}
	}
}