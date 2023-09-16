using Rylogic.Common;

namespace ADUFO;

public class Sliders : SettingsSet<Sliders>
{
	public Sliders()
	{
		Drag = 0.1;
		WorkStream_to_WorkStream_StringConstant = 1.0;
		WorkStream_to_WorkStream_CoulombConstant = 1.0;
		WorkStream_to_Epic_StringConstant = 1.0;
		WorkStream_to_Epic_CoulombConstant = 1.0;
	}

	/// <summary>General drag to slow things down</summary>
	public double Drag
	{
		get => get<double>(nameof(Drag));
		set => set(nameof(Drag), value);
	}

	/// <summary>Work stream to Work stream spring constant</summary>
	public double WorkStream_to_WorkStream_StringConstant
	{
		get => get<double>(nameof(WorkStream_to_WorkStream_StringConstant));
		set => set(nameof(WorkStream_to_WorkStream_StringConstant), value);
	}

	/// <summary>Work stream to Work stream spring constant</summary>
	public double WorkStream_to_WorkStream_CoulombConstant
	{
		get => get<double>(nameof(WorkStream_to_WorkStream_CoulombConstant));
		set => set(nameof(WorkStream_to_WorkStream_CoulombConstant), value);
	}

	/// <summary>Work stream to Epic spring constant</summary>
	public double WorkStream_to_Epic_StringConstant
	{
		get => get<double>(nameof(WorkStream_to_Epic_StringConstant));
		set => set(nameof(WorkStream_to_Epic_StringConstant), value);
	}

	/// <summary>Work stream to Epic spring constant</summary>
	public double WorkStream_to_Epic_CoulombConstant
	{
		get => get<double>(nameof(WorkStream_to_Epic_CoulombConstant));
		set => set(nameof(WorkStream_to_Epic_CoulombConstant), value);
	}
}
