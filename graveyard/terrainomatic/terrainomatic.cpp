
#include "PR/Common/Fmt.h"
#include "PR/Common/LineDrawerHelper.h"
#include "PR/Geometry/PRGeometry.h"
#include "PR/Geometry/Manipulator/Manipulator.h"
#include "PR/Geometry/XFile/XFile.h"
#include "Planet.h"

using namespace pr;

void main()
{
	Planet planet(0.5f);

	xfile::Save(planet.m_globe, "C:/DeleteMe/planet.x");
}
