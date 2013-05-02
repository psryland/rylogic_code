//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#include "sol/main/stdafx.h"
#include "sol/objects/star.h"

using namespace pr;
using namespace sol;

Star::Star(pr::v4 const& position, float radius, float mass, pr::Renderer& rdr, wchar_t const* texture)
:AstronomicalBody(position, radius, mass, rdr, texture)
{}

