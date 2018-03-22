using CommonServiceLocator;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Native;
using DXVisualTestFixer.Services;
using DXVisualTestFixer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DXVisualTestFixer.Core {
    public static class TestsService {
        public static List<TestInfo> LoadParrallel(List<FarmTaskInfo> farmTasks, LoadingProgressController loadingProgressController) {
            //Team team = new Team();
            //team.Name = "ThemedWindow";
            //team.Version = "18.1";
            //team.TeamInfos = new List<TeamInfo>() { new TeamInfo() { Dpi = 96, ServerFolderName = "4DXThemedWindow_dpi_96", TestResourcesPath = @"Images_dpi96\WpfCore\ThemedWindowImages", Optimized = false } };
            //var s = TeamConfigsReader.SaveConfig(team);
            //File.WriteAllText($"c:\\1\\myConfig.config", s);
            //foreach(var version in Repository.Versions) {
            //    foreach(var team in Teams) {
            //        TeamConfigsReader r = new TeamConfigsReader();
            //        team.Version = version;
            //        var s = r.SaveConfig(team);
            //        File.WriteAllText($"c:\\1\\{version}_{team.Name}.config", s);
            //    }
            //}
            List<TestInfo> result = new List<TestInfo>();
            List<Task<TestInfo>> allTasks = new List<Task<TestInfo>>();
            foreach(CorpDirTestInfo corpDirTestInfo in LoadFromFarmTaskInfoParallel(farmTasks, loadingProgressController)) {
                CorpDirTestInfo info = corpDirTestInfo;
                allTasks.Add(Task.Factory.StartNew<TestInfo>(() => LoadTestInfo(info, loadingProgressController)));
            }
            Task.WaitAll(allTasks.ToArray());
            allTasks.ForEach(t => { if(t.Result != null) result.Add(t.Result); });
            return result;
        }
        static List<CorpDirTestInfo> LoadFromFarmTaskInfoParallel(List<FarmTaskInfo> farmTasks, LoadingProgressController loadingProgressController) {
            loadingProgressController.Enlarge(farmTasks.Count);
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"Collecting tests information from farm");
            List<CorpDirTestInfo> corpDirTestInfoList = new List<CorpDirTestInfo>();
            List<Task<List<CorpDirTestInfo>>> allTasks = new List<Task<List<CorpDirTestInfo>>>();
            foreach(FarmTaskInfo farmTaskInfo in farmTasks) {
                FarmTaskInfo info = farmTaskInfo;
                allTasks.Add(Task.Factory.StartNew<List<CorpDirTestInfo>>(() => LoadFromFarmTaskInfo(info, loadingProgressController)));
            }
            Task.WaitAll(allTasks.ToArray());
            allTasks.ForEach(t => corpDirTestInfoList.AddRange(t.Result));
            loadingProgressController.Flush();
            loadingProgressController.Enlarge(corpDirTestInfoList.Count);
            return corpDirTestInfoList;
        }
        static List<CorpDirTestInfo> LoadFromFarmTaskInfo(FarmTaskInfo farmTaskInfo, LoadingProgressController loadingProgressController) {
            List<CorpDirTestInfo> taskTestInfos = TestLoader.LoadFromInfo(farmTaskInfo);
            loadingProgressController.IncreaseProgress(1);
            return taskTestInfos;
        }
        static TestInfo LoadTestInfo(CorpDirTestInfo corpDirTestInfo, LoadingProgressController loadingProgressController) {
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"Start load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
            TestInfo testInfo = TryCreateTestInfo(corpDirTestInfo);
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"End load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
            if(testInfo != null) {
                if(testInfo.Valid != TestState.Error)
                    testInfo.Valid = TestStatus(testInfo);
                loadingProgressController.IncreaseProgress(1);
                return testInfo;
            }
            loadingProgressController.IncreaseProgress(1);
            return null;
        }

        static TestInfo TryCreateTestInfo(CorpDirTestInfo corpDirTestInfo) {
            TestInfo testInfo = new TestInfo();
            testInfo.Version = corpDirTestInfo.FarmTaskInfo.Repository.Version;
            testInfo.Name = corpDirTestInfo.TestName;
            testInfo.NameWithNamespace = corpDirTestInfo.TestNameWithNamespace;
            testInfo.ResourceFolderName = corpDirTestInfo.ResourceFolderName;
            if(corpDirTestInfo.TeamName == CorpDirTestInfo.ErrorTeamName) {
                testInfo.Valid = TestState.Error;
                testInfo.TextDiff = "+" + testInfo.Name + Environment.NewLine + Environment.NewLine + corpDirTestInfo. ErrorText;
                testInfo.Theme = "Error";
                testInfo.Dpi = 0;
                testInfo.Team = new Team() { Name = CorpDirTestInfo.ErrorTeamName, Version = corpDirTestInfo.FarmTaskInfo.Repository.Version };
                return testInfo;
            }
            TeamInfo info = null;
            testInfo.Team = TeamConfigsReader.GetTeam(corpDirTestInfo.FarmTaskInfo.Repository.Version, corpDirTestInfo.ServerFolderName, out info);
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
        
        static void LoadTextFile(string path, Action<string> saveAction) {
            string pathWithExtension = Path.ChangeExtension(path, ".xml");
            if(!File.Exists(pathWithExtension)) {
                //log
                Debug.WriteLine("fire LoadTextFile");
                return;
            }
            saveAction(File.ReadAllText(pathWithExtension));
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
            string differences = null;
            if(!IsTextEquals(testInfo.TextBefore, testInfo.TextCurrent, out differences))
                testInfo.TextDiff = differences;
        }
        static bool IsTextEquals(string left, string right, out string diff) {
            diff = null;
            if(left == right)
                return true;
            diff = BuildDefferences(left, right);
            return false;
        }

        static string BuildDefferences(string left, string right) {
            StringBuilder sb = new StringBuilder();
            string[] leftArr = left.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string[] rightArr = right.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for(int line = 0; line < Math.Min(leftArr.Length, rightArr.Length); line++) {
                string leftstr = leftArr[line];
                string rightstr = rightArr[line];
                if(leftstr == rightstr) {
                    sb.AppendLine(leftstr);
                    continue;
                }
                sb.AppendLine("-" + leftstr);
                sb.AppendLine("+" + rightstr);
            }
            return sb.ToString();
        }

        static bool IsImageEquals(byte[] left, byte[] right) {
            Bitmap imgLeft = null;
            using(MemoryStream s = new MemoryStream(left)) {
                imgLeft = Image.FromStream(s) as Bitmap;
            }
            Bitmap imgRight = null;
            using(MemoryStream s = new MemoryStream(right)) {
                imgRight = Image.FromStream(s) as Bitmap;
            }
            return ImageComparer.CompareMemCmp(imgLeft, imgRight);
        }
        static bool LoadImage(string path, Action<byte[]> saveAction) {
            if(!File.Exists(path)) {
                Debug.WriteLine("fire LoadImage");
                return false;
            }
            saveAction(File.ReadAllBytes(path));
            return true;
        }

        public static TestState TestStatus(TestInfo test) {
            TestState result = TestState.Valid;
            string actualTestResourceName = GetTestResourceName(test, true);
            if(actualTestResourceName == null) {
                return TestState.Invalid;
            }
            string xmlPath = GetXmlFilePath(actualTestResourceName, test, true);
            if(xmlPath == null) {
                result = TestState.Invalid;
            }
            string imagePath = GetImageFilePath(actualTestResourceName, test, true);
            if(imagePath == null) {
                result = TestState.Invalid;
            }
            if(result == TestState.Invalid)
                return result;
            if(!IsTextEquals(test.TextCurrent, File.ReadAllText(xmlPath), out _)) {
                return TestState.Valid;
            }
            byte[] imageSource = null;
            if(!LoadImage(imagePath, img => imageSource = img)) {
                test.LogCustomError($"File Can not load: \"{imagePath}\"");
                return TestState.Invalid;
            }
            if(IsImageEquals(test.ImageCurrentArr, imageSource))
                return TestState.Fixed;
            return result;
        }

        static string GetTestResourceName(TestInfo test, bool checkDirectoryExists) {
            var repository = ConfigSerializer.GetConfig().Repositories.Where(r => r.Version == test.Version).FirstOrDefault();
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

        public static bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc) {
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

    public enum TestState {
        Valid, Invalid, Fixed, Error
    }
}
