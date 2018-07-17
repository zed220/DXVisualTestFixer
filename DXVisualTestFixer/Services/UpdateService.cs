using DXVisualTestFixer.Common;
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

        public override void Update() {
            UpdateManager.RestartApp();
        }

        protected override async Task<bool> CheckUpdateCore() {
            try {
                if(!Directory.Exists(serverfolder))
                    Directory.CreateDirectory(serverfolder);
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
            var updateDotExe = Path.Combine(Path.GetDirectoryName(assembly.Location), "..", "Update.exe");
            return File.Exists(updateDotExe);
        }
    }

    public abstract class UpdateServiceBase : BindableBase, IUpdateService {
        DispatcherTimer Timer;
        bool _HasUpdate;
        bool isInUpdate;

        public bool HasUpdate {
            get { return _HasUpdate; }
            private set { SetProperty(ref _HasUpdate, value); }
        }
        public bool IsNetworkDeployment { get; set; }

        public UpdateServiceBase() {
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
            if(isInUpdate)
                return;
            isInUpdate = true;
            HasUpdate = await CheckUpdateCore();
            isInUpdate = false;

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

    public class ClickOnceUpdateService : UpdateServiceBase {
        protected override bool GetIsNetworkDeployment() {
            return ApplicationDeployment.IsNetworkDeployed;
        }

        protected override async Task<bool> CheckUpdateCore() {
            UpdateCheckInfo info = null;
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            try {
                info = ad.CheckForDetailedUpdate();
            }
            catch(DeploymentDownloadException) {
                return false;
            }
            catch(InvalidDeploymentException) {
                return false;
            }
            catch(InvalidOperationException) {
                return false;
            }
            if(!info.UpdateAvailable) {
                return false;
            }
            bool handled = false;
            AsyncCompletedEventHandler handler = (sender, e) => {
                handled = true;
            };
            ad.UpdateCompleted += handler;
            ad.UpdateAsync();
            while(!handled)
                await Task.Delay(100);
            ad.UpdateCompleted -= handler;
            return true;
        }

        public override void Update() {
            System.Windows.Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }
    }
}
