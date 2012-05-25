//***********************************************************************
//
//	Physics Lab Objects
//
//***********************************************************************

#ifndef OBJECTS_H
#define OBJECTS_H

#include "Maths/Maths.h"

struct OModel
{
	OModel() : m_model_id(0), m_model_list_index(0) {}
	uint	m_model_id;
	uint	m_model_list_index;
};

struct OInstance
{
	OInstance() : m_model_id(0), m_name(""), m_colour(0xFFFFFFFF), m_instance_to_world(m4x4Identity) {}
	uint		m_model_id;
	std::string	m_name;
	D3DCOLOR	m_colour;
	m4x4		m_instance_to_world;
};

#endif//OBJECTS_H