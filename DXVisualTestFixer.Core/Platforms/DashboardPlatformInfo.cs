using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class DashboardPlatformInfo : IPlatformInfo {
		public string Name => "Dashboard";
		public string GitRepository => "http://gitserver/CrossPlatform/VisualTestsDashboard.git";
		public string MinioRepository => "Dashboard";
		public string LocalPath => "20{0}_VisualTests_Dashboard";
		public string VersionsFileName => "versions_Dashboard.xml";
		public string FarmTaskName => "Test.v{0} Dashboard VisualTests";
		public string ForkFolderName => "Core";
	}
}