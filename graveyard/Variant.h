//********************************************************
//
//	Variant - (c) Paul Ryland Jan 2005
//
//********************************************************
#ifndef VARIANT_H
#define VARIANT_H

namespace Variant
{
	enum Type
	{
		Char = 0,
		Bool,
		Short,
		Int,
		Long,
		Float,
		Double,
		String,
		Pointer,
		NumberOf,
		Unknown
	};

	union Var
	{
		enum { MAX_STRING_LENGTH = 256 };
		char	m_char;
		bool	m_bool;
		short	m_short;
		int		m_int;
		long	m_long;
		float	m_float;
		double	m_double;
		char	m_string[MAX_STRING_LENGTH];
		void*	m_pointer;
	};
}

#endif//VARIANT_H
