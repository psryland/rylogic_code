//******************************************
// Collision Call Backs
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"

// Unified collision data
namespace col
{
	struct Contact
	{
		Contact() {}
		Contact(pr::v4 const& ws_point, pr::v4 const& ws_normal, pr::v4 const& ws_impulse, pr::v4 const& ws_delta_vel, pr::uint	prim_id)
		:m_ws_point(ws_point)
		,m_ws_normal(ws_normal)
		,m_ws_impulse(ws_impulse)
		,m_ws_delta_vel(ws_delta_vel)
		,m_prim_id(prim_id)
		{}
		pr::v4		m_ws_point;
		pr::v4		m_ws_normal;
		pr::v4		m_ws_impulse;
		pr::v4		m_ws_delta_vel;
		pr::uint	m_prim_id;
	};

	struct Data
	{
		PhysObj const*	m_objA;
		PhysObj const*	m_objB;
		ColInfo const*	m_info;
		
		// Implemented in the respective physics engine.cpp's
		virtual pr::uint NumContacts() const = 0;
		virtual Contact	 GetContact(int obj_index, int contact_index) const = 0;
	};

}//namespace col

// Collision call back observers
typedef bool (*PreCollisionCallBack)(col::Data const& col_data); // Return true for the collision to proceed
typedef void (*PstCollisionCallBack)(col::Data const& col_data);
typedef std::vector<PreCollisionCallBack> TPreCollCB; extern TPreCollCB g_pre_coll_cb;
typedef std::vector<PstCollisionCallBack> TPstCollCB; extern TPstCollCB g_pst_coll_cb;
inline void RegisterPreCollisionCB(PreCollisionCallBack func, bool add)
{
	TPreCollCB::iterator fn = g_pre_coll_cb.begin(), fn_end = g_pre_coll_cb.end();
	for( ;fn != fn_end; ++fn )
	{
		if( *fn == func ) break;
	}
	if	   (  add && fn == fn_end ) g_pre_coll_cb.push_back(func);
	else if( !add && fn != fn_end ) g_pre_coll_cb.erase(fn);
}
inline void RegisterPstCollisionCB(PstCollisionCallBack func, bool add)
{
	TPstCollCB::iterator fn = g_pst_coll_cb.begin(), fn_end = g_pst_coll_cb.end();
	for( ;fn != fn_end; ++fn )
	{
		if( *fn == func ) break;
	}
	if	   (  add && fn == fn_end ) g_pst_coll_cb.push_back(func);
	else if( !add && fn != fn_end ) g_pst_coll_cb.erase(fn);
}

