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
using WindowsInput;

// This is a utility for Dota 2. When roshan is killed, the user presses their chosen hotkey and the following will be copied to their clipboard:
//	Roshan's death time, Aegis reclaim time (assuming it is picked up immediately), Roshan's earliest spawn time, Roshan's latest spawn time

namespace D2RoshTimer {
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
			errorSwitch();
			outputSwitch();
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

		// Take any keypress for main key except for either alt, either control, either shift, and either windows key as they are modifiers. Set in settings and update UI
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
				box.Checked -= modifierCheckBoxChange;
			}
			altCheckbox.IsChecked = Settings.Default.AltModifier;
			controlCheckbox.IsChecked = Settings.Default.ControlModifier;
			shiftCheckbox.IsChecked = Settings.Default.ShiftModifier;
			windowsCheckbox.IsChecked = Settings.Default.WindowsModifier;
			foreach(CheckBox box in collection) {
				box.Checked += modifierCheckBoxChange;
			}
		}

		// Output current keybind to textbox
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

		// Convert Key object to human comprehensible names
		private string getRealKey(Key key) {
			int virtKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			GetKeyboardState(keyboardState);
			uint scanCode = MapVirtualKey((uint)virtKey, mapType.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);
			int result = ToUnicode((uint)virtKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			switch(result) {
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

		// Any time a user manually changes a checkbox for modifier keys, update UI and settings
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

		// Used for Dota2GSI - Create config file in ..\dota\cfg folder to access json data from local dota client
		private void createGsiFile() {
			RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570\");
			if(regKey != null) {
				string gsiFolder = regKey.GetValue("InstallLocation") + @"\game\dota\cfg\gamestate_integration";
				if(!Directory.Exists(gsiFolder)) {
					Directory.CreateDirectory(gsiFolder);
				}
				string gsifile = gsiFolder + @"\gamestate_integration_D2RandomerSimple.cfg";
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
						"        \"provider\"      \"0\"",
						"        \"map\"           \"1\"",
						"        \"player\"        \"0\"",
						"        \"hero\"          \"0\"",
						"        \"abilities\"     \"0\"",
						"        \"items\"         \"0\"",
						"    }",
						"}",

					};
					File.WriteAllLines(gsifile, contentofgsifile);
				}
			} else if(Settings.Default.ErrorDisplay) {
				MessageBox.Show("Registry key for Dota 2 not found, cannot create Gamestate Integration file.");
			}
		}

		// When registered hotkey is pressed, calculate minutes and seconds from currentTime, construct output and copy to clipboard
		private void hotKeyManagerPressed(object sender, EventArgs e) {
			// This enables the use of the key IF no modifiers are enabled, without this you are unable to use set hotkey in normal operation.
			if(!Settings.Default.AltModifier && !Settings.Default.ControlModifier && !Settings.Default.ShiftModifier && !Settings.Default.WindowsModifier) {
				InputSimulator keySim = new InputSimulator();
				StringBuilder charPressed = new StringBuilder(256);
				ToUnicode((uint)KeyInterop.VirtualKeyFromKey(Settings.Default.KeyBind), 0, new byte[256], charPressed, charPressed.Capacity, 0);
				keySim.Keyboard.TextEntry(charPressed.ToString());
			}
			TimeSpan offset = new TimeSpan(0, 0, 3);
			Process[] proc = Process.GetProcessesByName("dota2");
			if(proc.Length > 0 && proc[0].ToString().Equals("System.Diagnostics.Process (dota2)") && DateTime.Compare(DateTime.Now, lastRun.Add(offset)) >= 0) {
				using(GameStateListener gsl = new GameStateListener(42345)) {
					gsl.NewGameState += onNewGameState;
					if(!gsl.Start() && Settings.Default.ErrorDisplay) {
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
						if(!Settings.Default.LongOutputDisplay) {
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
						if(!Settings.Default.LongOutputDisplay) {
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
					string data = killTime + aegisTime + earlyTime + lateTime;
					// Reset values to ensure next use has fresh values on this run
					currentTime = -200;
					gamestate = DOTA_GameState.Undefined;
					// Try to set clipboard data, occassionally fails for unknown reasons, try again then
					for(int i = 0;i <= 1;i++) {
						try {
							Clipboard.SetDataObject(data);
							i = 5;
						} catch(COMException ex) { }
					}
					lastRun = DateTime.Now;
				} else if (lastTime == currentTime && Settings.Default.ErrorDisplay) {
					MessageBox.Show("You are running when the in-game clock is the same.");
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

		// As the game data updates, repeatedly set to new time and gamestate.
		private void onNewGameState(GameState gs) {
			currentTime = gs.Map.ClockTime;
			gamestate = gs.Map.GameState;
			currentMatchID = gs.Map.MatchID;
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
		
		// Output switch button click to change values
		private void outputSwitch_Click(object sender, RoutedEventArgs e) {
			outputSwitch();
		}

		// Switch between long form (Kill 3:34 | Aegis 8:34 | Early 11:34 | Late 14:34) and short form (3:34 8:34 11:34 14:34)
		private void outputSwitch() {
			string setShort = "Set to short form (3:45 8:45 11:45 14:45)", setLong = "Set to long form (Kill 3:34 | Aegis 8:34 ...)";
			if(outputSwitchButton.Content.Equals(setShort)) {
				Settings.Default.LongOutputDisplay = false;
				outputSwitchButton.Content = setLong;
			} else if(outputSwitchButton.Content.Equals(setLong)) {
				Settings.Default.LongOutputDisplay = true;
				outputSwitchButton.Content = setShort;
			} else if(Settings.Default.LongOutputDisplay) {
				Settings.Default.LongOutputDisplay = true;
				outputSwitchButton.Content = setShort;
			} else {
				Settings.Default.LongOutputDisplay = false;
				outputSwitchButton.Content = setLong;
			}
		}

		// Error switch button click to change values
		private void errorSwitch_Click(object sender, RoutedEventArgs e) {
			errorSwitch();
		}

		// Enable/Disable Error Popups
		private void errorSwitch() {
			string disable = "Disable Error Popups", enable = "Enable Error Popups";
			if(errorSwitchButton.Content.Equals(disable)) {
				Settings.Default.ErrorDisplay = false;
				errorSwitchButton.Content = enable;
			} else if(errorSwitchButton.Content.Equals(enable)) {
				Settings.Default.ErrorDisplay = true;
				errorSwitchButton.Content = disable;
			} else if(Settings.Default.ErrorDisplay) {
				Settings.Default.ErrorDisplay = true;
				errorSwitchButton.Content = disable;
			} else {
				Settings.Default.ErrorDisplay = false;
				errorSwitchButton.Content = enable;
			}
		}

		// Reset all settings to default 
		private void default_Click(object sender, RoutedEventArgs e) {
			Settings.Default.ControlModifier = false;
			Settings.Default.ShiftModifier = false;
			Settings.Default.WindowsModifier = false;
			Settings.Default.AltModifier = true;
			Settings.Default.ErrorDisplay = true;
			errorSwitchButton.Content = "Disable Error Popups";
			Settings.Default.LongOutputDisplay = true;
			outputSwitchButton.Content = "Set to short form (3:45 8:45 11:45 14:45)";
			Settings.Default.KeyBind = Key.O;
			quietSetCheckbox();
			printKeys(Settings.Default.KeyBind);
		}

		// Save settings and close window
		private void done_Click(object sender, RoutedEventArgs e) {
			if(keyTextbox.Text.Equals("")) {
				Settings.Default.KeyBind = Key.O;
				printKeys(Settings.Default.KeyBind);
			}
			formHotkey();
			Settings.Default.Save();
			Application.Current.MainWindow.Hide();
		}

		// Shutdown Application
		private void close_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
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
