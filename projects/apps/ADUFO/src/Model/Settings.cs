using Rylogic.Common;

namespace ADUFO;

public class Settings : SettingsBase<Settings>
{
	public Settings()
	{
		Organization = string.Empty;
		PersonalAccessToken = string.Empty;
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

	/// <summary>The Personal Access Token used to access ADO</summary>
	public string PersonalAccessToken
	{
		get => get<string>(nameof(PersonalAccessToken));
		set => set(nameof(PersonalAccessToken), value);
	}
}
