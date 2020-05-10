using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class WinFormsPlatformInfo : IPlatformInfo {
		public string Name => "WinForms";
		public string GitRepository => "http://gitserver/XPF/VisualTestsWinForms.git";
		public string MinioRepository => "WinForms";
		public string LocalPath => "20{0}_VisualTests_WinForms";
		public string VersionsFileName => "versions_WinForms.xml";
		public string FarmTaskName => "Test.v{0} WinForms VisualTests";
	}
}