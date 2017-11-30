using DevExpress.Mvvm.UI.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.Behaviors {
    public class ScrollVIewerDragBehavior : Behavior<ScrollViewer> {
        static List<ScrollViewer> TrackingScrollViewers = new List<ScrollViewer>();

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            TrackingScrollViewers.Add(AssociatedObject);
            AssociatedObject.ScrollChanged += AssociatedObject_ScrollChanged;
        }

        void AssociatedObject_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            foreach(ScrollViewer sv in TrackingScrollViewers) {
                sv.ScrollToHorizontalOffset(e.HorizontalOffset);
                sv.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        void AssociatedObject_Unloaded(object sender, RoutedEventArgs e) {
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            Detach();
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.ScrollChanged -= AssociatedObject_ScrollChanged;
            if(TrackingScrollViewers.Contains(AssociatedObject))
                TrackingScrollViewers.Remove(AssociatedObject);
        }
    }
}
