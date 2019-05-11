﻿using System;
using System.Windows;
using System.Windows.Input;

namespace D2RoshTimer {
	public class NotifyIconViewModel {
		public ICommand ShowWindowCommand {
			get {
				return new DelegateCommand {
					CommandAction = () => {
						Application.Current.MainWindow.Show();
					}
				};
			}
		}
		public ICommand HideWindowCommand {
			get {
				return new DelegateCommand {
					CommandAction = () => Application.Current.MainWindow.Close(),
				};
			}
		}
		public ICommand ExitApplicationCommand {
			get {
				return new DelegateCommand {CommandAction = () => Application.Current.Shutdown()};
			}
		}
	}
	public class DelegateCommand : ICommand {
		public Action CommandAction { get; set; }
		public Func<bool> CanExecuteFunc { get; set; }
		public void Execute(object parameter) {
			CommandAction();
		}
		public bool CanExecute(object parameter) {
			return CanExecuteFunc == null  || CanExecuteFunc();
		}
		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
	}
}
