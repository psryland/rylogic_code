#ifndef NANA_UNICODE_BIDI_HPP
#define NANA_UNICODE_BIDI_HPP
#include <vector>

namespace nana
{
	class unicode_bidi
	{
	public:
		typedef wchar_t char_type;
		
		struct directional_override_status
		{
			enum t{neutral, right_to_left, left_to_right};
		};

		struct bidi_char
		{
			enum t{	L, LRE, LRO, R, AL, RLE, RLO,
				PDF = 0x1000, EN, ES, ET, AN, CS, NSM, BN,
				B = 0x2000, S, WS, ON};
		};

		struct bidi_category
		{
			enum t{strong, weak = 0x1000, neutral = 0x2000};
		};
		
		const static char_type LRE = 0x202A;
		const static char_type RLE = 0x202B;
		const static char_type PDF = 0x202C;
		const static char_type LRO = 0x202D;
		const static char_type RLO = 0x202E;
		const static char_type LRM = 0x200E;
		const static char_type RLM = 0x200F;
		
		struct remember
		{
			unsigned level;
			directional_override_status::t override;	
		};
		
		struct entity
		{
			const wchar_t * begin, * end;
			bidi_char::t bidi_char_type;
			unsigned level;
		};

		unicode_bidi();
		void linestr(const char_type* str, std::size_t len, std::vector<entity> & reordered);
	private:
		static unsigned _m_paragraph_level(const char_type * begin, const char_type * end);

		void _m_push_entity(const char_type * begin, const char_type *end, unsigned level, bidi_char::t bidi_char_type);

		std::vector<entity>::iterator _m_search_first_character();

		bidi_char::t _m_eor(std::vector<entity>::iterator i);

		void _m_resolve_weak_types();
		void _m_resolve_neutral_types();
		void _m_resolve_implicit_levels();
		void _m_reordering_resolved_levels(const char_type * str, std::vector<entity> & reordered);
		static bidi_category::t _m_bidi_category(bidi_char::t bidi_char_type);
		static bidi_char::t _m_char_dir(char_type ch);
	private:
		void _m_output_levels() const;
		void _m_output_bidi_char() const;
	private:
		std::vector<entity>	levels_;
	};

}

#endif
