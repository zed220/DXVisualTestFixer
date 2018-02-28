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
        static int Scale = 100;

        protected override void OnAttached() {
            base.OnAttached();
            TrackingImages.Add(AssociatedObject);
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            AssociatedObject.LayoutTransform = new ScaleTransform() { ScaleX = Scale, ScaleY = Scale };
            SetScale(100);
        }

        void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            e.Handled = true;
            if(e.Delta < 0)
                ZoomOut();
            else
                ZoomIn();
        }

        public static void ZoomOut() {
            SetScale(Math.Max(30, Scale - 10));
        }
        public static void ZoomIn() {
            SetScale(Math.Min(300, Scale + 10));
        }
        public static void Zoom100() {
            SetScale(100);
        }
        public static void SetScale(int scale) {
            foreach(Image img in TrackingImages) {
                ScaleTransform scaleTransform = img.LayoutTransform as ScaleTransform;
                if(scaleTransform == null)
                    return;
                scaleTransform.ScaleX = scale / 100d;
                scaleTransform.ScaleY = scale / 100d;
            }
            Scale = scale;
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            if(TrackingImages.Contains(AssociatedObject))
                TrackingImages.Remove(AssociatedObject);
        }
    }
}
