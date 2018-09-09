namespace RyLogViewer
{
	/// <summary>Interface for controlling time limited features</summary>
	public interface ILicensedFeature
	{
		/// <summary>An html description of the licensed feature</summary>
		string FeatureDescription { get; }

		/// <summary>True if the licensed feature is still currently in use</summary>
		bool FeatureInUse { get; }

		/// <summary>Called to stop the use of the feature</summary>
		void CloseFeature();
	}
}
