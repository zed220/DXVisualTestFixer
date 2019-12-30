using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.Native;

namespace DXVisualTestFixer.Core {
	class TestInfoContainer : ITestInfoContainer {
		public TestInfoContainer(bool allowEditing) {
			TestList = new List<TestInfo>();
			UsedFilesLinks = new Dictionary<Repository, List<string>>();
			ElapsedTimes = new Dictionary<Repository, List<IElapsedTimeInfo>>();
			Teams = new Dictionary<Repository, List<Team>>();
			ChangedTests = new List<TestInfo>();
			Timings = new List<TimingInfo>();
			AllowEditing = allowEditing;
		}

		public List<TestInfo> TestList { get; }
		public Dictionary<Repository, List<string>> UsedFilesLinks { get; }
		public Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }
		public Dictionary<Repository, List<Team>> Teams { get; }
		public List<TimingInfo> Timings { get; }
		public List<TestInfo> ChangedTests { get; }
		public bool AllowEditing { get; }

		public async Task UpdateProblems() {
			await Task.Factory.StartNew(() => {
				Parallel.ForEach(TestList, test => {
					test.Problem = int.MinValue;
					test.ImageDiffsCount = test.PredefinedImageDiffsCount ?? CalculateImageDiffsCount(test);
				});
			}).ConfigureAwait(false);

			var diffs = new List<int>();
			TestList.ForEach(t => diffs.Add(t.ImageDiffsCount));
			diffs.Sort();
			diffs = diffs.Distinct().ToList();
			var problems = new Dictionary<int, int>();
			var proplemNumber = 1;
			var currentD = 0;
			foreach(var d in diffs)
				if(currentD * 1.2d < d) {
					problems.Add(currentD, proplemNumber++);
					currentD = d;
				}

			if(!problems.ContainsKey(currentD))
				problems.Add(currentD, proplemNumber++);
			var namedProblems = new Dictionary<int, HashSet<string>>();
			foreach(var d in problems)
			foreach(var test in TestList)
				if(test.ImageDiffsCount < d.Key * 1.2d && test.Problem == int.MinValue) {
					test.Problem = d.Value;
					HashSet<string> tests = null;
					if(!namedProblems.TryGetValue(d.Value, out tests))
						namedProblems[d.Value] = tests = new HashSet<string>();
					if(!tests.Contains(test.Team.Name))
						tests.Add(test.Team.Name);
				}

			foreach(var test in TestList) {
				if(!namedProblems.TryGetValue(test.Problem, out var namedProblemsList))
					namedProblemsList = new HashSet<string>();
				var problemNumber = $"{test.Problem:D2}";
				test.ProblemName = $"#{problemNumber} ({string.Join(", ", namedProblemsList)})";
			}
		}

		static int CalculateImageDiffsCount(TestInfo test) {
			if(test.ImageDiffArrLazy.Value == null)
				return int.MaxValue;
			if(TestsService.CompareSHA256(test.ImageBeforeSha, test.ImageCurrentSha))
				return int.MaxValue;
			if(test.ImageBeforeSha == null || test.ImageCurrentSha == null)
				return int.MaxValue;
			using var imgDiff = ImageHelper.CreateImageFromArray(test.ImageDiffArrLazy.Value);
			return ImageHelper.RedCount(imgDiff);
		}
	}
}