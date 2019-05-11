using D2RoshTimer.Properties;
using Dota2GSI;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using NHotkey.Wpf;
using Dota2GSI.Nodes;

namespace D2RoshTimer {
	public partial class MainWindow : Window {
		private DateTime lastRun = DateTime.MinValue;
		private int currentTime = -200;
		private DOTA_GameState gamestate = DOTA_GameState.Undefined;

		public MainWindow() {
			InitializeComponent();
			this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
			this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
			this.Topmost = true;
			quietSetCheckbox();
			printKeys(Settings.Default.KeyBind);
			createGsiFile();
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(onGotMouseCapture));
		}

		// Set the Checkboxes without triggering modifierCheckBoxChange
		private void quietSetCheckbox() {
			IEnumerable<CheckBox> collection = MainCanvas.Children.OfType<CheckBox>();
			foreach(CheckBox box in collection) {
				altCheckbox.Checked -= modifierCheckBoxChange;
			}
			altCheckbox.IsChecked = Settings.Default.AltModifier;
			controlCheckbox.IsChecked = Settings.Default.ControlModifier;
			shiftCheckbox.IsChecked = Settings.Default.ShiftModifier;
			windowsCheckbox.IsChecked = Settings.Default.WindowsModifier;
			foreach(CheckBox box in collection) {
				altCheckbox.Checked += modifierCheckBoxChange;
			}
		}

		// Output current keybind to textbox - Special to Output the key as is on Keyboard, not Key Name as a variable.
		private void printKeys(Key key) {
			int virtKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			GetKeyboardState(keyboardState);
			uint scanCode = MapVirtualKey((uint)virtKey, MapType.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);
			int result = ToUnicode((uint)virtKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			switch(result) {
				case -1:
					keyTextbox.Text = key.ToString();
					break;
				case 0:
					keyTextbox.Text = key.ToString();//check gamestate in progress or pregame
					break;
				case 1: {
					if(key == Key.Enter) {
						keyTextbox.Text = "Enter";
						break;
					}
					if(key == Key.Tab) {
						keyTextbox.Text = "Tab";
						break;
					}
					keyTextbox.Text = stringBuilder[0].ToString().ToUpper();
					if(key.ToString().StartsWith("NumPad") || key.ToString().Equals("Multiply") || key.ToString().Equals("Divide") || key.ToString().Equals("Add") || key.ToString().Equals("Subtract")) {
						keyTextbox.AppendText("(NumPad)");
					}
					break;
				}
				default: {
					keyTextbox.Text = stringBuilder[0].ToString().ToUpper();
					break;
				}
			}
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
			stringBuilder = null;
		}

		// Used for Dota2GSI - Create config file in dota cfg folder to access json data from local dota client.
		private void createGsiFile() {
			RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570\");
			if(regKey != null) {
				string gsiFolder = regKey.GetValue("InstallLocation") + @"\game\dota\cfg\gamestate_integration";
				Directory.CreateDirectory(gsiFolder);
				string gsifile = gsiFolder + @"\gamestate_integration_roshtimer.cfg";
				if(File.Exists(gsifile)) {
					return;
				}
				string[] contentofgsifile = {
						"\"Dota 2 Integration Configuration\"",
						"{",
						"    \"uri\"           \"http://localhost:42345\"",
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
		}

		// When selecting the textbox to enter new key, clear it
		private void onGotMouseCapture(object sender, MouseEventArgs e) {
			keyTextbox.Text = "";
		}

		// Take any keypress for main key except for both alt, both control, both shift as they are modifier keys. Set as new keybind and update UI (Do I need to block windows key?)
		private void keyTextbox_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key != Key.LeftAlt && e.Key != Key.RightAlt && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftShift
												&& e.Key != Key.RightShift && e.Key != Key.RWin && e.Key != Key.LWin && e.Key != Key.System) {
				Settings.Default.KeyBind = e.Key;
				printKeys(Settings.Default.KeyBind);
			}
		}

		// Catch and skip copy/cut/paste commands
		private void keyTextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e) {
			if(e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste) {
				e.Handled = true;
			}
		}

		// Any time a user manually changes a checkbok for modifier keys, update settings and UI
		private void modifierCheckBoxChange(object sender, RoutedEventArgs e) {
			CheckBox box = (CheckBox)sender;
			bool flag = box.IsChecked.Value;
			if(box.Content.Equals("Alt")) {
				Settings.Default.AltModifier = flag;
			} else if(box.Content.Equals("Control")) {
				Settings.Default.ControlModifier = flag;
			} else if(box.Content.Equals("Shift")) {
				Settings.Default.ShiftModifier = flag;
			} else {
				Settings.Default.WindowsModifier = flag;
			}
			printKeys(Settings.Default.KeyBind);
		}

		// When registered hotkey is pressed, calculate minutes and seconds from currentTime, construct output and copy to clipboard
		private void hotKeyManagerPressed(object sender, EventArgs e) {
			TimeSpan offset = new TimeSpan(0, 0, 4);
			Process[] proc = Process.GetProcessesByName("dota2");
			if(proc.Length > 0 && proc[0].ToString().Equals("System.Diagnostics.Process (dota2)") && DateTime.Compare(DateTime.Now, lastRun.Add(offset)) >= 0) {
				GameStateListener gsl;
				using(gsl = new GameStateListener(42345)) {
					gsl.NewGameState += onNewGameState;
					if(!gsl.Start() && !gsl.Running) { }
					int tries = 0;
					while(currentTime <= -200 && tries < 20) {
						Thread.Sleep(100);
					}
					gsl.NewGameState += onNewGameState;
					tries = 0;
				}
				if(currentTime > -200 && (gamestate == DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS || gamestate == DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME)) {
					string killTime = "", aegisTime = "", earlyTime = "", lateTime = "";
					int minutes = currentTime / 60;
					int seconds = Math.Abs(currentTime % 60);
					lastRun = DateTime.Now;
					if(currentTime >= 0) {
						killTime = "Kill " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						minutes += 5;
						aegisTime = "Aegis Reclaim " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						minutes += 3;
						earlyTime = "Early Spawn " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						minutes += 3;
						lateTime = "Late Spawn " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
					} else {
						int temp = 300 - Math.Abs(currentTime);
						int newMinutes = temp / 60;
						int newSeconds = temp % 60;
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
					string data = killTime + aegisTime + earlyTime + lateTime;
					currentTime = -200;
					gamestate = DOTA_GameState.Undefined;
					for(int i = 0;i < 10;i++) {
						try {
							Clipboard.SetDataObject(data);
							return;
						} catch(COMException ex) {
							const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;
							if((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN) {
								throw;
							}
						}
					}
				}
				return;
			}
		}

		// As the game updates, repeatedly get in-game time as seconds.
		private void onNewGameState(GameState gs) {
			currentTime = gs.Map.ClockTime;
			gamestate = gs.Map.GameState;
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

		// Reset all settings to default 
		private void default_Click(object sender, RoutedEventArgs e) {
			Settings.Default.ControlModifier = false;
			Settings.Default.ShiftModifier = false;
			Settings.Default.WindowsModifier = false;
			Settings.Default.AltModifier = true;
			Settings.Default.KeyBind = Key.O;
			quietSetCheckbox();
			printKeys(Settings.Default.KeyBind);
		}

		// Save settings and close window
		private void ok_Click(object sender, RoutedEventArgs e) {
			if(keyTextbox.Text.Equals("")) {
				Settings.Default.KeyBind = Key.O;
				printKeys(Settings.Default.KeyBind);
			}
			formHotkey();
			Settings.Default.Save();
			Application.Current.MainWindow.Hide();
		}

		// Utility for displaying characters correct from keyboard
		public enum MapType : uint {
			MAPVK_VK_TO_VSC = 0x0,
			MAPVK_VSC_TO_VK = 0x1,
			MAPVK_VK_TO_CHAR = 0x2,
			MAPVK_VSC_TO_VK_EX = 0x3,
		}
		[DllImport("user32.dll")]
		public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
		[DllImport("user32.dll")]
		public static extern bool GetKeyboardState(byte[] lpKeyState);
		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint uCode, MapType uMapType);
	}
}
