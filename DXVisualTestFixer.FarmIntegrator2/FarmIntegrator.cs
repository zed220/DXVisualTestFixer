using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVisualTestFixer.FarmIntegrator2 {
	public class FarmIntegrator : IFarmIntegrator {
		List<IFarmTaskInfo> IFarmIntegrator.GetAllTasks(IEnumerable<Repository> repositories) {
			using var serverRemotingClient = new CruiseServerRemotingClient("tcp://ccnet.devexpress.devx:21234/CruiseManager.rem");
			return repositories.Select(repository => new FarmTaskInfo(repository, GetUrl(serverRemotingClient, repository.GetTaskName()))).Cast<IFarmTaskInfo>().ToList();
		}

		static string GetUrl(CruiseServerClientBase serverRemotingClient, string taskName) => $"http://ccnet.devexpress.devx/ccnet/server/farm/project/{taskName.Replace(" ", "%20")}/build/{serverRemotingClient.GetLatestBuildName(taskName)}/ViewBuildReport.aspx";
	}
}