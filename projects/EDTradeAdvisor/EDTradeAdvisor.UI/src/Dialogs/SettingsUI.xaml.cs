﻿using System;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Common;
using Rylogic.Gui.WPF;

namespace EDTradeAdvisor.UI.Dialogs
{
	public partial class SettingsUI : Window
	{
		static SettingsUI()
		{
			DataPathProperty = Gui_.DPRegister<SettingsUI>(nameof(DataPath));
			EDMCDataPathProperty = Gui_.DPRegister<SettingsUI>(nameof(EDMCDataPath));
		}
		public SettingsUI(Window owner)
		{
			InitializeComponent();
			Owner = owner;
			DataPath = Settings.Instance.DataPath;
			EDMCDataPath = Settings.Instance.EDMCDataPath;
			DataContext = this;
		}

		/// <summary>The directory containing data generated by this tool</summary>
		public string DataPath
		{
			get { return (string)GetValue(DataPathProperty); }
			set { SetValue(DataPathProperty, value); }
		}
		private void DataPath_Changed(string new_value)
		{
			Settings.Instance.DataPath = new_value;
		}
		public static readonly DependencyProperty DataPathProperty;
		public Func<string, ValidationResult> DataPath_Validate = path =>
		{
			return !Path_.IsValidDirectory(path, true)
				? new ValidationResult(false, "Must be a valid directory path")
				: ValidationResult.ValidResult;
		};

		/// <summary>The directory containing data generated by EDMC</summary>
		public string EDMCDataPath
		{
			get { return (string)GetValue(EDMCDataPathProperty); }
			set { SetValue(EDMCDataPathProperty, value); }
		}
		private void EDMCDataPath_Changed(string new_value)
		{
			Settings.Instance.EDMCDataPath = new_value;
		}
		public static readonly DependencyProperty EDMCDataPathProperty;
		public Func<string, ValidationResult> EDMCDataPath_Validate = path =>
		{
			return !string.IsNullOrEmpty(path) && !Path_.DirExists(path)
				? new ValidationResult(false, "Must be empty or a valid directory path")
				: ValidationResult.ValidResult;
		};
	}
}