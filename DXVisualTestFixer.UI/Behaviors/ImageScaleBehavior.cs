using DevExpress.Mvvm.UI.Interactivity;
using DXVisualTestFixer.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DXVisualTestFixer.UI.Behaviors {
    public class ImageScaleBehavior : Behavior<ScrollViewer> {
        static List<ScaleImageControl> TrackingControls = new List<ScaleImageControl>();
        static int scale = 100;

        public static bool IsPerfectPixel { get; private set; }
        public static bool ShowGridLines { get; private set; }

        protected override void OnAttached() {
            base.OnAttached();
            if(!AssociatedObject.IsLoaded)
                AssociatedObject.Loaded += AssociatedObject_Loaded;
            else
                Initialize();
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e) {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            Initialize();
        }

        ScaleImageControl TrackingControl { get { return (ScaleImageControl)AssociatedObject.Content; } }

        void Initialize() {
            TrackingControls.Add(TrackingControl);
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            TrackingControl.LayoutTransform = new ScaleTransform();

            SetScale(100);
        }

        void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            e.Handled = true;
            if(e.Delta < 0)
                ZoomOut();
            else
                ZoomIn();
        }

        public static void ChangeScaleMode(bool perfectPixel) {
            SetScale(100);
            IsPerfectPixel = perfectPixel;
        }
        public static void ChangeShowGridLines(bool showGridLines) {
            ShowGridLines = showGridLines;
            SetScale(scale);
        }

        public static void ZoomOut() {
            int d = IsPerfectPixel ? 100 : 10;
            int min = IsPerfectPixel ? 100 : 30;
            SetScale(Math.Max(min, scale - d));
        }
        public static void ZoomIn() {
            int d = IsPerfectPixel ? 100 : 10;
            SetScale(Math.Min(600, scale + d));
        }
        public static void Zoom100() {
            SetScale(100);
        }
        public static void SetScale(int scale) {
            foreach(var trackingControl in TrackingControls) {
                ScaleTransform scaleTransform = trackingControl.LayoutTransform as ScaleTransform;
                if(scaleTransform == null)
                    return;
                if(IsPerfectPixel) {
                    scaleTransform.ScaleX = 1;
                    scaleTransform.ScaleY = 1;
                    trackingControl.Scale = scale / 100;
                    trackingControl.ShowGridLines = ShowGridLines;
                }
                else {
                    trackingControl.Scale = 1;
                    scaleTransform.ScaleX = scale / 100d;
                    scaleTransform.ScaleY = scale / 100d;
                }
            }
            ImageScaleBehavior.scale = scale;
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            if(TrackingControls.Contains(TrackingControl))
                TrackingControls.Remove(TrackingControl);
        }
    }
}
