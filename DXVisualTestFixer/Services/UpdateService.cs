using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace DXVisualTestFixer.Services {
    public class UpdateService : BindableBase, IUpdateService {
        DispatcherTimer Timer;
        bool isNetworkDeployment;
        bool isInUpdate = false;

        bool _HasUpdate;

        public bool HasUpdate {
            get { return _HasUpdate; }
            private set { SetProperty(ref _HasUpdate, value); }
        }
        public bool IsNetworkDeployment { get; }

        public UpdateService() {
            IsNetworkDeployment = ApplicationDeployment.IsNetworkDeployed;
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

        public void Start() {
            if(!isNetworkDeployment)
                return;
            Timer.Start();
        }

        public void Stop() {
            if(!isNetworkDeployment)
                return;
            Timer.Stop();
        }

        async Task CheckUpdate() {
            if(HasUpdate)
                return;
            if(isInUpdate)
                return;
            isInUpdate = true;
            await CheckUpdateCore();
            isInUpdate = false;

        }
        async Task CheckUpdateCore() {
            UpdateCheckInfo info = null;
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            try {
                info = ad.CheckForDetailedUpdate();
            }
            catch(DeploymentDownloadException) {
                return;
            }
            catch(InvalidDeploymentException) {
                return;
            }
            catch(InvalidOperationException) {
                return;
            }
            if(!info.UpdateAvailable) {
                return;
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
            HasUpdate = true;
        }
    }
}
