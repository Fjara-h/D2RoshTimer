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
		private int roshCurrentTime = -200;
		private int aegisCurrentTime = -200;
		private DOTA_GameState gamestate = DOTA_GameState.Undefined;
		private bool hasAegis = false;
		private bool aegisLock = false;
		private bool roshLock = false;

		public MainWindow() {
			InitializeComponent();
			this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
			this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);
			this.Topmost = true;
			errorSwitch();
			quietSetCheckbox();
			printKeys(Settings.Default.KeyBind);
			createGsiFile();
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(onGotMouseCapture));
		}

		// Pressing on non-selectable UI causes drag if held and moved
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			base.OnMouseLeftButtonDown(e);
			this.DragMove();
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
			keyTextbox.Text = getRealKey(key);
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

		// Convert Key object identifiers to human understandable/common use terms
		private string getRealKey(Key key) {
			int virtKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			GetKeyboardState(keyboardState);
			uint scanCode = MapVirtualKey((uint)virtKey, mapType.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);
			int result = ToUnicode((uint)virtKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			string finalKey = "";
			switch(result) {
				case -1:
				case 0:
					finalKey = key.ToString();
					break;
				case 1: {
					if(key == Key.Enter) {
						finalKey = "Enter";
						break;
					}
					if(key == Key.Tab) {
						finalKey = "Tab";
						break;
					}
					finalKey = stringBuilder[0].ToString().ToUpper();
					if(key.ToString().StartsWith("NumPad") || key.ToString().Equals("Multiply") || key.ToString().Equals("Divide") || key.ToString().Equals("Add") || key.ToString().Equals("Subtract")) {
						finalKey += "(NumPad)";
					}
					break;
				}
				default: {
					finalKey = stringBuilder[0].ToString().ToUpper();
					break;
				}
			}
			stringBuilder = null;
			return finalKey;
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
						"        \"provider\"      \"0\"",
						"        \"map\"           \"1\"",
						"        \"player\"        \"0\"",
						"        \"hero\"          \"0\"",
						"        \"abilities\"     \"0\"",
						"        \"items\"         \"1\"",
						"    }",
						"}",

					};
				File.WriteAllLines(gsifile, contentofgsifile);
			} else if(Settings.Default.ErrorDisplay) {
				MessageBox.Show("Registry key for Dota 2 not found, cannot create Gamestate Integration file.");
			}
		}

		// When registered hotkey is pressed, calculate minutes and seconds from currentTime, construct output and copy to clipboard
		private void hotKeyManagerPressed(object sender, EventArgs e) {
			TimeSpan offset = new TimeSpan(0, 0, 1);
			Process[] proc = Process.GetProcessesByName("dota2");
			// If it's been more than 4 seconds since last press, run
			if(proc.Length > 0 && proc[0].ToString().Equals("System.Diagnostics.Process (dota2)") && DateTime.Compare(DateTime.Now, lastRun.Add(offset)) >= 0) {
				GameStateListener gsl;
				using(gsl = new GameStateListener(42345)) {
					gsl.NewGameState += onNewGameState;
					if(!gsl.Start() && !gsl.Running && Settings.Default.ErrorDisplay) {
						MessageBox.Show("GameStateListener could not start. Try running as Administrator.");
					}
					int tries = 0;
					// Listen to gamestate data for 4.5 seconds or until user picks up aegis themselves
					while(tries < 20 && !aegisLock && roshCurrentTime <= -200) {
						Thread.Sleep(100);
						tries++;
					}
					gsl.NewGameState += onNewGameState;
					tries = 0;
				}
				// If game time has been updated and gamestate is where it actually matters, run
				if(currentTime > -200 && (gamestate == DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS || gamestate == DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME)) {
					string killTime = "", aegisTime = "", earlyTime = "", lateTime = "";
					int minutes = roshCurrentTime / 60;
					int seconds = Math.Abs(roshCurrentTime % 60);
					lastRun = DateTime.Now;
					// If rosh was taken after the horn (0:00 clock time), otherwise its before and special math is needed
					if(roshCurrentTime >= 0) {
						killTime = "Kill " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						if(hasAegis) {
							int aegisNewMin = aegisCurrentTime / 60;
							int aegisNewSec = Math.Abs(aegisCurrentTime) % 60;
							aegisTime = "Aegis Reclaim " + aegisNewMin + ":" + aegisNewSec.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						} else {
							aegisTime = "Aegis Reclaim " + (minutes + 5) + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						}
						minutes += 8;
						earlyTime = "Early Spawn " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						minutes += 3;
						lateTime = "Late Spawn " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
					} else {
						int temp = 300 - Math.Abs(roshCurrentTime);
						int newMinutes = temp / 60;
						int newSeconds = temp % 60;
						if(minutes != 0) {
							killTime = "Kill " + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						} else {
							killTime = "Kill -" + minutes + ":" + seconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						}
						if(hasAegis){
							int aegisNewMin = (300 - Math.Abs(aegisCurrentTime)) / 60;
							int aegisNewSec = (300 - Math.Abs(aegisCurrentTime)) % 60;
							aegisTime = "Aegis Reclaim " + aegisNewMin + ":" + aegisNewSec.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						} else {
							aegisTime = "Aegis Reclaim " + (newMinutes + 5) + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						}
						newMinutes += 8;
						earlyTime = "Early Spawn " + newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " |  ";
						newMinutes += 3;
						lateTime = "Late Spawn " + newMinutes + ":" + newSeconds.ToString("D2", CultureInfo.InvariantCulture) + " ";
					}
					string data = killTime + aegisTime + earlyTime + lateTime;
					// Reset values to ensure next use has fresh values.
					currentTime = -200;
					roshCurrentTime = -200;
					aegisCurrentTime = -200;
					gamestate = DOTA_GameState.Undefined;
					hasAegis = false;
					roshLock = false;
					aegisLock = false;
					// Try to set clipboard data, occassionally fails for unknown reasons, try again a few times 
					for(int i = 0;i < 1;i++) {
						try {
							Clipboard.SetDataObject(data);
							return;
						} catch(COMException ex) {
							/*const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;
							if((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN) {
								ex
							}*/
						}
					}
					Thread.Sleep(1000);
				} else if(gamestate != DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS && gamestate != DOTA_GameState.DOTA_GAMERULES_STATE_PRE_GAME && Settings.Default.ErrorDisplay) {
					MessageBox.Show("This only runs when loaded into a game.");
				} else if(Settings.Default.ErrorDisplay) {
					MessageBox.Show("GSI failed to update currentTime.");
				}
			} else if(proc.Length == 0 && Settings.Default.ErrorDisplay) {
				MessageBox.Show("Dota 2 not running.");
			} else if(Settings.Default.ErrorDisplay) {
				MessageBox.Show("You are running again too soon.");
			}
		}

		// As the game updates, repeatedly get in-game time as seconds.
		private void onNewGameState(GameState gs) {
			currentTime = gs.Map.ClockTime;
			// Lock on first instance of game time, so excess runs do not update it causing timer to be off
			if(!roshLock) {
				roshCurrentTime = currentTime;
				roshLock = true;
			}
			gamestate = gs.Map.GameState;
			hasAegis = gs.Items.InventoryContains("item_aegis");
			// If user picks up aegis within the time frame then use an updated timer for aegis expiration timer
			if(hasAegis && !aegisLock) {
				aegisCurrentTime = currentTime;
				aegisLock = true;
			}
			
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

		// Error button click to change values
		private void error_Click(object sender, RoutedEventArgs e) {
			errorSwitch();
		}

		// Enable/Disable Error Popups
		private void errorSwitch() {
			string disable = "Disable Error Popups", enable = "Enable Error Popups";
			if(errorButton.Content.Equals(disable)) {
				Settings.Default.ErrorDisplay = false;
				errorButton.Content = enable;
			} else if(errorButton.Content.Equals(enable)) {
				Settings.Default.ErrorDisplay = true;
				errorButton.Content = disable;
			} else if(Settings.Default.ErrorDisplay) {
				Settings.Default.ErrorDisplay = true;
				errorButton.Content = disable;
			} else {
				Settings.Default.ErrorDisplay = false;
				errorButton.Content = enable;
			}
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
		public enum mapType : uint { MAPVK_VK_TO_VSC = 0x0, MAPVK_VSC_TO_VK = 0x1, MAPVK_VK_TO_CHAR = 0x2, MAPVK_VSC_TO_VK_EX = 0x3,}
		[DllImport("user32.dll")]
		public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
		[DllImport("user32.dll")]
		public static extern bool GetKeyboardState(byte[] lpKeyState);
		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint uCode, mapType uMapType);
	}
}
