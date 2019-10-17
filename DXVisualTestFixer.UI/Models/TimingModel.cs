using System;

namespace DXVisualTestFixer.UI.Models {
	public class TimingModel {
		public TimingModel(string fullName, TimeSpan time) {
			FullName = fullName;
			Time = time;
			PopulateAttributes();
		}

		public string FullName { get; }
		public string Prefix { get; set; }
		public string Team { get; set; }
		public string Dpi { get; set; }
		public string Part { get; set; }

		public TimeSpan Time { get; }

		void PopulateAttributes() {
			var split = FullName.Split(new[] {"_", "-"}, StringSplitOptions.RemoveEmptyEntries);
			if(split.Length < 1)
				return;
			Prefix = split[0];
			if(split.Length < 2)
				return;
			Team = split[1];
			if(split.Length < 3)
				return;
			Dpi = split[2];
			if(split.Length < 4)
				return;
			Part = split[3];
		}
	}
}