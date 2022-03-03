namespace RyLogViewer
{
	/// <summary>Interface for a single column within a row of log data</summary>
	public interface ILogDataElement
	{
		/// <summary>The text of the data element</summary>
		string Text { get; }
	}
}
