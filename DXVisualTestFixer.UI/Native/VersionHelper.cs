using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.UI.Native {
	public static class VersionHelper {
		public static string Version => GetVersion();

		static string GetVersion() {
			return ServiceLocator.Current.GetInstance<IVersionService>().Version.ToString();
		}
	}
}