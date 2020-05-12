using DevExpress.CCNetSmart.Lib;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Ccnet.Core {
	class CCNetProblem : ICCNetProblem {
		CCNetProblem(string testName, string volunteer) {
			TestName = testName;
			Volunteer = volunteer;
		}

		public string TestName { get; }
		public string Volunteer { get; }

		public static ICCNetProblem FromCCNet(ProjectProblem problem) {
			return new CCNetProblem(problem.Name, problem.Volunteer);
		}
	}
}