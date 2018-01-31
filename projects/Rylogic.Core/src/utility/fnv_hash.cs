namespace Rylogic.Utility
{
	public static class FNV1a
	{
		public const int OffsetBasis = unchecked((int)2166136261);
		public const int Prime = 16777619;

		/// <summary>Simple FNV-1a hasher</summary>
		public static int Hash(int value, int hash = OffsetBasis)
		{
			return (hash ^ value) * Prime;
		}

		/// <summary>Simple FNV-1a hasher</summary>
		public static int Hash(byte[] data, int hash = OffsetBasis)
		{
			for (int i = 0; i != data.Length; ++i)
				hash = Hash(data[i], hash);

			return hash;
		}

		/// <summary>Simple FNV-1a hasher</summary>
		public static int Hash(string str, int hash = OffsetBasis)
		{
			for (int i = 0; i != str.Length; ++i)
				hash = Hash(str[i], hash);

			return hash;
		}
	}
}
