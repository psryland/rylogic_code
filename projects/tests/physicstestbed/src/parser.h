//******************************************
// Parser
//******************************************
#pragma once
#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "pr/common/PRScript.h"
//#include "pr/common/ScriptParser.h" // use this one day

//namespace parse
//{
//	// A fail policy for the script parser
//	struct PhysicsScriptFailPolicy
//	{
//		// Called when a requested token is not found in the source.
//		// Expected behaviour is to report an error and throw an exception or return false;
//		template <typename Iter> static bool NotFound(pr::script::EToken /*token*/, Iter /*iter*/) { return false; }
//
//		// Called when an error is found in the source.
//		// Expected behaviour is to report an error and throw an exception or return false;
//		template <typename Iter> static bool Error(pr::script::EResult /*result*/, Iter /*iter*/)	{ return false; }
//	};
//
//	// Include handler for the script parser
//	struct PhysicsScriptIncludeHandler
//	{
//		// Handles a request to load an include.
//		// Doing nothing causes the include to be ignored.
//		// Expected behaviour is for the include handler to
//		// push the current iterator onto a stack, then set
//		// it to the beginning of the included data.
//		template <typename Iter> static void Push(char const* /*include_string*/, Iter& /*iter*/) {}
//		
//		// Called when the source iterator reaches a 0.
//		// Returning false indicates the end of the source data
//		// Expected behaviour is for the include handler to set
//		// the provided iterator to the value popped off the top
//		// of a stack.
//		template <typename Iter> static bool Pop(Iter& /*iter*/) { return false; }
//	};
//
//	typedef pr::impl::ScriptParser
//	<
//		char const*,
//		PhysicsScriptFailPolicy,
//		PhysicsScriptIncludeHandler
//	> ScriptParser;
//}

class Parser
{
public:
	parse::Output m_output;

	bool Load					(const char* filename);
	bool Load					(const char* src, std::size_t len);
	bool Load					(pr::ScriptLoader& loader);
	parse::EObjectType Parse	(pr::ScriptLoader& loader, std::string const& keyword);
	parse::EObjectType Parse	(pr::ScriptLoader& loader);
	void ParseV4				(pr::ScriptLoader& loader, float w);
	void ParseRandomV4			(pr::ScriptLoader& loader, float w);
	void ParseRandomDirection	(pr::ScriptLoader& loader);
	void ParseTransform			(pr::ScriptLoader& loader);
	void ParseRandomTransform	(pr::ScriptLoader& loader);
	void ParseEulerPos			(pr::ScriptLoader& loader);
	void ParseColour			(pr::ScriptLoader& loader);
	void ParseRandomColour		(pr::ScriptLoader& loader);
	void ParseGfx				(pr::ScriptLoader& loader);
	void ParseTerrain			(pr::ScriptLoader& loader);
	void ParseMaterial			(pr::ScriptLoader& loader);
	void ParseGravityField		(pr::ScriptLoader& loader);
	void ParseDrag				(pr::ScriptLoader& loader);
	void ParseModel				(pr::ScriptLoader& loader);
	void ParseModelByName		(pr::ScriptLoader& loader);
	bool ParsePrimCommon		(pr::ScriptLoader& loader, std::string const& keyword);
	void ParseBox				(pr::ScriptLoader& loader);
	void ParseCylinder			(pr::ScriptLoader& loader);
	void ParseSphere			(pr::ScriptLoader& loader);
	void ParsePolytope			(pr::ScriptLoader& loader);
	void ParseTriangle			(pr::ScriptLoader& loader);
	void ParseSkeleton			(pr::ScriptLoader& loader, parse::Skeleton& skel);
	void ParseDeformable		(pr::ScriptLoader& loader);
	void ParseDeformableByName	(pr::ScriptLoader& loader);
	void ParseStaticObject		(pr::ScriptLoader& loader);
	void ParsePhysicsObject		(pr::ScriptLoader& loader);
	void ParsePhysObjByName		(pr::ScriptLoader& loader);
	void ParseMultibody			(pr::ScriptLoader& loader, parse::Multibody* parent = 0);

private:
	// Return value for:
	//	EObjectType_Mass
	float m_value;
	
	// Return value for:
	//	EObjectType_Model
	//	EObjectType_ModelByName
	//	EObjectType_StaticObject
	//	EObjectType_PhysicsObject
	//	EObjectType_PhysObjByName
	//	EObjectType_Deformable
	//	EObjectType_DeformableByName
	//	EObjectType_Multibody
	std::size_t m_index;

	// Return value for:
	//	EObjectType_Position
	//	EObjectType_Direction
	//	EObjectType_Velocity
	//	EObjectType_AngVelocity
	//	EObjectType_Gravity
	pr::v4 m_vec;

	// Return value for:
	//	EObjectType_Transform
	pr::m4x4 m_mat;

	// Return value for:
	//	EObjectType_Colour
	pr::Colour32 m_colour;

	// Return value for:
	//	EObjectType_Name
	std::string m_str;

	// Return value for:
	parse::Prim m_prim;
};

//namespace parse
//{
//	std::string GenerateLdrString(parse::PhysObj const& phys, std::string const& model_str);
//	std::string GenerateLdrString(parse::Model const& model);
//	std::string GenerateLdrString(parse::Prim const& prim, int prim_number);
//}//namespace parse

