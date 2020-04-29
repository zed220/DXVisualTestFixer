using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class WPFPlatformInfo : IPlatformInfo {
		public string Name => "WPF";
		public string GitRepository => "http://gitserver/XPF/VisualTests.git";
		public string MinioRepository => "XPF";
		public string DeployPath => @"\\corp\internal\common\visualTests_squirrel\";
		public string LocalPath => "20{0}_VisualTests";
		public string ApplicationName => "DXVisualTestFixer";
	}
}