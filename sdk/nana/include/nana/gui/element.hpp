#ifndef NANA_GUI_ELEMENT_HPP
#define NANA_GUI_ELEMENT_HPP

#include <nana/paint/graphics.hpp>
#include <nana/pat/cloneable.hpp>

namespace nana{	namespace gui
{
	namespace element
	{
		namespace detail
		{
			class draw_interface
			{
			public:
				typedef paint::graphics & graph_reference;

				virtual ~draw_interface()
				{}

				virtual void switch_to(const char*) = 0;

				virtual bool draw(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const nana::rectangle&, element_state) = 0;
			};
		}//end namespace detail

		class crook_interface
		{
		public:
			typedef paint::graphics & graph_reference;
			typedef checkstate	state;

			struct data
			{
				state	check_state;
				bool	radio;
			};
				
			virtual ~crook_interface()
			{}

			virtual bool draw(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const nana::rectangle&, element_state, const data&) = 0;
		};

		class provider
		{
		public:
			template<typename ElementInterface>
			struct factory_interface
			{
				virtual ~factory_interface(){}
				virtual ElementInterface* create() const = 0;
				virtual void destroy(ElementInterface*) const = 0;
			};

			template<typename Element, typename ElementInterface>
			class factory
				: public factory_interface<ElementInterface>
			{
			public:
				typedef factory_interface<ElementInterface> interface_type;

				ElementInterface * create() const override
				{
					return (new Element);
				}

				void destroy(ElementInterface * p) const override
				{
					delete p;
				}
			};

			void add_crook(const std::string& name, const pat::cloneable<factory_interface<crook_interface>>&);
			crook_interface* const * keeper_crook(const std::string& name);
		};

		template<typename UserElement>
		void add_crook(const std::string& name)
		{
			typedef provider::factory<UserElement, crook_interface> factory_t;
			provider().add_crook(name, pat::cloneable<typename factory_t::interface_type>(factory_t()));
		}
		
		class crook;
	}//end namespace element

	template<typename Object>
	struct object
	{
		typedef Object type;
	};

	template<typename Element> class facade;

	template<>
	class facade<element::crook>
		: public element::detail::draw_interface
	{
	public:
		typedef ::nana::paint::graphics & graph_reference;
		typedef element::crook_interface::state state;

		facade();
		facade(const char* name);

		facade & reverse();
		facade & check(state);
		state checked() const;

		facade& radio(bool);
		bool radio() const;
	public:
		//Implement draw_interface
		void switch_to(const char*) override;
		bool draw(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const nana::rectangle& r, element_state) override;
	private:
		element::crook_interface::data data_;
		element::crook_interface* const * keeper_;
	};
}//end namespace gui
}//end namespace nana

#endif	//NANA_GUI_ELEMENT_HPP
