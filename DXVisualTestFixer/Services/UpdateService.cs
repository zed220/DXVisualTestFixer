using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using Prism.Mvvm;
using Squirrel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace DXVisualTestFixer.Services {
    public class SquirrelUpdateService : UpdateServiceBase {
        const string serverfolder = @"\\corp\internal\common\visualtests_squirrel";

        public SquirrelUpdateService(INotificationService notificationService) : base(notificationService) { }

        public override void Update() {
            UpdateManager.RestartApp();
        }

        protected override async Task<bool> CheckUpdateCore() {
            try {
                if(!Directory.Exists(serverfolder))
                    return false;
            }
            catch(IOException e) {
                return false;
            }
            using(var mgr = new UpdateManager(serverfolder)) {
                UpdateInfo updateInfo = await mgr.CheckForUpdate();
                if(!updateInfo.ReleasesToApply.Any())
                    return false;
                var ver = await mgr.UpdateApp();
                return true;
            }
        }

        protected override bool GetIsNetworkDeployment() {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyFolder = Path.GetDirectoryName(assembly.Location);
            var assemblyFolderParent = Path.GetFullPath(Path.Combine(assemblyFolder, ".."));
            var updateDotExe = Path.Combine(assemblyFolderParent, "Update.exe");
            if(assemblyFolderParent.EndsWith("bin"))
                return false;
            return File.Exists(updateDotExe);
        }
    }

    public abstract class UpdateServiceBase : BindableBase, IUpdateService {
        readonly INotificationService notificationService;
        readonly Dispatcher dispatcher;

        DispatcherTimer Timer;
        bool _HasUpdate;
        bool _IsInUpdate;

        public bool HasUpdate {
            get { return _HasUpdate; }
            private set { SetProperty(ref _HasUpdate, value); }
        }
        public bool IsInUpdate {
            get { return _IsInUpdate; }
            private set { SetProperty(ref _IsInUpdate, value); }
        }
        public bool IsNetworkDeployment { get; set; }

        public UpdateServiceBase(INotificationService notificationService) {
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

        async void Timer_Tick(object sender, EventArgs e) {
            await CheckUpdate();
        }
        async Task CheckUpdate() {
            if(HasUpdate)
                return;
            if(IsInUpdate)
                return;
            IsInUpdate = true;
            try {
                HasUpdate = await CheckUpdateCore();
            }
            catch(Exception e) {
                dispatcher.Invoke(() => {
                    notificationService?.DoNotification("Update error", e.Message, System.Windows.MessageBoxImage.Error);
                    Timer.Stop();
                });
                
            }
            IsInUpdate = false;

        }

        protected abstract bool GetIsNetworkDeployment();
        protected abstract Task<bool> CheckUpdateCore();

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
    }
}
