using DevExpress.Mvvm;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Dialogs;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer.Behaviors {
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
            var dialog = new DXFolderBrowserDialog() { ShowNewFolderButton = false };
            var result = dialog.ShowDialog(Application.Current.Windows.Cast<Window>().Last());
            if(result.HasValue && (bool)result)
                AssociatedObject.EditValue = dialog.SelectedPath;
        }
    }
}
