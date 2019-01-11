using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.FarmIntegrator2 {
    class FarmRefreshedEventArgs : IFarmRefreshedEventArgs {
        public FarmRefreshType RefreshType { get; set; }

        public void Parse() { }
    }

    //class NotificationReceivedEventArgs : FarmRefreshedEventArgs {
    //    public string Message { get; private set; }
    //    //readonly BuildNotification buildNotification;
    //    public string ProjectName { get; private set; }
    //    public string BuildName { get; private set; }
    //    public string Sender { get; private set; }
    //    public bool IsServiceUser => Sender?.StartsWith("dxvcs2git") ?? false;
    //    //public NotificationReceivedEventArgs(BuildNotification buildNotification) {
    //    //    this.buildNotification = buildNotification;
    //    //}
    //    public override FarmRefreshType RefreshType => FarmRefreshType.notification;

    //    public override void Parse() {
    //        string projectName;
    //        string buildName;
    //        DXCCTrayHelper.ParseBuildUrl(buildNotification.BuildUrl, out projectName, out buildName);
    //        Sender = buildNotification.Recipient;
    //        ProjectName = projectName;
    //        BuildName = buildName;
    //        Message = CalcBalloonMessage(projectName, buildNotification);
    //    }
    //    string CalcBalloonMessage(string projectName, BuildNotification bn) {
    //        if(IsServiceUser) {
    //            var bytes = Convert.FromBase64String(bn.Sender);
    //            return Encoding.UTF8.GetString(bytes);
    //        }
    //        return $"{projectName} - {(bn.BuildChangeStatus == BuildChangeStatus.None ? bn.BuildStatus.ToString() : bn.BuildChangeStatus.ToString())}";
    //    }
    //}
}
