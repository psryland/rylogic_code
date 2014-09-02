//*********************************************
// Code Edit MFC
//	(C)opyright Rylogic Limited 2007
//*********************************************

#ifndef PR_MFC_CODE_EDIT_H
#define PR_MFC_CODE_EDIT_H

#include <afxwin.h>         // MFC core and standard components
#include <afxcmn.h>			// MFC support for Windows Common Controls
#include "pr/common/StdVector.h"
#include "pr/common/StdAlgorithm.h"

#ifndef IDC_LIST_PR_CODE_EDIT
	#define IDC_LIST_PR_CODE_EDIT	101
#endif//IDC_LIST_CODE_EDIT

namespace pr
{
	typedef std::vector<std::string> TDictionary;

	namespace codeedit
	{
		// Predefined dictionaries
		extern char const	cpp_dictionary[];
		extern std::size_t	cpp_dictionary_size;
		extern char const	lua_dictionary[];
		extern std::size_t	lua_dictionary_size;
	}

	// Adding code editor behaviour to an edit control
	class CCodeEdit : public CRichEditCtrl
	{
	public:
		CCodeEdit();
		~CCodeEdit();

		// Auto complete
		void AddToDictionary(TDictionary const& dict);
		void AddToDictionary(std::string const& word);
		void AddToDictionary(char const* words, std::size_t words_length);	// comma separated words

	protected:
		void	SetSel(std::size_t start, std::size_t end);
		bool	GetCurrentWord();
		void	DoAutoComplete();
		void	SetWord(std::string const& word, BOOL final);
		void	SetWord(BOOL final);

		BOOL	PreTranslateMessage(MSG* pMsg);
		
		DECLARE_MESSAGE_MAP()
		afx_msg void OnSetFocus(CWnd* pOldWnd);
		afx_msg UINT OnGetDlgCode();
		afx_msg void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);
		afx_msg void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
		afx_msg void OnListBoxChanged();
		afx_msg BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint pt);
		afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
		afx_msg void OnRButtonDown(UINT nFlags, CPoint point);
		afx_msg void OnKillFocus(CWnd* pNewWnd);
		//afx_msg void OnLButtonDblClk(UINT nFlags, CPoint point);
		
	protected:
		typedef std::pair<TDictionary::const_iterator, TDictionary::const_iterator> TDictRange;

		TDictionary		m_dictionary;
		CListBox		m_listbox;
		std::string		m_word_partial;
		std::size_t		m_word_partial_pos_s;
		std::size_t		m_word_partial_pos_e;
	};
}//namespace pr

#endif//PR_MFC_CODE_EDIT_H
