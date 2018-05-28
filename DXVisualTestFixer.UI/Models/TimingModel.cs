using System;

namespace DXVisualTestFixer.UI.Models {
    public class TimingModel {
        public TimingModel(string fullName, TimeSpan time) {
            FullName = fullName;
            Time = time;
            PopulateAttributes();
        }

        void PopulateAttributes() {
            string[] splitted = FullName.Split(new string[] { "_", "-" }, StringSplitOptions.RemoveEmptyEntries);
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

        public string FullName { get; }
        public string Prefix { get; private set; }
        public string Team { get; private set; }
        public string Dpi { get; private set; }
        public string Part { get; private set; }

        public TimeSpan Time { get; }
    }
}
