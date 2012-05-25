//*****************************************************************************//
//                                                                             //
//				A starting point for creating DirectX applications             //
//                                                                             //
//*****************************************************************************//

#ifndef D3DCONSOLE_H
#define D3DCONSOLE_H

#include "WinConsole.h"

//*****
// A base class to derive Directx apps from
class D3DConsole : public WinConsole
{
public:
	D3DConsole();
	virtual ~D3DConsole() {}
	
protected:

};

#endif//D3DCONSOLE_H