//***********************************************
// Singleton
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

namespace Rylogic.Common
{
	public class Singleton<Type> where Type : new()
	{
		class SingletonCreator
		{
			static SingletonCreator() { }
			internal static readonly Type m_instance = new Type();
		}
		
		private Singleton() {}
		public static Type Instance
		{
			get { return SingletonCreator.m_instance; }
		}
	}
}