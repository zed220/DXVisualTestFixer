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
			var splitted = FullName.Split(new[] {"_", "-"}, StringSplitOptions.RemoveEmptyEntries);
			if(splitted.Length < 1)
				return;
			Prefix = splitted[0];
			if(splitted.Length < 2)
				return;
			Team = splitted[1];
			if(splitted.Length < 3)
				return;
			Dpi = splitted[2];
			if(splitted.Length < 4)
				return;
			Part = splitted[3];
		}
	}
}