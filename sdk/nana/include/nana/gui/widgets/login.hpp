#ifndef NANA_GUI_WIDGETS_LOGIN_HPP
#define NANA_GUI_WIDGETS_LOGIN_HPP

#include "widget.hpp"
#include <nana/pat/cloneable.hpp>

namespace nana{ namespace gui{
	class login;

	namespace drawerbase
	{
		namespace login
		{
			struct extra_events
			{
				struct flags_t
				{
					bool remember_user;
					bool remember_password;
				};

				nana::fn_group<void(nana::gui::login&, const nana::string& user)> forget;
				nana::fn_group<bool(nana::gui::login&, const nana::string& user, const nana::string& password, const flags_t&)> verify;
				nana::fn_group<void(nana::gui::login&, const nana::string& user)> remove;
			};

			class trigger
				: public drawer_trigger
			{
				class drawer;
			public:
				struct label_strings
				{
					nana::string user;
					nana::string password;
					nana::string forget;
					nana::string remember_user;
					nana::string remember_password;
					nana::string login;
					nana::string remove;
					nana::string require_user;
					nana::string require_password;
					nana::string other_user;
				};

				trigger();
				~trigger();

				drawer * get_drawer() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void detached()	override;
				void refresh(graph_reference)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
			private:
				drawer * impl_;
			};
		}
	}//end namespace drawerbase

	class login
		: public widget_object<category::widget_tag, drawerbase::login::trigger>
	{
	public:
		typedef drawer_trigger_t::label_strings label_strings_t;
		typedef drawerbase::login::extra_events ext_event_type;

		login();
		login(window, bool visible);
		login(window, const rectangle& = rectangle(), bool visible = true);

		bool transparent() const;
		void transparent(bool);

		void insert(const nana::string& user, const nana::string& password);
		void insert(const nana::string& user, const nana::string& password, const nana::paint::image&);

		ext_event_type& ext_event() const;

		void selection(bool);
		bool selection() const;
		void set(const label_strings_t&);
		void reset();
	};

}//end namespace gui 
}//end namespace nana

#endif
