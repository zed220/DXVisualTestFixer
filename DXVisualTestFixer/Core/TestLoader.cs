using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public static class TestLoader {
        public static List<CorpDirTestInfo> LoadFromInfo(FarmTaskInfo taskInfo) {
            List<CorpDirTestInfo> result = new List<CorpDirTestInfo>();
            result.AddRange(LoadFromUri(taskInfo, taskInfo.Url));
            return result;
        }

        static List<CorpDirTestInfo> LoadFromUri(FarmTaskInfo taskInfo, string url) {
            List<CorpDirTestInfo> result = new List<CorpDirTestInfo>();
            HtmlDocument htmlSnippet = new HtmlWeb().Load(url);
            foreach(HtmlNode testStartNode in FindAllTestLineStarts(url)) {
                HtmlNode testHeaderNode = GetTestHeaderNode(testStartNode);
                HtmlNode testNameNode = GetTestNameNode(testHeaderNode);
                string testName = testNameNode.InnerText;
                HtmlNode testMessageHeaderNode = GetTestMessageHeaderNode(testStartNode);
                HtmlNode testMessageNode = GetTestMessageNode(testMessageHeaderNode);
                string message = testMessageNode.InnerText;
                ParseMessage(taskInfo, testName, message, result);
            }
            return result;
        }

        static void ParseMessage(FarmTaskInfo farmTaskInfo, string testName, string message, List<CorpDirTestInfo> resultList) {
            List<string> themedResultPaths = message.Split(new[] { " - failed:" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach(var part in themedResultPaths) {
                ParseMessagePart(farmTaskInfo, testName, part, resultList);
            }
        }
        static void ParseMessagePart(FarmTaskInfo farmTaskInfo, string testName, string message, List<CorpDirTestInfo> resultList) {
            List<string> paths = message.Split(new[] { @"\\corp"}, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string>  resultPaths = PatchPaths(paths);
            CorpDirTestInfo info = null;
            if(!CorpDirTestInfo.TryCreate(farmTaskInfo, testName, resultPaths, out info))
                return;
            resultList.Add(info);
        }

        static List<string> PatchPaths(List<string> resultPaths) {
            List<string> result = new List<string>();
            foreach(var pathCandidate in resultPaths) {
                string cleanPath = @"\\corp" + pathCandidate.Replace("\r", String.Empty).Replace("\n", String.Empty).Replace(@"\\", @"\");
                if(cleanPath.Contains(' '))
                    continue;
                if(File.Exists(cleanPath)) {
                    result.Add(cleanPath);
                }
                else {
                    if(cleanPath.Contains("BitmapDif.png")) {
                        cleanPath = cleanPath.Split(new[] { "BitmapDif.png" }, StringSplitOptions.RemoveEmptyEntries).First() + "BitmapDif.png";
                        if(File.Exists(cleanPath))
                            result.Add(cleanPath);
                    }
                }
            }
            return result;
        }

        static List<HtmlNode> FindAllTestLineStarts(string url) {
            List<HtmlNode> result = new List<HtmlNode>();
            HtmlDocument htmlSnippet = new HtmlWeb().Load(url);
            try {
                foreach(HtmlNode link in htmlSnippet.DocumentNode.SelectNodes("//hr[@width]"))
                    result.Add(link.ParentNode.ParentNode);
            }
            catch { }
            return result;
        }
        static HtmlNode GetTestHeaderNode(HtmlNode startNode) {
            foreach(HtmlNode node in startNode.NextSibling.NextSibling.ChildNodes) {
                if(node.InnerText == "Test:")
                    return node;
            }
            return null;
        }
        static HtmlNode GetTestNameNode(HtmlNode currentNode) {
            foreach(HtmlNode node in currentNode.NextSibling.NextSibling.ChildNodes) {
                if(node.InnerText.StartsWith("DevExpress."))
                    return node;
            }
            return null;
        }
        static HtmlNode GetTestMessageHeaderNode(HtmlNode startNode) {
            foreach(HtmlNode node in startNode.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.ChildNodes) {
                if(node.InnerText == "Message:")
                    return node;
            }
            return null;
        }
        static HtmlNode GetTestMessageNode(HtmlNode currentNode) {
            return currentNode.NextSibling.NextSibling;
        }
    }
}
