using System.Globalization;

namespace EscapeVelocity
{
	public static class EPerm
	{
		// Ordered such that AA,BB occurs before AB.
		// This is for bond ordering so that 'AB' is prefered over
		// AA or BB when A and B are the same element. Similarly for C,D
		public const int AA = 0;
		public const int BB = 1;
		public const int AB = 2;
		public const int CC	= 3;
		public const int AC	= 4;
		public const int BC	= 5;
		public const int DD	= 6;
		public const int AD	= 7;
		public const int BD	= 8;
		public const int CD	= 9;
		public const int Perm2Count = 3;
		public const int Perm4Count = 10; 
		
		public static string ToString(int perm)
		{
			switch (perm)
			{
			default: return perm.ToString(CultureInfo.InvariantCulture);
			case 0: return "AA";
			case 1: return "BB";
			case 2: return "AB";
			case 3: return "CC";
			case 4: return "AC";
			case 5: return "BC";
			case 6: return "DD";
			case 7: return "AD";
			case 8: return "BD";
			case 9: return "CD";
			}
		}
	}
}