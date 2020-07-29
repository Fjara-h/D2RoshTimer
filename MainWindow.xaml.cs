using D2RoshTimer.Properties;
using Dota2GSI.Nodes;
using Dota2GSI;
using Microsoft.Win32;
using NHotkey.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System;
using WindowsInput;
using System.Net.NetworkInformation;

// This is a utility for Dota 2. When roshan is killed, the user presses their chosen hotkey and the following will be copied to their clipboard:
//	Roshan's death time, Aegis reclaim time (assuming it is picked up immediately), Roshan's earliest spawn time, Roshan's latest spawn time

namespace D2RoshTimer {
	public static class Globals {
		public const string procName = "dota2";
		public const string dotaUninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570\";
	}
	public partial class MainWindow : Window {

		private DateTime lastRun = DateTime.MinValue;
		private long currentMatchID = 0, lastRunMatchID = -1;
		private int lastTime = -300;
		private int currentTime = -200;
		private DOTA_GameState gamestate = DOTA_GameState.Undefined;

		public MainWindow() {
			InitializeComponent();
			this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
			this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
			this.Topmost = true;
			initializeSettings();			
			createGsiFile();
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(onGotMouseCapture));
			createOverlay("","","","");
		}

		// Apply saved/default settings to the current menu controls
		private void initializeSettings() {
			printKeys();
			setSizeTextbox.Text = Settings.Default.OverlaySize.ToString();
			setXTextbox.Text = Settings.Default.OverlayXPosition.ToString();
			setYTextbox.Text = Settings.Default.OverlayYPosition.ToString();
			IEnumerable<CheckBox> collection = MainCanvas.Children.OfType<CheckBox>();
			foreach(CheckBox box in collection) {
				box.Checked -= modifierCheckBoxChange;
			}
			altCheckbox.IsChecked = Settings.Default.AltModifier;
			controlCheckbox.IsChecked = Settings.Default.ControlModifier;
			shiftCheckbox.IsChecked = Settings.Default.ShiftModifier;
			windowsCheckbox.IsChecked = Settings.Default.WindowsModifier;
			overlayEnableCheckbox.IsChecked = Settings.Default.OverlayDisplay;
			overlayOrientationCheckbox.IsChecked = Settings.Default.VerticalOverlayDisplay;
			overlayButtonsCheckbox.IsChecked = Settings.Default.OverlayButtonDisplay;
			killCheckbox.IsChecked = Settings.Default.KillDisplay;
			aegisCheckbox.IsChecked = Settings.Default.AegisDisplay;
			earlyCheckbox.IsChecked = Settings.Default.EarlyDisplay;
			lateCheckbox.IsChecked = Settings.Default.LateDisplay;
			longOutputCheckbox.IsChecked = Settings.Default.LongOutput;
			errorOutputCheckbox.IsChecked = Settings.Default.ErrorOutput;
			foreach(CheckBox box in collection) {
				box.Checked += modifierCheckBoxChange;
			}
		}

		// Used for Dota2GSI - Create config file in ..\dota\cfg folder to access json data from local dota client
		private void createGsiFile() {
			RegistryKey regKey = Registry.LocalMachine.OpenSubKey(Globals.dotaUninstallKey);
			if(regKey != null) {
				string gsiFolder = regKey.GetValue("InstallLocation") + @"\game\dota\cfg\gamestate_integration";
				if(!Directory.Exists(gsiFolder)) {
					Directory.CreateDirectory(gsiFolder);
				}
				string gsifile = gsiFolder + @"\gamestate_integration_D2RoshTimer.cfg";
				if(!File.Exists(gsifile)) {
					string[] contentofgsifile = {
						"\"Dota 2 Integration Configuration\"",
						"{",
						"    \"uri\"           \"http://localhost:42340\"",
						"    \"timeout\"       \"5.0\"",
						"    \"buffer\"        \"0.1\"",
						"    \"throttle\"      \"0.1\"",
						"    \"heartbeat\"     \"30.0\"",
						"    \"data\"",
						"    {",
						"        \"map\"           \"1\"",
						"    }",
						"}",

					};
					File.WriteAllLines(gsifile, contentofgsifile);
				}
			} else if(Settings.Default.ErrorOutput) {
				MessageBox.Show("Registry key for Dota 2 not found, cannot create Gamestate Integration file.");
			}
		}

		// When selecting the keyTextbox to enter new key, clear it
		private void onGotMouseCapture(object sender, MouseEventArgs e) {
			if(((TextBox)sender).Name.Equals(keyTextbox.Name)) {
				keyTextbox.Text = "";
			}
		}

		// Pressing on non-selectable UI causes drag if held and moved
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			base.OnMouseLeftButtonDown(e);
			this.DragMove();
		}

		// Allow any keybind except L/R alt, L/R control, L/R shift, or L/R windows key (all modifiers). Set in settings and update UI
		private void keyTextbox_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key != Key.LeftAlt && e.Key != Key.RightAlt && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftShift && e.Key != Key.RightShift && e.Key != Key.RWin && e.Key != Key.LWin && e.Key != Key.System) {
				Settings.Default.KeyBind = e.Key;
				printKeys();
			}
		}

		// Catch and skip copy/cut/paste commands
		private void textBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e) {
			if(e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste) {
				e.Handled = true;
			}
		}

		// Output current keybind to textbox
		private void printKeys() {
			keyTextbox.Text = getRealKey(Settings.Default.KeyBind);
			if(Settings.Default.AltModifier) {
				keyTextbox.AppendText(" + Alt");
			}
			if(Settings.Default.ControlModifier) {
				keyTextbox.AppendText(" + Control");
			}
			if(Settings.Default.ShiftModifier) {
				keyTextbox.AppendText(" + Shift");
			}
			if(Settings.Default.WindowsModifier) {
				keyTextbox.AppendText(" + Windows");
			}
			Keyboard.ClearFocus();
		}

		// Allow up to 4 digits, update textbox display, update setting, call update overlay
		private void xyTextbox_KeyDown(object sender, KeyEventArgs e) {
			string key = getRealKey(e.Key);
			if((((TextBox)sender).Text.Length < 4) && (key.Equals("0") || key.Equals("1") || key.Equals("2") || key.Equals("3") || key.Equals("4") || key.Equals("5") || key.Equals("6") || key.Equals("7") || key.Equals("8") || key.Equals("9"))) {
				if(((TextBox)sender).Name.Equals(setXTextbox.Name)) {
					Settings.Default.OverlayXPosition = Int32.Parse(setXTextbox.Text);
				} else if(((TextBox)sender).Name.Equals(setYTextbox.Name)) {
					Settings.Default.OverlayYPosition = Int32.Parse(setYTextbox.Text);
				}
				updateOverlay();
			} else { e.Handled = true; }
			
		}

		// get position of key placement?
		// Allow up to 4 digits, a seperator (, or ,), 2 more digits
		private void sizeTextbox_KeyDown(object sender, KeyEventArgs e) {
			string key = getRealKey(e.Key);
			bool seperator = (setSizeTextbox.Text.IndexOf('.') != -1 || setSizeTextbox.Text.IndexOf(',') != -1);
			if(((TextBox)sender).Name.Equals(setSizeTextbox.Name)) {
				if(key.Equals("0") || key.Equals("1") || key.Equals("2") || key.Equals("3") || key.Equals("4") || key.Equals("5") || key.Equals("6") || key.Equals("7") || key.Equals("8") || key.Equals("9")) {
					//substring 
				} else if((key.Equals(".") || key.Equals(",")) && !seperator) {
				} else { e.Handled = true; }
				updateOverlay();
			}
		}

		// Convert Key object to human comprehensible names
		private string getRealKey(Key key) {
			int virtKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			KeyboardHelper.GetKeyboardState(keyboardState);
			StringBuilder stringBuilder = new StringBuilder(2);
			switch(KeyboardHelper.ToUnicode((uint)virtKey, KeyboardHelper.MapVirtualKey((uint)virtKey, KeyboardHelper.mapType.MAPVK_VK_TO_VSC), keyboardState, stringBuilder, stringBuilder.Capacity, 0)) {
				case -1:
				case 0:
					return key.ToString();
				case 1: {
					if(key == Key.Enter) {
						return "Enter";
					}
					if(key == Key.Tab) {
						return "Tab";
					}
					if(key.ToString().StartsWith("NumPad") || key.ToString().Equals("Multiply") || key.ToString().Equals("Divide") || key.ToString().Equals("Add") || key.ToString().Equals("Subtract")) {
						return stringBuilder[0].ToString().ToUpper() + "(NumPad)";
					}
					else {
						return stringBuilder[0].ToString().ToUpper();
					}
				}
				default: {
					return stringBuilder[0].ToString().ToUpper();
				}
			}
		}

		// Give user mouse to click on position of window where to display the overlay
		private void setOverlayLocation_Click(object sender, RoutedEventArgs e) {
			//create overlay iwth placeholder in - will be closed/deleted whwen Done()
		}

		// When user changes a checkbox update UI and settings
		private void modifierCheckBoxChange(object sender, RoutedEventArgs e) {
			CheckBox box = (CheckBox)sender;
			bool flag = box.IsChecked.Value;
			if(box.Content.Equals(altCheckbox.Content)) {
				Settings.Default.AltModifier = flag;
				printKeys();
			} else if(box.Content.Equals(controlCheckbox.Content)) {
				Settings.Default.ControlModifier = flag;
				printKeys();
			} else if(box.Content.Equals(shiftCheckbox.Content)) {
				Settings.Default.ShiftModifier = flag;
				printKeys();
			} else if(box.Content.Equals(windowsCheckbox.Content)) {
				Settings.Default.WindowsModifier = flag;
				printKeys();
			} else if(box.Content.Equals(overlayEnableCheckbox.Content)) {
				Settings.Default.OverlayDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(overlayOrientationCheckbox.Content)) {
				Settings.Default.VerticalOverlayDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(overlayButtonsCheckbox.Content)) {
				Settings.Default.OverlayButtonDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(killCheckbox.Content)) {
				Settings.Default.KillDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(aegisCheckbox.Content)) {
				Settings.Default.AegisDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(earlyCheckbox.Content)) {
				Settings.Default.EarlyDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(lateCheckbox.Content)) {
				Settings.Default.LateDisplay = flag;
				updateOverlay();
			} else if(box.Content.Equals(longOutputCheckbox.Content)) {
				Settings.Default.LongOutput = flag;
				updateOverlay();
			} else if(box.Content.Equals(errorOutputCheckbox.Content)) {
				Settings.Default.ErrorOutput = flag;
				updateOverlay();
			}
		}

		// When registered hotkey is pressed, calculate minutes and seconds from currentTime, construct output and copy to clipboard
		private void hotKeyManagerPressed(object sender, EventArgs e) {

			// This enables the use of the key IF no modifiers are enabled, without this you are unable to use set hotkey in normal operation.
			if(!Settings.Default.AltModifier && !Settings.Default.ControlModifier && !Settings.Default.ShiftModifier && !Settings.Default.WindowsModifier) {
				InputSimulator keySim = new InputSimulator();
				StringBuilder charPressed = new StringBuilder(256);
				KeyboardHelper.ToUnicode((uint)KeyInterop.VirtualKeyFromKey(Settings.Default.KeyBind), 0, new byte[256], charPressed, charPressed.Capacity, 0);
				keySim.Keyboard.TextEntry(charPressed.ToString());
			}
			TimeSpan offset = new TimeSpan(0, 0, 3);
			Process[] proc = Process.GetProcessesByName("dota2");
			if(proc.Length > 0 && proc[0].ToString().Equals("System.Diagnostics.Process (dota2)") && DateTime.Compare(DateTime.Now, lastRun.Add(offset)) >= 0) {
				using(GameStateListener gsl = new GameStateListener(42340)) {
					gsl.NewGameState += onNewGameState;
					if(!gsl.Start() && Settings.Default.ErrorOutput) {
						MessageBox.Show("GameStateListener could not start. Try running as Administrator.");
					}
					int tries = 0;
					// Listen to gamestate data for at most 2 seconds
					while(tries < 20 && currentTime <= -200) {
						Thread.Sleep(100);
						tries++;
					}
					gsl.NewGameState -= onNewGameState;
				}
				// If game time has been updated and gamestate is when it is possible to kill rosh
				if(!(currentMatchID == lastRunMatchID)) {
					lastTime = -300;
				}
				if(currentTime > -200 && (gamestate == DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS || gamestate == DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME) && lastTime + 3 < currentTime) {
					lastRunMatchID = currentMatchID;
					string killTime = "", aegisTime = "", earlyTime = "", lateTime = "";
					int minutes = currentTime / 60;
					int seconds = Math.Abs(currentTime % 60);
					// If rosh was taken after the horn (0:00 clock time), otherwise its before and special math is needed
					if(currentTime >= 0) {
						if(!Settings.Default.LongOutput) {
							killTime = minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							aegisTime = (minutes + 5) + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							minutes += 8;
							earlyTime = minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							minutes += 3;
							lateTime = minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
						} else {
							killTime = "Kill " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							aegisTime = "Aegis Reclaim " + (minutes + 5) + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							minutes += 8;
							earlyTime = "Early Spawn " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							minutes += 3;
							lateTime = "Late Spawn " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
						}
					} else {
						currentTime -= 1; // Update roshCurrentTime to -1 seconds because gameClockTime is actually off by 1 second from in game timer.
						minutes = currentTime / 60;
						seconds = Math.Abs(currentTime % 60);
						int temp = 300 - Math.Abs(currentTime);
						int newMinutes = temp / 60;
						int newSeconds = temp % 60;
						if(!Settings.Default.LongOutput) {
							if(minutes != 0) {
								killTime = "| " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							} else {
								killTime = "| -" + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							}
							aegisTime = newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							newMinutes += 3;
							earlyTime = newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
							newMinutes += 3;
							lateTime = newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
						} else {
							if(minutes != 0) {
								killTime = "Kill " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							} else {
								killTime = "Kill -" + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							}
							aegisTime = "Aegis Reclaim " + newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							newMinutes += 3;
							earlyTime = "Early Spawn " + newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
							newMinutes += 3;
							lateTime = "Late Spawn " + newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
						}
					}
					string data = "";
					if(Settings.Default.KillDisplay) {
						data += killTime;
					}
					if(Settings.Default.AegisDisplay) {
						data += aegisTime;
					}
					if(Settings.Default.EarlyDisplay) {
						data += earlyTime;
					}
					if(Settings.Default.LateDisplay) {
						data += lateTime;
					}
					if(Settings.Default.OverlayDisplay) {
						createOverlay(killTime, aegisTime, earlyTime, lateTime);
					}
					// Reset values to ensure next use has fresh values on this run
					currentTime = -200;
					gamestate = DOTA_GameState.Undefined;
					// Try to set clipboard data, occassionally fails if it is used by something else
					for(int i = 0;i <= 1;i++) {
						try {
							Clipboard.SetDataObject(data);
							i = 5;
						} catch(COMException ex) { }
					}
					lastRun = DateTime.Now;
				} else if (lastTime == currentTime && Settings.Default.ErrorOutput) {
					MessageBox.Show("You are running when the in-game clock is the same.");
				} else if(gamestate != DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS && gamestate != DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME && Settings.Default.ErrorOutput) {
					MessageBox.Show("This only runs when loaded into a game.");
				} else if(Settings.Default.ErrorOutput) {
					MessageBox.Show("GSI failed to update currentTime.");
				}
			} else if(proc.Length == 0 && Settings.Default.ErrorOutput) {
				MessageBox.Show("Dota 2 not running.");
			} else if(Settings.Default.ErrorOutput) {
				MessageBox.Show("You are running again too soon.");
			}
		}

		// As the game data updates, repeatedly set to new time and gamestate.
		private void onNewGameState(GameState gs) {
			currentTime = gs.Map.ClockTime;
			gamestate = gs.Map.GameState;
			currentMatchID = gs.Map.MatchID;
		}

		// Create the over with kill times
		private void createOverlay(string killTime, string aegisTime, string earlyTime, string lateTime) {
			//get the x,y of the dota window, add x and y from settings, dynamically create textboxes for vars above
			//after 11 minutes remove them from display
			Overlay overlay = new Overlay();
			overlay.Show();
		}

		//create overlay
		private void updateOverlay() {

		}

		// Save settings and close window
		private void done_Click(object sender, RoutedEventArgs e) {
			//close/remove/delete placeholder overlay
			if(keyTextbox.Text.Equals("")) {
				Settings.Default.KeyBind = Key.O;
				printKeys();
			}
			if(setXTextbox.Text.Equals("")) {
				Settings.Default.OverlayXPosition = 100;//placeholder value
				updateOverlay();
			}
			if(setYTextbox.Text.Equals("")) {
				Settings.Default.OverlayYPosition = 100;//placeholder value
				updateOverlay();
			}
			if(setSizeTextbox.Text.Equals("")) {
				Settings.Default.OverlaySize = 12;//placeholder value
			}
			formHotkey();
			Settings.Default.Save();
			Application.Current.MainWindow.Hide();
		}

		// Register new hotkey
		private void formHotkey() {
			ModifierKeys mk = ModifierKeys.None;
			if(Settings.Default.AltModifier) {
				mk |= ModifierKeys.Alt;
			}
			if(Settings.Default.ControlModifier) {
				mk |= ModifierKeys.Control;
			}
			if(Settings.Default.ShiftModifier) {
				mk |= ModifierKeys.Shift;
			}
			if(Settings.Default.WindowsModifier) {
				mk |= ModifierKeys.Windows;
			}
			HotkeyManager.Current.AddOrReplace("runTimer", Settings.Default.KeyBind, mk, hotKeyManagerPressed);
		}

		// Shutdown Application
		private void close_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		// Utilities for window controls
		public static class WindowHelper {
			private const int SW_RESTORE = 9;
			[StructLayout(LayoutKind.Sequential)]
			public struct RECT {
				public int Left;
				public int Top;
				public int Right;
				public int Bottom;
			}

			public static void bringProcessToFront(Process process) {
				if(IsIconic(process.MainWindowHandle)) {
					ShowWindow(process.MainWindowHandle, SW_RESTORE);
				}
				SetForegroundWindow(process.MainWindowHandle);
			}
			public static Point getWindowLoc() {
				GetWindowRect(Process.GetProcessesByName(Globals.procName)[0].MainWindowHandle, out RECT rect);
				return new Point(rect.Left, rect.Top);
			}
			public static Point getWindowRes() {
				GetWindowRect(Process.GetProcessesByName(Globals.procName)[0].MainWindowHandle, out RECT rect);
				return new Point(rect.Right - rect.Left, rect.Bottom - rect.Top);
			}

			[DllImport("User32.dll")]
			private static extern bool SetForegroundWindow(IntPtr handle);
			[DllImport("User32.dll")]
			private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
			[DllImport("User32.dll")]
			private static extern bool IsIconic(IntPtr handle);
			[DllImport("User32.dll")]
			private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
		}

		// Utility for displaying characters correct from keyboard
		public static class KeyboardHelper {
			public enum mapType : uint { MAPVK_VK_TO_VSC = 0x0, MAPVK_VSC_TO_VK = 0x1, MAPVK_VK_TO_CHAR = 0x2, MAPVK_VSC_TO_VK_EX = 0x3, }
			[DllImport("user32.dll")]
			public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
			[DllImport("user32.dll")]
			public static extern bool GetKeyboardState(byte[] lpKeyState);
			[DllImport("user32.dll")]
			public static extern uint MapVirtualKey(uint uCode, mapType uMapType);
		}
	}
}
