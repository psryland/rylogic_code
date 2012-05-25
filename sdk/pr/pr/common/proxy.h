//******************************************
// Imposter
//  Copyright © Rylogic Ltd 2010
//******************************************

namespace pr
{
	// Makes a heap allocated type look like a value type
	// Useful for storing objects with large alignments in std containers
	// e.g.
	//   __declspec(align(16)) struct V { float x,y,z,w; };
	//  std::list< pr::Proxy<V> >
	template <typename Type> struct Proxy
	{
		Type* m_ptr;
		Proxy() :m_ptr(new Type)               {}
		Proxy(Proxy const& rhs)                { m_ptr = new Type(*rhs.m_ptr); }
		Proxy(Type const& rhs)                 { m_ptr = new Type(rhs); }
		~Proxy()                               { delete m_ptr; }
		Proxy& operator = (Proxy const& rhs)   { if (&rhs != this)  { Type* ptr = new Type(*rhs.m_ptr); delete m_ptr; m_ptr = ptr; } return *this; }
		Proxy& operator = (Type const& rhs)    { if (&rhs != m_ptr) { Type* ptr = new Type(rhs);        delete m_ptr; m_ptr = ptr; } return *this; }
		Type const& get() const                { return *m_ptr; }
		Type&       get()                      { return *m_ptr; }
		operator Type const&() const           { return *m_ptr; }
		operator Type& ()                      { return *m_ptr; }
	};
}