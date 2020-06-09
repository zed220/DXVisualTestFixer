using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.Core {
	public class CorpDirTestInfoContainer {
		public CorpDirTestInfoContainer(List<CorpDirTestInfo> failedTests, List<string> usedFilesLinks, List<IElapsedTimeInfo> elapsedTimes, (DateTime sources, DateTime tests)? buildTime) {
			FailedTests = failedTests;
			UsedFilesLinks = usedFilesLinks;
			ElapsedTimes = elapsedTimes;
			SourcesBuildTime = buildTime?.sources;
			TestsBuildTime = buildTime?.tests;
		}

		public List<CorpDirTestInfo> FailedTests { get; }
		public List<string> UsedFilesLinks { get; }
		public List<IElapsedTimeInfo> ElapsedTimes { get; }
		public DateTime? SourcesBuildTime { get; }
		public DateTime? TestsBuildTime { get; }
	}

	class ElapsedTimeInfo : IElapsedTimeInfo {
		public ElapsedTimeInfo(string name, TimeSpan time) {
			Name = name;
			Time = time;
		}

		public string Name { get; }
		public TimeSpan Time { get; }
	}

	static class TestLoader {
		public static async Task<CorpDirTestInfoContainer> LoadFromMinio(MinioRepository minioRepository) {
			var minio = ServiceLocator.Current.GetInstance<IMinioWorker>();
			
			var failedTests = new ConcurrentBag<CorpDirTestInfo>();
			var usedFiles = new ConcurrentBag<string>();
			var elapsedTimes = new ConcurrentBag<IElapsedTimeInfo>();
			var failed = false;
			(DateTime sources, DateTime tests)? timings = null;

			var tasks = new List<Task>();

			var files = await minio.Discover(minioRepository.Path);

			var failedTestsList = new List<CorpDirTestInfo>();
			if(files != null) {
				foreach(var file in files) {
					if(!file.EndsWith(".xml"))
						continue;
					if(file.Contains("fail"))
						failed = true;
					var task = Task.Run(async () => {
						var xmlString = (await minio.Download(file)).FixAmpersands();
						var myXmlDocument = new XmlDocument();
						myXmlDocument.LoadXml("<root>" + xmlString + "</root>");
						var rootNode = myXmlDocument.FindByName("root");

						if(file.EndsWith("tests_timings.xml")) {
							timings = FindTimings(rootNode);
							return;
						}

						foreach(var testCaseXml in FindAllFailedTests(rootNode)) {
							var testNameAndNamespace = testCaseXml.GetAttribute("name");
							var failureNode = testCaseXml.FindByName("failure");
							var resultNode = failureNode.FindByName("message");
							var stackTraceNode = failureNode.FindByName("stack-trace");
							foreach(var test in ParseMessage(minioRepository.Repository, testNameAndNamespace, resultNode.InnerText, stackTraceNode.InnerText))
								failedTests.Add(test);
						}

						FindErrors(rootNode)?.ForEach(error => failedTests.Add(CorpDirTestInfo.CreateError(minioRepository.Repository, "Error", error.ExtractErrorText(), null)));

						usedFiles.Add(FindUsedFilesLink(rootNode));
						elapsedTimes.Add(FindElapsedTime(rootNode));
					});
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);

				failedTestsList = failedTests.ToList();

				if(failedTestsList.Count == 0 && failed)
					failedTestsList.Add(CorpDirTestInfo.CreateError(minioRepository.Repository, "BuildError", "BuildError", "BuildError"));
			}
			else
				failedTestsList.Add(CorpDirTestInfo.CreateError(minioRepository.Repository, "Error", $"Files on the minio server does not stored correctly for v{minioRepository.Repository.Version}. Please try again later.", null));

			return new CorpDirTestInfoContainer(failedTestsList, usedFiles.ToList(), elapsedTimes.ToList(), timings);
		}

		static readonly (char escape, string replace)[] escapeSymbols = { ('<', "&lt;"), ('>', "&gt;") };
		static string FixAmpersands(this string xml) {
			const string messageTagStart = "<message>";
			const string messageTagEnd = "</message>";
			
			xml = xml.Replace("&", "&amp;");
			var sb = new StringBuilder();
			foreach(var str in xml.Split(new [] { Environment.NewLine}, StringSplitOptions.None)) {
				var msgStartIndex = str.IndexOf(messageTagStart);
				if(msgStartIndex == -1) {
					sb.AppendLine(str);
					continue;
				}
				var fixedStr = str;
				var msgEndIndex = fixedStr.IndexOf(messageTagEnd);
				if(msgEndIndex == -1) {
					sb.AppendLine(str);
					continue;
				}
				foreach(var symbol in escapeSymbols) {
					while(true) {
						var ltIndex= fixedStr.IndexOf(symbol.escape, msgStartIndex + messageTagStart.Length);
						if(ltIndex < 0 || ltIndex > msgEndIndex - 1)
							break;
						var sbReplacer = new StringBuilder(fixedStr);
						sbReplacer.Replace(symbol.escape.ToString(), symbol.replace, ltIndex, 1);
						fixedStr = sbReplacer.ToString();
						msgEndIndex = fixedStr.IndexOf(messageTagEnd);
					}
				}
				sb.AppendLine(fixedStr);
			}
			return sb.ToString();
		}

		static string ExtractErrorText(this XmlElement xmlElement) {
			const string errorBegin = "-[Error]-----------------------------------------------------------------";
			const string errorEnd = "-------------------------------------------------------------------------";
			var sb = new StringBuilder();
			foreach(var msg in xmlElement.FindAllByName("message")) {
				var innerText = msg.InnerText;
				if(innerText == errorBegin || innerText == errorEnd)
					continue;
				sb.AppendLine(innerText);
			}
			return sb.ToString();
		}

		static (DateTime sources, DateTime tests)? FindTimings(XmlNode rootNode) {
			var timingsNode = rootNode?.FindByName("Timings");
			if(timingsNode == null)
				return null;
			try {
				return (GetDateTimeFromTimingsString(timingsNode.FindByName("Sources").InnerText), GetDateTimeFromTimingsString(timingsNode.FindByName("Tests").InnerText));
			}
			catch {
				return null;
			}
		}

		static DateTime GetDateTimeFromTimingsString(string dateStr) {
			var dateAndTime = dateStr.Split('_');
			var dateSplit = dateAndTime[0].Split('-');
			var timeSplit = dateAndTime[1].Split('-');
			return new DateTime(Convert.ToInt32(dateSplit[0]), Convert.ToInt32(dateSplit[1]), Convert.ToInt32(dateSplit[2]), Convert.ToInt32(timeSplit[0]), Convert.ToInt32(timeSplit[1]), 0);
		}

		static IEnumerable<XmlElement> FindErrors(XmlNode rootNode) => rootNode.FindAllByName("buildresults").Cast<XmlElement>();

		static string FindUsedFilesLink(XmlNode rootNode) => rootNode?.FindByName("FileUsingLogLink")?.InnerText.Replace("\r\n", string.Empty).Split('/').Last();//Remove tomorrow

		static IElapsedTimeInfo FindElapsedTime(XmlNode rootNode) {
			var elapsedTimeNode = rootNode.FindByName("ElapsedTime");
			if(elapsedTimeNode == null)
				return null;
			if(!elapsedTimeNode.TryGetAttribute("Name", out string name))
				return null;
			if(!elapsedTimeNode.TryGetAttribute("Time", out string time))
				return null;
			if(int.TryParse(time.Split('.').FirstOrDefault() ?? time, out var sec))
				return new ElapsedTimeInfo(name, TimeSpan.FromSeconds(sec));
			return null;
		}

		static bool TryGetAttribute<T>(this XmlNode node, string name, out T value) {
			value = default;
			var res = node.Attributes[name];
			if(res == null)
				return false;
			//value = (T)Convert.ChangeType(res, typeof(T));
			var converter = TypeDescriptor.GetConverter(typeof(T));
			value = (T) converter.ConvertFrom(res.Value);
			return true;
		}

		static IEnumerable<XmlElement> FindAllFailedTests(XmlNode testResults) {
			foreach(XmlNode node in testResults.ChildNodes)
				if(node is XmlElement xmlElement && xmlElement.Name == "test-case" && xmlElement.GetAttribute("success") == "False")
					yield return xmlElement;
				else
					foreach(var subNode in FindAllFailedTests(node))
						yield return subNode;
		}

		static XmlNode FindByName(this XmlNode element, string name) {
			foreach(XmlNode node in element.ChildNodes)
				if(node.Name == name)
					return node;
			return null;
		}

		static IEnumerable<XmlNode> FindAllByName(this XmlNode element, string name) {
			foreach(XmlNode node in element.ChildNodes)
				if(node.Name == name)
					yield return node;
		}

		static IEnumerable<CorpDirTestInfo> ParseMessage(Repository repository, string testNameAndNamespace, string message, string stackTrace) {
			if(!message.StartsWith("Exception - NUnit.Framework.AssertionException")) {
				yield return CorpDirTestInfo.CreateError(repository, testNameAndNamespace, message, stackTrace);
				yield break;
			}

			var themedResultPaths = message.Split(new[] {" - failed:"}, StringSplitOptions.RemoveEmptyEntries).ToList();
			if(themedResultPaths.Count == 1) {
				yield return CorpDirTestInfo.CreateError(repository, testNameAndNamespace, message, stackTrace);
				yield break;
			}

			foreach(var part in themedResultPaths)
				if(TryParseMessagePart(repository, testNameAndNamespace, part, out var info))
					yield return info;
		}

		static bool TryParseMessagePart(Repository repository, string testNameAndNamespace, string message, out CorpDirTestInfo info) {
			var parts = message.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).ToList();
			var paths = parts.Where(x => x.Contains(@"\\corp")).Select(x => x.Replace(@"\\corp", string.Empty)).ToList();

			var resultPaths = PatchPaths(paths);
			var shaList = PatchSHA(parts.Where(x => x.Contains("sha-")).Select(x => x.Replace("sha-", string.Empty)).ToList());
			var diffCount = TryGetDiffCount(parts.Where(x => x.Contains("diffCount=")).Select(x => x.Replace("diffCount=", string.Empty)).LastOrDefault());
			var testFullName = GetStringFromParts(parts, "TestFullName:");
			var testDisplayName = GetStringFromParts(parts, "TestDisplayName:");
			var testCategory = GetStringFromParts(parts, "TestCategory:");
			var testResourcesFolder = GetStringFromParts(parts, "ResourcesFolder:");
			var testProperties = GetTestProperties(parts);
			info = null;
			return CorpDirTestInfo.TryCreate(repository, testFullName, testDisplayName, resultPaths, shaList, testCategory, testResourcesFolder, testProperties, diffCount, out info);
		}

		static List<TestProperty> GetTestProperties(List<string> parts) {
			var result = new List<TestProperty>();
			foreach(var part in parts) {
				var attributeStr = GetStringFromPart(part, "TestProperty:");
				if(attributeStr == null)
					continue;
				var nameValue = attributeStr.Replace("Name=", "").Split(new [] { ",Value=" }, StringSplitOptions.RemoveEmptyEntries);
				if(nameValue.Length != 2 || nameValue.Any(string.IsNullOrWhiteSpace))
					continue;
				result.Add(new TestProperty(nameValue[0], nameValue[1]));
			}
			return result;
		}

		static string GetStringFromParts(List<string> parts, string partName) {
			return parts.Select(x => GetStringFromPart(x, partName)).LastOrDefault(x => x!= null);
		}
		static string GetStringFromPart(string part, string partName) {
			if(part.Contains(partName))
				return part.Replace(partName, string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
			return null;
		}

		static int? TryGetDiffCount(string str) {
			if(int.TryParse(str, out var res))
				return res;
			return null;
		}

		static List<string> PatchSHA(List<string> shaList) {
			var result = new List<string>();
			foreach(var sha in shaList) result.Add(sha.Replace("\r", string.Empty).Replace("\n", string.Empty));
			return result;
		}

		static List<string> PatchPaths(List<string> resultPaths) {
			var result = new List<string>();
			foreach(var pathCandidate in resultPaths) {
				if(!pathCandidate.Contains('\\'))
					continue;
				var cleanPath = @"\\corp" + pathCandidate.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(@"\\", @"\");
				if(cleanPath.Contains(' '))
					continue;
				if(File.Exists(cleanPath)) {
					result.Add(cleanPath);
					continue;
				}

				if(cleanPath.Contains("InstantBitmap.png.sha")) {
					SafeAddPath("InstantBitmap.png.sha", cleanPath, result);
					continue;
				}

				if(cleanPath.Contains("InstantBitmap.png")) {
					SafeAddPath("InstantBitmap.png", cleanPath, result);
					continue;
				}

				if(cleanPath.Contains("BitmapDif.png")) {
					SafeAddPath("BitmapDif.png", cleanPath, result);
					continue;
				}

				if(cleanPath.Contains("CurrentBitmap.png.sha")) {
					SafeAddPath("CurrentBitmap.png.sha", cleanPath, result);
					continue;
				}

				if(cleanPath.Contains("CurrentBitmap.png")) SafeAddPath("CurrentBitmap.png", cleanPath, result);
			}

			return result;
		}

		static void SafeAddPath(string fileName, string pathCandidate, List<string> paths) {
			var cleanPath = pathCandidate.Split(new[] {fileName}, StringSplitOptions.RemoveEmptyEntries).First() + fileName;
			if(File.Exists(cleanPath))
				paths.Add(cleanPath);
		}
	}
}