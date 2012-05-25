//*********************************************************************************
//
// Helper functions
//
//*********************************************************************************
#include "Headers.h"
#include "Common\MsgBox.h"
#include "Utility.h"

//*****
// Externed globals
Matrix3	g_Max_Transform;
Point3	g_Max_Scale;

//*****
// File global prototypes
bool	GetTransform(INode* node);
Point3	GetVertexNormal(Mesh* mesh, int face_number, int vertex_number);
bool	IsRotationEqual(Matrix3 &a, Matrix3 &b);

//*****
// Get the mesh corresponding to node number 'node_number'
Mesh* GetMesh(Interface* max_interface, INode* node)
{
	TimeValue	time	= max_interface->GetTime();
	Object*		object	= node->EvalWorldState(time).obj;
	if( !object ) return NULL;
	
	if( !object->CanConvertToType(Class_ID(TRIOBJ_CLASS_ID, 0)) )
    {
		MsgBox("Error", "Can't convert object to a tri object.");
		return NULL;
	}

	TriObject *triobject = (TriObject*)object->ConvertToType(time, Class_ID(TRIOBJ_CLASS_ID, 0));
	if( !triobject )
	{
		MsgBox("Error", "Failed to get tri object.");
		return NULL;
	}
		
	Mesh* mesh = &(triobject->GetMesh());

	// Note: 'triobject' should only be deleted if the pointer to it is
	// not equal to the 'object' pointer that called ConvertToType()
	if( object != triobject ) delete triobject;
	
	if( !mesh )
	{
		MsgBox("Error", "Failed to get mesh from tri object.");
		return NULL;
	}
	
	// Calculate the MAX to Driver3 transform
	if( !GetTransform(node) )
	{
		MsgBox("Error", "Failed to calculate transform.\n");
		return NULL;
	}

	return mesh;
}

//*****
// This function calculates the matrix transform to convert from MAX space
// to game space.
bool GetTransform(INode* node)
{
	Matrix3 max_world_to_node	= node->GetNodeTM(0);
	Matrix3 max_world_to_object	= node->GetObjectTM(0);
	if( !IsRotationEqual(max_world_to_node, max_world_to_object) )
	{
		MsgBox("Error", "Object needs transform resetting.");
		return false;
	}
	Matrix3 node_to_object = max_world_to_object * Inverse(max_world_to_node);

	// Check for a scaled max_world_to_node transform
	Point3 row1 = max_world_to_node.GetRow(0);
	Point3 row2 = max_world_to_node.GetRow(1);
	Point3 row3 = max_world_to_node.GetRow(2);
	g_Max_Scale.x = FLength(row1);
	g_Max_Scale.y = FLength(row2);
	g_Max_Scale.z = FLength(row3);
	if( fabsf(g_Max_Scale.x - 1.0f) > 0.00001f ||
		fabsf(g_Max_Scale.y - 1.0f) > 0.00001f ||
		fabsf(g_Max_Scale.z - 1.0f) > 0.00001f )
	{
		MsgBox("Error", "Object is scaled.");
		return false;
	}

	// MAX is in centimetres so scale to metres
	float cmscale = float(GetMasterScale(UNITS_METERS));
	g_Max_Scale.x *= cmscale;
	g_Max_Scale.y *= cmscale;
	g_Max_Scale.z *= cmscale;

	// Add any rotations etc here...
	// e.g.
	// In MAX Z is up, convert to Y as up
//PSR...	Matrix3 rotation = max_world_to_node;
//PSR...	Matrix3 rotation = node_to_object;
//PSR...	rotation.NoTrans();
//PSR...	rotation.RotateX(-HALFPI);
//PSR...	
//PSR...	// Vertices are converted from MAX space to game space as follows:
//PSR...	// game space vertex = (g_Max_Transform * MAX vertex) * g_Max_Scale;
//PSR...	g_Max_Transform = node_to_object * rotation;

	g_Max_Transform = max_world_to_node;
	return true;
}

//*****
// This functions checks that the rotation component of two Matrix3 (actually [4][3]) are the same
bool IsRotationEqual(Matrix3 &a, Matrix3 &b)
{
	Point3	p;
	p = a.GetRow(0) - b.GetRow(0);
	if( p.x || p.y || p.z ) return false;

	p = a.GetRow(1) - b.GetRow(1);
	if( p.x || p.y || p.z ) return false;

	p = a.GetRow(2) - b.GetRow(2);
	if( p.x || p.y || p.z ) return false;
	return true;
}

//*****
// Max plugins must be linked with the Multithreaded DLL runtime library, even in debug mode.
// This stub function is here to prevent linker errors in debug
#ifndef NDEBUG
//PSR...extern "C" {
//PSR...int __cdecl _CrtDbgReport(int,const char *,int,const char *,const char *,...)
//PSR...{
//PSR...	_asm int 3;
//PSR...	return 0;
//PSR...}
//PSR...}
#endif//NDEBUG
