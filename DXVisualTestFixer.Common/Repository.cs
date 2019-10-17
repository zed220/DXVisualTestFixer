﻿using System.IO;

namespace DXVisualTestFixer.Common {
	public class Repository {
		public string Version { get; set; }
		public string Path { get; set; }

		public bool IsDownloaded() => File.Exists(System.IO.Path.Combine(Path, "VisualTestsConfig.xml"));

		public string GetTaskName() => $"Test.v{Version} WPF VisualTests";
	}
}