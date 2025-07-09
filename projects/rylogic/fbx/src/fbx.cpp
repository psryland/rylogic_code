//********************************
// FBX Model loader
//  Copyright (c) Rylogic Ltd 2014
//********************************
// FBX files come in two variants; binary and text.
// The format is closed source however, so we need to use the AutoDesk FBX SDK.
#include <string>
#include <vector>
#include <format>
#include <ranges>
#include <iostream>
#include <functional>
#include <filesystem>
#include <unordered_set>
#include <algorithm>
#include <stdexcept>
#include <future>
#include <fbxsdk.h>
#include "pr/common/to.h"
#include "pr/common/cast.h"
#include "pr/common/algorithm.h"
#include "pr/container/vector.h"
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
		void operator()(FbxPose* pose) { pose->Destroy(); }
	};
	using ManagerPtr = std::unique_ptr<FbxManager, FbxObjectCleanUp>;
	using ImporterPtr = std::unique_ptr<FbxImporter, FbxObjectCleanUp>;
	using ExporterPtr = std::unique_ptr<FbxExporter, FbxObjectCleanUp>;
	using ScenePtr = std::unique_ptr<FbxScene, FbxObjectCleanUp>;
	using PosePtr = std::unique_ptr<FbxPose, FbxObjectCleanUp>;

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
	template <> struct Convert<Colour, FbxColor>
	{
		static Colour To_(FbxColor const& c)
		{
			return Colour(s_cast<float>(c.mRed), s_cast<float>(c.mGreen), s_cast<float>(c.mBlue), s_cast<float>(c.mAlpha));
		}
	};
	template <> struct Convert<Colour, FbxDouble3>
	{
		static Colour To_(FbxDouble3 const& c)
		{
			return Colour(s_cast<float>(c[0]), s_cast<float>(c[1]), s_cast<float>(c[2]), 1.0f);
		}
	};

	// Vec2
	template <Scalar S> struct Convert<Vec2<S, void>, FbxVector2>
	{
		static Vec2<S, void> To_(FbxVector2 const& v)
		{
			return Vec2<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]));
		}
	};

	// Vec4
	template <Scalar S> struct Convert<Vec4<S, void>, FbxDouble3>
	{
		static Vec4<S, void> To_(FbxDouble3 const& v)
		{
			return Vec4<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]), s_cast<S>(v[2]), s_cast<S>(0));
		}
	};

	// Vec4
	template <Scalar S> struct Convert<Vec4<S, void>, FbxDouble4>
	{
		static Vec4<S, void> To_(FbxDouble4 const& v)
		{
			return Vec4<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]), s_cast<S>(v[2]), s_cast<S>(v[3]));
		}
	};
	template <Scalar S> struct Convert<Vec4<S, void>, FbxVector4>
	{
		static Vec4<S, void> To_(FbxVector4 const& v)
		{
			return Vec4<S, void>(s_cast<S>(v[0]), s_cast<S>(v[1]), s_cast<S>(v[2]), s_cast<S>(v[3]));
		}
	};

	// Quaternion
	template <Scalar S> struct Convert<Quat<S, void, void>, FbxQuaternion>
	{
		static Quat<S, void, void> To_(FbxQuaternion const& q)
		{
			return Quat<S, void, void>(s_cast<S>(q[0]), s_cast<S>(q[1]), s_cast<S>(q[2]), s_cast<S>(q[3]));
		}
	};

	// Mat4x4
	template <Scalar S> struct Convert<Mat4x4<S, void, void>, FbxAMatrix>
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
	template <Scalar S> struct Convert<Mat4x4<S, void, void>, FbxMatrix>
	{
		static Mat4x4<S, void, void> To_(FbxMatrix const& m)
		{
			return Mat4x4<S, void, void>(
				Vec4<S, void>(s_cast<S>(m[0][0]), s_cast<S>(m[0][1]), s_cast<S>(m[0][2]), s_cast<S>(m[0][3])),
				Vec4<S, void>(s_cast<S>(m[1][0]), s_cast<S>(m[1][1]), s_cast<S>(m[1][2]), s_cast<S>(m[1][3])),
				Vec4<S, void>(s_cast<S>(m[2][0]), s_cast<S>(m[2][1]), s_cast<S>(m[2][2]), s_cast<S>(m[2][3])),
				Vec4<S, void>(s_cast<S>(m[3][0]), s_cast<S>(m[3][1]), s_cast<S>(m[3][2]), s_cast<S>(m[3][3])));
		}
	};

	// Bone type
	template <> struct Convert<geometry::fbx::BoneKey::EInterpolation, FbxAnimCurveDef::EInterpolationType>
	{
		static geometry::fbx::BoneKey::EInterpolation To_(FbxAnimCurveDef::EInterpolationType ty)
		{
			using EType = geometry::fbx::BoneKey::EInterpolation;
			switch (ty)
			{
				case FbxAnimCurveDef::EInterpolationType::eInterpolationConstant: return EType::Constant;
				case FbxAnimCurveDef::EInterpolationType::eInterpolationLinear: return EType::Linear;
				case FbxAnimCurveDef::EInterpolationType::eInterpolationCubic: return EType::Cubic;
				default: throw std::runtime_error(std::format("Unknown bone type: {}", int(ty)));
			}
		}
	};

	// String conversion
	template <> struct Convert<std::string_view, FbxNodeAttribute::EType>
	{
		static std::string_view To_(FbxNodeAttribute::EType ty)
		{
			switch (ty)
			{
				case FbxNodeAttribute::EType::eUnknown: return "eUnknown";
				case FbxNodeAttribute::EType::eNull: return "eNull";
				case FbxNodeAttribute::EType::eMarker: return "eMarker";
				case FbxNodeAttribute::EType::eSkeleton: return "eSkeleton";
				case FbxNodeAttribute::EType::eMesh: return "eMesh";
				case FbxNodeAttribute::EType::eNurbs: return "eNurbs";
				case FbxNodeAttribute::EType::ePatch: return "ePatch";
				case FbxNodeAttribute::EType::eCamera: return "eCamera";
				case FbxNodeAttribute::EType::eCameraStereo: return "eCameraStereo";
				case FbxNodeAttribute::EType::eCameraSwitcher: return "eCameraSwitcher";
				case FbxNodeAttribute::EType::eLight: return "eLight";
				case FbxNodeAttribute::EType::eOpticalReference: return "eOpticalReference";
				case FbxNodeAttribute::EType::eOpticalMarker: return "eOpticalMarker";
				case FbxNodeAttribute::EType::eNurbsCurve: return "eNurbsCurve";
				case FbxNodeAttribute::EType::eTrimNurbsSurface: return "eTrimNurbsSurface";
				case FbxNodeAttribute::EType::eBoundary: return "eBoundary";
				case FbxNodeAttribute::EType::eNurbsSurface: return "eNurbsSurface";
				case FbxNodeAttribute::EType::eShape: return "eShape";
				case FbxNodeAttribute::EType::eLODGroup: return "eLODGroup";
				case FbxNodeAttribute::EType::eSubDiv: return "eSubDiv";
				case FbxNodeAttribute::EType::eCachedEffect: return "eCachedEffect";
				case FbxNodeAttribute::EType::eLine: return "eLine";
				default: return "<unknown attribute type>";
			}
		}
	};
	template <> struct Convert<std::string_view, FbxSkeleton::EType>
	{
		static std::string_view To_(FbxSkeleton::EType ty)
		{
			switch (ty)
			{
				case FbxSkeleton::EType::eRoot: return "eRoot";
				case FbxSkeleton::EType::eLimb: return "eLimb";
				case FbxSkeleton::EType::eLimbNode: return "eLimbNode";
				case FbxSkeleton::EType::eEffector: return "eEffector";
				default: return "<unknown skeleton type>";
			}
		}
	};
	template <> struct Convert<std::string_view, FbxCluster::ELinkMode>
	{
		static std::string_view To_(FbxCluster::ELinkMode ty)
		{
			switch (ty)
			{
				case FbxCluster::ELinkMode::eNormalize: return "eNormalize";
				case FbxCluster::ELinkMode::eAdditive: return "eAdditive";
				case FbxCluster::ELinkMode::eTotalOne: return "eTotalOne";
				default: return "<unknown cluster link mode type>";
			}
		}
	};
}

// FBX support
namespace pr::geometry::fbx
{
	static constexpr int NoIndex = -1;
	static constexpr Vert NoVert = {};
	static constexpr uint64_t NoUniqueId = 0;
	struct MeshNode
	{
		FbxMesh* mesh;
		FbxMesh const* root;
		int level;
		int index;
	};
	struct BoneNode
	{
		FbxSkeleton* bone;
		FbxSkeleton const* root;
		int level;
		int index;
	};
	struct KeyTime
	{
		FbxTime time;
		FbxAnimCurveDef::EInterpolationType interpolation;
		bool operator == (KeyTime const& rhs) const { return time == rhs.time; }
		bool operator < (KeyTime const& rhs) const { return time < rhs.time; }
	};
	using ErrorList = std::vector<std::string>;
	using MeshNodeMap = std::unordered_map<uint64_t, MeshNode>; // Map from mesh id to mesh
	using BoneNodeMap = std::unordered_map<uint64_t, BoneNode>; // Map from bone id to bone

	// Check that 'ptr' is not null. Throw if it is
	template <typename T> inline T* Check(T* ptr, std::string_view message)
	{
		if (ptr) return ptr;
		throw std::runtime_error(std::string(message));
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
						if (vidx < 0 || vidx >= layer_elements->GetDirectArray().GetCount())
							throw std::runtime_error(std::format("Invalid vertex index {} for layer element with {} control points", vidx, layer_elements->GetDirectArray().GetCount()));

						auto idx = layer_elements->GetIndexArray().GetAt(vidx);
						if (idx < 0 || idx >= layer_elements->GetDirectArray().GetCount())
							throw std::runtime_error(std::format("Invalid index {} for layer element with {} direct elements", idx, layer_elements->GetDirectArray().GetCount()));

						return layer_elements->GetDirectArray().GetAt(idx);
					}
					case FbxLayerElement::eByPolygonVertex:
					{
						if (iidx < 0 || iidx >= layer_elements->GetIndexArray().GetCount())
							throw std::runtime_error(std::format("Invalid index {} for layer element with {} indices", iidx, layer_elements->GetIndexArray().GetCount()));

						auto idx = layer_elements->GetIndexArray().GetAt(iidx);
						if (idx < 0 || idx >= layer_elements->GetDirectArray().GetCount())
							throw std::runtime_error(std::format("Invalid index {} for layer element with {} direct elements", idx, layer_elements->GetDirectArray().GetCount()));

						return layer_elements->GetDirectArray().GetAt(idx);
					}
					case FbxLayerElement::eByPolygon:
					{
						if (fidx < 0 || fidx >= layer_elements->GetIndexArray().GetCount())
							throw std::runtime_error(std::format("Invalid face index {} for layer element with {} indices", fidx, layer_elements->GetIndexArray().GetCount()));

						auto idx = layer_elements->GetIndexArray().GetAt(fidx);
						if (idx < 0 || idx >= layer_elements->GetDirectArray().GetCount())
							throw std::runtime_error(std::format("Invalid index {} for layer element with {} direct elements", idx, layer_elements->GetDirectArray().GetCount()));

						return layer_elements->GetDirectArray().GetAt(idx);
					}
					case FbxLayerElement::eByEdge:
					{
						throw std::runtime_error("ByEdge mapping not implemented");
					}
					case FbxLayerElement::eAllSame:
					{
						if (layer_elements->GetDirectArray().GetCount() == 0)
							throw std::runtime_error("Layer element has no direct elements");

						auto idx = layer_elements->GetIndexArray().GetAt(0);
						if (idx < 0 || idx >= layer_elements->GetDirectArray().GetCount())
							throw std::runtime_error(std::format("Invalid index {} for layer element with {} direct elements", idx, layer_elements->GetDirectArray().GetCount()));

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

	// FbxAttribute to FbxNodeAttribute::EType
	template <typename FbxNodeType> struct attribute;
	template <> struct attribute<FbxMesh> { static constexpr FbxNodeAttribute::EType value = FbxNodeAttribute::eMesh; };
	template <> struct attribute<FbxSkeleton> { static constexpr FbxNodeAttribute::EType value = FbxNodeAttribute::eSkeleton; };

	// Find the next node attribute of the given type in 'node'
	template <typename FbxNodeType, typename FbxNodeRef>
	static FbxNodeType* Find(FbxNodeRef& node, int start = 0)
	{
		for (int i = start, iend = node.GetNodeAttributeCount(); i < iend; ++i)
		{
			auto& attr = *node.GetNodeAttributeByIndex(i);
			if (attr.GetAttributeType() == attribute<std::remove_const_t<FbxNodeType>>::value)
				return FbxCast<FbxNodeType>(&attr);
		}
		return nullptr;
	}

	// Enumerate the self + parents of 'node' of type 'FbxNodeType' up to the root
	template <typename FbxNodeType, typename FbxNodeRef>
	static auto Parents(FbxNodeRef& node)
	{
		struct Iterator
		{
			FbxNodeType* node;
			void operator ++()
			{
				auto* parent = node ? node->GetNode()->GetParent() : nullptr;
				node = parent ? Find<FbxNodeType>(*parent) : nullptr;
			}
			FbxNodeType& operator*() const
			{
				return *node;
			}
			bool operator !=(Iterator const& rhs) const
			{
				return node != rhs.node;
			}
		};
		struct Range
		{
			FbxNodeType* node;
			Iterator begin() const { return { node }; }
			Iterator end() const { return { nullptr }; }
		};
		return Range{ Find<FbxNodeType>(node) };
	}

	// Enumerate the children of 'node' of type 'FbxNodeType'
	template <typename FbxNodeType, typename FbxNodeRef>
	static auto Children(FbxNodeRef& node)
	{
		struct Iterator
		{
			std::remove_reference_t<FbxNodeRef>* node; int i;
			void operator ++() { for (++i; i != node->GetChildCount() && Find<FbxNodeType>(*node->GetChild(i)) == nullptr; ++i) {} }
			FbxNodeType& operator*() const { return *Find<FbxNodeType>(*node->GetChild(i)); }
			bool operator !=(Iterator const& rhs) const { return i != rhs.i; }
		};
		struct Range
		{
			std::remove_reference_t<FbxNodeRef>* node;
			Iterator begin() const { return { node, 0 }; }
			Iterator end() const { return { node, node->GetChildCount() }; }
		};
		return Range{ &node };
	}

	// Find the top-most node with the given attribute
	template <typename FbxNodeType, typename FbxNodeRef>
	static FbxNodeType* FindRoot(FbxNodeRef& node)
	{
		FbxNodeType* root = nullptr;
		for (auto& parent : Parents<FbxNodeType>(node))
			root = &parent;

		return root;
	}

	// Return the parent node with the given attribute
	template <typename FbxNodeType, typename FbxNodeRef>
	static FbxNodeType* FindParent(FbxNodeRef& node)
	{
		auto* parent = node.GetParent();
		return parent ? Find<FbxNodeType>(*parent) : nullptr;
	}

	// Find the skeleton associated with 'mesh'
	static FbxSkeleton const* FindSkeleton(FbxMesh const& mesh)
	{
		if (int dcount = mesh.GetDeformerCount(FbxDeformer::eSkin); dcount != 0)
		{
			auto& skin = *FbxCast<FbxSkin>(mesh.GetDeformer(0, FbxDeformer::eSkin));
			if (int ccount = skin.GetClusterCount(); ccount != 0)
			{
				// Find the bone with no parent and assume that is the root
				auto& cluster = *skin.GetCluster(0);
				auto const& bone = *cluster.GetLink();
				return FindRoot<FbxSkeleton const>(bone);
			}
		}
		return nullptr;
	}

	// Find the bind pose for the given node(s)
	static FbxPose const* FindBindPose(FbxScene& fbxscene, std::span<BoneNode const*> nodes)
	{
		// Find a bind pose that contains values for all nodes
		// This isn't 100% reliable, but that's the best FBX offers
		for (int i = 0, iend = fbxscene.GetPoseCount(); i != iend; ++i)
		{
			auto* pose = fbxscene.GetPose(i);
			if (!pose || !pose->IsBindPose())
				continue;

			// If the bind pose contains values for all nodes, then that's close enough
			if (all(nodes, [pose](BoneNode const* bone_node) { return pose->Find(bone_node->bone->GetNode()) != -1; }))
				return pose;
		}
		return nullptr;
	}
	static FbxPose const* FindBindPose(FbxScene& fbxscene, BoneNode const& node)
	{
		auto* ptr = &node;
		return FindBindPose(fbxscene, { &ptr, 1 });
	}

	// Traverse the scene hierarchy building up lookup tables from unique IDs to nodes (mesh, skeleton)
	static void TraverseHierarchy(FbxScene& fbxscene, EParts parts, MeshNodeMap& meshes, BoneNodeMap& bones)
	{
		// Notes:
		//  - Root nodes for meshes, skeletons can occur at any level.
		//  - Any mesh/skeleton node whose parent is not a mesh/skeleton
		//    node is the start of a new mesh/skeleton hierarchy.

		struct NodeAndLevel { FbxNode* node; int level; };
		vector<NodeAndLevel> stack = { {fbxscene.GetRootNode(), 0} };
		for (int mindex = 0, bindex = 0; !stack.empty(); )
		{
			auto [node, level] = stack.back();
			stack.pop_back();

			// Process all attributes of the node
			for (int i = 0, iend = node->GetNodeAttributeCount(); i != iend; ++i)
			{
				auto& attr = *node->GetNodeAttributeByIndex(i);

				// Create a map from attribute unique id to node data
				if (AllSet(parts, EParts::Meshes) && attr.GetAttributeType() == FbxNodeAttribute::eMesh)
				{
					auto* mesh = FbxCast<FbxMesh>(&attr);
					auto* root = FindRoot<FbxMesh>(*node);
					meshes[mesh->GetUniqueID()] = { mesh, root, level, mindex++ };
				}
				if (AllSet(parts, EParts::Skeletons) && attr.GetAttributeType() == FbxNodeAttribute::eSkeleton)
				{
					auto* bone = FbxCast<FbxSkeleton>(&attr);
					auto* root = FindRoot<FbxSkeleton>(*node);
					bones[bone->GetUniqueID()] = { bone, root, level, bindex++ };
				}
			}

			// Recurse in depth first order
			for (int i = node->GetChildCount(); i-- != 0; )
				stack.push_back({ node->GetChild(i), level + 1 });
		}
	}

	// Get the hierarchy address of 'node'
	static std::string Address(FbxNode const* node)
	{
		if (node == nullptr) return "";
		std::string address = Address(node->GetParent());
		return std::move(address.append(address.empty() ? "" : ".").append(node->GetName()));
	}

	// RAII fbx array wrapper
	template <typename T>
	struct FbxArray : fbxsdk::FbxArray<T>
	{
		FbxArray() : fbxsdk::FbxArray<T>() {}
		~FbxArray() { FbxArrayDelete<T>(*this); }
	};

	// An RAII dll reference
	struct Context
	{
		using SceneCont = std::vector<std::unique_ptr<SceneData>>;

		ErrorHandler m_error_cb;
		FbxManager* m_manager;
		FbxIOSettings* m_settings;
		char const* m_version;
		SceneCont m_scenes;

		Context(ErrorHandler error_cb)
			: m_error_cb(error_cb)
			, m_manager(Check(FbxManager::Create(), "Error: Unable to create FBX Manager"))
			, m_settings(Check(FbxIOSettings::Create(m_manager, IOSROOT), "Error: Unable to create settings"))
			, m_version(m_manager->GetVersion())
			, m_scenes()
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
		Context(Context&&) = delete;
		Context(Context const&) = delete;
		Context& operator=(Context&&) = delete;
		Context& operator=(Context const&) = delete;
		~Context()
		{
			// Notes:
			//  - FbxImporter must be destroyed before any FbxScenes it creates because of bugs in the 'fbxsdk' dll.

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

	// Model data types
	struct MaterialData
	{
		Colour m_ambient = ColourBlack;
		Colour m_diffuse = ColourWhite;
		Colour m_specular = ColourZero;
		std::string m_tex_diff;
	};
	struct SkeletonData
	{
		// Notes:
		//  - Skeletons can have multiple root bones. Check for a 'm_hierarchy[i] == 0' values
		uint64_t m_id = 0ULL;        // Unique skeleton Id
		vector<uint64_t> m_bone_ids; // Bone unique ids
		vector<std::string> m_names; // Bone names
		vector<m4x4> m_o2bp;         // Inverse of the bind-pose to root-object-space transform for each bone
		vector<int> m_hierarchy;     // Hierarchy levels. level == 0 are root bones.

		// The number of bones in this skeleton
		int size() const
		{
			assert(isize(m_bone_ids) == isize(m_names) && isize(m_names) == isize(m_o2bp) && isize(m_o2bp) == isize(m_hierarchy));
			return isize(m_bone_ids);
		}
	};
	struct SkinData
	{
		uint64_t m_skel_id = ~0ULL;
		vector<int> m_offsets;    // Index offset to the first bone for each vertex
		vector<uint64_t> m_bones; // The Ids of the bones that influence a vertex
		vector<double> m_weights; // The influence weights

		explicit operator bool() const
		{
			return !m_offsets.empty() && m_offsets.back() != 0;
		}
	};
	struct AnimationData
	{
		SkeletonData const* m_skel; // The skeleton that these tracks should match
		std::vector<int> m_offsets; // The offset to the start of each bone's track
		std::vector<BoneKey> m_alltracks; // A track for each bone (concatenated)
		Animation::TimeRange m_time_range; // The time span of the animation

		// Return the track associated with the 'bone_index'th bone in the skeleton
		std::span<BoneKey> track(int bone_index)
		{
			auto ofs = m_offsets[s_cast<size_t>(bone_index) + 0];
			auto len = m_offsets[s_cast<size_t>(bone_index) + 1] - ofs;
			return std::span{ m_alltracks.data() + ofs, s_cast<size_t>(len) };
		}

		// True for non-empty animations
		explicit operator bool() const
		{
			return !m_offsets.empty() && m_offsets.back() != 0;
		}
	};
	struct MeshData
	{
		using Name = std::string;
		using VBuffer = std::vector<Vert>;
		using IBuffer = std::vector<int>;
		using NBuffer = std::vector<Nugget>;

		uint64_t m_id = {};
		Name m_name;
		VBuffer m_vbuf;
		IBuffer m_ibuf;
		NBuffer m_nbuf;
		SkinData m_skin;
		BBox m_bbox = {};
		m4x4 m_o2p = {};
		int m_level = {};
	};

	// Loaded scene data
	struct SceneData
	{
		using MeshCont = std::vector<MeshData>;
		using MaterialCont = std::unordered_map<uint64_t, MaterialData>;
		using SkeletonCont = std::vector<SkeletonData>;
		using AnimationCont = std::vector<AnimationData>;

		FbxScene* m_fbxscene;

		// One or more model hierarchies.
		// Meshes with 'm_level == 0' are the roots of a model tree
		// Meshes are in depth-first order.
		MeshCont m_meshes;

		// Material definitions
		MaterialCont m_materials;

		// One or more skeleton hierarchies.
		SkeletonCont m_skeletons;

		// Animation data
		AnimationCont m_animations;

		SceneData(FbxScene* fbxscene)
			:m_fbxscene(fbxscene)
		{}
	};

	// Read an fbx scene
	struct Reader
	{
		// Notes:
		//  - The FbxSDK library is very slow, single threaded, poorly documented.
		//  - The FBX file contains collections of attributes (meshes, materials, skeletons, animations, etc.)
		//    and a hierarchical node tree (scene graph) where each node may reference one or more attributes.
		//  - There are two main ways to read data:
		//    - Iterate all objects of a specific type (e.g., GetSrcObject<FbxMesh>), but not all these objects
		//      are necessarily in the scene.
		//    - Recursively traverse the node hierarchy from GetRootNode() and iterate over attributes.
		//  - Animation data is stored in FbxAnimStacks (takes), each containing one or more FbxAnimLayers.
		//    - Curves are attached to animate-able properties and can be retrieved per layer.
		//  - Relationships like materials and textures are represented via FBX connections, not ownership.
		//  - Units, axis orientation, and coordinate systems should be checked via GlobalSettings.
		//  - Vertex data is organized into layers, each with mapping and reference modes that define indexing.
		//  - The root nodes of Meshes or Skeletons can begin at any level, e.g. Nodes might be a Group which then
		//    contain meshes or skeletons starting below the group.
		//  - Mesh hierarchies can reference multiple disconnected skeletons.
		//  - Nodes can only have one attribute of each type.
		// TODO:
		//  - LOD Groups?

		using KeyTimes = std::vector<KeyTime>;

		SceneData& m_scene;
		FbxScene& m_fbxscene;
		ReadOptions const& m_opts;
		MeshNodeMap m_meshes;
		BoneNodeMap m_bones;
		FbxAnimStack* m_animstack;
		FbxAnimLayer* m_layer;
		FbxTimeSpan m_time_span;
		double m_frame_rate;

		Reader(SceneData& scene, ReadOptions const& opts)
			: m_scene(scene)
			, m_fbxscene(*scene.m_fbxscene)
			, m_opts(opts)
			, m_meshes()
			, m_bones()
			, m_animstack()
			, m_layer()
			, m_time_span()
			, m_frame_rate(FbxTime::GetFrameRate(m_fbxscene.GetGlobalSettings().GetTimeMode()))
		{
			auto basis0 = FbxAxisSystem{FbxAxisSystem::eYAxis, FbxAxisSystem::eParityOdd, FbxAxisSystem::eRightHanded};
			auto basis1 = m_fbxscene.GetGlobalSettings().GetAxisSystem();
			if (basis0 != basis1)
				basis0.ConvertScene(&m_fbxscene);
		}

		// Read the scene
		void Do()
		{
			TraverseHierarchy(m_fbxscene, m_opts.m_parts, m_meshes, m_bones);

			if (AllSet(m_opts.m_parts, EParts::Materials))
				ReadMaterials();
			if (AllSet(m_opts.m_parts, EParts::Meshes))
				ReadGeometry();
			if (AllSet(m_opts.m_parts, EParts::Skeletons))
				ReadSkeletons();
			if (AllSet(m_opts.m_parts, EParts::Animation))
				ReadAnimation();
		}

		// Read the materials
		void ReadMaterials()
		{
			// Add a default material for unknown materials
			m_scene.m_materials[0] = {};

			// Output each material in the scene
			for (int m = 0, mend = m_fbxscene.GetMaterialCount(); m != mend; ++m)
			{
				MaterialData material = {};
				Progress(1 + m, mend, "Reading materials...");

				auto const& mat = *m_fbxscene.GetMaterial(m);
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

				m_scene.m_materials[mat.GetUniqueID()] = material;
			}
		}

		// Read meshes from the FBX scene
		void ReadGeometry()
		{
			std::vector<std::future<MeshData>> tasks;

			// Process each mesh in a worker thread, because triangulation is really slow
			for (auto const& [id, meshnode] : m_meshes)
			{
				// Create thread-local manager and scene
				ManagerPtr manager{ FbxManager::Create() };
				ScenePtr scene{ FbxScene::Create(manager.get(), "IsolatedScene") };

				// Clone the mesh
				auto fbxmesh = FbxCast<FbxMesh>(meshnode.mesh->Clone(FbxObject::eDeepClone, scene.get()));
				
				// Start a task to convert the geometry
				tasks.emplace_back(std::async(std::launch::async, [m = std::move(manager), s = std::move(scene), id, fbxmesh]
				{
					MeshData out;
					out.m_id = id;

					// Ensure the mesh is triangles
					auto trimesh = fbxmesh;
					if (!trimesh->IsTriangleMesh())
					{
						// Must do this before triangulating the mesh due to an FBX bug in Triangulate.
						// Edge hardnees triangulation give wrong edge hardness so we convert them to a smooth group during the triangulation.
						FbxGeometryConverter converter(m.get());
						for (int j = 0, jend = trimesh->GetLayerCount(FbxLayerElement::eSmoothing); j != jend; ++j)
						{
							auto* smoothing = fbxmesh->GetLayer(j)->GetSmoothing();
							if (smoothing && smoothing->GetMappingMode() != FbxLayerElement::eByPolygon)
								converter.ComputePolygonSmoothingFromEdgeSmoothing(trimesh, j);
						}

						// Convert polygons to triangles
						trimesh = FbxCast<FbxMesh>(converter.Triangulate(trimesh, true));
						if (!trimesh->IsTriangleMesh()) // If triangulation failed, ignore the mesh
							throw std::runtime_error(std::format("Failed to convert mesh '{}' to a triangle mesh", trimesh->GetName()));
					}

					// "Inflate" the verts into a unique list of each required combination
					{
						auto vcount = trimesh->GetControlPointsCount();
						auto fcount = trimesh->GetPolygonCount();
						auto ncount = trimesh->GetElementMaterialCount();
						auto const* verts = trimesh->GetControlPoints();
						auto const* layer0 = trimesh->GetLayer(0);
						auto const* materials = layer0->GetMaterials();
						auto const* colours = layer0->GetVertexColors();
						auto const* normals = layer0->GetNormals();
						auto const* uvs = layer0->GetUVs(FbxLayerElement::eTextureDiffuse);
						vector<int> vlookup;

						// Initialise buffers
						out.m_vbuf.reserve(s_cast<size_t>(vcount) * 3 / 2);
						out.m_ibuf.reserve(s_cast<size_t>(fcount) * 3);
						out.m_nbuf.reserve(s_cast<size_t>(ncount));
						vlookup.reserve(s_cast<size_t>(vcount) * 3 / 2);

						// Add a vertex to 'm_vbuf' and return its index
						auto AddVert = [&out, &vlookup](int src_vidx, v4 const& pos, Colour const& col, v4 const& norm, v2 const& uv) -> int
						{
							Vert v = {
								.m_vert = pos,
								.m_colr = col,
								.m_norm = norm,
								.m_tex0 = uv,
								.m_idx0 = {src_vidx, 0},
							};

							// 'Vlookup' is a linked list of vertices that are permutations of 'src_idx'
							for (int vidx = src_vidx;;)
							{
								// If 'vidx' is outside the buffer, add it
								auto vbuf_count = isize(out.m_vbuf);
								if (vidx >= vbuf_count)
								{
									out.m_vbuf.resize(std::max(vbuf_count, vidx + 1), NoVert);
									vlookup.resize(std::max(vbuf_count, vidx + 1), NoIndex);
									out.m_vbuf[vidx] = v;
									vlookup[vidx] = NoIndex;
									return vidx;
								}

								// If 'v' is already in the buffer, use it's index
								if (out.m_vbuf[vidx] == v)
								{
									return vidx;
								}

								// If the position 'vidx' is an unused slot, use it
								if (out.m_vbuf[vidx] == NoVert)
								{
									out.m_vbuf[vidx] = v;
									return vidx;
								}

								// If there is no "next", prepare to insert it at the end
								if (vlookup[vidx] == NoIndex)
								{
									vlookup[vidx] = vbuf_count;
								}

								// Go to the next vertex to check
								vidx = vlookup[vidx];
							}
						};

						// Get or add a nugget
						auto GetOrAddNugget = [&out](uint64_t mat_id) -> Nugget&
						{
							for (auto& n : out.m_nbuf)
							{
								if (n.m_mat_id != mat_id) continue;
								return n;
							}
							out.m_nbuf.push_back(Nugget{ .m_mat_id = mat_id });
							return out.m_nbuf.back();
						};

						// Read the faces of 'mesh' adding them to a nugget based on their material
						for (int f = 0, fend = trimesh->GetPolygonCount(); f != fend; ++f)
						{
							if (trimesh->GetPolygonSize(f) != 3)
								throw std::runtime_error(std::format("Mesh {} has a polygon with {} vertices, but only triangles are supported", trimesh->GetName(), trimesh->GetPolygonSize(f)));

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
								auto vidx = trimesh->GetPolygonVertex(f, j);
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

								// Get the vertex UV
								v2 uv = {};
								if (uvs != nullptr)
								{
									nugget.m_geom |= EGeom::Tex0;
									uv = To<v2>(GetLayerElement<FbxVector2>(uvs, f, iidx, vidx));
								}

								// Add the vertex to the vertex buffer and it's index to the index buffer
								vidx = AddVert(vidx, pos, col, norm, uv);
								out.m_ibuf.push_back(vidx);

								nugget.m_vrange.grow(vidx);
								nugget.m_irange.grow(isize(out.m_ibuf) - 1);
							}
						}
					}

					return out;
				}));
			}

			// There should be one mesh for each mesh node
			m_scene.m_meshes.resize(m_meshes.size());

			// Wait for each mesh.
			for (auto& task : tasks)
			{
				Progress(1 + &task - tasks.data(), ssize(tasks), "Reading models...");

				task.wait();
				auto mesh = task.get();

				// Find the associated FbxMesh and the hierarchy root
				auto& meshnode = m_meshes[mesh.m_id];
				auto& rootnode = m_meshes[meshnode.root->GetUniqueID()];
				auto& node = *meshnode.mesh->GetNode();

				// Populate the other mesh data from the 'meshnode'
				mesh.m_name = node.GetName();
				mesh.m_bbox = BoundingBox(*meshnode.mesh);
				mesh.m_level = meshnode.level - rootnode.level;

				// Determine the object to parent transform.
				mesh.m_o2p = To<m4x4>(node.EvaluateLocalTransform());

				#if _DEBUG
				auto o2w = To<m4x4>(node.EvaluateGlobalTransform());
				auto p2w = To<m4x4>(node.GetParent()->EvaluateGlobalTransform());
				auto o2p = InvertFast(p2w) * o2w;
				assert(FEql(mesh.m_o2p, o2p)); // Could we actually use the local transform?
				#endif

				// Read the skinning data for this mesh
				if (AllSet(m_opts.m_parts, EParts::Skinning))
					mesh.m_skin = ReadSkin(meshnode);

				// Output the mesh
				m_scene.m_meshes[meshnode.index] = std::move(mesh);
			}
		}

		// Read the skin data for 'meshnode'
		SkinData ReadSkin(MeshNode const& meshnode)
		{
			// Find the corresponding FbxMesh
			auto& fbxmesh = *meshnode.mesh;

			// Gather the bones and weights per vertex
			struct Influence
			{
				vector<uint64_t, 8> m_bones;
				vector<double, 8> m_weights;
			};
			std::vector<Influence> influences;
			influences.resize(fbxmesh.GetControlPointsCount());

			// Get the skinning data for this mesh
			for (int d = 0, dend = fbxmesh.GetDeformerCount(FbxDeformer::eSkin); d != dend; ++d)
			{
				auto& fbxskin = *FbxCast<FbxSkin>(fbxmesh.GetDeformer(d, FbxDeformer::eSkin));
				for (int c = 0, cend = fbxskin.GetClusterCount(); c != cend; ++c)
				{
					auto& cluster = *fbxskin.GetCluster(c);
					if (cluster.GetControlPointIndicesCount() == 0)
						continue;

					// Check for supported features
					auto link_mode = cluster.GetLinkMode();
					if (link_mode != FbxCluster::ELinkMode::eNormalize)
						throw std::runtime_error("Non-normalise link modes not implemented");

					// Get the bone that influences this cluster
					auto& bone = *Find<FbxSkeleton>(*cluster.GetLink());

					// Get the span of vert indices and weights
					auto indices = std::span<int>{ cluster.GetControlPointIndices(), static_cast<size_t>(cluster.GetControlPointIndicesCount()) };
					auto weights = std::span<double>{ cluster.GetControlPointWeights(), static_cast<size_t>(cluster.GetControlPointIndicesCount()) };
					for (int w = 0, wend = cluster.GetControlPointIndicesCount(); w != wend; ++w)
					{
						auto vidx = indices[w];
						auto weight = weights[w];

						influences[vidx].m_bones.push_back(bone.GetUniqueID());
						influences[vidx].m_weights.push_back(weight);
					}
				}
			}

			// Populate the skinning data
			SkinData skin;
			skin.m_offsets.reserve(s_cast<size_t>(fbxmesh.GetControlPointsCount()) + 1);
			skin.m_bones.reserve(skin.m_offsets.capacity() * 8);
			skin.m_weights.reserve(skin.m_bones.capacity());

			int count = 0;
			for (auto const& influence : influences)
			{
				skin.m_offsets.push_back(count);
				auto influence_count = isize(influence.m_bones);
				count += influence_count;

				for (int i = 0; i != influence_count; ++i)
				{
					skin.m_bones.push_back(influence.m_bones[i]);
					skin.m_weights.push_back(influence.m_weights[i]);
				}
			}
			skin.m_offsets.push_back(count);

			return skin;
		}

		// Read skeletons from the FBX scene
		void ReadSkeletons()
		{
			// Notes:
			//  - Mesh hierarchies (trees) can reference multiple disconnected skeletons,
			//    but also, single skeletons can influence multiple meshes.
			//  - To find the unique skeletons, scan all meshes in the scene and record
			//    which roots each mesh-tree is associated with. Separate skeletons are those
			//    that don't share mesh-trees.
			struct SkeletonBuilder
			{
				FbxScene& m_fbxscene;
				SceneData::SkeletonCont& m_skeletons;
				BoneNodeMap const& m_bone_map;
				std::unordered_set<FbxSkeleton const*> m_bone_set;
				std::unordered_map<BoneNode const*, std::unordered_set<MeshData*>> m_roots;

				SkeletonBuilder(FbxScene& fbxscene, SceneData::SkeletonCont& skeletons, BoneNodeMap const& bone_map)
					: m_fbxscene(fbxscene)
					, m_skeletons(skeletons)
					, m_bone_map(bone_map)
					, m_bone_set()
					, m_roots()
				{}
				void Collect(std::span<MeshData> meshtree, MeshNodeMap& mesh_map)
				{
					// Find the skeleton roots for all bones referenced by this mesh-tree
					for (auto const& mesh : meshtree)
					{
						auto& fbxmesh = *mesh_map[mesh.m_id].mesh;
						for (int d = 0, dend = fbxmesh.GetDeformerCount(FbxDeformer::eSkin); d != dend; ++d)
						{
							auto& fbxskin = *FbxCast<FbxSkin>(fbxmesh.GetDeformer(d, FbxDeformer::eSkin));
							for (int c = 0, cend = fbxskin.GetClusterCount(); c != cend; ++c)
							{
								auto& cluster = *fbxskin.GetCluster(c);
								if (cluster.GetControlPointIndicesCount() == 0)
									continue;

								if (cluster.GetLinkMode() != FbxCluster::eNormalize)
									throw std::runtime_error("Unsupported link mode");

								// Record the root and which mesh-tree is associated with it
								auto& node = *cluster.GetLink();
								auto& root = *FindRoot<FbxSkeleton const>(node);
								auto const& root_node = m_bone_map.at(root.GetUniqueID());
								m_roots[&root_node].insert(&meshtree.front());
							}
						}
					}
				}
				void CollectBones(FbxSkeleton const& bone)
				{
					m_bone_set.insert(&bone);
					for (auto& child : Children<FbxSkeleton>(*bone.GetNode()))
						CollectBones(child);
				}
				void Build()
				{
					// For each root, add all connected bones (children) to the bone set
					for (auto const& [root, _] : m_roots)
						CollectBones(*root->bone);

					// Create unique skeletons from roots that don't share mesh-trees
					for (; !m_roots.empty(); )
					{
						auto& [_, set] = *m_roots.begin();

						// Build a list of the roots that are part of this skeleton
						vector<BoneNode const*> roots;
						for (auto& [r, s] : m_roots)
						{
							// If any meshes in 's' are in 'set', then add 'r' to 'roots' and 's' to 'set'
							if (std::ranges::any_of(s, [&set](MeshData* m) { return set.contains(m); }))
							{
								roots.push_back(r);
								set.insert(begin(s), end(s));
							}
						}

						// Ensure a stable root order
						std::sort(roots.begin(), roots.end(), [](BoneNode const* lhs, BoneNode const* rhs) { return lhs->index < rhs->index; });

						// Find the bind pose
						auto* bind_pose = FindBindPose(m_fbxscene, roots);

						// Construct a skeleton
						SkeletonData skeleton = { .m_id = FindCommonAncestor(roots)->GetUniqueID() };
						skeleton.m_bone_ids.reserve(m_bone_set.size());
						skeleton.m_names.reserve(m_bone_set.size());
						skeleton.m_o2bp.reserve(m_bone_set.size());
						skeleton.m_hierarchy.reserve(m_bone_set.size());
						for (auto root : roots)
							Build(skeleton, bind_pose, *root, 0);

						// Update the skeleton ID in each mesh
						for (auto mesh : set)
							mesh->m_skin.m_skel_id = skeleton.m_id;

						// Add the skeleton to the output
						m_skeletons.push_back(std::move(skeleton));

						// Remove 'roots' from 'm_roots'
						for (auto root : roots)
							m_roots.erase(root);
					}
				}
				void Build(SkeletonData& skeleton, FbxPose const* bind_pose, BoneNode const& bone_node, int level)
				{
					auto const& bone = *bone_node.bone;
					auto const& node = *bone.GetNode();

					int idx = -1;
					if (!bind_pose || (idx = bind_pose->Find(&node)) == -1)
					{
						bind_pose = FindBindPose(m_fbxscene, bone_node);
						idx = bind_pose ? bind_pose->Find(&node) : -1;
					}

					// Object space to bind pose.
					// The bind pose is a snapshot of the global transforms of the bones
					// at the time skinning was authored in the DCC tool.
					// Fall-back to time zero as the bind pose.
					auto o2bp = idx != -1
						? InvertFast(To<m4x4>(bind_pose->GetMatrix(idx)))
						: InvertFast(To<m4x4>(const_cast<FbxNode&>(node).EvaluateGlobalTransform(0)));

					skeleton.m_bone_ids.push_back(bone.GetUniqueID());
					skeleton.m_names.push_back(node.GetName());
					skeleton.m_o2bp.push_back(o2bp);
					skeleton.m_hierarchy.push_back(level);

					for (auto& child : Children<FbxSkeleton const>(node))
					{
						auto const& child_node = m_bone_map.at(child.GetUniqueID());
						Build(skeleton, bind_pose, child_node, level + 1);
					}
				}
				FbxNode const* FindCommonAncestor(std::span<BoneNode const*> nodes)
				{
					if (nodes.empty())
						return nullptr;
					if (nodes.size() == 1)
						return nodes.front()->bone->GetNode();

					// Get all the root nodes and their levels
					struct NodeAndLevel { FbxNode const* node; int level; };
					vector<NodeAndLevel> ancestors; ancestors.reserve(nodes.size());
					for (auto const& node : nodes)
						ancestors.push_back(NodeAndLevel{ node->bone->GetNode(), node->level });

					// Find the shallowest node
					auto shallowest = std::numeric_limits<int>::max();
					for (auto const& ancestor : ancestors)
						shallowest = std::min(shallowest, ancestor.level);

					// Equalize depths
					for (auto& ancestor : ancestors)
					{
						for (; ancestor.node && ancestor.level > shallowest;)
						{
							ancestor.node = ancestor.node->GetParent();
							--ancestor.level;
						}
					}

					// Walk up until all nodes are the same
					for (;;)
					{
						auto all_same = true;
						auto const& first = ancestors.front();
						for (auto const& ancestor : ancestors)
							all_same &= ancestor.node == first.node;

						if (all_same)
							return first.node;

						for (auto& ancestor : ancestors)
						{
							ancestor.node = ancestor.node->GetParent();
							--ancestor.level;
						}
					}
				}
			};

			// Skeleton builder helper
			SkeletonBuilder skelbuilder(m_fbxscene, m_scene.m_skeletons, m_bones);

			// Return the range of Mesh objects for a complete mesh tree.
			auto NextMeshTree = [this](std::span<MeshData> const* prev = nullptr) -> std::span<MeshData>
			{
				// Use for iteration: 'for (auto mesh = NextMeshTree(); !mesh.empty(); mesh = NextMeshTree(&mesh)) {}'
				auto beg = prev ? s_cast<int>(prev->data() - m_scene.m_meshes.data() + prev->size()) : 0;
				auto end = beg;

				auto scene_count = isize(m_scene.m_meshes);
				for (; end != scene_count && (beg == end || m_scene.m_meshes[end].m_level > m_scene.m_meshes[beg].m_level); ++end) {}
				return { m_scene.m_meshes.data() + beg, s_cast<size_t>(end - beg) };
			};

			// Find all bones from all the meshes in the scene
			for (auto meshtree = NextMeshTree(); !meshtree.empty(); meshtree = NextMeshTree(&meshtree))
			{
				Progress(1 + meshtree.data() - m_scene.m_meshes.data(), ssize(m_scene.m_meshes), "Reading skeletons...");
				skelbuilder.Collect(meshtree, m_meshes);
			}

			// Compile the unique skeletons
			skelbuilder.Build();
		}

		// Read the animation data from the scene
		void ReadAnimation()
		{
			// Notes:
			//  - FbxAnimStack can affect any node in the scene so it's possible for one animation to affect multiple skeletons.
			//  - By far the slowest part of this is calling 'EvaluateXXXXTransform', so we only want to loop over time once.
			//    This means collecting animation key frames for all skeletons at the same time.
			//  - In order to parallelize reading the animation data, we have to cache the transforms in a snapshot
			//    of the node transforms for every bone in the skeleton at a key frame time.
			//  - Each thread worker needs to add a transform to 'animation' for each bone that has a key frame at the
			//    given time.
			//  - Fbx curve keys have an interpolation type, they are either constant, linear, or cubic.

			// Set the animation to use
			auto animation_count = m_fbxscene.GetSrcObjectCount<FbxAnimStack>();
			for (int i = 0; i != animation_count; ++i)
			{
				Progress(1 + i, animation_count, "Reading animation...");

				m_animstack = Check(m_fbxscene.GetSrcObject<FbxAnimStack>(i), "Requested animation stack does not exist");
				m_time_span = m_animstack->GetLocalTimeSpan();
				m_fbxscene.SetCurrentAnimationStack(m_animstack);

				// Only support one animation layer at the moment
				auto layer_count = std::min(1, m_animstack->GetMemberCount<FbxAnimLayer>());
				for (int j = 0; j != layer_count; ++j)
				{
					m_layer = m_animstack->GetMember<FbxAnimLayer>(j);

					// Read the key frame times for all bones
					std::vector<uint64_t> bone_id_lookup(m_bones.size());
					std::vector<KeyTimes> times_per_bone(m_bones.size());
					for (auto& [id, bonenode] : m_bones)
					{
						times_per_bone[bonenode.index] = FindKeyFrameTimes(*bonenode.bone->GetNode());
						bone_id_lookup[bonenode.index] = id;
					}

					std::vector<AnimationData> animations;

					// Animations are associated with a skeleton. Create an animation for each
					// skeleton that contains a bone that is moved by this animation.
					for (auto const& skel : m_scene.m_skeletons)
					{
						// Is the skeleton moved by this animation?
						if (pr::all(skel.m_bone_ids, [this, &times_per_bone](uint64_t id) { return times_per_bone[m_bones[id].index].empty(); }))
							continue;

						// Create an animation for 'skel'
						AnimationData animation = {
							.m_skel = &skel,
							.m_offsets = {},
							.m_alltracks = {},
							.m_time_range = Animation::TimeRange::Reset()
						};

						// Preallocate the arrays in 'animation' so we can populate it in parallel
						animation.m_offsets.reserve(skel.m_bone_ids.size() + 1);

						// Record an entry for every bone in the skeleton, even if they're empty (it makes bone lookup easier)
						int key_count = 0;
						for (auto id : skel.m_bone_ids)
						{
							auto const& times = times_per_bone[m_bones[id].index];
							animation.m_offsets.push_back(key_count);
							key_count += isize(times);

							if (!times.empty())
							{
								animation.m_time_range.grow(times.front().time.GetSecondDouble());
								animation.m_time_range.grow(times.back().time.GetSecondDouble());
							}
						}

						// The last offset is the total number of keys
						animation.m_offsets.push_back(key_count);
						animation.m_alltracks.resize(key_count);
						animations.push_back(std::move(animation));
					}

					std::vector<std::future<void>> tasks;

					// For each key frame time, start a worker that adds the key frame to each bone with that key
					zip<EZip::SetsFull, KeyTime>(times_per_bone, [this, &animations, &tasks, &bone_id_lookup](KeyTime keytime, std::span<ZipSet const> indices)
					{
						Progress(1 + (keytime.time - m_time_span.GetStart()).GetSecondCount(), m_time_span.GetDuration().GetSecondCount(), "Reading key frames...", 1);

						// A snapshot of all bone transforms at a time
						struct Snapshot
						{
							// At a given time (the snapshot time) one or more bones may have a 'Key'.
							// Each bone has a different number of keys, so this key has a different
							// index within each bone (m_key_index).
							struct Key
							{
								int m_bone_index; // The bone that has this key
								int m_key_index; // The index of this key within the bone's keys
								int m_parent_index; // The index of the parent bone
								BoneKey::EInterpolation m_interpolation; // How the key frame interpolates
							};

							std::vector<Key> m_keys;    // The keys at this snapshot
							std::vector<m4x4> m_b2w;    // Bone to world for each bone at this time
							double m_time;              // The time value in seconds at this snapshot time
						};

						// Capture a snapshot of the bone transforms at this time
						Snapshot snapshot = {
							.m_keys = {},
							.m_b2w = {},
							.m_time = keytime.time.GetSecondDouble(), 
						};

						// Find the parent bone index
						auto ParentOf = [this, &bone_id_lookup](int bone_index)
						{
							auto* parent = FindParent<FbxSkeleton>(*m_bones[bone_id_lookup[bone_index]].bone->GetNode());
							return parent ? m_bones[parent->GetUniqueID()].index : -1;
						};

						// Record the bones with a key frame at this time, and which key frame that is
						snapshot.m_keys.reserve(indices.size());
						for (auto& [bone_idx, key_idx] : indices)
						{
							snapshot.m_keys.push_back({
								.m_bone_index = s_cast<int>(bone_idx),
								.m_key_index = s_cast<int>(key_idx),
								.m_parent_index = ParentOf(s_cast<int>(bone_idx)),
								.m_interpolation = To<BoneKey::EInterpolation>(keytime.interpolation),
							});
						}

						// Buffer the transforms for each bone node. All bones are needed because we need the
						// parent transforms to be correct, even if there isn't a key frame for the parent.
						snapshot.m_b2w.reserve(m_bones.size());
						for (auto id : bone_id_lookup)
						{
							auto& node = *m_bones[id].bone->GetNode();
							auto b2w = To<m4x4>(node.EvaluateGlobalTransform(keytime.time));
							//auto b2w = To<m4x4>(node.EvaluateLocalTransform(keytime.time)); //todo test local. May not need parents
							snapshot.m_b2w.push_back(b2w);
						}

						// Start the worker to add key frames for each bone with a key at this time
						tasks.emplace_back(std::async(std::launch::async, [&animations, &bone_id_lookup, snapshot = std::move(snapshot)]
						{
							// Loop over the keys in this snapshot
							for (auto const& key : snapshot.m_keys)
							{
								// All bone nodes should be captured in the pose snapshot
								auto const& bone_id = bone_id_lookup[key.m_bone_index];
								auto const& b2w = snapshot.m_b2w[key.m_bone_index];
								auto const& p2w = key.m_parent_index != -1 ? snapshot.m_b2w[key.m_parent_index] : m4x4::Identity();
								auto b2p = InvertFast(p2w) * b2w;

								BoneKey keyframe = {
									b2p.pos,
									quat(b2p.rot),
									b2p.rot.scale().trace(),
									snapshot.m_time,
									uint64_t(key.m_interpolation),
								};

								// Add this key frame to the appropriate track of each animation that contains this bone.
								for (auto& animation : animations)
								{
									// Find the index of 'bone_id' within the skeleton
									auto idx = pr::index_of(animation.m_skel->m_bone_ids, bone_id);
									if (idx == isize(animation.m_skel->m_bone_ids))
										continue;

									// These transforms are the bone offset relative to the parent over time.
									animation.track(idx)[key.m_key_index] = keyframe;
								}
							}
						}));
					});

					// Wait for all tasks
					for (auto& task : tasks)
						task.wait();

					// Output the animations
					for (auto& anim : animations)
						m_scene.m_animations.push_back(std::move(anim));
				}
			}
		}

		// Find the set of unique key frame times for 'node'
		KeyTimes FindKeyFrameTimes(FbxNode& node) const
		{
			// Channels of a node's transform (x, y, z etc)
			struct Curve
			{
				FbxAnimCurve const* m_curve = nullptr;
				size_t size() const { return m_curve->KeyGetCount(); }
				FbxTime operator[](size_t i) const { return m_curve->KeyGetTime(int(i)); }
			};
			auto channels = {
				node.LclTranslation.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_X),
				node.LclTranslation.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_Y),
				node.LclTranslation.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_Z),
				node.LclRotation.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_X),
				node.LclRotation.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_Y),
				node.LclRotation.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_Z),
				node.LclScaling.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_X),
				node.LclScaling.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_Y),
				node.LclScaling.GetCurve(m_layer, FBXSDK_CURVENODE_COMPONENT_Z),
			};

			vector<Curve, 9, true> curves;
			size_t max_keys = 0;

			// Read the non-null, non-empty transform animation curves
			for (auto curve : channels)
			{
				if (curve == nullptr || curve->KeyGetCount() == 0) continue;
				max_keys = std::max(max_keys, s_cast<size_t>(curve->KeyGetCount()));
				curves.push_back({ curve });
			}

			// Find the set of unique key frame times for 'node'
			KeyTimes keytimes; keytimes.reserve(max_keys); // could be more than this, but it's a reasonable inital guess
			zip<EZip::Unique, FbxTime>(curves, [&keytimes, &curves](FbxTime time, size_t idx)
			{
				// Assume the same interpolation for all transform channels
				auto interp = curves[idx].m_curve->KeyGetInterpolation(0);
				keytimes.push_back({ time, interp });
			});
			return keytimes;
		}

		// Get the mesh bounding box
		BBox BoundingBox(FbxMesh& mesh) const
		{
			mesh.ComputeBBox();
			auto min = To<v4>(mesh.BBoxMin.Get());
			auto max = To<v4>(mesh.BBoxMax.Get());
			return BBox{ (max + min) * 0.5f, (max - min) * 0.5f };
		}

		// Report progress
		void Progress(int64_t step, int64_t total, char const* message, int nest = 0) const
		{
			if (m_opts.m_progress.cb == nullptr) return;
			if (m_opts.m_progress.cb(m_opts.m_progress.ctx, step, total, message, nest)) return;
			throw std::runtime_error("user cancelled");
		}
	};

	// Dump an fbx scene to 'std::ostream'
	struct Dumper 
	{
		using NodeAndLevel = struct NodeAndLevel { FbxNode* node; int level; };

		std::ostream& m_out;
		FbxScene& m_fbxscene;
		DumpOptions const& m_opts;
		MeshNodeMap m_meshes;
		BoneNodeMap m_bones;

		Dumper(FbxScene& fbxscene, DumpOptions const& opts, std::ostream& out)
			: m_out(out)
			, m_fbxscene(fbxscene)
			, m_opts(opts)
			, m_meshes()
			, m_bones()
		{
			if (m_opts.m_convert_axis_system)
			{
				auto basis0 = FbxAxisSystem{ FbxAxisSystem::eYAxis, FbxAxisSystem::eParityOdd, FbxAxisSystem::eRightHanded };
				auto basis1 = m_fbxscene.GetGlobalSettings().GetAxisSystem();
				if (basis0 != basis1)
					basis0.ConvertScene(&m_fbxscene);
			}

			out << std::showpos;
		}

		// Read the scene
		void Do()
		{
			TraverseHierarchy(m_fbxscene, m_opts.m_parts, m_meshes, m_bones);

			if (AllSet(m_opts.m_parts, EParts::GlobalSettings))
				DumpGlobalSettings();
			if (AllSet(m_opts.m_parts, EParts::NodeHierarchy))
				DumpHierarchy();
			if (AllSet(m_opts.m_parts, EParts::Materials))
				DumpMaterials();
			if (AllSet(m_opts.m_parts, EParts::Meshes))
				DumpGeometry();
			if (AllSet(m_opts.m_parts, EParts::Skeletons))
				DumpSkeletons();
			if (AllSet(m_opts.m_parts, EParts::Animation))
				DumpAnimations();
		}

		// Output the scene global settings
		void DumpGlobalSettings()
		{
			//auto basis = m_fbxscene.GetGlobalSettings().GetAxisSystem();
		}

		// Output the scene node hierarchy
		void DumpHierarchy()
		{
			m_out << " NODE HIERARCHY ===================================================================================================\n";

			// Root nodes for meshes, skeletons can occur at any level.
			// Any mesh/skeleton node whose parent is not a mesh/skeleton node is the start of a new mesh/skeleton hierarchy
			vector<NodeAndLevel> stack = { {m_fbxscene.GetRootNode(), 0} };
			for (; !stack.empty(); )
			{
				auto [node, level] = stack.back();
				stack.pop_back();

				m_out << Indent(level) << "Node \"" << node->GetName() << "\":\n";

				// Attributes
				{
					m_out << Indent(level + 1) << "Attributes: [" << node->GetNodeAttributeCount() << "] ";
					for (int i = 0, iend = node->GetNodeAttributeCount(); i != iend; ++i)
					{
						auto& attr = *node->GetNodeAttributeByIndex(i);
						m_out << (i != 0 ? ", " : "") << To<std::string_view>(attr.GetAttributeType());
					}
					m_out << "\n";
				}

				// Node Transform
				{
					auto o2p = To<m4x4>(node->EvaluateLocalTransform());
					DumpTransform(level + 1, o2p);
				}

				// Recurse in depth first order
				for (int i = node->GetChildCount(); i-- != 0; )
					stack.push_back({ node->GetChild(i), level + 1 });
			}

			m_out << "\n";
		}

		// Output the material definitions
		void DumpMaterials()
		{
			m_out << " MATERIALS ===================================================================================================\n";
			m_out << "\n";
		}

		// Output the Mesh definitions
		void DumpGeometry()
		{
			m_out << " MESHES ===================================================================================================\n";

			// Process each mesh in a worker thread, because triangulation is really slow
			for (auto const& [id, meshnode] : m_meshes)
			{
				if (m_opts.m_triangulate_meshes && !meshnode.mesh->IsTriangleMesh())
				{
					FbxGeometryConverter converter(m_fbxscene.GetFbxManager());
					const_cast<MeshNode&>(meshnode).mesh = FbxCast<FbxMesh>(converter.Triangulate(meshnode.mesh, true));
				}

				auto& mesh = *meshnode.mesh;
				auto& node = *mesh.GetNode();
				auto level = meshnode.level;

				m_out << Indent(level) << "Mesh \"" << node.GetName() << "\" (" << mesh.GetUniqueID() << "):\n";
				m_out << Indent(level + 1) << "IsRoot: " << (meshnode.mesh == meshnode.root ? "ROOT" : "CHILD") << "\n";
				m_out << Indent(level + 1) << "Polys: " << (mesh.IsTriangleMesh() ? "TRIANGLES" : "POLYGONS") << "\n";
				m_out << Indent(level + 1) << "Mat #: " << mesh.GetElementMaterialCount() << "\n";

				// Verts
				{
					auto const* verts = mesh.GetControlPoints();

					m_out << Indent(level + 1) << "Verts: [" << mesh.GetControlPointsCount() << "]\n";
					for (int i = 0, iend = std::min(m_opts.m_summary_length, mesh.GetControlPointsCount()); i != iend; ++i)
					{
						m_out << Indent(level + 2) << Round(To<v4>(verts[i]).xyz) << "\n";
					}
					DumpSummary(level + 2, mesh.GetControlPointsCount());
				}

				// Faces
				{
					auto const* layer0 = mesh.GetLayer(0);
					auto const* materials = layer0->GetMaterials();

					//auto const* faces = mesh.GetPolygonCount
					m_out << Indent(level + 1) << "Faces: [" << mesh.GetPolygonCount() << "]\n";
					for (int f = 0, fend = std::min(m_opts.m_summary_length, mesh.GetPolygonCount()); f != fend; ++f)
					{
						// Polygons
						m_out << Indent(level + 2) << "[" << mesh.GetPolygonSize(f) << "] ";
						for (int p = 0, pend = mesh.GetPolygonSize(f); p != pend; ++p)
						{
							m_out << (p != 0 ? ", " : "") << mesh.GetPolygonVertex(f, p);
						}

						// Get the material used on this face
						if (materials != nullptr)
						{
							auto mat = GetLayerElement<FbxSurfaceMaterial*>(materials, f, -1, -1);
							uint64_t mat_id = mat ? mat->GetUniqueID() : 0;
							m_out << "  MatID=" << mat_id;
						}

						m_out << "\n";
					}
					DumpSummary(level + 2, mesh.GetPolygonCount());
				}

				// Bounding Box
				{
					mesh.ComputeBBox();
					auto min = To<v4>(mesh.BBoxMin.Get());
					auto max = To<v4>(mesh.BBoxMax.Get());
					BBox bb{ (max + min) * 0.5f, (max - min) * 0.5f };
					m_out << Indent(level + 1) << "BBox: c=(" << Round(bb.Centre().xyz) << "), r=(" << Round(bb.Radius().xyz) << ")\n";
				}

				// Node Transform
				{
					m_out << Indent(level + 1) << "Mesh-to-Object:\n";
					DumpTransform(level + 2, To<m4x4>(node.EvaluateGlobalTransform()));
					m_out << Indent(level + 1) << "Mesh-to-Parent:\n";
					DumpTransform(level + 2, To<m4x4>(node.EvaluateLocalTransform()));
				}

				// Skinning info
				if (AllSet(m_opts.m_parts, EParts::Skinning))
					DumpSkinning(meshnode, level + 1);
			}

			m_out << "\n";
		}

		// Output the Skeleton definitions
		void DumpSkeletons()
		{
			m_out << " SKELETONS ===================================================================================================\n";

			for (auto const& [id, bonenode] : m_bones)
			{
				auto& bone = *bonenode.bone;
				auto& node = *bone.GetNode();
				auto level = bonenode.level;

				m_out << Indent(level) << "Bone \"" << node.GetName() << "\":\n";
				m_out << Indent(level + 1) << "ID: " << bone.GetUniqueID() << "\n";
				m_out << Indent(level + 1) << "IsRoot: " << (bonenode.bone == bonenode.root ? "ROOT" : "CHILD") << "\n";
				m_out << Indent(level + 1) << "Type: " << To<std::string_view>(bone.GetSkeletonType()) << "\n";

				// Bind pose
#if 0 //todo
				{
					m4x4 bp2o;
					auto bind_pose = FindBindPose(node);
					if (int idx = bind_pose ? bind_pose->Find(&node) : -1; idx != -1)
						bp2o = To<m4x4>(bind_pose->GetMatrix(idx));
					else // Fall-back to time zero as the bind pose
						bp2o = To<m4x4>(node.EvaluateGlobalTransform(0));

					m_out << Indent(level + 1) << "BindPose-to-Object:\n";
					DumpTransform(level + 2, bp2o);
				}
#endif
			}
			m_out << "\n";
		}

		// Output the Animation definitions
		void DumpAnimations()
		{
			// todo
		}

		// Output the skinning info for 'meshnode'
		void DumpSkinning(MeshNode const& meshnode, int level)
		{
			auto& fbxmesh = *meshnode.mesh;

			auto const* fbxskeleton = FindSkeleton(fbxmesh);
			if (fbxskeleton == nullptr)
				return; // Mesh has no skeleton => not animated

			m_out << Indent(level) << "Skin:\n";
			m_out << Indent(level + 1) << "Skeleton: \"" << fbxskeleton->GetNode()->GetName() << "\" (" << fbxskeleton->GetUniqueID() << ")\n";

			// Get the skinning data for this mesh
			m_out << Indent(level + 1) << "Deformers: [" << fbxmesh.GetDeformerCount(FbxDeformer::eSkin) << "]\n";
			for (int d = 0, dend = fbxmesh.GetDeformerCount(FbxDeformer::eSkin); d != dend; ++d)
			{
				auto& fbxskin = *FbxCast<FbxSkin>(fbxmesh.GetDeformer(d, FbxDeformer::eSkin));

				m_out << Indent(level + 2) << "Clusters: [" << fbxskin.GetClusterCount() << "]\n";
				for (int b = 0, bend = fbxskin.GetClusterCount(); b != bend; ++b)
				{
					auto& cluster = *fbxskin.GetCluster(b);
					if (cluster.GetControlPointIndicesCount() == 0)
						continue;

					auto& bone = *Find<FbxSkeleton>(*cluster.GetLink());
					m_out << Indent(level + 3) << "Cluster: \"" << cluster.GetName() << "\" (" << b << "):\n";
					m_out << Indent(level + 4) << "Link: " << *cluster.GetLink()->GetName() << "\n";
					m_out << Indent(level + 4) << "Bone: " << bone.GetName() << "(" << bone.GetUniqueID() << ")\n";
					m_out << Indent(level + 4) << "LinkMode: " << To<std::string_view>(cluster.GetLinkMode()) << "\n";

					// Get the span of vert indices and weights
					auto indices = std::span<int>{ cluster.GetControlPointIndices(), static_cast<size_t>(cluster.GetControlPointIndicesCount()) };
					auto weights = std::span<double>{ cluster.GetControlPointWeights(), static_cast<size_t>(cluster.GetControlPointIndicesCount()) };
					m_out << Indent(level + 4) << "Bone Weights:\n";
					for (int w = 0, wend = std::min(cluster.GetControlPointIndicesCount(), m_opts.m_summary_length); w != wend; ++w)
					{
						m_out << Indent(level + 5) << "VIdx=(" << indices[w] << "), Weight=(" << weights[w] << ")\n";
					}
					DumpSummary(level + 5, cluster.GetControlPointIndicesCount());

					FbxAMatrix ref_to_world; cluster.GetTransformMatrix(ref_to_world);
					FbxAMatrix link_to_world; cluster.GetTransformLinkMatrix(link_to_world);
					m_out << Indent(level + 4) << "Cluster Xform:\n";
					DumpTransform(level + 5, To<m4x4>(ref_to_world));
					m_out << Indent(level + 4) << "Link Xform:\n";
					DumpTransform(level + 5, To<m4x4>(link_to_world));
				}
			}
		}

		// Dump transform helper
		void DumpTransform(int level, m4x4 const& a2b) const
		{
			m_out << Indent(level) << "Position: " << Round(a2b.pos.xyz) << "\n";
			m_out << Indent(level) << "Rotation: " << Round(RadiansToDegrees(EulerAngles(quat(a2b.rot)).xyz)) << "\n";
			m_out << Indent(level) << "Scale   : " << Round(a2b.rot.scale().trace().xyz) << "\n";
		}

		// Output the 'and so on' indicator if 'count' is greater than the summary length
		void DumpSummary(int level, int count)
		{
			if (count > m_opts.m_summary_length)
				m_out << Indent(level) << "...\n";
		}

		// Rounding helpers
		static float Round(float v)
		{
			return Quantise(v, 1 << 15);
		}
		static v3 Round(v3 const& v)
		{
			return CompOp(v, [](float x) { return Round(x); });
		}
		static v4 Round(v4 const& v)
		{
			return CompOp(v, [](float x) { return Round(x); });
		}
		static m4x4 Round(m4x4 const& m)
		{
			CompOp(m, [](v4 const& v) { return Round(v); });
		}

		// Indent helper
		static std::string_view Indent(int amount)
		{
			constexpr static char const space[] = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
			constexpr static int len = (int)_countof(space);
			return std::string_view(space, amount < len ? amount : len);
		}
	};

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
		virtual size_t Write(const void* data, FbxUInt64 size) override { return m_out.write(static_cast<char const*>(data), static_cast<std::streamsize>(size)).good() ? s_cast<size_t>(size) : 0; }

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
		virtual size_t Read(void* pData, FbxUInt64 pSize) const override { return s_cast<size_t>(m_src.read(static_cast<char*>(pData), static_cast<std::streamsize>(pSize)).gcount()); }

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

	// Write an FBX scene to disk
	static void Export(Context& manager, std::ostream& out, FbxScene& scene, char const* format = Formats::FbxBinary, ErrorList* errors = nullptr)
	{
		ExporterPtr exporter(Check(FbxExporter::Create(manager, ""), "Failed to create Exporter"));

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
	static FbxScene* Import(Context& manager, std::istream& src, char const* format = Formats::FbxBinary, ErrorList* errors = nullptr)
	{
		auto* scene = Check(FbxScene::Create(manager, ""), "Error: Unable to create FBX scene");

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
		auto result = importer->Import(scene);
		if (!result || importer->GetStatus() != FbxStatus::eSuccess)
			throw std::runtime_error(std::format("Failed to read fbx from file. {}", importer->GetStatus().GetErrorString()));

		return scene;
	}
}

extern "C"
{
	using namespace pr::geometry::fbx;

	std::mutex g_mutex;
	std::vector<std::unique_ptr<Context>> g_contexts;
	HINSTANCE g_instance;

	// DLL entry point
	BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
	{
		g_instance = hInstance;
		switch (ul_reason_for_call)
		{
			case DLL_PROCESS_ATTACH: break;
			case DLL_PROCESS_DETACH: break;
			case DLL_THREAD_ATTACH: break;
			case DLL_THREAD_DETACH: break;
		}
		return TRUE;
	}

	// Create a dll context
	__declspec(dllexport) Context* __stdcall Fbx_Initialise(ErrorHandler error_cb)
	{
		try
		{
			std::lock_guard<std::mutex> lock(g_mutex);
			g_contexts.push_back(std::unique_ptr<Context>(new Context(error_cb)));
			return g_contexts.back().get();
		}
		catch (std::exception const& ex)
		{
			error_cb(ex.what());
			return nullptr;
		}
	}

	// Release a dll context
	__declspec(dllexport) void __stdcall Fbx_Release(Context* ctx)
	{
		if (ctx == nullptr)
			return;

		try
		{
			std::lock_guard<std::mutex> lock(g_mutex);
			g_contexts.erase(std::remove_if(begin(g_contexts), end(g_contexts), [ctx](auto& p) { return p.get() == ctx; }), end(g_contexts));
		}
		catch (std::exception const& ex)
		{
			ctx->m_error_cb(ex.what());
		}
	}

	// Load an fbx scene
	__declspec(dllexport) SceneData* __stdcall Fbx_Scene_Load(Context& ctx, std::istream& src)
	{
		try
		{
			auto fbxscene = Import(ctx, src);
			ctx.m_scenes.emplace_back(new SceneData(fbxscene));
			return ctx.m_scenes.back().get();
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return nullptr;
		}
	}

	// Save an fbx scene
	__declspec(dllexport) void __stdcall Fbx_Scene_Save(Context& ctx, SceneData const& scene, std::ostream& out, char const* format)
	{
		try
		{
			Export(ctx, out, *scene.m_fbxscene, format);
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	// Read meta data about the scene
	__declspec(dllexport) SceneProps __stdcall Fbx_Scene_ReadProps(Context& ctx, SceneData const& scene)
	{
		try
		{
			if (scene.m_fbxscene == nullptr)
				throw std::runtime_error("Scene is null");

			// Read properties from the scene
			SceneProps props = {};
			props.m_animation_stack_count = scene.m_fbxscene->GetSrcObjectCount<FbxAnimStack>();
			props.m_frame_rate = FbxTime::GetFrameRate(scene.m_fbxscene->GetGlobalSettings().GetTimeMode());
			props.m_mesh_count = pr::isize(scene.m_meshes);
			props.m_material_count = pr::isize(scene.m_materials);
			props.m_skeleton_count = pr::isize(scene.m_skeletons);
			props.m_animation_count = pr::isize(scene.m_animations);
			return props;
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return {};
		}
	}

	// Read the hierarchy from the scene
	__declspec(dllexport) void __stdcall Fbx_Scene_Read(Context& ctx, SceneData& scene, ReadOptions const& options)
	{
		try
		{
			if (scene.m_fbxscene == nullptr)
				throw std::runtime_error("Scene is null");

			Reader reader(scene, options);
			reader.Do();
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	// Dump info about the scene to 'out'
	__declspec(dllexport) void __stdcall Fbx_Scene_Dump(Context& ctx, SceneData const& scene, DumpOptions const& options, std::ostream& out)
	{
		try
		{
			Dumper dumper(*scene.m_fbxscene, options, out);
			dumper.Do();
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
		}
	}

	// Access a mesh by index
	__declspec(dllexport) Mesh __stdcall Fbx_Scene_MeshGet(Context& ctx, SceneData const& scene, int i)
	{
		try
		{
			auto const& m = scene.m_meshes[i];
			return Mesh{
				.m_id = m.m_id,
				.m_name = m.m_name,
				.m_vbuf = m.m_vbuf,
				.m_ibuf = m.m_ibuf,
				.m_nbuf = m.m_nbuf,
				.m_skin = Skin{
					.m_skel_id = m.m_skin.m_skel_id,
					.m_offsets = m.m_skin.m_offsets,
					.m_bones = m.m_skin.m_bones,
					.m_weights = m.m_skin.m_weights,
				},
				.m_bbox = m.m_bbox,
				.m_o2p = m.m_o2p,
				.m_level = m.m_level,
			};
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return {};
		}
	}

	// Access a skeleton by index
	__declspec(dllexport) Skeleton __stdcall Fbx_Scene_SkeletonGet(Context& ctx, SceneData const& scene, int i)
	{
		try
		{
			auto const& s = scene.m_skeletons[i];
			return Skeleton{
				.m_id = s.m_id,
				.m_bone_ids = s.m_bone_ids,
				.m_names = s.m_names,
				.m_o2bp = s.m_o2bp,
				.m_hierarchy = s.m_hierarchy,
			};
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return {};
		}
	}

	// Access an animation by index
	__declspec(dllexport) Animation __stdcall Fbx_Scene_AnimationGet(Context& ctx, SceneData const& scene, int i)
	{
		try
		{
			auto const& a = scene.m_animations[i];
			return Animation{
				.m_skel_id = a.m_skel->m_id,
				.m_offsets = a.m_offsets,
				.m_tracks = a.m_alltracks,
				.m_time_range = a.m_time_range,
			};
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return {};
		}
	}

	// Access a material by id
	__declspec(dllexport) Material __stdcall Fbx_Scene_MaterialGetById(Context& ctx, SceneData const& scene, uint64_t mat_id)
	{
		try
		{
			auto& m = scene.m_materials.at(mat_id);
			return Material{
				.m_ambient = m.m_ambient,
				.m_diffuse = m.m_diffuse,
				.m_specular = m.m_specular,
				.m_tex_diff = m.m_tex_diff,
			};
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return {};
		}
	}

	// Access a skeleton by index
	__declspec(dllexport) Skeleton __stdcall Fbx_Scene_SkeletonGetById(Context& ctx, SceneData const& scene, uint64_t skel_id)
	{
		try
		{
			auto& s = pr::get_if(scene.m_skeletons, [&skel_id](SkeletonData const& s) { return s.m_id == skel_id; });
			return Skeleton{
				.m_id = s.m_id,
				.m_bone_ids = s.m_bone_ids,
				.m_names = s.m_names,
				.m_o2bp = s.m_o2bp,
				.m_hierarchy = s.m_hierarchy,
			};
		}
		catch (std::exception const& ex)
		{
			ctx.m_error_cb(ex.what());
			return {};
		}
	}

	// Static function signature checks
	void Fbx::StaticChecks()
	{
		#define PR_FBX_API_CHECK(prefix, name, function_type) static_assert(std::is_same_v<Fbx::prefix##name##Fn, decltype(&Fbx_##prefix##name)>, "Function signature mismatch for Fbx_"#prefix#name);
		PR_FBX_API(PR_FBX_API_CHECK)
		#undef PR_FBX_API_CHECK
	}
}
