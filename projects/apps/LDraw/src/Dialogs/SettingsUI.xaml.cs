﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;
using Rylogic.Windows;

namespace LDraw.Dialogs
{
	public partial class SettingsUI :Window, INotifyPropertyChanged
	{
		public SettingsUI(Window owner, Model model)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			Model = model;

			AvailableFonts = CollectionViewSource.GetDefaultView(Fonts.SystemFontFamilies);
			Profiles = CollectionViewSource.GetDefaultView(Settings.Profiles);
			Profiles.MoveCurrentTo(Model.Profile);

			Accept = Command.Create(this, AcceptInternal);
			SaveProfile = Command.Create(this, SaveProfileInternal);
			DeleteProfile = Command.Create(this, DeleteProfileInternal, DeleteProfileAvailable);
			AddIncludePath = Command.Create(this, AddIncludePathInternal, AddIncludePathAvailable);
			RemoveIncludePath = Command.Create(this, RemoveIncludePathInternal, RemoveIncludePathAvailable);
			MoveIncludePathUp = Command.Create(this, MoveIncludePathUpInternal, MoveIncludePathUpAvailable);
			MoveIncludePathDown = Command.Create(this, MoveIncludePathDownInternal, MoveIncludePathDownAvailable);
			ResetToDefaults = Command.Create(this, ResetToDefaultsInternal, ResetToDefaultsAvailable);

			DataContext = this;
		}

		/// <summary>The app model</summary>
		public Model Model
		{
			get => m_model;
			set
			{
				if (Model == value) return;
				if (m_model != null)
				{
					m_model.PropertyChanged -= HandlePropertyChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.PropertyChanged += HandlePropertyChanged;
				}

				// Handlers
				void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
						case nameof(Model.Profile):
						{
							Profiles.MoveCurrentTo(Model.Profile);
							break;
						}
						case nameof(SettingsData.Profiles):
						{
							Profiles.Refresh();
							DeleteProfile.NotifyCanExecuteChanged();
							break;
						}
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary>The settings data</summary>
		public SettingsData Settings => Model.Settings;

		/// <summary>The currently selected profile</summary>
		public SettingsProfile Profile => Model.Profile;

		/// <summary>Available fonts</summary>
		public ICollectionView AvailableFonts { get; }

		/// <summary>The available profiles</summary>
		public ICollectionView Profiles
		{
			get => m_profiles;
			set
			{
				if (Profiles == value) return;
				if (m_profiles != null)
				{
					m_profiles.CurrentChanged -= HandleCurrentProfileChanged;
				}
				m_profiles = value;
				if (m_profiles != null)
				{
					m_profiles.CurrentChanged += HandleCurrentProfileChanged;
				}
				NotifyPropertyChanged(nameof(Profiles));

				// Handlers
				void HandleCurrentProfileChanged(object? sender, EventArgs e)
				{
					if (Profiles.CurrentItem is SettingsProfile profile)
					{
						Model.Profile = profile;
					}
				}
			}
		}
		private ICollectionView m_profiles = null!;

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}

		/// <summary>Save the current configuration as a profile</summary>
		public Command SaveProfile { get; }
		private void SaveProfileInternal()
		{
			var dlg = new PromptUI(this)
			{
				Title = "Choose a Name of this Profile",
				Prompt = "Profile name:",
				Value = SettingsProfile.DefaultProfileName,
				ShowWrapCheckbox = false,
				MultiLine = false,
				ReadOnly = false,
				MinWidth = 400,
			};
			if (dlg.ShowDialog() != true)
				return;

			var profile = Profile != null
				? new SettingsProfile(Profile) { Name = dlg.Value }
				: new SettingsProfile() { Name = dlg.Value };

			// Add or replace
			Settings.Profiles.RemoveAll(x => x.Name == dlg.Value);
			Settings.Profiles.Add(profile);
			Settings.NotifySettingChanged(nameof(Settings.Profiles));

			// Select the newly created profile
			Profiles.MoveCurrentTo(profile);
		}

		/// <summary>Delete the current profile</summary>
		public Command DeleteProfile { get; }
		private void DeleteProfileInternal()
		{
			if (!DeleteProfileAvailable() || Profile is null)
				return;

			// Remove any profile matching the name
			var doomed = Profile.Name;
			Settings.Profiles.RemoveAll(x => x.Name == doomed);
			Settings.NotifySettingChanged(nameof(Settings.Profiles));

			// Ensure at least one profile
			if (Settings.Profiles.Count == 0)
				Settings.Profiles.Add(new SettingsProfile { Name = SettingsProfile.DefaultProfileName });

			// Make sure the active profile is not the deleted one
			if (Model.Profile.Name == doomed)
				Model.Profile = Settings.Profiles.First();

			// Select the currently active profile
			Profiles.MoveCurrentTo(Model.Profile);
		}
		private bool DeleteProfileAvailable()
		{
			return Profile is not null && Model.Settings.Profiles.Count > 1;
		}

		/// <summary>Add a path to the include paths collection</summary>
		public Command AddIncludePath { get; }
		private void AddIncludePathInternal()
		{
			if (Profile is null)
				return;

			var dlg = new OpenFolderUI { Title = "Add an Include Path" };
			if (dlg.ShowDialog(this) == true)
				Profile.IncludePaths = Profile.IncludePaths.Append(dlg.SelectedPath).ToArray();
		}
		private bool AddIncludePathAvailable()
		{
			return Profile is not null;
		}

		/// <summary>Add a path to the include paths collection</summary>
		public Command RemoveIncludePath { get; }
		private void RemoveIncludePathInternal(object? parameter)
		{
			if (Profile is null)
				return;

			if (parameter is string path)
				Profile.IncludePaths = Profile.IncludePaths.Where(x => x != path).ToArray();
		}
		private bool RemoveIncludePathAvailable(object? parameter)
		{
			return Profile is not null;
		}

		/// <summary>Reorder the include paths</summary>
		public Command MoveIncludePathUp { get; }
		private void MoveIncludePathUpInternal(object? parameter)
		{
			if (!MoveIncludePathUpAvailable(parameter) || Profile is null)
				return;

			var index = (int?)parameter ?? throw new Exception("Parameter is not an integer");
			Util.Swap(ref Profile.IncludePaths[index], ref Profile.IncludePaths[index - 1]);
			Profile.IncludePaths = Profile.IncludePaths.ToArray();
		}
		private bool MoveIncludePathUpAvailable(object? parameter)
		{
			return Profile is not null && parameter is int index && index >= 1 && index < Profile.IncludePaths.Length;
		}

		/// <summary>Reorder the include paths</summary>
		public Command MoveIncludePathDown { get; }
		private void MoveIncludePathDownInternal(object? parameter)
		{
			if (!MoveIncludePathDownAvailable(parameter) || Profile is null)
				return;

			var index = (int?)parameter ?? throw new Exception("Parameter is not an integer");
			Util.Swap(ref Profile.IncludePaths[index], ref Profile.IncludePaths[index + 1]);
			Profile.IncludePaths = Profile.IncludePaths.ToArray();
		}
		private bool MoveIncludePathDownAvailable(object? parameter)
		{
			return Profile is not null && parameter is int index && index >= 0 && index < Profile.IncludePaths.Length - 1;
		}

		/// <summary>Reset the settings to defaults</summary>
		public Command ResetToDefaults { get; }
		private void ResetToDefaultsInternal()
		{
			if (Profile is null)
				return;

			// Reset the profile to the default settings. Preserve the name though
			var name = Profile.Name;
			Profile.Reset();
			Profile.Name = name;

			Settings.Save();
		}
		private bool ResetToDefaultsAvailable()
		{
			return Profile is not null;
		}

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
