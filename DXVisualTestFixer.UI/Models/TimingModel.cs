using System;
using System.Linq;

namespace DXVisualTestFixer.UI.Models {
	public class TimingModel {
		public TimingModel(string fullName, TimeSpan time) {
			FullName = fullName;
			Time = time;
			PopulateAttributes();
		}

		public string FullName { get; }
		public string Team { get; set; }
		public string Dpi { get; set; }
		public string Part { get; set; }

		public TimeSpan Time { get; }

		void PopulateAttributes() {
			var split1 = FullName.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
			if(split1.Length > 1)
				Part = split1[1];
			var split = split1[0].Split(new[] {"_" }, StringSplitOptions.RemoveEmptyEntries);
			if(split.Length < 3)
				return;
			Dpi = split.Last();
			Team = string.Join("_", split.Skip(1).Take(split.Length - 2));
		}
	}
}