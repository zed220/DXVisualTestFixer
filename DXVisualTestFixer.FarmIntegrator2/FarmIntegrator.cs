using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVisualTestFixer.FarmIntegrator2 {
    public class FarmIntegrator : IFarmIntegrator {
        async Task<List<IFarmTaskInfo>> IFarmIntegrator.GetAllTasks(Repository[] repositories) {
            return await Task.Factory.StartNew(() => {
                var result = new List<IFarmTaskInfo>();
                CruiseServerClientBase serverRemotingClient = new CruiseServerRemotingClient("tcp://ccnet.devexpress.devx:21234/CruiseManager.rem");
                foreach(var repository in repositories)
                    result.Add(new FarmTaskInfo(repository, GetUrl(serverRemotingClient, repository.GetTaskName())));
                return result;
            }).ConfigureAwait(false);
        }

        static string GetUrl(CruiseServerClientBase serverRemotingClient , string taskName) {
            var xmlName = serverRemotingClient.GetLatestBuildName(taskName);
            return string.Format("http://ccnet.devexpress.devx/ccnet/server/farm/project/{0}/build/{1}/ViewBuildReport.aspx", taskName.Replace(" ", "%20"), xmlName);
        }
    }
}
