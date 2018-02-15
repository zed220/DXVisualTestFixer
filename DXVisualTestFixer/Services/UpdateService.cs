using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DXVisualTestFixer.Services {
    public interface IUpdateService {
        void Update();
        void Start();
        void Stop();
        bool HasUpdate { get; }
    }
    public class UpdateService : BindableBase, IUpdateService {
        DispatcherTimer Timer;
        bool isNetworkDeployment;

        public bool HasUpdate {
            get { return GetProperty(() => HasUpdate); }
            private set { SetProperty(() => HasUpdate, value); }
        }

        public UpdateService() {
            isNetworkDeployment = ApplicationDeployment.IsNetworkDeployed;
            if(!isNetworkDeployment)
                return;
            Timer = new DispatcherTimer(DispatcherPriority.ContextIdle);
            Timer.Interval = TimeSpan.FromMinutes(1);
            Timer.Tick += Timer_Tick;
        }

        void Timer_Tick(object sender, EventArgs e) {
            CheckUpdate();
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

        void CheckUpdate() {
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
            HasUpdate = false;
            ad.Update();
            HasUpdate = true;
        }

        //public void Update(IMessageBoxService messageBoxService, bool informNoUpdate) {
        //    UpdateCheckInfo info = null;

        //    if(ApplicationDeployment.IsNetworkDeployed) {
        //        ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
        //        try {
        //            info = ad.CheckForDetailedUpdate();
        //        }
        //        catch(DeploymentDownloadException dde) {
        //            messageBoxService?.ShowMessage("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
        //            return;
        //        }
        //        catch(InvalidDeploymentException ide) {
        //            messageBoxService?.ShowMessage("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
        //            return;
        //        }
        //        catch(InvalidOperationException ioe) {
        //            messageBoxService?.ShowMessage("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
        //            return;
        //        }
        //        if(!info.UpdateAvailable) {
        //            if(informNoUpdate)
        //                messageBoxService?.ShowMessage("No updates available", "No updates available");
        //            return;
        //        }
        //        Boolean doUpdate = true;
        //        if(!info.IsUpdateRequired) {
        //            MessageResult? dr = messageBoxService?.ShowMessage("An update is available. Would you like to update the application now?", "Update Available", MessageButton.OKCancel);
        //            if(dr != MessageResult.OK)
        //                doUpdate = false;
        //        }
        //        else {
        //            // Display a message that the app MUST reboot. Display the minimum required version.
        //            messageBoxService?.ShowMessage("This application has detected a mandatory update from your current " + "version to version " +
        //                info.MinimumRequiredVersion.ToString() +
        //                ". The application will now install the update and restart.",
        //                "Update Available", MessageButton.OK,
        //                MessageIcon.Information);
        //        }

        //        if(doUpdate) {
        //            try {
        //                ad.Update();
        //                messageBoxService?.ShowMessage("The application has been upgraded, and will now restart.");
        //                System.Windows.Application.Current.Shutdown();
        //                System.Windows.Forms.Application.Restart();
        //            }
        //            catch(DeploymentDownloadException dde) {
        //                messageBoxService?.ShowMessage("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
        //                return;
        //            }
        //        }
        //    }
        //}
    }
}
