using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

namespace SimpLauncherPlus {
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application {
		private Mutex _instanceMutex = null;

		protected override void OnStartup(StartupEventArgs e) {
			// check that there is only one instance of the control panel running...
			bool createdNew;
			_instanceMutex = new Mutex(true, @"SimpLauncher+", out createdNew);
			if (!createdNew) {
				_instanceMutex = null;
				MessageBox.Show("SimpLauncher+가 이미 실행중입니다.");
				Application.Current.Shutdown();
				return;
			}
			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e) {
			if (_instanceMutex != null)
				_instanceMutex.ReleaseMutex();
			base.OnExit(e);
		}
	}
}
