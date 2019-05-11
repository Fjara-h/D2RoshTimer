using Hardcodet.Wpf.TaskbarNotification;
using System.Threading;
using System.Windows;

namespace D2RoshTimer {
	public partial class App : Application {
		Semaphore sema;
		bool shouldRelease = false, shouldClose = false;
		private TaskbarIcon notifyIcon;
		protected override void OnStartup(StartupEventArgs e) {
			bool result = Semaphore.TryOpenExisting("D2RoshTimer", out sema);
			if(result) {
				shouldClose = true;
			} else {
				try {
					sema = new Semaphore(1, 1, "D2RoshTimer");
				} catch {
					shouldClose = true;
				}
			}
			if(!sema.WaitOne(0)) {
				shouldClose = true;
			} else {
				shouldRelease = true;
			}
			if(shouldClose) {
				App.Current.Shutdown();
			}
			base.OnStartup(e);
			notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
			Application.Current.MainWindow = new MainWindow();
			Application.Current.MainWindow.Show();
		}
		protected override void OnExit(ExitEventArgs e) {
			if(sema != null && shouldRelease) {
				sema.Release();
				notifyIcon.Dispose();
				notifyIcon = null;
			}
			base.OnExit(e);
		}
	}
}
