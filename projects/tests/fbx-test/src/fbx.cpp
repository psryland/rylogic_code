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

	inline double FloatClamp(double f)
	{
		return
			f >= +HUGE_VAL ? +std::numeric_limits<double>::infinity() :
			f <= -HUGE_VAL ? -std::numeric_limits<double>::infinity() :
			f;
	}
	std::ostream& operator << (std::ostream& out, fbxsdk::FbxVector2 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]);
	}
	std::ostream& operator << (std::ostream& out, fbxsdk::FbxVector4 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]) << ", " << FloatClamp(vec[2]) << ", " << FloatClamp(vec[3]);
	}
	std::ostream& operator << (std::ostream& out, fbxsdk::FbxDouble3 const& vec)
	{
		return out << FloatClamp(vec[0]) << ", " << FloatClamp(vec[1]) << ", " << FloatClamp(vec[2]);
	}
	std::ostream& operator << (std::ostream& out, fbxsdk::FbxColor const& col)
	{
		return out << "R=" << (float)col.mRed << ", G=" << (float)col.mGreen << ", B=" << (float)col.mBlue << ", A=" << col.mAlpha << "\n";
	}

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
						case FbxNodeAttribute::eMarker:
						{
							WriteMarker(node);
							break;
						}
						case FbxNodeAttribute::eSkeleton:
						{
							WriteSkeleton(node);
							break;
						}
						case FbxNodeAttribute::eMesh:
						{
							WriteMesh(node);
							break;
						}
						case FbxNodeAttribute::eNurbs:
						{
							WriteNurb(node);
							break;
						}
#if 0
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
				WriteTarget(node);
				WritePivotsAndLimits(node);
				WriteTransformPropagation(node);
				WriteGeometricTransform(node);

				// Recurse
				for (int i = 0; i != node.GetChildCount(); ++i)
					Write(*node.GetChild(i));
			}
			void WriteMarker(FbxNode const& node)
			{
				auto const& marker = *static_cast<FbxMarker const*>(node.GetNodeAttribute());

				m_out << "Marker Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(marker);

				// Type
				m_out << "    Marker Type: ";
				switch (marker.GetType())
				{
					case FbxMarker::eStandard:   m_out << "Standard\n";    break;
					case FbxMarker::eOptical:    m_out << "Optical\n";     break;
					case FbxMarker::eEffectorIK: m_out << "IK Effector\n"; break;
					case FbxMarker::eEffectorFK: m_out << "FK Effector\n"; break;
				}

				// Look
				m_out << "    Marker Look: ";
				switch (marker.Look.Get())
				{
					default: break;
					case FbxMarker::eCube:        m_out << "Cube\n";        break;
					case FbxMarker::eHardCross:   m_out << "Hard Cross\n";  break;
					case FbxMarker::eLightCross:  m_out << "Light Cross\n"; break;
					case FbxMarker::eSphere:      m_out << "Sphere\n";      break;
				}

				// Size
				m_out << "    Size: " << marker.Size.Get() << "\n";

				// Color
				FbxDouble3 c = marker.Color.Get();
				FbxColor color(c[0], c[1], c[2]);
				m_out << "    Color: " << color << "\n";

				// IKPivot
				m_out << "    IKPivot: " << marker.IKPivot.Get() << "\n";
			}
			void WriteSkeleton(FbxNode const& node)
			{
				auto const& skel = *static_cast<FbxSkeleton const*>(node.GetNodeAttribute());

				m_out << "Skeleton Name: " << skel.GetName() << "\n";
				WriteMetaDataConnections(skel);

				const char* skel_types[] = { "Root", "Limb", "Limb Node", "Effector" };
				m_out << "    Type: " << skel_types[skel.GetSkeletonType()] << "\n";

				switch (skel.GetSkeletonType())
				{
					case FbxSkeleton::eLimb:
					{
						m_out << "    Limb Length: " << FloatClamp(skel.LimbLength.Get()) << "\n";
						break;
					}
					case FbxSkeleton::eLimbNode:
					{
						m_out << "    Limb Node Size: " << FloatClamp(skel.Size.Get()) << "\n";
						break;
					}
					case FbxSkeleton::eRoot:
					{
						m_out << "    Limb Root Size: " << FloatClamp(skel.Size.Get()) << "\n";
						break;
					}
				}
				m_out << "    Color: " << skel.GetLimbNodeColor() << "\n";
			}
			void WriteMesh(FbxNode const& node)
			{
				auto const& lMesh = *static_cast<FbxMesh const*>(node.GetNodeAttribute());

				m_out << "Mesh Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(lMesh);
				#if 0
				WriteControlsPoints(lMesh);
				WritePolygons(lMesh);
				WriteMaterialMapping(lMesh);
				WriteMaterial(lMesh);
				WriteTexture(lMesh);
				WriteMaterialConnections(lMesh);
				WriteLink(lMesh);
				WriteShape(lMesh);
				WriteCache(lMesh);
				#endif
			}
			void WriteNurb(FbxNode const& node)
			{
				auto const& nurbs = *static_cast<FbxNurbs const*>(node.GetNodeAttribute());

				m_out << "Nurb Name: " << node.GetName() << "\n";
				WriteMetaDataConnections(nurbs);

				const char* modes[] = { "Raw", "Low No Normals", "Low", "High No Normals", "High" };
				m_out << "    Surface Mode: " << modes[nurbs.GetSurfaceMode()] << "\n";

				auto lControlPointsCount = nurbs.GetControlPointsCount();
				auto lControlPoints = nurbs.GetControlPoints();

				for (int i = 0; i != lControlPointsCount; ++i)
				{
					m_out << "    Control Point " << i << "\n";
					m_out << "        Coordinates: " << lControlPoints[i] << "\n";
					m_out << "        Weight: " << lControlPoints[i][3] << "\n";
				}

				const char* nurb_types[] = { "Periodic", "Closed", "Open" };

				m_out << "    Nurb U Type: " << nurb_types[nurbs.GetNurbsUType()] << "\n";
				m_out << "    U Count: " << nurbs.GetUCount() << "\n";
				m_out << "    Nurb V Type: " << nurb_types[nurbs.GetNurbsVType()] << "\n";
				m_out << "    V Count: " << nurbs.GetVCount() << "\n";
				m_out << "    U Order: " << nurbs.GetUOrder() << "\n";
				m_out << "    V Order: " << nurbs.GetVOrder() << "\n";
				m_out << "    U Step: " << nurbs.GetUStep() << "\n";
				m_out << "    V Step: " << nurbs.GetVStep() << "\n";

				FbxString lString;
				double* lUKnotVector = nurbs.GetUKnotVector();
				double* lVKnotVector = nurbs.GetVKnotVector();
				int* lUMultiplicityVector = nurbs.GetUMultiplicityVector();
				int* lVMultiplicityVector = nurbs.GetVMultiplicityVector();

				m_out << "    U Knot Vector: ";
				for (int i = 0, lUKnotCount = nurbs.GetUKnotCount(); i != lUKnotCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << (float)lUKnotVector[i];
				}
				m_out << "\n";
				m_out << "    V Knot Vector: ";
				for (int i = 0, lVKnotCount = nurbs.GetVKnotCount(); i != lVKnotCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << (float)lVKnotVector[i];
				}
				m_out << "\n";
				m_out << "    U Multiplicity Vector: ";
				for (int i = 0, lUMultiplicityCount = nurbs.GetUCount(); i != lUMultiplicityCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << lUMultiplicityVector[i];
				}
				m_out << "\n";
				m_out << "    V Multiplicity Vector: ";
				for (int i = 0, lVMultiplicityCount = nurbs.GetVCount(); i != lVMultiplicityCount; ++i)
				{
					if (i != 0) m_out << ", ";
					m_out << lVMultiplicityVector[i];
				}
				m_out << "\n";
				m_out << "\n";

#if 0
				WriteTexture(nurbs);
				WriteMaterial(nurbs);
				WriteLink(nurbs);
				WriteShape(nurbs);
				WriteCache(nurbs);
#endif
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
							if (prop.GetPropertyDataType().Is(fbxsdk::FbxColor3DT) || prop.GetPropertyDataType().Is(fbxsdk::FbxColor4DT))
							{
								auto rgba = prop.Get<FbxColor>();
								m_out << std::format("            Default Value: R={}, G={}, B={}, A={}", rgba.mRed, rgba.mGreen, rgba.mBlue, rgba.mAlpha) << "\n";
								break;
							}
#if 0
#endif
							m_out << "            Default Value: UNIDENTIFIED" << "\n";
							break;
						}
					}
				}
			}
			void WriteTarget(FbxNode const& node)
			{
				if (node.GetTarget() != nullptr)
				{
					m_out << "    Target Name: " << node.GetTarget()->GetName() << "\n";
				}
			}
			void WritePivotsAndLimits(FbxNode const& node)
			{
				FbxVector4 lTmpVector;

				// Pivots
				m_out << "    Pivot Information\n";

				FbxNode::EPivotState pivot_state;
				node.GetPivotState(FbxNode::eSourcePivot, pivot_state);
				m_out << std::format("        Pivot State: {}\n", pivot_state == FbxNode::ePivotActive ? "Active" : "Reference");

				lTmpVector = node.GetPreRotation(FbxNode::eSourcePivot);
				m_out << std::format("        Pre-Rotation: {} {} {}\n", lTmpVector[0], lTmpVector[1], lTmpVector[2]);

				lTmpVector = node.GetPostRotation(FbxNode::eSourcePivot);
				m_out << std::format("        Post-Rotation: {} {} {}\n", lTmpVector[0], lTmpVector[1], lTmpVector[2]);

				lTmpVector = node.GetRotationPivot(FbxNode::eSourcePivot);
				m_out << std::format("        Rotation Pivot: {} {} {}\n", lTmpVector[0], lTmpVector[1], lTmpVector[2]);

				lTmpVector = node.GetRotationOffset(FbxNode::eSourcePivot);
				m_out << std::format("        Rotation Offset: {} {} {}\n", lTmpVector[0], lTmpVector[1], lTmpVector[2]);

				lTmpVector = node.GetScalingPivot(FbxNode::eSourcePivot);
				m_out << std::format("        Scaling Pivot: {} {} {}\n", lTmpVector[0], lTmpVector[1], lTmpVector[2]);

				lTmpVector = node.GetScalingOffset(FbxNode::eSourcePivot);
				m_out << std::format("        Scaling Offset: {} {} {}\n", lTmpVector[0], lTmpVector[1], lTmpVector[2]);

				// Limits
				bool		lIsActive, lMinXActive, lMinYActive, lMinZActive;
				bool		lMaxXActive, lMaxYActive, lMaxZActive;
				FbxDouble3	lMinValues, lMaxValues;

				m_out << "    Limits Information\n";

				lIsActive = node.TranslationActive;
				lMinXActive = node.TranslationMinX;
				lMinYActive = node.TranslationMinY;
				lMinZActive = node.TranslationMinZ;
				lMaxXActive = node.TranslationMaxX;
				lMaxYActive = node.TranslationMaxY;
				lMaxZActive = node.TranslationMaxZ;
				lMinValues = node.TranslationMin;
				lMaxValues = node.TranslationMax;

				m_out << "        Translation limits: " << (lIsActive ? "Active" : "Inactive") << "\n";
				m_out << "            X\n";
				m_out << "                Min Limit: " << (lMinXActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[0] << "\n";
				m_out << "                Max Limit: " << (lMaxXActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[0] << "\n";
				m_out << "            Y\n";
				m_out << "                Min Limit: " << (lMinYActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[1] << "\n";
				m_out << "                Max Limit: " << (lMaxYActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[1] << "\n";
				m_out << "            Z\n";
				m_out << "                Min Limit: " << (lMinZActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[2] << "\n";
				m_out << "                Max Limit: " << (lMaxZActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[2] << "\n";

				lIsActive = node.RotationActive;
				lMinXActive = node.RotationMinX;
				lMinYActive = node.RotationMinY;
				lMinZActive = node.RotationMinZ;
				lMaxXActive = node.RotationMaxX;
				lMaxYActive = node.RotationMaxY;
				lMaxZActive = node.RotationMaxZ;
				lMinValues = node.RotationMin;
				lMaxValues = node.RotationMax;

				m_out << "        Rotation limits: " << (lIsActive ? "Active" : "Inactive") << "\n";
				m_out << "            X\n";
				m_out << "                Min Limit: " <<  (lMinXActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[0] << "\n";
				m_out << "                Max Limit: " << (lMaxXActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[0] << "\n";
				m_out << "            Y\n";
				m_out << "                Min Limit: " << (lMinYActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[1] << "\n";
				m_out << "                Max Limit: " << (lMaxYActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[1] << "\n";
				m_out << "            Z\n";
				m_out << "                Min Limit: " << (lMinZActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[2] << "\n";
				m_out << "                Max Limit: " << (lMaxZActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[2] << "\n";

				lIsActive = node.ScalingActive;
				lMinXActive = node.ScalingMinX;
				lMinYActive = node.ScalingMinY;
				lMinZActive = node.ScalingMinZ;
				lMaxXActive = node.ScalingMaxX;
				lMaxYActive = node.ScalingMaxY;
				lMaxZActive = node.ScalingMaxZ;
				lMinValues = node.ScalingMin;
				lMaxValues = node.ScalingMax;

				m_out << "        Scaling limits: " << (lIsActive ? "Active" : "Inactive") << "\n";
				m_out << "            X\n";
				m_out << "                Min Limit: " << (lMinXActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[0] << "\n";
				m_out << "                Max Limit: " << (lMaxXActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[0] << "\n";
				m_out << "            Y\n";
				m_out << "                Min Limit: " << (lMinYActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[1] << "\n";
				m_out << "                Max Limit: " << (lMaxYActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[1] << "\n";
				m_out << "            Z\n";
				m_out << "                Min Limit: " << (lMinZActive ? "Active" : "Inactive") << "\n";
				m_out << "                Min Limit Value: " << lMinValues[2] << "\n";
				m_out << "                Max Limit: " << (lMaxZActive ? "Active" : "Inactive") << "\n";
				m_out << "                Max Limit Value: " << lMaxValues[2] << "\n";
			}
			void WriteTransformPropagation(FbxNode const& node)
			{
				m_out << "    Transformation Propagation\n";

				// Rotation Space
				FbxEuler::EOrder order;
				node.GetRotationOrder(FbxNode::eSourcePivot, order);

				m_out << "        Rotation Space: ";
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
				m_out << std::format("        Use the Rotation Space for Limit specification only: {}\n", node.GetUseRotationSpaceForLimitOnly(FbxNode::eSourcePivot) ? "Yes" : "No");

				// Inherit Type
				FbxTransform::EInheritType inherit_type;
				node.GetTransformationInheritType(inherit_type);

				m_out << "        Transformation Inheritance: ";
				switch (inherit_type)
				{
					case FbxTransform::eInheritRrSs: m_out << "RrSs\n"; break;
					case FbxTransform::eInheritRSrs: m_out << "RSrs\n"; break;
					case FbxTransform::eInheritRrs: m_out << "Rrs\n"; break;
				}
			}
			void WriteGeometricTransform(FbxNode const& node)
			{
				m_out << "    Geometric Transformations\n";

				auto xyz = node.GetGeometricTranslation(FbxNode::eSourcePivot);
				m_out << std::format("        Translation: {} {} {}\n", xyz[0], xyz[1], xyz[2]);

				auto rot = node.GetGeometricRotation(FbxNode::eSourcePivot);
				m_out << std::format("        Rotation:    {} {} {}\n", rot[0], rot[1], rot[2]);

				auto scl = node.GetGeometricScaling(FbxNode::eSourcePivot);
				m_out << std::format("        Scaling:     {} {} {}\n", scl[0], scl[1], scl[2]);
			}
			void WriteMetaDataConnections(FbxObject const& node)
			{
				int nbMetaData = node.GetSrcObjectCount<FbxObjectMetaData>();
				if (nbMetaData > 0)
					m_out << "    MetaData connections " << "\n";

				for (int i = 0; i < nbMetaData; i++)
				{
					FbxObjectMetaData* metaData = node.GetSrcObject<FbxObjectMetaData>(i);
					m_out << "        Name: " << metaData->GetName() << "\n";
				}
			}
			void WriteTextureInfo(FbxTexture const& pTexture, int pBlendMode)
			{
				FbxFileTexture const* lFileTexture = FbxCast<FbxFileTexture>(&pTexture);
				FbxProceduralTexture const* lProceduralTexture = FbxCast<FbxProceduralTexture>(&pTexture);

				m_out << "            Name: \"" << pTexture.GetName() << "\"\n";
				if (lFileTexture)
				{
					m_out << "            Type: File Texture" << "\n";
					m_out << "            File Name: \"" << lFileTexture->GetFileName() << "\"\n";
				}
				else if (lProceduralTexture)
				{
					m_out << "            Type: Procedural Texture" << "\n";
				}
				m_out << "            Scale U: " << pTexture.GetScaleU() << "\n";
				m_out << "            Scale V: " << pTexture.GetScaleV() << "\n";
				m_out << "            Translation U: " << pTexture.GetTranslationU() << "\n";
				m_out << "            Translation V: " << pTexture.GetTranslationV() << "\n";
				m_out << "            Swap UV: " << pTexture.GetSwapUV() << "\n";
				m_out << "            Rotation U: " << pTexture.GetRotationU() << "\n";
				m_out << "            Rotation V: " << pTexture.GetRotationV() << "\n";
				m_out << "            Rotation W: " << pTexture.GetRotationW() << "\n";

				const char* lAlphaSources[] = { "None", "RGB Intensity", "Black" };
				m_out << "            Alpha Source: " << lAlphaSources[pTexture.GetAlphaSource()] << "\n";
				m_out << "            Cropping Left: " << pTexture.GetCroppingLeft() << "\n";
				m_out << "            Cropping Top: " << pTexture.GetCroppingTop() << "\n";
				m_out << "            Cropping Right: " << pTexture.GetCroppingRight() << "\n";
				m_out << "            Cropping Bottom: " << pTexture.GetCroppingBottom() << "\n";

				const char* lMappingTypes[] = { "Null", "Planar", "Spherical", "Cylindrical", "Box", "Face", "UV", "Environment" };
				m_out << "            Mapping Type: " << lMappingTypes[pTexture.GetMappingType()] << "\n";

				if (pTexture.GetMappingType() == FbxTexture::ePlanar)
				{
					const char* lPlanarMappingNormals[] = { "X", "Y", "Z" };
					m_out << "            Planar Mapping Normal: " << lPlanarMappingNormals[pTexture.GetPlanarMappingNormal()] << "\n";
				}

				const char* lBlendModes[] = {
					"Translucent", "Additive", "Modulate", "Modulate2", "Over", "Normal", "Dissolve", "Darken", "ColorBurn", "LinearBurn",
					"DarkerColor", "Lighten", "Screen", "ColorDodge", "LinearDodge", "LighterColor", "SoftLight", "HardLight", "VividLight",
					"LinearLight", "PinLight", "HardMix", "Difference", "Exclusion", "Substract", "Divide", "Hue", "Saturation", "Color",
					"Luminosity", "Overlay"
				};

				if (pBlendMode >= 0)
					m_out << "            Blend Mode: " << lBlendModes[pBlendMode] << "\n";
				
				m_out << "            Alpha: " << pTexture.GetDefaultAlpha() << "\n";

				if (lFileTexture)
				{
					const char* lMaterialUses[] = { "Model Material", "Default Material" };
					m_out << "            Material Use: " << lMaterialUses[lFileTexture->GetMaterialUse()] << "\n";
				}

				const char* pTextureUses[] = { "Standard", "Shadow Map", "Light Map", "Spherical Reflexion Map", "Sphere Reflexion Map", "Bump Normal Map" };
				m_out << "            Texture Use: " << pTextureUses[pTexture.GetTextureUse()] << "\n";
				m_out << "\n";
			}
		};

		FbxNode* node = scene.m_scene->GetRootNode();
		if (node == nullptr)
			return;

		Writer writer(out);
		writer.Write(*node);
	}
}
