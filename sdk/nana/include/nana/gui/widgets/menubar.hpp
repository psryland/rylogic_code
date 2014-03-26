#ifndef NANA_GUI_WIDGETS_MENUBAR_HPP
#define NANA_GUI_WIDGETS_MENUBAR_HPP
#include "widget.hpp"
#include "menu.hpp"

namespace nana
{
namespace gui
{
	namespace drawerbase
	{
		namespace menubar
		{
			class item_renderer
			{
			public:
				enum state_t{state_normal, state_highlight, state_selected};
				typedef nana::paint::graphics& graph_reference;

				item_renderer(window, graph_reference);
				virtual void background(const nana::point& pos, const nana::size& size, state_t);
				virtual void caption(int x, int y, const nana::string& text);
			private:
				window	handle_;
				graph_reference graph_;
			};

			class trigger
				: public gui::drawer_trigger
			{
				class itembase;
			public:
				trigger();
				~trigger();
				gui::menu* push_back(const nana::string&);
				gui::menu* at(size_t) const;
				std::size_t size() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void refresh(graph_reference)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
				void mouse_down(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void focus(graph_reference, const eventinfo&)		override;
				void key_down(graph_reference, const eventinfo&)	override;
				void key_up(graph_reference, const eventinfo&)		override;
				void shortkey(graph_reference, const eventinfo&)	override;
			private:
				void _m_move(bool to_left);
				bool _m_popup_menu();
				void _m_total_close();
				bool _m_close_menu();
				void _m_unload_menu_window();
				std::size_t _m_item_by_pos(int x, int y);
				bool _m_track_mouse(int x, int y);
				void _m_draw();
			private:
				widget *widget_;
				nana::paint::graphics	*graph_;
				
				itembase*	items_;

				struct state_type
				{
					enum behavior_t
					{
						behavior_none, behavior_focus, behavior_menu,
					};

					state_type();

					std::size_t active;
					behavior_t behavior;

					bool menu_active;
					bool passive_close;

					bool nullify_mouse;

					nana::gui::menu *menu;
					nana::point mouse_pos;
				}state_;
			};
		}//end namespace menubar
	}//end namespace drawerbase

	class menubar
		:	public widget_object<category::widget_tag, drawerbase::menubar::trigger>
	{
	public:
		menubar();
		menubar(window);
		void create(window);
		menu& push_back(const nana::string&);
		menu& at(size_t) const;
		std::size_t length() const;
	};//end class menubar
}//end namespace gui
}//end namespace nana


#endif
