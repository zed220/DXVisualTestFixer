using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class WinFormsPlatformInfo : IPlatformInfo {
		public string Name => "WinForms";
		public string GitRepository => "http://gitserver/XPF/VisualTestsWinForms.git";
		public string MinioRepository => "WinForms";
		public string DeployPath => @"\\corp\internal\common\visualTests_squirrel_winForms\";
		public string LocalPath => "20{0}_VisualTests_WinForms";
		public string ApplicationName => "DXVisualTestFixerWinForms";
	}
}