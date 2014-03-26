/*
 *	A Runtime Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/runtime_manager.hpp
 *
 */
#ifndef NANA_GUI_DETAIL_RUNTIME_MANAGER_HPP
#define NANA_GUI_DETAIL_RUNTIME_MANAGER_HPP

#include <map>

namespace nana
{
namespace gui
{
	namespace detail
	{
		template<typename Window, typename Bedrock>
		class runtime_manager
		{
		public:
			typedef Window	window_handle;
			
			template<typename Form>
			Form* create_form()
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				if (holder->create())
				{
					holder_[holder->get_handle()] = holder;
					return holder->get();
				}

				delete holder;
				return nullptr;
			}

			template<typename Form, typename Param>
			Form* create_form(Param param)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				if (holder->template create<Param>(param))
				{
					holder_[holder->get_handle()] = holder;
					return holder->get();
				}

				delete holder;
				return nullptr;
			}

			template<typename Form, typename Param1, typename Param2>
			Form* create_form(Param1 p1, Param2 p2)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				if (holder->template create<Param1, Param2>(p1, p2))
				{
					holder_[holder->get_handle()] = holder;
					return holder->get();
				}

				delete holder;
				return nullptr;
			}

			template<typename Form, typename Param1, typename Param2, typename Param3>
			Form* create_form(Param1 p1, Param2 p2, Param3 p3)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				if (holder->template create<Param1, Param2, Param3>(p1, p2, p3))
				{
					holder_[holder->get_handle()] = holder;
					return holder->get();
				}

				delete holder;
				return nullptr;
			}

			template<typename Form, typename Param1, typename Param2, typename Param3, typename Param4>
			Form* create_form(Param1 p1, Param2 p2, Param3 p3, Param4 p4)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				if (holder->template create<Param1, Param2, Param3, Param4>(p1, p2, p3, p4))
				{
					holder_[holder->get_handle()] = holder;
					return holder->get();
				}

				delete holder;
				return nullptr;
			}

			template<typename Form, typename Param1, typename Param2, typename Param3, typename Param4, typename Param5>
			Form* create_form(Param1 p1, Param2 p2, Param3 p3, Param4 p4, Param5 p5)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				if (holder->template create<Param1, Param2, Param3, Param4, Param5>(p1, p2, p3, p4, p5))
				{
					holder_[holder->get_handle()] = holder;
					return holder->get();
				}

				delete holder;
				return nullptr;
			}

			void remove_if_exists(window_handle wd)
			{
				auto i = holder_.find(wd);
				if(i != holder_.cend())
				{
					delete i->second;
					holder_.erase(i);
				}
			}
		private:
			class widget_holder
			{
			public:
				virtual ~widget_holder(){}
				virtual window_handle get_handle() const = 0;
			};

			template<typename Form>
			class widget_placer : public widget_holder
			{
			public:
				widget_placer()
					:	form_(nullptr)
				{}

				~widget_placer()
				{
					delete form_;
				}

				bool create()
				{
					if (nullptr == form_)
						form_ = new Form;

					return (form_ && !form_->empty());
				}

				template<typename Param>
				bool create(Param param)
				{
					if (nullptr == form_)
						form_ = new Form(param);

					return (form_ && !form_->empty());
				}

				template<typename Param1, typename Param2>
				bool create(Param1 p1, Param2 p2)
				{
					if (nullptr == form_)
						form_ = new Form(p1, p2);

					return (form_ && !form_->empty());
				}

				template<typename Param1, typename Param2, typename Param3>
				bool create(Param1 p1, Param2 p2, Param3 p3)
				{
					if (nullptr == form_)
						form_ = new Form(p1, p2, p3);

					return (form_ && !form_->empty());
				}

				template<typename Param1, typename Param2, typename Param3, typename Param4>
				bool create(Param1 p1, Param2 p2, Param3 p3, Param4 p4)
				{
					if (nullptr == form_)
						form_ = new Form(p1, p2, p3, p4);

					return (form_ && !form_->empty());
				}

				template<typename Param1, typename Param2, typename Param3, typename Param4, typename Param5>
				bool create(Param1 p1, Param2 p2, Param3 p3, Param4 p4, Param5 p5)
				{
					if (nullptr == form_)
						form_ = new Form(p1, p2, p3, p4, p5);

					return (form_ && !form_->empty());
				}

				Form* get() const
				{
					return form_;
				}

				window_handle get_handle() const override
				{
					return reinterpret_cast<window_handle>(form_ ? form_->handle() : nullptr);
				}
			private:
				Form * form_;
			};
		private:
			std::map<window_handle, widget_holder*>	holder_;
		}; //end class runtime_manager

	}//end namespace detail

}//end namespace gui
}//end namespace nana


#endif
