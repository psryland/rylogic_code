//*********************************************
// Code Edit MFC
//	(C)opyright Rylogic Limited 2007
//*********************************************

#include "CodeEdit.h"
#include "pr/common/assert.h"
#include "pr/common/PRString.h"
using namespace pr;

// A predicate for finding the range of matches for a partial word in the dictionary
struct DictPred
{
	std::size_t m_len;
	DictPred() : m_len(INT_MAX) {}
	DictPred(std::string const& word) : m_len(word.size()) {}
	bool operator ()(std::string const& lhs, std::string const& rhs) const
	{
		return _strnicmp(lhs.c_str(), rhs.c_str(), m_len) < 0;
	}
};

CCodeEdit::CCodeEdit()
:CRichEditCtrl()
{}

CCodeEdit::~CCodeEdit()
{}

// Dictionary
void CCodeEdit::AddToDictionary(std::string const& word)
{
	TDictionary::iterator i = std::lower_bound(m_dictionary.begin(), m_dictionary.end(), word, DictPred());
	if( i == m_dictionary.end() || !str::Equal(*i, word) ) m_dictionary.insert(i, word);
}
void CCodeEdit::AddToDictionary(TDictionary const& dict)
{
	for( TDictionary::const_iterator i = dict.begin(), i_end = dict.end(); i != i_end; ++i )
		AddToDictionary(*i);
}
void CCodeEdit::AddToDictionary(char const* words, std::size_t words_length)
{
	std::string str;
	char const* comma;
	char const* words_end = words + words_length;
	do
	{
		comma = std::find(words, words_end, ',');
		str.assign(words, 0, comma - words);
		AddToDictionary(str);
		words = comma + 1;
	}
	while( comma != words_end );
}


// Auto complete the current word
void CCodeEdit::DoAutoComplete()
{
	// Get the current partial word
	if( !GetCurrentWord() ) return;

	// Get words from the dictionary that match the current partial word
	TDictRange match_range = std::equal_range(m_dictionary.begin(), m_dictionary.end(), m_word_partial, DictPred(m_word_partial));
	std::size_t num_matches = match_range.second - match_range.first;
	if( num_matches == 0 ) return;

	// If there are a number of options, display a list box with them in
	if( num_matches > 1 )
	{
		// Calculate a position to display the list box
		CHARFORMAT cf; GetSelectionCharFormat(cf);
		CRect pos;
		pos.top		= GetCharPos(static_cast<long>(m_word_partial_pos_s)).y + cf.yHeight / 10;
		pos.left	= GetCharPos(static_cast<long>(m_word_partial_pos_s)).x;
		pos.right	= pos.left + 100;

		// Display the list box
		if( !m_listbox.m_hWnd )
		{
			m_listbox.Create(CBS_DROPDOWNLIST | WS_VSCROLL | WS_CHILD | WS_VISIBLE | LBS_STANDARD | LBS_HASSTRINGS, pos, this, IDC_LIST_PR_CODE_EDIT);
			m_listbox.SetFont(GetParent()->GetFont());
		}
		else
		{
			m_listbox.ResetContent();
		}
		pos.bottom = pos.top + static_cast<long>(m_listbox.GetItemHeight(0) * (num_matches + 1 < 10 ? num_matches + 1 : 10));
		m_listbox.MoveWindow(&pos);

		// Add the matching words to the list box
		for( TDictionary::const_iterator i = match_range.first; i != match_range.second; ++i )
			m_listbox.AddString(i->c_str());

		// Find the best match in the range
		TDictionary::const_iterator best_match = std::lower_bound(match_range.first, match_range.second, m_word_partial, DictPred());
		PR_ASSERT(PR_DBG, best_match != match_range.second);

		// Select the best match
		m_listbox.SelectString(-1, best_match->c_str());
	}

	// Otherwise, just complete the word
	else
	{
		SetWord(*match_range.first, TRUE);
	}
}

// Helper overload of SetSel for using std::size_t
inline void CCodeEdit::SetSel(std::size_t start, std::size_t end)
{
	CRichEditCtrl::SetSel(static_cast<long>(start), static_cast<long>(end));
}

// Scan backwards from the current cursor position to the first whitespace
// Return this as the current word
bool CCodeEdit::GetCurrentWord()
{
	CHARRANGE selection; GetSel(selection);
	long pos   = selection.cpMin;
	long pos_s = selection.cpMin;
	long pos_e = selection.cpMin;

	// Get the text for this line
	CString line;
	long line_len = LineLength();
	long line_s   = LineIndex();
	long line_e   = line_s + line_len;
	GetTextRange(line_s, line_e, line);
	
	// Search back and forward to the ends of the line or to the first whitespace
	TCHAR const* line_str = line.GetString();
	for( ; pos_s > line_s; --pos_s ) { if( !iscsym(line_str[pos_s - line_s - 1]) ) { break; } }
	for( ; pos_e < line_e; ++pos_e ) { if( !iscsym(line_str[pos_e - line_s    ]) ) { break; } }
	
	m_word_partial_pos_s = pos_s;
	m_word_partial_pos_e = pos_e;
	m_word_partial.assign(line_str + pos_s - line_s, pos - pos_s);
	return !m_word_partial.empty();
}

// Set the current word to the one selected in the list box
void CCodeEdit::SetWord(std::string const& word, BOOL final)
{
	// Replace the current selection with the new word
	if( !final )
	{
		std::size_t pos_s = m_word_partial_pos_s + m_word_partial.size();
		SetSel(pos_s, m_word_partial_pos_e);
		ReplaceSel(word.c_str() + m_word_partial.size(), FALSE);
		m_word_partial_pos_e = m_word_partial_pos_s + word.size();
	}
	else
	{
		SetSel(m_word_partial_pos_s, m_word_partial_pos_e);
		ReplaceSel(word.c_str(), TRUE);
		m_word_partial_pos_e = m_word_partial_pos_s + word.size();
		SetSel(m_word_partial_pos_e, m_word_partial_pos_e);
	}
	SetFocus();
}
void CCodeEdit::SetWord(BOOL final)
{
	// Get the word from the list box
	PR_ASSERT(PR_DBG, m_listbox.m_hWnd);
	CString word; m_listbox.GetText(m_listbox.GetCurSel(), word);
	SetWord(word.GetString(), final);
}

BOOL CCodeEdit::PreTranslateMessage(MSG* pMsg) 
{
	if( pMsg->hwnd == m_listbox.m_hWnd && pMsg->message == WM_CHAR ) SetFocus();
	return CRichEditCtrl::PreTranslateMessage(pMsg);
}

BEGIN_MESSAGE_MAP(CCodeEdit, CRichEditCtrl)
	ON_WM_SETFOCUS()
	ON_WM_GETDLGCODE()
	ON_WM_CHAR()
	ON_WM_KEYDOWN()
	ON_LBN_SELCHANGE(IDC_LIST_PR_CODE_EDIT, OnListBoxChanged)
	ON_WM_MOUSEWHEEL()
	ON_WM_LBUTTONDOWN()
	ON_WM_RBUTTONDOWN()
//	ON_WM_LBUTTONDBLCLK()
	ON_WM_KILLFOCUS()
END_MESSAGE_MAP()

void CCodeEdit::OnSetFocus(CWnd* pOldWnd)
{
	// Set tab stops every inch (=1440 twip)
	//CRichEditCtrl::SetSel(-1,-1);
	PARAFORMAT pf;
	pf.cbSize		= sizeof(PARAFORMAT);
	pf.dwMask		= PFM_TABSTOPS;
	pf.cTabCount	= MAX_TAB_STOPS;
	for( int i = 0; i != pf.cTabCount; ++i ) pf.rgxTabs[i] = (i + 1) * 230;
	SetParaFormat(pf);

	return CRichEditCtrl::OnSetFocus(pOldWnd);
}

UINT CCodeEdit::OnGetDlgCode()
{
	return CRichEditCtrl::OnGetDlgCode() | DLGC_WANTTAB;
}

void CCodeEdit::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags) 
{
	bool ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
	bool lb_visible = m_listbox.m_hWnd != 0;
	switch( nChar )
	{
	case ' ':
		if( ctrl )			{ DoAutoComplete(); return; }
		if( lb_visible )	{ SetWord(TRUE); m_listbox.DestroyWindow(); return; }
		break;
	case VK_RETURN:
		if( lb_visible )	{ SetWord(TRUE); m_listbox.DestroyWindow(); return; }
		break;
	case VK_TAB:
		ReplaceSel("\t", TRUE);
		return;
	}

	CRichEditCtrl::OnChar(nChar, nRepCnt, nFlags);

	if( nChar == VK_RETURN )
	{
		// Tab in to the current indent level
	}
}

// Check for virtual keys
void CCodeEdit::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags) 
{
	bool lb_visible = m_listbox.m_hWnd != 0;
	switch( nChar )
	{
	case VK_LEFT:
		if( lb_visible ) { SetWord("", TRUE); m_listbox.DestroyWindow(); return; }
		break;
	case VK_RIGHT:
		if( lb_visible ) { SetWord(TRUE); m_listbox.DestroyWindow(); return; }
		break;
	case VK_UP:
	case VK_DOWN:
	case VK_PRIOR:
	case VK_NEXT:
	case VK_HOME:
	case VK_END:
		if( lb_visible ) { m_listbox.SendMessage(WM_KEYDOWN, nChar, MAKELPARAM(nRepCnt, nFlags)); return; }
		break;
	case VK_TAB:
		if( lb_visible ) { SetWord(TRUE); m_listbox.DestroyWindow(); return; }
		return;
	case VK_ESCAPE:
		if( lb_visible ) { SetWord("", TRUE); m_listbox.DestroyWindow(); return; }
		break;
	}
	CRichEditCtrl::OnKeyDown(nChar, nRepCnt, nFlags);
}

// Handle the selection changing in the list box
void CCodeEdit::OnListBoxChanged()
{
	SetWord(FALSE);
}

BOOL CCodeEdit::OnMouseWheel(UINT nFlags, short zDelta, CPoint pt) 
{
	if( m_listbox.m_hWnd )	return m_listbox.SendMessage(WM_MOUSEWHEEL, MAKEWPARAM(nFlags, zDelta), MAKELPARAM( pt.x, pt.y)) != 0 ? TRUE : FALSE;
	else					return CRichEditCtrl::OnMouseWheel(nFlags, zDelta, pt);
}

void CCodeEdit::OnLButtonDown(UINT nFlags, CPoint point) 
{
	if( m_listbox.m_hWnd ) m_listbox.DestroyWindow();
	CRichEditCtrl::OnLButtonDown(nFlags, point);
}

void CCodeEdit::OnRButtonDown(UINT nFlags, CPoint point) 
{
	if( m_listbox.m_hWnd ) m_listbox.DestroyWindow();
	CRichEditCtrl::OnRButtonDown(nFlags, point);
}

void CCodeEdit::OnKillFocus(CWnd* pNewWnd) 
{
	if( m_listbox.m_hWnd ) m_listbox.DestroyWindow();
	CRichEditCtrl::OnKillFocus(pNewWnd);
}

namespace pr
{
	namespace codeedit
	{
		// Predefined dictionaries
		char const cpp_dictionary[] =
		"char,const,else,if,namespace,return,void";
		std::size_t	cpp_dictionary_size = sizeof(cpp_dictionary) / sizeof(cpp_dictionary[0]);

		char const	lua_dictionary[] =
		"end,function,print";
		std::size_t	lua_dictionary_size = sizeof(lua_dictionary) / sizeof(lua_dictionary[0]);
	}//namespace codeedit
}//namespace pr
