namespace Rylogic.Utility
{
	public static class Boxed
	{
		// Notes:
		//  - Use these to prevent unnecessary boxing of constants

		/// <summary>Boxed True/False</summary>
		public static readonly object True = true;
		public static readonly object False = false;
		public static object Box(bool value) => value ? True : False;

		/// <summary>Boxed int constants</summary>
		public static readonly object IntZero = 0;
		public static readonly object IntOne = 1;

		/// <summary>Boxed double constants</summary>
		public static readonly object DblZero = 0.0;
		public static readonly object DblOne = 1.0;
	}
}
