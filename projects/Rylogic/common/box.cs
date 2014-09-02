//***************************************************
// Box
//  Copyright © Rylogic Ltd 2011
//***************************************************

namespace pr.common
{
	/// <summary>Box a type into a reference type</summary>
	public class Box<T>
	{
		public T obj      { get; set; }
		public Box(T obj) { this.obj = obj; }
		public static implicit operator T(Box<T> box) { return box.obj; }
	}
}
