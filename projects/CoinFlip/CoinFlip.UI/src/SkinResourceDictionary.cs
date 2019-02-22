using System;
using System.Windows;

namespace CoinFlip.UI
{
	public class SkinResourceDictionary : ResourceDictionary
	{
		/// <summary>Default skin</summary>
		public Uri SourceDefault
		{
			get { return m_source_default; }
			set
			{
				m_source_default = value;
				UpdateSource();
			}
		}
		private Uri m_source_default;

		/// <summary>Dark skin</summary>
		public Uri SourceDark
		{
			get { return m_source_dark; }
			set
			{
				m_source_dark = value;
				UpdateSource();
			}
		}
		private Uri m_source_dark;

		/// <summary>Called to</summary>
		internal void UpdateSource()
		{
			switch (App.Skin)
			{
			default: throw new Exception($"Unknown skin value: {App.Skin}");
			case ESkin.Default:
				{
					if (SourceDefault != null)
						base.Source = SourceDefault;
					break;
				}
			case ESkin.Dark:
				{
					if (SourceDark != null)
						base.Source = SourceDark;
					break;
				}
			}
		}
	}
}
