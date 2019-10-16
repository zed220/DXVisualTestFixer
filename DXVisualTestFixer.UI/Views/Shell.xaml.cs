using System;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.UI.Views {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Shell : ThemedWindow {
		public Shell() => InitializeComponent();

		protected override void OnActivated(EventArgs e) {
			base.OnActivated(e);
			ServiceLocator.Current.GetInstance<IActiveService>().IsActive = true;
		}

		protected override void OnDeactivated(EventArgs e) {
			base.OnDeactivated(e);
			ServiceLocator.Current.GetInstance<IActiveService>().IsActive = false;
		}
	}
}