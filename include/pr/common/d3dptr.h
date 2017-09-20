//*****************************************
// DirectX Ref Ptr
//  Copyright (c) Rylogic Ltd 2011
//*****************************************
#pragma once

#include "pr/common/refptr.h"

template <typename D3DInterfaceType>
using D3DPtr = pr::RefPtr<D3DInterfaceType>;
