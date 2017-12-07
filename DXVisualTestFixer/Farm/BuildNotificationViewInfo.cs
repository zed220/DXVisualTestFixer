using DevExpress.CCNetSmart.Lib;

namespace DXVisualTestFixer.Farm {
    public class BuildNotificationViewInfo {
        BuildNotification notification;
        bool read;
        public bool Read {
            get { return read; }
            set { read = value; }
        }
        public BuildNotification Notification {
            get {
                return notification;
            }
        }
        public BuildNotificationViewInfo(BuildNotification notification) {
            this.notification = notification;
            read = false;
        }
    }
}
