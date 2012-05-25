//***************************************************
// Enum Extensions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;

namespace pr.extn
{
	public static class EnumExtensions
	{
		/// <summary>Return the index of this enum value within the enum (for non-sequential enums)
		/// Use: (MyEnum)Enum.GetValues(typeof(MyEnum)).GetValue(i) for the reverse of this method</summary>
		public static int ToIndex(this Enum e)
		{
			int i = 0;
			foreach (var v in Enum.GetValues(e.GetType()))
			{
				if (e.GetHashCode() == v.GetHashCode()) return i;
				++i;
			}
			return -1;
		}
	}
}
