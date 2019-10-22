using System;
using System.Text;
using JetBrains.Annotations;

namespace DXVisualTestFixer.Common {
	public class TestInfo {
		StringBuilder _invalidLogBuilder;

		public TestInfo(Repository repository) {
			Repository = repository;
			ImageBeforeArrLazy = new Lazy<byte[]>(() => null);
			ImageCurrentArrLazy = new Lazy<byte[]>(() => null);
			ImageDiffArrLazy = new Lazy<byte[]>(() => null);

			TextBeforeLazy = new Lazy<string>(() => null);
			TextCurrentLazy = new Lazy<string>(() => null);
			TextDiffLazy = new Lazy<string>(() => null);
			TextDiffFullLazy = new Lazy<string>(() => null);
		}

		StringBuilder InvalidLogBuilder => _invalidLogBuilder ??= new StringBuilder();

		public string Name { get; set; }
		public string NameWithNamespace { get; set; }
		public string ResourceFolderName { get; set; }
		public Repository Repository { get; }
		public Team Team { get; set; }
		public TeamInfo TeamInfo { get; set; }
		public string Theme { get; set; }
		public string Version { get; set; }
		public int Dpi { get; set; }
		public Lazy<byte[]> ImageBeforeArrLazy { get; set; }
		public byte[] ImageBeforeSha { get; set; }
		public Lazy<byte[]> ImageCurrentArrLazy { get; set; }
		public byte[] ImageCurrentSha { get; set; }
		public Lazy<byte[]> ImageDiffArrLazy { get; set; }
		public Lazy<string> TextBeforeLazy { get; set; }
		public byte[] TextBeforeSha { get; set; }
		public Lazy<string> TextCurrentLazy { get; set; }
		public byte[] TextCurrentSha { get; set; }
		public bool Optimized { get; set; }
		public Lazy<string> TextDiffLazy { get; set; }
		public Lazy<string> TextDiffFullLazy { get; set; }
		public TestState Valid { get; set; }
		public bool ImageEquals { get; set; }
		public int ImageDiffsCount { get; set; }
		public int? PredefinedImageDiffsCount { get; set; }
		public int Problem { get; set; } = int.MinValue;
		public string ProblemName { get; set; }

		[UsedImplicitly] public string InvalidLog => InvalidLogBuilder.ToString();

		public string AdditionalParameters { get; set; }

		public void LogCustomError(string text) {
			InvalidLogBuilder.AppendLine(text);
		}

		public void LogDirectoryNotFound(string dirPath) {
			LogCustomError($"Directory not found: \"{dirPath}\"");
		}

		public void LogFileNotFound(string filePath) {
			LogCustomError($"File not found: \"{filePath}\"");
		}
	}
}