//******************************************
// Prop
//******************************************
#pragma once

#include "pr/filesys/fileex.h"
#include "pr/linedrawer/plugininterface.h"
#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/CollisionModel.h"
#include "PhysicsTestbed/Ldr.h"

// Physics objects
class Prop
{
public:
	Prop();
	virtual ~Prop();
	pr::m4x4			I2W() const;
	void				ApplyGravity();
	void				ApplyDrag(float drag);
	void				MultiAttach(parse::Multibody const& multi, Prop* parent);
	bool				IsMultibody() const;
	void				BreakMultibody();
	void				ViewStateUpdate();
	virtual void		UpdateGraphics();
	virtual void		Step(float step_size) = 0;
	virtual void		ExportTo(pr::Handle& file, bool physics_scene) const = 0;
	virtual void		OnCollision(col::Data const& col_data) = 0;
	
	Ldr						m_prop_ldr;
	std::size_t				m_created_time;
	bool					m_valid;

protected:
	Ldr						m_ldr_velocity;
	Ldr						m_ldr_ang_vel;
	Ldr						m_ldr_ang_mom;
	Ldr						m_ldr_ws_bbox;
	Ldr						m_ldr_os_bbox;
	Ldr						m_ldr_com;
	Ldr						m_ldr_inertia;
	Ldr						m_ldr_resting_contact[4];
	bool					m_displaying_resting_contacts;
	Prop*					m_parent;			// Parent object if this is a multibody, equal to this if root of a multi
	std::vector<Prop*>		m_children;
	PhysObj*				m_object;
	CollisionModel			m_col_model;		// Collision model for the prop
};
typedef std::map<pr::ldr::ObjectHandle, Prop*> TProp;
