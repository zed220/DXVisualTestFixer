using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	public class PlatformProvider : IPlatformProvider {
		public IPlatformInfo[] PlatformInfos { get; } = {new WPFPlatformInfo(), new WinFormsPlatformInfo(), new DashboardPlatformInfo(), new BlazorPlatformInfo() };
	}
}