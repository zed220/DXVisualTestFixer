using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DXVisualTestFixer.UI.Native {
	public static class FileSystemHelper {
		[DllImport("kernel32.dll")]
		static extern bool CreateSymbolicLink(
			string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

		enum SymbolicLink
		{
			[PublicAPI]
			File = 0,
			Directory = 1
		}

		[PublicAPI]
		public static bool CreateDirectoryLink(string sourcePath, string targetPath) => CreateSymbolicLink(targetPath, sourcePath, SymbolicLink.Directory);
	}
}