using DXVisualTestFixer.Common;
using DXVisualTestFixer.Core.Configuration;
using DXVisualTestFixer.Native;
using HtmlAgilityPack;
using Microsoft.Practices.ServiceLocation;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    class TestInfoCached {
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
    class TestInfoContainer : ITestInfoContainer {
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

            List<int> diffs = new List<int>();
            TestList.ForEach(t => diffs.Add(t.ImageDiffsCount));
            diffs.Sort();
            diffs = diffs.Distinct().ToList();
            Dictionary<int, int> problems = new Dictionary<int, int>();
            int proplemNumber = 1;
            int currentD = 0;
            foreach(var d in diffs) {
                if(currentD * 1.2d < d) {
                    problems.Add(currentD, proplemNumber++);
                    currentD = d;
                }
            }
            if(!problems.ContainsKey(currentD))
                problems.Add(currentD, proplemNumber++);
            Dictionary<int, HashSet<string>> namedProblems = new Dictionary<int, HashSet<string>>();
            foreach(var d in problems) {
                foreach(var test in TestList) {
                    if(test.ImageDiffsCount < d.Key * 1.2d && test.Problem == int.MinValue) {
                        test.Problem = d.Value;
                        HashSet<string> tests = null;
                        if(!namedProblems.TryGetValue(d.Value, out tests))
                            namedProblems[d.Value] = tests = new HashSet<string>();
                        if(!tests.Contains(test.Team.Name))
                            tests.Add(test.Team.Name);
                    }
                }
            }
            foreach(var test in TestList) {
                HashSet<string> namedProblemsList = null;
                if(!namedProblems.TryGetValue(test.Problem, out namedProblemsList))
                    namedProblemsList = new HashSet<string>();
                var problemNumber = string.Format("{0:D2}", test.Problem);
                test.ProblemName = $"#{problemNumber} ({string.Join(", ", namedProblemsList)})";
            }
        }
        int CalculateImageDiffsCount(TestInfo test) {
            if(test.ImageDiffArrLazy.Value == null)
                return int.MaxValue;
            if(TestsService.CompareSHA256(test.ImageBeforeSHA, test.ImageCurrentSHA))
                return int.MaxValue;
            if(test.ImageBeforeSHA == null || test.ImageCurrentSHA == null)
                return int.MaxValue;
            using(var imgDiff = ImageHelper.CreateImageFromArray(test.ImageDiffArrLazy.Value)) {
                return ImageHelper.RedCount(imgDiff);
            }
        }
    }
    public class TestsService : BindableBase, ITestsService {
        readonly ILoadingProgressController loadingProgressController;
        readonly IConfigSerializer configSerializer;
        readonly ILoggingService loggingService;
        readonly IFarmIntegrator farmIntegrator;

        Dictionary<IFarmTaskInfo, TestInfoCached> RealUrlCache = new Dictionary<IFarmTaskInfo, TestInfoCached>();

        string _CurrentFilter = null;

        public TestsService(ILoadingProgressController loadingProgressController, IConfigSerializer configSerializer, ILoggingService loggingService, IFarmIntegrator farmIntegrator) {
            this.loadingProgressController = loadingProgressController;
            this.configSerializer = configSerializer;
            this.loggingService = loggingService;
            this.farmIntegrator = farmIntegrator;
        }

        public ITestInfoContainer ActualState { get; private set; }

        public string CurrentFilter {
            get { return _CurrentFilter; }
            set {
                _CurrentFilter = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentFilter)));
            }
        }

        public async Task UpdateTests(INotificationService notificationService) {
            CurrentFilter = null;
            var allTasks = await farmIntegrator.GetAllTasks(configSerializer.GetConfig().GetLocalRepositories().ToArray());
            var actualState = await LoadTestsAsync(allTasks, notificationService);
            loggingService.SendMessage($"Start updating problems");
            await ((TestInfoContainer)actualState).UpdateProblems();
            loggingService.SendMessage($"Almost there");
            ActualState = actualState;
        }

        async Task<ITestInfoContainer> LoadTestsAsync(List<IFarmTaskInfo> farmTasks, INotificationService notificationService) {
            loadingProgressController.Flush();
            loadingProgressController.Enlarge(farmTasks.Count);
            loggingService.SendMessage($"Collecting tests information from farm");
            List<Task<TestInfoCached>> allTasks = new List<Task<TestInfoCached>>();
            foreach(IFarmTaskInfo farmTaskInfo in farmTasks) {
                if(String.IsNullOrEmpty(farmTaskInfo.Url)) {
                    notificationService?.DoNotification($"Farm Task Not Found For {farmTaskInfo.Repository.Version}", $"Farm Task {farmTaskInfo.Repository.Version} from path {farmTaskInfo.Repository.Path} does not found. Maybe new branch created, but corresponding farm task missing. It well be added later. Otherwise, contact app owner for details.", System.Windows.MessageBoxImage.Information);
                    continue;
                }
                IFarmTaskInfo info = farmTaskInfo;
                var task = LoadTestsCoreAsync(info);
                allTasks.Add(task);
            }
            TestInfoContainer result = new TestInfoContainer();
            result.Timings.Clear();
            foreach(TestInfoCached cached in await Task.WhenAll(allTasks.ToArray()).ConfigureAwait(false)) {
                result.TestList.AddRange(cached.TestList);
                result.UsedFilesLinks[cached.Repository] = cached.UsedFilesLinks;
                result.ElapsedTimes[cached.Repository] = cached.ElapsedTimes.Cast<IElapsedTimeInfo>().ToList();
                result.Teams[cached.Repository] = cached.Teams;
                result.Timings.Add(new TimingInfo(cached.Repository, cached.SourcesBuildTime, cached.TestsBuildTime));
            }
            return result;
        }
        async Task<TestInfoCached> LoadTestsCoreAsync(IFarmTaskInfo farmTaskInfo) {
            if(RealUrlCache.TryGetValue(farmTaskInfo, out TestInfoCached cache)) {
                if(cache.RealUrl == farmTaskInfo.Url) {
                    ActualizeTests(cache.TestList);
                    return cache;
                }
            }
            List<Task<TestInfo>> allTasks = new List<Task<TestInfo>>();
            CorpDirTestInfoContainer corpDirTestInfoContainer = LoadFromFarmTaskInfo(farmTaskInfo, farmTaskInfo.Url);
            foreach(var corpDirTestInfo in corpDirTestInfoContainer.FailedTests) {
                CorpDirTestInfo info = corpDirTestInfo;
                allTasks.Add(Task.Factory.StartNew<TestInfo>(() => LoadTestInfo(info, corpDirTestInfoContainer.Teams)));
            }
            List<TestInfo> result = (await Task.WhenAll(allTasks.ToArray()).ConfigureAwait(false)).ToList();
            TestInfoCached cachedValue = new TestInfoCached(farmTaskInfo.Repository, farmTaskInfo.Url, result, corpDirTestInfoContainer);
            RealUrlCache[farmTaskInfo] = cachedValue;
            return cachedValue;
        }

        void ActualizeTests(List<TestInfo> testList) {
            List<Task> allTasks = new List<Task>();
            foreach(TestInfo test in testList) {
                TestInfo t = test;
                allTasks.Add(Task.Factory.StartNew(() => UpdateTestStatus(test)));
            }
            Task.WaitAll(allTasks.ToArray());
        }

        CorpDirTestInfoContainer LoadFromFarmTaskInfo(IFarmTaskInfo farmTaskInfo, string realUrl) {
            CorpDirTestInfoContainer corpDirTestInfoContainer = TestLoader.LoadFromInfo(farmTaskInfo, realUrl);
            loadingProgressController.Enlarge(corpDirTestInfoContainer.FailedTests.Count);
            return corpDirTestInfoContainer;
        }

        TestInfo LoadTestInfo(CorpDirTestInfo corpDirTestInfo, List<Team> teams) {
            loggingService.SendMessage($"Start load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
            TestInfo testInfo = TryCreateTestInfo(corpDirTestInfo, teams);
            loggingService.SendMessage($"End load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
            if(testInfo != null) {
                UpdateTestStatus(testInfo);
                loadingProgressController.IncreaseProgress(1);
                return testInfo;
            }
            loadingProgressController.IncreaseProgress(1);
            return null;
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
            TestInfo testInfo = new TestInfo(corpDirTestInfo.FarmTaskInfo.Repository);
            testInfo.Version = corpDirTestInfo.FarmTaskInfo.Repository.Version;
            testInfo.Name = corpDirTestInfo.TestName;
            testInfo.AdditionalParameters = corpDirTestInfo.AdditionalParameter;
            testInfo.NameWithNamespace = corpDirTestInfo.TestNameWithNamespace;
            testInfo.ResourceFolderName = corpDirTestInfo.ResourceFolderName;
            if(corpDirTestInfo.TeamName == CorpDirTestInfo.ErrorTeamName) {
                testInfo.Valid = TestState.Error;
                testInfo.TextDiffLazy = new Lazy<string>(() => "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText);
                testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
                testInfo.Theme = "Error";
                testInfo.Dpi = 0;
                testInfo.Team = new Team() { Name = CorpDirTestInfo.ErrorTeamName, Version = corpDirTestInfo.FarmTaskInfo.Repository.Version };
                return testInfo;
            }
            TeamInfo info = null;
            Team team = testInfo.Team = GetTeam(teams, corpDirTestInfo.FarmTaskInfo.Repository.Version, corpDirTestInfo.ServerFolderName, out info);
            if(team == null) {
                testInfo.Valid = TestState.Error;
                testInfo.TextDiffLazy = new Lazy<string>(() => "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText);
                testInfo.TextDiffFullLazy = new Lazy<string>(() => string.Empty);
                testInfo.Theme = "Error";
                testInfo.Dpi = 0;
                testInfo.Team = new Team() { Name = CorpDirTestInfo.ErrorTeamName, Version = corpDirTestInfo.FarmTaskInfo.Repository.Version };
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
            testInfo.TextBeforeSHA = corpDirTestInfo.InstantTextEditSHA ?? LoadBytes(corpDirTestInfo.InstantTextEditSHAPath);
            testInfo.TextCurrentLazy = new Lazy<string>(() => LoadTextFile(corpDirTestInfo.CurrentTextEditPath));
            testInfo.TextCurrentSHA = corpDirTestInfo.CurrentTextEditSHA ?? LoadBytes(corpDirTestInfo.CurrentTextEditSHAPath);
            InitializeTextDiff(testInfo);

            testInfo.ImageBeforeArrLazy = new Lazy<byte[]>(() => LoadBytes(corpDirTestInfo.InstantImagePath));
            testInfo.ImageBeforeSHA = corpDirTestInfo.InstantImageSHA ?? LoadBytes(corpDirTestInfo.InstantImageSHAPath);
            testInfo.ImageCurrentArrLazy = new Lazy<byte[]>(() => LoadBytes(corpDirTestInfo.CurrentImagePath));
            testInfo.ImageCurrentSHA = corpDirTestInfo.CurrentImageSHA ?? LoadBytes(corpDirTestInfo.CurrentImageSHAPath);

            testInfo.ImageDiffArrLazy = new Lazy<byte[]>(() => LoadBytes(corpDirTestInfo.ImageDiffPath));
            //if(TestValid(testInfo))
            //    testInfo.Valid = true;
            return testInfo;
        }

        static object lockLoadFromDisk = new object();
        static object lockLoadFromNet = new object();

        static string LoadTextFile(string path) {
            string pathWithExtension = Path.ChangeExtension(path, ".xml");
            if(!File.Exists(pathWithExtension)) {
                //log
                //Debug.WriteLine("fire LoadTextFile");
                return null;
            }
            string text;
            if(!path.StartsWith(@"\\corp")) {
                lock(lockLoadFromDisk) {
                    text = File.ReadAllText(pathWithExtension);
                }
            }
            else {
                lock(lockLoadFromNet) {
                    text = File.ReadAllText(pathWithExtension);
                }
            }
            return text;
        }
        static byte[] LoadBytes(string path) {
            if(!File.Exists(path))
                return null;
            byte[] bytes;
            if(!path.StartsWith(@"\\corp")) {
                lock(lockLoadFromDisk) {
                    bytes = File.ReadAllBytes(path);
                }
            }
            else {
                lock(lockLoadFromNet) {
                    bytes = File.ReadAllBytes(path);
                }
            }
            return bytes;
        }
        public static bool CompareSHA256(byte[] instant, byte[] current) {
            if(instant == null || current == null)
                return false;
            if(instant.Length != current.Length)
                return false;
            for(int i = 0; i < instant.Length; i++)
                if(instant[i] != current[i])
                    return false;
            return true;
        }
        static byte[] GetSHA256(Stream stream, bool dispose = false) {
            if(stream == null)
                return null;
            stream.Seek(0, SeekOrigin.Begin);
            byte[] result = null;
            using(SHA256 sha256Hash = SHA256.Create()) {
                result = sha256Hash.ComputeHash(stream);
            }
            stream.Seek(0, SeekOrigin.Begin);
            if(dispose)
                stream.Dispose();
            return result;
        }
        static bool IsTextEquals(string left, string right, out string diff, out string fullDiff) {
            if(left == right) {
                diff = "+Texts Equals";
                fullDiff = null;
                return true;
            }
            TextDifferenceInfo info = BuildDefferences(left, right);
            diff = info.DiffCompact;
            fullDiff = info.DiffFull;
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

        static TextDifferenceInfo BuildDefferences(string left, string right) {
            StringBuilder sbFull = new StringBuilder();
            string[] leftArr = left.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string[] rightArr = right.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            List<int> diffLines = new List<int>();
            for(int line = 0; line < Math.Min(leftArr.Length, rightArr.Length); line++) {
                string leftstr = leftArr[line];
                string rightstr = rightArr[line];
                if(leftstr == rightstr) {
                    sbFull.AppendLine(leftstr);
                    continue;
                }
                diffLines.Add(line);
                sbFull.AppendLine("-" + leftstr);
                sbFull.AppendLine("+" + rightstr);
            }

            return new TextDifferenceInfo(BuildCompactDifferences(leftArr, rightArr, diffLines), sbFull.ToString());
        }

        static string BuildCompactDifferences(string[] leftArr, string[] rightArr, List<int> diffLines) {
            if(diffLines.Count == 0)
                return null;
            StringBuilder sbCompact = new StringBuilder();
            bool inRegion = false;
            foreach(int line in diffLines) {
                if(line >= Math.Min(leftArr.Length, rightArr.Length))
                    break;
                if(!inRegion) {
                    inRegion = true;
                    if(line > 0) sbCompact.AppendLine("<...>");
                    for(int i = line - 2; i < line; i++) {
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
                    for(int i = line + 1; i < line + 3; i++) {
                        if(i < 0)
                            continue;
                        if(i < leftArr.Length) sbCompact.AppendLine(leftArr[i]);
                        if(i < rightArr.Length) sbCompact.AppendLine(rightArr[i]);
                    }
                    if((diffLines.IndexOf(line) == diffLines.Count - 1) && line < Math.Min(leftArr.Length, rightArr.Length)) sbCompact.AppendLine("<...>");
                }
            }
            return sbCompact.ToString();
        }
        static void InitializeTextDiff(TestInfo testInfo) {
            testInfo.TextDiffLazy = new Lazy<string>(() => String.Empty);
            testInfo.TextDiffFullLazy = new Lazy<string>(() => String.Empty);
            if(CompareSHA256(testInfo.TextBeforeSHA, testInfo.TextCurrentSHA))
                return;

            testInfo.TextDiffLazy = new Lazy<string>(() => {
                if(String.IsNullOrEmpty(testInfo.TextBeforeLazy.Value) && String.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
                    return String.Empty;
                if(String.IsNullOrEmpty(testInfo.TextBeforeLazy.Value))
                    return testInfo.TextCurrentLazy.Value;
                if(String.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
                    return testInfo.TextBeforeLazy.Value;
                if(!IsTextEquals(testInfo.TextBeforeLazy.Value, testInfo.TextCurrentLazy.Value, out string differences, out string fullDifferences))
                    return differences;
                return String.Empty;
            });
            testInfo.TextDiffFullLazy = new Lazy<string>(() => {
                if(String.IsNullOrEmpty(testInfo.TextBeforeLazy.Value) && String.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
                    return String.Empty;
                if(String.IsNullOrEmpty(testInfo.TextBeforeLazy.Value))
                    return testInfo.TextCurrentLazy.Value;
                if(String.IsNullOrEmpty(testInfo.TextCurrentLazy.Value))
                    return testInfo.TextBeforeLazy.Value;
                if(!IsTextEquals(testInfo.TextBeforeLazy.Value, testInfo.TextCurrentLazy.Value, out string differences, out string fullDifferences))
                    return fullDifferences;
                return String.Empty;
            });
        }

        void UpdateTestStatus(TestInfo test) {
            if(test.Valid == TestState.Error)
                return;
            test.Valid = TestState.Valid;
            string actualTestResourceName = GetTestResourceName(test, true);
            if(actualTestResourceName == null) {
                test.Valid = TestState.Invalid;
                return;
            }
            string xmlPath = GetXmlFilePath(actualTestResourceName, test, true);
            string imagePath = GetImageFilePath(actualTestResourceName, test, true);
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
                test.LogCustomError($"Is it new test?");
                return;
            }
            if(CompareSHA256(test.ImageBeforeSHA, test.ImageCurrentSHA))
                test.ImageEquals = true;
            bool imageFixed = test.ImageEquals;
            if(!imageFixed)
                imageFixed = CompareSHA256(LoadBytes(imagePath + ".sha"), test.ImageCurrentSHA);
            bool textEquals = CompareSHA256(test.TextBeforeSHA, test.TextCurrentSHA);
            bool textFixed = textEquals;
            if(!textFixed)
                textFixed = CompareSHA256(LoadBytes(xmlPath + ".sha"), test.TextCurrentSHA);
            if(imageFixed && textFixed) {
                test.Valid = TestState.Fixed;
                return;
            }
            test.Valid = TestState.Valid;
        }
        public string GetResourcePath(Repository repository, string relativePath) {
            return Path.Combine(repository.Path, relativePath);
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
            string testResourcesPath = test.Optimized && test.TeamInfo.TestResourcesPath_Optimized != null ? test.TeamInfo.TestResourcesPath_Optimized : test.TeamInfo.TestResourcesPath;
            string actualTestResourcesPath = Path.Combine(repository.Path, testResourcesPath, test.ResourceFolderName);
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
            string xmlPath = testResourceName + ".xml";
            if(checkExists && !File.Exists(xmlPath)) {
                test.LogFileNotFound(xmlPath);
                return null;
            }
            return xmlPath;
        }
        static string GetImageFilePath(string testResourceName, TestInfo test, bool checkExists) {
            string imagePath = testResourceName + ".png";
            if(checkExists && !File.Exists(imagePath)) {
                test.LogFileNotFound(imagePath);
                return null;
            }
            return imagePath;
        }

        public bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc) {
            string actualTestResourceName = GetTestResourceName(test, false);
            string xmlPath = GetXmlFilePath(actualTestResourceName, test, false);
            string imagePath = GetImageFilePath(actualTestResourceName, test, false);
            if(imagePath == null || xmlPath == null) {
                return false;
            }
            if(!SafeDeleteFile(xmlPath, checkoutFunc))
                return false;
            if(!SafeDeleteFile(imagePath, checkoutFunc))
                return false;
            string xmlSHAPath = xmlPath + ".sha";
            if(!SafeDeleteFile(xmlSHAPath, checkoutFunc))
                return false;
            string imageSHAPath = imagePath + ".sha";
            if(!SafeDeleteFile(imageSHAPath, checkoutFunc))
                return false;
            File.WriteAllText(xmlPath, test.TextCurrentLazy.Value);
            if(test.TextCurrentSHA == null)
                test.TextCurrentSHA = GetSHA256(new MemoryStream(File.ReadAllBytes(xmlPath)));
            if(test.ImageCurrentSHA == null)
                test.ImageCurrentSHA = GetSHA256(new MemoryStream(test.ImageCurrentArrLazy.Value));
            File.WriteAllBytes(xmlSHAPath, test.TextCurrentSHA);
            File.WriteAllBytes(imagePath, test.ImageCurrentArrLazy.Value);
            File.WriteAllBytes(imageSHAPath, test.ImageCurrentSHA);
            return true;
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
    }
}
