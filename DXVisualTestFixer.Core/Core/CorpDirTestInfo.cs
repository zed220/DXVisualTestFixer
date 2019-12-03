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
		public string ResourceFolderName { get; private set; }
		public string ThemeName { get; private set; }
		public string StackTrace { get; set; }
		public string ErrorText { get; private set; }
		public int Dpi { get; set; } = 96;
		public string AdditionalParameter { get; private set; } = string.Empty;

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
				TestName = GetTestName(testNameAndNamespace),
				TestNameWithNamespace = testNameAndNamespace
			};

		public static bool TryCreate(Repository repository, string testNameAndNamespace, List<string> corpPaths, List<string> shaList, int? diffCount, out CorpDirTestInfo result) {
			result = null;
			var temp = new CorpDirTestInfo();
			temp.Repository = repository;
			temp.Version = repository.Version;
			temp.TestName = GetTestName(testNameAndNamespace);
			temp.TestNameWithNamespace = testNameAndNamespace;
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
				//AppendSHA256Base64(sb, "xml_current", currentTxtSHA);
				//AppendSHA256Base64(sb, "xml_instant", instantTxtSHA);
				//AppendSHA256Base64(sb, "png_current", currentImageSHA);
				//AppendSHA256Base64(sb, "png_instant", instantImageSHA);
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

			if(temp.CurrentTextEditPath == null || temp.CurrentImagePath == null) return false; // && temp.ImageDiffPath != null
			//&& temp.InstantTextEditPath != null && temp.InstantImagePath != null
			temp.ServerFolderName = temp.CurrentTextEditPath.Split(new[] {@"\\corp\builds\testbuilds\"}, StringSplitOptions.RemoveEmptyEntries).First().Split('\\').First();
			if(temp.ServerFolderName.Contains("_dpi_")) {
				var nameAndDpi = temp.ServerFolderName.Split(new[] {"_dpi_"}, StringSplitOptions.RemoveEmptyEntries);
				temp.TeamName = nameAndDpi[0];
				temp.Dpi = int.Parse(nameAndDpi[1]);
			}
			else {
				temp.TeamName = temp.ServerFolderName;
			}

			if(temp.TeamName?.ToLower().Contains("scheduler") ?? false)
				if(temp.CurrentImagePath.Contains("Colorized"))
					temp.AdditionalParameter = "Colorized";

			var folderNameAndTheme = Path.GetDirectoryName(temp.CurrentTextEditPath).Split('\\').Last();
			if(!TryUpdateThemeAndFolderName(folderNameAndTheme, temp))
				return false;
			//string[] testNameAndTheme = Path.GetDirectoryName(temp.CurrentTextEditPath).Split('\\').Last().Split('.');
			//temp.TestName = testNameAndTheme[0];
			//temp.ThemeName = testNameAndTheme[1];
			//if(temp.InstantTextEditPath == null || temp.InstantImagePath == null) {
			//    temp.PossibleNewTest = true;
			//}
			//if(testNameAndTheme.Length > 2)
			//    temp.ThemeName += '.' + testNameAndTheme[2];
			result = temp;
			return true;
		}

		static byte[] ExtractSHA(string str) {
			if(!str.Contains(":") || !str.Contains("{") || !str.Contains("}"))
				return null;
			return Convert.FromBase64String(str.Split(new[] {"{"}, StringSplitOptions.RemoveEmptyEntries).Last().Split(new[] {"}"}, StringSplitOptions.RemoveEmptyEntries).First());
		}

		static bool TryUpdateThemeAndFolderName(string folderNameAndTheme, CorpDirTestInfo result) {
			var allThemes = ServiceLocator.Current.GetInstance<IThemesProvider>().AllThemes.ToList();
			allThemes.Add("Base");
			allThemes.Add("Super");
			allThemes.Sort(new ThemeNameComparer());
			foreach(var theme in allThemes.Where(t => t.Contains("Touch")).Concat(allThemes.Where(t => !t.Contains("Touch")))) {
				var themeName = theme.Replace(";", ".");
				if(!folderNameAndTheme.Contains(themeName))
					continue;
				result.ThemeName = themeName;
				result.ResourceFolderName = folderNameAndTheme.Replace("." + themeName, "");
				return true;
			}

			return false;
		}
	}

	class ThemeNameComparer : IComparer<string> {
		public int Compare(string x, string y) {
			if(x.Length > y.Length)
				return -1;
			return x.Length < y.Length ? 1 : Comparer<string>.Default.Compare(x, y);
		}
	}
}