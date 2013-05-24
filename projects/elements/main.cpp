#include "elements/stdafx.h"
#include "elements/game_instance.h"
#include "elements/console_ui.h"

int main(int, char*[])
{
	ele::GameInstance ctx(0);
	ele::ConsoleUI ui(ctx);
}
