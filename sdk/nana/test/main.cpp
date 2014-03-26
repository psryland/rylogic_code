#include <nana/gui/wvl.hpp>
#include <nana/gui/widgets/form.hpp>
#include <nana/gui/widgets/treebox.hpp>

using namespace nana;
using namespace nana::gui;

int main(void)
{
	form dlg;
	treebox tree(dlg, rectangle(point(0,0), dlg.size()), true);
	auto item = tree.insert(STR("eh"), STR("wtf?"));

	dlg.show();

	nana::gui::exec();
}