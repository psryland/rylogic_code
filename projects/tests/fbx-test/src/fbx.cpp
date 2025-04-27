//*************************************************************************************************
// Fbx
// Rylogic (C) 2025
//*************************************************************************************************
#include <stdexcept>
#include <string_view>
#include <format>
#include <fbxsdk.h>
#include "src/fbx.h"

using namespace fbxsdk;

namespace pr::fbx
{
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
		FbxArray()
			: fbxsdk::FbxArray<T>()
		{
		}
		~FbxArray()
		{
			FbxArrayDelete<T>(*this);
		}
	};

	// Manager ----------------------------------------------------------------------------------------

	Manager::Manager()
		: m_manager(Check(FbxManager::Create(), "Error: Unable to create FBX Manager"))
		, m_version(m_manager->GetVersion())
	{
		//#ifndef FBXSDK_ENV_WINSTORE
		////Load plugins from the executable directory (optional)
		//FbxString lPath = FbxGetApplicationDirectory();
		//m_manager->LoadPluginsDirectory(lPath.Buffer());
		//#endif

	}
	Manager::~Manager()
	{
		// Delete the FBX Manager. All the objects that have been allocated using the FBX Manager and that haven't been explicitly destroyed are also automatically destroyed.
		if (m_manager != nullptr)
			m_manager->Destroy();
	}

	// Settings -----------------------------------------------------------------------------------

	Settings::Settings(Manager& manager)
		: m_settings(Check(FbxIOSettings::Create(manager.m_manager, IOSROOT), "Error: Unable to create settings"))
	{
		// Set the export states. By default, the export states are always set to 
		// true except for the option eEXPORT_TEXTURE_AS_EMBEDDED. The code below 
		// shows how to change these states.
		//m_settings->SetBoolProp(EXP_FBX_MATERIAL, true);
		//m_settings->SetBoolProp(EXP_FBX_TEXTURE, true);
		//m_settings->SetBoolProp(EXP_FBX_EMBEDDED, embed_media);
		//m_settings->SetBoolProp(EXP_FBX_SHAPE, true);
		//m_settings->SetBoolProp(EXP_FBX_GOBO, true);
		//m_settings->SetBoolProp(EXP_FBX_ANIMATION, true);
		//m_settings->SetBoolProp(EXP_FBX_GLOBAL_SETTINGS, true);
	}
	Settings::~Settings()
	{
		if (m_settings != nullptr)
			m_settings->Destroy();
	}

	// Get/set the password
	std::string Settings::Password() const
	{
		FbxString pw = m_settings->GetStringProp(IMP_FBX_PASSWORD, "");
		return pw.Buffer();
	}
	void Settings::Password(std::string_view password)
	{
		FbxString pw(password.data(), password.size());
		m_settings->SetStringProp(IMP_FBX_PASSWORD, pw);
		m_settings->SetBoolProp(IMP_FBX_PASSWORD_ENABLE, !password.empty());
	}

	// Scene --------------------------------------------------------------------------------------

	Scene::Scene(Manager& manager)
		: m_scene(Check(FbxScene::Create(manager, ""), "Error: Unable to create FBX scene"))
	{}
	Scene::~Scene()
	{
		if (m_scene)
			m_scene->Destroy();
	}
	Scene::Scene(Scene&& rhs)
		: m_scene(nullptr)
	{
		std::swap(m_scene, rhs.m_scene);
	}
	Scene& Scene::operator=(Scene&& rhs)
	{
		if (this == &rhs) return *this;
		std::swap(m_scene, rhs.m_scene);
		return *this;
	}

	// Exporter -----------------------------------------------------------------------------------

	Exporter::Exporter(Manager& manager, Settings const& settings)
		: m_manager(manager)
		, m_exporter(FbxExporter::Create(m_manager, ""))
	{
		m_exporter->SetIOSettings(settings);
	}
	Exporter::~Exporter()
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
	void Exporter::Export(Scene& scene, std::filesystem::path const& filepath, int format)
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

	// Importer -----------------------------------------------------------------------------------

	Importer::Importer(Manager& manager, Settings const& settings)
		: m_manager(manager)
		, m_importer(Check(FbxImporter::Create(m_manager, ""), "Failed to create Importer"))
	{
		m_importer->SetIOSettings(settings);
	}
	Importer::~Importer()
	{
		if (m_importer != nullptr)
			m_importer->Destroy();
	}

	// Load an fbx scene
	Scene Importer::Import(std::filesystem::path const& filepath, std::vector<std::string>* errors)
	{
		// Initialize the importer by providing a filename.
		if (!m_importer->Initialize(filepath.string().c_str()))
		{
			FbxVersion sdk, file;
			FbxManager::GetFileFormatVersion(sdk.Major, sdk.Minor, sdk.Revs);
			m_importer->GetFileVersion(file.Major, file.Minor, file.Revs);
			if (m_importer->GetStatus().GetCode() == FbxStatus::eInvalidFileVersion)
				throw std::runtime_error(std::format("Unsupported file version '{}.{}.{}'. SDK Version supports '{}.{}.{}'", file.Major, file.Minor, file.Revs, sdk.Major, sdk.Minor, sdk.Revs));
			else
				throw std::runtime_error(std::format("FbxImporter::Initialize() failed. {}", m_importer->GetStatus().GetErrorString()));
		}

		// Check the file is actually an fbx file
		if (m_importer->IsFBX())
		{
			// From this point, it is possible to access animation stack information without the expense of loading the entire file.
			//FBXSDK_printf("Animation Stack Information\n");

			//auto lAnimStackCount = m_importer->GetAnimStackCount();

			//FBXSDK_printf("    Number of Animation Stacks: %d\n", lAnimStackCount);
			//FBXSDK_printf("    Current Animation Stack: \"%s\"\n", lImporter->GetActiveAnimStackName().Buffer());
			//FBXSDK_printf("\n");

			//for (int i = 0; i < lAnimStackCount; i++)
			//{
			//	FbxTakeInfo* lTakeInfo = lImporter->GetTakeInfo(i);

			//	FBXSDK_printf("    Animation Stack %d\n", i);
			//	FBXSDK_printf("         Name: \"%s\"\n", lTakeInfo->mName.Buffer());
			//	FBXSDK_printf("         Description: \"%s\"\n", lTakeInfo->mDescription.Buffer());

			//	// Change the value of the import name if the animation stack should be imported 
			//	// under a different name.
			//	FBXSDK_printf("         Import Name: \"%s\"\n", lTakeInfo->mImportName.Buffer());

			//	// Set the value of the import state to false if the animation stack should be not
			//	// be imported. 
			//	FBXSDK_printf("         Import State: %s\n", lTakeInfo->mSelect ? "true" : "false");
			//	FBXSDK_printf("\n");
			//}

			//// Set the import states. By default, the import states are always set to 
			//// true. The code below shows how to change these states.
			//m_settings->SetBoolProp(IMP_FBX_MATERIAL, true);
			//m_settings->SetBoolProp(IMP_FBX_TEXTURE, true);
			//m_settings->SetBoolProp(IMP_FBX_LINK, true);
			//m_settings->SetBoolProp(IMP_FBX_SHAPE, true);
			//m_settings->SetBoolProp(IMP_FBX_GOBO, true);
			//m_settings->SetBoolProp(IMP_FBX_ANIMATION, true);
			//m_settings->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, true);
		}

		// Import the scene.
		Scene scene(m_manager);
		auto result = m_importer->Import(scene.m_scene);
		if (result && m_importer->GetStatus() == FbxStatus::eSuccess)
			return scene;

		// Record any errors
		if (errors != nullptr)
		{
			FbxArray<FbxString*> history;
			m_importer->GetStatus().GetErrorStringHistory(history);
			for (int i = 0; i != history.GetCount(); ++i)
				errors->push_back(history[i]->Buffer());
		}

		throw std::runtime_error("Failed to read file");
	}

	// Debug -----------------------------------------------------------------------------------------

	void WriteContent(Scene const& scene, std::ostream& out)
	{
		struct Writer
		{
			std::ostream& m_out;
			Writer(std::ostream& out)
				: m_out(out)
			{}

			// Display the content of an fbx node (recursive)
			void Write(FbxNode const& node)
			{
				if (node.GetNodeAttribute() != nullptr)
				{
					switch (node.GetNodeAttribute()->GetAttributeType())
					{
#if 0
						case FbxNodeAttribute::eMarker:
							DisplayMarker(pNode);
							break;

						case FbxNodeAttribute::eSkeleton:
							DisplaySkeleton(pNode);
							break;

						case FbxNodeAttribute::eMesh:
							DisplayMesh(pNode);
							break;

						case FbxNodeAttribute::eNurbs:
							DisplayNurb(pNode);
							break;

						case FbxNodeAttribute::ePatch:
							DisplayPatch(pNode);
							break;

						case FbxNodeAttribute::eCamera:
							DisplayCamera(pNode);
							break;

						case FbxNodeAttribute::eLight:
							DisplayLight(pNode);
							break;

						case FbxNodeAttribute::eLODGroup:
							DisplayLodGroup(pNode);
							break;
#endif
						default:
						{
							break;
						}
					}
					//m_out << "NULL Node Attribute\n\n";
					//return;
				}

				// ??
				WriteUserProperties(node);
#if 0
				DisplayTarget(pNode);
				DisplayPivotsAndLimits(pNode);
				DisplayTransformPropagation(pNode);
				DisplayGeometricTransform(pNode);
#endif
				// Recurse
				for (int i = 0; i != node.GetChildCount(); ++i)
					Write(*node.GetChild(i));
			}
			void WriteUserProperties(fbxsdk::FbxObject const& node)
			{
				m_out << "    User Properties:\n"; int i = 0;
				for (FbxProperty prop = node.GetFirstProperty(); prop.IsValid(); prop = node.GetNextProperty(prop), ++i)
				{
					if (!prop.GetFlag(FbxPropertyFlags::eUserDefined))
						continue;

					m_out << "        Property " << i << "\n";
					m_out << "            Display Name: " << prop.GetLabel().Buffer() << "\n";
					m_out << "            Internal Name: " << prop.GetName().Buffer() << "\n";
					m_out << "            Type: " << prop.GetPropertyDataType().GetName() << "\n";
					if (prop.HasMinLimit()) m_out << "            Min Limit: " << prop.GetMinLimit() << "\n";
					if (prop.HasMaxLimit()) m_out << "            Max Limit: " << prop.GetMaxLimit() << "\n";
					m_out << "            Is Animatable: " << prop.GetFlag(FbxPropertyFlags::eAnimatable) << "\n";

					switch (prop.GetPropertyDataType().GetType())
					{
						case EFbxType::eFbxBool:
						{
							m_out << "            Default Value: " << prop.Get<FbxBool>() << "\n";
							break;
						}
						case EFbxType::eFbxDouble:
						case EFbxType::eFbxFloat:
						{
							m_out << "            Default Value: " << prop.Get<FbxDouble>() << "\n";
							break;
						}
						case EFbxType::eFbxInt:
						{
							m_out << "            Default Value: " << prop.Get<FbxInt>() << "\n";
							break;
						}
						case EFbxType::eFbxDouble3:
						case EFbxType::eFbxDouble4:
						{
							auto xyz = prop.Get<FbxDouble3>();
							m_out << std::format("            Default Value: X={}, Y={}, Z={}", xyz[0], xyz[1], xyz[2]) << "\n";
							break;
						}
						case EFbxType::eFbxEnum:
						{
							m_out << "            Default Value: " << prop.Get<FbxEnum>() << "\n";
							break;
						}
						default:
						{
#if 0
							if (prop.GetPropertyDataType().Is(fbxsdk::FbxColor3DT) || prop.GetPropertyDataType().Is(fbxsdk::FbxColor4DT))
							{
								auto rgba = prop.Get<FbxColor>();
								m_out << std::format("            Default Value: R={}, G={}, B={}, A={}", rgba.mRed, rgba.mGreen, rgba.mBlue, rgba.mAlpha) << "\n";
								break;
							}
#endif
							m_out << "            Default Value: UNIDENTIFIED" << "\n";
							break;
						}
					}
				}
			}
		};

		FbxNode* node = scene.m_scene->GetRootNode();
		if (node == nullptr)
			return;

		Writer writer(out);
		writer.Write(*node);
	}
}
