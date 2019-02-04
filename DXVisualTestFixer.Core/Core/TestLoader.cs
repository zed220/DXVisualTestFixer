using DXVisualTestFixer.Common;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace DXVisualTestFixer.Core {
    public class CorpDirTestInfoContainer {
        public CorpDirTestInfoContainer(List<CorpDirTestInfo> failedTests, List<string> usedFiles, List<IElapsedTimeInfo> elapsedTimes, List<Team> teams) {
            FailedTests = failedTests;
            UsedFiles = usedFiles;
            ElapsedTimes = elapsedTimes;
            Teams = teams;
        }

        public List<CorpDirTestInfo> FailedTests { get; }
        public List<string> UsedFiles { get; }
        public List<IElapsedTimeInfo> ElapsedTimes { get; }
        public List<Team> Teams { get; }
    }
    class ElapsedTimeInfo : IElapsedTimeInfo {
        public ElapsedTimeInfo(string name, TimeSpan time) {
            Name = name;
            Time = time;
        }

        public string Name { get; }
        public TimeSpan Time { get; }
    }

    public static class TestLoader {
        public static CorpDirTestInfoContainer LoadFromInfo(IFarmTaskInfo taskInfo, string realUrl) {
            List<CorpDirTestInfo> failedTests = new List<CorpDirTestInfo>();
            if(realUrl == null || !realUrl.Contains("ViewBuildReport.aspx")) {
                throw new NotSupportedException("Contact Petr Zinovyev, please.");
            }
            XmlDocument myXmlDocument = new XmlDocument();
            myXmlDocument.Load(realUrl.Replace("ViewBuildReport.aspx", "XmlBuildLog.xml"));
            List<Task<List<CorpDirTestInfo>>> failedTestsTasks = new List<Task<List<CorpDirTestInfo>>>();
            if(!IsSuccessBuild(myXmlDocument)) {
                foreach(XmlElement testCaseXml in FindFailedTests(myXmlDocument)) {
                    string testNameAndNamespace = testCaseXml.GetAttribute("name");
                    XmlNode failureNode = testCaseXml.FindByName("failure");
                    failedTestsTasks.Add(Task.Factory.StartNew<List<CorpDirTestInfo>>(() => {
                        XmlNode resultNode = failureNode.FindByName("message");
                        XmlNode stackTraceNode = failureNode.FindByName("stack-trace");
                        List<CorpDirTestInfo> localRes = new List<CorpDirTestInfo>();
                        ParseMessage(taskInfo, testNameAndNamespace, resultNode.InnerText, stackTraceNode.InnerText, localRes);
                        return localRes;
                    }));
                }
                if(failedTestsTasks.Count > 0) {
                    Task.WaitAll(failedTestsTasks.ToArray());
                    failedTestsTasks.ForEach(t => failedTests.AddRange(t.Result));
                }
                else {
                    if(!taskInfo.Success)
                        failedTests.Add(CorpDirTestInfo.CreateError(taskInfo, "BuildError", "BuildError", "BuildError"));
                }
            }
            return new CorpDirTestInfoContainer(failedTests, FindUsedFiles(myXmlDocument).ToList(), FindElapsedTimes(myXmlDocument), FindTeams(taskInfo.Repository.Version, myXmlDocument));
        }

        private static bool IsSuccessBuild(XmlDocument myXmlDocument) {
            XmlNode buildNode = FindBuildNode(myXmlDocument);
            if(buildNode == null)
                return false;
            return !buildNode.TryGetAttibute("error", out string _);
        }

        static IEnumerable<XmlElement> FindFailedTests(XmlDocument myXmlDocument) {
            XmlNode buildNode = FindBuildNode(myXmlDocument);
            if(buildNode == null)
                yield break;
            foreach(var testResults in buildNode.FindAllByName("test-results")) {
                foreach(XmlElement subNode in FindAllFailedTests(testResults))
                    yield return subNode;
            }
        }
        static IEnumerable<string> FindUsedFiles(XmlDocument myXmlDocument) {
            XmlNode buildNode = FindBuildNode(myXmlDocument);
            if(buildNode == null)
                yield break;
            foreach(var usedFilesNode in buildNode.FindAllByName("FileUsingLog")) {
                foreach(string usedFile in usedFilesNode.InnerText.Split('\n')) {
                    if(String.IsNullOrEmpty(usedFile))
                        continue;
                    string result = usedFile.Replace("\r", "");
                    if(result.Contains(@"\VisualTests\"))
                        yield return result.Split(new[] { @"\VisualTests\" }, StringSplitOptions.RemoveEmptyEntries).Last().ToLower();
                    else
                        yield return result;
                }
            }
        }
        static List<IElapsedTimeInfo> FindElapsedTimes(XmlDocument myXmlDocument) {
            List<IElapsedTimeInfo> result = new List<IElapsedTimeInfo>();
            XmlNode buildNode = FindBuildNode(myXmlDocument);
            if(buildNode == null)
                return result;
            foreach(var elapsedTimeNode in buildNode.FindAllByName("ElapsedTime")) {
                string name;
                if(!elapsedTimeNode.TryGetAttibute("Name", out name))
                    continue;
                string time;
                if(!elapsedTimeNode.TryGetAttibute("Time", out time))
                    continue;
                if(int.TryParse(time.Split('.').FirstOrDefault() ?? time, out int sec))
                    result.Add(new ElapsedTimeInfo(name, TimeSpan.FromSeconds(sec)));
            }
            return result;
        }
        static List<Team> FindTeams(string version, XmlDocument myXmlDocument) {
            Dictionary<string, Team> result = new Dictionary<string, Team>();
            XmlNode buildNode = FindBuildNode(myXmlDocument);
            if(buildNode == null)
                return null;
            foreach(var teamNode in buildNode.FindAllByName("Project")) {
                int dpi;
                if(!teamNode.TryGetAttibute("Dpi", out dpi))
                    continue;
                string teamName;
                if(!teamNode.TryGetAttibute("IncludedCategories", out teamName))
                    continue;
                string resourcesFolder;
                if(!teamNode.TryGetAttibute("ResourcesFolder", out resourcesFolder))
                    continue;
                string testResourcesPath;
                if(!teamNode.TryGetAttibute("TestResourcesPath", out testResourcesPath))
                    continue;
                var projectInfosNode = teamNode.FindByName("ProjectInfos");
                if(projectInfosNode == null)
                    continue;
                foreach(var projectInfoNode in projectInfosNode.FindAllByName("ProjectInfo")) {
                    string serverFolderName;
                    if(!projectInfoNode.TryGetAttibute("ServerFolderName", out serverFolderName))
                        continue;
                    bool optimized;
                    projectInfoNode.TryGetAttibute("Optimized", out optimized);
                    Team team;
                    if(!result.TryGetValue(teamName, out team))
                        result[teamName] = team = new Team() { Name = teamName, Version = version };
                    team.TeamInfos.Add(new TeamInfo() { Dpi = dpi, Optimized = optimized, ServerFolderName = serverFolderName, TestResourcesPath = Path.Combine(resourcesFolder, testResourcesPath) });
                }
            }
            return result.Values.Count == 0 ? null : result.Values.ToList();
        }

        static bool TryGetAttibute<T>(this XmlNode node, string name, out T value) {
            value = default(T);
            var res = node.Attributes[name];
            if(res == null)
                return false;
            //value = (T)Convert.ChangeType(res, typeof(T));
            var converter = TypeDescriptor.GetConverter(typeof(T));
            value = (T)converter.ConvertFrom(res.Value);
            return true;
        }
        static XmlNode FindBuildNode(XmlDocument myXmlDocument) {
            return myXmlDocument.FindByName("cruisecontrol")?.FindByName("build");
        }
        static IEnumerable<XmlElement> FindAllFailedTests(XmlNode testResults) {
            foreach(XmlNode node in testResults.ChildNodes) {
                XmlElement xmlElement = node as XmlElement;
                if(xmlElement != null && xmlElement.Name == "test-case" && xmlElement.GetAttribute("success") == "False")
                    yield return xmlElement;
                else {
                    foreach(XmlElement subNode in FindAllFailedTests(node))
                        yield return subNode;
                }
            }
        }
        static XmlNode FindByName(this XmlNode element, string name) {
            foreach(XmlNode node in element.ChildNodes) {
                if(node.Name == name)
                    return node;
            }
            return null;
        }
        static IEnumerable<XmlNode> FindAllByName(this XmlNode element, string name) {
            foreach(XmlNode node in element.ChildNodes) {
                if(node.Name == name)
                    yield return node;
            }
        }
        static string LoadXmlString(string xmlUri) {
            HtmlWeb htmlWeb = new HtmlWeb();
            var r = htmlWeb.Load(xmlUri);
            using(Stream s = new MemoryStream()) {
                r.Save(s);
                s.Seek(0, SeekOrigin.Begin);
                using(StreamReader reader = new StreamReader(s)) {
                    return reader.ReadToEnd();
                }
            }
        }
        public static void ParseMessage(IFarmTaskInfo farmTaskInfo, string testNameAndNamespace, string message, string stackTrace, List<CorpDirTestInfo> resultList) {
            if(!message.StartsWith("Exception - NUnit.Framework.AssertionException")) {
                resultList.Add(CorpDirTestInfo.CreateError(farmTaskInfo, testNameAndNamespace, message, stackTrace));
                return;
            }
            List<string> themedResultPaths = message.Split(new[] { " - failed:" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach(var part in themedResultPaths) {
                ParseMessagePart(farmTaskInfo, testNameAndNamespace, part, resultList);
            }
        }
        static void ParseMessagePart(IFarmTaskInfo farmTaskInfo, string testNameAndNamespace, string message, List<CorpDirTestInfo> resultList) {
            List<string> paths = message.Split(new[] { @"\\corp" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> resultPaths = PatchPaths(paths);
            CorpDirTestInfo info = null;
            if(!CorpDirTestInfo.TryCreate(farmTaskInfo, testNameAndNamespace, resultPaths, out info))
                return;
            resultList.Add(info);
        }

        static List<string> PatchPaths(List<string> resultPaths) {
            List<string> result = new List<string>();
            foreach(var pathCandidate in resultPaths) {
                if(!pathCandidate.Contains('\\'))
                    continue;
                string cleanPath = @"\\corp" + pathCandidate.Replace("\r", String.Empty).Replace("\n", String.Empty).Replace(@"\\", @"\");
                if(cleanPath.Contains(' '))
                    continue;
                if(File.Exists(cleanPath)) {
                    result.Add(cleanPath);
                }
                else {
                    if(cleanPath.Contains("InstantBitmap.png")) {
                        cleanPath = cleanPath.Split(new[] { "InstantBitmap.png" }, StringSplitOptions.RemoveEmptyEntries).First() + "InstantBitmap.png";
                        if(File.Exists(cleanPath))
                            result.Add(cleanPath);
                    }
                    if(cleanPath.Contains("BitmapDif.png")) {
                        cleanPath = cleanPath.Split(new[] { "BitmapDif.png" }, StringSplitOptions.RemoveEmptyEntries).First() + "BitmapDif.png";
                        if(File.Exists(cleanPath))
                            result.Add(cleanPath);
                    }
                    if(cleanPath.Contains("CurrentBitmap.png")) {
                        cleanPath = cleanPath.Split(new[] { "CurrentBitmap.png" }, StringSplitOptions.RemoveEmptyEntries).First() + "CurrentBitmap.png";
                        if(File.Exists(cleanPath))
                            result.Add(cleanPath);
                    }
                }
            }
            return result;
        }
    }
}
