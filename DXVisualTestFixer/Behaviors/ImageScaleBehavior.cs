using DevExpress.Mvvm.UI.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DXVisualTestFixer.Behaviors {
    public class ImageScaleBehavior : Behavior<Image> {
        static List<Image> TrackingImages = new List<Image>();

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            TrackingImages.Add(AssociatedObject);
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            AssociatedObject.LayoutTransform = new ScaleTransform();
        }

        void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            e.Handled = true;
            double dScale = e.Delta > 0 ? 0.1 : -0.1;
            foreach(Image img in TrackingImages) {
                ScaleTransform scaleTransform = img.LayoutTransform as ScaleTransform;
                if(scaleTransform == null)
                    return;
                scaleTransform.ScaleX = Math.Round(scaleTransform.ScaleX + dScale, 1);
                scaleTransform.ScaleY = Math.Round(scaleTransform.ScaleY + dScale, 1);
            }
        }

        void AssociatedObject_Unloaded(object sender, RoutedEventArgs e) {
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            Detach();
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            if(TrackingImages.Contains(AssociatedObject))
                TrackingImages.Remove(AssociatedObject);
        }
    }
}
