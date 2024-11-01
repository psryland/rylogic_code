#include "src/forward.h"
#include "src/view_ship.h"
#include "src/view_base.h"
#include "src/game_instance.h"

using namespace pr::console;

namespace ele
{
	ViewShip::ViewShip(pr::Console& cons, GameInstance& inst)
		:ViewBase(cons, inst)
	{}
}