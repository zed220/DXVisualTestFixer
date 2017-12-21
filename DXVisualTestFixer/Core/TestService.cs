using CommonServiceLocator;
using DXVisualTestFixer.Configuration;
using KellermanSoftware.CompareNetObjects;
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
        public static List<TestInfo> LoadParrallel(List<FarmTaskInfo> farmTasks) {
            //foreach(var version in Repository.Versions) {
            //    foreach(var team in Teams) {
            //        TeamConfigsReader r = new TeamConfigsReader();
            //        team.Version = version;
            //        var s = r.SaveConfig(team);
            //        File.WriteAllText($"c:\\1\\{version}_{team.Name}.config", s);
            //    }
            //}
            List<TestInfo> result = new List<TestInfo>();
            List<Task<List<TestInfo>>> allTasks = new List<Task<List<TestInfo>>>();
            foreach(FarmTaskInfo farmTaskInfo in farmTasks) {
                FarmTaskInfo info = farmTaskInfo;
                allTasks.Add(Task.Factory.StartNew<List<TestInfo>>(() => LoadFromFarmTaskInfo(info)));
            }
            Task.WaitAll(allTasks.ToArray());
            allTasks.ForEach(t => result.AddRange(t.Result));
            return result;
        }
        static List<TestInfo> LoadFromFarmTaskInfo(FarmTaskInfo farmTaskInfo) {
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"Load tests for {farmTaskInfo.Url}");
            List<TestInfo> result = new List<TestInfo>();
            foreach(CorpDirTestInfo corpDirTestInfo in TestLoader.LoadFromInfo(farmTaskInfo)) {
                ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"Start load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
                TestInfo testInfo = TryCreateTestInfo(corpDirTestInfo);
                ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"End load test v{corpDirTestInfo.FarmTaskInfo.Repository.Version} {corpDirTestInfo.TestName}.{corpDirTestInfo.ThemeName}");
                if(testInfo != null) {
                    testInfo.Valid = TestStatus(testInfo);
                    result.Add(testInfo);
                }
            }
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage($"Finish load tests for {farmTaskInfo.Url}");
            return result;
        }

        static TestInfo TryCreateTestInfo(CorpDirTestInfo corpDirTestInfo) {
            TestInfo testInfo = new TestInfo();
            testInfo.Version = corpDirTestInfo.FarmTaskInfo.Repository.Version;
            testInfo.Name = corpDirTestInfo.TestName;
            testInfo.Team = TeamConfigsReader.GetTeam(corpDirTestInfo.FarmTaskInfo.Repository.Version, corpDirTestInfo.TeamName);
            testInfo.Theme = corpDirTestInfo.ThemeName;
            if(testInfo.Team.SupportOptimized)
                testInfo.Optimized = !corpDirTestInfo.FarmTaskInfo.Url.Contains("UnoptimizedMode");
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
            CompareLogic compareLogic = new CompareLogic();
            ComparisonResult result = compareLogic.Compare(left, right);
            diff = null;
            if(!result.AreEqual)
                diff = result.DifferencesString;
            return result.AreEqual;
        }
        static bool IsImageEquals(byte[] left, byte[] right) {
            CompareLogic compareLogic = new CompareLogic();
            Bitmap imgLeft = null;
            using(MemoryStream s = new MemoryStream(left)) {
                imgLeft = Image.FromStream(s) as Bitmap;
            }
            Bitmap imgRight = null;
            using(MemoryStream s = new MemoryStream(right)) {
                imgRight = Image.FromStream(s) as Bitmap;
            }
            return IsImageEqualsCore(imgLeft, imgRight);
        }
        static bool IsImageEqualsCore(Bitmap firstImage, Bitmap secondImage) {
            if(firstImage == null || secondImage == null) {
                //log
                Debug.WriteLine("fire IsImageEqualsCore");
                return false;
            }
            if(firstImage.Width != secondImage.Width || firstImage.Height != secondImage.Height)
                return false;
            for(int i = 0; i < firstImage.Width; i++) {
                for(int j = 0; j < firstImage.Height; j++) {
                    string firstPixel = firstImage.GetPixel(i, j).ToString();
                    string secondPixel = secondImage.GetPixel(i, j).ToString();
                    if(firstPixel != secondPixel) {
                        return false;
                    }
                }
            }
            return true;
        }

        static bool LoadImage(string path, Action<byte[]> saveAction) {
            if(!File.Exists(path)) {
                //log
                Debug.WriteLine("fire LoadImage");
                return false;
            }
            saveAction(File.ReadAllBytes(path));
            return true;
        }

        public static TestState TestStatus(TestInfo test) {
            TestState result = TestState.Valid;
            string actualTestResourceName = GetTestResourceName(test);
            if(actualTestResourceName == null) {
                return TestState.Invalid;
            }
            string xmlPath = GetXmlFilePath(actualTestResourceName, test);
            if(xmlPath == null) {
                result = TestState.Invalid;
            }
            string imagePath = GetImageFilePath(actualTestResourceName, test);
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

        static string GetTestResourceName(TestInfo test) {
            var repository = ConfigSerializer.GetConfig().Repositories.Where(r => r.Version == test.Version).FirstOrDefault();
            if(repository == null) {
                test.LogCustomError($"Config not found for version \"{test.Version}\"");
                return null;
            }
            string actualTestResourcesPath = Path.Combine(repository.Path, test.Team.TestResourcesPath, test.Name);
            if(!Directory.Exists(actualTestResourcesPath)) {
                test.LogDirectoryNotFound(actualTestResourcesPath);
                return null;
            }
            return Path.Combine(actualTestResourcesPath, test.Theme);
        }
        static string GetXmlFilePath(string testResourceName, TestInfo test) {
            if(test.Optimized)
                testResourceName = testResourceName + "_optimized";
            string xmlPath = Path.ChangeExtension(testResourceName, "xml");
            if(!File.Exists(xmlPath)) {
                test.LogFileNotFound(xmlPath);
                return null;
            }
            return xmlPath;
        }
        static string GetImageFilePath(string testResourceName, TestInfo test) {
            string imagePath = Path.ChangeExtension(testResourceName, "png");
            if(!File.Exists(imagePath)) {
                test.LogFileNotFound(imagePath);
                return null;
            }
            return imagePath;
        }

        public static bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc) {
            string actualTestResourceName = GetTestResourceName(test);
            string xmlPath = GetXmlFilePath(actualTestResourceName, test);
            string imagePath = GetImageFilePath(actualTestResourceName, test);
            if(imagePath == null || xmlPath == null) {
                //log
                //Debug.WriteLine("fire save test" + test.ToLog());
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
        Valid, Invalid, Fixed
    }
}
