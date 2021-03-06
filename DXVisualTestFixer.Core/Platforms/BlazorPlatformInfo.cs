using DXVisualTestFixer.Common;

namespace DXVisualTestFixer {
	class BlazorPlatformInfo : IPlatformInfo {
		public string Name => "Blazor";
		public string GitRepository => "git@gitserver:XPF/VisualTestsBlazor.git";
		public string MinioRepository => "Blazor";
		public string LocalPath => "20{0}_VisualTests_Blazor";
		public string VersionsFileName => "versions_Blazor.xml";
		public string FarmTaskName => "Test.v{0} WPF VisualTests Blazor";
		public string ForkFolderName => "Common";
		public string TestStartString => "Exception - Xunit.Sdk.";
		public (string sourcePath, string targetPath)[] Links => new [] {
			(@"_VisualTests_WorkBinPath_\netcore", @"Bin\netcore\standard"),
		};
}
}