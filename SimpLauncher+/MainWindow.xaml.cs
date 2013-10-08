using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CoreAudioApi;
using Microsoft.Win32;

namespace SimpLauncherPlus {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		SwitchWindow windowSwitch;
		public bool isRealClose = false;
		string pathSetting = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpLauncher+.ini";
		string pathSettingPref = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpLauncherSet+.ini";
		string pathSettingPrevVer = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpLauncher.ini";
		List<InData> listFiles = new List<InData>();
		Point beforePoint, afterPoint;
		public bool isWindowsStartup = false, isLeftSwitchOn = true, isVolumeVisible = true;
		int layoutMaxWidth = 3, layoutMaxHeight = 2;
		int[] itemsIndex, itemsPosition, itemsNewPosition;


		public MainWindow(SwitchWindow wSwitch) {
			InitializeComponent();

			Timeline.DesiredFrameRateProperty.OverrideMetadata(
				typeof(Timeline),
				new FrameworkPropertyMetadata { DefaultValue = 60 });

			if (!File.Exists(pathSetting)) {

				if (File.Exists(pathSettingPrevVer)) {
					if (MessageBox.Show("이전 SimpLauncher의 데이터가 발견되었습니다.\n데이터를 이전합니다.") == MessageBoxResult.OK) {
						StreamReader srt = new StreamReader(pathSettingPrevVer);
						StreamWriter swt = new StreamWriter(pathSetting);

						string[] splitStr = srt.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
						srt.Close();
						int tN = Convert.ToInt32(splitStr[0]);

						for (int i = 1; i <= tN; i++) {
							if (splitStr[i] == "설정?설정?True") { continue; }
							string[] splitPath = splitStr[i].Split(new string[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
							swt.WriteLine(splitPath[0] + "#!SIMPLAUNCHER!#" + splitPath[1] + "#!SIMPLAUNCHER!#" + splitPath[2]);
						}
						swt.Flush(); swt.Close();
						splitStr = null;
					}
				} else {
					StreamWriter swt = new StreamWriter(pathSetting, false);
					swt.WriteLine("");
					swt.Flush(); swt.Close();
				}
				StreamWriter sws = new StreamWriter(pathSettingPref);
				sws.WriteLine("STARTUP=False");
				sws.WriteLine("LEFTSWITCH=True");
				sws.WriteLine("VOLUME=True");
				sws.Flush(); sws.Close();

				//new TutoWindow().Show();
			}

			StreamReader srt2 = new StreamReader(pathSetting);
			string[] splitStr2 = srt2.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); srt2.Close();
			foreach (string filePath in splitStr2) {
				string[] splitPath = filePath.Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries);
				InData idt = new InData() {
					path = splitPath[0], caption = splitPath[1],
					isSpecial = Convert.ToBoolean(splitPath[2]),
				};
				if (idt.isSpecial) {
					if (idt.path == "휴지통") {
						idt.img = new BitmapImage(new Uri(@"/Resources/trash.png", UriKind.Relative));
					}
				} else { idt.img = rtIconImage(splitPath[0]); }
				listFiles.Add(idt);
			}
			StreamReader srt3 = new StreamReader(pathSettingPref);
			splitStr2 = srt3.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); srt2.Close();
			srt3.Close();
			foreach (string filePath in splitStr2) {
				string[] splitPath = filePath.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
				switch (splitPath[0]) {
					case "STARTUP": isWindowsStartup = Convert.ToBoolean(splitPath[1]); break;
					case "LEFTSWITCH": isLeftSwitchOn = Convert.ToBoolean(splitPath[1]); break;
					case "VOLUME": isVolumeVisible = Convert.ToBoolean(splitPath[1]); break;
				}
			}

			if (isWindowsStartup) {
				buttonStartup.Style = FindResource("TransparentButtonOn") as Style;
			} else { buttonStartup.Style = FindResource("TransparentButtonOff") as Style; }

			if (isLeftSwitchOn) {
				buttonLeftSwitch.Style = FindResource("TransparentButtonLeftOn") as Style;
			} else { buttonLeftSwitch.Style = FindResource("TransparentButtonLeftOff") as Style; }

			if (isVolumeVisible) {
				buttonVolume.Style = FindResource("TransparentButtonOn") as Style;
			} else { buttonVolume.Style = FindResource("TransparentButtonOff") as Style; }

			buttonStartup.Click += (o, e) => {
				isWindowsStartup = !isWindowsStartup;
				RegistryKey add = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
				if (isWindowsStartup) {
					add.SetValue("SimpLauncher+", "\"" + System.AppDomain.CurrentDomain.BaseDirectory + System.AppDomain.CurrentDomain.FriendlyName + "\"");
					buttonStartup.Style = FindResource("TransparentButtonOn") as Style;
				} else {
					add.DeleteValue("SimpLauncher", false);
					buttonStartup.Style = FindResource("TransparentButtonOff") as Style;
				}
				saveSetting();
			};

			buttonLeftSwitch.Click += (o, e) => {
				isLeftSwitchOn = !isLeftSwitchOn;
				if (isLeftSwitchOn) {
					windowSwitch.Visibility = Visibility.Visible;
					buttonLeftSwitch.Style = FindResource("TransparentButtonLeftOn") as Style;
				} else {
					windowSwitch.Visibility = Visibility.Collapsed;
					buttonLeftSwitch.Style = FindResource("TransparentButtonLeftOff") as Style;
				}
				saveSetting();
			};

			buttonVolume.Click += (o, e) => {
				isVolumeVisible = !isVolumeVisible;
				if (isVolumeVisible) {
					buttonVolume.Style = FindResource("TransparentButtonOn") as Style;
				} else { buttonVolume.Style = FindResource("TransparentButtonOff") as Style; }
				saveSetting();
			};

			//imageSetting.Source = new BitmapImage(new Uri(@"/Resources/delete.png", UriKind.Relative));

			this.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Left + 20;
			this.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2 - this.Height / 2;

			gridMain.RenderTransformOrigin = new Point(0, 0.5);
			gridMain.RenderTransform = new ScaleTransform(0, 0);

			windowSwitch = wSwitch;

			DispatcherTimer dtm = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500), IsEnabled = true };
			dtm.Tick += (o, e) => {
				CultureInfo cultures = CultureInfo.CreateSpecificCulture("ko-KR");
				textblockTime.Text = DateTime.Now.ToString(string.Format("M월 dd일 (ddd) HH:mm", cultures));
			};

			LayoutReform(0);
			for (int i = 0; i < listFiles.Count; i++) { makeItems(listFiles[i], i); }

			buttonModCancel.Click += (o, e) => { AnimateSettings(0, 350); };
			textboxModify.PreviewKeyDown += (o, e) => { if (e.Key == Key.Enter) { buttonModOK_Click(null, null); } };
			buttonModOK.PreviewMouseDown += buttonModOK_Click;
			
			bool isMoving = false, dragRemove = false;
			int nowCursorIndex = -1, nowMovingIndex = -1;

			this.MouseMove += (o, e) => {
				e.Handled = false;
				afterPoint = e.GetPosition(this);
				if (Math.Max(Math.Abs(beforePoint.X - afterPoint.X), Math.Abs(beforePoint.Y - afterPoint.Y)) < 1 && !isMoving) { return; }
				if (Math.Max(Math.Abs(beforePoint.X - afterPoint.X), Math.Abs(beforePoint.Y - afterPoint.Y)) >= 3 && isMoving) { nowModifing = ""; }
				if (nowPressed == "") { return; }

				e.Handled = true;
				if (Math.Max(Math.Abs(beforePoint.X - afterPoint.X), Math.Abs(beforePoint.Y - afterPoint.Y)) >= 5 && !isMoving) {
					isMoving = true;
					gridMove.Visibility = Visibility.Visible;
					imageRemove.Visibility = Visibility.Collapsed;
					imageMove.Opacity = 1;

					int[] ck = new int[gridItems.Children.Count];
					itemsIndex = new int[gridItems.Children.Count];
					itemsPosition = new int[gridItems.Children.Count];
					itemsNewPosition = new int[gridItems.Children.Count];

					for (int i = 0; i < gridItems.Children.Count; i++) { ck[i] = itemsIndex[i] = -1; }
					for (int i = 0; i < gridItems.Children.Count; i++) {
						Grid grid = (Grid)gridItems.Children[i];
						string strSplit = ((string)grid.Tag).Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[0];

						//if (string.Compare((string)grid.Tag, 0, nowPressed, 0, nowPressed.Length) == 0) {
						if (strSplit == nowPressed) {
							grid.Visibility = Visibility.Collapsed;
							nowMovingIndex = i;
							textblockTime.Visibility = Visibility.Collapsed;
							textblockTitle.Visibility = Visibility.Visible;
							textblockTitle.Text = "이동:" + ((string)grid.Tag).Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[1];
						} else {
							ck[(((int)grid.Margin.Top) / 110) * layoutMaxWidth + ((int)grid.Margin.Left) / 100] = i;
						}
					}
					//string temp = ""; 
					int n = 0; nowCursorIndex = -1;
					for (int i = 0; i < gridItems.Children.Count; i++) {
						if (ck[i] >= 0) {
							itemsIndex[ck[i]] = n++;
							itemsPosition[ck[i]] = i;
						} else { nowCursorIndex = i; }
					}
					//textblockTime.Text = temp;

					imageSetting.Source = new BitmapImage(new Uri(@"/Resources/delete.png", UriKind.Relative));
				}
				if (isMoving) {
					Point afterPoint2 = e.GetPosition(gridMain);
					Point toward = new Point(afterPoint2.X - 40, afterPoint2.Y - 40);
					toward.X = Math.Max(toward.X, 0);
					toward.Y = Math.Max(toward.Y, 0);
					toward.X = Math.Min(toward.X, layoutMaxWidth * 100 - 20);
					toward.Y = Math.Min(toward.Y, layoutMaxHeight * 110);
					gridMove.Margin = new Thickness(toward.X, toward.Y, 0, 0);

					if (afterPoint2.X >= 174 && afterPoint2.X <= 222 && afterPoint2.Y >= 24 && afterPoint2.Y <= 46) {
						imageRemove.Visibility = Visibility.Visible;
						imageMove.Opacity = 0.7;
						dragRemove = true;
					} else {
						imageRemove.Visibility = Visibility.Collapsed;
						imageMove.Opacity = 1;
						dragRemove = false;
					}
					afterPoint2 = e.GetPosition(gridItems);
					afterPoint2.X = Math.Max(0, afterPoint2.X);
					afterPoint2.Y = Math.Max(0, afterPoint2.Y);
					afterPoint2.X = Math.Min(gridItems.Width - 10, afterPoint2.X);
					afterPoint2.Y = Math.Min(gridItems.Height - 10, afterPoint2.Y);
					int focusIndex;
					focusIndex = (((int)afterPoint2.Y - 10) / 110) * layoutMaxWidth + ((int)afterPoint2.X - 10) / 100;
					focusIndex = Math.Min(listFiles.Count - 1, focusIndex);

					if (focusIndex != nowCursorIndex) {
						nowCursorIndex = focusIndex;
						for (int i = 0; i < listFiles.Count; i++) {
							if (itemsIndex[i] < 0) { continue; }
							if (itemsIndex[i] >= nowCursorIndex) {
								itemsNewPosition[i] = itemsIndex[i] + 1;
							} else { itemsNewPosition[i] = itemsIndex[i]; }
						}
						for (int i = 0; i < listFiles.Count; i++) {
							if (itemsIndex[i] < 0) { continue; }
							if (itemsPosition[i] != itemsNewPosition[i]) {
								itemsPosition[i] = itemsNewPosition[i];
								((Grid)gridItems.Children[i]).BeginAnimation(Grid.MarginProperty,
									new ThicknessAnimation(new Thickness(10 + 100 * (itemsPosition[i] % layoutMaxWidth), 10 + 110 * (itemsPosition[i] / layoutMaxWidth), 0, 0)
										, TimeSpan.FromMilliseconds(250)) {
											EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
											//EasingFunction = new PowerEase() { Power = 5, EasingMode = EasingMode.EaseInOut }
										});
								//((Grid)gridItems.Children[i]).Margin = new Thickness(10 + 100 * (itemsPosition[i] % x), 10 + 110 * (itemsPosition[i] / x), 0, 0);
							}
						}
					}
					//textblockTime.Text = focusIndex.ToString();
					//textblockTime.Text = afterPoint2.X + " : " + afterPoint2.Y;
				}
			};
			this.MouseUp += (o, e) => {
				e.Handled = false;
				gridMove.Visibility = Visibility.Collapsed;
				foreach(Grid grid in gridItems.Children){ grid.Visibility = Visibility.Visible; }

				if (dragRemove) {
					textblockTime.Visibility = Visibility.Visible;
					textblockTitle.Visibility = Visibility.Collapsed;

					dragRemove = false;
					
					e.Handled = true;

					for (int i = 0; i < gridItems.Children.Count; i++) {
						string str = (string)((Grid)gridItems.Children[i]).Tag;
						string[] str2 = str.Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries);
						if (str2[0] == nowPressed) {
							for (int j = 0; j < listFiles.Count; j++) {
								if (listFiles[j].path == nowPressed) {
									listFiles.RemoveAt(j);
									break;
								}
							}
							gridItems.Children.RemoveAt(i);
							LayoutReform(200);
							saveFileFolder();
							AnimateSettings(0, 0);
							break;
						}
					}
					isMoving = false;
					nowPressed = "";
				} else {
					if (isMoving) {
						textblockTime.Visibility = Visibility.Visible;
						textblockTitle.Visibility = Visibility.Collapsed;
						isMoving = false;
						imageSetting.Source = new BitmapImage(new Uri(@"/Resources/settings.png", UriKind.Relative));
						((Grid)gridItems.Children[nowMovingIndex]).BeginAnimation(Grid.MarginProperty,
									new ThicknessAnimation(new Thickness(10 + 100 * (nowCursorIndex % layoutMaxWidth), 10 + 110 * (nowCursorIndex / layoutMaxWidth), 0, 0)
										, TimeSpan.FromMilliseconds(0)) {
											EasingFunction = new PowerEase() { Power = 5, EasingMode = EasingMode.EaseInOut }
										});

						itemsPosition[nowMovingIndex] = nowCursorIndex;
						//textblockTime.Text = nowMovingIndex + " : " + nowCursorIndex;
						for (int i = 0; i < listFiles.Count; i++) { itemsNewPosition[itemsPosition[i]] = i; }

						StreamWriter sws = new StreamWriter(pathSetting);
						for (int i = 0; i < listFiles.Count; i++) {
							Grid grid = (Grid)gridItems.Children[itemsNewPosition[i]];
							string[] splitPath = ((string)grid.Tag).Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries);
							sws.WriteLine(splitPath[0] + "#!SIMPLAUNCHER!#" + splitPath[1] + "#!SIMPLAUNCHER!#" + splitPath[2]);
						}
						sws.Flush(); sws.Close();

						string temp = "";
						for (int i = 0; i < listFiles.Count; i++) {
							temp += itemsNewPosition[i] + ".";
						}
						//textblockTime.Text = temp;
					} else { e.Handled = false; }
				}

				nowPressed = "";
			};

			this.PreviewKeyDown += MainWindow_PreviewKeyDown;

			this.Closing += (o, e) => {
				if (!isRealClose) { e.Cancel = true; }
				try {
					volumeWindow.isRealClose = true;
					volumeWindow.Close();
				} catch { }
			};
			this.Deactivated += (o, e) => { if (isShown) { AnimateWindow(0); } };
			polygonTime.PreviewMouseDown += (o, e) => { e.Handled = true; };
			gridTransparent.PreviewMouseDown += (o, e) => { AnimateSettings(0, 350); };
			this.Show();
		}

		private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) { return; }
			textblockTime.Text = e.Key.ToString();
		}

		private void makeItems(InData idata, int i) {
			Grid grid = new Grid() {
				Width = 90, Height = 100, Background = Brushes.Transparent,
				Margin = new Thickness(10 + 100 * (i % layoutMaxWidth), 10 + 110 * (i / layoutMaxWidth), 0, 0),
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				VerticalAlignment = System.Windows.VerticalAlignment.Top,
				Tag = (string)idata.path + "#!SIMPLAUNCHER!#" + idata.caption + "#!SIMPLAUNCHER!#" + idata.isSpecial,
			};
			Image img = new Image() {
				Width = 70, Height = 70,
				VerticalAlignment = VerticalAlignment.Top,
				Source = idata.img,
				Margin = new Thickness(5)
			};
			TextBlock txt = new TextBlock() {
				Text = idata.caption,
				Foreground = Brushes.Black,
				Margin = new Thickness(0, 75, 0, 0),
				Style = FindResource("TextblockStyle") as Style,
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			Rectangle rect = new Rectangle() {
				Width = 90, Height = 3, Fill = Brushes.OrangeRed,
				Opacity = 0, Margin = new Thickness(0, 92, 0, 0),
			};
			grid.Children.Add(img);
			grid.Children.Add(txt);
			grid.Children.Add(rect);

			grid.MouseEnter += (o, e) => {
				if (isModifyView) { return; }
				if (nowPressed != "" && nowModifing == "") { return; }

				foreach (Grid g in gridItems.Children) {
					((Rectangle)g.Children[2]).BeginAnimation(Rectangle.OpacityProperty,
						new DoubleAnimation(0, TimeSpan.FromMilliseconds(50)));
				}

				((Rectangle)((Grid)o).Children[2]).BeginAnimation(Rectangle.OpacityProperty,
					new DoubleAnimation(0.4, TimeSpan.FromMilliseconds(50)) {
						EasingFunction = new PowerEase() {
							Power = 5, EasingMode = EasingMode.EaseOut
						}
					}
					);
			};

			grid.MouseLeave += (o, e) => {
				((Rectangle)((Grid)o).Children[2]).BeginAnimation(Rectangle.OpacityProperty,
						new DoubleAnimation(0, TimeSpan.FromMilliseconds(50)));
			};

			grid.MouseDown += (o, e) => {
				if (isModifyView) { return; }
				if (nowPressed != "" && nowModifing == "") { return; }
				((Rectangle)((Grid)o).Children[2]).BeginAnimation(Rectangle.OpacityProperty,
					new DoubleAnimation(1, TimeSpan.FromMilliseconds(0)));
				nowPressed = ((string)((Grid)o).Tag).Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[0];
				nowModifing = nowPressed;

				foreach (InData data in listFiles) {
					if (data.path == nowPressed) {
						imageMove.Source = idata.img;
						break;
					}
				}
				if (e.ChangedButton == MouseButton.Middle) { return; }
				beforePoint = e.GetPosition(this);
			};

			grid.MouseLeftButtonUp += (o, e) => {
				if (isModifyView) { return; }
				if (nowPressed != "" && nowModifing == "") { return; }
				afterPoint = e.GetPosition(this);
				if (Math.Max(Math.Abs(beforePoint.X - afterPoint.X), Math.Abs(beforePoint.Y - afterPoint.Y)) > 1) { return; }

				AnimateWindow(0);
				foreach (Grid g in gridItems.Children) {
					((Rectangle)g.Children[2]).BeginAnimation(Rectangle.OpacityProperty,
						new DoubleAnimation(0, TimeSpan.FromMilliseconds(0)));
				}

				if (nowModifing == "") { return; }
				nowPressed = "";

				StartProcess((string)((Grid)o).Tag);
			};
			grid.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e) {
				if (isModifyView) { return; }
				if (nowPressed != "" && nowModifing == "") { return; }
				((Rectangle)((Grid)sender).Children[2]).BeginAnimation(Rectangle.OpacityProperty,
					new DoubleAnimation(0, TimeSpan.FromMilliseconds(0)));

				afterPoint = e.GetPosition(this);
				if (Math.Max(Math.Abs(beforePoint.X - afterPoint.X), Math.Abs(beforePoint.Y - afterPoint.Y)) > 1) { return; }
				if (nowModifing == "") { return; }

				string filePath = ((string)((Grid)sender).Tag).Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[0];
				string fileCaption = ((string)((Grid)sender).Tag).Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[1];
				isModifyView = true;
				imageSetting.Source = new BitmapImage(new Uri(@"/Resources/delete.png", UriKind.Relative));

				stackModify.Visibility = Visibility.Visible;
				textblockTitle.Visibility = Visibility.Visible;
				textblockTime.Visibility = Visibility.Collapsed;

				textblockTitle.Text = "수정:" + fileCaption;
				textboxModify.Text = fileCaption;
				textblockModify.Text = "파일 위치 : " + filePath;
				textboxModify.Focus(); textboxModify.SelectAll();
				AnimateSettings(1, 350);
			};
			gridItems.Children.Add(grid);
		}

		private void StartProcess(string tagText) {
			string filePath = tagText.Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[0];
			bool isSpec = Convert.ToBoolean(tagText.Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries)[2]);
			Process pro = new Process();
			if (isSpec) {
				if (filePath == "휴지통") {
					pro.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\explorer.exe";
					pro.StartInfo.Arguments = "e,::{645FF040-5081-101B-9F08-00AA002F954E}";
				}
			} else {
				if (File.Exists(filePath)) {
					pro.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(filePath);
				} else if (Directory.Exists(filePath)) {
				} else { return; }
				pro.StartInfo.FileName = filePath;
			}
			pro.Start();
		}

		public void addItems(List<string> files) {
			//MessageBox.Show(files.Count.ToString());
			//return;

			files.Sort();
			for (int i = 0; i < files.Count; i++) {
				bool flag = true;
				for (int j = 0; j < listFiles.Count; j++) {
					if (listFiles[j].path == files[i]) {
						flag = false; break;
					}
				}

				
				if (!flag) { continue; }
				string cap = "";
				try {
					if (File.Exists(files[i])) {
						cap = System.IO.Path.GetFileNameWithoutExtension(files[i]);
					} else { cap = System.IO.Path.GetFileName(files[i]); }
				} catch {
					cap = files[i];
				}
				if (cap == "") { cap = files[i]; }

				InData idt = new InData() {
					path = files[i], caption = cap,
					isSpecial = false,
				};
				idt.img = rtIconImage(files[i]);
				listFiles.Add(idt);
				makeItems(idt, listFiles.Count - 1);
			}

			files.Clear();
			saveFileFolder();
			LayoutReform(0);
			AnimateWindow(1);
		}

		private void LayoutReform(double Duration) {
			int nc = (int)Math.Sqrt(listFiles.Count / 2);
			layoutMaxHeight = Math.Max(1, nc);
			for (int i = nc; ; i++) {
				if (i * layoutMaxHeight >= listFiles.Count) { layoutMaxWidth = i; break; }
			}
			layoutMaxWidth = Math.Max(3, layoutMaxWidth);

			//textblockTime.Text = x + " : " + y + " : " + fileList.Count;

			List<KeyValuePair<double, string>> listLocation = new List<KeyValuePair<double, string>>();
			foreach (UIElement uie in gridItems.Children) {
				listLocation.Add(new KeyValuePair<double, string>(
					((Grid)uie).Margin.Top * 10000 + ((Grid)uie).Margin.Left,
					(string)((Grid)uie).Tag));
			}
			listLocation = listLocation.OrderBy(k => k.Key).ToList();

			DispatcherTimer dtm = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(Duration + 50), IsEnabled = true };
			dtm.Tick += (o, e) => {
				((DispatcherTimer)o).Stop();
				windowMain.Width = layoutMaxWidth * 100 + 60; windowMain.Height = layoutMaxHeight * 110 + 80;
				stackOutside.Width = layoutMaxWidth * 100 + 20; stackOutside.Height = layoutMaxHeight * 110 + 40;
				gridContents.Width = layoutMaxWidth * 100 + 20; gridContents.Height = layoutMaxHeight * 110 + 10;
				gridItems.Width = layoutMaxWidth * 100 + 10; gridItems.Height = layoutMaxHeight * 110 + 10;
				gridItemsBackground.Width = layoutMaxWidth * 100 + 10; gridItemsBackground.Height = layoutMaxHeight * 110 + 10;
				gridTransparent.Width = layoutMaxWidth * 100 + 10; gridTransparent.Height = layoutMaxHeight * 110 + 10;
				this.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2 - this.Height / 2;
			};

			foreach (UIElement uie in gridItems.Children) {
				for (int i = 0; i < listLocation.Count; i++) {
					if (listLocation[i].Value == (string)((Grid)uie).Tag) {
						((Grid)uie).BeginAnimation(Grid.MarginProperty,
									new ThicknessAnimation(new Thickness(10 + 100 * (i % layoutMaxWidth), 10 + 110 * (i / layoutMaxWidth), 0, 0), TimeSpan.FromMilliseconds(Duration)) {
										EasingFunction = new PowerEase() {
											Power = 5, EasingMode = EasingMode.EaseInOut
										}, BeginTime = TimeSpan.FromMilliseconds(50),
									}
								);
						break;
					}
				}
			}
		}

		private void saveFileFolder() {
			List<KeyValuePair<double, string>> listLocation = new List<KeyValuePair<double, string>>();
			foreach (UIElement uie in gridItems.Children) {
				listLocation.Add(new KeyValuePair<double, string>(
					((Grid)uie).Margin.Top * 10000 + ((Grid)uie).Margin.Left,
					(string)((Grid)uie).Tag));
			}
			listLocation = listLocation.OrderBy(k => k.Key).ToList();

			StreamWriter sws = new StreamWriter(pathSetting);
			foreach (KeyValuePair<double, string> val in listLocation) {
				string[] splitPath = val.Value.Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.None);
				sws.WriteLine(splitPath[0] + "#!SIMPLAUNCHER!#" + splitPath[1] + "#!SIMPLAUNCHER!#" + splitPath[2]);
			}
			sws.Flush(); sws.Close();
		}

		//delegate(object sender, MouseButtonEventArgs e) {
		void buttonModOK_Click(object sender, RoutedEventArgs e) {
			for (int i = 0; i < listFiles.Count; i++) {
				if (listFiles[i].path == nowModifing) {
					string test = listFiles[i].path + "#!SIMPLAUNCHER!#" + listFiles[i].caption + "#!SIMPLAUNCHER!#" + listFiles[i].isSpecial;
					foreach (UIElement uie in gridItems.Children) {
						if ((string)test == (string)((Grid)uie).Tag) {
							((Grid)uie).Tag = (string)listFiles[i].path + "#!SIMPLAUNCHER!#" + textboxModify.Text + "#!SIMPLAUNCHER!#" + listFiles[i].isSpecial;
							((TextBlock)((Grid)uie).Children[1]).Text = textboxModify.Text;
							break;
						}
					}

					InData data = listFiles[i];
					data.caption = textboxModify.Text;
					listFiles.RemoveAt(i);
					listFiles.Insert(i, data);
					saveFileFolder();
					AnimateSettings(0, 350);
					break;
				}
			}
		}

		string nowModifing, nowPressed = "";
		MMDevice device;
		MMDeviceEnumerator DevEnum;
		bool isMuted; int nowVolume;

		private void windowMain_Loaded(object sender, RoutedEventArgs e) {
			volumeWindow = new VolumeWindow(this);
			new AltTab().HideAltTab(this);


			DevEnum = new MMDeviceEnumerator();
			device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
			isMuted = device.AudioEndpointVolume.Mute;
			device.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;

			DispatcherTimer dtm = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(1), IsEnabled = true };
			dtm.Tick += (o, ex) => {
				DevEnum = new MMDeviceEnumerator();
				device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
				isMuted = device.AudioEndpointVolume.Mute;
				device.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
			};
			dtm.Start();
		}

		VolumeWindow volumeWindow;
		private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data) {
			if (isMuted == data.Muted && nowVolume == (int)(data.MasterVolume * 100)) { return; }
			nowVolume = (int)(data.MasterVolume * 100);
			isMuted = data.Muted;

			/*
			Dispatcher.Invoke(new Action(() => {
				textInnerDebug.Text = nowVolume.ToString();
			}));
			 */

			Dispatcher.Invoke(new Action(() => {
				volumeWindow.RefreshVolume(nowVolume, data.Muted);
				volumeWindow.Show();
			}));
		}

		public bool isShown = false;
		public void AnimateWindow(double isShowing) {
			if (isShowing != 0) { isShown = true; } else { isShown = false; }
			if (isShown) {
				isSettingVisible = 0;
				gridTransparent.Visibility = Visibility.Collapsed;
				textblockTitle.Visibility = Visibility.Collapsed;
				textblockTime.Visibility = Visibility.Visible;
				stackItems.BeginAnimation(StackPanel.MarginProperty, new ThicknessAnimation(
					new Thickness(0), TimeSpan.FromMilliseconds(0)));
				gridItems.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(
					1, TimeSpan.FromMilliseconds(0)));
			} else { windowSwitch.canTouch = false; }

			isModifyView = false;
			imageSetting.Source = new BitmapImage(new Uri(@"/Resources/settings.png", UriKind.Relative));

			//rectSelect.Opacity = 0;

			Storyboard sb = new Storyboard();
			sb.Duration = TimeSpan.FromMilliseconds(300);

			DoubleAnimation da1 = new DoubleAnimation(isShowing, TimeSpan.FromMilliseconds(200));
			DoubleAnimation da2 = new DoubleAnimation(isShowing, TimeSpan.FromMilliseconds(200));

			da1.EasingFunction = new BackEase() { Amplitude = 0.3, EasingMode = EasingMode.EaseInOut };
			da2.EasingFunction = new BackEase() { Amplitude = 0.3, EasingMode = EasingMode.EaseInOut };

			Storyboard.SetTarget(da1, gridMain); Storyboard.SetTarget(da2, gridMain);
			Storyboard.SetTargetProperty(da1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
			Storyboard.SetTargetProperty(da2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

			DoubleAnimation da3 = new DoubleAnimation(isShowing, TimeSpan.FromMilliseconds(200));
			Storyboard.SetTarget(da3, this);
			Storyboard.SetTargetProperty(da3, new PropertyPath(Window.OpacityProperty));
			sb.Children.Add(da3);

			sb.Children.Add(da1); sb.Children.Add(da2);
			sb.Completed += delegate(object sender, EventArgs e) {
				windowSwitch.canTouch = true;
				if (isShown) { this.Activate(); }
			};

			sb.Begin(this);
		}

		double isSettingVisible = 0;
		bool isModifyView = false;
		private void ButtonSetting_Click(object sender, MouseButtonEventArgs e) {
			if (isModifyView) {
				for (int i = 0; i < gridItems.Children.Count; i++) {
					string str = (string)((Grid)gridItems.Children[i]).Tag;
					string[] str2 = str.Split(new string[] { "#!SIMPLAUNCHER!#" }, StringSplitOptions.RemoveEmptyEntries);
					if (str2[0] == nowModifing) {
						for (int j = 0; j < listFiles.Count; j++) {
							if (listFiles[j].path == nowModifing) {
								listFiles.RemoveAt(j);
								break;
							}
						}
						gridItems.Children.RemoveAt(i);
						LayoutReform(200);
						AnimateSettings(0, 0);
						saveFileFolder();
						break;
					}
				}
			} else {
				stackModify.Visibility = Visibility.Collapsed;
				if (isSettingVisible != 0) {
					AnimateSettings(0, 350);
				} else { AnimateSettings(1, 350); }
			}
		}

		private void AnimateSettings(double isShowing, double duration) {
			isSettingVisible = isShowing;
			if (isSettingVisible != 0) {
				gridTransparent.Visibility = Visibility.Visible;
			} else {
				textblockTitle.Visibility = Visibility.Collapsed;
				textblockTime.Visibility = Visibility.Visible;
				gridTransparent.Visibility = Visibility.Collapsed;
			}
			Storyboard sb = new Storyboard();

			ThicknessAnimation ta = new ThicknessAnimation(new Thickness(200 * isSettingVisible, 0, 0, 0), TimeSpan.FromMilliseconds(duration)) {
				EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut },
				BeginTime = TimeSpan.FromMilliseconds(100)
			};
			DoubleAnimation da = new DoubleAnimation(1 - 0.75 * isSettingVisible, TimeSpan.FromMilliseconds(duration)) {
				EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut },
				BeginTime = TimeSpan.FromMilliseconds(100)
			};

			Storyboard.SetTarget(ta, stackItems);
			Storyboard.SetTargetProperty(ta, new PropertyPath(StackPanel.MarginProperty));
			Storyboard.SetTarget(da, gridItems);
			Storyboard.SetTargetProperty(da, new PropertyPath(Grid.OpacityProperty));

			if (isSettingVisible == 0) { imageSetting.Source = new BitmapImage(new Uri(@"/Resources/settings.png", UriKind.Relative)); }

			sb.Children.Add(ta);
			sb.Children.Add(da);
			sb.Completed += (o, e) => {
				if (isSettingVisible == 0) {
					isModifyView = false;
				}
			};
			sb.Begin(this);
		}

		private void saveSetting() {
			StreamWriter sws = new StreamWriter(pathSettingPref);
			sws.WriteLine("STARTUP=" + isWindowsStartup);
			sws.WriteLine("LEFTSWITCH=" + isLeftSwitchOn);
			sws.WriteLine("VOLUME=" + isVolumeVisible);
			sws.Flush(); sws.Close();
		}

		private BitmapSource rtIconImage(string path) {
			if (!Directory.Exists(path) && !File.Exists(path)) { return null; }
			BitmapSource img = null;
			try {
				img = ExtractIcon.Get(path);
			} catch { }
			RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Fant);
			return img;
		}
	}

	public struct InData {
		public string caption, path;
		public BitmapSource img;
		public bool isSpecial;
	}
}

