//**************************************************
// Profile Viewer
//  Copyright © Rylogic Ltd 2007
//**************************************************

#ifndef PR_PROFILE_VIEWER_PROFILE_DATABASE_H
#define PR_PROFILE_VIEWER_PROFILE_DATABASE_H

#include <vector>
#include "pr/common/vectormap.h"
#include "pr/threads/critical_section.h"

typedef pr::vec_map<profile::ID, Caller> TCallers;

struct ProfileData
{
	profile::ID		m_id;
	char			m_name[profile::NameSize];
	float			m_call_count;			// Average number of calls per frame
	float			m_incl_time_ms;
	float			m_excl_time_ms;
	TCallers		m_callers;				// All the other profiles that have called into this one
};
typedef std::vector<ProfileData> TProfileDB;
typedef std::vector<std::size_t> TOrder;

enum ESortBy
{
	ESortBy_ByName,
	ESortBy_CallCount,
	ESortBy_InclTime,
	ESortBy_ExclTime,
};

enum EUnits
{
	EUnits_ms,
	EUnits_pc,
	EUnits_pc_of_60hz_frame
};

struct ProfileDatabase
{
	TProfileDB			m_data;				// Storage for the profile data
	TOrder				m_order;			// A buffer of profile::ID's indicating the order to display elements in
	unsigned long		m_frame_number;		// The frame number of the application since start up (not the update number)
	unsigned long		m_frames;			// The number of frames included in the most recent update
	float				m_frame_time_ms;	// The average length of a frame in milliseconds
	ESortBy				m_sort_by;
	bool				m_sort_needed;		// Lazy sort
	EUnits				m_units;			// Units to display in
	pr::threads::CritSection m_output_cs;
	short				m_output_start_y;	// The Y coordinate to start printing output at

	ProfileDatabase();

	// Update the database with profile data received from the pipe
	void Update(void const* data, std::size_t data_size, bool);

	// Display a print out of the database
	void OutputLine(ProfileData const& data);
	void Output();
	void Output(short y)	{ m_output_start_y = y; Output(); }
};

#endif//PR_PROFILE_VIEWER_PROFILE_DATABASE_H
