using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace DXVisualTestFixer.Core {
    public static class TestLoader {
        public static List<CorpDirTestInfo> LoadFromInfo(FarmTaskInfo taskInfo) {
            string realUrl = CapureRealUrl(taskInfo.Url);
            List<CorpDirTestInfo> result = new List<CorpDirTestInfo>();
            if(realUrl == null || !realUrl.Contains("ViewBuildReport.aspx")) {
                return TestLoader_Old.LoadFromUri(taskInfo);
            }
            XmlDocument myXmlDocument = new XmlDocument();
            myXmlDocument.Load(realUrl.Replace("ViewBuildReport.aspx", "XmlBuildLog.xml"));
            List<Task<List<CorpDirTestInfo>>> allTasks = new List<Task<List<CorpDirTestInfo>>>();
            foreach(XmlElement testCaseXml in FindFailedTests(myXmlDocument)) {
                XmlNode resultNode = testCaseXml.FindByName("failure").FindByName("message");
                allTasks.Add(Task.Factory.StartNew<List<CorpDirTestInfo>>(() => {
                    List<CorpDirTestInfo> localRes = new List<CorpDirTestInfo>();
                    ParseMessage(taskInfo, resultNode.InnerText, localRes);
                    return localRes;
                }));
            }
            Task.WaitAll(allTasks.ToArray());
            allTasks.ForEach(t => result.AddRange(t.Result));
            return result;
        }
        static IEnumerable<XmlElement> FindFailedTests(XmlDocument myXmlDocument) {
            XmlNode testResults = myXmlDocument.FindByName("cruisecontrol")?.FindByName("build")?.FindByName("test-results");
            if(testResults == null)
                return null;
            return FindAllFailedTests(testResults);
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
        static string CapureRealUrl(string url) {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlSnippet = htmlWeb.Load(url);
            return htmlWeb.ResponseUri.ToString();
        }
        public static void ParseMessage(FarmTaskInfo farmTaskInfo, string message, List<CorpDirTestInfo> resultList) {
            List<string> themedResultPaths = message.Split(new[] { " - failed:" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach(var part in themedResultPaths) {
                ParseMessagePart(farmTaskInfo, part, resultList);
            }
        }
        static void ParseMessagePart(FarmTaskInfo farmTaskInfo, string message, List<CorpDirTestInfo> resultList) {
            List<string> paths = message.Split(new[] { @"\\corp" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> resultPaths = PatchPaths(paths);
            CorpDirTestInfo info = null;
            if(!CorpDirTestInfo.TryCreate(farmTaskInfo, resultPaths, out info))
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
                }
            }
            return result;
        }
    }
}
