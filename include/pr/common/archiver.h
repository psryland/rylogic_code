//**************************************************************************
// Archiver
//  Copyright (c) Rylogic Ltd 2009
//**************************************************************************
#ifndef PR_ARCHIVER_H
#define PR_ARCHIVER_H
#define NEW_ARCHIVER
#ifdef NEW_ARCHIVER

// Archive File Format:
//	4bytes - 'P', 'R', 'A', 'R'
//	4bytes - Number of templates
//	[template]
//	[template]
//	[...]
//	[template_instance]
//	[template_instance]
//	[template_instance]
//	[...]
// A template is a tuple_count, and list of <type:identifer:count> tuples
// 'type' is a builtin type or an earlier defined template
// e.g. A template for:
//	struct MyType
//	{
//		static char const* ArchiveTemplate() { return "int:m_int:1,-char::1,float:m_float:4,"; }
//		int m_int;
//		char m_ignored;
//		float m_float[4];
//	};
// is:
//	count = 2							- 4bytes
//	id_of("int"):hash("m_int"):1		- 8bytes
//	id_of("float"):hash("m_float"):4	- 8bytes
//		2b        :   2b          :4b
//
// Usage:
//	pr::archive::File arch("filename");
//	arch.RegisterTemplate(MyType::ArchiveTemplate());
//	arch.Write(my_type);
//
#include <vector>
#include <algorithm>
#include <typeinfo>
#include "pr/common/fmt.h"
#include "pr/common/hash.h"

#ifndef PR_ASSERT
#	define PR_ASSERT_STR_DEFINED
#	define PR_ASSERT(grp, exp, str)
#endif
#ifndef PR_EXPAND
#	define PR_EXPAND_DEFINED
#	define PR_EXPAND(grp, exp)
#endif
#define PR_ARCHIVE_DBG			1
#define PR_ARCHIVE_SHOW_TYPES	0

namespace pr
{
	namespace archive
	{
		// Built-in types
		namespace EType
		{
			enum Type
			{
				s8		= 0x0c5d8c41,
				s16		= 0x0800461d,
				s32		= 0x0537f586,
				s64		= 0x07e9c746,
				u8		= 0x1bf3bb96,
				u16		= 0x18832d9b,
				u32		= 0x15b49e00,
				u64		= 0x176aacc0,
				f32		= 0x1a8da2d5,
				f64		= 0x18539015,
				f128	= 0x1c5971be,
			};
		}

		typedef char				S_008;
		typedef short				S_016;
		typedef int					S_032;
		typedef __int64				S_064;
		typedef unsigned char		U_008;
		typedef unsigned short		U_016;
		typedef unsigned int		U_032;
		typedef unsigned __int64	U_064;
		typedef float				F_032;
		typedef double				F_064;
		typedef long double			F_128;

		// A single field in a template type
		struct Field
		{
			U_032 m_type;	// The hash of the type name, e.g. s8, f32, s32, or an earlier defined template id
			U_032 m_name;	// The hash of the member name for this field
			U_032 m_count;	// The number of fields of this type in an array
			U_032 m_offset;	// The byte offset to this member in the source type
		};
		typedef std::vector<Field> Fields;

		// A single template
		struct Template
		{
			U_032	m_type_info;	// The hash of the typeinfo name of this type
			U_032	m_type_name;	// The hash of the name of this type
			U_032	m_size;			// The size in bytes of written instances of this template
			Fields	m_fields;		// The fields to be written for this template
		};
		typedef std::vector<Template> Templates;
		inline bool match_typeinfo	(Template const& lhs, U_032 type_info)		{ return lhs.m_type_info == type_info; }
		inline bool match_typename	(Template const& lhs, U_032 type_name)		{ return lhs.m_type_name == type_name; }
		inline bool operator ==		(Template const& lhs, Template const& rhs)	{ return lhs.m_type_info == rhs.m_type_info; }

		// The data source that we're archiving to/from.
		// 'IO' is a type providing read/write operations on 'Handle'
		template <typename IO, typename Handle>
		class Archive
		{
			#define PR_ARCHIVE_4CC "PRAR"

			Templates	m_templates;
			Handle		m_data;

			// Return the 'on-disk' size of data for a template
			U_032 Sizeof(U_032 type_name) const
			{
				switch (type_name)
				{
				case EType::s8:		case EType::u8:		return 1;
				case EType::s16:	case EType::u16:	return 2;
				case EType::s32:	case EType::u32:	case EType::f32:	return 4;
				case EType::s64:	case EType::u64:	case EType::f64:	return 8;
				case EType::f128:	return 16;
				default:			return GetTemplateByName(type_name).m_size;
				}
			}

			// Return true if 'type_name' is a built in type
			bool IsBuiltinType(U_032 type_name) const
			{
				return	type_name==EType::s8 ||
						type_name==EType::u8 ||
						type_name==EType::s16 ||
						type_name==EType::u16 ||
						type_name==EType::s32 ||
						type_name==EType::u32 ||
						type_name==EType::f32 ||
						type_name==EType::s64 ||
						type_name==EType::u64 ||
						type_name==EType::f64 ||
						type_name==EType::f128;
			}

			// Return the template for 'type_name' or null if not found
			Template const* FindTemplateByName(U_032 type_name) const
			{
				for (Templates::const_iterator i = m_templates.begin(), iend = m_templates.end(); i != iend; ++i)
					if (i->m_type_name == type_name) return &*i;
				return 0;
			}

			// Return true if 'type_name' is a registered template id
			bool IsTemplateName(U_032 type_name) const
			{
				return FindTemplateByName(type_name) != 0;
			}

			// Return the template corresponding to 'type_name'
			Template const& GetTemplateByName(U_032 type_name) const
			{
				Template const* tmp = FindTemplateByName(type_name);
				PR_ASSERT(PR_ARCHIVE_DBG, tmp != 0, "Template not found");
				return *tmp;
			}

			// Return the template for 'type_info' or null if not found
			Template const* FindTemplateByTypeInfo(U_032 type_info) const
			{
				for (Templates::const_iterator i = m_templates.begin(), iend = m_templates.end(); i != iend; ++i)
					if (i->m_type_info == type_info) return &*i;
				return 0;
			}

			// Return true if 'type_info' is a registered template
			bool IsTemplateTypeInfo(U_032 type_info) const
			{
				return FindTemplateByTypeInfo(type_info) != 0;
			}

			// Return the template corresponding to 'type_info'
			Template const& GetTemplateByTypeInfo(U_032 type_info) const
			{
				Template const* tmp = FindTemplateByTypeInfo(type_info);
				PR_ASSERT(PR_ARCHIVE_DBG, tmp != 0, "Template not found");
				return *tmp;
			}

			// Read bytes from 'src' using 'tmp' and write them to 'm_data'
			void Write(Template const& tmp, char const* src)
			{
				for (Fields::const_iterator i = tmp.m_fields.begin(), iend = tmp.m_fields.end(); i != iend; ++i)
				{
					Field const& field = *i;
					if (IsBuiltinType(field.m_type))
						IO::Write(m_data, src + field.m_offset, Sizeof(field.m_type) * field.m_count);
					else
						Write(GetTemplateByName(field.m_type), src + field.m_offset);
				}
			}

			// Read bytes from 'm_data' using 'tmp' and write them to 'dst'
			void Read(Template const& tmp, char* dst)
			{
				for (Fields::const_iterator i = tmp.m_fields.begin(), iend = tmp.m_fields.end(); i != iend; ++i)
				{
					Field const& field = *i;
					if (IsBuiltinType(field.m_type))
						IO::Read(m_data, dst + field.m_offset, Sizeof(field.m_type) * field.m_count);
					else
						Read(GetTemplateByName(field.m_type), dst + field.m_offset);
				}
			}

		public:

			Archive()
			:m_templates()
			,m_data()
			{
				using namespace pr::hash;
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("s8"  ) == EType::s8   ,FmtS("Hash of s8   is incorrect. Should be 0x%08x\n", HashC("s8"  )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("s16" ) == EType::s16  ,FmtS("Hash of s16  is incorrect. Should be 0x%08x\n", HashC("s16" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("s32" ) == EType::s32  ,FmtS("Hash of s32  is incorrect. Should be 0x%08x\n", HashC("s32" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("s64" ) == EType::s64  ,FmtS("Hash of s64  is incorrect. Should be 0x%08x\n", HashC("s64" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("u8"  ) == EType::u8   ,FmtS("Hash of u8   is incorrect. Should be 0x%08x\n", HashC("u8"  )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("u16" ) == EType::u16  ,FmtS("Hash of u16  is incorrect. Should be 0x%08x\n", HashC("u16" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("u32" ) == EType::u32  ,FmtS("Hash of u32  is incorrect. Should be 0x%08x\n", HashC("u32" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("u64" ) == EType::u64  ,FmtS("Hash of u64  is incorrect. Should be 0x%08x\n", HashC("u64" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("f32" ) == EType::f32  ,FmtS("Hash of f32  is incorrect. Should be 0x%08x\n", HashC("f32" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("f64" ) == EType::f64  ,FmtS("Hash of f64  is incorrect. Should be 0x%08x\n", HashC("f64" )));
				PR_ASSERT(PR_ARCHIVE_DBG, HashC("f128") == EType::f128 ,FmtS("Hash of f128 is incorrect. Should be 0x%08x\n", HashC("f128")));
			}

			// Register a template description for 'Type'.
			// Note templates should be registered before assigned the data
			// source to write to. This is because Write(Handle) writes the header
			// and templates immediately.
			// Format:
			//	template_tag,type:name:count,type:name:count,...,type:name:count,\0
			template <typename Type> void RegisterTemplate(char const* template_desc)
			{
				PR_ASSERT(PR_ARCHIVE_DBG, IO::Invalid(m_data), "Register templates before assigning the data source");

				// Add the template
				Template tmp;
				tmp.m_type_info = pr::hash::HashC(typeid(Type).name());
				tmp.m_type_name = pr::hash::Hash(template_desc, ','); ++template_desc;
				PR_ASSERT(PR_ARCHIVE_DBG, !IsBuiltinType(tmp.m_type_name), "Do not register template descriptions for built-in types");
				PR_ASSERT(PR_ARCHIVE_DBG, !IsTemplateTypeInfo(tmp.m_type_info), "Template already defined for this type");
				PR_ASSERT(PR_ARCHIVE_DBG, !IsTemplateName(tmp.m_type_name), "Template for type with this name already defined");
				PR_EXPAND(PR_ARCHIVE_SHOW_TYPES, printf("Type: '%s' -> TypeInfo: 0x%08x  TypeName: 0x%08x\n", typeid(Type).name(), tmp.m_type_info, tmp.m_type_name));

				U_032 offset = 0, size = 0;
				while (*template_desc != 0)
				{
					// A minus sign indicates the type should be skipped
					bool add_field = *template_desc != '-';
					template_desc += int(!add_field);

					// Add a field to the template
					Field field;
					field.m_type	= pr::hash::Hash(template_desc, ':');	++template_desc;
					field.m_name	= pr::hash::Hash(template_desc, ':');	++template_desc;
					field.m_count	= strtoul(template_desc, (char**)&template_desc, 10);		template_desc += int(*template_desc == ',');
					field.m_offset	= offset;

					// Check that 'type' is a built-in type or a previously defined template
					PR_ASSERT(PR_ARCHIVE_DBG, IsBuiltinType(field.m_type) || IsTemplateName(field.m_type), "Field type not defined");

					U_032 sz = Sizeof(field.m_type) * field.m_count;
					if (add_field)	{ size += sz; tmp.m_fields.push_back(field); }
					offset += sz;
				}
				tmp.m_size = size;
				m_templates.push_back(tmp);
			}

			// Assign the source to write the archive to
			void Write(Handle data)
			{
				m_data = data;

				// Write the file identifier 4CC
				IO::Write(m_data, PR_ARCHIVE_4CC, 4);

				// Write the number of templates
				U_032 tmp_count = U_032(m_templates.size());
				IO::Write(m_data, &tmp_count, 4);

				// Write each template
				for (Templates::const_iterator i = m_templates.begin(), iend = m_templates.end(); i != iend; ++i)
				{
					Template const& tmp = *i;
					U_032 field_count = U_032(tmp.m_fields.size());
					IO::Write(m_data, &tmp.m_type_info, sizeof(tmp.m_type_info));
					IO::Write(m_data, &tmp.m_type_name, sizeof(tmp.m_type_name));
					IO::Write(m_data, &tmp.m_size, sizeof(tmp.m_size));
					IO::Write(m_data, &field_count, sizeof(field_count));
					if (!tmp.m_fields.empty())
						IO::Write(m_data, &tmp.m_fields[0], sizeof(Field) * tmp.m_fields.size());
				}
			}

			// Assign the source to read the archive from
			void Read(Handle data)
			{
				m_data = data;

				// Read the file identifier 4CC
				char file_4cc[4];
				IO::Read(m_data, &file_4cc[0], 4);
				PR_ASSERT(PR_ARCHIVE_DBG,	file_4cc[0] == PR_ARCHIVE_4CC[0] &&
												file_4cc[1] == PR_ARCHIVE_4CC[1] &&
												file_4cc[2] == PR_ARCHIVE_4CC[2] &&
												file_4cc[3] == PR_ARCHIVE_4CC[3], "Not an archive file");

				// Read the number of templates
				U_032 tmp_count;
				IO::Read(m_data, &tmp_count, 4);

				// Read each template definition
				for (U_032 i = 0; i != tmp_count; ++i)
				{
					Template tmp;
					U_032 field_count;
					IO::Read(m_data, &tmp.m_type_info, sizeof(tmp.m_type_info));
					IO::Read(m_data, &tmp.m_type_name, sizeof(tmp.m_type_name));
					IO::Read(m_data, &tmp.m_size, sizeof(tmp.m_size));
					IO::Read(m_data, &field_count, sizeof(field_count));
					tmp.m_fields.resize(field_count);
					if (!tmp.m_fields.empty())
						IO::Read(m_data, &tmp.m_fields[0], sizeof(Field) * tmp.m_fields.size());
					m_templates.push_back(tmp);
				}
			}

			// Write a type for which a template has been registered
			template <typename Type> void Write(Type const& type)
			{
				U_032 type_info = pr::hash::HashC(typeid(Type).name());
				Write(GetTemplateByTypeInfo(type_info), reinterpret_cast<char const*>(&type));
			}

			// Read a type from 'm_data'
			template <typename Type> void Read(Type& type)
			{
				U_032 type_info = pr::hash::HashC(typeid(Type).name());
				Read(GetTemplateByTypeInfo(type_info), reinterpret_cast<char*>(&type));
			}

			#undef PR_ARCHIVE_4CC
		};

		//// Generic conversion function pointer
		//typedef void (*ConvFunc)(void const* src, void* dst);

		//// Read a value of type 'SrcType' from 'src', cast it to 'DstType' and write it to 'dst'
		//template <typename SrcType, typename DstType> inline void Read(void const* src, void* dst)
		//{
		//	*static_cast<DstType*>(dst) = static_cast<DstType>(*static_cast<SrcType const*>(src));
		//}

		//struct Converter
		//{
		//	impl::Fields m_fields;

		//	// Generate a mapping from src to dst
		//	Converter(char const* src_archive_desc, char const* dst_archive_desc)
		//	{
		//	}
		//};

		//// Populate 'dst' from 'src' using 'conv' to interpret the data in 'src'
		//template <typename Src, typename Dst> void Read(Dst& dst, byte const* src, Converter const& conv)
		//{
		//	// Each field in the converter writes to a field in 'dst'.
		//	for (Converter::Fields::const_iterator i = conv.m_fields.begin(), iend = conv.m_fields.end(); i != iend; ++i)
		//	{
		//		Converter::Field const& field = *i;
		//		src = field.Read(src, dst);
		//	}
		//}

		//// Write a type into the archive
		//template <typename Type> void Write(Type const& value)
		//{
		//	// Locate the template for this type
		//	int id = Hash(typeid(Type).name());
		//	Templates::const_iterator iter = m_templates.find(id);
		//	PR_ASSERT(1, iter != m_templates.end(), "Type not registered");
		//
		//	Fields& fields = iter->second;

		//}

		// Read a type from the archive

		//// Read from 'src' and write to 'dst' using runtime types 'src_type' and 'dst_type'
		//inline void Read(int src_type, int dst_type, void const* src, void* dst)
		//{
		//	static ConvFunc conv[EType::NumberOf][EType::NumberOf] =
		//	{
		//		{&Read<S_008,S_008> ,&Read<S_008,S_016> ,&Read<S_008,S_032> ,&Read<S_008,S_064> ,&Read<S_008,U_008> ,&Read<S_008,U_016> ,&Read<S_008,U_032> ,&Read<S_008,U_064> ,&Read<S_008,F_032> ,&Read<S_008,F_064> ,&Read<S_008,F_128> },
		//		{&Read<S_016,S_008> ,&Read<S_016,S_016> ,&Read<S_016,S_032> ,&Read<S_016,S_064> ,&Read<S_016,U_008> ,&Read<S_016,U_016> ,&Read<S_016,U_032> ,&Read<S_016,U_064> ,&Read<S_016,F_032> ,&Read<S_016,F_064> ,&Read<S_016,F_128> },
		//		{&Read<S_032,S_008> ,&Read<S_032,S_016> ,&Read<S_032,S_032> ,&Read<S_032,S_064> ,&Read<S_032,U_008> ,&Read<S_032,U_016> ,&Read<S_032,U_032> ,&Read<S_032,U_064> ,&Read<S_032,F_032> ,&Read<S_032,F_064> ,&Read<S_032,F_128> },
		//		{&Read<S_064,S_008> ,&Read<S_064,S_016> ,&Read<S_064,S_032> ,&Read<S_064,S_064> ,&Read<S_064,U_008> ,&Read<S_064,U_016> ,&Read<S_064,U_032> ,&Read<S_064,U_064> ,&Read<S_064,F_032> ,&Read<S_064,F_064> ,&Read<S_064,F_128> },
		//		{&Read<U_008,S_008> ,&Read<U_008,S_016> ,&Read<U_008,S_032> ,&Read<U_008,S_064> ,&Read<U_008,U_008> ,&Read<U_008,U_016> ,&Read<U_008,U_032> ,&Read<U_008,U_064> ,&Read<U_008,F_032> ,&Read<U_008,F_064> ,&Read<U_008,F_128> },
		//		{&Read<U_016,S_008> ,&Read<U_016,S_016> ,&Read<U_016,S_032> ,&Read<U_016,S_064> ,&Read<U_016,U_008> ,&Read<U_016,U_016> ,&Read<U_016,U_032> ,&Read<U_016,U_064> ,&Read<U_016,F_032> ,&Read<U_016,F_064> ,&Read<U_016,F_128> },
		//		{&Read<U_032,S_008> ,&Read<U_032,S_016> ,&Read<U_032,S_032> ,&Read<U_032,S_064> ,&Read<U_032,U_008> ,&Read<U_032,U_016> ,&Read<U_032,U_032> ,&Read<U_032,U_064> ,&Read<U_032,F_032> ,&Read<U_032,F_064> ,&Read<U_032,F_128> },
		//		{&Read<U_064,S_008> ,&Read<U_064,S_016> ,&Read<U_064,S_032> ,&Read<U_064,S_064> ,&Read<U_064,U_008> ,&Read<U_064,U_016> ,&Read<U_064,U_032> ,&Read<U_064,U_064> ,&Read<U_064,F_032> ,&Read<U_064,F_064> ,&Read<U_064,F_128> },
		//		{&Read<F_032,S_008> ,&Read<F_032,S_016> ,&Read<F_032,S_032> ,&Read<F_032,S_064> ,&Read<F_032,U_008> ,&Read<F_032,U_016> ,&Read<F_032,U_032> ,&Read<F_032,U_064> ,&Read<F_032,F_032> ,&Read<F_032,F_064> ,&Read<F_032,F_128> },
		//		{&Read<F_064,S_008> ,&Read<F_064,S_016> ,&Read<F_064,S_032> ,&Read<F_064,S_064> ,&Read<F_064,U_008> ,&Read<F_064,U_016> ,&Read<F_064,U_032> ,&Read<F_064,U_064> ,&Read<F_064,F_032> ,&Read<F_064,F_064> ,&Read<F_064,F_128> },
		//		{&Read<F_128,S_008> ,&Read<F_128,S_016> ,&Read<F_128,S_032> ,&Read<F_128,S_064> ,&Read<F_128,U_008> ,&Read<F_128,U_016> ,&Read<F_128,U_032> ,&Read<F_128,U_064> ,&Read<F_128,F_032> ,&Read<F_128,F_064> ,&Read<F_128,F_128> },
		//	};
		//	conv[src_type][dst_type](src, dst);
		//}
	}
}

#ifdef PR_ASSERT_STR_DEFINED
#	undef PR_ASSERT_STR_DEFINED
#	undef PR_ASSERT
#endif
#ifdef PR_EXPAND_DEFINED
#	undef PR_EXPAND
#endif

#else
// If there is not an overload for your type here add one or more of the following
// functions to your code. Don't add them here.
// namespace Archiver
// {
//		inline void Serialise		(const T&	t,						ByteCont& data)	{ implementation }
//		inline void SerialiseArray	(const T*	pt,	std::size_t count,	ByteCont& data)	{ implementation }
//		inline void DeSerialise		(T&			t,						const void*& data)	{ implementation }
//		inline void DeSerialiseArray(T*			pt,	std::size_t count,	const void*& data)	{ implementation }
// } // Archiver

#include "pr/common/StdVector.h"
#include "pr/common/StdList.h"
#include "pr/common/StdMap.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace archiver
	{
		// Serialise functions ***************************************************************************
		namespace impl
		{
			template <typename PodType> inline void SerialisePod(const PodType& pod, ByteCont& data)
			{
				std::size_t len = data.length();
				data.resize(len + sizeof(PodType));
				CopyMemory(&data[len], &pod, sizeof(PodType));
			}
			template <typename PodArrayType> inline void SerialisePodArray(const PodArrayType* pod_array, std::size_t count, ByteCont& data)
			{
				std::size_t len = data.length();
				data.resize(len + count * sizeof(PodType));
				CopyMemory(&data[len], pod_array, count * sizeof(PodType));
			}
		}// namespace impl

		// Do not create a templated Serialise or SerialiseArray function. It's important that
		// people using these functions think about how to serialise their objects. They should
		// write the appropriate Serialise or SerialiseArray function in the code where they need it.
		inline void Serialise		(const char&		c,					ByteCont& data) { impl::SerialisePod<char>			(c, data); }
		inline void Serialise		(const bool&		b,					ByteCont& data) { impl::SerialisePod<bool>			(b, data); }
		inline void Serialise		(const uint&		i,					ByteCont& data) { impl::SerialisePod<uint>			(i, data); }
		inline void Serialise		(const std::size_t&	s,					ByteCont& data) { impl::SerialisePod<std::size_t>	(s, data); }
		inline void Serialise		(const float&		f,					ByteCont& data) { impl::SerialisePod<float>			(f, data); }
		inline void Serialise		(const v4&			v,					ByteCont& data) { impl::SerialisePod<v4>				(v, data); }
		inline void Serialise		(const m4x4&		m,					ByteCont& data) { impl::SerialisePod<m4x4>			(m, data); }
		inline void Serialise		(const BBox& bbox,				ByteCont& data) { impl::SerialisePod<BBox>	(bbox, data); }
		inline void SerialiseArray	(const char*		pc,	  std::size_t count, ByteCont& data) { impl::SerialisePodArray<char>			(pc,	count, data); }
		inline void SerialiseArray	(const uint8*		pui8, std::size_t count, ByteCont& data) { impl::SerialisePodArray<uint8>		(pui8,	count, data); }
		inline void SerialiseArray	(const uint*		pui,  std::size_t count, ByteCont& data) { impl::SerialisePodArray<uint>			(pui,	count, data); }
		inline void SerialiseArray	(const float*		pf,	  std::size_t count, ByteCont& data) { impl::SerialisePodArray<float>		(pf,	count, data); }
		inline void SerialiseArray	(const v4*			pv,	  std::size_t count, ByteCont& data) { impl::SerialisePodArray<v4>			(pv,	count, data); }
		inline void SerialiseArray	(const m4x4*		pm,	  std::size_t count, ByteCont& data) { impl::SerialisePodArray<m4x4>			(pm,	count, data); }
		inline void SerialiseArray	(const BBox* pbbox,std::size_t count, ByteCont& data) { impl::SerialisePodArray<BBox>	(pbbox, count, data); }
		// Stl serialise functions. If you get a compile error in these
		// functions you probably need to implement your own Serialise
		// and SerialiseArray for the types in your container
		inline void Serialise(const std::string& str, ByteCont& data)
		{
			Serialise		(str.length(), data);
			SerialiseArray	(str.c_str(), str.length(), data);
		}
		inline void SerialiseArray(const std::string* str, std::size_t count, ByteCont& data)
		{
			for( const std::string* str_end = str + count; str != str_end; ++str ) { Serialise(*str, data); }
		}
		template <typename T> inline void Serialise(const std::vector<T>& vec, ByteCont& data)
		{
			Serialise		(vec.size(), data);
			SerialiseArray	(&vec[0], vec.size(), data);
		}
		template <typename T> inline void SerialiseArray(const std::vector<T>* vec_array, std::size_t count, ByteCont& data)
		{
			for( const std::vector<T>* vec_array_end = vec_array + count; vec_array != vec_array_end; ++vec_array ) { Serialise(*vec_array, data); }
		}
		template <typename T> inline void Serialise(const std::list<T>& list, ByteCont& data)
		{
			Serialise		(list.size(), data);
			std::list<T>::const_iterator iter = list.begin(), iter_end = list.end();
			while( iter != iter_end )
			{
				Serialise	(*iter,	data);
				++iter;
			}
		}
		template <typename T> inline void SerialiseArray(const std::list<T>* list_array, std::size_t count, ByteCont& data)
		{
			for( const std::list<T>* list_array_end = list_array + count; list_array != list_array_end; ++list_array ) { Serialise(*list_array, data); }
		}
		template <typename T1, typename T2> inline void Serialise(const std::map<T1, T2>& map, ByteCont& data)
		{
			Serialise		(map.size(), data);
			std::map<T1, T2>::const_iterator iter = map.begin(), iter_end = map.end();
			while( iter != iter_end )
			{
				Serialise	(iter->first,	data);
				Serialise	(iter->second,	data);
				++iter;
			}
		}
		template <typename T> inline void SerialiseArray(const std::map<T1, T2>* map_array, std::size_t count, ByteCont& data)
		{
			for( const std::map<T1, T2>* map_array_end = map_array + count; map_array != map_array_end; ++map_array ) { Serialise(*map_array, data); }
		}

		// DeSerialise functions ***************************************************************************
		namespace impl
		{
			template <typename PodType> inline void DeSerialisePod(PodType& pod, const void*& data)
			{
				CopyMemory(&pod, data, sizeof(PodType));
				data = (unsigned char*)data + sizeof(PodType);
			}
			template <typename PodArrayType> inline void DeSerialisePodArray(PodArrayType* pod_array, std::size_t count, const void*& data)
			{
				CopyMemory(pod_array, data, count * sizeof(PodType));
				data = (unsigned char*)data + count * sizeof(PodType);
			}
		} // impl

		// Do not create a templated DeSerialise or DeSerialiseArray function. It's important that
		// people using these functions think about how to deserialise their objects. They should
		// write the appropriate DeSerialise or DeSerialiseArray function in the code where they need it.
		inline void DeSerialise		(char&			c,							const void*& data)	{ impl::DeSerialisePod<char>			(c, data); }
		inline void DeSerialise		(bool&			b,							const void*& data)	{ impl::DeSerialisePod<bool>			(b, data); }
		inline void DeSerialise		(uint&			i,							const void*& data)	{ impl::DeSerialisePod<uint>			(i, data); }
		inline void DeSerialise		(std::size_t&	s,							const void*& data)	{ impl::DeSerialisePod<std::size_t>		(s, data); }
		inline void DeSerialise		(float&			f,							const void*& data)	{ impl::DeSerialisePod<float>			(f, data); }
		inline void DeSerialise		(v4&			v,							const void*& data)	{ impl::DeSerialisePod<v4>				(v, data); }
		inline void DeSerialise		(m4x4&			m,							const void*& data)	{ impl::DeSerialisePod<m4x4>			(m, data); }
		inline void DeSerialise		(BBox&	bbox,						const void*& data)	{ impl::DeSerialisePod<BBox>		(bbox, data); }
		inline void DeSerialiseArray(char*			pc,		std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<char>		(pc,	count, data); }
		inline void DeSerialiseArray(uint8*			pui8,	std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<uint8>		(pui8,	count, data); }
		inline void DeSerialiseArray(uint*			pui,	std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<uint>		(pui,	count, data); }
		inline void DeSerialiseArray(float*			pf,		std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<float>		(pf,	count, data); }
		inline void DeSerialiseArray(v4*			pv,		std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<v4>			(pv,	count, data); }
		inline void DeSerialiseArray(m4x4*			pm,		std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<m4x4>		(pm,	count, data); }
		inline void DeSerialiseArray(BBox*	pbbox,	std::size_t count,	const void*& data)	{ impl::DeSerialisePodArray<BBox>(pbbox, count, data); }

		// Stl deserialise functions. If you get a compile error in these
		// functions you probably need to implement your own DeSerialise
		// and DeSerialiseArray for the types in your container
		inline void DeSerialise(std::string& str, const void*& data)
		{
			std::size_t length;
			DeSerialise		(length, data); str.resize(length, '\0');
			DeSerialiseArray(&str[0], length, data);
		}
		inline void DeSerialiseArray(std::string* str, std::size_t count, const void*& data)
		{
			for( std::string* str_end = str + count; str != str_end; ++str ) { DeSerialise(*str, data); }
		}
		template <typename T> inline void DeSerialise(std::vector<T>& vec, const void*& data)
		{
			std::size_t length;
			DeSerialise		(length, data);	vec.resize(length);
			DeSerialiseArray(&vec[0], length, data);
		}
		template <typename T> inline void DeSerialiseArray(const std::vector<T>* vec_array, std::size_t count, ByteCont& data)
		{
			for( const std::vector<T>* vec_array_end = vec_array + count; vec_array != vec_array_end; ++vec_array ) { DeSerialise(*vec_array, data); }
		}
		template <typename T> inline void DeSerialise(std::list<T>& list, const void*& data)
		{
			std::size_t length;
			DeSerialise		(length, data);
			for( std::size_t i = 0; i < length; ++i )
			{
				list.push_back(T);
				DeSerialise	(list.back(),	data);
			}
		}
		template <typename T> inline void DeSerialiseArray(const std::list<T>* list_array, std::size_t count, ByteCont& data)
		{
			for( const std::list<T>* list_array_end = list_array + count; list_array != list_array_end; ++list_array ) { DeSerialise(*list_array, data); }
		}
		template <typename T1, typename T2> inline void DeSerialise(std::map<T1, T2>& map, const void*& data)
		{
			std::size_t length;
			DeSerialise		(length, data);
			for( std::size_t i = 0; i < length; ++i )
			{
				T1 key;
				T2 type;
				DeSerialise	(key,	data);
				DeSerialise	(type,	data);
				map[key] = type;
			}
		}
		template <typename T> inline void DeSerialiseArray(const std::map<T1, T2>* map_array, std::size_t count, ByteCont& data)
		{
			for( const std::map<T1, T2>* map_array_end = map_array + count; map_array != map_array_end; ++map_array ) { DeSerialise(*map_array, data); }
		}
	}// namespace archiver
}//namespace pr
#endif
#endif
