//***********************************************
// Singleton
//  Copyright © Rylogic Ltd 2008
//***********************************************

namespace pr.common
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