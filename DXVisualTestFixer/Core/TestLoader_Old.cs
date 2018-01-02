using HtmlAgilityPack;
using System.Collections.Generic;

namespace DXVisualTestFixer.Core {
    public static class TestLoader_Old {
        public static List<CorpDirTestInfo> LoadFromUri(FarmTaskInfo taskInfo) {
            List<CorpDirTestInfo> result = new List<CorpDirTestInfo>();
            foreach(HtmlNode testStartNode in FindAllTestLineStarts(taskInfo.Url)) {
                HtmlNode testMessageHeaderNode = GetTestMessageHeaderNode(testStartNode);
                HtmlNode testMessageNode = GetTestMessageNode(testMessageHeaderNode);
                string message = testMessageNode.InnerText;
                TestLoader.ParseMessage(taskInfo, message, result);
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
