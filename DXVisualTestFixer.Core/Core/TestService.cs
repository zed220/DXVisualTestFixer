﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.Native;
using Prism.Mvvm;

namespace DXVisualTestFixer.Core {
	internal class TestInfoCached {
		public TestInfoCached(Repository repository, string realUrl, List<TestInfo> testList, CorpDirTestInfoContainer container) {
			Repository = repository;
			RealUrl = realUrl;
			TestList = testList;
			UsedFilesLinks = container.UsedFilesLinks;
			ElapsedTimes = container.ElapsedTimes;
			Teams = container.Teams;
			SourcesBuildTime = container.SourcesBuildTime;
			TestsBuildTime = container.TestsBuildTime;
		}

		public Repository Repository { get; }
		public string RealUrl { get; }
		public List<TestInfo> TestList { get; }
		public List<string> UsedFilesLinks { get; }
		public List<IElapsedTimeInfo> ElapsedTimes { get; }
		public List<Team> Teams { get; }
		public DateTime? SourcesBuildTime { get; }
		public DateTime? TestsBuildTime { get; }
	}

	internal class TestInfoContainer : ITestInfoContainer {
		public TestInfoContainer() {
			TestList = new List<TestInfo>();
			UsedFilesLinks = new Dictionary<Repository, List<string>>();
			ElapsedTimes = new Dictionary<Repository, List<IElapsedTimeInfo>>();
			Teams = new Dictionary<Repository, List<Team>>();
			ChangedTests = new List<TestInfo>();
			Timings = new List<TimingInfo>();
		}

		public List<TestInfo> TestList { get; }
		public Dictionary<Repository, List<string>> UsedFilesLinks { get; }
		public Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }
		public Dictionary<Repository, List<Team>> Teams { get; }
		public List<TimingInfo> Timings { get; }
		public List<TestInfo> ChangedTests { get; }

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

		int CalculateImageDiffsCount(TestInfo test) {
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

	public class TestsService : BindableBase, ITestsService {
		static readonly object lockLoadFromDisk = new object();
		static readonly object lockLoadFromNet = new object();
		readonly IConfigSerializer configSerializer;
		readonly IFarmIntegrator farmIntegrator;
		readonly ILoadingProgressController loadingProgressController;
		readonly ILoggingService loggingService;

		string _CurrentFilter;

		readonly Dictionary<IFarmTaskInfo, TestInfoCached> RealUrlCache = new Dictionary<IFarmTaskInfo, TestInfoCached>();

		public TestsService(ILoadingProgressController loadingProgressController, IConfigSerializer configSerializer, ILoggingService loggingService, IFarmIntegrator farmIntegrator) {
			this.loadingProgressController = loadingProgressController;
			this.configSerializer = configSerializer;
			this.loggingService = loggingService;
			this.farmIntegrator = farmIntegrator;
		}

		public ITestInfoContainer ActualState { get; set; }

		public string CurrentFilter {
			get => _CurrentFilter;
			set {
				_CurrentFilter = value;
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentFilter)));
			}
		}

		public async Task UpdateTests(INotificationService notificationService) {
			CurrentFilter = null;
			var allTasks = farmIntegrator.GetAllTasks(configSerializer.GetConfig().GetLocalRepositories());
			var actualState = await LoadTestsAsync(allTasks, notificationService);
			loggingService.SendMessage("Start updating problems");
			await ((TestInfoContainer) actualState).UpdateProblems();
			loggingService.SendMessage("Almost there");
			ActualState = actualState;
		}

		public string GetResourcePath(Repository repository, string relativePath) {
			return Path.Combine(repository.Path, relativePath);
		}

		public bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc) {
			var actualTestResourceName = GetTestResourceName(test, false);
			var xmlPath = GetXmlFilePath(actualTestResourceName, test, false);
			var imagePath = GetImageFilePath(actualTestResourceName, test, false);
			if(imagePath == null || xmlPath == null) return false;
			if(!SafeDeleteFile(xmlPath, checkoutFunc))
				return false;
			if(!SafeDeleteFile(imagePath, checkoutFunc))
				return false;
			var xmlSHAPath = xmlPath + ".sha";
			if(!SafeDeleteFile(xmlSHAPath, checkoutFunc))
				return false;
			var imageSHAPath = imagePath + ".sha";
			if(!SafeDeleteFile(imageSHAPath, checkoutFunc))
				return false;
			File.WriteAllText(xmlPath, test.TextCurrentLazy.Value);
			if(test.TextCurrentSha == null) {
				using var ms = new MemoryStream(File.ReadAllBytes(xmlPath));
				test.TextCurrentSha = GetSHA256(ms);
			}
			if(test.ImageCurrentSha == null) {
				using var ms = new MemoryStream(test.ImageCurrentArrLazy.Value);
				test.ImageCurrentSha = GetSHA256(ms);
			}

			File.WriteAllBytes(xmlSHAPath, test.TextCurrentSha);
			File.WriteAllBytes(imagePath, test.ImageCurrentArrLazy.Value);
			File.WriteAllBytes(imageSHAPath, test.ImageCurrentSha);
			return true;
		}

		async Task<ITestInfoContainer> LoadTestsAsync(List<IFarmTaskInfo> farmTasks, INotificationService notificationService) {
			loadingProgressController.Flush();
			loadingProgressController.Enlarge(farmTasks.Count);
			loggingService.SendMessage("Collecting tests information from farm");
			var allTasks = new List<Task>();
			var result = new TestInfoContainer();
			result.Timings.Clear();
			var locker = new object();
			foreach(var farmTaskInfo in farmTasks) {
				if(string.IsNullOrEmpty(farmTaskInfo.Url)) {
					notificationService?.DoNotification($"Farm Task Not Found For {farmTaskInfo.Repository.Version}", $"Farm Task {farmTaskInfo.Repository.Version} from path {farmTaskInfo.Repository.Path} does not found. Maybe new branch created, but corresponding farm task missing. It well be added later. Otherwise, contact app owner for details.");
					continue;
				}

				allTasks.Add(LoadTestsCoreAsync(farmTaskInfo).ContinueWith(cachedResult => {
					var cached = cachedResult.Result;
					lock(locker) {
						result.TestList.AddRange(cached.TestList);
						result.UsedFilesLinks[cached.Repository] = cached.UsedFilesLinks;
						result.ElapsedTimes[cached.Repository] = cached.ElapsedTimes.ToList();
						result.Teams[cached.Repository] = cached.Teams;
						result.Timings.Add(new TimingInfo(cached.Repository, cached.SourcesBuildTime, cached.TestsBuildTime));
					}
				}));
			}

			await Task.WhenAll(allTasks);
			return result;
		}

		async Task<TestInfoCached> LoadTestsCoreAsync(IFarmTaskInfo farmTaskInfo) {
			if(RealUrlCache.TryGetValue(farmTaskInfo, out var cache) && cache.RealUrl == farmTaskInfo.Url) {
				await ActualizeTestsAsync(cache.TestList);
				return cache;
			}
			var allTasks = new List<Task<TestInfo>>();
			var corpDirTestInfoContainer = LoadFromFarmTaskInfo(farmTaskInfo, farmTaskInfo.Url);
			foreach(var corpDirTestInfo in corpDirTestInfoContainer.FailedTests) {
				var info = corpDirTestInfo;
				allTasks.Add(LoadTestInfoAsync(info, corpDirTestInfoContainer.Teams));
			}

			var result = (await Task.WhenAll(allTasks)).ToList();
			return RealUrlCache[farmTaskInfo] = new TestInfoCached(farmTaskInfo.Repository, farmTaskInfo.Url, result, corpDirTestInfoContainer);
		}

		async Task ActualizeTestsAsync(List<TestInfo> testList) {
			var allTasks = testList.Select(UpdateTestStatusAsync).ToList();
			await Task.WhenAll(allTasks);
		}

		CorpDirTestInfoContainer LoadFromFarmTaskInfo(IFarmTaskInfo farmTaskInfo, string realUrl) {
			var corpDirTestInfoContainer = TestLoader.LoadFromInfo(farmTaskInfo, realUrl);
			loadingProgressController.Enlarge(corpDirTestInfoContainer.FailedTests.Count);
			return corpDirTestInfoContainer;
		}

		async Task<TestInfo> LoadTestInfoAsync(CorpDirTestInfo corpDirTestInfo, List<Team> teams) => await LoadTestInfo(corpDirTestInfo, teams);

		async Task<TestInfo> LoadTestInfo(CorpDirTestInfo corpDirTestInfo, List<Team> teams) {
			loggingService.SendMessage($"Start load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
			var testInfo = TryCreateTestInfo(corpDirTestInfo, teams);
			loggingService.SendMessage($"End load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
			if(testInfo != null)
				await UpdateTestStatusAsync(testInfo);
			loadingProgressController.IncreaseProgress(1);
			return testInfo;
		}

		static Team GetTeam(List<Team> teams, string version, string serverFolderName, out TeamInfo info) {
			foreach(var team in teams.Where(t => t.Version == version)) {
				info = team.TeamInfos.FirstOrDefault(i => i.ServerFolderName == serverFolderName);
				if(info != null)
					return team;
			}

			info = null;
			return null;
		}

		static TestInfo TryCreateTestInfo(CorpDirTestInfo corpDirTestInfo, List<Team> teams) {
			var testInfo = new TestInfo(corpDirTestInfo.FarmTaskInfo.Repository);
			testInfo.Version = corpDirTestInfo.FarmTaskInfo.Repository.Version;
			testInfo.Name = corpDirTestInfo.TestName;
			testInfo.AdditionalParameters = corpDirTestInfo.AdditionalParameter;
			testInfo.NameWithNamespace = corpDirTestInfo.TestNameWithNamespace;
			testInfo.ResourceFolderName = corpDirTestInfo.ResourceFolderName;
			if(corpDirTestInfo.TeamName == Team.ErrorName) {
				testInfo.Valid = TestState.Error;
				testInfo.TextDiffLazy = new Lazy<string>(() => "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText);
				testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
				testInfo.Theme = "Error";
				testInfo.Dpi = 0;
				testInfo.Team = Team.CreateErrorTeam(corpDirTestInfo.FarmTaskInfo.Repository.Version);
				return testInfo;
			}

			TeamInfo info = null;
			var team = testInfo.Team = GetTeam(teams, corpDirTestInfo.FarmTaskInfo.Repository.Version, corpDirTestInfo.ServerFolderName, out info);
			if(team == null) {
				testInfo.Valid = TestState.Error;
				testInfo.TextDiffLazy = new Lazy<string>(() => "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText);
				testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
				testInfo.Theme = "Error";
				testInfo.Dpi = 0;
				testInfo.Team = Team.CreateErrorTeam(corpDirTestInfo.FarmTaskInfo.Repository.Version);
				return testInfo;
			}

			testInfo.TeamInfo = info;
			testInfo.Dpi = info.Dpi;
			testInfo.Theme = corpDirTestInfo.ThemeName;
			testInfo.PredefinedImageDiffsCount = corpDirTestInfo.DiffCount;
			if(Convert.ToInt32(testInfo.Version.Split('.')[0]) < 18) {
				if(testInfo.TeamInfo.Optimized.HasValue)
					testInfo.Optimized = !corpDirTestInfo.FarmTaskInfo.Url.Contains("UnoptimizedMode");
			}
			else {
				//new version
				if(testInfo.TeamInfo.Optimized.HasValue)
					testInfo.Optimized = testInfo.TeamInfo.Optimized.Value;
			}

			testInfo.TextBeforeLazy = new Lazy<string>(() => LoadTextFile(corpDirTestInfo.InstantTextEditPath));
			testInfo.TextBeforeSha = corpDirTestInfo.InstantTextEditSHA ?? LoadBytes(corpDirTestInfo.InstantTextEditSHAPath);
			testInfo.TextCurrentLazy = new Lazy<string>(() => LoadTextFile(corpDirTestInfo.CurrentTextEditPath));
			testInfo.TextCurrentSha = corpDirTestInfo.CurrentTextEditSHA ?? LoadBytes(corpDirTestInfo.CurrentTextEditSHAPath);
			InitializeTextDiff(testInfo);

			testInfo.ImageBeforeArrLazy = new Lazy<byte[]>(() => LoadBytes(corpDirTestInfo.InstantImagePath));
			testInfo.ImageBeforeSha = corpDirTestInfo.InstantImageSHA ?? LoadBytes(corpDirTestInfo.InstantImageSHAPath);
			testInfo.ImageCurrentArrLazy = new Lazy<byte[]>(() => LoadBytes(corpDirTestInfo.CurrentImagePath));
			testInfo.ImageCurrentSha = corpDirTestInfo.CurrentImageSHA ?? LoadBytes(corpDirTestInfo.CurrentImageSHAPath);

			testInfo.ImageDiffArrLazy = new Lazy<byte[]>(() => LoadBytes(corpDirTestInfo.ImageDiffPath));
			//if(TestValid(testInfo))
			//    testInfo.Valid = true;
			return testInfo;
		}

		static string LoadTextFile(string path) {
			var pathWithExtension = Path.ChangeExtension(path, ".xml");
			if(!File.Exists(pathWithExtension)) //log
				//Debug.WriteLine("fire LoadTextFile");
				return null;
			string text;
			if(!path.StartsWith(@"\\corp"))
				lock(lockLoadFromDisk) {
					text = File.ReadAllText(pathWithExtension);
				}
			else
				lock(lockLoadFromNet) {
					text = File.ReadAllText(pathWithExtension);
				}

			return text;
		}

		static byte[] LoadBytes(string path) {
			if(!File.Exists(path))
				return null;
			byte[] bytes;
			if(!path.StartsWith(@"\\corp"))
				lock(lockLoadFromDisk) {
					bytes = File.ReadAllBytes(path);
				}
			else
				lock(lockLoadFromNet) {
					bytes = File.ReadAllBytes(path);
				}

			return bytes;
		}

		public static bool CompareSHA256(byte[] instant, byte[] current) {
			if(instant == null || current == null)
				return false;
			if(instant.Length != current.Length)
				return false;
			for(var i = 0; i < instant.Length; i++)
				if(instant[i] != current[i])
					return false;
			return true;
		}

		static byte[] GetSHA256(Stream stream) {
			if(stream == null)
				return null;
			stream.Seek(0, SeekOrigin.Begin);
			using var sha256Hash = SHA256.Create();
			return sha256Hash.ComputeHash(stream);
		}

		static bool IsTextEquals(string left, string right, out string diff, out string fullDiff) {
			if(left == right) {
				diff = "+Texts Equals";
				fullDiff = null;
				return true;
			}

			var info = BuildDifferences(left, right);
			diff = info.DiffCompact;
			fullDiff = info.DiffFull;
			return false;
		}

		static TextDifferenceInfo BuildDifferences(string left, string right) {
			var sbFull = new StringBuilder();
			var leftArr = left.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
			var rightArr = right.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
			var diffLines = new List<int>();
			for(var line = 0; line < Math.Min(leftArr.Length, rightArr.Length); line++) {
				var leftStr = leftArr[line];
				var rightStr = rightArr[line];
				if(leftStr == rightStr) {
					sbFull.AppendLine(leftStr);
					continue;
				}

				diffLines.Add(line);
				sbFull.AppendLine("-" + leftStr);
				sbFull.AppendLine("+" + rightStr);
			}

			return new TextDifferenceInfo(BuildCompactDifferences(leftArr, rightArr, diffLines), sbFull.ToString());
		}

		static string BuildCompactDifferences(string[] leftArr, string[] rightArr, List<int> diffLines) {
			if(diffLines.Count == 0)
				return null;
			var sbCompact = new StringBuilder();
			var inRegion = false;
			foreach(var line in diffLines) {
				if(line >= Math.Min(leftArr.Length, rightArr.Length))
					break;
				if(!inRegion) {
					inRegion = true;
					if(line > 0) sbCompact.AppendLine("<...>");
					for(var i = line - 2; i < line; i++) {
						if(i < 0)
							continue;
						if(i < leftArr.Length) sbCompact.AppendLine(leftArr[i]);
						if(i < rightArr.Length) sbCompact.AppendLine(rightArr[i]);
					}
				}

				if(line < leftArr.Length) sbCompact.AppendLine("-" + leftArr[line]);
				if(line < rightArr.Length) sbCompact.AppendLine("+" + rightArr[line]);
				if(!diffLines.Contains(line + 1)) {
					inRegion = false;
					for(var i = line + 1; i < line + 3; i++) {
						if(i < 0)
							continue;
						if(i < leftArr.Length) sbCompact.AppendLine(leftArr[i]);
						if(i < rightArr.Length) sbCompact.AppendLine(rightArr[i]);
					}

					if(diffLines.IndexOf(line) == diffLines.Count - 1 && line < Math.Min(leftArr.Length, rightArr.Length)) sbCompact.AppendLine("<...>");
				}
			}

			return sbCompact.ToString();
		}

		static void InitializeTextDiff(TestInfo testInfo) {
			testInfo.TextDiffLazy = new Lazy<string>(() => string.Empty);
			testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
			if(CompareSHA256(testInfo.TextBeforeSha, testInfo.TextCurrentSha))
				return;

			testInfo.TextDiffLazy = new Lazy<string>(() => {
				if(string.IsNullOrEmpty(testInfo.TextBeforeLazy.Value) && string.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
					return string.Empty;
				if(string.IsNullOrEmpty(testInfo.TextBeforeLazy.Value))
					return testInfo.TextCurrentLazy.Value;
				if(string.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
					return testInfo.TextBeforeLazy.Value;
				if(!IsTextEquals(testInfo.TextBeforeLazy.Value, testInfo.TextCurrentLazy.Value, out var differences, out var fullDifferences))
					return differences;
				return string.Empty;
			});
			testInfo.TextDiffFullLazy = new Lazy<string>(() => {
				if(string.IsNullOrEmpty(testInfo.TextBeforeLazy.Value) && string.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
					return string.Empty;
				if(string.IsNullOrEmpty(testInfo.TextBeforeLazy.Value))
					return testInfo.TextCurrentLazy.Value;
				if(string.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
					return testInfo.TextBeforeLazy.Value;
				if(!IsTextEquals(testInfo.TextBeforeLazy.Value, testInfo.TextCurrentLazy.Value, out var differences, out var fullDifferences))
					return fullDifferences;
				return string.Empty;
			});
		}

		async Task UpdateTestStatusAsync(TestInfo test) => await Task.Factory.StartNew(() => UpdateTestStatus(test));

		void UpdateTestStatus(TestInfo test) {
			if(test.Valid == TestState.Error)
				return;
			test.Valid = TestState.Valid;
			var actualTestResourceName = GetTestResourceName(test, true);
			if(actualTestResourceName == null) {
				test.Valid = TestState.Invalid;
				return;
			}

			var xmlPath = GetXmlFilePath(actualTestResourceName, test, true);
			var imagePath = GetImageFilePath(actualTestResourceName, test, true);
			if(xmlPath == null || imagePath == null) {
				test.Valid = TestState.Invalid;
				return;
			}

			if(!File.Exists(imagePath)) {
				test.LogCustomError($"File Can not load: \"{imagePath}\"");
				test.Valid = TestState.Invalid;
			}

			if(!File.Exists(xmlPath)) {
				test.LogCustomError($"File Can not load: \"{xmlPath}\"");
				test.Valid = TestState.Invalid;
			}

			if(test.Valid == TestState.Invalid) {
				test.LogCustomError("Is it new test?");
				return;
			}

			if(CompareSHA256(test.ImageBeforeSha, test.ImageCurrentSha))
				test.ImageEquals = true;
			var imageFixed = test.ImageEquals;
			if(!imageFixed)
				imageFixed = CompareSHA256(LoadBytes(imagePath + ".sha"), test.ImageCurrentSha);
			var textEquals = CompareSHA256(test.TextBeforeSha, test.TextCurrentSha);
			var textFixed = textEquals;
			if(!textFixed)
				textFixed = CompareSHA256(LoadBytes(xmlPath + ".sha"), test.TextCurrentSha);
			if(imageFixed && textFixed) {
				test.Valid = TestState.Fixed;
				return;
			}

			test.Valid = TestState.Valid;
		}

		public Repository GetRepository(string version) {
			return configSerializer.GetConfig().Repositories.Where(r => r.Version == version).FirstOrDefault();
		}

		string GetTestResourceName(TestInfo test, bool checkDirectoryExists) {
			var repository = GetRepository(test.Version);
			if(repository == null) {
				test.LogCustomError($"Config not found for version \"{test.Version}\"");
				return null;
			}

			var testResourcesPath = test.Optimized && test.TeamInfo.TestResourcesPathOptimized != null ? test.TeamInfo.TestResourcesPathOptimized : test.TeamInfo.TestResourcesPath;
			var actualTestResourcesPath = Path.Combine(repository.Path, testResourcesPath, test.ResourceFolderName);
			if(!Directory.Exists(actualTestResourcesPath)) {
				if(checkDirectoryExists) {
					test.LogDirectoryNotFound(actualTestResourcesPath);
					return null;
				}

				Directory.CreateDirectory(actualTestResourcesPath);
			}

			return Path.Combine(actualTestResourcesPath, test.Theme);
		}

		static string GetXmlFilePath(string testResourceName, TestInfo test, bool checkExists) {
			if(test.Optimized)
				testResourceName = testResourceName + "_optimized";
			var xmlPath = testResourceName + ".xml";
			if(checkExists && !File.Exists(xmlPath)) {
				test.LogFileNotFound(xmlPath);
				return null;
			}

			return xmlPath;
		}

		static string GetImageFilePath(string testResourceName, TestInfo test, bool checkExists) {
			var imagePath = testResourceName + ".png";
			if(checkExists && !File.Exists(imagePath)) {
				test.LogFileNotFound(imagePath);
				return null;
			}

			return imagePath;
		}

		static bool SafeDeleteFile(string path, Func<string, bool> checkoutFunc) {
			if(!File.Exists(path))
				return true;
			if((File.GetAttributes(path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
				return true;
			if(checkoutFunc(path)) {
				if((File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					return false;
				File.Delete(path);
				return true;
			}

			return false;
		}

		class TextDifferenceInfo {
			public TextDifferenceInfo(string diffCompact, string diffFull) {
				DiffCompact = diffCompact;
				DiffFull = diffFull;
			}

			public string DiffCompact { get; }
			public string DiffFull { get; }
		}
	}
}