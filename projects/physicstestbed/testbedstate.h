//********************************************
// Testbed State
//********************************************
#pragma once

struct TestbedState
{
	explicit TestbedState(bool load = true);
	~TestbedState();
	void Save();
	void Load();
	void AddToLog(const char* str);
	void AddToLog(const std::string& str) { AddToLog(str.c_str()); }

	std::string m_state_filename;
	std::string	m_log_filename;
	
	// Show
	bool m_show_velocity;
	bool m_show_ang_velocity;
	bool m_show_ang_momentum;
	bool m_show_ws_bounding_boxes;
	bool m_show_os_bounding_boxes;
	bool m_show_centre_of_mass;
	bool m_show_sleeping;
	bool m_show_contact_points;
	bool m_show_inertia;
	bool m_show_resting_contacts;
	bool m_show_collision_impulses;
	bool m_show_terrain_sampler;

	// Simulation
	bool			m_stop_at_frame;
	unsigned int	m_stop_at_frame_number;
	int				m_step_size_inv;
	int				m_step_rate;
	int				m_scale;
};

