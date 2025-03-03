//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"

namespace pr::rdr12::ldraw
{
	// Error codes for parsing failures
	enum class EParseError
	{
		UnknownError,
		UnknownKeyword,
		NotFound,
		InvalidValue,
		IndexOutOfRange,
		TooLarge,
		DataMissing,
		UnexpectedToken,
	};
	constexpr std::string_view ToString(EParseError code)
	{
		switch (code)
		{
			case EParseError::UnknownError: return "Unknown error";
			case EParseError::UnknownKeyword: return "Unknown Keyword";
			case EParseError::NotFound: return "Item not found";
			case EParseError::InvalidValue: return "Value is invalid";
			case EParseError::IndexOutOfRange: return "Index out of range";
			case EParseError::TooLarge: return "Object data size is too large";
			case EParseError::DataMissing: return "Data is missing";
			case EParseError::UnexpectedToken: return "Unexpected token";
			default: throw std::runtime_error("Unknown error code");
		}
	}

	// A location in a source file
	struct Location
	{
		std::filesystem::path m_filepath;
		int64_t m_offset;
		int m_column;
		int m_line;
	};

	// The results of parsing ldr script
	struct ParseResult
	{
		using ModelLookup = std::unordered_map<size_t, ModelPtr>;

		ObjectCont  m_objects;    // Reference to the objects container to fill
		ModelLookup m_models;     // A lookup map for models based on hashed object name
		Camera      m_cam;        // Camera description has been read
		ECamField   m_cam_fields; // Bitmask of fields in 'm_cam' that were given in the camera description
		bool        m_wireframe;  // True if '*Wireframe' was read in the script

		ParseResult()
			: m_objects()
			, m_models()
			, m_cam()
			, m_cam_fields()
			, m_wireframe()
		{
		}
		size_t count() const
		{
			return m_objects.size();
		}
		LdrObjectPtr operator[](size_t index) const
		{
			return m_objects[index];
		}
	};

	// Callback function type used during script parsing
	// 'bool function(Guid context_id, ParseResult& out, Location const& loc, bool complete)'
	// Returns 'true' to continue parsing, false to abort parsing.
	using ParseProgressCB = StaticCB<bool, Guid const&, ParseResult const&, Location const&, bool>;
	using ReportErrorCB = StaticCB<void, EParseError, Location const&, std::string_view>;
	using ParseEnumIdentCB = int64_t(*)(std::string_view);

	// Interface for parsing Ldraw script data
	struct IReader
	{
		// DOM:
		// - A nested tree of sections.
		// - Each section has a keyword identifier
		// - Sections either contain data, or nested sections, not both
		// - FindKeyword is not supported because this requires random access.
		//   The reader is intended to handle streamed data.
		// - Don't assume that 'ReportError' throws an exception.
		// - Strings/Identifiers:
		//    Strings are UTF8, that means a first byte with '10xxxxxx' is invalid.
		//    This can be used for string length. Each '10xxxxxx' contributes 6 bits to the string length.
		//    The first byte that doesn't match this pattern is the first UTF-8 character code.
		// - Style suggestion:
		//    for (int kw; reader.NextKeyword(kw);) switch (kw)
		//    {
		//        case rdr12::ldraw::HashI("Radius"): radius = reader.Vector2f(); break;
		//        case rdr12::ldraw::HashI("Depth"): depth = reader.Int<int>() != 0; break;
		//    }
		IReader(ReportErrorCB report_error_cb = nullptr, ParseProgressCB progress_cb = nullptr, IPathResolver const& resolver = PathResolver::Instance())
			: ReportError(report_error_cb ? report_error_cb : &ReportErrorDefaultCB)
			, Progress(progress_cb ? progress_cb : &ParseProgressDefaultCB)
			, PathResolver(resolver)
		{
		}
		virtual ~IReader() = default;

		// Error handling
		ReportErrorCB ReportError;

		// Progress handling
		ParseProgressCB Progress;

		// Path resolver
		IPathResolver const& PathResolver;

		// Get/Set the current location in the source
		virtual Location const& Loc() const = 0;

		// Move into a nested section
		virtual void PushSection() = 0;

		// Leave the current nested section
		virtual void PopSection() = 0;

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() = 0;

		// True when the source is exhausted
		virtual bool IsSourceEnd() = 0;

		// RAII section scope. Use this if a section contains nested keywords
		Scope<void> SectionScope()
		{
			return Scope<void>(
				[this] { PushSection(); },
				[this] { PopSection(); });
		}

		// Get the next keyword within the current section. Returns false if at the end of the section
		template <typename TKeyword> bool NextKeyword(TKeyword& kw)
		{
			auto kw_int = 0;
			if (!NextKeywordImpl(kw_int)) return false;
			kw = static_cast<TKeyword>(kw_int);
			return true;
		}

		// Read an identifier from the current section
		template <typename StrType> StrType Identifier()
		{
			auto s = IdentifierImpl();
			if constexpr (std::is_same_v<StrType, string32>)
				return s;
			else
				return StrType(std::begin(s), std::end(s));
		}

		// Read a string from the current section
		template <typename StrType> StrType String(char escape_char = 0)
		{
			auto s = StringImpl(escape_char);
			if constexpr (std::is_same_v<StrType, string32>)
				return s;
			else
				return StrType(std::begin(s), std::end(s));
		}

		// Read an integer value
		template <std::integral IntType> IntType Int(int radix = 10)
		{
			return static_cast<IntType>(IntImpl(sizeof(IntType), radix));
		}

		// Read a floating point value
		template <std::floating_point FloatType> FloatType Real()
		{
			return static_cast<FloatType>(RealImpl(sizeof(FloatType)));
		}

		// Read an enumeration value as type 'TEnum'
		template <typename TEnum> requires std::is_enum_v<TEnum> TEnum Enum()
		{
			if constexpr (ReflectedEnum<TEnum>)
			{
				static ParseEnumIdentCB parse = [](std::string_view str) { return static_cast<int64_t>(pr::Enum<TEnum>::Parse(str, false)); };
				auto value = EnumImpl(sizeof(TEnum), parse);
				return static_cast<TEnum>(value);
			}
			else
			{
				static ParseEnumIdentCB parse = [](std::string_view str) { return static_cast<int64_t>(pr::To<TEnum>(str)); };
				auto value = EnumImpl(sizeof(TEnum), parse);
				return static_cast<TEnum>(value);
			}
		}

		// Read a boolean value
		bool Bool()
		{
			return BoolImpl();
		}

		// Read floating point vectors
		v2 Vector2f()
		{
			return v2{ Real<float>(), Real<float>() };
		}
		v3 Vector3f()
		{
			return v3{ Real<float>(), Real<float>(), Real<float>() };
		}
		v4 Vector4f()
		{
			return v4{ Real<float>(), Real<float>(), Real<float>(), Real<float>() }; // Note; brace initializer guarantees call order
		}

		// Read integer vectors
		iv2 Vector2i(int radix = 10)
		{
			return iv2{ Int<int>(radix), Int<int>(radix) };
		}
		iv3 Vector3i(int radix = 10)
		{
			return iv3{ Int<int>(radix), Int<int>(radix), Int<int>(radix) };
		}
		iv4 Vector4i(int radix = 10)
		{
			return iv4{ Int<int>(radix), Int<int>(radix), Int<int>(radix), Int<int>(radix) };
		}

		// Read matrix types
		m3x4 Matrix3x3()
		{
			return m3x4{ Vector3f().w0(), Vector3f().w0(), Vector3f().w0() };
		}
		m4x4 Matrix4x4()
		{
			return m4x4{ Vector4f(), Vector4f(), Vector4f(), Vector4f() };
		}

		// Reads a transform accumulatively. 'o2w' must be a valid initial transform
		m4x4& Transform(m4x4& o2w);

		// Get the next keyword within the current section. Returns false if at the end of the section
		virtual bool NextKeywordImpl(int& kw) = 0;

		// Read an identifier from the current section. Leading '10xxxxxx' bytes are the length (in bytes). Default length is the full section
		virtual string32 IdentifierImpl() = 0;

		// Read a utf8 string from the current section. Leading '10xxxxxx' bytes are the length (in bytes). Default length is the full section
		virtual string32 StringImpl(char escape_char = 0) = 0;

		// Read an integral value from the current section
		virtual int64_t IntImpl(int byte_count, int radix) = 0;

		// Read a floating point value from the current section
		virtual double RealImpl(int byte_count) = 0;

		// Read an enum value from the current section. Text readers will read an identifier and use 'parse'. Binary reader will read an integer value.
		virtual int64_t EnumImpl(int byte_count, ParseEnumIdentCB parse) = 0;

		// Read a boolean value from the current section
		virtual bool BoolImpl() = 0;

		// Defaults for callbacks
		static bool ParseProgressDefaultCB(void*, Guid const&, ParseResult const&, Location const&, bool) { return true; }
		static void ReportErrorDefaultCB(void*, EParseError, Location const&, std::string_view) {}
	};

	// Parse the ldr script in 'reader' adding the results to 'out'.
	// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
	// There is synchronisation in the renderer for creating/allocating models. The calling thread must control the
	// life-times of the script reader, the parse output, and the 'store' container it refers to.
	ParseResult Parse(
		Renderer& rdr,                      // The renderer to create models for
		IReader& reader,                    // The source of the script
		Guid const& context_id = GuidZero); // The context id to assign to each created object
	ParseResult Parse(
		Renderer& rdr,                      // The renderer to create models for
		std::string_view ldr_script,        // The source of the script
		Guid const& context_id = GuidZero); // The context id to assign to each created object
	ParseResult Parse(
		Renderer& rdr,                      // The renderer to create models for
		std::wstring_view ldr_script,       // The source of the script
		Guid const& context_id = GuidZero); // The context id to assign to each created object
	ParseResult ParseFile(
		Renderer& rdr,                      // The renderer to create models for
		std::filesystem::path ldr_filepath, // The source of the script
		Guid const& context_id = GuidZero); // The context id to assign to each created object

	// Create an ldr object from creation data.
	LdrObjectPtr Create(
		Renderer& rdr,                      // The reader to create models for
		ELdrObject type,                    // Object type
		MeshCreationData const& cdata,      // Model creation data
		Guid const& context_id = GuidZero); // The context id to assign to the object

	// Create an ldr object from a p3d model.
	LdrObjectPtr CreateP3D(
		Renderer& rdr,                             // The reader to create models for
		ELdrObject type,                           // Object type
		std::filesystem::path const& p3d_filepath, // Model filepath
		Guid const& context_id = GuidZero);        // The context id to assign to the object
	LdrObjectPtr CreateP3D(
		Renderer& rdr,                       // The reader to create models for
		ELdrObject type,                     // Object type
		std::span<std::byte const> p3d_data, // The length of the data pointed to by 'p3d_data'
		Guid const& context_id = GuidZero);  // The context id to assign to the object

	// Create an instance of an existing ldr object.
	LdrObjectPtr CreateInstance(
		LdrObject const* existing);         // The existing object whose model the instance will use.

	// Callback function for editing a dynamic model
	// This callback is intentionally low level, providing the whole model for editing.
	// Remember to update the bounding box, vertex and index ranges, and regenerate nuggets.
	using EditObjectCB = void(__stdcall*)(Model* model, void* ctx, Renderer& rdr);

	// Create an ldr object using a callback to populate the model data.
	// Objects created by this method will have dynamic usage and are suitable for updating every frame via the 'Edit' function.
	LdrObjectPtr CreateEditCB(
		Renderer& rdr,                      // The reader to create models for
		ELdrObject type,                    // Object type
		int vcount,                         // The number of verts to create the model with
		int icount,                         // The number of indices to create the model with
		int ncount,                         // The number of nuggets to create the model with
		EditObjectCB edit_cb,               // The callback function, called after the model is created, to populate the model data
		void* ctx,                          // Callback user context
		Guid const& context_id = GuidZero); // The context id to assign to the object

	// Modify the geometry of an LdrObject
	void Edit(Renderer& rdr, LdrObject* object, EditObjectCB edit_cb, void* ctx);

	// Update 'object' with info from 'desc'. 'keep' describes the properties of 'object' to update
	void Update(Renderer& rdr, LdrObject* object, IReader& reader, EUpdateObject flags);

	// Remove all objects from 'objects' that have a context id matching one in 'incl' and not in 'excl'
	// If 'incl' is empty, all are assumed included. If 'excl' is empty, none are assumed excluded.
	// 'excl' is considered after 'incl' so if any context ids are in both arrays, they will be excluded.
	void Remove(ObjectCont& objects, std::span<Guid const> incl, std::span<Guid const> excl);

	// Remove 'obj' from 'objects'
	void Remove(ObjectCont& objects, LdrObject* obj);

	// Generate a scene that demos the supported object types and modifiers.
	std::string CreateDemoScene();

	// Return the auto completion templates
	std::string AutoCompleteTemplates();
}