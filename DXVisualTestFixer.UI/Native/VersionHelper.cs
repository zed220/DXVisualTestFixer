using DXVisualTestFixer.Common;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.UI.Native {
	[UsedImplicitly]
	public static class VersionHelper {
		[UsedImplicitly]
		public static string Version => GetVersion();

		static string GetVersion() {
			return ServiceLocator.Current.GetInstance<IVersionService>().Version.ToString();
		}
	}
}