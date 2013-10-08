using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using CoreAudioApi;

namespace SimpLauncherPlus {
	/// <summary>
	/// SwitchWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class SwitchWindow : Window {
		public bool isStartWindows;
		private int LaunchKey, LaunchKeyA;
		public string saveFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpLauncher.ini";
		public MainWindow mainWindow;
		public bool canTouch = true;

		public SwitchWindow() {
			InitializeComponent();

			// set Switch Location
			this.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Left;
			//this.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2 - this.Height / 2;
			this.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2 - 100;

			// add Switch Event Handler
			buttonSwitch.MouseEnter += delegate(object sender, MouseEventArgs e) { AnimateSwitch(1, 0); };
			this.MouseLeave += delegate(object sender, MouseEventArgs e) { AnimateSwitch(0, 0); };
			buttonSwitch.Click += delegate(object sender, RoutedEventArgs e) {
				if (!canTouch) { return; }
				if (!mainWindow.isShown) {
					//AnimateSwitch(0, 0);
					mainWindow.AnimateWindow(1);
				}
			};

			// Notify Icon

			System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
			var iconHandle = SimpLauncherPlus.Properties.Resources.Delicious.Handle;
			ni.Icon = System.Drawing.Icon.FromHandle(iconHandle);
			ni.Visible = true; ni.Text = "SimpLauncher+";
			System.Windows.Forms.ContextMenuStrip ctxt = new System.Windows.Forms.ContextMenuStrip();
			System.Windows.Forms.ToolStripMenuItem cshutdown = new System.Windows.Forms.ToolStripMenuItem("닫기");
			//cstartup.Checked = isStartWindows;
			cshutdown.Click += delegate(object sender2, EventArgs e2) { this.Close(); };
			ctxt.Items.Add(cshutdown);
			ni.ContextMenuStrip = ctxt;

			// total closing event
			this.Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e) {
				ni.Dispose();
				mainWindow.isRealClose = true;
				mainWindow.Close();
			};

			// Item Drag Drop Event
			this.PreviewDragLeave += delegate(object sender, DragEventArgs e) { AnimateSwitch(0, 0); };
			this.PreviewDragOver += delegate(object sender, DragEventArgs e) { e.Handled = true; };
			this.PreviewDragEnter += delegate(object sender, DragEventArgs e) {
				var dropPossible = e.Data != null && ((DataObject)e.Data).ContainsFileDropList();
				if (dropPossible) {
					e.Effects = DragDropEffects.Copy;
					AnimateSwitch(1, 1);
				}
			};
			this.PreviewDrop += delegate(object sender, DragEventArgs e) {
				if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList()) {
					List<string> list = new List<string>();
					foreach (string filePath in ((DataObject)e.Data).GetFileDropList()) {
						if (!File.Exists(filePath) && !Directory.Exists(filePath)) { continue; }
						list.Add(filePath);
					}
					mainWindow.addItems(list);
				}
			};

			DispatcherTimer dtm2 = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };
			dtm2.Tick += delegate(object sender2, EventArgs e2) { this.Topmost = false; this.Topmost = true; };
			dtm2.Start();

			mainWindow = new MainWindow(this);
		}

		private IntPtr MainWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if (msg == Win32.WM_HOTKEY) {
				if (wParam.ToString() == LaunchKey.ToString() || wParam.ToString() == LaunchKeyA.ToString()) {
					if (mainWindow.isShown) {
						mainWindow.AnimateWindow(0);
					} else { mainWindow.AnimateWindow(1); }
				}
				handled = true;
			}
			return IntPtr.Zero;
		}

		private void AnimateSwitch(int isShowing, int isAdding) {
			Storyboard sb = new Storyboard();

			DoubleAnimation da1 = new DoubleAnimation(0.01 + 0.8 * isShowing, TimeSpan.FromMilliseconds(50));
			Storyboard.SetTarget(da1, this);
			Storyboard.SetTargetProperty(da1, new PropertyPath(Window.OpacityProperty));
			sb.Children.Add(da1);

			DoubleAnimation da2 = new DoubleAnimation(40 * isShowing * isAdding, TimeSpan.FromMilliseconds(200));
			da2.EasingFunction = new PowerEase() { Power = 6, EasingMode = EasingMode.EaseOut };
			Storyboard.SetTarget(da2, rectAddItems);
			Storyboard.SetTargetProperty(da2, new PropertyPath(Rectangle.WidthProperty));
			sb.Children.Add(da2);

			DoubleAnimation da3 = new DoubleAnimation(2 + 8 * isShowing, TimeSpan.FromMilliseconds(100));
			Storyboard.SetTarget(da3, buttonSwitch);
			Storyboard.SetTargetProperty(da3, new PropertyPath(Button.WidthProperty));
			sb.Children.Add(da3);

			sb.Begin(this);
		}

		private HwndSource hWndSource;
		private void windowSwitch_Loaded(object sender, RoutedEventArgs e) {
			WindowInteropHelper wih = new WindowInteropHelper(this);
			hWndSource = HwndSource.FromHwnd(wih.Handle);
			hWndSource.AddHook(MainWindowProc);
			LaunchKey = Win32.GlobalAddAtom("SimpLauncherStart");
			LaunchKeyA = Win32.GlobalAddAtom("SimpLauncherStartA");
			Win32.RegisterHotKey(wih.Handle, LaunchKey, 8, Win32.VK_KEY_Q);
			Win32.RegisterHotKey(wih.Handle, LaunchKeyA, 8, Win32.VK_KEY_A);
			new AltTab().HideAltTab(this);
		}
	}
}
