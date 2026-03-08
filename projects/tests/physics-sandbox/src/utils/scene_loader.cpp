#include "src/forward.h"
#include "src/utils/scene_loader.h"

namespace physics_sandbox::scene_loader
{
	// Read a 3-element JSON array as a position vector (w=1) or direction vector (w=0).
	v4 ReadVec3(pr::json::Value const& arr, float w)
	{
		auto const& a = arr.to_array();
		if (a.size() < 3)
			throw std::runtime_error("Expected a 3-element array for vector");

		return v4{
			a[0].to<float>(),
			a[1].to<float>(),
			a[2].to<float>(),
			w
		};
	}

	// Parse a single body definition from a JSON object
	BodyDesc ReadBody(pr::json::Value const& body_json)
	{
		auto const& obj = body_json.to_object();
		auto desc = BodyDesc{};

		// Name (required)
		desc.name = obj["name"].to<std::string>();

		// Shape (required)
		auto const& shape_obj = obj["shape"].to_object();
		auto shape_type = shape_obj["type"].to<std::string>();
		if (shape_type == "box")
		{
			desc.shape_type = BodyDesc::EShape::Box;
			desc.box_dimensions = ReadVec3(shape_obj["dimensions"], 0);
		}
		else if (shape_type == "sphere")
		{
			desc.shape_type = BodyDesc::EShape::Sphere;
			desc.sphere_radius = shape_obj["radius"].to<float>();
		}
		else if (shape_type == "line")
		{
			desc.shape_type = BodyDesc::EShape::Line;
			desc.line_length = shape_obj["length"].to<float>();
			if (auto* t = shape_obj.find("thickness"))
				desc.line_thickness = t->to<float>();
			else
				desc.line_thickness = 0.0f;
		}
		else if (shape_type == "triangle")
		{
			desc.shape_type = BodyDesc::EShape::Triangle;
			auto const& verts = shape_obj["vertices"].to_array();
			if (verts.size() < 3)
				throw std::runtime_error("Triangle shape requires 3 vertices");
			desc.tri_verts[0] = ReadVec3(verts[0], 0);
			desc.tri_verts[1] = ReadVec3(verts[1], 0);
			desc.tri_verts[2] = ReadVec3(verts[2], 0);
		}
		else if (shape_type == "polytope")
		{
			desc.shape_type = BodyDesc::EShape::Polytope;
			auto const& verts = shape_obj["vertices"].to_array();
			if (verts.size() < 4)
				throw std::runtime_error("Polytope shape requires at least 4 non-coplanar vertices");
			for (auto const& v : verts)
				desc.polytope_verts.push_back(ReadVec3(v, 1.0f));
		}
		else
		{
			throw std::runtime_error(pr::FmtS("Unknown shape type: '%s'", shape_type.c_str()));
		}

		// Mass (required)
		desc.mass = obj["mass"].to<float>();

		// Position (required)
		desc.position = ReadVec3(obj["position"], 1.0f);

		// Velocity (optional, defaults to zero)
		if (auto* vel = obj.find("velocity"))
			desc.velocity = ReadVec3(*vel, 0.0f);
		else
			desc.velocity = v4::Zero();

		// Angular velocity (optional, defaults to zero)
		if (auto* avel = obj.find("angular_velocity"))
			desc.angular_velocity = ReadVec3(*avel, 0.0f);
		else
			desc.angular_velocity = v4::Zero();

		return desc;
	}

	// Parse a scene description from a JSON file
	SceneDesc LoadFromFile(std::filesystem::path const& filepath)
	{
		auto doc = pr::json::Read(filepath);
		auto const& root = doc.to_object();

		// The top-level object should contain a "scene" key
		auto const& scene_obj = root["scene"].to_object();

		auto desc = SceneDesc{};

		// Description (optional)
		if (auto* d = scene_obj.find("description"))
			desc.description = d->to<std::string>();

		// Gravity (optional, defaults to zero — no gravity)
		if (auto* g = scene_obj.find("gravity"))
			desc.gravity = ReadVec3(*g, 0.0f);
		else
			desc.gravity = v4::Zero();

		// Material properties (optional, defaults to perfectly elastic + frictionless)
		desc.elasticity = 1.0f;
		desc.friction = 0.0f;
		if (auto* mat = scene_obj.find("material"))
		{
			auto const& mat_obj = mat->to_object();
			if (auto* e = mat_obj.find("elasticity"))
				desc.elasticity = e->to<float>();
			if (auto* f = mat_obj.find("friction"))
				desc.friction = f->to<float>();
		}

		// Ground plane (optional)
		desc.has_ground = false;
		if (auto* gp = scene_obj.find("ground_plane"))
		{
			desc.has_ground = true;
			auto const& gp_obj = gp->to_object();
			if (auto* h = gp_obj.find("height"))
				desc.ground.height = h->to<float>();
			if (auto* t = gp_obj.find("texture"))
				desc.ground.texture = t->to<std::string>();
		}

		// Bodies (required, must be an array)
		auto const& bodies_arr = scene_obj["bodies"].to_array();
		for (auto const& body_val : bodies_arr)
			desc.bodies.push_back(ReadBody(body_val));

		return desc;
	}
}
