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
		QueryWorkStreams =
			"select [System.Id] from [WorkItems] " +
			"where ([Work Item Type] = 'Work Stream')" +
			"and ([System.State] <> 'Removed' and [System.State] <> 'Closed')";
		Sliders = new Sliders();
		UILayout = null;
		TestQuery = string.Empty;
		TestWorkItemIds = string.Empty;
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
	
	/// <summary>Query for retrieving work streams</summary>
	public string QueryWorkStreams
	{
		get => get<string>(nameof(QueryWorkStreams));
		set => set(nameof(QueryWorkStreams), value);
	}

	/// <summary>Simulation controls</summary>
	public Sliders Sliders
	{
		get => get<Sliders>(nameof(Sliders));
		set => set(nameof(Sliders), value);
	}

	/// <summary>The arrangement of windows</summary>
	public XElement? UILayout
	{
		get => get<XElement?>(nameof(UILayout));
		set => set(nameof(UILayout), value);
	}

	/// <summary>Test query string</summary>
	public string TestQuery
	{
		get => get<string>(nameof(TestQuery));
		set => set(nameof(TestQuery), value);
	}

	/// <summary>Test query string</summary>
	public string TestWorkItemIds
	{
		get => get<string>(nameof(TestWorkItemIds));
		set => set(nameof(TestWorkItemIds), value);
	}
}
