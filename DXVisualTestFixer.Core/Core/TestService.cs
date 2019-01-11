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
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    class TestInfoCached {
        public TestInfoCached(Repository repository, string realUrl, List<TestInfo> testList, CorpDirTestInfoContainer container) {
            Repository = repository;
            RealUrl = realUrl;
            TestList = testList;
            UsedFiles = container.UsedFiles;
            ElapsedTimes = container.ElapsedTimes;
            Teams = container.Teams;
        }

        public Repository Repository { get; }
        public string RealUrl { get; }
        public List<TestInfo> TestList { get; }
        public List<string> UsedFiles { get; }
        public List<IElapsedTimeInfo> ElapsedTimes { get; }
        public List<Team> Teams { get; }
    }
    class TestInfoContainer : ITestInfoContainer {
        public TestInfoContainer() {
            TestList = new List<TestInfo>();
            UsedFiles = new Dictionary<Repository, List<string>>();
            ElapsedTimes = new Dictionary<Repository, List<IElapsedTimeInfo>>();
            Teams = new Dictionary<Repository, List<Team>>();
            ChangedTests = new List<TestInfo>();
        }

        public List<TestInfo> TestList { get; }
        public Dictionary<Repository, List<string>> UsedFiles { get; }
        public Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }
        public Dictionary<Repository, List<Team>> Teams { get; }
        public List<TestInfo> ChangedTests { get; }
        public void UpdateProblems() {
            Parallel.ForEach(TestList, test => {
                test.Problem = int.MinValue;
                test.ImageDiffsCount = CalculateImageDiffsCount(test);
            });
            List<int> diffs = new List<int>();
            TestList.ForEach(t => diffs.Add(t.ImageDiffsCount));
            diffs.Sort();
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
            if(test.ImageDiffArr == null || test.ImageBeforeArr == null || test.ImageCurrentArr == null)
                return int.MaxValue;
            using(var imgBefore = ImageHelper.CreateImageFromArray(test.ImageBeforeArr))
                using(var imgCurrent = ImageHelper.CreateImageFromArray(test.ImageCurrentArr)) {
                    if(imgBefore.Size != imgCurrent.Size)
                        return int.MaxValue;
                    return ImageHelper.DeltaUnsafe(imgBefore, imgCurrent);
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFilter)));
            }
        }

        public async Task UpdateTests(INotificationService notificationService) {
            CurrentFilter = null;
            var allTasks = await farmIntegrator.GetAllTasks(configSerializer.GetConfig().Repositories);
            ActualState = await LoadTestsAsync(allTasks, notificationService);
            ((TestInfoContainer)ActualState).UpdateProblems();
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
            foreach(TestInfoCached cached in await Task.WhenAll(allTasks.ToArray()).ConfigureAwait(false)) {
                result.TestList.AddRange(cached.TestList);
                result.UsedFiles[cached.Repository] = cached.UsedFiles;
                result.ElapsedTimes[cached.Repository] = cached.ElapsedTimes.Cast<IElapsedTimeInfo>().ToList();
                result.Teams[cached.Repository] = cached.Teams;
            }
            return result;
        }
        async Task<TestInfoCached> LoadTestsCoreAsync(IFarmTaskInfo farmTaskInfo) {
            string realUrl = await CapureRealUrl(farmTaskInfo.Url).ConfigureAwait(false);
            if(RealUrlCache.TryGetValue(farmTaskInfo, out TestInfoCached cache)) {
                if(cache.RealUrl == realUrl) {
                    ActualizeTests(cache.TestList);
                    return cache;
                }
            }
            List<Task<TestInfo>> allTasks = new List<Task<TestInfo>>();
            CorpDirTestInfoContainer corpDirTestInfoContainer = LoadFromFarmTaskInfo(farmTaskInfo, realUrl);
            foreach(var corpDirTestInfo in corpDirTestInfoContainer.FailedTests) {
                CorpDirTestInfo info = corpDirTestInfo;
                allTasks.Add(Task.Factory.StartNew<TestInfo>(() => LoadTestInfo(info, corpDirTestInfoContainer.Teams)));
            }
            List<TestInfo> result = (await Task.WhenAll(allTasks.ToArray()).ConfigureAwait(false)).ToList();
            TestInfoCached cachedValue = new TestInfoCached(farmTaskInfo.Repository, realUrl, result, corpDirTestInfoContainer);
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
        static Task<string> CapureRealUrl(string url) {
            return Task.Factory.StartNew<string>(() => {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmlSnippet = htmlWeb.Load(url);
                return htmlWeb.ResponseUri.ToString();
            });
        }

        TestInfo LoadTestInfo(CorpDirTestInfo corpDirTestInfo, List<Team> teams) {
            loggingService.SendMessage($"Start load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
            TestInfo testInfo = TryCreateTestInfo(corpDirTestInfo, teams ?? TeamConfigsReader.GetAllTeams());
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
            TestInfo testInfo = new TestInfo();
            testInfo.Version = corpDirTestInfo.FarmTaskInfo.Repository.Version;
            testInfo.Name = corpDirTestInfo.TestName;
            testInfo.NameWithNamespace = corpDirTestInfo.TestNameWithNamespace;
            testInfo.ResourceFolderName = corpDirTestInfo.ResourceFolderName;
            if(corpDirTestInfo.TeamName == CorpDirTestInfo.ErrorTeamName) {
                testInfo.Valid = TestState.Error;
                testInfo.TextDiff = "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText;
                testInfo.Theme = "Error";
                testInfo.Dpi = 0;
                testInfo.Team = new Team() { Name = CorpDirTestInfo.ErrorTeamName, Version = corpDirTestInfo.FarmTaskInfo.Repository.Version };
                return testInfo;
            }
            TeamInfo info = null;
            Team team = testInfo.Team = GetTeam(teams, corpDirTestInfo.FarmTaskInfo.Repository.Version, corpDirTestInfo.ServerFolderName, out info);
            if(team == null) {
                testInfo.Valid = TestState.Error;
                testInfo.TextDiff = "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo.ErrorText;
                testInfo.Theme = "Error";
                testInfo.Dpi = 0;
                testInfo.Team = new Team() { Name = CorpDirTestInfo.ErrorTeamName, Version = corpDirTestInfo.FarmTaskInfo.Repository.Version };
                return testInfo;
            }
            testInfo.TeamInfo = info;
            testInfo.Dpi = info.Dpi;
            testInfo.Theme = corpDirTestInfo.ThemeName;
            if(Convert.ToInt32(testInfo.Version.Split('.')[0]) < 18) {
                if(testInfo.TeamInfo.Optimized.HasValue)
                    testInfo.Optimized = !corpDirTestInfo.FarmTaskInfo.Url.Contains("UnoptimizedMode");
            }
            else {
                //new version
                if(testInfo.TeamInfo.Optimized.HasValue)
                    testInfo.Optimized = testInfo.TeamInfo.Optimized.Value;
            }
            LoadTextFile(corpDirTestInfo.InstantTextEditPath, s => { testInfo.TextBefore = s; });
            LoadTextFile(corpDirTestInfo.CurrentTextEditPath, s => { testInfo.TextCurrent = s; });
            BuildTextDiff(testInfo);
            LoadImage(corpDirTestInfo.InstantImagePath, s => { testInfo.ImageBeforeArr = s; });
            LoadImage(corpDirTestInfo.CurrentImagePath, s => { testInfo.ImageCurrentArr = s; });
            LoadImage(corpDirTestInfo.ImageDiffPath, s => { testInfo.ImageDiffArr = s; });
            //if(TestValid(testInfo))
            //    testInfo.Valid = true;
            return testInfo;
        }

        static object lockLoadFromDisk = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        static void LoadTextFile(string path, Action<string> saveAction) {
            string pathWithExtension = Path.ChangeExtension(path, ".xml");
            if(!File.Exists(pathWithExtension)) {
                //log
                //Debug.WriteLine("fire LoadTextFile");
                return;
            }
            string text;
            if(!path.StartsWith(@"\\corp")) {
                lock(lockLoadFromDisk) {
                    text = File.ReadAllText(pathWithExtension);
                }
            }
            else
                text = File.ReadAllText(pathWithExtension);
            saveAction(text);
        }
        static void BuildTextDiff(TestInfo testInfo) {
            if(String.IsNullOrEmpty(testInfo.TextBefore) && String.IsNullOrEmpty(testInfo.TextCurrent))
                return;
            if(String.IsNullOrEmpty(testInfo.TextBefore)) {
                testInfo.TextDiff = testInfo.TextCurrent;
                return;
            }
            if(String.IsNullOrEmpty(testInfo.TextCurrent)) {
                testInfo.TextDiff = testInfo.TextBefore;
                return;
            }
            if(!IsTextEquals(testInfo.TextBefore, testInfo.TextCurrent, out string differences, out string fullDifferences)) {
                testInfo.TextDiff = differences;
                testInfo.TextDiffFull = fullDifferences;
            }
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

        static bool IsImageEquals(byte[] left, byte[] right) {
            using(var imgLeft = ImageHelper.CreateImageFromArray(left))
                using(var imgRight = ImageHelper.CreateImageFromArray(right))
                    return ImageHelper.CompareUnsafe(imgLeft, imgRight);
        }
        static bool LoadImage(string path, Action<byte[]> saveAction) {
            if(!File.Exists(path)) {
                return false;
            }
            byte[] bytes;
            if(!path.StartsWith(@"\\corp")) {
                lock(lockLoadFromDisk) {
                    bytes = File.ReadAllBytes(path);
                }
            }
            else
                bytes = File.ReadAllBytes(path);
            saveAction(bytes);
            return true;
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
            if(xmlPath == null) {
                test.Valid = TestState.Invalid;
            }
            string imagePath = GetImageFilePath(actualTestResourceName, test, true);
            if(imagePath == null) {
                test.Valid = TestState.Invalid;
            }
            if(test.Valid == TestState.Invalid)
                return;
            if(test.ImageBeforeArr != null) {
                if(IsImageEquals(test.ImageCurrentArr, test.ImageBeforeArr))
                    test.ImageEquals = true;
            }
            if(!IsTextEquals(test.TextCurrent, File.ReadAllText(xmlPath), out _, out _)) {
                test.Valid = TestState.Valid;
                return;
            }
            byte[] imageSource = null;
            if(!LoadImage(imagePath, img => imageSource = img)) {
                test.LogCustomError($"File Can not load: \"{imagePath}\"");
                test.Valid = TestState.Invalid;
                return;
            }
            if(IsImageEquals(test.ImageCurrentArr, imageSource)) {
                test.Valid = TestState.Fixed;
                return;
            }
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
            string actualTestResourcesPath = Path.Combine(repository.Path, test.TeamInfo.TestResourcesPath, test.ResourceFolderName);
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
            File.WriteAllText(xmlPath, test.TextCurrent);
            File.WriteAllBytes(imagePath, test.ImageCurrentArr);
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
