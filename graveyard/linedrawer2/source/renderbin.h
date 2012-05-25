#ifndef RENDERBIN_H
#define RENDERBIN_H

#include "pr/renderer/Viewport/SortKey.h"

enum EDrawOrder
{
	EDrawOrder_First		= 0 << pr::rdr::ESort_RenderSortKeyOfs,
	EDrawOrder_Overlay		= 1 << pr::rdr::ESort_RenderSortKeyOfs,
	EDrawOrder_Mask			= 1 << pr::rdr::ESort_RenderSortKeyOfs
};

#endif//RENDERBIN_H
