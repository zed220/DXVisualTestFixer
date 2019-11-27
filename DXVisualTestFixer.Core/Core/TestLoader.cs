using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.ServiceLocation;
using Minio.Exceptions;

namespace DXVisualTestFixer.Core {
	public class CorpDirTestInfoContainer {
		public CorpDirTestInfoContainer(List<CorpDirTestInfo> failedTests, List<string> usedFilesLinks, List<IElapsedTimeInfo> elapsedTimes, List<Team> teams, (DateTime sources, DateTime tests)? buildTime) {
			FailedTests = failedTests;
			UsedFilesLinks = usedFilesLinks;
			ElapsedTimes = elapsedTimes;
			Teams = teams;
			SourcesBuildTime = buildTime?.sources;
			TestsBuildTime = buildTime?.tests;
		}

		public List<CorpDirTestInfo> FailedTests { get; }
		public List<string> UsedFilesLinks { get; }
		public List<IElapsedTimeInfo> ElapsedTimes { get; }
		public List<Team> Teams { get; }
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

	public static class TestLoader {
		public static async Task<CorpDirTestInfoContainer> LoadFromMinio(Repository repository) {
			var minio = ServiceLocator.Current.GetInstance<IMinioWorker>();
			var dir = await minio.DiscoverLast($"XPF/{repository.Version}/");
			
			var failedTests = new ConcurrentBag<CorpDirTestInfo>();
			var usedFiles = new ConcurrentBag<string>();
			var elapsedTimes = new ConcurrentBag<IElapsedTimeInfo>();
			var teams = new ConcurrentBag<Team>();
			var failed = false;
			(DateTime sources, DateTime tests)? timings = null;

			var tasks = new List<Task>();

			string[] files = null;
			for(int i = 0; i < 10; i++, await Task.Delay(TimeSpan.FromSeconds(10))) {
				files = await minio.Discover(dir);
				if(files.FirstOrDefault(x => x.EndsWith("final")) != null)
					break;
				files = null;
			}

			var failedTestsList = new List<CorpDirTestInfo>();
			if(files != null) {
				foreach(var file in files) {
					if(!file.EndsWith(".xml"))
						continue;
					if(file.Contains("fail"))
						failed = true;
					var task = Task.Run(async () => {
						var xmlString = await minio.Download(file);
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
							foreach(var test in ParseMessage(repository, testNameAndNamespace, resultNode.InnerText, stackTraceNode.InnerText))
								failedTests.Add(test);
						}

						FindErrors(rootNode)?.ForEach(error => failedTests.Add(CorpDirTestInfo.CreateError(repository, "Error", error.InnerText, null)));

						usedFiles.Add(FindUsedFilesLink(rootNode));
						elapsedTimes.Add(FindElapsedTime(rootNode));
						teams.Add(FindTeam(repository.Version, rootNode));
					});
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);

				failedTestsList = failedTests.ToList();

				if(failedTestsList.Count == 0 && failed)
					failedTestsList.Add(CorpDirTestInfo.CreateError(repository, "BuildError", "BuildError", "BuildError"));
			}
			else
				failedTestsList.Add(CorpDirTestInfo.CreateError(repository, "Error", $"Files on the minio server does not stored correctly for v{repository.Version}. Please try again later.", null));

			return new CorpDirTestInfoContainer(failedTestsList, usedFiles.ToList(), elapsedTimes.ToList(), MergeTeams(teams.ToList()), timings);
		}

		static List<Team> MergeTeams(List<Team> teams) {
			var result = new Dictionary<string, Team>();
			foreach(var team in teams) {
				if(!result.TryGetValue(team.Name, out var storedTeam)) {
					result[team.Name] = team;
					continue;
				}
				storedTeam.TeamInfos.AddRange(team.TeamInfos);
			}
			return result.Values.ToList();
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
		
		static IEnumerable<XmlElement> FindErrors(XmlNode rootNode) => rootNode.FindByName("root")?.FindAllByName("error").Cast<XmlElement>();

		static string FindUsedFilesLink(XmlNode rootNode) => rootNode?.FindByName("FileUsingLogLink")?.InnerText.Replace("\r\n", string.Empty);

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

		static Team FindTeam(string version, XmlNode buildNode) {
			var result = new Dictionary<string, Team>();
			if(buildNode == null)
				return null;
			var teamNode = buildNode.FindByName("Project");
			if(!teamNode.TryGetAttribute("Dpi", out int dpi))
				return null;
			if(!teamNode.TryGetAttribute("IncludedCategories", out string teamName))
				return null;
			if(!teamNode.TryGetAttribute("ResourcesFolder", out string resourcesFolder))
				return null;
			if(!teamNode.TryGetAttribute("TestResourcesPath", out string testResourcesPath))
				return null;
			testResourcesPath = Path.Combine(resourcesFolder, testResourcesPath);
			teamNode.TryGetAttribute("TestResourcesPath_Optimized", out string testResourcesPath_optimized);
			if(testResourcesPath_optimized != null)
				testResourcesPath_optimized = Path.Combine(resourcesFolder, testResourcesPath_optimized);
			var projectInfosNode = teamNode.FindByName("ProjectInfos");
			if(projectInfosNode == null)
				return null;
			Team team = null;
			foreach(var projectInfoNode in projectInfosNode.FindAllByName("ProjectInfo")) {
				if(!projectInfoNode.TryGetAttribute("ServerFolderName", out string serverFolderName))
					continue;
				projectInfoNode.TryGetAttribute("Optimized", out bool optimized);
				if(!result.TryGetValue(teamName, out team))
					result[teamName] = team = new Team(teamName, version);
				team.TeamInfos.Add(new TeamInfo {Dpi = dpi, Optimized = optimized, ServerFolderName = serverFolderName, TestResourcesPath = testResourcesPath, TestResourcesPathOptimized = testResourcesPath_optimized});
			}
			return team;
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
			info = null;
			return CorpDirTestInfo.TryCreate(repository, testNameAndNamespace, resultPaths, shaList, diffCount, out info);
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