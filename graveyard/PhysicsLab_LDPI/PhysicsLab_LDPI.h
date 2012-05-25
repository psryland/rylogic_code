//***************************************************************************
//
//	A global app for the LineDrawer plugin for testing the physics library
//
//***************************************************************************

#ifndef LDPI_PHYSICS_LAB_H
#define LDPI_PHYSICS_LAB_H

class CPhysicsLab_LDPIApp : public CWinApp
{
public:
	virtual BOOL InitInstance();
	virtual int ExitInstance();
	DECLARE_MESSAGE_MAP()
};

extern CPhysicsLab_LDPIApp g_PhysicsLab;

#endif//LDPI_PHYSICS_LAB_H
