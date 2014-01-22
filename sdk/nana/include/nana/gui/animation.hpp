/*
 *	An Animation Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/animation.hpp
 */

#ifndef NANA_GUI_ANIMATION_HPP
#define NANA_GUI_ANIMATION_HPP

#include <nana/paint/image.hpp>
#include <nana/functor.hpp>
#include <nana/memory.hpp>


namespace nana{	namespace gui
{
	class animation;

	class frameset
	{
		friend class animation;
		struct impl;
	public:
		typedef nana::functor<bool(std::size_t pos, paint::graphics&, nana::size&)> framebuilder;

		frameset();
		void push_back(const paint::image&);
		void push_back(framebuilder& fb, std::size_t length);
	private:
		nana::shared_ptr<impl> impl_;
	};

	class animation
	{
		struct branch_t
		{
			frameset frames;
			nana::functor<std::size_t(const std::string&, std::size_t, std::size_t&)> condition;
		};
		
		struct impl;
		class performance_manager;
	public:
		animation();

		void push_back(const frameset& frms);
		/*
		void branch(const std::string& name, const frameset& frms)
		{
			impl_->branches[name].frames = frms;
		}

		void branch(const std::string& name, const frameset& frms, std::function<std::size_t(const std::string&, std::size_t, std::size_t&)> condition)
		{
			auto & br = impl_->branches[name];
			br.frames = frms;
			br.condition = condition;
		}
		*/

		void looped(bool enable);

		void play();

		void pause();

		void output(window wd, const nana::point& pos);
	private:
		//A helper for old version of GCC due to the 'friend' issue
		static frameset::impl* _m_frameset_impl(frameset & );
	private:
		impl * impl_;
	};

}	//end namespace gui
}	//end namespace nana

#endif	//NANA_GUI_ANIMATION_HPP
