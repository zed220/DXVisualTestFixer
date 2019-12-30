using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using Minio.Exceptions;
using Prism.Mvvm;

namespace DXVisualTestFixer.Core {
	public class TestsService : BindableBase, ITestsService {
		const string xpfStateName = "XPF";
		
		static readonly object lockLoadFromDisk = new object();
		static readonly object lockLoadFromNet = new object();
		readonly IConfigSerializer configSerializer;
		readonly ILoadingProgressController loadingProgressController;
		readonly ILoggingService loggingService;
		readonly IMinioWorker minioWorker;
		readonly INotificationService notificationService;

		readonly Dictionary<string, TestInfoCached> MinioPathCache = new Dictionary<string, TestInfoCached>();

		string _CurrentFilter;
		string SelectedStateName;

		public TestsService(ILoadingProgressController loadingProgressController, IConfigSerializer configSerializer, ILoggingService loggingService, IMinioWorker minioWorker, INotificationService notificationService) {
			this.loadingProgressController = loadingProgressController;
			this.configSerializer = configSerializer;
			this.loggingService = loggingService;
			this.minioWorker = minioWorker;
			this.notificationService = notificationService;
		}

		public ITestInfoContainer SelectedState { get; private set; }

		public string CurrentFilter {
			get => _CurrentFilter;
			set {
				_CurrentFilter = value;
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentFilter)));
			}
		}

		public Dictionary<string, List<Repository>> States { get; } = new Dictionary<string, List<Repository>>();
		
		async Task UpdateTests() {
			CurrentFilter = null;
			await FillStates();
			bool forked = SelectedStateName != xpfStateName;
			var actualRepositories = forked ? GetForkedMinioRepositories() : await GetXpfMinioRepositories();
			var state = await LoadTestsAsync(actualRepositories, !forked);
			loggingService.SendMessage("Start updating problems");
			await ((TestInfoContainer) state).UpdateProblems();
			loggingService.SendMessage("Almost there");
			SelectedState = state;
		}

		async Task FillStates() {
			States.Clear();
			States[xpfStateName] = configSerializer.GetConfig().GetLocalRepositories().ToList();
			var forkedRepositories = await DetectUsersPaths();
			foreach(var userName in forkedRepositories.Keys) 
				States[userName] = forkedRepositories[userName].ToList();
		}
		
		public async Task SelectState(string stateName) {
			SelectedStateName = stateName;
			await UpdateTests();
		}
		
		async Task<List<MinioRepository>> GetXpfMinioRepositories() {
			var result = new List<MinioRepository>();
			foreach(var repository in States[xpfStateName]) {
				repository.MinioPath = await GetResultPath(repository);
				result.Add(new MinioRepository(repository, repository.MinioPath));
			}
			return result;
		}

		async Task<Dictionary<string, List<Repository>>> DetectUsersPaths() {
			var result = new Dictionary<string, List<Repository>>();
			foreach(var userPath in await minioWorker.DetectUserPaths()) {
				var fullUserPath = userPath + "testbuild/";
				if(!await minioWorker.Exists(fullUserPath, "results"))
					continue;
				var resultsPath = fullUserPath + "results/";
				var last = await minioWorker.DiscoverLast(resultsPath);
				if(!await IsResultsLoaded(last))
					continue;
				var userName = userPath.Split('/').First();
				if(!result.TryGetValue(userName, out var repos))
					result[userName] = repos = new List<Repository>();
				var forkName = userPath.Split(new[] {"Common"}, StringSplitOptions.RemoveEmptyEntries).Last().Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries).First();
				var version = await minioWorker.Download(fullUserPath + "version.txt");
				version = version.Replace(Environment.NewLine, string.Empty);
				repos.Add(Repository.CreateFork(userName, version, forkName, last));
			}
			return result;
		}
		List<MinioRepository> GetForkedMinioRepositories() {
			var result = new List<MinioRepository>();
			foreach(var repository in States[SelectedStateName]) {
				result.Add(new MinioRepository(repository, repository.MinioPath));
			}
			return result;
		}
		
		async Task<string> GetResultPath(Repository repository) {
			var lastBuild = await minioWorker.DiscoverLast($"XPF/{repository.Version}/");
			if(!await minioWorker.Exists(lastBuild, "results"))
				lastBuild = await minioWorker.DiscoverPrev($"XPF/{repository.Version}/");
			var last = await minioWorker.DiscoverLast($"{lastBuild}results/");
			
			for(int i = 0; i < 20; i++, await Task.Delay(TimeSpan.FromSeconds(10))) {
				if(await IsResultsLoaded(last))
					return last;
				loggingService.SendMessage($"Waiting while the {repository.Version} was completely finished in the path {last}");
			}
			throw new MinioException($"Results for {repository.Version} does not stored correctly in {last}");
		}
		async Task<bool> IsResultsLoaded(string resultsPath) {
			return (await minioWorker.Discover(resultsPath)).FirstOrDefault(x => x.EndsWith("final")) != null;
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

		async Task<ITestInfoContainer> LoadTestsAsync(List<MinioRepository> minioRepositories, bool allowEditing) {
			loadingProgressController.Flush();
			loadingProgressController.Enlarge(minioRepositories.Count);
			loggingService.SendMessage("Collecting tests information from minio");
			var allTasks = new List<Task>();
			var result = new TestInfoContainer(allowEditing);
			var locker = new object();
			foreach(var minioRepository in minioRepositories) {
				if(string.IsNullOrEmpty(minioRepository.Path)) {
					notificationService?.DoNotification($"Minio path not found for {minioRepository.Repository.Version}", $"Version {minioRepository.Repository.Version} from path {minioRepository.Repository.Path} does not found. Maybe new branch created, but corresponding minio path mission. It will be added later. Otherwise, contact app owners (Zinovyev, Litvinov) for details.");
					continue;
				}

				allTasks.Add(LoadTestsCoreAsync(minioRepository).ContinueWith(cachedResult => {
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

		async Task<TestInfoCached> LoadTestsCoreAsync(MinioRepository minioRepository) {
			if(MinioPathCache.TryGetValue(minioRepository.Repository.Version, out var cache) && cache.RealUrl == minioRepository.Path) {
				await ActualizeTestsAsync(cache.TestList);
				return cache;
			}

			var allTasks = new List<Task<TestInfo>>();
			var corpDirTestInfoContainer = await LoadForRepositoryAsync(minioRepository);
			foreach(var corpDirTestInfo in corpDirTestInfoContainer.FailedTests) {
				var info = corpDirTestInfo;
				var testInfoTask = LoadTestInfo(minioRepository.Repository, info, corpDirTestInfoContainer.Teams); 
				allTasks.Add(testInfoTask);
			}

			var result = (await Task.WhenAll(allTasks)).ToList();
			return MinioPathCache[minioRepository.Repository.Version] = new TestInfoCached(minioRepository.Repository, minioRepository.Path, result, corpDirTestInfoContainer);
		}

		async Task ActualizeTestsAsync(List<TestInfo> testList) {
			var allTasks = testList.Select(UpdateTestStatusAsync).ToList();
			await Task.WhenAll(allTasks);
		}

		async Task<CorpDirTestInfoContainer> LoadForRepositoryAsync(MinioRepository minioRepository) {
			var corpDirTestInfoContainer = await TestLoader.LoadFromMinio(minioRepository);
			loadingProgressController.Enlarge(corpDirTestInfoContainer.FailedTests.Count);
			return corpDirTestInfoContainer;
		}

		async Task<TestInfo> LoadTestInfo(Repository repository, CorpDirTestInfo corpDirTestInfo, List<Team> teams) {
			loggingService.SendMessage($"Start load test v{corpDirTestInfo.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
			var testInfo = TryCreateTestInfo(repository, corpDirTestInfo, teams);
			loggingService.SendMessage($"End load test v{corpDirTestInfo.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
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

		static TestInfo TryCreateTestInfo(Repository repository, CorpDirTestInfo corpDirTestInfo, List<Team> teams) {
			var testInfo = new TestInfo(repository);
			testInfo.Version = corpDirTestInfo.Version;
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
				testInfo.Team = Team.CreateErrorTeam(corpDirTestInfo.Version);
				return testInfo;
			}

			TeamInfo info = null;
			var team = testInfo.Team = GetTeam(teams, corpDirTestInfo.Version, corpDirTestInfo.ServerFolderName, out info);
			if(team == null) {
				testInfo.Valid = TestState.Error;
				testInfo.TextDiffLazy = new Lazy<string>(() => "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText);
				testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
				testInfo.Theme = "Error";
				testInfo.Dpi = 0;
				testInfo.Team = Team.CreateErrorTeam(corpDirTestInfo.Version);
				return testInfo;
			}

			testInfo.TeamInfo = info;
			testInfo.Dpi = info.Dpi;
			testInfo.Theme = corpDirTestInfo.ThemeName;
			testInfo.PredefinedImageDiffsCount = corpDirTestInfo.DiffCount;
			if(testInfo.TeamInfo.Optimized.HasValue)
				testInfo.Optimized = testInfo.TeamInfo.Optimized.Value;

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
				if(diffLines.Contains(line + 1)) continue;
				inRegion = false;
				for(var i = line + 1; i < line + 3; i++) {
					if(i < 0)
						continue;
					if(i < leftArr.Length) sbCompact.AppendLine(leftArr[i]);
					if(i < rightArr.Length) sbCompact.AppendLine(rightArr[i]);
				}

				if(diffLines.IndexOf(line) == diffLines.Count - 1 && line < Math.Min(leftArr.Length, rightArr.Length)) sbCompact.AppendLine("<...>");
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
				if(!IsTextEquals(testInfo.TextBeforeLazy.Value, testInfo.TextCurrentLazy.Value, out var differences, out _))
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
				return !IsTextEquals(testInfo.TextBeforeLazy.Value, testInfo.TextCurrentLazy.Value, out _, out var fullDifferences) ? fullDifferences : string.Empty;
			});
		}

		async Task UpdateTestStatusAsync(TestInfo test) {
			await Task.Factory.StartNew(() => UpdateTestStatus(test));
		}

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
			return checkExists && !CheckFileExistsAndLog(test, xmlPath) ? null : xmlPath;
		}

		static string GetImageFilePath(string testResourceName, TestInfo test, bool checkExists) {
			var imagePath = testResourceName + ".png";
			return checkExists && !CheckFileExistsAndLog(test, imagePath) ? null : imagePath;
		}

		static bool CheckFileExistsAndLog(TestInfo test, string filePath) {
			if(File.Exists(filePath)) return true;
			test.LogFileNotFound(filePath);
			return false;
		}

		static bool SafeDeleteFile(string path, Func<string, bool> checkoutFunc) {
			if(!File.Exists(path))
				return true;
			if((File.GetAttributes(path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
				return true;
			if(!checkoutFunc(path)) return false;
			if((File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				return false;
			File.Delete(path);
			return true;
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