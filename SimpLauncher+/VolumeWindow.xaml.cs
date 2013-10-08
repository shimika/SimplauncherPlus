using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpLauncherPlus {
	/// <summary>
	/// VolumeWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class VolumeWindow : Window {
		MainWindow mainWindow;
		public VolumeWindow(MainWindow mWindow) {
			InitializeComponent();
			mainWindow = mWindow;
			this.Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Left + 90;
			this.Top = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Top + 100;
		}

		bool isFirst = true;
		public bool isRealClose = false;
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			AltTab alttab = new AltTab();
			alttab.HideAltTab(this);

			this.Closing += delegate(object senderx, System.ComponentModel.CancelEventArgs ex) {
				if (!isRealClose) {
					ex.Cancel = true;
					this.Topmost = false;
					this.Topmost = true;
				}
			};
		}

		bool muted = false;
		int tempvolume = 0;
		public void RefreshVolume(int volume, bool isMuted) {
			if (!mainWindow.isVolumeVisible) { return; }
			this.Topmost = false;
			this.Topmost = true;
			this.Opacity = 1;
			if (isFirst) {
				if (isMuted) {
					imageVolume.Source = new BitmapImage(new Uri(@"/Resources/voloff.png", UriKind.Relative));
					textblockVolume.Text = "X";
					tempvolume = 0;
				} else {
					imageVolume.Source = new BitmapImage(new Uri(@"/Resources/volon.png", UriKind.Relative));
					textblockVolume.Text = volume.ToString();
					tempvolume = volume;
				}
				isFirst = false;
			} else {
				if (isMuted != muted) {
					if (isMuted) {
						imageVolume.Source = new BitmapImage(new Uri(@"/Resources/voloff.png", UriKind.Relative));
					} else {
						imageVolume.Source = new BitmapImage(new Uri(@"/Resources/volon.png", UriKind.Relative));
					}
				}
				if (isMuted) {
					tempvolume = 0;
					textblockVolume.Text = "X";
				} else {
					textblockVolume.Text = volume.ToString();
					tempvolume = volume;
				}
			}
			rectVolume.Width = volume;
			muted = isMuted;

			rectVolume.Width = tempvolume;

			Storyboard sb = new Storyboard();
			DoubleAnimation winAnipre = new DoubleAnimation(1, 1, TimeSpan.FromMilliseconds(2500));
			Storyboard.SetTarget(winAnipre, this);
			Storyboard.SetTargetProperty(winAnipre, new PropertyPath(Window.OpacityProperty));

			DoubleAnimation winAni = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
			Storyboard.SetTarget(winAni, this);
			Storyboard.SetTargetProperty(winAni, new PropertyPath(Window.OpacityProperty));
			winAni.BeginTime = TimeSpan.FromMilliseconds(2500);

			sb.Children.Add(winAnipre);
			sb.Children.Add(winAni);
			sb.Begin(this);
		}
	}
}
