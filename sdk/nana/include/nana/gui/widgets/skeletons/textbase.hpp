/*
 *	A textbase class implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/skeletons/textbase.hpp
 *	@description: This class manages the multi-line text and provides some operation on text
 */

#ifndef NANA_GUI_WIDGET_DETAIL_TEXTBASE_HPP
#define NANA_GUI_WIDGET_DETAIL_TEXTBASE_HPP
#include <string>
#include <deque>
#include <memory>
#include <fstream>
#include <nana/charset.hpp>

#include "textbase_extra_evtbase.hpp"

namespace nana
{
namespace gui
{
namespace widgets
{
namespace skeletons
{
	template<typename CharT>
	class textbase
		: public noncopyable
	{
	public:
		typedef CharT						char_type;
		typedef std::basic_string<CharT>	string_type;
		typedef typename string_type::size_type	size_type;

		textbase()
			: evtbase_(nullptr), changed_(false)
		{
			//Insert an empty string for the first line of empty text.
			text_cont_.emplace_back();
		}

		void bind_ext_evtbase(textbase_extra_evtbase<char_type>& extevt)
		{
			evtbase_ = &extevt;
		}

		bool empty() const
		{
			return (text_cont_.size() == 0 ||
					((text_cont_.size() == 1) && (text_cont_[0].size() == 0)));
		}

		void load(const char* tfs)
		{
			std::ifstream ifs(tfs);
			ifs.seekg(0, std::ios::end);
			std::size_t bytes = static_cast<std::size_t>(ifs.tellg());
			ifs.seekg(0, std::ios::beg);

			if(bytes >= 2)
			{
				int ch = ifs.get();
				if(0xEF == ch && bytes >= 3)
				{
					//UTF8
					ch = ifs.get();
					if(0xBB == ch && 0xBF == ifs.get())
					{
						ifs.close();
						load(tfs, nana::unicode::utf8);
						return;
					}
				}
				else if(0xFF == ch)
				{
					if(0xFE == ifs.get())
					{
						//UTF16,UTF32
						if(bytes >= 4)
						{
							if(ifs.get() == 0 && ifs.get() == 0)
							{
								ifs.close();
								load(tfs, nana::unicode::utf32);
								return;
							}
						}
						ifs.close();
						load(tfs, nana::unicode::utf16);
						return;
					}
				}
				else if(0xFE == ch)
				{
					if(ifs.get() == 0xFF)
					{
						//UTF16(big-endian)
						ifs.close();
						load(tfs, nana::unicode::utf16);
						return;
					}
				}
				else if(0 == ch)
				{
					if(bytes >= 4 && ifs.get() == 0)
					{
						ch = ifs.get();
						if(0xFE == ch && ifs.get() == 0xFF)
						{
							//UTF32(big_endian)
							ifs.close();
							load(tfs, nana::unicode::utf32);
							return;
						}
					}
				}
			}

			ifs.clear();
			ifs.seekg(0, std::ios::beg);

			text_cont_.clear();		//Clear only if the file can be opened.
			attr_max_.reset();

			std::string str;
			std::size_t lines = 0;
			while(ifs.good())
			{
				std::getline(ifs, str);
				text_cont_.push_back(nana::charset(str));
				if(text_cont_.back().size() > attr_max_.size)
				{
					attr_max_.size = text_cont_.back().size();
					attr_max_.line = lines;
				}
				++lines;
			}
		}

		static void byte_order_translate_2bytes(std::string& str)
		{
			char * s = const_cast<char*>(str.c_str());
			char * end = s + str.size();
			for(; s < end; s += 2)
			{
				char c = *s;
				*s = *(s + 1);
				*(s + 1) = c;
			}
		}

		static void byte_order_translate_4bytes(std::string& str)
		{
			char * s = const_cast<char*>(str.c_str());
			char * end = s + str.size();
			for(; s < end; s += 4)
			{
				char c = *s;
				*s = *(s + 3);
				*(s + 3) = c;

				c = *(s + 1);
				*(s + 1) = *(s + 2);
				*(s + 2) = c;
			}
		}

		void load(const char * tfs, nana::unicode encoding)
		{
			std::ifstream ifs(tfs);
			std::string str;
			bool big_endian = true;

			if(ifs.good())
			{
				text_cont_.clear();		//Clear only if the file can be opened.
				attr_max_.reset();

				std::getline(ifs, str);

				std::size_t len_of_BOM = 0;
				switch(encoding)
				{
				case nana::unicode::utf8:
					len_of_BOM = 3;	break;
				case nana::unicode::utf16:
					len_of_BOM = 2;	break;
				case nana::unicode::utf32:
					len_of_BOM = 4;	break;
				}

				big_endian = (str[0] == 0x00 || str[0] == char(0xFE));
				str.erase(0, len_of_BOM);
				if(big_endian)
				{
					if(nana::unicode::utf16 == encoding)
						byte_order_translate_2bytes(str);
					else
						byte_order_translate_4bytes(str);
				}

				text_cont_.push_back(nana::charset(str, encoding));
				if(text_cont_.back().size() > attr_max_.size)
				{
					attr_max_.size = text_cont_.back().size();
					attr_max_.line = 0;
				}
			}

			std::size_t lines = 1;

			while(ifs.good())
			{
				std::getline(ifs, str);

				if(big_endian)
				{
					if(nana::unicode::utf16 == encoding)
						byte_order_translate_2bytes(str);
					else
						byte_order_translate_4bytes(str);
				}

				text_cont_.push_back(nana::charset(str, encoding));
				if(text_cont_.back().size() > attr_max_.size)
				{
					attr_max_.size = text_cont_.back().size();
					attr_max_.line = lines;
				}
				++lines;
			}
		}

		void store(const char* tfs) const
		{
			std::ofstream ofs(tfs, std::ios::binary);
			if(ofs && text_cont_.size())
			{
				if(text_cont_.size() > 1)
				{
					for(auto i = text_cont_.cbegin(), end = text_cont_.cend() - 1; i != end; ++i)
					{
						std::string mbs = nana::charset(*i);
						ofs.write(mbs.c_str(), mbs.size());
						ofs.write("\r\n", 2);
					}
				}
				std::string mbs = nana::charset(text_cont_.back());
				ofs.write(mbs.c_str(), mbs.size());
				_m_saved(tfs);
			}
		}

		void store(const char* tfs, nana::unicode encoding) const
		{
			std::ofstream ofs(tfs, std::ios::binary);
			if(ofs && text_cont_.size())
			{
				const char * le_boms[] = {"\xEF\xBB\xBF", "\xFF\xFE", "\xFF\xFE\x0\x0"};	//BOM for little-endian
				int bytes = 0;
				switch(encoding)
				{
				case nana::unicode::utf8:
					bytes = 3;	break;
				case nana::unicode::utf16:
					bytes = 2;	break;
				case nana::unicode::utf32:
					bytes = 4;	break;
				}

				if(bytes)
					ofs.write(le_boms[static_cast<int>(encoding)], bytes);

				if(text_cont_.size() > 1)
				{
					std::string mbs;
					for(auto i = text_cont_.cbegin(), end = text_cont_.cend() - 1; i != end; ++i)
					{
						mbs = nana::charset(*i).to_bytes(encoding);
						mbs += "\r\n";
						ofs.write(mbs.c_str(), static_cast<std::streamsize>(mbs.size()));
					}
				}
				std::string mbs = nana::charset(text_cont_.back()).to_bytes(encoding);
				ofs.write(mbs.c_str(), static_cast<std::streamsize>(mbs.size()));
				_m_saved(tfs);
			}
		}

		size_type lines() const
		{
			return text_cont_.size();
		}

		const string_type& getline(size_type pos) const
		{
			if(pos < text_cont_.size())
				return text_cont_[pos];

			if(nullptr == nullstr_)
				nullstr_ = std::shared_ptr<string_type>(new string_type);
			return *nullstr_;
		}

		std::pair<size_t, size_t> max_line() const
		{
			return std::make_pair(attr_max_.line, attr_max_.size);
		}
	public:
		void replace(size_type pos, const char_type* text)
		{
			if(text_cont_.size() <= pos)
			{
				text_cont_.emplace_back(text);
				pos = text_cont_.size() - 1;
			}
			else
				text_cont_[pos] = text;

			_m_make_max(pos);
			_m_edited();
		}

		void insert(size_type line, size_type pos, const char_type* str)
		{
			if(line < text_cont_.size())
			{
				string_type& lnstr = text_cont_[line];

				if(pos < lnstr.size())
					lnstr.insert(pos, str);
				else
					lnstr += str;
			}
			else
			{
				text_cont_.emplace_back(str);
				line = text_cont_.size() - 1;
			}

			_m_make_max(line);
			_m_edited();
		}

		void insert(size_type line, size_type pos, char_type ch)
		{
			if(line < text_cont_.size())
			{
				string_type& lnstr = text_cont_[line];

				if(pos < lnstr.size())
					lnstr.insert(pos, 1, ch);
				else
					lnstr += ch;
			}
			else
			{
				text_cont_.emplace_back(1, ch);
				line = text_cont_.size() - 1;
			}

			_m_make_max(line);
			_m_edited();
		}

		void insertln(size_type line, const string_type& str)
		{
			if(line < text_cont_.size())
				text_cont_.insert(text_cont_.begin() + line, str);
			else
				text_cont_.push_back(str);

			_m_make_max(line);
			_m_edited();
		}

		void erase(size_type line, size_type pos, size_type count)
		{
			if(line < text_cont_.size())
			{
				string_type& lnstr = text_cont_[line];
				if((pos == 0) && (count >= lnstr.size()))
					lnstr.clear();
				else
					lnstr.erase(pos, count);

				if(attr_max_.line == line)
					_m_scan_for_max();

				_m_edited();
			}
		}

		void erase(size_type pos)
		{
			if(pos < text_cont_.size())
				text_cont_.erase(text_cont_.begin() + pos);

			if(pos == attr_max_.line)
				_m_scan_for_max();
			else if(pos < attr_max_.line)
				attr_max_.line--;

			_m_edited();
		}

		void erase_all()
		{
			std::deque<string_type>().swap(text_cont_);
			attr_max_.reset();
			_m_saved("");
		}

		void merge(size_type pos)
		{
			if(text_cont_.size() && (pos < text_cont_.size() - 1))
			{
				text_cont_[pos] += text_cont_[pos + 1];
				text_cont_.erase(text_cont_.begin() + (pos + 1));
				_m_make_max(pos);
				if(pos < attr_max_.line)
					--attr_max_.line;

				_m_edited();
			}
		}

		const std::string& filename() const
		{
			return filename_;
		}

		bool edited() const
		{
			return changed_;
		}

		bool saved() const
		{
			return ! not_saved();
		}

		bool not_saved() const
		{
			return edited() || filename_.empty();
		}
	private:
		void _m_make_max(std::size_t pos)
		{
			const string_type& str = text_cont_[pos];
			if(str.size() > attr_max_.size)
			{
				attr_max_.size = str.size();
				attr_max_.line = pos;
			}
		}

		void _m_scan_for_max()
		{
			attr_max_.size = 0;
			std::size_t n = 0;
			for(auto & s : text_cont_)
			{
				if(s.size() > attr_max_.size)
				{
					attr_max_.size = s.size();
					attr_max_.line = n;
				}
				++n;
			}
		}

		void _m_first_change() const
		{
			if(evtbase_)
				evtbase_->first_change();
		}

        void _m_saved(std::string && filename) const
        {
            if(filename_ != filename)
            {
                filename_ = std::move(filename);
                changed_ = false;
                _m_first_change();
            } 
            else if(changed_)
            {
                changed_ = false;
                _m_first_change();
            }
            changed_ = false;
        }

        void _m_edited()
        {
            if(!changed_)
            {
                changed_ = true;
                _m_first_change();
            }
            changed_ = true;
        }
	private:
		std::deque<string_type>	text_cont_;
		textbase_extra_evtbase<char_type>*	evtbase_;

		mutable bool			changed_;
		mutable std::string		filename_;	//A string for the saved filename.
		mutable std::shared_ptr<string_type> nullstr_;

		struct attr_max
		{
			attr_max()
				:line(0), size(0)
			{}

			std::size_t line;
			std::size_t size;

			void reset()
			{
				line = 0;
				size = 0;
			}
		}attr_max_;
	};

}//end namespace detail
}//end namespace widgets
}//end namespace gui
}//end namespace nana
#endif
