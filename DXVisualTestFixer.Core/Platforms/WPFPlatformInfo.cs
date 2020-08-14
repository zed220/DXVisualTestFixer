using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class WPFPlatformInfo : IPlatformInfo {
		public string Name => "WPF";
		public string GitRepository => "git@gitserver:XPF/VisualTests.git";
		public string MinioRepository => "XPF";
		public string LocalPath => "20{0}_VisualTests";
		public string VersionsFileName => "versions_WPF.xml";
		public string FarmTaskName => "Test.v{0} WPF VisualTests";
		public string ForkFolderName => "Common";
	}
}