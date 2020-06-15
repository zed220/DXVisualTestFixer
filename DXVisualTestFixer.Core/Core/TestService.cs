using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;
using Prism.Mvvm;

namespace DXVisualTestFixer.Core {
	public class TestsService : BindableBase, ITestsService {
		class PlatformAndVersion : IEquatable<PlatformAndVersion> {
			public readonly string Platform;
			public readonly string Version;
			
			public PlatformAndVersion(string platform, string version) {
				Platform = platform;
				Version = version;
			}

			public bool Equals(PlatformAndVersion other) {
				if(ReferenceEquals(null, other)) return false;
				if(ReferenceEquals(this, other)) return true;
				return Platform == other.Platform && Version == other.Version;
			}

			public override bool Equals(object obj) {
				if(ReferenceEquals(null, obj)) return false;
				if(ReferenceEquals(this, obj)) return true;
				if(obj.GetType() != this.GetType()) return false;
				return Equals((PlatformAndVersion) obj);
			}

			public override int GetHashCode() {
				unchecked {
					return ((Platform != null ? Platform.GetHashCode() : 0) * 397) ^ (Version != null ? Version.GetHashCode() : 0);
				}
			}
		}
		
		
		readonly IPlatformProvider platformProvider;
		
		static readonly object lockLoadFromDisk = new object();
		static readonly object lockLoadFromNet = new object();
		readonly IConfigSerializer configSerializer;
		readonly ILoadingProgressController loadingProgressController;
		readonly ILoggingService loggingService;
		readonly IMinioWorker minioWorker;
		readonly INotificationService notificationService;

		readonly Dictionary<PlatformAndVersion, TestInfoCached> MinioPathCache = new Dictionary<PlatformAndVersion, TestInfoCached>();

		string _CurrentFilter;
		string Platform;
		string SelectedStateName;

		public TestsService(ILoadingProgressController loadingProgressController, IConfigSerializer configSerializer, ILoggingService loggingService, IMinioWorker minioWorker, INotificationService notificationService) {
			this.loadingProgressController = loadingProgressController;
			this.configSerializer = configSerializer;
			this.loggingService = loggingService;
			this.minioWorker = minioWorker;
			this.notificationService = notificationService;
			platformProvider = ServiceLocator.Current.GetInstance<IPlatformProvider>();
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
			bool forked = SelectedStateName != Platform;
			var actualRepositories = forked ? GetForkedMinioRepositories() : await GetRegularMinioRepositories();
			var state = await LoadTestsAsync(actualRepositories, !forked);
			loggingService.SendMessage("Start updating problems");
			await state.UpdateProblems();
			loggingService.SendMessage("Almost there");
			SelectedState = state;
		}

		async Task FillStates() {
			States.Clear();
			States[Platform] = configSerializer.GetConfig().GetLocalRepositories().Where(r => r.Platform == Platform).ToList();
			var forkedRepositories = await DetectUsersPaths(Platform);//TODO
			foreach(var userName in forkedRepositories.Keys) 
				States[userName] = forkedRepositories[userName].ToList();
		}
		
		public async Task SelectState(string platform, string stateName) {
			Platform = platform;
			SelectedStateName = stateName;
			await UpdateTests();
		}
		
		async Task<List<MinioRepository>> GetRegularMinioRepositories() {
			var result = new List<MinioRepository>();
			foreach(var repository in States[Platform]) {
				repository.MinioPath = await GetResultPath(repository);
				if(repository.MinioPath != null)
					result.Add(new MinioRepository(repository, repository.MinioPath));
			}
			return result;
		}

		async Task<Dictionary<string, List<Repository>>> DetectUsersPaths(string platform) {
			var result = new Dictionary<string, List<Repository>>();
			foreach(var userPath in await minioWorker.DetectUserPaths(platformProvider.PlatformInfos.Single(p => p.Name == Platform).MinioRepository)) {
				var fullUserPath = userPath + "testbuild/";
				if(!await minioWorker.Exists(fullUserPath, "results"))
					continue;
				var resultsPath = fullUserPath + "results/";
				var last = await minioWorker.DiscoverLast(resultsPath);
				if(last == null)
					continue;
				if(!await IsResultsLoaded(last))
					continue;
				var userName = userPath.Split('/').Skip(1).First();
				if(!result.TryGetValue(userName, out var repos))
					result[userName] = repos = new List<Repository>();
				var forkName = userPath.Split(new[] {"Common"}, StringSplitOptions.RemoveEmptyEntries).Last().Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries).First();
				var version = await minioWorker.Download(fullUserPath + "version.txt");
				version = version.Replace(Environment.NewLine, string.Empty);
				repos.Add(Repository.CreateFork(platform, version, forkName, last, States[Platform].FirstOrDefault(r => r.Version == version)?.Path));
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
			string lastBuild = null;
			var minioPath = platformProvider.PlatformInfos.Single(p => p.Name == repository.Platform).MinioRepository;
			for(int prevCount = 0; prevCount < 5; prevCount++) {
				lastBuild = await minioWorker.DiscoverPrev($"{minioPath}/{minioPath}/{repository.Version}/", prevCount);
				if(lastBuild == null && repository.ReadOnly)
					return null;
				if(await minioWorker.Exists(lastBuild, "results"))
					break;
			}
			var last = await minioWorker.DiscoverLast($"{lastBuild}results/");
			
			for(int i = 0; i < 20; i++, await Task.Delay(TimeSpan.FromSeconds(10))) {
				if(await IsResultsLoaded(last))
					return last;
				loggingService.SendMessage($"Waiting while the {repository.Version} was completely finished in the path {last}");
			}
			throw new Exception($"Results for {repository.Version} does not stored correctly in {last}");
		}
		async Task<bool> IsResultsLoaded(string resultsPath) {
			return (await minioWorker.Discover(resultsPath)).FirstOrDefault(x => x.EndsWith("final")) != null;
		}

		public bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc) {
			var actualTestResourceName = GetTestResourceName(test, false);
			var xmlPath = GetXmlFilePath(actualTestResourceName, test);
			var imagePath = GetImageFilePath(actualTestResourceName, test);
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
			
			if(test.TextCurrentSha == null) {
				using var ms = new MemoryStream(File.ReadAllBytes(xmlPath));
				test.TextCurrentSha = StringToSha(test.TextCurrentLazy.Value);
			}
			if(test.ImageCurrentSha == null) {
				using var ms = new MemoryStream(test.ImageCurrentArrLazy.Value);
				test.ImageCurrentSha = GetSHA256(ms);
			}
			if(!test.CommitAsBlinking) {
				foreach(var xmlBlinkSha in GetAllBlinkingSha(Path.ChangeExtension(xmlPath, "").TrimEnd('.'), ".xml")) 
					if(!SafeDeleteFile(xmlBlinkSha, checkoutFunc))
						return false;
				foreach(var xmlBlinkSha in GetAllBlinkingSha(Path.ChangeExtension(xmlPath, "").TrimEnd('.').Replace("_optimized", ""), ".xml")) 
					if(!SafeDeleteFile(xmlBlinkSha, checkoutFunc))
						return false;
				foreach(var imageBlinkSha in GetAllBlinkingSha(Path.ChangeExtension(imagePath, "").TrimEnd('.'), ".png")) 
					if(!SafeDeleteFile(imageBlinkSha, checkoutFunc))
						return false;
				File.WriteAllText(xmlPath, test.TextCurrentLazy.Value);
				File.WriteAllBytes(imagePath, test.ImageCurrentArrLazy.Value);
			}

			File.WriteAllBytes(xmlSHAPath, test.TextCurrentSha);
			File.WriteAllBytes(imageSHAPath, test.ImageCurrentSha);
			return true;
		}
		
		static byte[] StringToSha(String value) {
			using var hash = SHA256Managed.Create();
			var enc = Encoding.UTF8;
			return hash.ComputeHash(enc.GetBytes(value));
		}

		async Task<TestInfoContainer> LoadTestsAsync(List<MinioRepository> minioRepositories, bool allowEditing) {
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

				allTasks.Add(LoadTestsCoreAsync(minioRepository).ContinueWith(async c => await UpdateVolunteers(c.Result)).ContinueWith(cachedResult => {
					var cached = cachedResult.Result.Result;
					lock(locker) {
						result.TestList.AddRange(cached.TestList);
						result.UsedFilesLinks[cached.Repository] = cached.UsedFilesLinks;
						result.ElapsedTimes[cached.Repository] = cached.ElapsedTimes.ToList();
						result.Timings.Add(new TimingInfo(cached.Repository, cached.SourcesBuildTime, cached.TestsBuildTime));
					}
				}));
			}

			await Task.WhenAll(allTasks);
			return result;
		}

		async Task<TestInfoCached> UpdateVolunteers(TestInfoCached cached) {
			var platformInfo = ServiceLocator.Current.GetInstance<IPlatformProvider>().PlatformInfos.Single(p => p.Name == cached.Repository.Platform);
			var problems = await ServiceLocator.Current.GetInstance<ICCNetProblemsLoader>().GetProblemsAsync(String.Format(platformInfo.FarmTaskName, cached.Repository.Version)).ConfigureAwait(false);
			foreach(var test in cached.TestList) {
				test.Volunteer = problems.FirstOrDefault(p => p.TestName == test.NameWithNamespace)?.Volunteer;
			}
			return cached;
		}
		
		PlatformAndVersion CreatePlatformAndVersion(Repository repository) => new PlatformAndVersion(repository.Platform, repository.Version);
		
		async Task<TestInfoCached> LoadTestsCoreAsync(MinioRepository minioRepository) {
			if(MinioPathCache.TryGetValue(CreatePlatformAndVersion(minioRepository.Repository), out var cache) && cache.RealUrl == minioRepository.Path) {
				await ActualizeTestsAsync(cache.TestList);
				return cache;
			}

			var allTasks = new List<Task<TestInfo>>();
			var corpDirTestInfoContainer = await LoadForRepositoryAsync(minioRepository);
			foreach(var corpDirTestInfo in corpDirTestInfoContainer.FailedTests) {
				var info = corpDirTestInfo;
				var testInfoTask = LoadTestInfo(minioRepository.Repository, info); 
				allTasks.Add(testInfoTask);
			}

			var result = (await Task.WhenAll(allTasks)).ToList();
			return MinioPathCache[CreatePlatformAndVersion(minioRepository.Repository)] = new TestInfoCached(minioRepository.Repository, minioRepository.Path, result, corpDirTestInfoContainer);
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

		async Task<TestInfo> LoadTestInfo(Repository repository, CorpDirTestInfo corpDirTestInfo) {
			loggingService.SendMessage($"Start load test v{corpDirTestInfo.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
			var testInfo = TryCreateTestInfo(repository, corpDirTestInfo);
			loggingService.SendMessage($"End load test v{corpDirTestInfo.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
			if(testInfo != null)
				await UpdateTestStatusAsync(testInfo);
			loadingProgressController.IncreaseProgress(1);
			return testInfo;
		}

		static bool GetBoolTestParameter(CorpDirTestInfo corpDirTestInfo, string parameter) {
			return corpDirTestInfo.AdditionalParameters.FirstOrDefault(x => x.Name == parameter && x.Value == "True") != null;
		}
		
		static TestInfo TryCreateTestInfo(Repository repository, CorpDirTestInfo corpDirTestInfo) {
			var testInfo = new TestInfo(repository);
			testInfo.Version = corpDirTestInfo.Version;
			testInfo.Name = corpDirTestInfo.TestName;
			testInfo.TeamName = corpDirTestInfo.TeamName;
			if(corpDirTestInfo.AdditionalParameters.FirstOrDefault(p => p.Name == "Dpi")?.Value is var dpi_str && Int32.TryParse(dpi_str, out var dpi))
				testInfo.Dpi = dpi;
			testInfo.NameWithNamespace = corpDirTestInfo.TestNameWithNamespace;
			testInfo.ResourcesFullPath = corpDirTestInfo.ResourcesFullPath;
			testInfo.Theme = corpDirTestInfo.ThemeName;
			testInfo.Optimized = GetBoolTestParameter(corpDirTestInfo, "Optimized");
			testInfo.Colorized = GetBoolTestParameter(corpDirTestInfo, "Colorized");
			//todo: found new parameter error
			if(corpDirTestInfo.TeamName == CorpDirTestInfo.ErrorName) {
				testInfo.Valid = TestState.Error;
				testInfo.TextDiffLazy = new Lazy<string>(() => corpDirTestInfo.ErrorText);
				testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
				return testInfo;
			}

			testInfo.PredefinedImageDiffsCount = corpDirTestInfo.DiffCount;

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

			var xmlPath = GetActualXmlFilePath(actualTestResourceName, test);
			var imagePath = GetActualImageFilePath(actualTestResourceName, test);
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
			if(!imageFixed) {
				foreach(var blinkShaPath in GetAllBlinkingSha(Path.ChangeExtension(imagePath, "").TrimEnd('.'), ".png")) {
					if(CompareSHA256(test.ImageCurrentSha, LoadBytes(blinkShaPath))) {
						imageFixed = true;
						break;
					}
				}
			}

			var textEquals = CompareSHA256(test.TextBeforeSha, test.TextCurrentSha);
			var textFixed = textEquals;
			if(!textFixed)
				textFixed = CompareSHA256(LoadBytes(xmlPath + ".sha"), test.TextCurrentSha);
			if(!textFixed) {
				foreach(var blinkShaPath in GetAllBlinkingSha(Path.ChangeExtension(xmlPath, "").TrimEnd('.'), ".xml")) {
					if(CompareSHA256(test.TextCurrentSha, LoadBytes(blinkShaPath))) {
						textFixed = true;
						break;
					}
				}
			}

			if(imageFixed && textFixed) {
				test.Valid = TestState.Fixed;
				return;
			}

			test.Valid = TestState.Valid;
		}

		string[] GetAllBlinkingSha(string testResourceName, string extension) {
			return Directory.GetFiles(Path.GetDirectoryName(testResourceName), $"{Path.GetFileName(testResourceName)}--*{extension}.sha");
		}
		string GetTestResourceName(TestInfo test, bool checkDirectoryExists) {
			if(!Directory.Exists(test.ResourcesFullPath)) {
				if(checkDirectoryExists) {
					test.LogDirectoryNotFound(test.ResourcesFullPath);
					return null;
				}
				Directory.CreateDirectory(test.ResourcesFullPath);
			}
			return Path.Combine(test.ResourcesFullPath, test.Theme);
		}

		static string GetBlinkingFilePath(string testResourceName, string extension) {
			var result = testResourceName;
			var i = 1;
			while(true) {
				if(i == 10)
					return null;
				result = $"{testResourceName}--{i++}";
				if(!File.Exists(result + extension + ".sha"))
					return result;
			}
		}
		static string GetXmlFilePath(string testResourceName, TestInfo test) {
			if(test.Optimized)
				testResourceName += "_optimized";
			if(test.CommitAsBlinking)
				testResourceName = GetBlinkingFilePath(testResourceName, ".xml");
			if(testResourceName == null)
				return null;
			return testResourceName + ".xml";
		}
		static string GetImageFilePath(string testResourceName, TestInfo test) {
			if(test.CommitAsBlinking)
				testResourceName = GetBlinkingFilePath(testResourceName, ".png");
			if(testResourceName == null)
				return null;
			return testResourceName + ".png";
		}
		
		static string GetActualXmlFilePath(string testResourceName, TestInfo test) {
			if(test.Optimized)
				testResourceName += "_optimized";
			var xmlPath = testResourceName + ".xml";
			return !CheckFileExistsAndLog(test, xmlPath) ? null : xmlPath;
		}
		static string GetActualImageFilePath(string testResourceName, TestInfo test) {
			var imagePath = testResourceName + ".png";
			return CheckFileExistsAndLog(test, imagePath) ? imagePath : null;
		}

		static bool CheckFileExistsAndLog(TestInfo test, string filePath) {
			if(File.Exists(filePath)) return true;
			test.LogFileNotFound(filePath);
			return false;
		}

		static bool SafeDeleteFile(string path, Func<string, bool> checkoutFunc) {
			if(!File.Exists(path))
				return true;
			if((File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
				if(!checkoutFunc(path)) return false;
				if((File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					return false;
			}
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