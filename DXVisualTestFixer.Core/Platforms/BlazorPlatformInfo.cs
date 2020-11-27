using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class BlazorPlatformInfo : IPlatformInfo {
		public string Name => "Blazor";
		public string GitRepository => "git@gitserver:XPF/VisualTestsBlazor.git";
		public string MinioRepository => "XPF";
		public string LocalPath => "20{0}_VisualTests_Blazor";
		public string VersionsFileName => "versions_Blazor.xml";
		public string FarmTaskName => "Test.v{0} WPF VisualTests Blazor";
		public string ForkFolderName => "Common";
	}
}