//*******************************************************************************************
//
// A class to interpret strings into LdrObject objects
// and past them on to the data manager
//
//*******************************************************************************************
#include "Stdafx.h"
#if USE_NEW_PARSER
#include "LineDrawer/Objects/Parser.h"
#include "LineDrawer/Objects/AnimationData.h"
#include "LineDrawer/Source/LineDrawer.h"
//#include "pr/filesys/filesys.h"
//#include "pr/storage/xfile/XFile.h"
//#include "LineDrawer/Source/FileLoader.h"

using namespace pr;

enum
{
	MaxKeywordLength = 256
};

// Generated enum of all of the line drawer keywords
enum ELdrKW
{
	#define LDR_KEYWORD(identifier, hash)	ELdrKW_##identifier	= hash,
	#define LDR_OBJECT(identifier, hash)	ELdrKW_##identifier	= hash,
	#include "LineDrawer/Objects/LdrObjects.inc"
	ELdrKW_Unknown = 0
};

// Exceptions thrown during parsing
struct LdrParseException
{
	std::string m_msg;
	LdrParseException(std::string const& msg) :m_msg(msg) {}
};

// Object parsing function forward declarations
#define LDR_OBJECT(identifier, hash) template <typename Parser> LdrObject* Parse##identifier(Parser& parser, LineDrawer& ldr);
#include "LineDrawer/Objects/LdrObjects.inc"

// Return the context from the current location in the source
template <typename Parser> inline std::string GetContext(Parser const& parser)
{
	return Fmt("Near \"%20s\"", parser.GetSource());
}

// Hash a string into a constant
inline std::size_t HashKeyword(char const* keyword)
{
	char kw[MaxKeywordLength];
	return str::Hash(str::LowerCase(keyword, kw, MaxKeywordLength), 0);
}

// Convert a keyword string to an id
inline ELdrKW ParseKeyword(char const* keyword)
{
	switch (HashKeyword(keyword))
	{
	default: return ELdrKW_Unknown;
	#define LDR_KEYWORD(identifier, hash)	case hash: return ELdrKW_##identifier;
	#define LDR_OBJECT(identifier, hash)	case hash: return ELdrKW_##identifier;
	#include "LineDrawer/Objects/LdrObjects.inc"
	}
}

// Extract a 3 element vector from the source
template <typename Parser> void ParseVector3(Parser& parser, v4& vec, float w)
{
	parser.FindSectionStart();
	parser.ExtractVector3(vec, w);
	parser.FindSectionEnd();
}

// Extract a 4 element vector from the source
template <typename Parser> void ParseVector4(Parser& parser, v4& vec)
{
	parser.FindSectionStart();
	parser.ExtractVector4(vec);
	parser.FindSectionEnd();
}

// Extract a transform from the source
template <typename Parser> void ParseTransform(Parser& parser, m4x4& transform)
{
	parser.FindSectionStart();
	parser.Extractm4x4(transform);
	while (parser.IsKeyword())
	{
		char kw[MaxKeywordLength];
		parser.GetKeyword(kw, MaxKeywordLength);
		switch (ParseKeyword(kw))
		{
		default: throw LdrParseException(0, Fmt("Unknown keyword found in Transform.\n") + GetContext(parser));
		case ELdrKW_Transpose:		transform.Transpose();		break;
		case ELdrKW_Inverse:		transform.Inverse();		break;
		case ELdrKW_Orthonormalise:	transform.Orthonormalise(); break;
		}
	}
	parser.FindSectionEnd();
}

// Extract a quaternion from the source
template <typename Parser> void ParseQuaternion(Parser& parser, Quat& quat)
{
	parser.FindSectionStart();
	parser.ExtractReal(quat.x);
	parser.ExtractReal(quat.y);
	parser.ExtractReal(quat.z);
	parser.ExtractReal(quat.w);
	parser.FindSectionEnd();
}

// Extract a colour from the source
template <typename Parser> void ParseColour(Parser& parser, Colour32& colour)
{
	parser.FindSectionStart();
	parser.ExtractInt(colour.m_aarrggbb, 16);
	parser.FindSectionEnd();
}

// Extract a random position from the source
template <typename Parser> void ParseRandomPosition(Parser& parser, v4& position)
{
	v4 centre; float range;
	parser.FindSectionStart();
	parser.ExtractVector3(centre, 1.0f);
	parser.ExtractFloat(range);
	parser.FindSectionEnd();
	position = v4Random3(centre, range, 1.0f);
}

// Extract a random transform from the source
template <typename Parser> void ParseRandomTransform(Parser& parser, m4x4& transform)
{
	v4 centre; float range;
	parser.FindSectionStart();
	parser.ExtractVector3(centre, 1.0f);
	parser.ExtractFloat(range);
	parser.FindSectionEnd();
	transform = m4x4Random(centre, range);
}

// Extract euler angles from the source
template <typename Parser> void ParseEuler(Parser& parser, float pitch, float yaw, float roll)
{
	parser.FindSectionStart();
	parser.ExtractReal(pitch);	pitch = DegreesToRadians(pitch);
	parser.ExtractReal(yaw);	yaw   = DegreesToRadians(yaw);
	parser.ExtractReal(roll);	roll  = DegreesToRadians(roll);
	parser.FindSectionEnd();
}

// Extract a direction from the source
template <typename Parser> void ParseAxisDirection(Parser& parser, m3x3& orientation)
{
	uint axis; v4 direction;
	parser.FindSectionStart();
	parser.ExtractInt(axis, 10);
	parser.ExtractVector3(direction, 0);
	parser.FindSectionEnd();
	OrientationFromDirection(orientation, direction, axis);
}

// Extract animation data from the source
template <typename Parser> void ParseAnimation(Parser& parser, AnimationData& animation)
{
	uint style;
	parser.FindSectionStart();
	parser.ExtractInt(style, 10);
	parser.ExtractReal(animation.m_period);
	parser.ExtractVector3(animation.m_velocity, 0.0f);
	parser.ExtractVector3(animation.m_rotation_axis, 0.0f);
	parser.ExtractReal(animation.m_angular_speed);
	parser.FindSectionEnd();
	if (style >= AnimationData::NumberOf) throw LdrParseException(0, "Invalid 'Style' value found in Animation Data")
	animation.m_style = static_cast<AnimationData::Style>(style);
	animation.m_rotation_axis.Normalise3IfNonZero();
}

// Extract  camera view data from the source
template <typename Parser> void ParseCamera(Parser& parser, ViewMask& view_mask, CameraView& view)
{
	parser.FindSectionStart();
	while (parser.IsKeyword())
	{
		char kw[MaxKeywordLength];
		parser.GetKeyword(kw, MaxKeywordLength);
		switch (ParseKeyword(kw))
		{
		default: throw LdrParseException(ELdrObjectException_SyntaxError, "Unknown keyword found in Camera description");
		case ELdrKW_Position:
			ParseVector3(parser, view.m_camera_position, 1.0f);
			m_view_mask[ViewMask::PositionX] = true;
			m_view_mask[ViewMask::PositionY] = true;
			m_view_mask[ViewMask::PositionZ] = true;
			break;
		case ELdrKW_Up:
			ParseVector3(parser, m_view.m_camera_up, 0.0f);
			m_view_mask[ViewMask::UpX] = true;
			m_view_mask[ViewMask::UpY] = true;
			m_view_mask[ViewMask::UpZ] = true;
			break;
		case ELdrKW_LookAt:
			ParseVector3(parser, m_view.m_lookat_centre, 1.0f);
			m_view_mask[ViewMask::LookAt] = true;
			break;
		case ELdrKW_PositionX:
			parser.ExtractReal(m_view.m_camera_position.x);
			m_view_mask[ViewMask::PositionX] = true;
			break;
		case ELdrKW_PositionY:
			parser.ExtractReal(m_view.m_camera_position.y);
			m_view_mask[ViewMask::PositionY] = true;
			break;
		case ELdrKW_PositionZ:
			parser.ExtractReal(m_view.m_camera_position.z);
			m_view_mask[ViewMask::PositionZ] = true;
			break;
		case ELdrKW_FOV:
			parser.ExtractReal(m_view.m_fov);
			m_view_mask[ViewMask::FOV] = true;
			break;
		case ELdrKW_Aspect:
			parser.ExtractReal(m_view.m_aspect);
			m_view_mask[ViewMask::Aspect] = true;
			break;
		case ELdrKW_Near:
			parser.ExtractReal(m_view.m_near);
			m_view_mask[ViewMask::Near] = true;
			break;
		case ELdrKW_Far:
			parser.ExtractReal(m_view.m_far);
			m_view_mask[ViewMask::Far] = true;
			break;
		case ELdrKW_AlignX:
			m_view_mask[ViewMask::AlignX] = true;
			break;
		case ELdrKW_AlignY:
			m_view_mask[ViewMask::AlignY] = true;
			break;
		case ELdrKW_AlignZ:
			m_view_mask[ViewMask::AlignZ] = true;
			break;
		}
	}
	parser.FindSectionEnd();
}

// Extract lock data from the source
template <typename Parser> void ParseLocks(Parser& parser, LockMask& locks)
{
	parser.FindSectionStart();
	while (parser.IsKeyword())
	{
		char kw[MaxKeywordLength];
		parser.GetKeyword(kw, MaxKeywordLength);
		switch (ParseKeyword(kw))
		{
		default: throw LdrParseException(0, "Unknown keyword found in Lock description");
		case ELdrKW_TransX:			{ locks[LockMask::TransX] = true; break; }
		case ELdrKW_TransY:			{ locks[LockMask::TransY] = true; break; }
		case ELdrKW_TransZ:			{ locks[LockMask::TransZ] = true; break; }
		case ELdrKW_RotX:			{ locks[LockMask::RotX] = true; break; }
		case ELdrKW_RotY:			{ locks[LockMask::RotY] = true; break; }
		case ELdrKW_RotZ:			{ locks[LockMask::RotZ] = true; break; }
		case ELdrKW_Zoom:			{ locks[LockMask::Zoom] = true; break; }
		case ELdrKW_CameraRelative:	{ locks[LockMask::CameraRelative] = true; break; }
		}
	}
	parser.FindSectionEnd();
}

// Parse one of the common object modifiers.
// Returns true if the keyword was recognised and parsed
template <typename Parser> bool ParseObjectModifier(Parser& parser, ELdrKW keyword, LdrObject* ldr_obejct)
{
	switch (keyword)
	{
	default return false;
	case ELdrKW_Position:			ParseVector3		(parser, ldr_object->m_object_to_parent.pos, 1.0f); break;
	case ELdrKW_Transform:			ParseTransform		(parser, ldr_object->m_object_to_parent); break;
	case ELdrKW_AxisDirection:		ParseAxisDirection	(parser, ldr_object->m_object_to_parent.Getm3x3()); break;
	case ELdrKW_RandomPosition:		ParseRandomPosition	(parser, ldr_object->m_object_to_parent.pos); break;
	case ELdrKW_RandomTransform:	ParseRandomTransform(parser, ldr_object->m_object_to_parent); break;
	case ELdrKW_RandomOrientation:	ldr_object->m_object_to_parent.Getm3x3().Random(); break;
	case ELdrKW_RandomColour:		ldr_object->SetColour(Colour32RandomRGB(), true, false); break;
	case ELdrKW_Hidden:				ldr_object->SetEnable(false, true); break;
	case ELdrKW_Wireframe:			ldr_object->SetWireframe(true, true); break;
	case ELdrKW_Animation:			ParseAnimation(parser, ldr_object->m_animation); break;
	case ELdrKW_Quaternion:			{ Quat quat;	ParseQuaternion(parser, quat); ldr_object->m_object_to_parent.Getm3x3().CreateFrom(quat); } break;
	case ELdrKW_Colour:				{ Colour32 col;	ParseColour(parser, col); ldr_object->SetColour(col, true, false); } break;
	case ELdrKW_ColourMask:			{ Colour32 col;	ParseColour(parser, col); ldr_object->SetColour(col, true, true); } break;
	case ELdrKW_Euler:				{ v4 euler;		ParseEuler(parser, euler.x, euler.y, euler.z); ldr_object->m_object_to_parent.Getm3x3().CreateFrom(euler.x, euler.y, euler.z); } break;
	case ELdrKW_Scale:				{ v4 scale;		ParseVector3(parser, scale, 0.0f); ldr_object->m_object_to_parent.x *= scale.x; ldr_object->m_object_to_parent.y *= scale.y; ldr_object->m_object_to_parent.z *= scale.z; } break;
	}
	return true;
}

// Parse an object description.
// Returns true if the keyword was recognised and parsed
template <typename Parser> bool ParseObject(Parser& parser, ELdrKW keyword, LineDrawer& ldr, TLdrObjectPtrVec& store)
{
	// Parse the keyword
	switch (keyword)
	{
	default: return false;
	// If Parse##identifier returns null then the parsed object was valid but did not contain any data
	#define LDR_OBJECT(identifier, hash)\
	case ELdrKW_#identifier:\
	{\
		LdrObject* object = Parse##identifier(parser, ldr);\
		if (!object) store.push_back(object);\
		return true;\
	}
	#include "LineDrawer/Objects/LdrObjects.inc"
	}
}

//******************************************************************************************************
// Allocate a new object
template <typename ObjectType, typename Parser> ObjectType* NewObject(Parser& parser, LineDrawer& ldr)
{
	std::string name; Colour32 colour;
	parser.ExtractIdentifier(name);
	parser.ExtractInt(colour.m_aarrggbb, 16);
	return new ObjectType(ldr, name, colour);
}

// Parse child objects and object modifiers, common to most objects
template <typename Parser> void ParseStandardChildren(Parser& parser, LineDrawer& ldr, LdrObject* ldr_object)
{
	char kw[MaxKeywordLength];
	ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
	if (ParseObjectModifier(parser, keyword, ldr_object)) return;
	if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) return;
	throw LdrParseException("Unknown keyword found");
}

//******************************************************************************************************
// Extract a group
template <typename Parser> LdrObject* ParseGroup(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TGroup> ldr_object = NewObject<TGroup>(parser, ldr);

	while (!parser.IsSectionEnd())
	{
		char kw[MaxKeywordLength];
		ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
		switch (keyword)
		{
		case ELdrKW_CycleMode:
			{
				int mode;
				parser.ExtractInt(mode, 10);
				if (mode >= EMode_NumberOf) throw LdrParseException("Group cycle mode invalid");
				ldr_object->m_mode = static_cast<TGroup::EMode>(mode);
			}break;
		case ELdrKW_FPS:
			{
				float fps;
				parser.ExtractReal(fps);
				if (fps <= 0.0f) throw LdrParseException("Invalid group cycle frames per second");
				ldr_object->m_ms_per_frame = 1000.0f / fps;
			}break;
		default:
			if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
			if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
			throw LdrParseException(0, "Unknown keyword found");
		}
	}
	ldr_object->SetColour(ldr_object->m_instance.m_colour, true, true);
	ldr_object->CreateRenderObject();
	return ldr_object.release();
}

//******************************************************************************************************
// Extract a list of points from the source
template <typename Parser> LdrObject* ParsePoint(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TPoints> ldr_object = NewObject<TPoints>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 pt;
			parser.ExtractorVector3(pt, 1.0f);
			points.push_back(pt);
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points);
	return ldr_object.release();
}

//******************************************************************************************************
// Extract common object modifiers for lines
template <typename Parser> void ParseLineCommon(Parser& parser, LdrObject* ldr_object, TVecCont& points, bool& normalise, GeomType& geom_type)
{
	char kw[MaxKeywordLength];
	ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
	switch (keyword)
	{
	case ELdrKW_Normalise:	normalise = true; break;
	case ELdrKW_Colours:	geom_type |= geometry::EType_Colour; break;
	case ELdrKW_Parametric:
		{
			if (points.size() < 2) throw LdrParseException("The *Parametric keyword applies to the previous line only");
			v4& a = points[points.size() - 2];
			v4& b = points[points.size() - 1];
			parser.FindSectionStart();
			float t0; parser.ExtractFloat(t0);
			float t1; parser.ExtractFloat(t1);
			parser.FindSectionEnd();
			v4 dir = b - a;
			b = a + t1 * dir;
			a = a + t0 * dir;
		}break;
	default:
		if (ParseObjectModifier(parser, keyword, ldr_object)) break;
		if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) break;
		throw LdrParseException(0, "Unknown keyword found");
	}
}

// Extract a list of lines from the source
template <typename Parser> LdrObject* ParseLine(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	TColour32Cont colours;
	bool normalise = false;
	GeomType geom_type = geometry::EType_Vertex;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 start, end; Colour32 col0, col1;
			parser.ExtractVector3(start, 1.0f);		if (geom_type & geometry::EType_Colour) { parser.ExtractInt(col0.m_aarrggbb, 16); }
			parser.ExtractVector3(end, 1.0f);		if (geom_type & geometry::EType_Colour) { parser.ExtractInt(col1.m_aarrggbb, 16); }
			if (normalise) { end = start + (end - start).Normalise3IfNonZero(); }
			points.push_back(start);
			points.push_back(end);
			if (geom_type & geometry::EType_Colour) { colours.push_back(col0); colours.push_back(col1); }
		}
		else
		{
			ParseLineCommon(parser, ldr_object, points, normalise, geom_type);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, colours);
	return ldr_object.release();
}

// Extract a list of lines given as a point and a direction vector
template <typename Parser> LdrObject* ParseLineD(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	TColour32Cont colours;
	bool normalise = false;
	GeomType geom_type = geometry::EType_Vertex;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 start, direction; Colour32 col;
			parser.ExtractVector3(start, 1.0f);
			parser.ExtractVector3(direction, 0.0f);
			if (geom_type & geometry::EType_Colour) { parser.ExtractInt(col.m_aarrggbb, 16); }
			if (normalise) direction.Normalise3IfNonZero();
			points.push_back(start);
			points.push_back(start + direction);
			if (geom_type & geometry::EType_Colour)	{ colours.push_back(col); colours.push_back(col); }
		}
		else
		{
			ParseLineCommon(parser, ldr_object, points, normalise, geom_type);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, colours);
	return ldr_object.release();
}

// Extract a list of lines given as a point, normal, and length
template <typename Parser> LdrObject* ParseLineNL(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	TColour32Cont colours;
	bool normalise = false;
	GeomType geom_type = geometry::EType_Vertex;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			float length; v4 start, normal; Colour32 col;
			parser.ExtractVector3(start, 1.0f);
			parser.ExtractVector3(normal, 0.0f);
			parser.ExtractFloat(length);
			if (geom_type & geometry::EType_Colour) { parser.ExtractInt(col.m_aarrggbb, 16); }
			if (normalise) { length = normal.IsZero3() ? 0.0f : 1.0f / normal.Length3(); }
			points.push_back(start);
			points.push_back(start + normal * length);
			if (geom_type & geometry::EType_Colour)	{ colours.push_back(col); colours.push_back(col); }
		}
		else
		{
			ParseLineCommon(parser, ldr_object, points, normalise, geom_type);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, colours);
	return ldr_object.release();
}

// Extract a list of lines given as a connected line list
template <typename Parser> LdrObject* ParseLineList(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TPointsVec points;
	TColour32Cont colours;
	bool normalise = false;
	GeomType geom_type = geometry::EType_Vertex;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 pt; Colour32 col;
			parser.ExtractVector3(pt, 1.0f);
			if (geom_type & geometry::EType_Colour) { parser.ExtractInt(col.m_aarrggbb, 16); }
			if (!points.empty())
			{
				points.push_back(pt);
				if (geom_type & geometry::EType_Colour) { colours.push_back(col); }
			}
			points.push_back(pt);
			if (geom_type & geometry::EType_Colour) { colours.push_back(col); }
		}
		else
		{
			ParseLineCommon(parser, ldr_object, points, normalise, geom_type);
		}
	}
	if (points.empty()) return 0;
	point.pop_back();
	ldr_object->CreateRenderObject(points, colours);
	return ldr_object.release();
}

// Extract a list of rectangles from the source
template <typename Parser> LdrObject* ParseRectangle(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 pt[4]; Colour32 col[4]
			parser.ExtractVector3(pt[0], 1.0f);
			parser.ExtractVector3(pt[1], 1.0f);
			parser.ExtractVector3(pt[2], 1.0f);
			parser.ExtractVector3(pt[3], 1.0f);
			points.push_back(pt[0]);
			points.push_back(pt[1]);
			points.push_back(pt[1]);
			points.push_back(pt[2]);
			points.push_back(pt[2]);
			poinst.push_back(pt[3]);
			points.push_back(pt[3]);
			points.push_back(pt[0]);
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, TColour32Cont());
	return ldr_object.release();
}

// Extract a list of rectangles from the source given by lower and upper corners
template <typename Parser> LdrObject* ParseRectangleLU(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 lower, upper;
			parser.ExtractVector3(lower, 1.0f);
			parser.ExtractVector3(upper, 1.0f);
			points.push_back(lower);
			points.push_back(v4::make(upper.x, lower.y, upper.z, 1.0f));
			points.push_back(v4::make(upper.x, lower.y, upper.z, 1.0f));
			points.push_back(upper);
			points.push_back(upper);
			poinst.push_back(v4::make(lower.x, upper.y, lower.z, 1.0f));
			points.push_back(v4::make(lower.x, upper.y, lower.z, 1.0f));
			points.push_back(lower);
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, TColour32Cont());
	return ldr_object.release();
}

// Extract a list of rectangles given by width, height, and z position
template <typename Parser> LdrObject* ParseRectangleWHZ(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 dim;
			parser.ExtractVector3(dim, 1.0f);
			points.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
			points.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
			points.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
			points.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			points.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			points.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
			points.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
			points.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, TColour32Cont());
	return ldr_object.release();
}

// Extract a list of circles.
template <typename Parser> LdrObject* ParseCircleR(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	uint divisions = 50;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			float radius;
			parser.ExtractFloat(radius);
			float da = 2.0f * maths::pi / divisions;
			for( uint t = 0; t != divisions; ++t )
			{
				v4 pt = v4::make(Cos(t * da) * radius, Sin(t * da) * radius, 0.0f, 1.0f);
				if (!points.empty()) { points.push_back(pt); }
				points.push_back(pt);
			}
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			if (keyword == ELdrKW_Divisions) { parser.ExtractInt(divisions, 10); continue; }
			if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
			if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
			throw LdrParseException(0, "Unknown keyword found");
		}
	}
	if (points.empty()) return 0;
	points.push_back(points.front());
	ldr_object->CreateRenderObject(points, TColour32Cont());
	return ldr_object.release();
}

// Extract a list of ellipses.
template <typename Parser> LdrObject* ParseCircleRxRyZ(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	uint divisions = 50;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 radius;
			parser.ExtractVector3(radius, 1.0f);
			float da = 2.0f * maths::pi / divisions;
			for( uint t = 0; t != divisions; ++t )
			{
				v4 pt = v4::make(Cos(t * da) * radius.x, Sin(t * da) * radius.y, radius.z, 1.0f);
				if (!point.empty())	{ points.push_back(pt); }
				points.push_back(pt);
			}
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			if (keyword == ELdrKW_Divisions) { parser.ExtractInt(divisions, 10); continue; }
			if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
			if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
			throw LdrParseException(0, "Unknown keyword found");
		}
	}
	if (points.empty()) return 0;
	points.push_back(points.front());
	ldr_object->CreateRenderObject(points, TColour32Cont());
	return ldr_object.release();
}

// Extract a 3x3 matrix
template <typename Parser> LdrObject* ParseMatrix3x3(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	TColour32Cont colours;
	while (!parser.IsSection())
	{
		if (!parser.IsKeyword())
		{
			v4 x_axis, y_axis, z_axis;
			parser.ExtractVector3(x_axis, 0.0f);
			parser.ExtractVector3(y_axis, 0.0f);
			parser.ExtractVector3(z_axis, 0.0f);
			points.push_back(v4Origin);
			points.push_back(v4Origin + x_axis);
			points.push_back(v4Origin);
			points.push_back(v4Origin + y_axis);
			points.push_back(v4Origin);
			points.push_back(v4Origin + z_axis);
			colours.push_back(Colour32Red);
			colours.push_back(Colour32Red);
			colours.push_back(Colour32Green);
			colours.push_back(Colour32Green);
			colours.push_back(Colour32Blue);
			colours.push_back(Colour32Blue);
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_Transpose:
				if (points.size() >= 6)
				{
					std::size_t i = points.size() - 6;
					m3x3 mat;
					mat.x = points[i+1] - points[i+0];
					mat.y = points[i+3] - points[i+2];
					mat.z = points[i+5] - points[i+4];
					mat.Transpose();
					points[i+1] = v4Origin + mat.x;
					points[i+3] = v4Origin + mat.y;
					points[i+5] = v4Origin + mat.z;
				}break;
			case ELdrKW_Inverse:
				if (points.size() >= 6)
				{
					std::size_t i = points.size() - 6;
					m3x3 mat;
					mat.x = points[i+1] - points[i+0];
					mat.y = points[i+3] - points[i+2];
					mat.z = points[i+5] - points[i+4];
					mat.Inverse();
					points[i+1] = v4Origin + mat.x;
					points[i+3] = v4Origin + mat.y;
					points[i+5] = v4Origin + mat.z;
				}break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, colours);
	return true;
}

// Extract a 4x4 matrix
template <typename Parser> LdrObject* ParseMatrix4x4(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	TColour32Cont colours;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 x_axis, y_axis, z_axis, pos;
			parser.ExtractVector4(x_axis):
			parser.ExtractVector4(y_axis);
			parser.ExtractVector4(z_axis);
			parser.ExtractVector4(pos);
			points.push_back(pos);
			points.push_back(pos + x_axis);
			points.push_back(pos);
			points.push_back(pos + y_axis);
			points.push_back(pos);
			points.push_back(pos + z_axis);
			colours.push_back(Colour32Red);
			colours.push_back(Colour32Red);
			colours.push_back(Colour32Green);
			colours.push_back(Colour32Green);
			colours.push_back(Colour32Blue);
			colours.push_back(Colour32Blue);
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_Transpose:
				if (points.size() >= 6)
				{
					std::size_t i = points.size() - 6;
					m4x4 mat;
					mat.x = points[i+1] - points[i+0];
					mat.y = points[i+3] - points[i+2];
					mat.z = points[i+5] - points[i+4];
					mat.pos = points[i];
					mat.Transpose();
					points[i+0] = mat.pos;
					points[i+1] = mat.pos + mat.x;
					points[i+3] = mat.pos + mat.y;
					points[i+5] = mat.pos + mat.z;
				}break;
			case ELdrKW_Inverse:
				if (points.size() >= 6)
				{
					std::size_t i = points.size() - 6;
					m4x4 mat;
					mat.x = points[i+1] - points[i+0];
					mat.y = points[i+3] - points[i+2];
					mat.z = points[i+5] - points[i+4];
					mat.pos = points[i];
					mat.Inverse();
					points[i+0] = mat.pos;
					points[i+1] = mat.pos + mat.x;
					points[i+3] = mat.pos + mat.y;
					points[i+5] = mat.pos + mat.z;
				}break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, colours);
	return ldr_object.release();
}

// Extract a grid.
template <typename Parser> LdrObject* ParseGridWH(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TLines> ldr_object = NewObject<TLines>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			float width, height;
			uint div_w, div_h;
			parser.ExtractReal(width);
			parser.ExtractReal(height);
			parser.ExtractInt(div_w, 10);
			parser.ExtractInt(div_h, 10);
			for (uint h = 0; h <= div_h; ++h)
			{
				points.push_back(v4::make(0.0f , h * height / float(div_h), 0.0f, 1.0f));
				points.push_back(v4::make(width, h * height / float(div_h), 0.0f, 1.0f));
			}
			for (uint w = 0; w <= div_w; ++w)
			{
				points.push_back(v4::make(w * width / float(div_w), 0.0f  , 0.0f, 1.0f));
				points.push_back(v4::make(w * width / float(div_w), height, 0.0f, 1.0f));
			}
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points, TColour32Cont());
	return ldr_object.release();
}

//******************************************************************************************************
// Extract attributes common to triangles
template <typename Parser> void ParseCommonTriangle(Parser& parser, LdrObject* ldr_object, GeomType& geom_type, std::string& texture)
{
	char kw[MaxKeywordLength];
	ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
	switch (keyword)
	{
	case ELdrKW_Normals:	geom_type |= geometry::EType_Normal; break;
	case ELdrKW_Colours:	geom_type |= geometry::EType_Colour; break;
	case ELdrKW_TexCoords:	geom_type |= geometry::EType_Texture; break;
	case ELdrKW_Texture:	parser.ExtractString(texture); break;
	default:
		if (ParseObjectModifier(parser, keyword, ldr_object)) break;
		if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) break;
		throw LdrParseException(0, "Unknown keyword found");
	}
}

// Extract a list of triangles from the source.
// Syntax:
//	*Triangle name FFFFFFFF
//	{
//		*Normals [optional]
//		*VertColours [optional]
//		*TexCoords [optional]
//		*Texture "skin.jpg"
//		1 1 1  0 1 0  FFFFFF00  0.2 0.4
//		2 2 2  0 1 0  FFFFFF00  0.2 0.4
//		3 3 3  0 1 0  FFFFFF00  0.2 0.4
//	}
template <typename Parser> LdrObject* ParseTriangle(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TTriangles> ldr_object = NewObject<TTriangles>(parser, ldr);

	TVertexCont verts;
	GeomType geom_type = geometry::EType_Vertex;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			Vertex vert[3];
			for (int i = 0; i != sizeof(vert)/sizeof(vert[0]); ++i)
			{
				parser.ExtractVector3(vert[i].m_vertex, 1.0f);
				if (geom_type & geometry::EType_Normal)		parser.ExtractVector3	(vert[i].m_normal, 0.0f);
				if (geom_type & geometry::EType_Colour)		parser.ExtractInt		(vert[i].m_colour, 16);
				if (geom_type & geometry::EType_Texture)	parser.ExtractVector2	(vert[i].m_tex_vertex);
			}
			verts.push_back(vert[0]);
			verts.push_back(vert[1]);
			verts.push_back(vert[2]);
		}
		else
		{
			ParseCommonTriangle(parser, ldr_object, geom_type, texture);
		}
	}
	if (verts.empty()) return 0;
	ldr_object->CreateRenderObject(verts, geom_type);
	return ldr_object.release();
}

// Extract a list of quads.
template <typename Parser> LdrObject* ParseQuad(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TTriangles> ldr_object = NewObject<TTriangles>(parser, ldr);

	TVertexCont verts;
	GeomType geom_type = geometry::EType_Vertex;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			Vertex vert[4];
			for (int i = 0; i != sizeof(vert)/sizeof(vert[0]); ++i)
			{
				parser.ExtractVector3(vert[i].m_vertex, 1.0f);
				if (geom_type & geometry::EType_Normal)		parser.ExtractVector3	(vert[i].m_normal, 0.0f);
				if (geom_type & geometry::EType_Colour)		parser.ExtractInt		(vert[i].m_colour, 16);
				if (geom_type & geometry::EType_Texture)	parser.ExtractVector2	(vert[i].m_tex_vertex);
			}
			verts.push_back(vert[0]);
			verts.push_back(vert[1]);
			verts.push_back(vert[2]);
			verts.push_back(vert[0]);
			verts.push_back(vert[2]);
			verts.push_back(vert[3]);
		}
		else
		{
			ParseCommonTriangle(parser, ldr_object, geom_type, texture);
		}
	}
	if (verts.empty()) return 0;
	ldr_object->CreateRenderObject(verts);
	return ldr_object.release();
}

// Extract a list of quads given by lower and upper corners
template <typename Parser> LdrObject* ParseQuadLU(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TTriangles> ldr_object = NewObject<TTriangles>(parser, ldr);

	TVertexCont verts;
	GeomType geom_type = geometry::EType_Vertex;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			Vertex vert[2];
			for (int i = 0; i != sizeof(vert)/sizeof(vert[0]); ++i)
			{
				parser.ExtractVector3(vert[i].m_vertex, 1.0f);
				if (geom_type & geometry::EType_Normal)		parser.ExtractVector3	(vert[i].m_normal, 0.0f);
				if (geom_type & geometry::EType_Colour)		parser.ExtractInt		(vert[i].m_colour, 16);
				if (geom_type & geometry::EType_Texture)	parser.ExtractVector2	(vert[i].m_tex_vertex);
			}
			verts.push_back(vert[0]);
			verts.push_back(v4::make(vert[0].x, vert[1].y, vert[1].z, 1.0f));
			verts.push_back(vert[1]);
			verts.push_back(v4::make(vert[1].x, vert[0].y, vert[0].z, 1.0f));
		}
		else
		{
			ParseCommonTriangle(parser, ldr_object, geom_type, texture);
		}
	}
	if (verts.empty()) return 0;
	ldr_object->CreateRenderObject(verts);
	return ldr_object.release();
}

// Extract a list of quads given by width, height and z position
template <typename Parser> LdrObject* ParseQuadWHZ(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TTriangles> ldr_object = NewObject<TTriangles>(parser, ldr);

	TVertexCont verts;
	GeomType geom_type = geometry::EType_Vertex;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 dim;
			parser.ExtractVector3(dim, 0.0f);
			verts.push_back(Vertex::make(v4::make(-dim.x, -dim.y,  dim.z, 1.0f), v4ZAxis, Colour32One, v2::make(0.0f, 0.0f)));
			verts.push_back(Vertex::make(v4::make(-dim.x,  dim.y,  dim.z, 1.0f), v4ZAxis, Colour32One, v2::make(0.0f, 1.0f)));
			verts.push_back(Vertex::make(v4::make( dim.x,  dim.y,  dim.z, 1.0f), v4ZAxis, Colour32One, v2::make(1.0f, 1.0f)));
			verts.push_back(Vertex::make(v4::make( dim.x, -dim.y,  dim.z, 1.0f), v4ZAxis, Colour32One, v2::make(1.0f, 0.0f)));
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			if (keyword == ELdrKW_Texture) { parser.ExtractString(texture); continue; }
			if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
			if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
			throw LdrParseException(0, "Unknown keyword found");
		}
	}
	if (verts.empty()) return 0;
	ldr_object->CreateRenderObject(verts);
	return ldr_object.release();
}

//******************************************************************************************************
// Extract a list of boxes given by width, height, and depth.
template <typename Parser> LdrObject* ParseBoxWHD(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TBoxes> ldr_object = NewObject<TBoxes>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 dim;
			parser.ExtractVector3(dim, 0.0f);
			dim /= 2.0f;
			points.push_back(v4::make(-dim.x, -dim.y, -dim.z, 1.0f));
			points.push_back(v4::make(-dim.x,  dim.y, -dim.z, 1.0f));
			points.push_back(v4::make( dim.x, -dim.y, -dim.z, 1.0f));
			points.push_back(v4::make( dim.x,  dim.y, -dim.z, 1.0f));
			points.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
			points.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			points.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
			points.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points);
	return ldr_object.release();
}

// Extract a list of boxes given by lower and upper corners.
template <typename Parser> LdrObject* ParseBoxLU(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TBoxes> ldr_object = NewObject<TBoxes>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 lower, upper;
			parser.ExtractVector3(lower, 1.0f);
			parser.ExtractVector3(upper, 1.0f);
			points.push_back(lower);
			points.push_back(v4::make(lower.x, upper.y, lower.z, 1.0f));
			points.push_back(v4::make(upper.x, lower.y, lower.z, 1.0f));
			points.push_back(v4::make(upper.x, upper.y, lower.z, 1.0f));
			points.push_back(v4::make(upper.x, lower.y, upper.z, 1.0f));
			points.push_back(upper);
			points.push_back(v4::make(lower.x, lower.y, upper.z, 1.0f));
			points.push_back(v4::make(lower.x, upper.y, upper.z, 1.0f));
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points);
	return ldr_object.release();
}

// Extract a list of boxes with predefined size given by positions.
template <typename Parser> LdrObject* ParseBoxList(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TBoxes> ldr_object = NewObject<TBoxes>(parser, ldr);

	TVecCont points;
	float size = 0.01f;
	bool constant_size = false;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 pos;
			if (!contant_size) parser.ExtractReal(size);
			parser.ExtractVector3(pos, 1.0f);
			points.push_back(v4::make(pos.x - size, pos.y - size, pos.z - size, 1.0f));
			points.push_back(v4::make(pos.x - size, pos.y + size, pos.z - size, 1.0f));
			points.push_back(v4::make(pos.x + size, pos.y - size, pos.z - size, 1.0f));
			points.push_back(v4::make(pos.x + size, pos.y + size, pos.z - size, 1.0f));
			points.push_back(v4::make(pos.x + size, pos.y - size, pos.z + size, 1.0f));
			points.push_back(v4::make(pos.x + size, pos.y + size, pos.z + size, 1.0f));
			points.push_back(v4::make(pos.x - size, pos.y - size, pos.z + size, 1.0f));
			points.push_back(v4::make(pos.x - size, pos.y + size, pos.z + size, 1.0f));
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			if (keyword == ELdrKW_Size) { parser.ExtractRead(size); continue; }		
			if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
			if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
			throw LdrParseException(0, "Unknown keyword found");
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points);
	return ldr_object.release();
}

// Extract a frustum given by width, height, near, and far
template <typename Parser> LdrObject* ParseFrustumWHNF(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TBoxes> ldr_object = NewObject<TBoxes>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			float width, height, Near, Far;
			parser.ExtractFloat(width);
			parser.ExtractFloat(height);
			parser.ExtractFloat(Near);
			parser.ExtractFloat(Far);

			width /= 2.0f;
			height /= 2.0f;
			float Width = width * Far / Near;
			float Height = height * Far / Near;

			points.push_back(v4::make(-width, -height, Near, 1.0f));
			points.push_back(v4::make(-width,  height, Near, 1.0f));
			points.push_back(v4::make( width, -height, Near, 1.0f));
			points.push_back(v4::make( width,  height, Near, 1.0f));
			points.push_back(v4::make( Width, -Height, Far,  1.0f));
			points.push_back(v4::make( Width,  Height, Far,  1.0f));
			points.push_back(v4::make(-Width, -Height, Far,  1.0f));
			points.push_back(v4::make(-Width,  Height, Far,  1.0f));
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points);
	return ldr_object.release();
}

// Extract a frustum given by width_angle, height_angle, near, and far
template <typename Parser> LdrObject* ParseFrustumATNF(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TBoxes> ldr_object = NewObject<TBoxes>(parser, ldr);

	TVecCont points;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			float alpha, theta, Near, Far;
			parser.ExtractFloat(alpha);
			parser.ExtractFloat(theta);
			parser.ExtractFloat(Near);
			parser.ExtractFloat(Far);

			float width		= 2.0f * Near * Tan(DegreesToRadians(alpha / 2.0f));
			float height	= 2.0f * Near * Tan(DegreesToRadians(theta / 2.0f));
			float Width		= width  * Far / Near;
			float Height	= height * Far / Near; 

			points.push_back(v4::make(-width, -height, Near, 1.0f));
			points.push_back(v4::make(-width,  height, Near, 1.0f));
			points.push_back(v4::make( width, -height, Near, 1.0f));
			points.push_back(v4::make( width,  height, Near, 1.0f));
			points.push_back(v4::make( Width, -Height, Far,  1.0f));
			points.push_back(v4::make( Width,  Height, Far,  1.0f));
			points.push_back(v4::make(-Width, -Height, Far,  1.0f));
			points.push_back(v4::make(-Width,  Height, Far,  1.0f));
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (points.empty()) return 0;
	ldr_object->CreateRenderObject(points);
	return ldr_object.release();
}

//******************************************************************************************************
// Extract a list of cylinders given by height and radius
template <typename Parser> LdrObject* ParseCylinderHR(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TCylinder> ldr_object = NewObject<TCylinder>(parser, ldr);

	bool cyl_read = false;
	float height = 1.0f, radius = 1.0f;
	uint wedges = 40, layers = 1;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			parser.ExtractReal(height);
			parser.ExtractReal(radius);
			cyl_read = true;
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_Wedges: parser.ExtractInt(wedges, 10); break;
			case ELdrKW_Layers: parser.ExtractInt(layers, 10); break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (!cyl_read) return 0;
	ldr_object->CreateRenderObject(height, radius, radius, wedges, layers);
	return ldr_object.release();
}

// Extract a list of cylinders given as height, x-radius, and z-radius
template <typename Parser> LdrObject* ParseCylinderHRxRz(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TCylinder> ldr_object = NewObject<TBoxes>(parser, ldr);

	bool cyl_read = false;
	float height = 1.0f, radiusX = 1.0f, radiusZ = 1.0f;
	uint wedges = 40, layers = 1;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			parser.ExtractReal(height);
			parser.ExtractReal(radiusX);
			parser.ExtractReal(radiusZ);
			cyl_read = true;
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_Wedges: parser.ExtractInt(wedges, 10); break;
			case ELdrKW_Layers: parser.ExtractInt(layers, 10); break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (!cyl_read) return 0;
	ldr_object->CreateRenderObject(height, radiusX, radiusZ, wedges, layers);
	return ldr_object.release();
}

//******************************************************************************************************
// Extract a sphere given by a single radius
template <typename Parser> LdrObject* ParseSphereR(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TSphere> ldr_object = NewObject<TSphere>(parser, ldr);

	bool sph_read = false;
	float radius = 1.0f;
	uint divisions = 3;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			parser.ExtractReal(radius);
			sph_read = true;
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_Divisions: parser.ExtractInt(divisions, 10); break;
			case ELdrKW_Texture: parser.ExtractString(texture, 10); break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (!sph_read) return 0;
	ldr_object->CreateRenderObject(radius, radius, radius, divisions, texture);
	return ldr_object.release();
}

// Extract a sphere given by x, y, and z radius.
template <typename Parser> LdrObject* ParseSphereRxRyRz(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TSphere> ldr_object = NewObject<TSphere>(parser, ldr);

	bool sph_read = false;
	float radiusX = 1.0f, radiusY = 1.0f, radiusZ = 1.0f;
	uint divisions = 3;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			parser.ExtractReal(radiusX);
			parser.ExtractReal(radiusY);
			parser.ExtractReal(radiusZ);
			sph_read = true;
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_Divisions: parser.ExtractInt(divisions, 10); break;
			case ELdrKW_Texture: parser.ExtractString(texture, 10); break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (!sph_read) return 0;
	ldr_object->CreateRenderObject(radiusX, radiusY, radiusZ, divisions, texture);
	return ldr_object;
}

//******************************************************************************************************
// Extract a mesh.
template <typename Parser> LdrObject* ParseMesh(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TMesh> ldr_object = NewObject<TMesh>(parser, ldr);

	TVertexCont verts;
	TIndexCont indices;
	GeomType geom_type = geometry::EType_Vertex | geometry::EType_Normal;
	bool generate_normals = true;
	bool line_list = false;
	std::string texture;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword()) throw LdrParseException("Mesh descriptions have no non-keyword data");

		char kw[MaxKeywordLength];
		ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
		switch (keyword)
		{
		case ELdrKW_Verts:
			parser.FindSectionStart();
			for (std::size_t i = 0; !parser.IsSectionEnd(); ++i)
			{
				if (i >= verts.size()) verts.push_back(Vertex());
				parser.ExtractVector3(verts[i].m_vertex, 1.0f);
			}
			parser.FindSectionEnd();
			break;

		case ELdrKW_Normals:
			parser.FindSectionStart();
			for (std::size_t i = 0; !parser.IsSectionEnd(); ++i)
			{
				if (i >= verts.size()) verts.push_back(Vertex());
				parser.ExtractVector3(verts[i].m_normal, 0.0f);
			}
			parser.FindSectionEnd();
			generate_normals = false;
			break;

		case ELdrKW_Lines:
			parser.FindSectionStart();
			indices.resize(0);
			while (!parser.IsSectionEnd())
			{
				uint i0, i1;
				parser.ExtractInt(i0, 10);
				parser.ExtractInt(i1, 10);
				indices.push_back(value_cast<Index>(i0));
				indices.push_back(value_cast<Index>(i1));
			}
			parser.FindSectionEnd();
			line_list = true;
			break;

		case ELdrKW_Faces:
			parser.FindSectionStart();
			indices.resize(0);
			while (!parser.IsSectionEnd())
			{
				uint i0, i1, i2;
				parser.ExtractInt(i0, 10);
				parser.ExtractInt(i1, 10);
				parser.ExtractInt(i2, 10);
				indices.push_back(value_cast<Index>(i0)); // should really be an if() throw...
				indices.push_back(value_cast<Index>(i1));
				indices.push_back(value_cast<Index>(i2));
			}
			parser.FindSectionEnd();
			line_list = false;
			break;

		case ELdrKW_Tetra:
			parser.FindSectionStart();
			indices.resize(0);
			while (!parser.IsSectionEnd())
			{
				uint i0, i1, i2, i3;
				parser.ExtractInt(i0, 10);
				parser.ExtractInt(i1, 10);
				parser.ExtractInt(i2, 10);
				parser.ExtractInt(i3, 10);
				indices.push_back(value_cast<Index>(i0));
				indices.push_back(value_cast<Index>(i1));
				indices.push_back(value_cast<Index>(i2));
				indices.push_back(value_cast<Index>(i0));
				indices.push_back(value_cast<Index>(i2));
				indices.push_back(value_cast<Index>(i3));
				indices.push_back(value_cast<Index>(i0));
				indices.push_back(value_cast<Index>(i3));
				indices.push_back(value_cast<Index>(i1));
				indices.push_back(value_cast<Index>(i3));
				indices.push_back(value_cast<Index>(i2));
				indices.push_back(value_cast<Index>(i1));
			}
			parser.FindSectionEnd();
			line_list = false;
			break;

		case ELdrKW_GenerateNormals:
			generate_normals = true;
			break;

		default:
			if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
			if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
			throw LdrParseException(0, "Unknown keyword found");
		}
	}
	if (verts.empty() || indices.empty()) return 0;
	ldr_object->CreateRenderObject(verts, indices, geom_type, generate_normals, line_list);
	return ldr_object.release();
}

// Extract a cloud of points and interpret them as a polytope
template <typename Parser> LdrObject* ParsePolytope(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TMesh> ldr_object = NewObject<TMesh>(parser, ldr);

	TVecCont verts;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			v4 pt;
			parser.ExtractVector3(pt, 1.0f);
			verts.push_back(pt);
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (verts.empty()) return 0;

	// Find the convex hull
	std::size_t num_verts, num_faces;
	TIndexCont faces(6 * (verts.size() - 2));
	ConvexHull(verts, verts.size(), &faces[0], &faces[0] + faces.size(), num_verts, num_faces);
	verts.resize(num_verts);
	faces.resize(num_faces*3);

	// Create the verts
	TVertexCont poly(num_verts);
	TVertexCont::iterator poly_vert = poly.begin();
	for (TVecCont::const_iterator v = verts.begin(), v_end = verts.end(); v != v_end; ++v, ++poly_vert)
		poly_vert->set(*v, v4Zero, Colour32One, v2Zero);

	ldr_object->CreateRenderObject(poly, faces, geometry::EType_Vertex | geometry::EType_Normal, true, false);
	return ldr_object.release();
}

// Extract a surface given as a width, height and width * height points
template <typename Parser> LdrObject* ParseSurfaceWHD(Parser& parser, LineDrawer& ldr)
{
	std::auto_ptr<TMesh> ldr_object = NewObject<TMesh>(parser, ldr);

	uint width = 0, height = 0;
	TVertexCont verts;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyword())
		{
			parser.ExtractInt(width, 10);
			parser.ExtractInt(height, 10);
			verts.reserve(width * height);
			for (uint h = 0; h != height; ++h)
			{
				for (uint w = 0; w != width; ++w)
				{
					v4 pt;
					parser.ExtractVector3(pt, 1.0f);
					verts.push_back(Vertex::make(pt, v4Zero, Colour32White, v2::make(w / float(width), h / float(height))));
				}
			}
		}
		else
		{
			ParseStandardChildren(parser, ldr, ldr_object);
		}
	}
	if (verts.empty()) return 0;

    uint num_faces		= 2 * (width - 1) * (height - 1);
	uint num_indices	= num_faces * 3;
	uint num_vertices	= uint(verts.size());

	// Generate the faces
	TIndexCont faces(num_indices);
	TIndexCont::iterator ib = faces.begin();
	for (uint h = 0; h != height - 1; ++h)
	{
		Index row = width * h;
		for (uint w = 0; w != width - 1; ++w)
		{
			Index col = row + w;
			*ib++ = col;
			*ib++ = col + width;
			*ib++ = col + 1 + width;
			*ib++ = col;
			*ib++ = col + 1 + width;
			*ib++ = col + 1;
		}
	}

	ldr_object->CreateRenderObject(verts, faces, geometry::EType_Vertex | geometry::EType_Normal, true, false);
	return ldr_object.release();
}

// Load a geometry file from disc
template <typename Parser> LdrObject* ParseFile(Parser& parser, LineDrawer &ldr)
{
	std::auto_ptr<TMesh> ldr_object = NewObject<TMesh>(parser, ldr);

	Geometry geometry;
	bool generate_normals = false;
	bool optimise_mesh = false;
	uint frame_index = 0;
	while (!parser.IsSectionEnd())
	{
		if (!parser.IsKeyWord())
		{
			std::string filename;
			parser.ExtractString(filename);

			// Load supported file types
			std::string extn = file_sys::GetExtension(filename);
			if (str::EqualNoCase(extn, "x"))
			{
				if (Failed(xfile::Load(filename.c_str(), geometry)))
					throw LdrParseException(FmtS("Failed to load X file '%s'", filename.c_str()));
			}
			else
			{
				throw LdrParseException(FmtS("Unsupported geometry file format '%s'", filename.c_str()));
			}
		}
		else
		{
			char kw[MaxKeywordLength];
			ELdrKW keyword = ParseKeyword(parser.GetKeyword(kw, MaxKeywordLength));
			switch (keyword)
			{
			case ELdrKW_GenerateNormals:
				generate_normals = true;
				break;
			case ELdrKW_Optimise:
				optimise_mesh = true;
				break;
			case ELdrKW_Frame:
				parser.ExtractInt(frame_index, 10);
				break;
			default:
				if (ParseObjectModifier(parser, keyword, ldr_object)) continue;
				if (ParseObject(parser, keyword, ldr, ldr_object->m_child)) continue;
				throw LdrParseException(0, "Unknown keyword found");
			}
		}
	}
	if (geometry.m_frame.empty()) return 0;

	// Get the frame to be viewed
	if (frame_index >= geometry.m_frame.size()) throw LdrParseException("Specified frame does not exist");
	geometry::Mesh& mesh = geometry.m_frame[frame_index];
	PR_ASSERT_STR(PR_DBG_LDR, geometry::IsValid(mesh.m_geometry_type), "Invalid geometry type");
	
	// If the first normal is zero then generate normals for the mesh
	if (generate_normals || mesh.m_vertex[0].IsZero3()) geometry::GenerateNormals(mesh);
	if (optimise) geometry::OptimiseMesh(mesh);

	ldr_object->CreateRenderObject(mesh);
	return ldr_object.release();
}

//******************************************************************************************************
// Recursive parsing function
template <typename Parser> bool ParseCommon(Parser& parser, LineDrawer& ldr, ParseResult& result)
{
	// Record the parse start time
	uint parse_start_time = uint(GetTickCount());
	try
	{
		while (parser.IsKeyword())
		{
			//// Update the progress bar
			//if( !ldr.SetProgress((uint)m_loader.GetPosition(), (uint)m_loader.GetDataLength(), Fmt("Parsing object: %s", keyword.c_str()).c_str(), (uint)(GetTickCount() - m_parse_start_time)) )
			//{	return false; }

			char kw[MaxKeywordLength];
			parser.GetKeyword(kw, MaxKeywordLength);
			ELdrKW keyword = ParseKeyword(kw);
			if (ParseObject(parser, keyword, ldr, result.m_objects))
				continue;
			
			// Parse top level script items
			switch (keyword)
			{
			default: throw LdrParseException("Unknown top level keyword");
			case ELdrKW_Camera:
				result.m_view.SetAspect(ldr.GetClientArea());
				ParseCamera(parser, result.m_view_mask, result.m_view);
				break;
			case ELdrKW_Lock:
				ParseLocks(parser, result.m_lock_mask);
				break;
			case ELdrKW_Delimiters:
				{
					std::string delim;
					parser.ExtractCString(delim);
					parser.SetDelimiters(delim.c_str());
				}break;
			case ELdrKW_GlobalWireframe:
				{
					parser.FindSectionStart();
					parser.ExtractInt(result.m_global_wireframe_mode, 10);
					parser.FindSectionEnd();
					if (result.m_global_wireframe_mode < -1 || result.m_global_wireframe_mode > 2) throw LdrParseException("Invalid global wireframe mode");
				}break;
			}
		}
	}
	catch (LdrParseException)
	{
		ldr.SetProgress(0, 0, "");
		return false;
	}
	ldr.SetProgress(0, 0, "");
	return true;
}
//******************************************************************************************************
// String parsing
// Failure policy for the str script parser
struct StrFailPolicy
{
	// Called when a requested token is not found in the source.
	// Expected behaviour is to report an error and throw an exception or return false;
	static bool NotFound(pr::script::EToken token, char const* iter)
	{
		throw LdrParseException(Fmt("Missing '%s' token.\nNear \"%20s\"", ToString(token), iter));
	}

	// Called when an error is found in the source.
	// Expected behaviour is to report an error and throw an exception or return false;
	static bool Error(pr::script::EResult result, char const* iter)
	{
		throw LdrParseException(Fmt("Error of type '%s' found in script.\nNear \"%20s\"", ToString(result), iter));
	}
};

// String based parser
typedef pr::impl::ScriptParser<char const*, StrFailPolicy, pr::script::StubIncludeHandler> LdrSScriptParser;

// Converts a source string into a ParseResult.
// 'src' must be a null terminated string
// Returns true if the complete source was parsed.
bool ParseSource(LineDrawer& ldr, char const* src, ParseResult& result)
{
	LdrSScriptParser parser;
	parser.SetSource(src);
	return ParseCommon(parser, ldr, result);
}

//******************************************************************************************************
// File parsing
//struct StrIncludeHandler
//{
//	std::vector<char const*> m_stack;
//
//	// Handles a request to load an include.
//	// Doing nothing causes the include to be ignored.
//	// Expected behaviour is for the include handler to
//	// push the current iterator onto a stack, then set
//	// it to the beginning of the included data.
//	void Push(char const* include_string, char const*& iter)
//	{
//		m_stack.push_back(iter);
//		int index; str::ExtractInt(index, 10, include_string);
//		iter = g_script_string[index];
//	}
//	
//	// Called when the source iterator reaches a 0.
//	// Returning false indicates the end of the source data
//	// Expected behaviour is for the include handler to set
//	// the provided iterator to the value popped off the top
//	// of a stack.
//	bool Pop(char const*& iter)
//	{
//		if( !m_stack.empty() )
//		{
//			iter = m_stack.back();
//			m_stack.resize(m_stack.size() - 1);
//			return true;
//		}
//		return false;
//	}
//};

// Converts the contents of a collection of files into a ParseResult
// Returns true if the complete source was parsed.
bool ParseSource(LineDrawer& ldr, FileLoader& file_loader, ParseResult& result)
{
	ldr;file_loader;result;
//// Parse data contained in the file loader
//bool StringParser::Parse(FileLoader& file_loader)
//{
//	file_loader.ClearWatchFiles();
//
//	std::string data;
//	for( TLdrFileVec::iterator f = file_loader.m_file.begin(), f_end = file_loader.m_file.end(); f != f_end; ++f )
//	{
//		LdrFile& file = *f;
//		SetWindowText(m_linedrawer->m_window_handle, Fmt("LineDrawer - Parsing file: \"%s\" ... Please wait", file.m_name.c_str()).c_str());
//
//		// This file needs watching
//		file_loader.AddFileToWatch(file.m_name.c_str());
//
//		// Get the file data
//		data.resize(0);
//		if( !file.GetData(data) )
//		{
//			m_linedrawer->m_error_output.Error(Fmt("FileLoader: Failed to load %s", file.m_name.c_str()).c_str());
//			//m_linedrawer->RemoveRecentFile(file.m_name.c_str());
//			continue;
//		}
//
//		// Add the file's path to the include paths in the loader
//		m_loader.AddIncludePath(file_sys::GetDirectory(file.m_name));
//
//		// Parse it
//		Parse(data.c_str(), data.size());
//
//		// Clear the include paths
//		m_loader.ClearIncludePaths();
//
//		// Add any includes files for watching as well
//		for( pr::script::TPaths::const_iterator i = m_loader.GetIncludedFiles().begin(), i_end = m_loader.GetIncludedFiles().end(); i != i_end; ++i )
//		{
//			file_loader.AddFileToWatch(i->c_str());
//		}
//	}
//
//	file_loader.m_refresh_pending = false;
//	return true;
//}
}

//******************************************************************************************************
ParseResult::ParseResult()
:m_objects()
,m_global_wireframe_mode(EGlobalWireframeMode_NotSet)
,m_lock_mask()
,m_view_mask()
,m_view()
{}

ParseResult::~ParseResult()
{
	for (TLdrObjectPtrVec::iterator i = m_objects.begin(), i_end = m_objects.end(); i != i_end; ++i)
		delete *i;
}

#if PR_DBG_LDR
struct HashCodeCheck
{
	HashCodeCheck()
	{
#		define LDR_KEYWORD(identifier, hash)	PR_ASSERT_STR(PR_DBG_LDR, str::Hash(#identifier) == hash, FmtS("Identifier %s has an incorrect hash code. Correct code: %x", #identifier, str::Hash(#identifier)));
#		define LDR_OBJECT(identifier, hash)		PR_ASSERT_STR(PR_DBG_LDR, str::Hash(#identifier) == hash, FmtS("Identifier %s has an incorrect hash code. Correct code: %x", #identifier, str::Hash(#identifier)));
#		include "LineDrawer/Objects/LdrObjects.inc"
	}
} g_hash_code_check;
#endif

#endif//USE_NEW_PARSER
