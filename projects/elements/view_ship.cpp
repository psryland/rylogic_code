#include "elements/stdafx.h"
#include "elements/view_ship.h"
#include "elements/view_base.h"
#include "elements/game_instance.h"

using namespace pr::console;

namespace ele
{
	ViewShip::ViewShip(pr::Console& cons, GameInstance& inst)
		:ViewBase(cons, inst)
	{}
}