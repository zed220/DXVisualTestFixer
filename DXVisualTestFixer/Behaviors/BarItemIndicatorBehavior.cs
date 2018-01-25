using DevExpress.Diagram.Core.Native.Ribbon;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer.Behaviors {
    public class BarButtonIndicatorBehavior : Behavior<DXImage> {

        UIElement AdornerElement { get; set; }

        protected override void OnAttached() {
            base.OnAttached();
            AdornerElement = LayoutHelper.GetTopContainerWithAdornerLayer(AssociatedObject);
        }
    }
}
