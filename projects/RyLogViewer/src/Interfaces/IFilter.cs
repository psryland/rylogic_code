namespace RyLogViewer
{
	/// <summary>Row filter</summary>
	public interface IFilter
	{
		/// <summary>Defines what a match with this filter means</summary>
		EIfMatch IfMatch { get; }

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		bool IsMatch(string text);
	}

	/// <summary>Filter behaviour</summary>
	public enum EIfMatch
	{
		Keep,
		Reject,
	}
}
