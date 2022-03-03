//********************************************
// Testbed State
//********************************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/TestbedState.h"
#include "PhysicsTestbed/ShapeGenParams.h"
#include "pr/common/PRScript.h"
#include "pr/filesys/File.h"

TestbedState::TestbedState(bool load)
:m_state_filename("PhysicsTestbedState.pr_script")
,m_log_filename("C:/DeleteMe/PhysicsTestbedLog.txt")
,m_show_velocity(false)
,m_show_ang_velocity(false)
,m_show_ang_momentum(false)
,m_show_ws_bounding_boxes(false)
,m_show_os_bounding_boxes(false)
,m_show_centre_of_mass(false)
,m_show_contact_points(false)
,m_show_inertia(false)
,m_show_collision_impulses(false)
,m_show_terrain_sampler(false)
,m_stop_at_frame(false)
,m_stop_at_frame_number(0)
,m_step_size_inv(120)
,m_step_rate(120)
,m_scale(50)
{
	if( load ) Load();
}

TestbedState::~TestbedState()
{
	Save();
}

void TestbedState::Save()
{
	pr::ScriptSaver saver;
	saver.WriteKeyword("ShowVelocity");				saver.WriteBool(m_show_velocity);			saver.Newline();
	saver.WriteKeyword("ShowAngVelocity");			saver.WriteBool(m_show_ang_velocity);		saver.Newline();
	saver.WriteKeyword("ShowAngMomentum");			saver.WriteBool(m_show_ang_momentum);		saver.Newline();
	saver.WriteKeyword("ShowWSBoundingBoxes");		saver.WriteBool(m_show_ws_bounding_boxes);	saver.Newline();
	saver.WriteKeyword("ShowOSBoundingBoxes");		saver.WriteBool(m_show_os_bounding_boxes);	saver.Newline();
	saver.WriteKeyword("ShowCentreOfMass");			saver.WriteBool(m_show_centre_of_mass);		saver.Newline();
	saver.WriteKeyword("ShowSleeping");				saver.WriteBool(m_show_sleeping);			saver.Newline();
	saver.WriteKeyword("ShowContactPoints");		saver.WriteBool(m_show_contact_points);		saver.Newline();
	saver.WriteKeyword("ShowInertia");				saver.WriteBool(m_show_inertia);			saver.Newline();
	saver.WriteKeyword("ShowRestingContacts");		saver.WriteBool(m_show_resting_contacts);	saver.Newline();
	saver.WriteKeyword("ShowCollisionImpulses");	saver.WriteBool(m_show_collision_impulses);	saver.Newline();
	saver.WriteKeyword("ShowTerrainSampler");		saver.WriteBool(m_show_terrain_sampler);	saver.Newline();

	saver.WriteKeyword("StopAtFrame");				saver.WriteBool(m_stop_at_frame);			saver.Newline();
	saver.WriteKeyword("StopAtFrameNumber");		saver.WriteUInt(m_stop_at_frame_number, 10);saver.Newline();
	saver.WriteKeyword("StepSizeInv");				saver.WriteInt(m_step_size_inv);			saver.Newline();
	saver.WriteKeyword("StepRate");					saver.WriteInt(m_step_rate);			saver.Newline();
	saver.WriteKeyword("Scale");					saver.WriteInt(m_scale);					saver.Newline();

	saver.WriteKeyword("ShapeGen_SphRadiusMin");	saver.WriteFloat(ShapeGen().m_sph_min_radius);	saver.Newline();
	saver.WriteKeyword("ShapeGen_SphRadiusMax");	saver.WriteFloat(ShapeGen().m_sph_max_radius);	saver.Newline();
	saver.WriteKeyword("ShapeGen_CylRadiusMin");	saver.WriteFloat(ShapeGen().m_cyl_min_radius);	saver.Newline();
	saver.WriteKeyword("ShapeGen_CylRadiusMax");	saver.WriteFloat(ShapeGen().m_cyl_max_radius);	saver.Newline();
	saver.WriteKeyword("ShapeGen_CylHeightMin");	saver.WriteFloat(ShapeGen().m_cyl_min_height);	saver.Newline();
	saver.WriteKeyword("ShapeGen_CylHeightMax");	saver.WriteFloat(ShapeGen().m_cyl_max_height);	saver.Newline();
	saver.WriteKeyword("ShapeGen_BoxDimMin");		saver.WriteVector3(ShapeGen().m_box_min_dim);	saver.Newline();
	saver.WriteKeyword("ShapeGen_BoxDimMax");		saver.WriteVector3(ShapeGen().m_box_max_dim);	saver.Newline();
	saver.WriteKeyword("ShapeGen_PolyVCount");		saver.WriteInt    (ShapeGen().m_ply_vert_count);saver.Newline();
	saver.WriteKeyword("ShapeGen_PolyMinDim");		saver.WriteVector3(ShapeGen().m_ply_min_dim);	saver.Newline();
	saver.WriteKeyword("ShapeGen_PolyMaxDim");		saver.WriteVector3(ShapeGen().m_ply_max_dim);	saver.Newline();

	saver.Save(m_state_filename.c_str());
}

void TestbedState::Load()
{
	try
	{
		pr::ScriptLoader loader;
		if( loader.LoadFromFile(m_state_filename.c_str()) != pr::script::EResult_Success ) return;

		std::string keyword;
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_velocity);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_ang_velocity);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_ang_momentum);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_ws_bounding_boxes);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_os_bounding_boxes);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_centre_of_mass);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_sleeping);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_contact_points);
		loader.GetKeyword(keyword);	loader.ExtractBool(m_show_inertia);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_resting_contacts);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_collision_impulses);
		loader.GetKeyword(keyword); loader.ExtractBool(m_show_terrain_sampler);

		loader.GetKeyword(keyword); loader.ExtractBool(m_stop_at_frame);
		loader.GetKeyword(keyword); loader.ExtractUInt(m_stop_at_frame_number, 10);
		loader.GetKeyword(keyword);	loader.ExtractInt(m_step_size_inv, 10);
		loader.GetKeyword(keyword); loader.ExtractInt(m_step_rate, 10);
		loader.GetKeyword(keyword); loader.ExtractInt(m_scale, 10);

		loader.GetKeyword(keyword); loader.ExtractFloat(ShapeGen().m_sph_min_radius);
		loader.GetKeyword(keyword); loader.ExtractFloat(ShapeGen().m_sph_max_radius);
		loader.GetKeyword(keyword); loader.ExtractFloat(ShapeGen().m_cyl_min_radius);
		loader.GetKeyword(keyword); loader.ExtractFloat(ShapeGen().m_cyl_max_radius);
		loader.GetKeyword(keyword); loader.ExtractFloat(ShapeGen().m_cyl_min_height);
		loader.GetKeyword(keyword); loader.ExtractFloat(ShapeGen().m_cyl_max_height);
		loader.GetKeyword(keyword); loader.ExtractVector3(ShapeGen().m_box_min_dim, 0.0f);
		loader.GetKeyword(keyword); loader.ExtractVector3(ShapeGen().m_box_max_dim, 0.0f);
		loader.GetKeyword(keyword); loader.ExtractInt    (ShapeGen().m_ply_vert_count, 10);
		loader.GetKeyword(keyword); loader.ExtractVector3(ShapeGen().m_ply_min_dim, 0.0f);
		loader.GetKeyword(keyword); loader.ExtractVector3(ShapeGen().m_ply_max_dim, 0.0f);
	}
	catch( const pr::script::Exception& )
	{
		*this = TestbedState(false);
	}
}

void TestbedState::AddToLog(const char* str)
{
	str;
	//pr::File log(m_log_filename.c_str(), "a+");
	//if( !log.IsOpen() ) return;

	//std::string log_str = str;
	//log.Write(log_str.c_str(), log_str.size());
}

