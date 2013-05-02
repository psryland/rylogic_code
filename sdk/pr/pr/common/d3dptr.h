//*****************************************
// DirectX Ref Ptr
//  Copyright © Rylogic Ltd 2011
//*****************************************
#pragma once
#ifndef PR_COMMON_D3D_POINTER_H
#define PR_COMMON_D3D_POINTER_H

#include "pr/common/assert.h"
#include "pr/common/refptr.h"

template <typename D3DInterfaceType> struct D3DPtr :pr::RefPtr<D3DInterfaceType>
{
	D3DPtr()
	:pr::RefPtr<D3DInterfaceType>()
	{}
	
	D3DPtr(int i)
	:pr::RefPtr<D3DInterfaceType>(i)
	{}
	
	D3DPtr(D3DPtr const& copy)
	:pr::RefPtr<D3DInterfaceType>(copy)
	{}

	template <typename IBaseType> D3DPtr(D3DPtr<IBaseType> const& rhs)
	:pr::RefPtr<D3DInterfaceType>(rhs)
	{}
	
	// Adopt a raw d3d interface pointer.
	// Creating the d3d pointer will have caused AddRef() to be called already.
	// So we can undo the AddRef() done by pr::RefPtr<>.
	// Set 'add_ref' to true for the cases when d3d hasn't done the AddRef()
	D3DPtr(D3DInterfaceType* t, bool add_ref = false)
	:pr::RefPtr<D3DInterfaceType>(t)
	{
		if (m_ptr && !add_ref)
		{
			// Check that the pointer will still have at least one reference
			PR_ASSERT(PR_DBG, pr::PtrRefCount(m_ptr) > 1, "This pointer only has one reference, 'add_ref = true' is needed");
			DecRef(m_ptr);
		}
	}
};

#endif


