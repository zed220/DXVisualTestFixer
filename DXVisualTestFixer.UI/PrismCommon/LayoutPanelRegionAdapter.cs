using System.Collections.Specialized;
using System.Linq;
using DevExpress.Xpf.Docking;
using JetBrains.Annotations;
using Prism.Regions;

namespace DXVisualTestFixer.UI.PrismCommon {
	[UsedImplicitly]
	public class LayoutPanelRegionAdapter : RegionAdapterBase<LayoutPanel> {
		public LayoutPanelRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory) { }

		protected override void Adapt(IRegion region, LayoutPanel regionTarget) {
			region.Views.CollectionChanged += (o, e) => Views_CollectionChanged(e, regionTarget);
		}

		void Views_CollectionChanged(NotifyCollectionChangedEventArgs e, LayoutPanel regionTarget) {
			switch(e.Action) {
				case NotifyCollectionChangedAction.Add:
					regionTarget.Content = e.NewItems.Cast<object>().Last();
					break;
				case NotifyCollectionChangedAction.Remove:
					regionTarget.Content = null;
					break;
				case NotifyCollectionChangedAction.Replace:
					regionTarget.Content = e.NewItems.Cast<object>().Last();
					break;
				case NotifyCollectionChangedAction.Move:
					break;
				case NotifyCollectionChangedAction.Reset:
					break;
			}
		}

		protected override IRegion CreateRegion() {
			return new SingleActiveRegion();
		}
	}
}