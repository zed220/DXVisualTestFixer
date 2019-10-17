using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DXVisualTestFixer.Common;
using Prism.Mvvm;
using Squirrel;

namespace DXVisualTestFixer.Services {
	public class SquirrelUpdateService : UpdateServiceBase {
		const string serverFolder = @"\\corp\internal\common\visualtests_squirrel";

		public SquirrelUpdateService(INotificationService notificationService) : base(notificationService) { }

		public override void Update() {
			UpdateManager.RestartApp();
		}

		protected override async Task<bool> CheckUpdateCore() {
			try {
				if(!Directory.Exists(serverFolder))
					return false;
			}
			catch(IOException) {
				return false;
			}

			using var mgr = new UpdateManager(serverFolder);
			var updateInfo = await mgr.CheckForUpdate();
			if(!updateInfo.ReleasesToApply.Any())
				return false;
			IsInUpdate = true;
			var ver = await mgr.UpdateApp();
			IsInUpdate = false;
			return true;
		}

		protected override bool GetIsNetworkDeployment() {
			var assembly = Assembly.GetEntryAssembly();
			var assemblyFolder = Path.GetDirectoryName(assembly.Location);
			var assemblyFolderParent = Path.GetFullPath(Path.Combine(assemblyFolder, ".."));
			var updateDotExe = Path.Combine(assemblyFolderParent, "Update.exe");
			return !assemblyFolderParent.EndsWith("bin") && File.Exists(updateDotExe);
		}
	}

	public abstract class UpdateServiceBase : BindableBase, IUpdateService {
		readonly Dispatcher dispatcher;
		readonly INotificationService notificationService;
		bool _HasUpdate;
		bool _IsInUpdate;

		bool isInUpdateCore;

		readonly DispatcherTimer Timer;

		public UpdateServiceBase(INotificationService notificationService) {
			this.notificationService = notificationService;
			dispatcher = Dispatcher.CurrentDispatcher;
			IsNetworkDeployment = GetIsNetworkDeployment();
			if(!IsNetworkDeployment) {
				HasUpdate = true;
				return;
			}

			Timer = new DispatcherTimer(DispatcherPriority.ContextIdle);
			Timer.Interval = TimeSpan.FromMinutes(1);
			Timer.Tick += Timer_Tick;
		}

		public bool HasUpdate {
			get => _HasUpdate;
			set => SetProperty(ref _HasUpdate, value);
		}

		public bool IsInUpdate {
			get => _IsInUpdate;
			protected set => SetProperty(ref _IsInUpdate, value);
		}

		public bool IsNetworkDeployment { get; set; }

		public void Start() {
			if(!IsNetworkDeployment)
				return;
			Timer.Start();
		}

		public void Stop() {
			if(!IsNetworkDeployment)
				return;
			Timer.Stop();
		}

		public abstract void Update();

		async void Timer_Tick(object sender, EventArgs e) {
			await CheckUpdate();
		}

		async Task CheckUpdate() {
			if(HasUpdate)
				return;
			if(IsInUpdate)
				return;
			if(isInUpdateCore)
				return;
			isInUpdateCore = true;
			try {
				HasUpdate = await CheckUpdateCore();
			}
			catch(Exception e) {
				dispatcher.Invoke(() => {
					notificationService?.DoNotification("Update error", e.Message, MessageBoxImage.Error);
					Timer.Stop();
				});
			}
			finally {
				isInUpdateCore = false;
			}
		}

		protected abstract bool GetIsNetworkDeployment();
		protected abstract Task<bool> CheckUpdateCore();
	}
}