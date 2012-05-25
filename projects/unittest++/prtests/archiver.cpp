//*************************************************************
// Archiver unit test
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/assert.h"
#include "pr/common/archiver.h"
#include "pr/maths/maths.h"

#pragma warning (disable:4351)

SUITE(TestArchiver)
{
	struct Type0
	{
		int       m_int;
		short     m_short;
		char      m_ignored;
		pr::uint8 m_byte;
		float     m_float[3];

		Type0() :m_int(0x12345678) ,m_short(0x1234) ,m_ignored('a') ,m_byte(0x12) ,m_float() {}
		static char const* ArchiveTemplate()
		{
			return 
				"Type0,"
				"s32:m_int:1,"
				"s16:m_short:1,"
				"-s8::1,"
				"u8:m_byte:1,"
				"f32:m_float:3,";
		}
	};
	struct Type1
	{
		double m_float[2];
		short  m_ignored;
		short  pad;
		Type0  m_type0;
		
		Type1() :m_float() ,m_ignored(0x5555) ,pad(0) ,m_type0() {}
		static char const* ArchiveTemplate()
		{
			return
				"Type1,"
				"f64:m_float:2,"
				"-s16::1,"
				"Type0:m_type0:1";
		}
	};
	struct Type2
	{
		int m_int;
		Type2() :m_int(0x55553333) {}
		static char const* ArchiveTemplate()
		{
			return
				"Type2,"
				"s32:m_int:1";
		}
	};

	struct IO
	{
		static bool Invalid(pr::uint8 const* buf)                         { return buf == 0; }
		static void Read (pr::uint8 const*& src ,void* dst ,size_t count) { memcpy(dst, src, count); src += count; }
		static void Write(pr::uint8*& dst ,void const* src ,size_t count) { memcpy(dst, src, count); dst += count; }
	};

	// Reading:
	// Read template definition:
	//	-> something that tells you what each value is and how big it is
	// Given two templates, generate a converter, that reads values from one and writes them into memory for another

	// A converter:
	//	read x bytes, convert to 'Y', write to location z
	//	read x bytes, convert to 'Y', write to location z
	//	...
	TEST(Converter)
	{
		Type0 t0;
		Type1 t1;
		Type2 t2;

		pr::uint8 buf[1024];
		pr::archive::Archive<IO, pr::uint8*> arch;
		arch.RegisterTemplate<Type0>(Type0::ArchiveTemplate());
		arch.RegisterTemplate<Type1>(Type1::ArchiveTemplate());
		arch.Write(buf);
		arch.Write(t0);

		pr::archive::Archive<IO, pr::uint8*> arch2;
		arch2.RegisterTemplate<Type2>(Type2::ArchiveTemplate());
		arch2.RegisterTemplate<Type0>(Type0::ArchiveTemplate());
		arch2.RegisterTemplate<Type1>(Type1::ArchiveTemplate());
		arch2.Write(buf);
		arch2.Write(t0);
		arch2.Write(t2);
		arch2.Write(t1);

		pr::archive::Archive<IO, pr::uint8 const*> arch3;
		arch3.Read(buf);
		arch3.Read(t0);
		arch3.Read(t2);
		arch3.Read(t1);

		Type0 ref0;
		Type1 ref1;
		Type2 ref2;
		CHECK_EQUAL(memcmp(&t0, &ref0, sizeof(t0)), 0);
		CHECK_EQUAL(memcmp(&t1, &ref1, sizeof(t1)), 0);
		CHECK_EQUAL(memcmp(&t2, &ref2, sizeof(t2)), 0);

		//// Implicit type
		//arch.Write(t0);					// looks up template for 't0' using typeid().name()

		//// Explicit type
		//int template_type0 = arch.RegisterTemplate(Type0::ArchiveTemplate());	// string given explicitly
		//arch.Write(template_type0, t0);	// Write 't0' using template 'template_type0'

		
		//// a stream of incoming bytes
		//byte const* src = reinterpret_cast<byte const*>(&t0);

		//Archiver::Converter conv;

		//Archiver::Read(t1, src, conv);

		//CHECK_EQUAL(t1.m_float = t0.m_float);
		//CHECK_EQUAL(t1.m_ch = 'A');
		//CHECK_EQUAL(t1.m_byte = t0.m_byte);
		//CHECK_EQUAL(t1.m_short = t0.m_short);
		//CHECK_EQUAL(t1.m_int = t0.m_int);
		//{
		//	int i = 12345678;
		//	float f;
		//	pr::archiver::Read(Archiver::EType::s32, Archiver::EType::f32, &i, &f);
		//	CHECK_EQUAL(f, 12345678.0f);
		//}

	}
}




	//template <typename type>	struct TypeId;	// type -> id
	//template <int id>			struct IdType;	// id -> type
	//template <>	struct TypeId<char				> { enum { value =  0 }; };		template < 0>	struct IdType { typedef char				type; };
	//template <>	struct TypeId<unsigned char		> { enum { value =  1 }; };		template < 1>	struct IdType { typedef unsigned char		type; };
	//template <>	struct TypeId<short				> { enum { value =  2 }; };		template < 2>	struct IdType { typedef short				type; };
	//template <>	struct TypeId<unsigned short	> { enum { value =  3 }; };		template < 3>	struct IdType { typedef unsigned short		type; };
	//template <>	struct TypeId<int				> { enum { value =  4 }; };		template < 4>	struct IdType { typedef int					type; };
	//template <>	struct TypeId<unsigned int		> { enum { value =  5 }; };		template < 5>	struct IdType { typedef unsigned int		type; };
	//template <>	struct TypeId<long				> { enum { value =  6 }; };		template < 6>	struct IdType { typedef long				type; };
	//template <>	struct TypeId<unsigned long		> { enum { value =  7 }; };		template < 7>	struct IdType { typedef unsigned long		type; };
	//template <>	struct TypeId<__int64			> { enum { value =  8 }; };		template < 8>	struct IdType { typedef __int64				type; };
	//template <>	struct TypeId<unsigned __int64	> { enum { value =  9 }; };		template < 9>	struct IdType { typedef unsigned __int64	type; };
	//template <>	struct TypeId<float				> { enum { value = 10 }; };		template <10>	struct IdType { typedef float				type; };
	//template <>	struct TypeId<double			> { enum { value = 11 }; };		template <11>	struct IdType { typedef double				type; };
	//template <>	struct TypeId<long double		> { enum { value = 12 }; };		template <12>	struct IdType { typedef long double			type; };

	//union Variant
	//{
	//	char				s8;
	//	unsigned char		u8;
	//	short				s16;
	//	unsigned short		u16;
	//	int					s32;
	//	unsigned int		u32;
	//	__int64				s64;
	//	unsigned __int64	u64;
	//	float				f;
	//	double				d;
	//	long double			ld;
	//};



