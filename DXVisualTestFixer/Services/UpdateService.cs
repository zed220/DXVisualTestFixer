using DevExpress.Mvvm;
using Microsoft.Practices.ServiceLocation;
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
    public interface IUpdateService {
        void Update();
        void Start();
        void Stop();
        bool HasUpdate { get; }
    }
    public interface IButton {
        bool IsEnabled { get; set; }
        ICommand Command { get; set; }
    }

    public class UpdateButtonModel : BindableBase, IButton {
        public bool IsEnabled {
            get { return GetProperty(() => IsEnabled); }
            set { SetProperty(() => IsEnabled, value); }
        }
        public ICommand Command {
            get { return GetProperty(() => Command); }
            set { SetProperty(() => Command, value); }
        }
    }

    public class UpdateService : BindableBase, IUpdateService {
        DispatcherTimer Timer;
        bool isNetworkDeployment;
        bool isInUpdate = false;

        public UpdateButtonModel UpdateButton {
            get { return GetProperty(() => UpdateButton); }
            private set { SetProperty(() => UpdateButton, value, OnUpdateButtonChanged); }
        }
        public bool HasUpdate {
            get { return GetProperty(() => HasUpdate); }
            private set { SetProperty(() => HasUpdate, value, OnHasUpdateChanged); }
        }

        void OnUpdateButtonChanged() {
            ServiceLocator.Current.GetInstance<IShell>().HeaderItem = UpdateButton;
        }

        void OnHasUpdateChanged() {
            if(HasUpdate && UpdateButton != null)
                return;
            if(!HasUpdate && UpdateButton == null)
                return;
            if(!HasUpdate && UpdateButton != null) {
                UpdateButton = null;
                return;
            }
            if(HasUpdate && UpdateButton == null) {
                var updateButton = new UpdateButtonModel();
                updateButton.Command = new DelegateCommand(Update);
                updateButton.IsEnabled = true;
                UpdateButton = updateButton;
            }
        }

        public UpdateService() {
            isNetworkDeployment = ApplicationDeployment.IsNetworkDeployed;
            if(!isNetworkDeployment) {
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

        public void Update() {
            if(!HasUpdate)
                return;
            System.Windows.Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
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
