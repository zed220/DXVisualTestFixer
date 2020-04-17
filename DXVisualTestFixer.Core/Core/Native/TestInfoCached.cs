using System;
using System.Collections.Generic;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Core {
	class TestInfoCached {
		public TestInfoCached(Repository repository, string realUrl, List<TestInfo> testList, CorpDirTestInfoContainer container) {
			Repository = repository;
			RealUrl = realUrl;
			TestList = testList;
			UsedFilesLinks = container.UsedFilesLinks;
			ElapsedTimes = container.ElapsedTimes;
			SourcesBuildTime = container.SourcesBuildTime;
			TestsBuildTime = container.TestsBuildTime;
		}

		public Repository Repository { get; }
		public string RealUrl { get; }
		public List<TestInfo> TestList { get; }
		public List<string> UsedFilesLinks { get; }
		public List<IElapsedTimeInfo> ElapsedTimes { get; }
		public DateTime? SourcesBuildTime { get; }
		public DateTime? TestsBuildTime { get; }
	}
}