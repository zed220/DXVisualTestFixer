using DevExpress.Xpf.Core;
using DevExpress.Xpf.Dialogs;
using Prism.Interactivity;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer.UI.PrismCommon {
    public class PopupDXDialogWindowAction : PopupWindowAction {
        protected override Window CreateWindow() {
            return new ThemedWindow();
        }
    }
}
