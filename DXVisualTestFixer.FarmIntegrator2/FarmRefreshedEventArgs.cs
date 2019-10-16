using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.FarmIntegrator2 {
	internal class FarmRefreshedEventArgs : IFarmRefreshedEventArgs {
		public FarmRefreshType RefreshType { get; set; }

		public void Parse() { }
	}

	//class NotificationReceivedEventArgs : FarmRefreshedEventArgs {
	//    public string Message { get; set; }
	//    //readonly BuildNotification buildNotification;
	//    public string ProjectName { get; set; }
	//    public string BuildName { get; set; }
	//    public string Sender { get; set; }
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