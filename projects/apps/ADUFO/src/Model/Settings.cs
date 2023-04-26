using System.Xml.Linq;
using Rylogic.Common;

namespace ADUFO;

public class Settings : SettingsBase<Settings>
{
	public Settings()
	{
		Organization = string.Empty;
		Project = string.Empty;
		PersonalAccessToken = string.Empty;
		UILayout = null;
		AutoSaveOnChanges = true;
	}
	public Settings(string filepath, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
		: base(filepath, flags)
	{
		AutoSaveOnChanges = true;
	}

	/// <summary>The ADO organization</summary>
	public string Organization
	{
		get => get<string>(nameof(Organization));
		set => set(nameof(Organization), value);
	}

	/// <summary>The ADO project</summary>
	public string Project
	{
		get => get<string>(nameof(Project));
		set => set(nameof(Project), value);
	}

	/// <summary>The Personal Access Token used to access ADO</summary>
	public string PersonalAccessToken
	{
		get => get<string>(nameof(PersonalAccessToken));
		set => set(nameof(PersonalAccessToken), value);
	}

	/// <summary>The arrangement of windows</summary>
	public XElement? UILayout
	{
		get => get<XElement?>(nameof(UILayout));
		set => set(nameof(UILayout), value);
	}
}
