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
			typedef Window	wnd_type;
			
			template<typename Form>
			Form* create_form()
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				holder->create();
				return _m_manage<Form>(holder);
			}

			template<typename Form, typename Param>
			Form* create_form(Param param)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				holder->template create<Param>(param);
				return _m_manage<Form>(holder);
			}

			template<typename Form, typename Param1, typename Param2>
			Form* create_form(Param1 p1, Param2 p2)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				holder->template create<Param1, Param2>(p1, p2);
				return _m_manage<Form>(holder);
			}

			template<typename Form, typename Param1, typename Param2, typename Param3>
			Form* create_form(Param1 p1, Param2 p2, Param3 p3)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				holder->template create<Param1, Param2, Param3>(p1, p2, p3);
				return _m_manage<Form>(holder);
			}

			template<typename Form, typename Param1, typename Param2, typename Param3, typename Param4>
			Form* create_form(Param1 p1, Param2 p2, Param3 p3, Param4 p4)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				holder->template create<Param1, Param2, Param3, Param4>(p1, p2, p3, p4);
				return _m_manage<Form>(holder);
			}

			template<typename Form, typename Param1, typename Param2, typename Param3, typename Param4, typename Param5>
			Form* create_form(Param1 p1, Param2 p2, Param3 p3, Param4 p4, Param5 p5)
			{
				widget_placer<Form> * holder = new widget_placer<Form>;
				holder->template create<Param1, Param2, Param3, Param4, Param5>(p1, p2, p3, p4, p5);
				return _m_manage<Form>(holder);
			}

			void remove_if_exists(wnd_type wnd)
			{
				typename std::map<wnd_type, widget_holder*>::iterator it = holder_.find(wnd);
				if(it != holder_.end())
				{
					delete it->second;
					holder_.erase(it);
				}
			}
		private:
			class widget_holder
			{
			public:
				virtual ~widget_holder(){}
				virtual void* read() = 0;
			};

			template<typename Form>
			class widget_placer: public widget_holder
			{
			public:
				widget_placer():empty_(true){}
				~widget_placer()
				{
					if(empty_ == false)
						reinterpret_cast<Form*>(object_place_)->~Form();	
				}

				void create()
				{
					if(empty_)
					{
						empty_ = false;
						new (object_place_) Form;
					}
				}

				template<typename Param>
				void create(Param param)
				{
					if(empty_)
					{
						empty_ = false;
						new (object_place_) Form(param);
					}			
				}

				template<typename Param1, typename Param2>
				void create(Param1 p1, Param2 p2)
				{
					if(empty_)
					{
						empty_ = false;
						new (object_place_) Form(p1, p2);
					}			
				}

				template<typename Param1, typename Param2, typename Param3>
				void create(Param1 p1, Param2 p2, Param3 p3)
				{
					if(empty_)
					{
						empty_ = false;
						new (object_place_) Form(p1, p2, p3);
					}			
				}

				template<typename Param1, typename Param2, typename Param3, typename Param4>
				void create(Param1 p1, Param2 p2, Param3 p3, Param4 p4)
				{
					if(empty_)
					{
						empty_ = false;
						new (object_place_) Form(p1, p2, p3, p4);
					}			
				}

				template<typename Param1, typename Param2, typename Param3, typename Param4, typename Param5>
				void create(Param1 p1, Param2 p2, Param3 p3, Param4 p4, Param5 p5)
				{
					if(empty_)
					{
						empty_ = false;
						new (object_place_) Form(p1, p2, p3, p4, p5);
					}			
				}

				void* read()
				{
					return object_place_;
				}
			private:
				bool empty_;
				char object_place_[sizeof(Form)];
			};

		private:
			template<typename Form>
			Form* _m_manage(widget_holder* entity)
			{
				Form * f = reinterpret_cast<Form*>(entity->read());
	
				wnd_type handle = reinterpret_cast<wnd_type>(f->handle());
				if(handle == 0)
				{
					f = 0;
					delete entity;
				}
				else
					holder_[handle] = entity;
				
				return f;
			}

		private:
			std::map<wnd_type, widget_holder*>	holder_;
		}; //end class runtime_manager

	}//end namespace detail

}//end namespace gui
}//end namespace nana


#endif
