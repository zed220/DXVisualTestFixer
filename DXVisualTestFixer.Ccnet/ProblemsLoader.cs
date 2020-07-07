using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using DevExpress.CCNetSmart.Lib;
using DXVisualTestFixer.Ccnet.Core;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Ccnet {
 	public class CCNetProblemsLoader : ICCNetProblemsLoader {
		// static readonly string path = @"tcp://ccnet.devexpress.devx:21234/CruiseManager.rem";
		static readonly string wcfPath = @"net.tcp://ccnet.devexpress.devx:21235/CruiseManager.rem";

		public async Task<List<ICCNetProblem>> GetProblemsAsync(string projectName) {
			await Task.Delay(1).ConfigureAwait(false);
			var cruiseManager = CreateCCNetManager();
			var problems = await GetProblemsAsyncCore(cruiseManager, projectName);
			return problems.Select(CCNetProblem.FromCCNet).ToList();
		} 
		async Task<List<ProjectProblem>> GetProblemsAsyncCore(ISmartCruiseManager cruiseManager, string projectName) {
			await Task.Delay(1).ConfigureAwait(false);
			return await Task.Run(() => GetProblemsCore(cruiseManager, projectName)).ConfigureAwait(false);
		}


		public async Task<bool> TakeVolunteers(string projectName, string[] testFullNames, string volunteer) {
			await Task.Delay(1).ConfigureAwait(false);
			var cruiseManager = CreateCCNetManager();
			var problems = (await GetProblemsAsyncCore(cruiseManager, projectName).ConfigureAwait(false)).Select(p => p.Name).ToHashSet();
			var validTestFullNames = testFullNames.Where(problems.Contains).Distinct().ToArray();
			try {
				cruiseManager.SetProjectProblemInfo(projectName, validTestFullNames, volunteer, string.Empty, volunteer);
			}
			catch {
				return false;
			}
			return true;
		}

		static List<ProjectProblem> GetProblemsCore(ISmartCruiseManager cruiseManager, string projectName) {
			var problems = cruiseManager.GetProjectProblems(projectName);
			return problems?.Problems.Where(p => p.IsActive).ToList() ?? new List<ProjectProblem>();
		}

		static ISmartCruiseManager CreateCCNetManager() {
			ChannelFactory<ISmartCruiseManager> cruiseManagerFactory =
				new ChannelFactory<ISmartCruiseManager>(new NetTcpBinding(SecurityMode.None) {MaxReceivedMessageSize = 1024102464}, new EndpointAddress(wcfPath));
			ISmartCruiseManager cruiseManager = cruiseManagerFactory.CreateChannel();
			return cruiseManager;
		}
    }
}