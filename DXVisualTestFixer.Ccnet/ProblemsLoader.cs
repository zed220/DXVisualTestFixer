using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using DevExpress.CCNetSmart.Lib;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Ccnet {
 	public class CCNetProblemsLoader : ICCNetProblemsLoader {
		// static readonly string path = @"tcp://ccnet.devexpress.devx:21234/CruiseManager.rem";
		static readonly string wcfPath = @"net.tcp://ccnet.devexpress.devx:21235/CruiseManager.rem";

		public async Task<List<ICCNetProblem>> GetProblemsAsync(string projectName) {
			await Task.Delay(1).ConfigureAwait(false);
			return await Task.Run(() => GetProblemsCore(projectName)).ConfigureAwait(false);
		}

		static List<ICCNetProblem> GetProblemsCore(string projectName) {
			ChannelFactory<ISmartCruiseManager> cruiseManagerFactory =
				new ChannelFactory<ISmartCruiseManager>(new NetTcpBinding(SecurityMode.None) {MaxReceivedMessageSize = 1024102464}, new EndpointAddress(wcfPath));
			ISmartCruiseManager cruiseManager = cruiseManagerFactory.CreateChannel();
			var problems = cruiseManager.GetProjectProblems(projectName);
			return problems.Problems.Where(p => p.IsActive && !p.IsUnstable).Select(CCNetProblem.FromCCNet).ToList();
		}
    }
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