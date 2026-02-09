using System;
using System.Windows;

namespace CoinFlip.UI
{
	public class SkinResourceDictionary : ResourceDictionary
	{
		/// <summary>Default skin</summary>
		public Uri SourceDefault
		{
			get;
			set
			{
				field = value;
				UpdateSource();
			}
		} = null!;

		/// <summary>Dark skin</summary>
		public Uri SourceDark
		{
			get;
			set
			{
				field = value;
				UpdateSource();
			}
		} = null!;

		/// <summary>Called to</summary>
		internal void UpdateSource()
		{
			switch (App.Skin)
			{
				case ESkin.Default:
				{
					if (SourceDefault != null)
						Source = SourceDefault;
					break;
				}
				case ESkin.Dark:
				{
					if (SourceDark != null)
						Source = SourceDark;
					break;
				}
				default:
				{
					throw new Exception($"Unknown skin value: {App.Skin}");
				}
			}
		}
	}
}
