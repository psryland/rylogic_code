//*********************************************************************************
//
// Helper functions
//
//*********************************************************************************

#ifndef UTILITY_H
#define UTILITY_H

#include "Headers.h"

extern Matrix3	g_Max_Transform;
extern Point3	g_Max_Scale;


Mesh* GetMesh(Interface* max_interface, INode* node);

#endif//UTILITY_H