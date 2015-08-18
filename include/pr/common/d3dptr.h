//*****************************************
// DirectX Ref Ptr
//  Copyright (c) Rylogic Ltd 2011
//*****************************************
#pragma once

#include "pr/common/refptr.h"

template <typename D3DInterfaceType> struct D3DPtr :pr::RefPtr<D3DInterfaceType>
{
	D3DPtr()
		:pr::RefPtr<D3DInterfaceType>()
	{}

	D3DPtr(nullptr_t)
		:pr::RefPtr<D3DInterfaceType>(nullptr)
	{}

	D3DPtr(D3DPtr&& rhs)
		:pr::RefPtr<D3DInterfaceType>(std::move(rhs))
	{}

	D3DPtr(D3DPtr const& rhs)
		:pr::RefPtr<D3DInterfaceType>(rhs)
	{}

	template <typename IBaseType>
	D3DPtr(D3DPtr<IBaseType>&& rhs)
		:pr::RefPtr<D3DInterfaceType>(std::move(rhs))
	{}

	template <typename IBaseType>
	D3DPtr(D3DPtr<IBaseType> const& rhs)
		: pr::RefPtr<D3DInterfaceType>(rhs)
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
			assert(pr::PtrRefCount(m_ptr) > 1 && "This pointer only has one reference, 'add_ref = true' is needed");
			DecRef(m_ptr);
		}
	}

	D3DPtr& operator = (D3DPtr&& rhs)
	{
		static_cast<pr::RefPtr<D3DInterfaceType>&>(*this) = std::move(rhs);
		return *this;
	}

	D3DPtr& operator = (D3DPtr const& rhs)
	{
		static_cast<pr::RefPtr<D3DInterfaceType>&>(*this) = rhs;
		return *this;
	}
};
