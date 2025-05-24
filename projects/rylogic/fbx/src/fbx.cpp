//********************************
// FBX Model loader
//  Copyright (c) Rylogic Ltd 2014
//********************************
// FBX files come in two variants; binary and text.
// The format is closed source however, so we need to use the AutoDesk FBX SDK.
#include <stdexcept>
#include <string_view>
#include <unordered_set>
#include <format>
#include <deque>
#include <fbxsdk.h>
#include "pr/common/to.h"
#include "pr/common/cast.h"
#include "pr/common/allocator.h"
#include "pr/maths/maths.h"
#include "pr/maths/bbox.h"
#include "pr/geometry/fbx.h"

using namespace fbxsdk;

// Extensions
namespace fbxsdk
{
	struct FbxVersion { int Major = 0; int Minor = 0; int Revs = 0; };

	using FbxObjectCleanUp = struct FbxObjectDeleter
	{
		void operator()(FbxManager* man) { man->Destroy(); }
		void operator()(FbxObject* imp) { imp->Destroy(); }
		void operator()(FbxScene* scene) { scene->Destroy(); }
	};
	using ManagerPtr = std::unique_ptr<FbxManager, FbxObjectCleanUp>;
	using ImporterPtr = std::unique_ptr<FbxImporter, FbxObjectCleanUp>;
	using ExporterPtr = std::unique_ptr<FbxExporter, FbxObjectCleanUp>;
	using ScenePtr = std::unique_ptr<FbxScene, FbxObjectCleanUp>;

	inline double FloatClamp(double f)
	{
		return
			f >= +HUGE_VAL ? +std::numeric_limits<double>::infinity() :
			f <= -HUGE_VAL ? -std::numeric_limits<double>::infinity() :
			f;
	}
	std::ostream& operator << (std::ostream& out, FbxNodeAttribute::EType node_type)
	{
		switch (node_type)
		{
			case FbxNodeAttribute::EType::eUnknown: return out << "Unknown";
			case FbxNodeAttribute::EType::eNull: return out << "Null";
			case FbxNodeAttribute::EType::eMarker: return out << "Marker";
			case FbxNodeAttribute::EType::eSkeleton: return out << "Skeleton";
			case FbxNodeAttribute::EType::eMesh: return out << "Mesh";
			case FbxNodeAttribute::EType::eNurbs: return out << "Nurbs";
			case FbxNodeAttribute::EType::ePatch: return out << "Patch";
			case FbxNodeAttribute::EType::eCamera: return out << "Camera";
			case FbxNodeAttribute::EType::eCameraStereo: return out << "CameraStereo";
			case FbxNodeAttribute::EType::eCameraSwitcher: return out << "CameraSwitcher";
			case FbxNodeAttribute::EType::eLight: return out << "Light";
			case FbxNodeAttribute::EType::eOpticalReference: return out << "OpticalReference";
			case FbxNodeAttribute::EType::eOpticalMarker: return out << "OpticalMarker";
			case FbxNodeAttribute::EType::eNurbsCurve: return out << "NurbsCurve";
			case FbxNodeAttribute::EType::eTrimNurbsSurface: return out << "TrimNurbsSurface";
			case FbxNodeAttribute::EType::eBoundary: return out << "Boundary";
			case FbxNodeAttribute::EType::eNurbsSurface: return out << "NurbsSurface";
			case FbxNodeAttribute::EType::eShape: return out << "Shape";
			case FbxNodeAttribute::EType::eLODGroup: return out << "LODGroup";
			case FbxNodeAttribute::EType::eSubDiv: return out << "SubDiv";
			case FbxNodeAttribute::EType::eCachedEffect: return out << "CachedEffect";
			case FbxNodeAttribute::EType::eLine: return out << "Lin";
			default: return out << "Unknown";
		}
	}
	std::ostream& operator << (std::ostream& out, FbxVector2 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]);
	}
	std::ostream& operator << (std::ostream& out, FbxVector4 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]) << ", " << FloatClamp(vec[2]) << ", " << FloatClamp(vec[3]);
	}
	std::ostream& operator << (std::ostream& out, FbxAMatrix const& vec)
	{
		return out
			<< FloatClamp(vec[0][0]) << ", " << FloatClamp(vec[0][1]) << ", " << FloatClamp(vec[0][2]) << ", " << FloatClamp(vec[0][3]) << ", "
			<< FloatClamp(vec[1][0]) << ", " << FloatClamp(vec[1][1]) << ", " << FloatClamp(vec[1][2]) << ", " << FloatClamp(vec[1][3]) << ", "
			<< FloatClamp(vec[2][0]) << ", " << FloatClamp(vec[2][1]) << ", " << FloatClamp(vec[2][2]) << ", " << FloatClamp(vec[2][3]) << ", "
			<< FloatClamp(vec[3][0]) << ", " << FloatClamp(vec[3][1]) << ", " << FloatClamp(vec[3][2]) << ", " << FloatClamp(vec[3][3]);
	}
	std::ostream& operator << (std::ostream& out, FbxDouble2 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]);
	}
	std::ostream& operator << (std::ostream& out, FbxDouble3 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]) << ", " << FloatClamp(vec[2]);
	}
	std::ostream& operator << (std::ostream& out, FbxDouble4 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]) << ", " << FloatClamp(vec[2]) << ", " << FloatClamp(vec[3]);
	}
	std::ostream& operator << (std::ostream& out, FbxDouble4x4 const& vec)
	{
		return out
			<< FloatClamp(vec[0][0]) << ", " << FloatClamp(vec[0][1]) << ", " << FloatClamp(vec[0][2]) << ", " << FloatClamp(vec[0][3]) << ", "
			<< FloatClamp(vec[1][0]) << ", " << FloatClamp(vec[1][1]) << ", " << FloatClamp(vec[1][2]) << ", " << FloatClamp(vec[1][3]) << ", "
			<< FloatClamp(vec[2][0]) << ", " << FloatClamp(vec[2][1]) << ", " << FloatClamp(vec[2][2]) << ", " << FloatClamp(vec[2][3]) << ", "
			<< FloatClamp(vec[3][0]) << ", " << FloatClamp(vec[3][1]) << ", " << FloatClamp(vec[3][2]) << ", " << FloatClamp(vec[3][3]);
	}
	std::ostream& operator << (std::ostream& out, FbxColor const& col)
	{
		return out << "R=" << (float)col.mRed << ", G=" << (float)col.mGreen << ", B=" << (float)col.mBlue << ", A=" << col.mAlpha;
	}
	std::ostream& operator << (std::ostream& out, FbxProperty prop)
	{
		auto data_type = prop.GetPropertyDataType();
		if (data_type == FbxBoolDT)
		{
			out << "  Bool: " << prop.Get<FbxBool>();
		}
		else if (data_type == FbxIntDT)
		{
			out << "   Int: " << prop.Get<FbxInt>();
		}
		else if (data_type == FbxEnumDT)
		{
			out << "  Enum: " << prop.Get<FbxInt>();
		}
		else if (data_type == FbxFloatDT)
		{
			out << " Float: " << prop.Get<FbxFloat>();
		}
		else if (data_type == FbxDoubleDT)
		{
			out << "Double: " << prop.Get<FbxDouble>();
		}
		else if (data_type == FbxStringDT)
		{
			out << "String: " << prop.Get<FbxString>().Buffer();
		}
		else if (data_type == FbxUrlDT)
		{
			out << "   URL: " << prop.Get<FbxString>().Buffer();
		}
		else if (data_type == FbxXRefUrlDT)
		{
			out << "RefURL: " << prop.Get<FbxString>().Buffer();
		}
		else if (data_type == FbxDouble2DT)
		{
			out << " Vec2D: " << prop.Get<FbxDouble2>();
		}
		else if (data_type == FbxDouble3DT)
		{
			out << " Vec3D: " << prop.Get<FbxDouble3>();
		}
		else if (data_type == FbxDouble4DT)
		{
			out << " Vec4D: " << prop.Get<FbxDouble4>();
		}
		else if (data_type == FbxColor3DT)
		{
			out << "Color3: " << prop.Get<FbxDouble3>();
		}
		else if (data_type == FbxColor4DT)
		{
			out << " Vec4D: " << prop.Get<FbxDouble4>();
		}
		else if (data_type == FbxDouble4x4DT)
		{
			out << "Mat4x4: " << prop.Get<FbxDouble4x4>();
		}
		return out;
	}
}
namespace pr
{
	// Colour
	template <>
	struct Convert<Colour, FbxColor>
	{
		static Colour To_(FbxColor const& c)
		{
			return Colour(s_cast<float>(c.mRed), s_cast<float>(c.mGreen), s_cast<float>(c.mBlue), s_cast<float>(c.mAlpha));
		}
	};
	template <>
	struct Convert<Colour, FbxDouble3>
	{
		static Colour To_(FbxDouble3 const& c)
		{
			return Colour(s_cast<float>(c[0]), s_cast<float>(c[1]), s_cast<float>(c[2]), 1.0f);
		}
	};

	// Vec2
	template <Scalar S>
	struct Convert<Vec2<S, void>, FbxVector2>
	{
		static Vec2<S, void> To_(FbxVector2 const& v)
		{
			return Vec2<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]));
		}
	};

	// Vec4
	template <Scalar S>
	struct Convert<Vec4<S, void>, FbxDouble3>
	{
		static Vec4<S, void> To_(FbxDouble3 const& v)
		{
			return Vec4<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]), s_cast<S>(v[2]), s_cast<S>(0));
		}
	};

	// Vec4
	template <Scalar S>
	struct Convert<Vec4<S, void>, FbxDouble4>
	{
		static Vec4<S, void> To_(FbxDouble4 const& v)
		{
			return Vec4<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]), s_cast<S>(v[2]), s_cast<S>(v[3]));
		}
	};
	template <Scalar S>
	struct Convert<Vec4<S, void>, FbxVector4>
	{
		static Vec4<S, void> To_(FbxVector4 const& v)
		{
			return Vec4<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]), s_cast<S>(v[2]), s_cast<S>(v[3]));
		}
	};

	// Mat4x4
	template <Scalar S>
	struct Convert<Mat4x4<S, void, void>, FbxAMatrix>
	{
		static Mat4x4<S, void, void> To_(FbxAMatrix const& m)
		{
			return Mat4x4<S, void, void>(
				Vec4<S, void>(s_cast<S>(m[0][0]), s_cast<S>(m[0][1]), s_cast<S>(m[0][2]), s_cast<S>(m[0][3])),
				Vec4<S, void>(s_cast<S>(m[1][0]), s_cast<S>(m[1][1]), s_cast<S>(m[1][2]), s_cast<S>(m[1][3])),
				Vec4<S, void>(s_cast<S>(m[2][0]), s_cast<S>(m[2][1]), s_cast<S>(m[2][2]), s_cast<S>(m[2][3])),
				Vec4<S, void>(s_cast<S>(m[3][0]), s_cast<S>(m[3][1]), s_cast<S>(m[3][2]), s_cast<S>(m[3][3])));
		}
	};

	// Bone type
	template <>
	struct Convert<geometry::fbx::EBoneType, FbxSkeleton::EType>
	{
		static geometry::fbx::EBoneType To_(FbxSkeleton::EType ty)
		{
			using EType = geometry::fbx::EBoneType;
			switch (ty)
			{
				case FbxSkeleton::EType::eRoot: return EType::Root;
				case FbxSkeleton::EType::eLimb: return EType::Limb;
				case FbxSkeleton::EType::eLimbNode: return EType::Limb;
				case FbxSkeleton::EType::eEffector: return EType::Effector;
				default: throw std::runtime_error(std::format("Unknown bone type: {}", int(ty)));
			}
		}
	};
}

// FBX support
namespace pr::geometry::fbx
{
	static constexpr int NoIndex = -1;
	static constexpr Vert NoVert = {};
	using ErrorList = std::vector<std::string>;

	// Check that 'ptr' is not null. Throw if it is
	template <typename T> inline T* Check(T* ptr, std::string_view message)
	{
		if (ptr) return ptr;
		throw std::runtime_error(std::string(message));
	}

	// RAII fbx array wrapper
	template <typename T>
	struct FbxArray : fbxsdk::FbxArray<T>
	{
		FbxArray() : fbxsdk::FbxArray<T>() {}
		~FbxArray()
		{
			FbxArrayDelete<T>(*this);
		}
	};

	// RAII wrapper for FBX manager
	struct Manager
	{
		FbxManager* m_manager;
		FbxIOSettings* m_settings;
		char const* m_version;

		Manager()
			: m_manager(Check(FbxManager::Create(), "Error: Unable to create FBX Manager"))
			, m_settings(Check(FbxIOSettings::Create(m_manager, IOSROOT), "Error: Unable to create settings"))
			, m_version(m_manager->GetVersion())
		{
			m_manager->SetIOSettings(m_settings);

			// Set the export states. By default, all options should be enabled
			// m_settings->SetBoolProp(EXP_FBX_MATERIAL, true);
			// m_settings->SetBoolProp(EXP_FBX_TEXTURE, true);
			// m_settings->SetBoolProp(EXP_FBX_EMBEDDED, true);
			// m_settings->SetBoolProp(EXP_FBX_SHAPE, true);
			// m_settings->SetBoolProp(EXP_FBX_GOBO, true);
			// m_settings->SetBoolProp(EXP_FBX_ANIMATION, true);
			// m_settings->SetBoolProp(EXP_FBX_GLOBAL_SETTINGS, true);

			// Set the import states. By default, all options should be enabled
			//m_settings->SetBoolProp(IMP_SKINS, true);
			//m_settings->SetBoolProp(IMP_DEFORMATION, true);
			//m_settings->SetBoolProp(IMP_BONE, true);
			//m_settings->SetBoolProp(IMP_TAKE, true);
			//
			//m_settings->SetBoolProp(IMP_FBX_MODEL_COUNT, true);
			//m_settings->SetBoolProp(IMP_FBX_DEVICE_COUNT, true);
			//m_settings->SetBoolProp(IMP_FBX_CHARACTER_COUNT, true);
			//m_settings->SetBoolProp(IMP_FBX_ACTOR_COUNT, true);
			//m_settings->SetBoolProp(IMP_FBX_CONSTRAINT_COUNT, true);
			//m_settings->SetBoolProp(IMP_FBX_MEDIA_COUNT, true);
			//
			//m_settings->SetBoolProp(IMP_FBX_TEMPLATE, true);
			//m_settings->SetBoolProp(IMP_FBX_PIVOT, true);
			//m_settings->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, true);
			//m_settings->SetBoolProp(IMP_FBX_CHARACTER, true);
			//m_settings->SetBoolProp(IMP_FBX_CONSTRAINT, true);
			//m_settings->SetBoolProp(IMP_FBX_MERGE_LAYER_AND_TIMEWARP, true);
			//m_settings->SetBoolProp(IMP_FBX_GOBO, true);
			//m_settings->SetBoolProp(IMP_FBX_SHAPE, true);
			//m_settings->SetBoolProp(IMP_FBX_LINK, true);

			//m_settings->SetBoolProp(IMP_FBX_MATERIAL, true);
			//m_settings->SetBoolProp(IMP_FBX_TEXTURE, true);
			//m_settings->SetBoolProp(IMP_FBX_MODEL, true);
			//m_settings->SetBoolProp(IMP_FBX_AUDIO, true);
			//m_settings->SetBoolProp(IMP_FBX_ANIMATION, true);
			//m_settings->SetBoolProp(IMP_FBX_PASSWORD, true);
			//m_settings->SetBoolProp(IMP_FBX_PASSWORD_ENABLE, true);
			//m_settings->SetBoolProp(IMP_FBX_CURRENT_TAKE_NAME, true);
			//m_settings->SetBoolProp(IMP_FBX_EXTRACT_EMBEDDED_DATA, true);
			//m_settings->SetBoolProp(IMP_FBX_CALCULATE_LEGACY_SHAPE_NORMAL, true);

			//m_settings->SetBoolProp(IMP_FBX_NORMAL, true);
			//m_settings->SetBoolProp(IMP_FBX_BINORMAL, true);
			//m_settings->SetBoolProp(IMP_FBX_TANGENT, true);
			//m_settings->SetBoolProp(IMP_FBX_VERTEXCOLOR, true);
			//m_settings->SetBoolProp(IMP_FBX_POLYGROUP, true);
			//m_settings->SetBoolProp(IMP_FBX_SMOOTHING, true);
			//m_settings->SetBoolProp(IMP_FBX_USERDATA, true);
			//m_settings->SetBoolProp(IMP_FBX_VISIBILITY, true);
			//m_settings->SetBoolProp(IMP_FBX_EDGECREASE, true);
			//m_settings->SetBoolProp(IMP_FBX_VERTEXCREASE, true);
			//m_settings->SetBoolProp(IMP_FBX_HOLE, true);

			//#ifndef FBXSDK_ENV_WINSTORE
			////Load plugins from the executable directory (optional)
			//FbxString lPath = FbxGetApplicationDirectory();
			//m_manager->LoadPluginsDirectory(lPath.Buffer());
			//#endif
		}
		~Manager()
		{
			// Notes:
			//  - FbxImporter must be destroyed before any FbxScenes it creates because of bugs in the 'fbxsdk' dll.
			
			// Clean up settings
			if (m_settings != nullptr)
				m_settings->Destroy();

			// Delete the FBX Manager. All the objects that have been allocated using the FBX Manager and that haven't been explicitly destroyed are also automatically destroyed.
			if (m_manager != nullptr)
				m_manager->Destroy();
		}

		// Return the file format ID for the give format (use 'Formats')
		int FileFormatID(char const* format) const
		{
			auto file_format = m_manager->GetIOPluginRegistry()->FindReaderIDByDescription(format);
			return file_format;
		}

		// Get/Set properties. Use the 'EXP_FBX_'/'IMP_FBX_...' flags
		template <typename T> T Prop(char const* prop_name, T default_value = {}) const
		{
			if constexpr (std::is_same_v<T, bool>)
			{
				return m_settings->GetBoolProp(prop_name, default_value);
			}
			else if constexpr (std::is_same_v<T, std::string>)
			{
				FbxString pw = m_settings->GetStringProp(prop_name, "");
				return pw.Buffer();
			}
			else
			{
				static_assert(pr::dependent_false<T>, "Unknown property type");
			}
		}
		template <typename T> void Prop(char const* prop_name, T value)
		{
			if constexpr (std::is_same_v<T, bool>)
			{
				m_settings->SetBoolProp(prop_name, value);
			}
			else if constexpr (std::is_same_v<T, std::string>)
			{
				FbxString val(value.data(), value.size());
				m_settings->SetStringProp(prop_name, val);
			}
			else
			{
				static_assert(pr::dependent_false<T>, "Unknown property type");
			}
		}

		// Get/set the password
		std::string Password() const
		{
			return Prop<std::string>(IMP_FBX_PASSWORD);
		}
		void Password(std::string_view password)
		{
			Prop<std::string>(IMP_FBX_PASSWORD, std::string{ password });
			Prop<bool>(IMP_FBX_PASSWORD_ENABLE, !password.empty());
		}

		FbxManager* operator ->() const
		{
			return m_manager;
		}
		operator FbxManager* () const
		{
			return m_manager;
		}
	};

	// Write an FBX scene to disk
	static void Export(Manager& manager, std::ostream& out, FbxScene& scene, char const* format = Formats::FbxBinary, ErrorList* errors = nullptr)
	{
		ExporterPtr exporter(Check(FbxExporter::Create(manager, ""), "Failed to create Exporter"));

		// Adapter from FbxStream to std::ostream
		struct OStream :FbxStream
		{
			std::ostream& m_out;
			int m_format;

			OStream(std::ostream& out, int format)
				: m_out(out)
				, m_format(format)
			{
				if (!m_out.good())
					throw std::runtime_error("FBX output stream is unhealthy");
			}
			OStream(OStream&&) = delete;
			OStream(OStream const&) = delete;
			OStream& operator =(OStream&&) = delete;
			OStream& operator =(OStream const&) = delete;

			virtual int GetReaderID() const override { return -1; }
			virtual int GetWriterID() const override { return m_format; }

			// Query the current state of the stream.
			virtual EState GetState() override { return m_out.bad() ? EState::eClosed : EState::eOpen; }

			// Open the stream.
			virtual bool Open(void* /*pStreamData*/) override { return m_out.seekp(0).good(); }

			// Close the stream.
			virtual bool Close() override { return true; }

			// Empties the internal data of the stream.
			virtual bool Flush() override { return m_out.flush().good(); }

			// Writes a memory block.
			virtual size_t Write(const void* data, FbxUInt64 size) override { return m_out.write(static_cast<char const*>(data), static_cast<std::streamsize>(size)).good() ? size : 0; }

			// Read bytes from the stream and store them in the memory block.
			virtual size_t Read(void* /*data*/, FbxUInt64 /*size*/) const override { throw std::runtime_error("not implemented"); }

			// Adjust the current stream position.
			virtual void Seek(const FbxInt64& offset, FbxFile::ESeekPos const& seek_pos) override { m_out.seekp(static_cast<std::streamoff>(offset), static_cast<int>(seek_pos)); }

			// Get the current stream position.
			virtual FbxInt64 GetPosition() const override { return static_cast<FbxInt64>(m_out.tellp()); }

			// Set the current stream position.
			virtual void SetPosition(FbxInt64 position) override { m_out.seekp(static_cast<std::streampos>(position)); }

			// Return 0 if no errors occurred. Otherwise, return 1 to indicate an error.
			virtual int GetError() const override { return m_out.good() ? 0 : 1; }

			// Clear current error condition by setting the current error value to 0.
			virtual void ClearError() override { throw std::runtime_error("not implemented"); }
		};

		// Initialise the exporter
		OStream stream(out, manager.FileFormatID(format));
		if (!exporter->Initialize(&stream, nullptr, manager.FileFormatID(format)))
		{
			FbxArray<FbxString*> history;
			exporter->GetStatus().GetErrorStringHistory(history);

			// Record any errors
			if (errors != nullptr)
			{
				for (int i = 0; i != history.GetCount(); ++i)
					errors->push_back(history[i]->Buffer());
			}

			// Throw the error
			throw std::runtime_error(std::format("FbxExporter::Initialize() failed. {}", exporter->GetStatus().GetErrorString()));
		}

		// Do the export
		auto result = exporter->Export(&scene);
		if (!result || exporter->GetStatus() != FbxStatus::eSuccess)
			throw std::runtime_error(std::format("Failed to write fbx file. {}", exporter->GetStatus().GetErrorString()));
	}

	// Parse and FBX Scene from 'src'
	static ScenePtr Import(Manager& manager, std::istream& src, char const* format = Formats::FbxBinary, ErrorList* errors = nullptr)
	{
		ScenePtr scene(Check(FbxScene::Create(manager, ""), "Error: Unable to create FBX scene"));

		// Adapter from FbxStream to std::istream
		struct IStream :FbxStream
		{
			std::istream& m_src;
			int m_format;

			IStream(std::istream& src, int format)
				: m_src(src)
				, m_format(format)
			{
				if (!m_src.good())
					throw std::runtime_error("FBX input stream is unhealthy");
			}
			IStream(IStream&&) = delete;
			IStream(IStream const&) = delete;
			IStream& operator =(IStream&&) = delete;
			IStream& operator =(IStream const&) = delete;

			virtual int GetReaderID() const override { return m_format; }
			virtual int GetWriterID() const override { return -1; }

			// Query the current state of the stream.
			virtual EState GetState() override { return m_src.bad() ? EState::eClosed : m_src.peek() == std::char_traits<char>::eof() ? EState::eEmpty : EState::eOpen; }

			// Open the stream.
			virtual bool Open(void* /*pStreamData*/) override { return m_src.seekg(0).good(); }

			// Close the stream.
			virtual bool Close() override { return true; }

			// Empties the internal data of the stream.
			virtual bool Flush() override { throw std::runtime_error("not implemented"); }

			// Writes a memory block.
			virtual size_t Write(const void* /*pData*/, FbxUInt64 /*pSize*/) override { throw std::runtime_error("not implemented"); }

			// Read bytes from the stream and store them in the memory block.
			virtual size_t Read(void* pData, FbxUInt64 pSize) const override { return m_src.read(static_cast<char*>(pData), static_cast<std::streamsize>(pSize)).gcount(); }

			// Adjust the current stream position.
			virtual void Seek(const FbxInt64& offset, FbxFile::ESeekPos const& seek_pos) override { m_src.seekg(static_cast<std::streamoff>(offset), static_cast<int>(seek_pos)); }

			// Get the current stream position.
			virtual FbxInt64 GetPosition() const override { return static_cast<FbxInt64>(m_src.tellg()); }

			// Set the current stream position.
			virtual void SetPosition(FbxInt64 pPosition) override { m_src.seekg(static_cast<std::streampos>(pPosition)); }

			// Return 0 if no errors occurred. Otherwise, return 1 to indicate an error.
			virtual int GetError() const override { return m_src.good() ? 0 : 1; }

			// Clear current error condition by setting the current error value to 0.
			virtual void ClearError() override { throw std::runtime_error("not implemented"); }
		};

		// Import the stream
		IStream stream(src, manager.FileFormatID(format));
		ImporterPtr importer(Check(FbxImporter::Create(manager, ""), "Failed to create Importer"));
		if (!importer->Initialize(&stream, nullptr, manager.FileFormatID(format)))
		{
			FbxArray<FbxString*> history;
			importer->GetStatus().GetErrorStringHistory(history);

			// Record any errors
			if (errors != nullptr)
			{
				for (int i = 0; i != history.GetCount(); ++i)
					errors->push_back(history[i]->Buffer());
			}

			// Throw the error
			throw std::runtime_error(std::format("FbxImporter::Initialize() failed. {}", importer->GetStatus().GetErrorString()));
		}

		// Check the file is actually an fbx file
		if (!importer->IsFBX())
			throw std::runtime_error(std::format("Imported file is not an FBX file"));

		// Import the scene.
		auto result = importer->Import(scene.get());
		if (!result || importer->GetStatus() != FbxStatus::eSuccess)
			throw std::runtime_error(std::format("Failed to read fbx from file. {}", importer->GetStatus().GetErrorString()));

		return scene;
	}
	
	// Get a value from a layer element
	template <typename T>
	static T GetLayerElement(FbxLayerElementTemplate<T> const* layer_elements, int fidx, int iidx, int vidx)
	{
		// 'f' (for face) = the polygon index
		// 'i' (for ibuf index) = the polygon * verts_per_poly index. e.g, if the polys are all triangles, then poly_idx = 'polygon * 3 + i'
		// 'v' (for vertex) = the control point (vertex) index
		auto ref_mode = layer_elements->GetReferenceMode();
		switch (ref_mode)
		{
			case FbxLayerElement::eIndex: // legacy
			case FbxLayerElement::eIndexToDirect:
			{
				auto mapping_mode = layer_elements->GetMappingMode();
				switch (mapping_mode)
				{
					case FbxLayerElement::eByControlPoint:
					{
						auto idx = layer_elements->GetIndexArray().GetAt(vidx);
						return layer_elements->GetDirectArray().GetAt(idx);
					}
					case FbxLayerElement::eByPolygonVertex:
					{
						auto idx = layer_elements->GetIndexArray().GetAt(iidx);
						return layer_elements->GetDirectArray().GetAt(idx);
					}
					case FbxLayerElement::eByPolygon:
					{
						auto idx = layer_elements->GetIndexArray().GetAt(fidx);
						return layer_elements->GetDirectArray().GetAt(idx);
					}
					case FbxLayerElement::eByEdge:
					{
						throw std::runtime_error("ByEdge mapping not implemented");
					}
					case FbxLayerElement::eAllSame:
					{
						auto idx = layer_elements->GetIndexArray().GetAt(0);
						return layer_elements->GetDirectArray().GetAt(idx);
					}
					default:
					{
						throw std::runtime_error("Unsupported mapping mode");
					}
				}
			}
			case FbxLayerElement::eDirect:
			{
				auto mapping_mode = layer_elements->GetMappingMode();
				switch (mapping_mode)
				{
					case FbxLayerElement::eByControlPoint:
					{
						return layer_elements->GetDirectArray().GetAt(vidx);
					}
					case FbxLayerElement::eByPolygonVertex:
					{
						return layer_elements->GetDirectArray().GetAt(iidx);
					}
					case FbxLayerElement::eByPolygon:
					{
						return layer_elements->GetDirectArray().GetAt(fidx);
					}
					case FbxLayerElement::eByEdge:
					{
						throw std::runtime_error("ByEdge mapping not implemented");
					}
					case FbxLayerElement::eAllSame:
					{
						return layer_elements->GetDirectArray().GetAt(0);
					}
					default:
					{
						throw std::runtime_error("Unsupported mapping mode");
					}
				}
			}
			default:
			{
				throw std::runtime_error(std::format("Reference mode {} not implemented", int(layer_elements->GetReferenceMode())));
			}
		}
	}
	
	// Get the hierarchy address of 'node'
	static std::string Address(FbxNode const* node)
	{
		if (node == nullptr) return "";
		std::string address = Address(node->GetParent());
		return std::move(address.append(address.empty() ? "" : ".").append(node->GetName()));
	}

	// Read properties from the scene
	static SceneProps ReadProps(FbxScene const& scene)
	{
		SceneProps props = {};
		props.m_animation_stack_count = scene.GetSrcObjectCount<FbxAnimStack>();
		scene.GetGlobalSettings();
		return props;
	}

	// Read the geometry from the scene
	static void ReadModel(FbxScene& scene, IModelOut& out, ReadModelOptions const& options)
	{
		// Read an fbx model
		struct Reader
		{
			// Notes:
			//  - The FBX file contains collections of object types (meshes, materials, animations, etc.)
			//    and a hierarchical node tree (scene graph) where each node may reference one or more attributes.
			//  - Nodes represent transform hierarchies; attributes define what the node *is* (e.g., mesh, light).
			//  - There are two main ways to read data:
			//      - Iterate all objects of a specific type (e.g., GetSrcObject<FbxMesh>)
			//      - Recursively traverse the node hierarchy from GetRootNode()
			//  - Animation data is stored in FbxAnimStacks (takes), each containing one or more FbxAnimLayers.
			//      - Curves are attached to animatable properties and can be retrieved per layer.
			//  - Relationships like materials and textures are represented via FBX connections, not ownership.
			//  - Units, axis orientation, and coordinate systems should be checked via GlobalSettings.
			//  - Vertex data is organized into layers, each with mapping and reference modes that define indexing.

			using VertexLookup = std::vector<int>;

			FbxScene& m_scene;
			IModelOut& m_out;
			ReadModelOptions const& m_opts;
			Mesh m_mesh;
			Skeleton m_skel;
			Skinning m_skin;
			VertexLookup m_vlookup;
			int m_root_level;

			Reader(FbxScene& scene, IModelOut& out, ReadModelOptions const& opts)
				: m_scene(scene)
				, m_out(out)
				, m_opts(opts)
				, m_mesh()
				, m_skel()
				, m_vlookup()
				, m_root_level()
			{
				//// Set the animation to use
				//int animation_count = m_scene.GetSrcObjectCount<FbxAnimStack>();
				//if (m_opts.m_anim >= 0 && m_opts.m_anim < animation_count)
				//{
				//	auto* anim_stack = Check(m_scene.GetSrcObject<FbxAnimStack>(m_opts.m_anim), "Requested animation stack does not exist");
				//	m_scene.SetCurrentAnimationStack(anim_stack);
				//}
			}

			// Read the model
			void Do()
			{
				if (AllSet(m_opts.m_parts, EParts::Materials))
					ReadMaterials();
				if (AllSet(m_opts.m_parts, EParts::Meshes))
					ReadGeometry(*m_scene.GetRootNode());
				if (AllSet(m_opts.m_parts, EParts::Skeleton))
					ReadSkeleton(*m_scene.GetRootNode());
				if (AllSet(m_opts.m_parts, EParts::Skinning))
					ReadSkinning(*m_scene.GetRootNode());
			}

			// Read the materials
			void ReadMaterials()
			{
				// Add a default material for unknown materials
				m_out.AddMaterial(0, {});

				// Output each material in the scene
				for (int m = 0, mend = m_scene.GetMaterialCount(); m != mend; ++m)
				{
					Material material = {};

					auto const& mat = *m_scene.GetMaterial(m);
					if (mat.GetClassId().Is(FbxSurfacePhong::ClassId))
					{
						auto const& phong = static_cast<FbxSurfacePhong const&>(mat);
						material.m_ambient = To<Colour>(phong.Ambient.Get());
						material.m_diffuse = To<Colour>(phong.Diffuse.Get());
						material.m_specular = To<Colour>(phong.Specular.Get());
	#if 0
						phong.AmbientFactor;
						phong.Bump;
						phong.BumpFactor;
						phong.DiffuseFactor;
						phong.SpecularFactor;
						phong.Emissive;
						phong.EmissiveFactor;
						phong.DisplacementColor;
						phong.DisplacementFactor;
						phong.Reflection;
						phong.ReflectionFactor;
						phong.NormalMap;
						phong.Shininess;
						phong.TransparentColor;
						phong.TransparencyFactor;
						phong.MultiLayer;
						phong.VectorDisplacementColor;
						phong.VectorDisplacementFactor;
	#endif
					}
					else if (mat.GetClassId().Is(FbxSurfaceLambert::ClassId))
					{
						auto const& lambert = static_cast<FbxSurfaceLambert const&>(mat);
						material.m_ambient = To<Colour>(lambert.Ambient.Get());
						material.m_diffuse = To<Colour>(lambert.Diffuse.Get());
						//material.m_specular = To<Colour>(lambert.Specular.Get());
					}
					else
					{
						if (auto prop = mat.FindProperty(FbxSurfaceMaterial::sAmbient); prop.IsValid())
							material.m_ambient = To<Colour>(prop.Get<FbxDouble3>());
						if (auto prop = mat.FindProperty(FbxSurfaceMaterial::sDiffuse); prop.IsValid())
							material.m_diffuse = To<Colour>(prop.Get<FbxDouble3>());
						if (auto prop = mat.FindProperty(FbxSurfaceMaterial::sSpecular); prop.IsValid())
							material.m_specular = To<Colour>(prop.Get<FbxDouble3>());
					}

					// Look for a diffuse texture
					if (auto prop = mat.FindProperty(FbxSurfaceMaterial::sDiffuse); prop.IsValid())
					{
						for (int t = 0, tend = prop.GetSrcObjectCount<FbxTexture>(); t != tend; ++t)
						{
							if (auto const* texture = prop.GetSrcObject<FbxTexture>(t))
							{
								if (auto const* file_texture = FbxCast<FbxFileTexture>(texture))
								{
									material.m_tex_diff = file_texture->GetFileName();
								}
							}
						}
					}

					m_out.AddMaterial(mat.GetUniqueID(), material);
				}
			}

			// Parse the FBX file
			void ReadGeometry(FbxNode& node, int* root_level = nullptr, int level = 0)
			{
				auto is_mesh_root = root_level == nullptr;

				// Process all attributes of the node
				for (int i = 0, iend = node.GetNodeAttributeCount(); i != iend; ++i)
				{
					// Look for mesh attributes
					auto& attr = *node.GetNodeAttributeByIndex(i);
					if (attr.GetAttributeType() != FbxNodeAttribute::eMesh)
						continue;

					// Populate 'm_mesh' from the triangulated mesh
					auto& trimesh = *EnsureTriangulated(FbxCast<FbxMesh>(&attr));
					{
						// Can't just output verts directly because each vert can have multiple normals
						// "Inflate" the verts into a unique list of each required combination
						auto vcount = trimesh.GetControlPointsCount();
						auto fcount = trimesh.GetPolygonCount();
						auto ncount = trimesh.GetElementMaterialCount();
						auto const* verts = trimesh.GetControlPoints();
						auto const* layer0 = trimesh.GetLayer(0);
						auto const* materials = layer0->GetMaterials();
						auto const* colours = layer0->GetVertexColors();
						auto const* normals = layer0->GetNormals();
						auto const* uvs = layer0->GetUVs(FbxLayerElement::eTextureDiffuse);

						// Initialise buffers
						m_mesh.reset(trimesh.GetUniqueID());
						m_mesh.m_vbuf.reserve(vcount * 3 / 2);
						m_mesh.m_ibuf.reserve(fcount * 3);
						m_mesh.m_nbuf.reserve(ncount);
						m_vlookup.reserve(vcount * 3 / 2);
						m_vlookup.resize(0);

						if (is_mesh_root)
						{
							root_level = &m_root_level;
							m_root_level = level;
						}

						// Add a vertex to 'm_vbuf' and return its index
						auto AddVert = [this](int src_vidx, v4 const& pos, Colour const& col, v4 const& norm, v2 const& uv) -> int
						{
							Vert v = {
								.m_vert = pos,
								.m_colr = col,
								.m_norm = norm,
								.m_tex0 = uv,
								.m_idx0 = {src_vidx, 0},
							};

							// Vlookup is a linked list of vertices that are permutations of 'src_idx'
							for (int vidx = src_vidx;;)
							{
								// If 'vidx' is outside the buffer, add it
								auto vbuf_count = isize(m_mesh.m_vbuf);
								if (vidx >= vbuf_count)
								{
									m_mesh.m_vbuf.resize(std::max(vbuf_count, vidx + 1), NoVert);
									m_vlookup.resize(std::max(vbuf_count, vidx + 1), NoIndex);
									m_mesh.m_vbuf[vidx] = v;
									m_vlookup[vidx] = NoIndex;
									return vidx;
								}

								// If 'v' is already in the buffer, use it's index
								if (m_mesh.m_vbuf[vidx] == v)
								{
									return vidx;
								}

								// If the position 'vidx' is an unused slot, use it
								if (m_mesh.m_vbuf[vidx] == NoVert)
								{
									m_mesh.m_vbuf[vidx] = v;
									return vidx;
								}

								// If there is no "next", prepare to insert it at the end
								if (m_vlookup[vidx] == NoIndex)
								{
									m_vlookup[vidx] = vbuf_count;
								}

								// Go to the next vertex to check
								vidx = m_vlookup[vidx];
							}
						};

						// Get or add a nugget
						auto GetOrAddNugget = [this](uint64_t mat_id) -> Nugget&
						{
							for (auto& n : m_mesh.m_nbuf)
							{
								if (n.m_mat_id != mat_id) continue;
								return n;
							}
							m_mesh.m_nbuf.push_back(Nugget{ .m_mat_id = mat_id });
							return m_mesh.m_nbuf.back();
						};

						// Read the faces of 'mesh' adding them to a nugget based on their material
						for (int f = 0, fend = trimesh.GetPolygonCount(); f != fend; ++f)
						{
							if (trimesh.GetPolygonSize(f) != 3)
								throw std::runtime_error(std::format("Mesh {} has a polygon with {} vertices, but only triangles are supported", trimesh.GetName(), trimesh.GetPolygonSize(f)));

							// Get the material used on this face
							uint64_t mat_id = 0;
							if (materials != nullptr)
							{
								auto mat = GetLayerElement<FbxSurfaceMaterial*>(materials, f, -1, -1);
								mat_id = mat ? mat->GetUniqueID() : 0;
							}

							// Add the triangle to the nugget associated with the material
							auto& nugget = GetOrAddNugget(mat_id);
							for (int j = 0, jend = 3; j != jend; ++j)
							{
								auto iidx = f * 3 + j;
								auto vidx = trimesh.GetPolygonVertex(f, j);
								v4 pos = To<v4>(verts[vidx]).w1();

								// Get the vertex colour
								Colour col = ColourWhite;
								if (colours != nullptr)
								{
									nugget.m_geom |= EGeom::Colr;
									col = To<Colour>(GetLayerElement<FbxColor>(colours, f, iidx, vidx));
								}

								// Get the vertex normal
								v4 norm = {};
								if (normals != nullptr)
								{
									nugget.m_geom |= EGeom::Norm;
									norm = To<v4>(GetLayerElement<FbxVector4>(normals, f, iidx, vidx)).w0();
								}

								// Get the vertex uv
								v2 uv = {};
								if (uvs != nullptr)
								{
									nugget.m_geom |= EGeom::Tex0;
									uv = To<v2>(GetLayerElement<FbxVector2>(uvs, f, iidx, vidx));
								}

								// Add the vertex to the vertex buffer and it's index to the index buffer
								vidx = AddVert(vidx, pos, col, norm, uv);
								m_mesh.m_ibuf.push_back(vidx);

								nugget.m_vrange.grow(vidx);
								nugget.m_irange.grow(isize(m_mesh.m_ibuf) - 1);
							}
						}
					}

					// Determine the object to parent transforms
					auto o2p = To<m4x4>(node.EvaluateLocalTransform());

					// Output the mesh
					m_mesh.m_name = node.GetName();
					m_mesh.m_bbox = BoundingBox(trimesh);
					m_out.AddMesh(m_mesh, o2p, level - m_root_level);
				}

				// Recurse
				for (int i = 0; i != node.GetChildCount(); ++i)
					ReadGeometry(*node.GetChild(i), root_level, level + 1);
			}

			// Recursively read skeletons
			void ReadSkeleton(FbxNode& node, Skeleton* skel = nullptr, int level = 0)
			{
				// If no skeleton is provided, then this could be the root bone
				auto is_skel_root = skel == nullptr;

				// Process all attributes of the node
				for (int i = 0, iend = node.GetNodeAttributeCount(); i != iend; ++i)
				{
					// Look for skeleton attributes
					auto const& attr = *node.GetNodeAttributeByIndex(i);
					if (attr.GetAttributeType() != FbxNodeAttribute::eSkeleton)
						continue;

					auto const& skeleton = *FbxCast<FbxSkeleton>(&attr);

					// Determine the object to world and object to parent transforms
					auto o2p = To<m4x4>(node.EvaluateLocalTransform());

					// Reset the skeleton instance for the root bone
					if (is_skel_root)
					{
						m_skel.reset(skeleton.GetUniqueID());
						skel = &m_skel;
						m_root_level = level;
					}

					// Add the bone to the skeleton
					skel->m_names.push_back(skeleton.GetNode()->GetName());
					skel->m_types.push_back(To<EBoneType>(skeleton.GetSkeletonType()));
					skel->m_levels.push_back(level - m_root_level);
					skel->m_b2p.push_back(o2p);
				}

				// Recurse
				for (int i = 0; i != node.GetChildCount(); ++i)
					ReadSkeleton(*node.GetChild(i), skel, level + 1);

				// Output the skeleton if this is the root node
				if (is_skel_root && skel != nullptr)
				{
					m_out.AddSkeleton(*skel);
				}
			}

			// Read skinning information from mesh nodes in 'node'
			void ReadSkinning(FbxNode& node, int level = 0)
			{
				// Process all attributes of the node
				for (int i = 0, iend = node.GetNodeAttributeCount(); i != iend; ++i)
				{
					// Look for mesh attributes
					auto& attr = *node.GetNodeAttributeByIndex(i);
					if (attr.GetAttributeType() != FbxNodeAttribute::eMesh)
						continue;

					// Ignore meshes without skins
					auto& mesh = *FbxCast<FbxMesh>(&attr);
					if (mesh.GetDeformerCount(FbxDeformer::eSkin) == 0)
						continue;

					// The id of this mesh
					auto mesh_id = mesh.GetUniqueID();

					// Find the skeleton associated with this skinned mesh
					auto skel_id = FindSkeletonId(mesh);

					// Create a new skin
					m_skin.reset(mesh_id, skel_id);
					m_skin.m_verts.resize(mesh.GetControlPointsCount());
					static auto NextZero = [](v4 const& v) -> int
					{
						return int(v.x != 0) + int(v.y != 0) + int(v.z != 0) + int(v.w != 0);
					};

					// Get the skinning data for this mesh
					for (int d = 0, dend = mesh.GetDeformerCount(FbxDeformer::eSkin); d != dend; ++d)
					{
						auto& skin = *FbxCast<FbxSkin>(mesh.GetDeformer(d, FbxDeformer::eSkin));
						for (int b = 0, bend = skin.GetClusterCount(); b != bend; ++b)
						{
							auto& cluster = *skin.GetCluster(b);
							auto& bone = *cluster.GetLink();

							// Find the bone in the skeleton
							auto bone_index = static_cast<int>(std::distance(begin(m_skel.m_names), std::find(begin(m_skel.m_names), end(m_skel.m_names), bone.GetName())));
							if (bone_index >= isize(m_skel.m_b2p))
								throw std::runtime_error("Bone index out of range in skeleton");

							// Get the span of vert indices and weights
							auto indices = std::span<int>{ cluster.GetControlPointIndices(), static_cast<size_t>(cluster.GetControlPointIndicesCount()) };
							auto weights = std::span<double>{ cluster.GetControlPointWeights(), static_cast<size_t>(cluster.GetControlPointIndicesCount()) };
							for (int w = 0, wend = cluster.GetControlPointIndicesCount(); w != wend; ++w)
							{
								auto vidx = indices[w];
								auto weight = weights[w];

								auto k = NextZero(m_skin.m_verts[vidx].m_weights);
								if (k >= 4)
									throw std::runtime_error("Too many bone influences");

								m_skin.m_verts[vidx].m_bones[k] = bone_index;
								m_skin.m_verts[vidx].m_weights[k] = static_cast<float>(weight);
							}
						}
					}

					// Output the skinning data for the mesh
					m_out.AddSkinning(m_skin);
				}

				// Recurse
				for (int i = 0; i != node.GetChildCount(); ++i)
					ReadSkinning(*node.GetChild(i), level + 1);
			}

			// Find the unique id of the root bone of the skeleton
			uint64_t FindSkeletonId(FbxMesh const& mesh) const
			{
				for (int d = 0, dend = mesh.GetDeformerCount(FbxDeformer::eSkin); d != dend; ++d)
				{
					auto& skin = *FbxCast<FbxSkin>(mesh.GetDeformer(d, FbxDeformer::eSkin));
					for (int c = 0, cend = skin.GetClusterCount(); c != cend; ++c)
					{
						auto& cluster = *skin.GetCluster(c);
						auto const* bone = cluster.GetLink();

						// Find the bone with no parent and assume that is the root
						FbxSkeleton const* root_bone = nullptr;
						for (; bone != nullptr; bone = bone->GetParent())
						{
							// Look for skeleton attributes
							auto const* attr = Find(FbxNodeAttribute::eSkeleton, *bone);
							if (attr == nullptr)
								break;

							// Note: GetSkeletonType() is not always set. Just find the 
							// top of the hierarchy and assume that is the root
							root_bone = FbxCast<FbxSkeleton>(attr);
						}

						// If we found the root of the skeleton, return it's id
						if (root_bone != nullptr)
							return root_bone->GetUniqueID();
					}
				}
				return 0;
			}

			// Find the next node attribute of the given type in 'node'
			FbxNodeAttribute const* Find(FbxNodeAttribute::EType attr_type, FbxNode const& node, int start = 0) const
			{
				for (int i = start, iend = node.GetNodeAttributeCount(); i < iend; ++i)
				{
					auto const& attr = *node.GetNodeAttributeByIndex(i);
					if (attr.GetAttributeType() == attr_type)
						return &attr;
				}
				return nullptr;
			}

			// Ensure the geometry in 'mesh' is triangles not polygons
			FbxMesh* EnsureTriangulated(FbxMesh* mesh)
			{
				// Ensure the mesh is triangles
				if (mesh->IsTriangleMesh())
					return mesh;

				// Must do this before triangulating the mesh due to an FBX bug in Triangulate.
				// Edge hardnees triangulation give wrong edge hardness so we convert them to a smooth group during the triangulation.
				FbxGeometryConverter converter(m_scene.GetFbxManager());
				for (int j = 0, jend = mesh->GetLayerCount(FbxLayerElement::eSmoothing); j != jend; ++j)
				{
					auto* smoothing = mesh->GetLayer(j)->GetSmoothing();
					if (smoothing && smoothing->GetMappingMode() != FbxLayerElement::eByPolygon)
						converter.ComputePolygonSmoothingFromEdgeSmoothing(mesh, j);
				}

				// Convert polygons to triangles
				mesh = FbxCast<FbxMesh>(converter.Triangulate(mesh, true));

				// If triangulation failed, ignore the mesh
				if (!mesh->IsTriangleMesh())
					throw std::runtime_error(std::format("Failed to convert mesh '{}' to a triangle mesh", mesh->GetName()));

				return mesh;
			}

			// Get the mesh bounding box
			BBox BoundingBox(FbxMesh& mesh)
			{
				mesh.ComputeBBox();
				auto min = To<v4>(mesh.BBoxMin.Get());
				auto max = To<v4>(mesh.BBoxMax.Get());
				return BBox{ (max + min) * 0.5f, (max - min) * 0.5f };
			}
		};

		Reader reader(scene, out, options);
		reader.Do();
	}

	// Dump diagnostic info for a scene
	static void DumpScene(FbxScene const& scene, std::ostream& out)
	{
		struct Writer
		{
			std::ostream& m_out;
			Writer(std::ostream& out)
				: m_out(out)
			{}

			// Indent helper
			static std::string_view Indent(int amount)
			{
				constexpr static char const space[] = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
				constexpr static int len = (int)_countof(space);
				return std::string_view(space, amount < len ? amount : len);
			}

			void Write(FbxScene const& scene, int indent = 0)
			{
				m_out << Indent(indent) << "Scene: " << scene.GetName() << "\n";
				WriteMetaData(*const_cast<FbxScene&>(scene).GetSceneInfo(), indent + 1);
				WriteGlobalSettings(scene.GetGlobalSettings(), indent + 1);
				WriteHierarchy(*scene.GetRootNode(), indent + 1);
				WriteContent(*scene.GetRootNode(), indent + 1);
				WritePose(scene, indent + 1);
				WriteAnimation(scene, indent + 1);
				WriteGenericInfo(scene, indent + 1);
			}
			void WriteMetaData(FbxDocumentInfo const& scene_info, int indent)
			{
				m_out << Indent(indent) << "Meta-Data:\n";
				++indent;

				m_out << Indent(indent) << "Title: " << scene_info.mTitle.Buffer() << "\n";
				m_out << Indent(indent) << "Subject: " << scene_info.mSubject.Buffer() << "\n";
				m_out << Indent(indent) << "Author: " << scene_info.mAuthor.Buffer() << "\n";
				m_out << Indent(indent) << "Keywords: " << scene_info.mKeywords.Buffer() << "\n";
				m_out << Indent(indent) << "Revision: " << scene_info.mRevision.Buffer() << "\n";
				m_out << Indent(indent) << "Comment: " << scene_info.mComment.Buffer() << "\n";

				FbxThumbnail const* thumbnail = const_cast<FbxDocumentInfo&>(scene_info).GetSceneThumbnail();
				if (thumbnail)
				{
					m_out << Indent(indent) << "Thumbnail:\n";
					switch (thumbnail->GetDataFormat())
					{
						case FbxThumbnail::eRGB_24: m_out << Indent(indent) << "Format: RGB\n"; break;
						case FbxThumbnail::eRGBA_32: m_out << Indent(indent) << "Format: RGBA\n"; break;
						default: m_out << Indent(indent) << "Format: UNKNOWN\n"; break;
					}
					switch (thumbnail->GetSize())
					{
						case FbxThumbnail::eNotSet: m_out << Indent(indent) << "Size: no dimensions specified (" << thumbnail->GetSizeInBytes() << " bytes)\n"; break;
						case FbxThumbnail::e64x64: m_out << Indent(indent) << "Size: 64 x 64 pixels (" << thumbnail->GetSizeInBytes() << " bytes)\n"; break;
						case FbxThumbnail::e128x128: m_out << Indent(indent) << "Size: 128 x 128 pixels (" << thumbnail->GetSizeInBytes() << " bytes)\n"; break;
						default: m_out << Indent(indent) << "Size: UNKNOWN\n"; break;
					}
				}
			}
			void WriteGlobalSettings(FbxGlobalSettings const& settings, int indent)
			{
				m_out << Indent(indent) << "Global Settings:\n";
				++indent;

				m_out << Indent(indent) << "Global Light Settings:\n";
				m_out << Indent(indent) << "Ambient Color: " << settings.GetAmbientColor() << "\n";
				m_out << Indent(indent) << "Global Camera Settings:\n";
				m_out << Indent(indent) << "Default Camera: " << settings.GetDefaultCamera() << "\n";
				m_out << Indent(indent) << "Global Time Settings:\n";
				m_out << Indent(indent) << "Time Mode: " << (int)settings.GetTimeMode() << "\n";

				FbxTimeSpan lTs; char time_string[256];
				settings.GetTimelineDefaultTimeSpan(lTs);
				m_out << Indent(indent) << "Time-line default timespan:\n";
				m_out << Indent(indent) << "Start: " << lTs.GetStart().GetTimeString(&time_string[0], _countof(time_string)) << "\n";
				m_out << Indent(indent) << "Stop: " << lTs.GetStop().GetTimeString(&time_string[0], _countof(time_string)) << "\n";
			}
			void WriteHierarchy(FbxNode const& node, int indent)
			{
				m_out << Indent(indent) << "Hierarchy:\n";

				struct L
				{
					static void Do(std::ostream& out, FbxNode const& node, int indent)
					{
						out << Indent(indent) << node.GetName() << "\n";
						for (int i = 0, iend = node.GetChildCount(); i != iend; ++i)
							Do(out, *node.GetChild(i), indent + 1);
					}
				};
				L::Do(m_out, node, indent+1);
			}
			void WriteContent(FbxNode const& node, int indent)
			{
				auto attr = node.GetNodeAttribute();
				auto attr_type = attr ? attr->GetAttributeType() : FbxNodeAttribute::EType::eUnknown;
				m_out << Indent(indent) << "Node(" << attr_type << ") " << node.GetName() << "\n";

				// Node properties
				m_out << Indent(indent + 1) << "Properties:\n";
				{
					WriteUserProperties(node, indent + 2);
					WriteTarget(node, indent + 2);
					WritePivotsAndLimits(node, indent + 2);
					WriteTransformPropagation(node, indent + 2);
					WriteGeometricTransform(node, indent + 2);
				}

				// Node specific data
				switch (attr_type)
				{
					case FbxNodeAttribute::eUnknown:
					case FbxNodeAttribute::eNull:
					{
						break;
					}
					case FbxNodeAttribute::eMarker:
					{
						WriteMarker(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::eSkeleton:
					{
						WriteSkeleton(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::eMesh:
					{
						WriteMesh(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::eNurbs:
					{
						WriteNurb(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::ePatch:
					{
						WritePatch(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::eCamera:
					{
						WriteCamera(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::eLight:
					{
						WriteLight(node, indent + 1);
						break;
					}
					case FbxNodeAttribute::eLODGroup:
					{
						WriteLodGroup(node, indent + 1);
						break;
					}
					default:
					{
						m_out << Indent(indent + 1) << "Not Implemented\n";
						break;
					}
				}

				// Recurse
				for (int i = 0, iend = node.GetChildCount(); i != iend; ++i)
					WriteContent(*node.GetChild(i), indent + 1);
			}
			void WriteMarker(FbxNode const& node, int indent)
			{
				auto const& marker = *static_cast<FbxMarker const*>(node.GetNodeAttribute());

				m_out << Indent(indent) << "Marker Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(marker, indent + 1);

				// Type
				m_out << Indent(indent) << "Marker Type: ";
				switch (marker.GetType())
				{
					case FbxMarker::eStandard:   m_out << "Standard\n";    break;
					case FbxMarker::eOptical:    m_out << "Optical\n";     break;
					case FbxMarker::eEffectorIK: m_out << "IK Effector\n"; break;
					case FbxMarker::eEffectorFK: m_out << "FK Effector\n"; break;
				}

				// Look
				m_out << Indent(indent) << "Marker Look: ";
				switch (marker.Look.Get())
				{
					default: break;
					case FbxMarker::eCube:        m_out << "Cube\n";        break;
					case FbxMarker::eHardCross:   m_out << "Hard Cross\n";  break;
					case FbxMarker::eLightCross:  m_out << "Light Cross\n"; break;
					case FbxMarker::eSphere:      m_out << "Sphere\n";      break;
				}

				// Size
				m_out << Indent(indent) << "Size: " << marker.Size.Get() << "\n";

				// Color
				m_out << Indent(indent) << "Color: " << marker.Color.Get() << "\n";

				// IKPivot
				m_out << Indent(indent) << "IKPivot: " << marker.IKPivot.Get() << "\n";
			}
			void WriteSkeleton(FbxNode const& node, int indent)
			{
				auto const& skel = *static_cast<FbxSkeleton const*>(node.GetNodeAttribute());

				m_out << Indent(indent) << "Skeleton Name: " << skel.GetName() << "\n";
				WriteMetaDataConnections(skel, indent+1);

				char const* skel_types[] = { "Root", "Limb", "Limb Node", "Effector" };
				m_out << Indent(indent) << "Type: " << skel_types[skel.GetSkeletonType()] << "\n";

				switch (skel.GetSkeletonType())
				{
					case FbxSkeleton::eRoot:
					{
						m_out << Indent(indent) << "Limb Root Size: " << FloatClamp(skel.Size.Get()) << "\n";
						break;
					}
					case FbxSkeleton::eLimb:
					{
						m_out << Indent(indent) << "Limb Length: " << FloatClamp(skel.LimbLength.Get()) << "\n";
						break;
					}
					case FbxSkeleton::eLimbNode:
					{
						m_out << Indent(indent) << "Limb Node Size: " << FloatClamp(skel.Size.Get()) << "\n";
						break;
					}
					default:
					{
						m_out << Indent(indent) << "Unsupported\n";
						break;
					}
				}

				m_out << Indent(indent) << "Color: " << skel.GetLimbNodeColor() << "\n";
			}
			void WriteMesh(FbxNode const& node, int indent)
			{
				auto const& lMesh = *static_cast<FbxMesh const*>(node.GetNodeAttribute());

				m_out << Indent(indent) << "Mesh Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(lMesh, indent+1);
				WriteControlsPoints(lMesh, indent+1);
				WritePolygons(lMesh, indent+1);
				WriteMaterialMapping(lMesh, indent+1);
				WriteMaterials(lMesh, indent+1);
				WriteTexture(lMesh, indent+1);
				WriteMaterialConnections(lMesh, indent+1);
				WriteLink(lMesh, indent+1);
				WriteShape(lMesh, indent+1);
				WriteCache(lMesh, indent+1);
			}
			void WriteControlsPoints(FbxMesh const& mesh, int indent)
			{
				m_out << Indent(indent) << "Control Points:" << "\n";
				++indent;

				FbxVector4* lControlPoints = mesh.GetControlPoints();
				for (int i = 0, iend = mesh.GetControlPointsCount(); i != iend; ++i)
				{
					m_out << Indent(indent) << "[" << i << "] " << lControlPoints[i];
					for (int j = 0, jend = mesh.GetElementNormalCount(); j != jend; ++j)
					{
						FbxGeometryElementNormal const* leNormals = mesh.GetElementNormal(j);
						if (leNormals->GetMappingMode() == FbxGeometryElement::eByControlPoint &&
							leNormals->GetReferenceMode() == FbxGeometryElement::eDirect)
						{
							m_out << (j == 0 ? " Normals: " : ", ") << leNormals->GetDirectArray().GetAt(i);
						}
					}
					m_out << "\n";
				}
			}
			void WritePolygons(FbxMesh const& mesh, int indent)
			{
				m_out << Indent(indent) << "Polygons:\n";

				int vertexId = 0;
				for (int i = 0, iend = mesh.GetPolygonCount(); i != iend; ++i)
				{
					m_out << Indent(indent + 1) << "Polygon: " << i << "\n";
					for (int j = 0, jend = mesh.GetPolygonSize(i); j != jend; ++j)
					{
						auto lControlPointIndex = mesh.GetPolygonVertex(i, j);
						m_out << Indent(indent + 2) << "Index=" << lControlPointIndex;
						for (int l = 0, lend = mesh.GetElementVertexColorCount(); l != lend; ++l)
						{
							l == 0 ? m_out << " Color=" : m_out;

							FbxGeometryElementVertexColor const* leVtxc = mesh.GetElementVertexColor(l);
							switch (leVtxc->GetMappingMode())
							{
								case FbxGeometryElement::eByControlPoint:
								{
									switch (leVtxc->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										{
											m_out << leVtxc->GetDirectArray().GetAt(lControlPointIndex);
											break;
										}
										case FbxGeometryElement::eIndexToDirect:
										{
											int id = leVtxc->GetIndexArray().GetAt(lControlPointIndex);
											m_out << leVtxc->GetDirectArray().GetAt(id);
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								case FbxGeometryElement::eByPolygonVertex:
								{
									switch (leVtxc->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										{
											m_out << leVtxc->GetDirectArray().GetAt(vertexId);
											break;
										}
										case FbxGeometryElement::eIndexToDirect:
										{
											int id = leVtxc->GetIndexArray().GetAt(vertexId);
											m_out << leVtxc->GetDirectArray().GetAt(id);
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								default:
								{
									m_out << "unsupported";
									break;
								}
							}
						}
						for (int l = 0, lend = mesh.GetElementUVCount(); l != lend; ++l)
						{
							l == 0 ? m_out << " UV=" : m_out;

							FbxGeometryElementUV const* leUV = mesh.GetElementUV(l);
							switch (leUV->GetMappingMode())
							{
								case FbxGeometryElement::eByControlPoint:
								{
									switch (leUV->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										{
											m_out << leUV->GetDirectArray().GetAt(lControlPointIndex);
											break;
										}
										case FbxGeometryElement::eIndexToDirect:
										{
											int id = leUV->GetIndexArray().GetAt(lControlPointIndex);
											m_out << leUV->GetDirectArray().GetAt(id);
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								case FbxGeometryElement::eByPolygonVertex:
								{
									switch (leUV->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										case FbxGeometryElement::eIndexToDirect:
										{
											m_out << leUV->GetDirectArray().GetAt(const_cast<FbxMesh&>(mesh).GetTextureUVIndex(i, j));
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								default:
								{
									m_out << "unsupported";
									break;
								}
							}
						}
						for (int l = 0, lend = mesh.GetElementNormalCount(); l != lend; ++l)
						{
							l == 0 ? m_out << " Normal=" : m_out;

							FbxGeometryElementNormal const* leNormal = mesh.GetElementNormal(l);
							switch (leNormal->GetMappingMode())
							{
								case FbxGeometryElement::eByPolygonVertex:
								{
									switch (leNormal->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										{
											m_out << leNormal->GetDirectArray().GetAt(vertexId);
											break;
										}
										case FbxGeometryElement::eIndexToDirect:
										{
											int id = leNormal->GetIndexArray().GetAt(vertexId);
											m_out << leNormal->GetDirectArray().GetAt(id);
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								default:
								{
									m_out << "unsupported";
									break;
								}
							}
						}
						for (int l = 0, lend = mesh.GetElementTangentCount(); l != lend; ++l)
						{
							l == 0 ? m_out << " Tangent=" : m_out;

							FbxGeometryElementTangent const* leTangent = mesh.GetElementTangent(l);
							switch (leTangent->GetMappingMode())
							{
								case FbxGeometryElement::eByPolygonVertex:
								{
									switch (leTangent->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										{
											m_out << leTangent->GetDirectArray().GetAt(vertexId);
											break;
										}
										case FbxGeometryElement::eIndexToDirect:
										{
											int id = leTangent->GetIndexArray().GetAt(vertexId);
											m_out << leTangent->GetDirectArray().GetAt(id);
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								default:
								{
									m_out << "unsupported";
									break;
								}
							}
						}
						for (int l = 0, lend = mesh.GetElementBinormalCount(); l != lend; ++l)
						{
							l == 0 ? m_out << " Binormal=" : m_out;

							FbxGeometryElementBinormal const* leBinormal = mesh.GetElementBinormal(l);
							switch (leBinormal->GetMappingMode())
							{
								case FbxGeometryElement::eByPolygonVertex:
								{
									switch (leBinormal->GetReferenceMode())
									{
										case FbxGeometryElement::eDirect:
										{
											m_out << leBinormal->GetDirectArray().GetAt(vertexId);
											break;
										}
										case FbxGeometryElement::eIndexToDirect:
										{
											int id = leBinormal->GetIndexArray().GetAt(vertexId);
											m_out << leBinormal->GetDirectArray().GetAt(id);
											break;
										}
										default:
										{
											m_out << "unsupported";
											break;
										}
									}
									break;
								}
								default:
								{
									m_out << "unsupported";
									break;
								}
							}
						}
						m_out << "\n";
						vertexId++;
					}
					for (int l = 0, lend = mesh.GetElementPolygonGroupCount(); l != lend; ++l)
					{
						FbxGeometryElementPolygonGroup const* lePolgrp = mesh.GetElementPolygonGroup(l);
						switch (lePolgrp->GetMappingMode())
						{
							case FbxGeometryElement::eByPolygon:
							{
								if (lePolgrp->GetReferenceMode() == FbxGeometryElement::eIndex)
									m_out << Indent(indent + 2) << "Assigned to group: " << lePolgrp->GetIndexArray().GetAt(i) << "\n";
								break;
							}
							default:
							{
								// any other mapping modes don't make sense
								m_out << Indent(indent + 2) << "unsupported group assignment" << "\n";
								break;
							}
						}
					}
				}

				// Check visibility for the edges of the mesh
				for (int l = 0, lend = mesh.GetElementVisibilityCount(); l != lend; ++l)
				{
					l == 0 ? m_out << Indent(indent + 1) << "Edge Visibility:\n" : m_out;

					FbxGeometryElementVisibility const* leVisibility = mesh.GetElementVisibility(l);
					switch (leVisibility->GetMappingMode())
					{
						case FbxGeometryElement::eByEdge:
						{
							//should be eDirect
							for (int j = 0, jend = mesh.GetMeshEdgeCount(); j != jend; ++j)
								m_out << Indent(indent + 2) << "[" << j << "] visibility: " << leVisibility->GetDirectArray().GetAt(j) << "\n";

							break;
						}
						default:
						{
							m_out << Indent(indent + 2) << "unsupported mapping mode\n";
							break;
						}
					}
				}
			}
			void WriteMaterialMapping(FbxMesh const& mesh, int indent)
			{
				const char* lMappingTypes[] = { "None", "By Control Point", "By Polygon Vertex", "By Polygon", "By Edge", "All Same" };
				const char* lReferenceMode[] = { "Direct", "Index", "Index to Direct" };

				for (int l = 0, lend = mesh.GetElementMaterialCount(); l != lend; ++l)
				{
					FbxGeometryElementMaterial const* leMat = mesh.GetElementMaterial(l);
					if (leMat)
					{
						m_out << Indent(indent) << "Material Element: " << l << "\n";
						m_out << Indent(indent) << "Mapping: " << lMappingTypes[leMat->GetMappingMode()] << "\n";
						m_out << Indent(indent) << "ReferenceMode: " << lReferenceMode[leMat->GetReferenceMode()] << "\n";

						int lMaterialCount = 0;
						if (leMat->GetReferenceMode() == FbxGeometryElement::eDirect ||
							leMat->GetReferenceMode() == FbxGeometryElement::eIndexToDirect)
						{
							lMaterialCount = mesh.GetNode()->GetMaterialCount();
						}

						if (leMat->GetReferenceMode() == FbxGeometryElement::eIndex ||
							leMat->GetReferenceMode() == FbxGeometryElement::eIndexToDirect)
						{
							m_out << Indent(indent) << "Indices: ";
							for (int i = 0, iend = leMat->GetIndexArray().GetCount(); i != iend; ++i)
								m_out << (i != 0 ? ", " : "") << leMat->GetIndexArray().GetAt(i);
							m_out << "\n";
						}
					}
				}
			}
			void WriteMaterials(FbxGeometry const& geometry, int indent)
			{
				m_out << Indent(indent) << "Materials:\n";
				++indent;

				static auto LookForImplementation = [](FbxSurfaceMaterial const* pMaterial) -> FbxImplementation const*
				{
					const FbxImplementation* lImplementation = nullptr;
					if (!lImplementation) lImplementation = GetImplementation(pMaterial, FBXSDK_IMPLEMENTATION_CGFX);
					if (!lImplementation) lImplementation = GetImplementation(pMaterial, FBXSDK_IMPLEMENTATION_HLSL);
					if (!lImplementation) lImplementation = GetImplementation(pMaterial, FBXSDK_IMPLEMENTATION_SFX);
					if (!lImplementation) lImplementation = GetImplementation(pMaterial, FBXSDK_IMPLEMENTATION_OGS);
					if (!lImplementation) lImplementation = GetImplementation(pMaterial, FBXSDK_IMPLEMENTATION_SSSL);
					return lImplementation;
				};
				auto WriteMaterial = [this](FbxSurfaceMaterial const& material, int indent)
				{
					m_out << Indent(indent) << "Name: \"" << material.GetName() << "\"\n";

					//Get the implementation to see if it's a hardware shader.
					const FbxImplementation* lImplementation = LookForImplementation(&material);
					if (lImplementation)
					{
						// Now we have a hardware shader, let's read it
						m_out << Indent(indent) << "Language: " << lImplementation->Language.Get().Buffer() << "\n";
						m_out << Indent(indent) << "LanguageVersion: " << lImplementation->LanguageVersion.Get().Buffer() << "\n";
						m_out << Indent(indent) << "RenderName: " << lImplementation->RenderName.Buffer() << "\n";
						m_out << Indent(indent) << "RenderAPI: " << lImplementation->RenderAPI.Get().Buffer() << "\n";
						m_out << Indent(indent) << "RenderAPIVersion: " << lImplementation->RenderAPIVersion.Get().Buffer() << "\n";

						FbxBindingTable const* lRootTable = lImplementation->GetRootTable();
						FbxString lFileName = lRootTable->DescAbsoluteURL.Get();
						FbxString lTechniqueName = lRootTable->DescTAG.Get();

						FbxBindingTable const* lTable = lImplementation->GetRootTable();
						for (int i = 0, iend = (int)lTable->GetEntryCount(); i != iend; ++i)
						{
							FbxBindingTableEntry const& lEntry = lTable->GetEntry(i);
							char const* lEntrySrcType = lEntry.GetEntryType(true);
							FbxProperty lFbxProp;

							FbxString lTest = lEntry.GetSource();
							m_out << Indent(indent) << "Entry: " << lTest.Buffer() << "\n";
							if (strcmp(FbxPropertyEntryView::sEntryType, lEntrySrcType) == 0)
							{
								lFbxProp = material.FindPropertyHierarchical(lEntry.GetSource());
								if (!lFbxProp.IsValid())
								{
									lFbxProp = material.RootProperty.FindHierarchical(lEntry.GetSource());
								}
							}
							else if (strcmp(FbxConstantEntryView::sEntryType, lEntrySrcType) == 0)
							{
								lFbxProp = lImplementation->GetConstants().FindHierarchical(lEntry.GetSource());
							}
							if (lFbxProp.IsValid())
							{
								if (lFbxProp.GetSrcObjectCount<FbxTexture>() > 0)
								{
									//do what you want with the textures
									for (int j = 0; j < lFbxProp.GetSrcObjectCount<FbxFileTexture>(); ++j)
									{
										FbxFileTexture* lTex = lFbxProp.GetSrcObject<FbxFileTexture>(j);
										m_out << Indent(indent) << "File Texture: " << lTex->GetFileName() << "\n";
									}
									for (int j = 0; j < lFbxProp.GetSrcObjectCount<FbxLayeredTexture>(); ++j)
									{
										FbxLayeredTexture* lTex = lFbxProp.GetSrcObject<FbxLayeredTexture>(j);
										m_out << Indent(indent) << "Layered Texture: " << lTex->GetName() << "\n";
									}
									for (int j = 0; j < lFbxProp.GetSrcObjectCount<FbxProceduralTexture>(); ++j)
									{
										FbxProceduralTexture* lTex = lFbxProp.GetSrcObject<FbxProceduralTexture>(j);
										m_out << Indent(indent) << "Procedural Texture: " << lTex->GetName() << "\n";
									}
								}
								else
								{
									m_out << Indent(indent) << lFbxProp << "\n";
								}
							}
						}
					}
					else if (material.GetClassId().Is(FbxSurfacePhong::ClassId))
					{
						// We found a Phong material.  Display its properties.
						auto phong = static_cast<FbxSurfacePhong const*>(&material);
						m_out << Indent(indent) << "Ambient: " << phong->Ambient << "\n";
						m_out << Indent(indent) << "Diffuse: " << phong->Diffuse << "\n";
						m_out << Indent(indent) << "Specular: " << phong->Specular << "\n";
						m_out << Indent(indent) << "Emissive: " << phong->Emissive << "\n";
						m_out << Indent(indent) << "Opacity: " << 1.0 - phong->TransparencyFactor.Get() << "\n";
						m_out << Indent(indent) << "Shininess: " << phong->Shininess.Get() << "\n";
						m_out << Indent(indent) << "Reflectivity: " << phong->ReflectionFactor.Get() << "\n";
					}
					else if (material.GetClassId().Is(FbxSurfaceLambert::ClassId))
					{
						// We found a Lambert material. Display its properties.
						auto lambert = static_cast<FbxSurfaceLambert const*>(&material);
						m_out << Indent(indent) << "Ambient: " << lambert->Ambient << "\n";
						m_out << Indent(indent) << "Diffuse: " << lambert->Diffuse << "\n";
						m_out << Indent(indent) << "Emissive: " << lambert->Emissive << "\n";
						m_out << Indent(indent) << "Opacity: " << 1.0 - lambert->TransparencyFactor.Get() << "\n";
					}
					else
					{
						m_out << Indent(indent) << "Unknown type of Material" << "\n";
					}

					m_out << Indent(indent) << "Shading Model: " << material.ShadingModel.Get().Buffer() << "\n";
				};

				for (int lCount = 0, lend = geometry.GetNode()->GetMaterialCount(); lCount != lend; ++lCount)
				{
					m_out << Indent(indent) << "Material " << lCount << "\n";
					
					FbxSurfaceMaterial const* lMaterial = geometry.GetNode()->GetMaterial(lCount);
					WriteMaterial(*lMaterial, indent + 1);
				}
			}
			void WriteTexture(FbxGeometry const& geometry, int indent)
			{
				for (int lMaterialIndex = 0, lend = geometry.GetNode()->GetSrcObjectCount<FbxSurfaceMaterial>(); lMaterialIndex != lend; ++lMaterialIndex)
				{
					FbxSurfaceMaterial const* lMaterial = geometry.GetNode()->GetSrcObject<FbxSurfaceMaterial>(lMaterialIndex);
					if (!lMaterial)
						continue;

					m_out << Indent(indent) << "Textures connected to Material " << lMaterialIndex << "\n";
					for (int lTextureIndex = 0; lTextureIndex != FbxLayerElement::sTypeTextureCount; ++lTextureIndex)
					{
						auto lProperty = lMaterial->FindProperty(FbxLayerElement::sTextureChannelNames[lTextureIndex]);
						if (!lProperty.IsValid())
							continue;

						for (int j = 0, jend = lProperty.GetSrcObjectCount<FbxTexture>(); j != jend; ++j)
						{
							// Here we have to check if it's layeredtextures, or just textures:
							FbxLayeredTexture* lLayeredTexture = lProperty.GetSrcObject<FbxLayeredTexture>(j);
							if (lLayeredTexture)
							{
								m_out << Indent(indent) << "Layered Texture: " << j << "\n";
								for (int k = 0, kend = lLayeredTexture->GetSrcObjectCount<FbxTexture>(); k != kend; ++k)
								{
									FbxTexture* lTexture = lLayeredTexture->GetSrcObject<FbxTexture>(k);
									if (!lTexture)
										continue;

									// NOTE the blend mode is ALWAYS on the LayeredTexture and NOT the one on the texture.
									// Why is that?  because one texture can be shared on different layered textures and might
									// have different blend modes.
									FbxLayeredTexture::EBlendMode lBlendMode;
									lLayeredTexture->GetTextureBlendMode(k, lBlendMode);
									m_out << Indent(indent) << "Textures for " << lProperty.GetName() << "\n";
									m_out << Indent(indent) << "Texture " << k << "\n";
									WriteTextureInfo(*lTexture, (int)lBlendMode, indent+1);
								}
							}
							else
							{
								//no layered texture simply get on the property
								FbxTexture* lTexture = lProperty.GetSrcObject<FbxTexture>(j);
								if (!lTexture)
									continue;

								m_out << Indent(indent) << "Textures for " << lProperty.GetName() << "\n";
								m_out << Indent(indent) << "Texture " << j << "\n";
								WriteTextureInfo(*lTexture, -1, indent+1);
							}
						}
					}
				}
			}
			void WriteTextureInfo(FbxTexture const& texture, int blend_mode, int indent)
			{
				m_out << Indent(indent) << "Name: \"" << texture.GetName() << "\"\n";
				if (FbxFileTexture const* lFileTexture = FbxCast<FbxFileTexture>(&texture))
				{
					m_out << Indent(indent) << "Type: File Texture\n";
					m_out << Indent(indent) << "File Name: \"" << lFileTexture->GetFileName() << "\"\n";
				}
				else if (FbxProceduralTexture const* lProceduralTexture = FbxCast<FbxProceduralTexture>(&texture))
				{
					m_out << Indent(indent) << "Type: Procedural Texture\n";
				}
				m_out << Indent(indent) << "Scale U: " << texture.GetScaleU() << "\n";
				m_out << Indent(indent) << "Scale V: " << texture.GetScaleV() << "\n";
				m_out << Indent(indent) << "Translation U: " << texture.GetTranslationU() << "\n";
				m_out << Indent(indent) << "Translation V: " << texture.GetTranslationV() << "\n";
				m_out << Indent(indent) << "Swap UV: " << texture.GetSwapUV() << "\n";
				m_out << Indent(indent) << "Rotation U: " << texture.GetRotationU() << "\n";
				m_out << Indent(indent) << "Rotation V: " << texture.GetRotationV() << "\n";
				m_out << Indent(indent) << "Rotation W: " << texture.GetRotationW() << "\n";

				char const* lAlphaSources[] = { "None", "RGB Intensity", "Black" };
				m_out << Indent(indent) << "Alpha Source: " << lAlphaSources[texture.GetAlphaSource()] << "\n";
				m_out << Indent(indent) << "Cropping Left: " << texture.GetCroppingLeft() << "\n";
				m_out << Indent(indent) << "Cropping Top: " << texture.GetCroppingTop() << "\n";
				m_out << Indent(indent) << "Cropping Right: " << texture.GetCroppingRight() << "\n";
				m_out << Indent(indent) << "Cropping Bottom: " << texture.GetCroppingBottom() << "\n";

				char const* lMappingTypes[] = { "Null", "Planar", "Spherical", "Cylindrical", "Box", "Face", "UV", "Environment" };
				m_out << Indent(indent) << "Mapping Type: " << lMappingTypes[texture.GetMappingType()] << "\n";

				if (texture.GetMappingType() == FbxTexture::ePlanar)
				{
					const char* lPlanarMappingNormals[] = { "X", "Y", "Z" };
					m_out << Indent(indent) << "Planar Mapping Normal: " << lPlanarMappingNormals[texture.GetPlanarMappingNormal()] << "\n";
				}

				if (blend_mode >= 0)
				{
					char const* lBlendModes[] = {
						"Translucent", "Additive", "Modulate", "Modulate2", "Over", "Normal", "Dissolve", "Darken", "ColorBurn", "LinearBurn",
						"DarkerColor", "Lighten", "Screen", "ColorDodge", "LinearDodge", "LighterColor", "SoftLight", "HardLight", "VividLight",
						"LinearLight", "PinLight", "HardMix", "Difference", "Exclusion", "Subtract", "Divide", "Hue", "Saturation", "Color",
						"Luminosity", "Overlay"
					};
					m_out << Indent(indent) << "Blend Mode: " << lBlendModes[blend_mode] << "\n";
				}
			
				m_out << Indent(indent) << "Alpha: " << texture.GetDefaultAlpha() << "\n";

				if (FbxFileTexture const* lFileTexture = FbxCast<FbxFileTexture>(&texture))
				{
					const char* lMaterialUses[] = { "Model Material", "Default Material" };
					m_out << Indent(indent) << "Material Use: " << lMaterialUses[lFileTexture->GetMaterialUse()] << "\n";
				}

				const char* pTextureUses[] = { "Standard", "Shadow Map", "Light Map", "Spherical Reflexion Map", "Sphere Reflexion Map", "Bump Normal Map" };
				m_out << Indent(indent) << "Texture Use: " << pTextureUses[texture.GetTextureUse()] << "\n";
			}
			void WriteMaterialConnections(FbxMesh const& mesh, int indent)
			{
				m_out << Indent(indent) << "Material Connections:\n";
				++indent;

				// check whether the material maps with only one mesh
				bool lIsAllSame = true;
				for (int l = 0, lend = mesh.GetElementMaterialCount(); l != lend; ++l)
				{
					FbxGeometryElementMaterial const* lMaterialElement = mesh.GetElementMaterial(l);
					lIsAllSame &= lMaterialElement->GetMappingMode() != FbxGeometryElement::eByPolygon;
				}

				auto WriteTextureNames = [this](FbxProperty const& property, int indent)
				{
					int lLayeredTextureCount = property.GetSrcObjectCount<FbxLayeredTexture>();
					if (lLayeredTextureCount > 0)
					{
						m_out << Indent(indent) << " Texture ";
						for (int j = 0; j != lLayeredTextureCount; ++j)
						{
							FbxLayeredTexture const* lLayeredTexture = property.GetSrcObject<FbxLayeredTexture>(j);
							for (int k = 0, kend = lLayeredTexture->GetSrcObjectCount<FbxTexture>(); k != kend; ++k)
								m_out << "\"" << lLayeredTexture->GetName() << "\" ";
							m_out << "of " << property.GetName() << " on layer " << j;
						}
						m_out << "\n";
					}
					else
					{
						//no layered texture simply get on the property
						m_out << Indent(indent) << " Texture ";
						for (int j = 0, jend = property.GetSrcObjectCount<FbxTexture>(); j != jend; ++j)
						{
							FbxTexture* lTexture = property.GetSrcObject<FbxTexture>(j);
							m_out << "\"" << (lTexture ? lTexture->GetName() : "unnamed") << "\" ";
						}
						m_out << "of " << property.GetName() << "\n";
					}
				};
				auto WriteMaterialTextureConnections = [this, WriteTextureNames](FbxSurfaceMaterial const& material, int material_id, int indent)
				{
					//Show all the textures
					m_out << Indent(indent) << "Material " << material_id << ":\n";
					++indent;

					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sDiffuse), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sDiffuseFactor), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sEmissive), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sEmissiveFactor), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sAmbient), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sAmbientFactor), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sSpecular), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sSpecularFactor), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sShininess), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sBump), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sNormalMap), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sTransparentColor), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sTransparencyFactor), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sReflection), indent);
					WriteTextureNames(material.FindProperty(FbxSurfaceMaterial::sReflectionFactor), indent);
				};

				// For eAllSame mapping type, just out the material and texture mapping info once
				if (lIsAllSame)
				{
					for (int l = 0, lend = mesh.GetElementMaterialCount(); l != lend; ++l)
					{
						FbxGeometryElementMaterial const* lMaterialElement = mesh.GetElementMaterial(l);
						if (lMaterialElement->GetMappingMode() == FbxGeometryElement::eAllSame)
						{
							FbxSurfaceMaterial const* lMaterial = mesh.GetNode()->GetMaterial(lMaterialElement->GetIndexArray().GetAt(0));
							int lMatId = lMaterialElement->GetIndexArray().GetAt(0);
							if (lMatId >= 0)
							{
								m_out << Indent(indent) << "all polygons share the same material in mesh " << l << "\n";
								WriteMaterialTextureConnections(*lMaterial, lMatId, indent+1);
							}
						}
					}

					//no material
					if (mesh.GetElementMaterialCount() == 0)
						m_out << Indent(indent) << "no material applied\n";
				}

				//For eByPolygon mapping type, just out the material and texture mapping info once
				else
				{
					for (int i = 0, iend = mesh.GetPolygonCount(); i != iend; ++i)
					{
						m_out << Indent(indent) << "Polygon " << i << "\n";
						for (int l = 0, lend = mesh.GetElementMaterialCount(); l != lend ; ++l)
						{
							FbxGeometryElementMaterial const* lMaterialElement = mesh.GetElementMaterial(l);

							FbxSurfaceMaterial* lMaterial = mesh.GetNode()->GetMaterial(lMaterialElement->GetIndexArray().GetAt(i));
							int lMatId = lMaterialElement->GetIndexArray().GetAt(i);
							if (lMatId >= 0)
							{
								WriteMaterialTextureConnections(*lMaterial, lMatId, indent+1);
							}
						}
					}
				}
			}
			void WriteLink(FbxGeometry const& geometry, int indent)
			{
				for (int i = 0, iend = geometry.GetDeformerCount(FbxDeformer::eSkin); i != iend; ++i)
				{
					auto skin = static_cast<FbxSkin const*>(geometry.GetDeformer(i, FbxDeformer::eSkin));
					for (int j = 0, jend = skin->GetClusterCount(); j != jend; ++j)
					{
						auto lCluster = skin->GetCluster(j);
						m_out << Indent(indent) << "Cluster " << i << "\n";

						char const* lClusterModes[] = { "Normalize", "Additive", "Total1" };
						m_out << Indent(indent) << "Mode: " << lClusterModes[lCluster->GetLinkMode()] << "\n";

						if (lCluster->GetLink() != nullptr)
							m_out << Indent(indent) << "Name: " << lCluster->GetLink()->GetName() << "\n";

						int const* lIndices = lCluster->GetControlPointIndices();
						m_out << Indent(indent) << "Link Indices: ";
						for (int k = 0, kend = lCluster->GetControlPointIndicesCount(); k != kend; ++k)
							m_out << (k == 0 ? "" : ", ") << lIndices[k];
						m_out << "\n";

						double* lWeights = lCluster->GetControlPointWeights();
						m_out << Indent(indent) << "Weight Values: ";
						for (int k = 0, kend = lCluster->GetControlPointIndicesCount(); k != kend; ++k)
							m_out << (k == 0 ? "" : ", ") << lWeights[k];
						m_out << "\n";

						FbxAMatrix lMatrix;

						lMatrix = lCluster->GetTransformMatrix(lMatrix);
						m_out << Indent(indent) << "Transform Translation: " << lMatrix.GetT() << "\n";
						m_out << Indent(indent) << "Transform Rotation: " << lMatrix.GetR() << "\n";
						m_out << Indent(indent) << "Transform Scaling: " << lMatrix.GetS() << "\n";

						lMatrix = lCluster->GetTransformLinkMatrix(lMatrix);
						m_out << Indent(indent) << "Transform Link Translation: " << lMatrix.GetT() << "\n";
						m_out << Indent(indent) << "Transform Link Rotation: " << lMatrix.GetR() << "\n";
						m_out << Indent(indent) << "Transform Link Scaling: " << lMatrix.GetS() << "\n";

						if (lCluster->GetAssociateModel() != nullptr)
						{
							lMatrix = lCluster->GetTransformAssociateModelMatrix(lMatrix);
							m_out << Indent(indent) << "Associate Model: " << lCluster->GetAssociateModel()->GetName() << "\n";
							m_out << Indent(indent) << "Associate Model Translation: " << lMatrix.GetT() << "\n";
							m_out << Indent(indent) << "Associate Model Rotation: " << lMatrix.GetR() << "\n";
							m_out << Indent(indent) << "Associate Model Scaling: " << lMatrix.GetS() << "\n";
						}
					}
				}
			}
			void WriteShape(FbxGeometry const& geometry, int indent)
			{
				for (int lBlendShapeIndex = 0, lend = geometry.GetDeformerCount(FbxDeformer::eBlendShape); lBlendShapeIndex != lend; ++lBlendShapeIndex)
				{
					auto lBlendShape = static_cast<FbxBlendShape const*>(geometry.GetDeformer(lBlendShapeIndex, FbxDeformer::eBlendShape));
					m_out << Indent(indent) << "BlendShape " << lBlendShape->GetName() << "\n";

					for (int lBlendShapeChannelIndex = 0, bscend = lBlendShape->GetBlendShapeChannelCount(); lBlendShapeChannelIndex != bscend; ++lBlendShapeChannelIndex)
					{
						auto lBlendShapeChannel = lBlendShape->GetBlendShapeChannel(lBlendShapeChannelIndex);
						m_out << Indent(indent) << "BlendShapeChannel " << lBlendShapeChannel->GetName() << "\n";
						m_out << Indent(indent) << "Default Deform Value: " << lBlendShapeChannel->DeformPercent.Get() << "\n";

						for (int lTargetShapeIndex = 0, tsend = lBlendShapeChannel->GetTargetShapeCount(); lTargetShapeIndex != tsend; ++lTargetShapeIndex)
						{
							auto lShape = lBlendShapeChannel->GetTargetShape(lTargetShapeIndex);
							m_out << Indent(indent) << "TargetShape " << lShape->GetName() << "\n";

							if (geometry.GetAttributeType() == FbxNodeAttribute::eMesh)
							{
								FbxMesh const* mesh = FbxCast<FbxMesh>(&geometry);
								if (mesh && lShape->GetControlPointsCount() && mesh->GetControlPointsCount())
								{
									if (lShape->GetControlPointsCount() == mesh->GetControlPointsCount())
									{
										// Display control points that are different from the mesh
										FbxVector4* lShapeControlPoint = lShape->GetControlPoints();
										FbxVector4* lMeshControlPoint = mesh->GetControlPoints();
										for (int j = 0; j < lShape->GetControlPointsCount(); j++)
										{
											FbxVector4 delta = lShapeControlPoint[j] - lMeshControlPoint[j];
											if (!FbxEqual(delta, FbxZeroVector4))
											{
												m_out << Indent(indent) << "Control Point[" << j << "]: " << lShapeControlPoint[j] << "\n";
											}
										}
									}

									for (int i = 0, iend = lShape->GetLayerCount(); i != iend; ++i)
									{
										#if 0 //todo
										const FbxLayer* pLayer = lShape->GetLayer(i);
										DisplayLayerElement<FbxVector4>(FbxLayerElement::eNormal, pLayer, pMesh);
										DisplayLayerElement<FbxColor>(FbxLayerElement::eVertexColor, pLayer, pMesh);
										DisplayLayerElement<FbxVector4>(FbxLayerElement::eTangent, pLayer, pMesh);
										DisplayLayerElement<FbxColor>(FbxLayerElement::eBiNormal, pLayer, pMesh);
										DisplayLayerElement<FbxColor>(FbxLayerElement::eUV, pLayer, pMesh);
										#endif
									}
								}
							}
							else
							{
								FbxLayerElementArrayTemplate<FbxVector4>* lNormals = nullptr;
								bool has_normals = lShape->GetNormals(&lNormals);

								for (int j = 0, jend = lShape->GetControlPointsCount(); j != jend; ++j)
								{
									m_out << Indent(indent) << "Control Point " << j << "\n";
									m_out << Indent(indent) << "Coordinates: " << lShape->GetControlPoints()[j] << "\n";
									if (has_normals && lNormals->GetCount() == jend)
									{
										m_out << Indent(indent) << "Normal Vector: " << lNormals->GetAt(j) << "\n";
									}
								}
							}
						}
					}
				}
			}
			void WriteCache(FbxGeometry const& geometry, int indent)
			{
				for (int i = 0, iend = geometry.GetDeformerCount(FbxDeformer::eVertexCache); i != iend; ++i)
				{
					auto lDeformer = static_cast<FbxVertexCacheDeformer const*>(geometry.GetDeformer(i, FbxDeformer::eVertexCache));
					if (!lDeformer)
						continue;

					auto lCache = lDeformer->GetCache();
					if (!lCache)
						continue;

					if (!lCache->OpenFileForRead())
						continue;

					m_out << Indent(indent) << "Vertex Cache:\n";
					int lChannelIndex = lCache->GetChannelIndex(lDeformer->Channel.Get());

					// skip normal channel
					if (lChannelIndex < 0)
						continue;

					FbxString lChnlName;
					lCache->GetChannelName(lChannelIndex, lChnlName);
					m_out << Indent(indent) << "Channel Name: " << lChnlName.Buffer() << "\n";
					
					FbxCache::EMCDataType lChnlType;
					lCache->GetChannelDataType(lChannelIndex, lChnlType);
					m_out << Indent(indent) << "Channel Type: ";
					switch (lChnlType)
					{
						case FbxCache::eUnknownData:       m_out << "Unknown Data"; break;
						case FbxCache::eDouble:            m_out << "Double"; break;
						case FbxCache::eDoubleArray:       m_out << "Double Array"; break;
						case FbxCache::eDoubleVectorArray: m_out << "Double Vector Array"; break;
						case FbxCache::eInt32Array:        m_out << "Int32 Array"; break;
						case FbxCache::eFloatArray:        m_out << "Float Array"; break;
						case FbxCache::eFloatVectorArray:  m_out << "Float Vector Array"; break;
					}
					m_out << "\n";

					FbxString lChnlInterp;
					lCache->GetChannelInterpretation(lChannelIndex, lChnlInterp);
					m_out << Indent(indent) << "Channel Interpretation: " << lChnlInterp.Buffer() << "\n";

					FbxCache::EMCSamplingType lChnlSampling;
					lCache->GetChannelSamplingType(lChannelIndex, lChnlSampling);
					m_out << Indent(indent) << "Channel Sampling Type: " << lChnlSampling << "\n";

					FbxTime start, stop, rate;
					unsigned int lChnlSampleCount;
					lCache->GetAnimationRange(lChannelIndex, start, stop);
					lCache->GetChannelSamplingRate(lChannelIndex, rate);
					lCache->GetChannelSampleCount(lChannelIndex, lChnlSampleCount);
					m_out << Indent(indent) << "Channel Sample Count: " << lChnlSampleCount << "\n";

					// Only display cache data if the data type is float vector array
					if (lChnlType != FbxCache::eFloatVectorArray)
						continue;

					m_out << Indent(indent) << (lChnlInterp == "normals" ? "Normal Cache Data" : "Points Cache Data") << "\n";

					int lFrame = 0;
					std::vector<float> buffer;
					for (FbxTime t = start; t <= stop; t += rate, ++lFrame)
					{
						m_out << Indent(indent) << "Frame " << lFrame << "\n";

						unsigned int lDataCount;
						lCache->GetChannelPointCount(lChannelIndex, t, lDataCount);

						buffer.resize(lDataCount);
						lCache->Read(lChannelIndex, t, buffer.data(), lDataCount);

						if (lChnlInterp == "normals")
						{
							// display normals cache data
							// the normal data is per-polygon per-vertex. we can get the polygon vertex index
							// from the index array of polygon vertex
							auto& mesh = static_cast<FbxMesh const&>(geometry);

							m_out << Indent(indent) << "Normal Count " << lDataCount << "\n";
							
							unsigned lNormalIndex = 0;
							for (int pi = 0, piend = mesh.GetPolygonCount(); pi != piend && lNormalIndex + 2 < lDataCount * 3; ++pi)
							{
								m_out << Indent(indent) << "Polygon " << pi << "\n";
								m_out << Indent(indent) << "Normals for Each Polygon Vertex: ";
								for (int j = 0, jend = mesh.GetPolygonSize(pi); j != jend && lNormalIndex + 2 < lDataCount * 3; ++j)
								{
									FbxVector4 normal(buffer[lNormalIndex], buffer[lNormalIndex + 1], buffer[lNormalIndex + 2]);
									m_out << Indent(indent) << "Normal Cache Data  " << normal << "\n";

									lNormalIndex += 3;
								}
							}
						}
						else
						{
							m_out << Indent(indent) << "Points Count: " << lDataCount << "\n";
							for (unsigned int j = 0; j < lDataCount * 3; j = j + 3)
							{
								FbxVector4 points(buffer[j], buffer[j + 1], buffer[j + 2]);
								m_out << Indent(indent) << "Points Cache Data: " << points << "\n";
							}
						}
					}

					lCache->CloseFile();
				}
			}
			void WriteNurb(FbxNode const& node, int indent)
			{
				auto const& nurbs = *static_cast<FbxNurbs const*>(node.GetNodeAttribute());

				m_out << Indent(indent) << "Nurb Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(nurbs, indent+1);

				const char* modes[] = { "Raw", "Low No Normals", "Low", "High No Normals", "High" };
				m_out << Indent(indent) << "Surface Mode: " << modes[nurbs.GetSurfaceMode()] << "\n";

				auto lControlPointsCount = nurbs.GetControlPointsCount();
				auto lControlPoints = nurbs.GetControlPoints();

				for (int i = 0; i != lControlPointsCount; ++i)
				{
					m_out << Indent(indent) << "Control Point " << i << "\n";
					m_out << Indent(indent) << "Coordinates: " << lControlPoints[i] << "\n";
					m_out << Indent(indent) << "Weight: " << lControlPoints[i][3] << "\n";
				}

				const char* nurb_types[] = { "Periodic", "Closed", "Open" };

				m_out << Indent(indent) << "Nurb U Type: " << nurb_types[nurbs.GetNurbsUType()] << "\n";
				m_out << Indent(indent) << "U Count: " << nurbs.GetUCount() << "\n";
				m_out << Indent(indent) << "Nurb V Type: " << nurb_types[nurbs.GetNurbsVType()] << "\n";
				m_out << Indent(indent) << "V Count: " << nurbs.GetVCount() << "\n";
				m_out << Indent(indent) << "U Order: " << nurbs.GetUOrder() << "\n";
				m_out << Indent(indent) << "V Order: " << nurbs.GetVOrder() << "\n";
				m_out << Indent(indent) << "U Step: " << nurbs.GetUStep() << "\n";
				m_out << Indent(indent) << "V Step: " << nurbs.GetVStep() << "\n";

				FbxString lString;
				double* lUKnotVector = nurbs.GetUKnotVector();
				double* lVKnotVector = nurbs.GetVKnotVector();
				int* lUMultiplicityVector = nurbs.GetUMultiplicityVector();
				int* lVMultiplicityVector = nurbs.GetVMultiplicityVector();

				m_out << Indent(indent) << "U Knot Vector: ";
				for (int i = 0, lUKnotCount = nurbs.GetUKnotCount(); i != lUKnotCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << (float)lUKnotVector[i];
				}
				m_out << "\n";
				m_out << Indent(indent) << "V Knot Vector: ";
				for (int i = 0, lVKnotCount = nurbs.GetVKnotCount(); i != lVKnotCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << (float)lVKnotVector[i];
				}
				m_out << "\n";
				m_out << Indent(indent) << "U Multiplicity Vector: ";
				for (int i = 0, lUMultiplicityCount = nurbs.GetUCount(); i != lUMultiplicityCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << lUMultiplicityVector[i];
				}
				m_out << "\n";
				m_out << Indent(indent) << "V Multiplicity Vector: ";
				for (int i = 0, lVMultiplicityCount = nurbs.GetVCount(); i != lVMultiplicityCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << lVMultiplicityVector[i];
				}
				m_out << "\n";

				WriteTexture(nurbs, indent+1);
				WriteMaterials(nurbs, indent+1);
				WriteLink(nurbs, indent+1);
				WriteShape(nurbs, indent+1);
				WriteCache(nurbs, indent+1);
			}
			void WritePatch(FbxNode const& node, int indent)
			{
				auto lPatch = static_cast<FbxPatch const*>(node.GetNodeAttribute());

				m_out << Indent(indent) << "Patch Name: " << node.GetName() << "\n";
				++indent;

				WriteMetaDataConnections(*lPatch, indent + 1);

				const char* lSurfaceModes[] = { "Raw", "Low No Normals", "Low", "High No Normals", "High" };
				m_out << Indent(indent) << "Surface Mode: " << lSurfaceModes[lPatch->GetSurfaceMode()] << "\n";

				FbxVector4* lControlPoints = lPatch->GetControlPoints();
				for (int i = 0, iend = lPatch->GetControlPointsCount(); i != iend; ++i)
				{
					m_out << Indent(indent) << "Control Point " << i << "\n";
					m_out << Indent(indent) << "Coordinates: " << lControlPoints[i] << "\n";
					m_out << Indent(indent) << "Weight: " << lControlPoints[i][3] << "\n";
				}

				const char* lPatchTypes[] = { "Bezier", "Bezier Quadric", "Cardinal", "B-Spline", "Linear" };
				m_out << Indent(indent) << "Patch U Type: "    << lPatchTypes[lPatch->GetPatchUType()] << "\n";
				m_out << Indent(indent) << "U Count: "         << lPatch->GetUCount() << "\n";
				m_out << Indent(indent) << "Patch V Type: "    << lPatchTypes[lPatch->GetPatchVType()] << "\n";
				m_out << Indent(indent) << "V Count: "         << lPatch->GetVCount() << "\n";
				m_out << Indent(indent) << "U Step: "          << lPatch->GetUStep() << "\n";
				m_out << Indent(indent) << "V Step: "          << lPatch->GetVStep() << "\n";
				m_out << Indent(indent) << "U Closed: "        << lPatch->GetUClosed() << "\n";
				m_out << Indent(indent) << "V Closed: "        << lPatch->GetVClosed() << "\n";
				m_out << Indent(indent) << "U Capped Top: "    << lPatch->GetUCappedTop() << "\n";
				m_out << Indent(indent) << "U Capped Bottom: " << lPatch->GetUCappedBottom() << "\n";
				m_out << Indent(indent) << "V Capped Top: "    << lPatch->GetVCappedTop() << "\n";
				m_out << Indent(indent) << "V Capped Bottom: " << lPatch->GetVCappedBottom() << "\n";

				WriteTexture(*lPatch, indent + 1);
				WriteMaterials(*lPatch, indent + 1);
				WriteLink(*lPatch, indent + 1);
				WriteShape(*lPatch, indent + 1);
			}
			void WriteCamera(FbxNode const& node, int indent)
			{
				m_out << "Camera Name: " << node.GetName() << "\n";

				auto pCamera = static_cast<FbxCamera const*>(node.GetNodeAttribute());
				if (!pCamera)
				{
					m_out << Indent(indent) << "NOT FOUND" << "\n";
					return;
				}
				
				WriteMetaDataConnections(*pCamera, indent + 1);
				
				m_out << Indent(indent) << "Camera Position and Orientation" << "\n";
				{
					m_out << Indent(indent) << "Position: " << pCamera->Position.Get() << "\n";

					if (FbxNode const* pTargetNode = node.GetTarget())
						m_out << Indent(indent) << "Camera Interest: " << pTargetNode->GetName() << "\n";
					else
						m_out << Indent(indent) << "Default Camera Interest Position: " << pCamera->InterestPosition.Get() << "\n";

					if (FbxNode const* pTargetUpNode = node.GetTargetUp())
						m_out << Indent(indent) << "Camera Up Target: " << pTargetUpNode->GetName() << "\n";
					else
						m_out << Indent(indent) << "Up Vector: " << pCamera->UpVector.Get() << "\n";

					m_out << Indent(indent) << "Roll: " << pCamera->Roll.Get() << "\n";

					char const* lProjectionTypes[] = { "Perspective", "Orthogonal" };
					m_out << Indent(indent) << "Projection Type: " << lProjectionTypes[pCamera->ProjectionType.Get()] << "\n";
				}

				m_out << Indent(indent) << "Viewing Area Controls:" << "\n";
				{
					char const* lCameraFormat[] = { "Custom", "D1 NTSC", "NTSC", "PAL", "D1 PAL", "HD", "640x480", "320x200", "320x240", "128x128", "Full Screen" };
					m_out << Indent(indent) << "Format: " << lCameraFormat[pCamera->GetFormat()] << "\n";

					const char* lAspectRatioModes[] = { "Window Size", "Fixed Ratio", "Fixed Resolution", "Fixed Width", "Fixed Height" };
					m_out << Indent(indent) << "Aspect Ratio Mode: " << lAspectRatioModes[pCamera->GetAspectRatioMode()] << "\n";

					// If the ratio mode is eWINDOW_SIZE, both width and height values aren't relevant.
					m_out << Indent(indent) << "Aspect Width: " << pCamera->AspectWidth.Get() << "\n";
					m_out << Indent(indent) << "Aspect Height: " << pCamera->AspectHeight.Get() << "\n";
					m_out << Indent(indent) << "Pixel Ratio: " << pCamera->PixelAspectRatio.Get() << "\n";
					m_out << Indent(indent) << "Near Plane: " << pCamera->NearPlane.Get() << "\n";
					m_out << Indent(indent) << "Far Plane: " << pCamera->FarPlane.Get() << "\n";
					m_out << Indent(indent) << "Mouse Lock: " << pCamera->LockMode.Get() << "\n";
				}

				// If camera projection type is set to FbxCamera::eOrthogonal, the aperture and film controls are not relevant.
				if (pCamera->ProjectionType.Get() != FbxCamera::eOrthogonal)
				{
					m_out << Indent(indent) << "Aperture and Film Controls" << "\n";
					{
						const char* lCameraApertureFormats[] = { "Custom", "16mm Theatrical", "Super 16mm", "35mm Academy", "35mm TV Projection", "35mm Full Aperture", "35mm 1.85 Projection", "35mm Anamorphic", "70mm Projection", "VistaVision", "Dynavision", "Imax" };
						m_out << Indent(indent) << "Aperture Format: " << lCameraApertureFormats[pCamera->GetApertureFormat()] << "\n";

						const char* lCameraApertureModes[] = { "Horizontal and Vertical", "Horizontal", "Vertical", "Focal Length" };
						m_out << Indent(indent) << "Aperture Mode: " << lCameraApertureModes[pCamera->GetApertureMode()] << "\n";
						m_out << Indent(indent) << "Aperture Width: " << pCamera->GetApertureWidth() << " inches\n";
						m_out << Indent(indent) << "Aperture Height: " << pCamera->GetApertureHeight() << " inches\n";
						m_out << Indent(indent) << "Squeeze Ratio: " << pCamera->GetSqueezeRatio() << "\n";
						m_out << Indent(indent) << "Focal Length: " << pCamera->FocalLength.Get() << "mm\n";
						m_out << Indent(indent) << "Field of View: " << pCamera->FieldOfView.Get() << " degrees\n";
					}
				}

				m_out << Indent(indent) << "Background Properties" << "\n";
				{
					m_out << Indent(indent) << "Background File Name: \"" << pCamera->GetBackgroundFileName() << "\"\n";

					const char* lBackgroundDisplayModes[] = { "Disabled", "Always", "When Media" };
					m_out << Indent(indent) << "Background Display Mode: " << lBackgroundDisplayModes[pCamera->ViewFrustumBackPlaneMode.Get()] << "\n";
					m_out << Indent(indent) << "Foreground Matte Threshold Enable: " << pCamera->ShowFrontplate.Get() << "\n";

					// This option is only relevant if background drawing mode is set to eFOREGROUND or eBACKGROUND_AND_FOREGROUND.
					if (pCamera->ForegroundOpacity.Get())
						m_out << Indent(indent) << "Foreground Matte Threshold: " << pCamera->BackgroundAlphaTreshold.Get() << "\n";

					m_out << Indent(indent) << "Background Placement Options: ";
					if (pCamera->GetBackPlateFitImage()) m_out << " Fit";
					if (pCamera->GetBackPlateCenter()) m_out << " Center";
					if (pCamera->GetBackPlateKeepRatio()) m_out << " Keep Ratio";
					if (pCamera->GetBackPlateCrop()) m_out << " Crop";
					m_out << "\n";

					m_out << Indent(indent) << "Background Distance: " << pCamera->BackPlaneDistance.Get() << "\n";

					const char* lCameraBackgroundDistanceModes[] = { "Relative to Interest", "Absolute from Camera" };
					m_out << Indent(indent) << "Background Distance Mode: " << lCameraBackgroundDistanceModes[pCamera->BackPlaneDistanceMode.Get()] << "\n";
				}

				m_out << Indent(indent) << "Camera View Options:" << "\n";
				{
					m_out << Indent(indent) << "View Camera Interest: " << pCamera->ViewCameraToLookAt.Get() << "\n";
					m_out << Indent(indent) << "View Near Far Planes: " << pCamera->ViewFrustumNearFarPlane.Get() << "\n";
					m_out << Indent(indent) << "Show Grid: " << pCamera->ShowGrid.Get() << "\n";
					m_out << Indent(indent) << "Show Axis: " << pCamera->ShowAzimut.Get() << "\n";
					m_out << Indent(indent) << "Show Name: " << pCamera->ShowName.Get() << "\n";
					m_out << Indent(indent) << "Show Info on Moving: " << pCamera->ShowInfoOnMoving.Get() << "\n";
					m_out << Indent(indent) << "Show Time Code: " << pCamera->ShowTimeCode.Get() << "\n";
					m_out << Indent(indent) << "Display Safe Area: " << pCamera->DisplaySafeArea.Get() << "\n";

					const char* lSafeAreaStyles[] = { "Round", "Square" };
					m_out << Indent(indent) << "Safe Area Style: " << lSafeAreaStyles[pCamera->SafeAreaDisplayStyle.Get()] << "\n";
					m_out << Indent(indent) << "Show Audio: " << pCamera->ShowAudio.Get() << "\n";
					m_out << Indent(indent) << "Background Color: " << pCamera->BackgroundColor.Get() << "\n";
					m_out << Indent(indent) << "Audio Color: " << pCamera->AudioColor.Get() << "\n";
					m_out << Indent(indent) << "Use Frame Color: " << pCamera->UseFrameColor.Get() << "\n";
					m_out << Indent(indent) << "Frame Color: " << pCamera->FrameColor.Get() << "\n";
				}

				m_out << Indent(indent) << "Render Options:" << "\n";
				{
					const char* lCameraRenderOptionsUsageTimes[] = { "Interactive", "At Render" };
					m_out << Indent(indent) << "Render Options Usage Time: " << lCameraRenderOptionsUsageTimes[pCamera->UseRealTimeDOFAndAA.Get()] << "\n";
					m_out << Indent(indent) << "Use Antialiasing: " << pCamera->UseAntialiasing.Get() << "\n";
					m_out << Indent(indent) << "Antialiasing Intensity: " << pCamera->AntialiasingIntensity.Get() << "\n";

					const char* lCameraAntialiasingMethods[] = { "Oversampling Antialiasing", "Hardware Antialiasing" };
					m_out << Indent(indent) << "Antialiasing Method: " << lCameraAntialiasingMethods[pCamera->AntialiasingMethod.Get()] << "\n";

					// This option is only relevant if antialiasing method is set to eOVERSAMPLING_ANTIALIASING.
					if (pCamera->AntialiasingMethod.Get() == FbxCamera::eAAOversampling)
						m_out << Indent(indent) << "Number of Samples: " << pCamera->FrameSamplingCount.Get() << "\n";


					const char* lCameraSamplingTypes[] = { "Uniform", "Stochastic" };
					m_out << Indent(indent) << "Sampling Type: " << lCameraSamplingTypes[pCamera->FrameSamplingType.Get()] << "\n";
					m_out << Indent(indent) << "Use Accumulation Buffer: " << pCamera->UseAccumulationBuffer.Get() << "\n";
					m_out << Indent(indent) << "Use Depth of Field: " << pCamera->UseDepthOfField.Get() << "\n";

					const char* lCameraFocusDistanceSources[] = { "Camera Interest", "Specific Distance" };
					m_out << Indent(indent) << "Focus Distance Source: " << lCameraFocusDistanceSources[pCamera->FocusSource.Get()] << "\n";

					// This parameter is only relevant if focus distance source is set to eSPECIFIC_DISTANCE.
					if (pCamera->FocusSource.Get() == FbxCamera::eFocusSpecificDistance)
						m_out << Indent(indent) << "Specific Distance: " << pCamera->FocusDistance.Get() << "\n";

					m_out << Indent(indent) << "Focus Angle: " << pCamera->FocusAngle.Get() << " degrees\n";
				}

				m_out << Indent(indent) << "Default Animation Values:" << "\n";
				{
					m_out << Indent(indent) << "Default Field of View: " << pCamera->FieldOfView.Get() << "\n";
					m_out << Indent(indent) << "Default Field of View X: " << pCamera->FieldOfViewX.Get() << "\n";
					m_out << Indent(indent) << "Default Field of View Y: " << pCamera->FieldOfViewY.Get() << "\n";
					m_out << Indent(indent) << "Default Optical Center X: " << pCamera->OpticalCenterX.Get() << "\n";
					m_out << Indent(indent) << "Default Optical Center Y: " << pCamera->OpticalCenterY.Get() << "\n";
					m_out << Indent(indent) << "Default Roll: " << pCamera->Roll.Get() << "\n";
				}
			}
			void WriteLight(FbxNode const& node, int indent)
			{
				auto light = static_cast<FbxLight const*>(node.GetNodeAttribute());

				m_out << Indent(indent) << "Light Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(*light, indent + 1);

				const char* lLightTypes[] = { "Point", "Directional", "Spot", "Area", "Volume" };
				m_out << Indent(indent) << "Type: " << lLightTypes[light->LightType.Get()] << "\n";
				m_out << Indent(indent) << "Cast Light: " << light->CastLight.Get() << "\n";

				if (!light->FileName.Get().IsEmpty())
				{
					m_out << Indent(indent) << "Gobo" << "\n";
					m_out << Indent(indent) << "File Name: \"" << light->FileName.Get() << "\"\n";
					m_out << Indent(indent) << "Ground Projection: " << light->DrawGroundProjection.Get() << "\n";
					m_out << Indent(indent) << "Volumetric Projection: " << light->DrawVolumetricLight.Get() << "\n";
					m_out << Indent(indent) << "Front Volumetric Projection: " << light->DrawFrontFacingVolumetricLight.Get() << "\n";
				}

				m_out << Indent(indent) << "Default Animation Values:\n";
				{
					m_out << Indent(indent) << "Default Color: " << light->Color.Get() << "\n";
					m_out << Indent(indent) << "Default Intensity: " << light->Intensity.Get() << "\n";
					m_out << Indent(indent) << "Default Outer Angle: " << light->OuterAngle.Get() << "\n";
					m_out << Indent(indent) << "Default Fog: " << light->Fog.Get() << "\n";
				}
			}
			void WriteLodGroup(FbxNode const& node, int indent)
			{
				char const* lDisplayLevels[] = { "UseLOD", "Show", "Hide" };

				m_out << Indent(indent) << "LodGroup Name: " << node.GetName() << "\n";
				++indent;

				m_out << Indent(indent) << node.GetChildCount() << " Geometries" << "\n";
				for (int i = 0, iend = node.GetChildCount(); i != iend; ++i)
					m_out << Indent(indent) << node.GetChild(i)->GetName() << "\n";

				auto lLodGroupAttr = static_cast<FbxLODGroup const*>(node.GetNodeAttribute());
				m_out << Indent(indent) << "MinMaxDistance Enabled: " << lLodGroupAttr->MinMaxDistance.Get() << "\n";
				if (lLodGroupAttr->MinMaxDistance.Get())
				{
					m_out << Indent(indent) << "Min Distance: " << lLodGroupAttr->MinDistance.Get() << "\n";
					m_out << Indent(indent) << "Max Distance: " << lLodGroupAttr->MaxDistance.Get() << "\n";
				}
				m_out << Indent(indent) << "Is World Space: " << lLodGroupAttr->WorldSpace.Get() << "\n";
				m_out << Indent(indent) << "Thresholds used as Percentage: " << lLodGroupAttr->ThresholdsUsedAsPercentage.Get() << "\n";

				m_out << Indent(indent) << "Thresholds:\n";
				for (int i = 0, iend = lLodGroupAttr->GetNumThresholds(); i != iend; ++i)
				{
					FbxDistance lThreshVal;
					bool res = lLodGroupAttr->GetThreshold(i, lThreshVal);
					if (res || (!res && lLodGroupAttr->ThresholdsUsedAsPercentage.Get()))
						// when thresholds are used as percentage, the GetThreshold returns false
						// and we would need to make sure that the value is not bogus
						m_out << Indent(indent+1) << lThreshVal.value() << "\n";
				}

				m_out << Indent(indent) << "DisplayLevels:\n";
				for (int i = 0, iend = lLodGroupAttr->GetNumDisplayLevels(); i != iend; ++i)
				{
					FbxLODGroup::EDisplayLevel lLevel;
					if (lLodGroupAttr->GetDisplayLevel(i, lLevel))
						m_out << Indent(indent+1) << lDisplayLevels[lLevel] << "\n";
				}
			}
			void WriteUserProperties(FbxObject const& node, int indent)
			{
				int i = 0; bool first = true;
				for (FbxProperty prop = node.GetFirstProperty(); prop.IsValid(); prop = node.GetNextProperty(prop), ++i)
				{
					if (!prop.GetFlag(FbxPropertyFlags::eUserDefined))
						continue;

					first ? m_out << Indent(indent) << "User Properties:\n" : m_out;
					WriteProperty(prop, i, indent + 1);
					first = false;
				}
			}
			void WriteTarget(FbxNode const& node, int indent)
			{
				if (node.GetTarget() != nullptr)
				{
					m_out << Indent(indent) << "Target Name: " << node.GetTarget()->GetName() << "\n";
				}
			}
			void WritePivotsAndLimits(FbxNode const& node, int indent)
			{
				FbxNode::EPivotState pivot_state;
				node.GetPivotState(FbxNode::eSourcePivot, pivot_state);
				if (pivot_state == FbxNode::ePivotActive)
				{
					FbxVector4 vec;

					m_out << Indent(indent) << "Pivot Information:\n";
					if (!(vec = node.GetPreRotation(FbxNode::eSourcePivot)).IsZero())
						m_out << Indent(indent) << std::format("Pre-Rotation: {} {} {}\n", vec[0], vec[1], vec[2]);
					if (!(vec = node.GetPostRotation(FbxNode::eSourcePivot)).IsZero())
						m_out << Indent(indent) << std::format("Post-Rotation: {} {} {}\n", vec[0], vec[1], vec[2]);
					if (!(vec = node.GetRotationPivot(FbxNode::eSourcePivot)).IsZero())
						m_out << Indent(indent) << std::format("Rotation Pivot: {} {} {}\n", vec[0], vec[1], vec[2]);
					if (!(vec = node.GetRotationOffset(FbxNode::eSourcePivot)).IsZero())
						m_out << Indent(indent) << std::format("Rotation Offset: {} {} {}\n", vec[0], vec[1], vec[2]);
					if (!(vec = node.GetScalingPivot(FbxNode::eSourcePivot)).IsZero())
						m_out << Indent(indent) << std::format("Scaling Pivot: {} {} {}\n", vec[0], vec[1], vec[2]);
					if (!(vec = node.GetScalingOffset(FbxNode::eSourcePivot)).IsZero())
						m_out << Indent(indent) << std::format("Scaling Offset: {} {} {}\n", vec[0], vec[1], vec[2]);
				}

				// Limits
				if (static_cast<bool>(node.TranslationActive) ||
					static_cast<bool>(node.RotationActive) ||
					static_cast<bool>(node.ScalingActive))
				{
					m_out << Indent(indent) << "Limits Information:\n";
					if (static_cast<bool>(node.TranslationActive))
					{
						FbxDouble3 min_values = node.TranslationMin;
						FbxDouble3 max_values = node.TranslationMax;
						if (!static_cast<bool>(node.TranslationMinX)) min_values[0] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.TranslationMinY)) min_values[1] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.TranslationMinZ)) min_values[2] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.TranslationMaxX)) max_values[0] = +std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.TranslationMaxY)) max_values[1] = +std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.TranslationMaxZ)) max_values[2] = +std::numeric_limits<double>::infinity();

						m_out << Indent(indent) << "Translation limits:\n";
						m_out << Indent(indent) << "X: [" << min_values[0] << ", " << max_values[0] << "]\n";
						m_out << Indent(indent) << "Y: [" << min_values[1] << ", " << max_values[1] << "]\n";
						m_out << Indent(indent) << "Z: [" << min_values[2] << ", " << max_values[2] << "]\n";
					}
					if (static_cast<bool>(node.RotationActive))
					{
						FbxDouble3 min_values = node.RotationMin;
						FbxDouble3 max_values = node.RotationMax;
						if (!static_cast<bool>(node.RotationMinX)) min_values[0] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.RotationMinY)) min_values[1] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.RotationMinZ)) min_values[2] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.RotationMaxX)) max_values[0] = +std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.RotationMaxY)) max_values[1] = +std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.RotationMaxZ)) max_values[2] = +std::numeric_limits<double>::infinity();

						m_out << Indent(indent) << "Rotation limits:\n";
						m_out << Indent(indent) << "X: [" << min_values[0] << ", " << max_values[0] << "]\n";
						m_out << Indent(indent) << "Y: [" << min_values[1] << ", " << max_values[1] << "]\n";
						m_out << Indent(indent) << "Z: [" << min_values[2] << ", " << max_values[2] << "]\n";
					}
					if (static_cast<bool>(node.ScalingActive))
					{
						FbxDouble3 min_values = node.ScalingMin;
						FbxDouble3 max_values = node.ScalingMax;
						if (!static_cast<bool>(node.ScalingMinX)) min_values[0] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.ScalingMinY)) min_values[1] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.ScalingMinZ)) min_values[2] = -std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.ScalingMaxX)) max_values[0] = +std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.ScalingMaxY)) max_values[1] = +std::numeric_limits<double>::infinity();
						if (!static_cast<bool>(node.ScalingMaxZ)) max_values[2] = +std::numeric_limits<double>::infinity();

						m_out << Indent(indent) << "Scaling limits:\n";
						m_out << Indent(indent) << "X: [" << min_values[0] << ", " << max_values[0] << "]\n";
						m_out << Indent(indent) << "Y: [" << min_values[1] << ", " << max_values[1] << "]\n";
						m_out << Indent(indent) << "Z: [" << min_values[2] << ", " << max_values[2] << "]\n";
					}
				}
			}
			void WriteTransformPropagation(FbxNode const& node, int indent)
			{
				m_out << Indent(indent) << "Transformation Propagation:\n";

				// Rotation Space
				FbxEuler::EOrder order;
				node.GetRotationOrder(FbxNode::eSourcePivot, order);

				m_out << Indent(indent) << "Rotation Space: ";
				switch (order)
				{
					case FbxEuler::eOrderXYZ: m_out << "Euler XYZ\n"; break;
					case FbxEuler::eOrderXZY: m_out << "Euler XZY\n"; break;
					case FbxEuler::eOrderYZX: m_out << "Euler YZX\n"; break;
					case FbxEuler::eOrderYXZ: m_out << "Euler YXZ\n"; break;
					case FbxEuler::eOrderZXY: m_out << "Euler ZXY\n"; break;
					case FbxEuler::eOrderZYX: m_out << "Euler ZYX\n"; break;
					case FbxEuler::eOrderSphericXYZ: m_out << "Spheric XYZ\n"; break;
					default: m_out << "UNKNOWN ORDER\n"; break;
				}

				// Use the Rotation space only for the limits
				// (keep using eEulerXYZ for the rest)
				m_out << Indent(indent) << std::format("Use the Rotation Space for Limit specification only: {}\n", node.GetUseRotationSpaceForLimitOnly(FbxNode::eSourcePivot) ? "Yes" : "No");

				// Inherit Type
				FbxTransform::EInheritType inherit_type;
				node.GetTransformationInheritType(inherit_type);

				m_out << Indent(indent) << "Transformation Inheritance: ";
				switch (inherit_type)
				{
					case FbxTransform::eInheritRrSs: m_out << "RrSs\n"; break;
					case FbxTransform::eInheritRSrs: m_out << "RSrs\n"; break;
					case FbxTransform::eInheritRrs: m_out << "Rrs\n"; break;
				}
			}
			void WriteGeometricTransform(FbxNode const& node, int indent)
			{
				m_out << Indent(indent) << "Geometric Transformations:\n";
				{
					auto xyz = node.GetGeometricTranslation(FbxNode::eSourcePivot);
					auto rot = node.GetGeometricRotation(FbxNode::eSourcePivot);
					auto scl = node.GetGeometricScaling(FbxNode::eSourcePivot);
					m_out << Indent(indent+1) << std::format("Translation: {} {} {}\n", xyz[0], xyz[1], xyz[2]);
					m_out << Indent(indent+1) << std::format("Rotation:    {} {} {}\n", rot[0], rot[1], rot[2]);
					m_out << Indent(indent+1) << std::format("Scaling:     {} {} {}\n", scl[0], scl[1], scl[2]);
				}
			}
			void WriteMetaDataConnections(FbxObject const& node, int indent)
			{
				for (int i = 0, iend = node.GetSrcObjectCount<FbxObjectMetaData>(); i != iend; ++i)
				{
					i == 0 ? m_out << Indent(indent) << "    MetaData connections:\n" : m_out;
					m_out << Indent(indent) << "Name: " << node.GetSrcObject<FbxObjectMetaData>(i)->GetName() << "\n";
				}
			}
			void WritePose(FbxScene const& scene, int indent)
			{
				for (int i = 0, iend = scene.GetPoseCount(); i != iend; ++i)
				{
					auto lPose = const_cast<FbxScene&>(scene).GetPose(i);

					m_out << Indent(indent) << "Pose " << i << "\n";
					m_out << Indent(indent+1) << "Pose Name: " << lPose->GetName() << "\n";
					m_out << Indent(indent+1) << "Is a bind pose: " << lPose->IsBindPose() << "\n";
					m_out << Indent(indent+1) << "Number of items in the pose: " << lPose->GetCount() << "\n";

					for (int j = 0, jend = lPose->GetCount(); j != jend; ++j)
					{
						m_out << Indent(indent+1) << "Item name: " << lPose->GetNodeName(j).GetCurrentName() << "\n";
						if (!lPose->IsBindPose())
							m_out << Indent(indent+1) << "Is local space matrix: " << lPose->IsLocalMatrix(j) << "\n"; // Rest pose can have local matrix

						m_out << Indent(indent+1) << "Matrix value: " << lPose->GetMatrix(j) << "\n";
					}
				}
				for (int i = 0, iend = scene.GetCharacterPoseCount(); i != iend; ++i)
				{
					FbxCharacterPose* lPose = const_cast<FbxScene&>(scene).GetCharacterPose(i);
					FbxCharacter* lCharacter = lPose->GetCharacter();
					if (!lCharacter)
						break;

					m_out << Indent(indent+1) << "Character Pose Name: " << lCharacter->GetName() << "\n";

					FbxCharacterLink lCharacterLink;
					for (auto lNodeId = FbxCharacter::eHips; lCharacter->GetCharacterLink(lNodeId, &lCharacterLink); lNodeId = FbxCharacter::ENodeId(int(lNodeId) + 1))
					{
						m_out << Indent(indent+1) << "Matrix value: " << lCharacterLink.mNode->EvaluateGlobalTransform(FBXSDK_TIME_ZERO) << "\n";
					}
				}
			}
			void WriteAnimation(FbxScene const& scene, int indent)
			{
				m_out << Indent(indent) << "Animation:\n";
				++indent;

				for (int i = 0, iend = scene.GetSrcObjectCount<FbxAnimStack>(); i != iend; ++i)
				{
					FbxAnimStack const* lAnimStack = scene.GetSrcObject<FbxAnimStack>(i);
					m_out << Indent(indent) << "Animation Stack Name: " << lAnimStack->GetName() << "\n";
					WriteAnimationStack(*lAnimStack, *scene.GetRootNode(), false, indent + 1);
				}
			}
			void WriteAnimationStack(FbxAnimStack const& anim_stack, FbxNode const& node, bool isSwitcher, int indent)
			{
				m_out << Indent(indent) << "contains ";
				int nbAnimLayers = anim_stack.GetMemberCount<FbxAnimLayer>();
				int nbAudioLayers = anim_stack.GetMemberCount<FbxAudioLayer>();
				if (nbAnimLayers == 0 && nbAudioLayers == 0)
					m_out << "no layers";
				if (nbAnimLayers != 0)
					m_out << nbAnimLayers << " Animation Layers";
				if (nbAudioLayers != 0)
					m_out << (nbAnimLayers != 0 ? " and " : "") << nbAudioLayers << " Audio Layers";
				m_out << "\n";

				for (int l = 0; l != nbAnimLayers; ++l)
				{
					FbxAnimLayer const* lAnimLayer = anim_stack.GetMember<FbxAnimLayer>(l);
					m_out << Indent(indent) << "AnimLayer " << l << "\n";
					WriteAnimationLayer(*lAnimLayer, node, isSwitcher, indent + 1);
				}
				for (int l = 0; l != nbAudioLayers; ++l)
				{
					FbxAudioLayer const* lAudioLayer = anim_stack.GetMember<FbxAudioLayer>(l);
					m_out << Indent(indent) << "AudioLayer " << l << "\n";
					WriteAudioLayer(*lAudioLayer, isSwitcher, indent + 1);
				}
			}
			void WriteAnimationLayer(FbxAnimLayer const& anim_layer, FbxNode const& node, bool isSwitcher, int indent)
			{
				m_out << Indent(indent) << "Node Name: " << node.GetName() << "\n";
				FbxAnimLayer* pAnimLayer = const_cast<FbxAnimLayer*>(&anim_layer);
				FbxNode* pNode = const_cast<FbxNode*>(&node);

				// Display general curves.
				if (!isSwitcher)
				{
					if (FbxAnimCurve const* lAnimCurve = pNode->LclTranslation.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_X)) WriteCurveKeys(*lAnimCurve, "TX", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclTranslation.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_Y)) WriteCurveKeys(*lAnimCurve, "TY", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclTranslation.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_Z)) WriteCurveKeys(*lAnimCurve, "TZ", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclRotation.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_X)) WriteCurveKeys(*lAnimCurve, "RX", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclRotation.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_Y)) WriteCurveKeys(*lAnimCurve, "RY", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclRotation.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_Z)) WriteCurveKeys(*lAnimCurve, "RZ", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclScaling.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_X)) WriteCurveKeys(*lAnimCurve, "SX", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclScaling.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_Y)) WriteCurveKeys(*lAnimCurve, "SY", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = pNode->LclScaling.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COMPONENT_Z)) WriteCurveKeys(*lAnimCurve, "SZ", indent + 1);
				}

				// Display curves specific to a light or marker.
				if (FbxNodeAttribute* lNodeAttribute = pNode->GetNodeAttribute())
				{
					if (FbxAnimCurve const* lAnimCurve = lNodeAttribute->Color.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COLOR_RED)) WriteCurveKeys(*lAnimCurve, "Red", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = lNodeAttribute->Color.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COLOR_GREEN)) WriteCurveKeys(*lAnimCurve, "Green", indent + 1);
					if (FbxAnimCurve const* lAnimCurve = lNodeAttribute->Color.GetCurve(pAnimLayer, FBXSDK_CURVENODE_COLOR_BLUE)) WriteCurveKeys(*lAnimCurve, "Blue", indent + 1);

					// Display curves specific to a light.
					if (FbxLight* light = pNode->GetLight())
					{
						if (FbxAnimCurve const* lAnimCurve = light->Intensity.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Intensity", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = light->OuterAngle.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Outer Angle", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = light->Fog.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Fog", indent + 1);
					}

					// Display curves specific to a camera.
					if (FbxCamera* camera = pNode->GetCamera())
					{
						if (FbxAnimCurve const* lAnimCurve = camera->FieldOfView.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Field of View", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = camera->FieldOfViewX.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Field of View X", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = camera->FieldOfViewY.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Field of View Y", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = camera->OpticalCenterX.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Optical Center X", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = camera->OpticalCenterY.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Optical Center Y", indent + 1);
						if (FbxAnimCurve const* lAnimCurve = camera->Roll.GetCurve(pAnimLayer)) WriteCurveKeys(*lAnimCurve, "Roll", indent + 1);
					}

					// Display curves specific to a geometry.
					if (lNodeAttribute->GetAttributeType() == FbxNodeAttribute::eMesh ||
						lNodeAttribute->GetAttributeType() == FbxNodeAttribute::eNurbs ||
						lNodeAttribute->GetAttributeType() == FbxNodeAttribute::ePatch)
					{
						FbxGeometry* lGeometry = (FbxGeometry*)lNodeAttribute;
						for (int lBlendShapeIndex = 0, bsend = lGeometry->GetDeformerCount(FbxDeformer::eBlendShape); lBlendShapeIndex != bsend; ++lBlendShapeIndex)
						{
							FbxBlendShape* lBlendShape = (FbxBlendShape*)lGeometry->GetDeformer(lBlendShapeIndex, FbxDeformer::eBlendShape);

							int lBlendShapeChannelCount = lBlendShape->GetBlendShapeChannelCount();
							for (int lChannelIndex = 0; lChannelIndex < lBlendShapeChannelCount; ++lChannelIndex)
							{
								FbxBlendShapeChannel* lChannel = lBlendShape->GetBlendShapeChannel(lChannelIndex);
								const char* lChannelName = lChannel->GetName();

								if (FbxAnimCurve const* lAnimCurve = lGeometry->GetShapeChannel(lBlendShapeIndex, lChannelIndex, pAnimLayer, true))
								{
									m_out << Indent(indent) << "Shape" << lChannelName << "\n";
									WriteCurveKeys(*lAnimCurve, "", indent + 1);
								}
							}
						}
					}
				}

				// Display curves specific to properties
				for (FbxProperty lProperty = pNode->GetFirstProperty(); lProperty.IsValid(); lProperty = pNode->GetNextProperty(lProperty))
				{
					if (!lProperty.GetFlag(FbxPropertyFlags::eUserDefined))
						continue;

					FbxAnimCurveNode* lCurveNode = lProperty.GetCurveNode(pAnimLayer);
					if (!lCurveNode)
						continue;

					FbxDataType lDataType = lProperty.GetPropertyDataType();
					if (lDataType.GetType() == eFbxBool || lDataType.GetType() == eFbxDouble || lDataType.GetType() == eFbxFloat || lDataType.GetType() == eFbxInt)
					{
						m_out << Indent(indent) << "Property " << lProperty.GetName() << " (Label: " << lProperty.GetLabel() << ")\n";
						for (int c = 0; c < lCurveNode->GetCurveCount(0U); c++)
						{
							if (FbxAnimCurve const* lAnimCurve = lCurveNode->GetCurve(0U, c))
								WriteCurveKeys(*lAnimCurve, "", indent + 1);
						}
					}
					else if (lDataType.GetType() == eFbxDouble3 || lDataType.GetType() == eFbxDouble4 || lDataType.Is(FbxColor3DT) || lDataType.Is(FbxColor4DT))
					{
						m_out << Indent(indent) << "Property " << lProperty.GetName() << " (Label: " << lProperty.GetLabel() << ")\n";
						for (int c = 0; c < lCurveNode->GetCurveCount(0U); c++)
						{
							if (FbxAnimCurve const* lAnimCurve = lCurveNode->GetCurve(0U, c))
								WriteCurveKeys(*lAnimCurve, "Component X", indent + 1);
						}
						for (int c = 0; c < lCurveNode->GetCurveCount(1U); c++)
						{
							if (FbxAnimCurve const* lAnimCurve = lCurveNode->GetCurve(1U, c))
								WriteCurveKeys(*lAnimCurve, "Component Y", indent + 1);
						}
						for (int c = 0; c < lCurveNode->GetCurveCount(2U); c++)
						{
							if (FbxAnimCurve const* lAnimCurve = lCurveNode->GetCurve(2U, c))
								WriteCurveKeys(*lAnimCurve, "Component Z", indent + 1);
						}
					}
					else if (lDataType.GetType() == eFbxEnum)
					{
						m_out << Indent(indent) << "Property " << lProperty.GetName() << " (Label: " << lProperty.GetLabel() << ")\n";
						for (int c = 0; c < lCurveNode->GetCurveCount(0U); c++)
						{
							if (FbxAnimCurve const* lAnimCurve = lCurveNode->GetCurve(0U, c))
								WriteListCurveKeys(*lAnimCurve, lProperty, indent + 1);
						}
					}
				}

				for (int lModelCount = 0; lModelCount != pNode->GetChildCount(); ++lModelCount)
					WriteAnimationLayer(anim_layer, *node.GetChild(lModelCount), isSwitcher, indent + 1);
			}
			void WriteCurveKeys(FbxAnimCurve const& pCurve, char const* label, int indent)
			{
				static const char* interpolation[] = { "?", "constant", "linear", "cubic" };
				static auto InterpolationFlagToIndex = [](int flags) -> int
				{
					if ((flags & FbxAnimCurveDef::eInterpolationConstant) == FbxAnimCurveDef::eInterpolationConstant) return 1;
					if ((flags & FbxAnimCurveDef::eInterpolationLinear) == FbxAnimCurveDef::eInterpolationLinear) return 2;
					if ((flags & FbxAnimCurveDef::eInterpolationCubic) == FbxAnimCurveDef::eInterpolationCubic) return 3;
					return 0;
				};

				static const char* constantMode[] = { "?", "Standard", "Next" };
				static auto ConstantmodeFlagToIndex = [](int flags) -> int
				{
					if ((flags & FbxAnimCurveDef::eConstantStandard) == FbxAnimCurveDef::eConstantStandard) return 1;
					if ((flags & FbxAnimCurveDef::eConstantNext) == FbxAnimCurveDef::eConstantNext) return 2;
					return 0;
				};

				static const char* cubicMode[] = { "?", "Auto", "Auto break", "Tcb", "User", "Break", "User break" };
				static auto TangentmodeFlagToIndex = [](int flags) -> int
				{
					if ((flags & FbxAnimCurveDef::eTangentAuto) == FbxAnimCurveDef::eTangentAuto) return 1;
					if ((flags & FbxAnimCurveDef::eTangentAutoBreak) == FbxAnimCurveDef::eTangentAutoBreak) return 2;
					if ((flags & FbxAnimCurveDef::eTangentTCB) == FbxAnimCurveDef::eTangentTCB) return 3;
					if ((flags & FbxAnimCurveDef::eTangentUser) == FbxAnimCurveDef::eTangentUser) return 4;
					if ((flags & FbxAnimCurveDef::eTangentGenericBreak) == FbxAnimCurveDef::eTangentGenericBreak) return 5;
					if ((flags & FbxAnimCurveDef::eTangentBreak) == FbxAnimCurveDef::eTangentBreak) return 6;
					return 0;
				};

				static const char* tangentWVMode[] = { "?", "None", "Right", "Next left" };
				static auto TangentweightFlagToIndex = [](int flags) -> int
				{
					if ((flags & FbxAnimCurveDef::eWeightedNone) == FbxAnimCurveDef::eWeightedNone) return 1;
					if ((flags & FbxAnimCurveDef::eWeightedRight) == FbxAnimCurveDef::eWeightedRight) return 2;
					if ((flags & FbxAnimCurveDef::eWeightedNextLeft) == FbxAnimCurveDef::eWeightedNextLeft) return 3;
					return 0;
				};
				static auto TangentVelocityFlagToIndex = [](int flags) -> int
				{
					if ((flags & FbxAnimCurveDef::eVelocityNone) == FbxAnimCurveDef::eVelocityNone) return 1;
					if ((flags & FbxAnimCurveDef::eVelocityRight) == FbxAnimCurveDef::eVelocityRight) return 2;
					if ((flags & FbxAnimCurveDef::eVelocityNextLeft) == FbxAnimCurveDef::eVelocityNextLeft) return 3;
					return 0;
				};

				m_out << Indent(indent) << label << ":\n";
				for (int lCount = 0, lend = pCurve.KeyGetCount(); lCount != lend; lCount++)
				{
					auto lKeyValue = static_cast<float>(pCurve.KeyGetValue(lCount));
					auto lKeyTime = pCurve.KeyGetTime(lCount);
					char time_string[256];

					m_out
						<< Indent(indent + 1)
						<< "Key Time: " << lKeyTime.GetTimeString(time_string, _countof(time_string))
						<< ".... Key Value: " << lKeyValue << " [ "
						<< interpolation[InterpolationFlagToIndex(pCurve.KeyGetInterpolation(lCount))];

					if ((pCurve.KeyGetInterpolation(lCount) & FbxAnimCurveDef::eInterpolationConstant) == FbxAnimCurveDef::eInterpolationConstant)
					{
						m_out << " | " << constantMode[ConstantmodeFlagToIndex(pCurve.KeyGetConstantMode(lCount))];
					}
					else if ((pCurve.KeyGetInterpolation(lCount) & FbxAnimCurveDef::eInterpolationCubic) == FbxAnimCurveDef::eInterpolationCubic)
					{
						m_out
							<< " | " << cubicMode[TangentmodeFlagToIndex(pCurve.KeyGetTangentMode(lCount))]
							<< " | " << tangentWVMode[TangentweightFlagToIndex(pCurve.KeyGet(lCount).GetTangentWeightMode())]
							<< " | " << tangentWVMode[TangentVelocityFlagToIndex(pCurve.KeyGet(lCount).GetTangentVelocityMode())];
					}
					m_out << " ]\n";
				}
			}
			void WriteListCurveKeys(FbxAnimCurve const& pCurve, FbxProperty const& pProperty, int indent)
			{
				for (int lCount = 0, lend = pCurve.KeyGetCount(); lCount != lend; ++lCount)
				{
					auto lKeyValue = static_cast<int>(pCurve.KeyGetValue(lCount));
					auto lKeyTime = pCurve.KeyGetTime(lCount);
					char time_string[256];

					m_out << Indent(indent)
						<< "Key Time: " << lKeyTime.GetTimeString(time_string, _countof(time_string))
						<< ".... Key Value: " << lKeyValue << " (" << pProperty.GetEnumValue(lKeyValue) << ")\n";
				}
			}
			void WriteAudioLayer(FbxAudioLayer const& pAudioLayer, bool, int indent)
			{
				m_out << Indent(indent) << "Name: " << pAudioLayer.GetName() << "\n";
				m_out << Indent(indent) << "Nb Audio Clips: " << pAudioLayer.GetMemberCount<FbxAudio>() << "\n";
				for (int i = 0, iend = pAudioLayer.GetMemberCount<FbxAudio>(); i != iend; ++i)
				{
					FbxAudio const* lClip = pAudioLayer.GetMember<FbxAudio>(i);
					m_out << Indent(indent) << "Clip[" << i << "]: " << lClip->GetName() << "\n";
				}
			}
			void WriteGenericInfo(FbxScene const& scene, int indent)
			{
				m_out << Indent(indent) << "Generic Info:\n";

				auto WriteProperties = [this](FbxObject const& pObject, int indent)
				{
					m_out << Indent(indent) << "Object: " << pObject.GetName() << "\n";
					++indent;

					// Display all the properties
					int i = 0;
					for (FbxProperty lProperty = pObject.GetFirstProperty(); lProperty.IsValid(); lProperty = pObject.GetNextProperty(lProperty), ++i)
						WriteProperty(lProperty, i, indent);
				};
				std::function<void(FbxObject const&, int)> WriteInfo = [this, &WriteInfo, &WriteProperties](FbxObject const& obj, int indent)
				{
					WriteProperties(obj, indent);
					if (FbxNode const* node = FbxCast<FbxNode>(&obj))
					{
						for (int i = 0, iend = node->GetChildCount(); i != iend; ++i)
							WriteInfo(*node->GetChild(i), indent + 1);
					}
				};

				// All objects directly connected onto the scene
				for (int i = 0, iend = scene.GetSrcObjectCount(); i != iend; ++i)
					WriteInfo(*scene.GetSrcObject(i), indent + 1);
			}
			void WriteProperty(FbxProperty const& prop, int index, int indent)
			{
				m_out << Indent(indent) << "Property: " << index << "\n";
				++indent;

				m_out << Indent(indent) << "Display Name: " << prop.GetLabel() << "\n";
				m_out << Indent(indent) << "Internal Name: " << prop.GetName() << "\n";
				m_out << Indent(indent) << "Type: " << prop.GetPropertyDataType().GetName() << "\n";
				if (prop.HasMinLimit()) m_out << Indent(indent) << "Min Limit: " << prop.GetMinLimit() << "\n";
				if (prop.HasMaxLimit()) m_out << Indent(indent) << "Max Limit: " << prop.GetMaxLimit() << "\n";
				m_out << Indent(indent) << "Is Animatable: " << prop.GetFlag(FbxPropertyFlags::eAnimatable) << "\n";
				m_out << Indent(indent) << "Default Value: " << prop << "\n";
			}
		};

		Writer writer(out);
		writer.Write(scene);
	}
}

extern "C"
{
	HINSTANCE g_hInstance;
	pr::geometry::fbx::Manager* g_manager;

	// DLL entry point
	BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
	{
		static int ref_count = 0;
		switch (ul_reason_for_call)
		{
			case DLL_PROCESS_ATTACH:
			{
				if (ref_count++ == 0)
					g_manager = new pr::geometry::fbx::Manager;

				g_hInstance = hInstance;
				break;
			}
			case DLL_PROCESS_DETACH:
			{
				if (--ref_count == 0)
					delete g_manager;

				g_hInstance = nullptr;
				break;
			}
			case DLL_THREAD_ATTACH: break;
			case DLL_THREAD_DETACH: break;
		}
		return TRUE;
	}

	// Load an fbx scene
	__declspec(dllexport) fbxsdk::FbxScene* __stdcall Fbx_LoadScene(std::istream& src)
	{
		using namespace pr::geometry::fbx;

		try
		{
			return Import(*g_manager, src).release();
		}
		catch (std::exception const& ex)
		{
			OutputDebugStringA(ex.what());
			DebugBreak();
			return nullptr;
		}
	}

	// Release an fbx scene
	__declspec(dllexport) void __stdcall Fbx_ReleaseScene(fbxsdk::FbxScene* scene)
	{
		try
		{
			if (scene != nullptr)
				scene->Destroy();
		}
		catch (std::exception const& ex)
		{
			OutputDebugStringA(ex.what());
			DebugBreak();
		}
	}

	// Read meta data about the scene
	__declspec(dllexport) pr::geometry::fbx::SceneProps __stdcall Fbx_ReadSceneProps(fbxsdk::FbxScene const* scene)
	{
		using namespace pr::geometry::fbx;

		try
		{
			return ReadProps(*scene);
		}
		catch (std::exception const& ex)
		{
			OutputDebugStringA(ex.what());
			DebugBreak();
			return {};
		}
	}

	// Read the model hierarchy from the scene
	__declspec(dllexport) void __stdcall Fbx_ReadModel(fbxsdk::FbxScene& scene, pr::geometry::fbx::IModelOut& out, pr::geometry::fbx::ReadModelOptions const& options)
	{
		using namespace pr::geometry::fbx;

		try
		{
			ReadModel(scene, out, options);
		}
		catch (std::exception const& ex)
		{
			OutputDebugStringA(ex.what());
			DebugBreak();
		}
	}

	// Dump info about the scene to 'out'
	__declspec(dllexport) void __stdcall Fbx_DumpScene(fbxsdk::FbxScene& scene, std::ostream& out)
	{
		using namespace pr::geometry::fbx;

		try
		{
			DumpScene(scene, out);
		}
		catch (std::exception const& ex)
		{
			OutputDebugStringA(ex.what());
			DebugBreak();
		}
	}

	// Round trip test an fbx scene
	__declspec(dllexport) void __stdcall Fbx_RoundTripTest(std::istream& src, std::ostream& out)
	{
		using namespace pr::geometry::fbx;

		try
		{
			Manager manager;
			ScenePtr scene = Import(manager, src);
			Export(manager, out, *scene.get());
		}
		catch (std::exception const& ex)
		{
			OutputDebugStringA(ex.what());
			DebugBreak();
		}
	}
}



#if 0

		// Read animation data
		void ReadAnimations()
		{
			int animations = m_scene.GetSrcObjectCount<FbxAnimStack>();
			for (int a = m_opts.m_anims.begin(); a != m_opts.m_anims.end(); ++a)
			{
				if (a < 0 || a >= animations)
					continue;

				// Tell the scene to use this animation stack
				auto& anim_stack = *m_scene.GetSrcObject<FbxAnimStack>(a);
				m_scene.SetCurrentAnimationStack(&anim_stack);

				// Get the time range
				auto& take = *m_scene.GetTakeInfo(anim_stack.GetName());

				for (int f = m_opts.m_frames.begin(); f != m_opts.m_frames.end(); ++f)
				{
					FbxTime startTime = tCurveX ? tCurveX->KeyGetTime(0) : FbxTime(0);
					FbxTime endTime = tCurveX ? tCurveX->KeyGetTime(tCurveX->KeyGetCount() - 1) : FbxTime(0);
					FbxTime time;
					FbxTime::EMode timeStep = FbxTime::eFrames30; // adjust for your framerate

					for (time = startTime; time <= endTime; time += timeStep)
					{
						FbxAMatrix globalTransform = node.EvaluateGlobalTransform(time);
						FbxVector4 translation = globalTransform.GetT();
						FbxVector4 rotation = globalTransform.GetR();

						// Store translation/rotation as a keyframe in your data structure
					}
					ReadAnimationFrame(*m_scene.GetRootNode(), *anim_layer);
				}
			}


				// Get the animation layer from this stack
				for (int l = 0, lend = anim_stack.GetMemberCount<FbxAnimLayer>(); i != lend; ++l)
				{
					auto& anim_layer = *FbxCast<FbxAnimLayer>(anim_stack.GetMember<FbxAnimLayer>(l));

				}
			}
		}

		//
		void ReadAnimation(FbxNode& node, FbxAnimLayer& anim_layer, FbxNode* parent = nullptr, int level = 0)
		{
			(void)parent,level;
			FbxAnimCurve* translation[3] = {
				node.LclTranslation.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_X),
				node.LclTranslation.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_Y),
				node.LclTranslation.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_Z),
			};
			FbxAnimCurve* rotation[3] = {
				node.LclRotation.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_X),
				node.LclRotation.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_Y),
				node.LclRotation.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_Z),
			};
			FbxAnimCurve* scaling[3] = {
				node.LclScaling.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_X),
				node.LclScaling.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_Y),
				node.LclScaling.GetCurve(&anim_layer, FBXSDK_CURVENODE_COMPONENT_Z),
			};
			//FbxTime startTime = tCurveX ? tCurveX->KeyGetTime(0) : FbxTime(0);
			//FbxTime endTime = tCurveX ? tCurveX->KeyGetTime(tCurveX->KeyGetCount() - 1) : FbxTime(0);
			//FbxTime time;
			//FbxTime::EMode timeStep = FbxTime::eFrames30; // adjust for your framerate

			//for (time = startTime; time <= endTime; time += timeStep)
			//{
			//	FbxAMatrix globalTransform = node.EvaluateGlobalTransform(time);
			//	FbxVector4 translation = globalTransform.GetT();
			//	FbxVector4 rotation = globalTransform.GetR();

			//	// Store translation/rotation as a keyframe in your data structure
			//}

			// Recurse
			for (int i = 0; i != node.GetChildCount(); ++i)
				ReadAnimation(*node.GetChild(i), anim_layer, &node, level + 1);
		}
void InspectAnimatedProperties(FbxNode* node, FbxAnimLayer* layer)
{
    FbxProperty property = node->GetFirstProperty();
    while (property.IsValid())
    {
        if (property.GetSrcObjectCount<FbxAnimCurve>() > 0)
        {
            const char* propName = property.GetNameAsCStr();
            FbxDataType dataType = property.GetPropertyDataType();

            printf("Animated property: %s (type: %s)\n", propName, dataType.GetName());

            int numComponents = property.GetSrcObjectCount<FbxAnimCurve>();
            for (int i = 0; i < numComponents; ++i)
            {
                FbxAnimCurve* curve = property.GetSrcObject<FbxAnimCurve>(i);
                if (!curve) continue;

                printf("  Curve %d has %d keys\n", i, curve->KeyGetCount());

                for (int k = 0; k < curve->KeyGetCount(); ++k)
                {
                    FbxTime time = curve->KeyGetTime(k);
                    float value = curve->KeyGetValue(k);
                    printf("    Time: %f, Value: %f\n", time.GetSecondDouble(), value);
                }
            }
        }

        property = node->GetNextProperty(property);
    }

    // Recurse children
    for (int i = 0; i < node->GetChildCount(); ++i)
        InspectAnimatedProperties(node->GetChild(i), layer);
}
void GetAnimationTimeRange(FbxAnimStack* animStack, FbxTime& outStart, FbxTime& outEnd)
{
    FbxTakeInfo* takeInfo = animStack->GetTakeInfo();
    if (takeInfo)
    {
        outStart = takeInfo->mLocalTimeSpan.GetStart();
        outEnd = takeInfo->mLocalTimeSpan.GetStop();
    }
    else
    {
        // Fallback: Walk through all nodes and find min/max times
        outStart = FbxTime::GetZero();
        outEnd = FbxTime(0);

        FbxAnimLayer* animLayer = animStack->GetMember<FbxAnimLayer>();
        // Call a recursive function to inspect all nodes and get the time span
        // (not shown here for brevity)
    }
}

		// Parse the FBX file
		void ReadGeometry(FbxNode& node, FbxNode* parent = nullptr, int level = 0)
		{
			// Determine the object to world and object to parent transforms
			auto o2w = To<m4x4>(node.EvaluateGlobalTransform());
			auto o2p = parent ? To<m4x4>(parent->EvaluateGlobalTransform().Inverse() * node.EvaluateGlobalTransform()) : m4x4::Identity();

			// Process all attributes of the node
			for (int i = 0, iend = node.GetNodeAttributeCount(); i != iend; ++i)
			{
				auto& attr = *node.GetNodeAttributeByIndex(i);
				auto attr_type = attr.GetAttributeType();
				switch (attr_type)
				{
					case FbxNodeAttribute::eMesh:
					{
						if (FbxMesh* mesh = FbxCast<FbxMesh>(&attr))
							if (FbxMesh* trimesh = EnsureTriangulated(mesh))
								ReadTriMesh(*trimesh, o2p, level);

						break;
					}
					case FbxNodeAttribute::eSkeleton:
					{
						//if (FbxSkeleton* skel = FbxCast<FbxSkeleton>(&attr))
						//	ReadSkeleton(*skel, o2p, level);

						break;
					}
					case FbxNodeAttribute::eLODGroup:
					{
						break;
					}
					case FbxNodeAttribute::eCamera:
					{
						break;
					}
					case FbxNodeAttribute::eLight:
					{
						break;
					}

					// Unsupported attributes
					case FbxNodeAttribute::eUnknown:
					case FbxNodeAttribute::eOpticalReference:
					case FbxNodeAttribute::eOpticalMarker:
					case FbxNodeAttribute::eCachedEffect:
					case FbxNodeAttribute::eMarker:
					case FbxNodeAttribute::eCameraStereo:
					case FbxNodeAttribute::eCameraSwitcher:
					case FbxNodeAttribute::eNurbs:
					case FbxNodeAttribute::ePatch:
					case FbxNodeAttribute::eNurbsCurve:
					case FbxNodeAttribute::eTrimNurbsSurface:
					case FbxNodeAttribute::eBoundary:
					case FbxNodeAttribute::eNurbsSurface:
					case FbxNodeAttribute::eSubDiv:
					case FbxNodeAttribute::eLine:
					{
						break;
					}
					case FbxNodeAttribute::eNull:
					default:
					{
						break;
					}
				}
			}

			// Recurse
			for (int i = 0, iend = node.GetChildCount(); i != iend; ++i)
				ReadGeometry(*node.GetChild(i), &node, level + 1);
		}

		// Read the materials used in 'mesh'
		void ReadMaterials(FbxMesh& mesh)
		{
			// Get the materials used by 'mesh'
			std::unordered_set<int> used_materials;
			if (auto material_layers = mesh.GetElementMaterial(); material_layers && !m_opts.all_materials)
			{
				auto& index = material_layers->GetIndexArray();
				switch (material_layers->GetMappingMode())
				{
					case FbxGeometryElement::eByPolygon:
					{
						// Expect a material index for each face
						if (index.GetCount() != mesh.GetPolygonCount())
						{
							m_errors.push_back(std::format("Mesh {} has the 'eByPolygon' material mapping, but does not have a material for each polygon ({} expected, {} available)", mesh.GetName(), mesh.GetPolygonCount(), index.GetCount()));
							break;
						}

						for (int f = 0, fend = mesh.GetPolygonCount(); f != fend; ++f)
							used_materials.insert(index.GetAt(f));

						break;
					}
					case FbxGeometryElement::eAllSame:
					{
						if (index.GetCount() != 1)
						{
							m_errors.push_back(std::format("Mesh {} has the 'eAllSame' material mapping, but there is an unexpected number ({}) of materials", mesh.GetName(), index.GetCount()));
							break;
						}

						used_materials.insert(index.GetAt(0));
						break;
					}
				}
			}

			// Output the materials
			for (int i = 0, iend = mesh.GetNode()->GetMaterialCount(); i != iend; ++i)
			{
				auto const& material = *mesh.GetNode()->GetMaterial(i);
				if (!m_opts.all_materials && !used_materials.count(i))
					continue;

				(void)material;
				m_out.AddMaterial(i, ColourWhite);//hack
			}
		}

	// RAII wrapper for FBX Exporter
	struct Exporter
	{
		Manager& m_manager;
		FbxExporter* m_exporter;

		Exporter(Manager& manager, Settings const& settings)
			: m_manager(manager)
			, m_exporter(FbxExporter::Create(m_manager, ""))
		{
			m_exporter->SetIOSettings(settings);
		}
		Exporter(Exporter&&) = delete;
		Exporter(Exporter const&) = delete;
		Exporter& operator=(Exporter&&) = delete;
		Exporter& operator=(Exporter const&) = delete;
		~Exporter()
		{
			if (m_exporter != nullptr)
				m_exporter->Destroy();
		}

		// Choose an output format
		static int ChooseFormat(FbxManager& manager, int format = -1)
		{
			auto& plugin_registry = *manager.GetIOPluginRegistry();
			if (format >= 0 && format < plugin_registry.GetWriterFormatCount())
				return format;

			// Try to export in ASCII if possible
			auto format_count = plugin_registry.GetWriterFormatCount();
			for (int i = 0; i != format_count; ++i)
			{
				if (!plugin_registry.WriterIsFBX(i))
					continue;

				FbxString desc = plugin_registry.GetWriterFormatDescription(i);
				if (desc.Find("ascii") < 0)
					continue;

				return i;
			}

			// Write in fall-back format if no ASCII format is found
			return plugin_registry.GetNativeWriterFormat();
		}

		// Export a scene
		void Export(Scene const& scene, std::filesystem::path const& filepath, int format = -1)
		{
			// Choose a format
			format = ChooseFormat(*m_manager, format);

			// Initialise the exporter
			if (!m_exporter->Initialize(filepath.string().c_str(), format))
				throw std::runtime_error(std::format("FbxExporter::Initialize() failed. {}", m_exporter->GetStatus().GetErrorString()));

			// Do the export
			auto status = m_exporter->Export(scene);
			if (status != 0)
				throw std::runtime_error("Export failed");
		}

		FbxExporter* operator ->() const
		{
			return m_exporter;
		}
		operator FbxExporter* () const
		{
			return m_exporter;
		}
	};

	// RAII wrapper for FBX Importer
	struct Importer
	{
		Manager& m_manager;
		FbxImporter* m_importer;

		Importer(Manager& manager, Settings const& settings)
			: m_manager(manager)
			, m_importer(Check(FbxImporter::Create(m_manager, ""), "Failed to create Importer"))
		{
			m_importer->SetIOSettings(settings);
		}
		Importer(Importer&&) = delete;
		Importer(Importer const&) = delete;
		Importer& operator=(Importer&&) = delete;
		Importer& operator=(Importer const&) = delete;
		~Importer()
		{
			if (m_importer != nullptr)
				m_importer->Destroy();
		}

		// Initialise the importer with a data source
		// After this call it is possible to access animation stack information without the expense of loading the entire scene.
		void Source(std::filesystem::path const& filepath, char const* format = Formats::FbxBinary, std::vector<std::string>* errors = nullptr)
		{
			// Initialize the importer by providing a filename.
			if (!m_importer->Initialize(filepath.string().c_str(), m_manager.FileFormatID(format)))
			{
				FbxArray<FbxString*> history;
				m_importer->GetStatus().GetErrorStringHistory(history);

				// Record any errors
				if (errors != nullptr)
				{
					for (int i = 0; i != history.GetCount(); ++i)
						errors->push_back(history[i]->Buffer());
				}

				auto status = m_importer->GetStatus();
				switch (status.GetCode())
				{
					case FbxStatus::eInvalidFileVersion:
					{
						FbxVersion sdk, file;
						FbxManager::GetFileFormatVersion(sdk.Major, sdk.Minor, sdk.Revs);
						m_importer->GetFileVersion(file.Major, file.Minor, file.Revs);
						throw std::runtime_error(std::format("Unsupported file version '{}.{}.{}'. SDK Version supports '{}.{}.{}'", file.Major, file.Minor, file.Revs, sdk.Major, sdk.Minor, sdk.Revs));
					}
					default:
					{
						throw std::runtime_error(std::format("FbxImporter::Initialize() failed. {}", status.GetErrorString()));
					}
				}
			}

			// Check the file is actually an fbx file
			if (!m_importer->IsFBX())
				throw std::runtime_error(std::format("Imported file is not an FBX file"));
		}
		void Source(Stream& src, char const* format = Formats::FbxBinary, std::vector<std::string>* errors = nullptr)
		{
			if (!m_importer->Initialize(&src, nullptr, m_manager.FileFormatID(format)))
			{
				FbxArray<FbxString*> history;
				m_importer->GetStatus().GetErrorStringHistory(history);

				// Record any errors
				if (errors != nullptr)
				{
					for (int i = 0; i != history.GetCount(); ++i)
						errors->push_back(history[i]->Buffer());
				}

				auto status = m_importer->GetStatus();
				switch (status.GetCode())
				{
					case FbxStatus::eInvalidFileVersion:
					{
						FbxVersion sdk, file;
						FbxManager::GetFileFormatVersion(sdk.Major, sdk.Minor, sdk.Revs);
						m_importer->GetFileVersion(file.Major, file.Minor, file.Revs);
						throw std::runtime_error(std::format("Unsupported file version '{}.{}.{}'. SDK Version supports '{}.{}.{}'", file.Major, file.Minor, file.Revs, sdk.Major, sdk.Minor, sdk.Revs));
					}
					default:
					{
						throw std::runtime_error(std::format("FbxImporter::Initialize() failed. {}", status.GetErrorString()));
					}
				}
			}

			// Check the file is actually an fbx file
			if (!m_importer->IsFBX())
				throw std::runtime_error(std::format("Imported file is not an FBX file"));
		}

		// Read the entire scene into memory
		void LoadScene(Scene& scene)
		{
			// This cannot create the scene and return it because of the crappy memory management in 'fbxsdk.dll'.
			// The importer is intended to be a short lived object that is destroyed before the scenes.

			// Import the scene.
			auto result = m_importer->Import(scene.m_scene);
			if (result && m_importer->GetStatus() == FbxStatus::eSuccess)
				return;
			
			throw std::runtime_error("Failed to read fbx from file");
		}

		// Access the animation stack
		void AnimationStack()
		{
			//m_out << "Animation Stack Information\n";

			//auto lAnimStackCount = m_importer->GetAnimStackCount();

			//m_out << "    Number of Animation Stacks: %d\n", lAnimStackCount;
			//m_out << "    Current Animation Stack: \"%s\"\n", lImporter->GetActiveAnimStackName().Buffer();
			//m_out << "\n";

			//for (int i = 0; i < lAnimStackCount; i++)
			//{
			//	FbxTakeInfo* lTakeInfo = lImporter->GetTakeInfo(i);

			//	m_out << "    Animation Stack %d\n", i;
			//	m_out << "         Name: \"%s\"\n", lTakeInfo->mName.Buffer();
			//	m_out << "         Description: \"%s\"\n", lTakeInfo->mDescription.Buffer();

			//	// Change the value of the import name if the animation stack should be imported 
			//	// under a different name.
			//	m_out << "         Import Name: \"%s\"\n", lTakeInfo->mImportName.Buffer();

			//	// Set the value of the import state to false if the animation stack should be not
			//	// be imported. 
			//	m_out << "         Import State: %s\n", lTakeInfo->mSelect ? "true" : "false";
			//	m_out << "\n";
			//}
		}

		FbxImporter* operator ->() const
		{
			return m_importer;
		}
		operator FbxImporter* () const
		{
			return m_importer;
		}
	};
#endif