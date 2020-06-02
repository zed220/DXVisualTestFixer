using System;
using System.Linq;

namespace DXVisualTestFixer.UI.Common {
	static class InitialsExtractor {
		public static string Extract(string fullName) {
			if(fullName == "XpfDutyService")
				return "XD";
			var initials = fullName
				.Replace(" ", string.Empty)
				.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries)
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s.First().ToString().ToUpper());
			return string.Concat(initials);
		}

	}
}