/*
 *	Paint Image Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/paint/image.cpp
 */

#include <nana/config.hpp>
#include PLATFORM_SPEC_HPP
#include <nana/paint/image.hpp>
#include <nana/auto_buf.hpp>
#include <algorithm>
#include <fstream>
#include <iterator>

#include <nana/paint/detail/image_impl_interface.hpp>
#include <nana/paint/pixel_buffer.hpp>
#if defined(NANA_ENABLE_PNG)
	#include <nana/paint/detail/image_png.hpp>
#endif
#include <nana/paint/detail/image_bmp.hpp>
#include <nana/paint/detail/image_ico.hpp>

namespace nana
{
namespace paint
{
	namespace detail
	{
		//class image_ico
			image_ico::image_ico(bool is_ico): is_ico_(is_ico){}

			bool image_ico::open(const nana::char_t* filename)
			{
				close();
#if defined(NANA_WINDOWS)
				HICON handle = 0;
				if(is_ico_)
				{
					handle = (HICON)::LoadImage(0, filename, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
				}
				else
				{
					SHFILEINFO    sfi;
					::SHGetFileInfo(filename, 0, &sfi, sizeof(sfi), SHGFI_ICON);
					handle = sfi.hIcon;
				}

				if(handle)
				{
					ptr_ = nana::shared_ptr<HICON>(new HICON(handle));
					ICONINFO info;
					::GetIconInfo(handle, &info);
					size_.width = (info.xHotspot << 1);
					size_.height = (info.yHotspot << 1);

					::DeleteObject(info.hbmColor);
					::DeleteObject(info.hbmMask);
					return true;
				}
#endif
				return false;
			}

			bool image_ico::alpha_channel() const
			{
				return false;
			}

			bool image_ico::empty() const
			{
				return (!ptr_);
			}

			void image_ico::close()
			{
				ptr_.reset();
			}

			nana::size image_ico::size() const
			{
				return size_;
			}

			void image_ico::paste(const nana::rectangle& src_r, graph_reference graph, int x, int y) const
			{
				if(ptr_ && (graph.empty() == false))
				{
#if defined(NANA_WINDOWS)
					::DrawIconEx(graph.handle()->context, x, y, *ptr_, src_r.width, src_r.height, 0, 0, DI_NORMAL);
#endif
				}
			}

			void image_ico::stretch(const nana::rectangle&, graph_reference graph, const nana::rectangle& r) const
			{
				if(ptr_ && (graph.empty() == false))
				{
#if defined(NANA_WINDOWS)
					::DrawIconEx(graph.handle()->context, r.x, r.y, *ptr_, r.width, r.height, 0, 0, DI_NORMAL);
#endif
				}
			}

			const image_ico::ptr_t& image_ico::ptr() const
			{
				return ptr_;
			}

#if defined(NANA_WINDOWS)
			//struct handle_deleter
				void image_ico::handle_deleter::operator()(HICON handle) const
				{
					if(handle)	::DestroyIcon(handle);
				}
			//end struct handle_deleter
#endif
		//end class image_ico
	}

	image::image_impl_interface::~image_impl_interface()
	{}

	namespace detail
	{
		int toupper(int c)
		{
			return (('a' <= c && c <= 'z') ? 
					c - ('a' - 'A')
					: c);
		}
	}//end namespace detail

	//class image
		image::image()
		{}

		image::image(const image& rhs)
			:image_ptr_(rhs.image_ptr_)
		{}

		image::image(const nana::char_t* file)
		{
			if(file)
				open(file);
		}

		image::image(const nana::string& file)
		{
			this->open(file);
		}

		image::~image()
		{
			close();
		}

		image & image::operator=(const image& rhs)
		{
			if(this != &rhs)
				image_ptr_ = rhs.image_ptr_;
			
			return * this;
		}

		bool image::open(const nana::string& filename)
		{
			image_ptr_.reset();
			image::image_impl_interface * helper = 0;

			if(filename.size())
			{
				nana::string fn;
				std::transform(filename.begin(), filename.end(), std::back_inserter(fn), detail::toupper);

				if(filename.size() >= 4)
				{
					nana::string suffix = fn.substr(fn.size() - 4);
					if(STR(".ICO") == suffix)
					{
#if defined(NANA_WINDOWS)
						helper = new detail::image_ico(true);
#endif
					}
#if defined(NANA_ENABLE_PNG)
					else if(STR(".PNG") == suffix)
						helper = new detail::image_png;
#endif
				}
				
				if(0 == helper)
				{
#if defined(NANA_UNICODE)
					std::ifstream ifs(static_cast<std::string>(nana::charset(filename)).c_str(), std::ios::binary);
#else
					std::ifstream ifs(filename.c_str(), std::ios::binary);
#endif
					if(ifs)
					{
						unsigned short meta = 0;
						ifs.read(reinterpret_cast<char*>(&meta), 2);
						if(*reinterpret_cast<const short*>("BM") == meta)
							helper = new detail::image_bmp;
						else if(*reinterpret_cast<const short*>("MZ") == meta)
							helper = new detail::image_ico(false);
					}
				}

				if(helper)
				{
					image_ptr_ = nana::shared_ptr<image_impl_interface>(helper);
					return helper->open(filename.data());
				}
			}
			return false;
		}

		bool image::empty() const
		{
			return ((!image_ptr_) || image_ptr_->empty());
		}

		image::operator const void*() const
		{
			return (image_ptr_ ? this : 0);
		}

		void image::close()
		{
			image_ptr_.reset();
		}

		nana::size image::size() const
		{
			return (image_ptr_ ? image_ptr_->size() : nana::size());
		}

		void image::paste(graphics& dst, int x, int y) const
		{
			image_impl_interface * sp = image_ptr_.get();
			if(sp)
				sp->paste(sp->size(), dst, x, y);
		}

		void image::paste(const nana::rectangle& r_src, graphics & dst, const nana::point& p_dst) const
		{
			image_impl_interface * sp = image_ptr_.get();
			if(sp)
				sp->paste(r_src, dst, p_dst.x, p_dst.y);	
		}

		void image::stretch(const nana::rectangle& r_src, graphics& dst, const nana::rectangle & r_dst) const
		{
			image_impl_interface * sp = image_ptr_.get();
			if(sp)
				sp->stretch(r_src, dst, r_dst);			
		}
	//end class image

}//end namespace paint
}//end namespace nana

