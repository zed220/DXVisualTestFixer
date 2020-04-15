using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.Core {
	public class CorpDirTestInfo {
		public Repository Repository { get; private set; }
		public string Version { get; private set; }
		public string CurrentTextEditPath { get; private set; }
		public string CurrentTextEditSHAPath { get; private set; }
		public byte[] CurrentTextEditSHA { get; private set; }
		public string InstantTextEditPath { get; set; }
		public string InstantTextEditSHAPath { get; private set; }
		public byte[] InstantTextEditSHA { get; private set; }
		public string CurrentImagePath { get; private set; }
		public string CurrentImageSHAPath { get; private set; }
		public byte[] CurrentImageSHA { get; private set; }
		public string InstantImagePath { get; private set; }
		public string InstantImageSHAPath { get; private set; }
		public byte[] InstantImageSHA { get; private set; }
		public string ImageDiffPath { get; private set; }
		public int? DiffCount { get; private set; }

		public string TeamName { get; private set; }
		public string ServerFolderName { get; private set; }
		public string TestName { get; private set; }
		public string TestNameWithNamespace { get; private set; }
		public string ResourcesFullPath { get; private set; }
		
		public string ThemeName { get; private set; }
		public string StackTrace { get; set; }
		public string ErrorText { get; private set; }
		public List<TestProperty> AdditionalParameters { get; private set; } = new List<TestProperty>();

		static string GetTestName(string testNameAndNamespace) {
			if(!testNameAndNamespace.Contains('.'))
				return testNameAndNamespace;
			if(!testNameAndNamespace.Contains('('))
				return testNameAndNamespace.Split('.').Last();
			var firstTestNamePart = testNameAndNamespace.Split('(').First();
			return GetTestName(firstTestNamePart) + testNameAndNamespace.Remove(0, firstTestNamePart.Length);
		}

		public static CorpDirTestInfo CreateError(Repository repository, string testNameAndNamespace, string errorText, string stackTrace) =>
			new CorpDirTestInfo {
				Repository = repository,
				Version = repository.Version,
				ErrorText = errorText,
				TeamName = Team.ErrorName,
				StackTrace = stackTrace,
				ThemeName = "Error",
				TestName = GetTestName(testNameAndNamespace),
				TestNameWithNamespace = testNameAndNamespace
			};

		public static bool TryCreate(Repository repository,
			string fullName,
			string displayName,
			List<string> corpPaths,
			List<string> shaList,
			string category,
			string testResourcesFolder,
			List<TestProperty> properties,
			int? diffCount,
			out CorpDirTestInfo result) {
			result = null;
			var temp = new CorpDirTestInfo();
			temp.Repository = repository;
			temp.Version = repository.Version;
			temp.TestName = displayName;
			temp.TestNameWithNamespace = fullName;
			temp.DiffCount = diffCount;
			foreach(var path in corpPaths) {
				if(path.EndsWith("CurrentTextEdit.xml.sha")) {
					temp.CurrentTextEditSHAPath = path;
					continue;
				}

				if(path.EndsWith("CurrentTextEdit.xml")) {
					temp.CurrentTextEditPath = path;
					continue;
				}

				if(path.EndsWith("InstantTextEdit.xml.sha")) {
					temp.InstantTextEditSHAPath = path;
					continue;
				}

				if(path.EndsWith("InstantTextEdit.xml")) {
					temp.InstantTextEditPath = path;
					continue;
				}

				if(path.EndsWith("CurrentBitmap.png.sha")) {
					temp.CurrentImageSHAPath = path;
					continue;
				}

				if(path.EndsWith("CurrentBitmap.png")) {
					temp.CurrentImagePath = path;
					continue;
				}

				if(path.EndsWith("InstantBitmap.png.sha")) {
					temp.InstantImageSHAPath = path;
					continue;
				}

				if(path.EndsWith("InstantBitmap.png")) {
					temp.InstantImagePath = path;
					continue;
				}

				if(path.EndsWith("BitmapDif.png")) temp.ImageDiffPath = path;
			}

			foreach(var sha in shaList) {
				if(sha.StartsWith("xml_current")) {
					temp.CurrentTextEditSHA = ExtractSHA(sha);
					continue;
				}

				if(sha.StartsWith("xml_instant")) {
					temp.InstantTextEditSHA = ExtractSHA(sha);
					continue;
				}

				if(sha.StartsWith("png_current")) {
					temp.CurrentImageSHA = ExtractSHA(sha);
					continue;
				}

				if(sha.StartsWith("png_instant")) temp.InstantImageSHA = ExtractSHA(sha);
			}

			if(temp.CurrentTextEditPath == null || temp.CurrentImagePath == null) return false;
			
			temp.TeamName = category;
			
			temp.ServerFolderName = temp.CurrentTextEditPath.Split(new[] {@"\\corp\builds\testbuilds\"}, StringSplitOptions.RemoveEmptyEntries).First().Split('\\').First();
			temp.AdditionalParameters = properties;
			temp.ThemeName = properties.FirstOrDefault(p => p.Name == "ThemeName")?.Value ?? Team.ErrorName;
			temp.ResourcesFullPath = Path.Combine(repository.Path, testResourcesFolder.Replace(@"C:\builds\", string.Empty));

			result = temp;
			return true;
		}

		static byte[] ExtractSHA(string str) {
			if(!str.Contains(":") || !str.Contains("{") || !str.Contains("}"))
				return null;
			return Convert.FromBase64String(str.Split(new[] {"{"}, StringSplitOptions.RemoveEmptyEntries).Last().Split(new[] {"}"}, StringSplitOptions.RemoveEmptyEntries).First());
		}
	}

	public class TestProperty {
		public TestProperty(string name, string value) {
			Name = name;
			Value = value;
		}
		public string Name { get; }
		public string Value { get; }
	}
}