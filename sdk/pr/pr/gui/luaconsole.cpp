//*********************************************
// Lua Console MFC
//	(C)opyright Rylogic Limited 2007
//*********************************************
#pragma warning (push)
#pragma warning (disable: 4355) // 'this' used in initialiser list

#include "LuaConsole.h"

using namespace pr;

// Global forwarding functions ***************************************************
int LuaConsolePrint(lua_State* lua_state)	{ pr::lua::Lua lua(lua_state); return lua::GetUserPointer<pr::LuaConsole>(lua, "pr::LuaConsole")->LuaPrint(lua_state); }

// LuaConsole dialog ***************************************************
IMPLEMENT_DYNAMIC(LuaConsole, CDialog)
LuaConsole::LuaConsole(pr::lua::Lua& lua, CWnd* pParent)
:CDialog(LuaConsole::IDD, pParent)
,m_lua(&lua)
,m_input(this)
{}

LuaConsole::~LuaConsole()
{}

void LuaConsole::Create(CWnd* parent)
{
	AfxInitRichEdit2();
	CDialog::Create(IDD, parent);

	//	LoadImage(0, 
	//HICON icon = LoadIcon(0, MAKEINTRESOURCE(IDI_ICON_LUA));
	//SetIcon(icon, TRUE);
	//SetIcon(icon, FALSE);
}

// Initialise the console dialog
BOOL LuaConsole::OnInitDialog()
{
	CDialog::OnInitDialog();

	SplitterCtrl::Settings settings;
	settings.m_type		= SplitterCtrl::Horizontal;
	settings.m_parent	= this;
	settings.m_side1	= GetDlgItem(IDC_RICHEDIT_LUA_CONSOLE_OUTPUT);
	settings.m_side2	= GetDlgItem(IDC_RICHEDIT_LUA_CONSOLE_INPUT);
	m_splitter.Initialise(settings);
	m_splitter.SetSplitFraction(0.8f);
	
	m_output.SetBackgroundColor(FALSE, 0x00C0C0C0);
	m_input.SetFocus();
	m_input.AddToDictionary(codeedit::lua_dictionary, codeedit::lua_dictionary_size);

	PostMessage(WM_SIZE);

	lua::AddUserPointer(*m_lua, "pr::LuaConsole", this);
	lua::Register(*m_lua, "print", LuaConsolePrint);
	return TRUE;
}

void LuaConsole::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_RICHEDIT_LUA_CONSOLE_OUTPUT, m_output);
	DDX_Control(pDX, IDC_RICHEDIT_LUA_CONSOLE_INPUT , m_input);
	DDX_Control(pDX, IDC_PRSPLITTER_LUA_CONSOLE,	  m_splitter);
}

BEGIN_MESSAGE_MAP(LuaConsole, CDialog)
	ON_WM_GETMINMAXINFO()
	ON_WM_SIZE()
END_MESSAGE_MAP()

// Define the limits for resizing
void LuaConsole::OnGetMinMaxInfo(MINMAXINFO* lpMMI)
{
	lpMMI->ptMinTrackSize.x = 50;
	lpMMI->ptMinTrackSize.y = 50;
	CDialog::OnGetMinMaxInfo(lpMMI);
}

// Resize the DataManagerGUI window
void LuaConsole::OnSize(UINT nType, int cx, int cy)
{
	CDialog::OnSize(nType, cx, cy);
	if( nType == SIZE_MINIMIZED ) return;

	// Get the new window size
	CRect rect, wndrect;
	GetClientRect(&rect);

	float split_fraction = m_splitter.GetSplitFraction();

	// Move the output edit box
	CWnd* wnd;
	wndrect	= rect;
	wndrect.bottom = rect.bottom/2 - 2;
	wnd = GetDlgItem(IDC_RICHEDIT_LUA_CONSOLE_OUTPUT);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the input edit box
	wndrect	= rect;
	wndrect.top = rect.bottom/2 + 2;
	wnd = GetDlgItem(IDC_RICHEDIT_LUA_CONSOLE_INPUT);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'splitter bar'
	wndrect = rect;
	wndrect.top = rect.bottom/2 - 2;
	wndrect.bottom = rect.bottom/2 + 2;
	wnd = GetDlgItem(IDC_PRSPLITTER_LUA_CONSOLE);
	if( wnd ) wnd->MoveWindow(wndrect);

	m_splitter.ResetMinMaxRange();
	m_splitter.SetSplitFraction(split_fraction);

	Invalidate();
}

// Executes a line of lua.
void LuaConsole::DoString(CString const& str, CString& syntax_error_msg)
{
	std::string err_msg;
	pr::lua::StepConsole(*m_lua, str.GetString(), err_msg);
	syntax_error_msg = err_msg.c_str();
}

// Lua registered functions
int LuaConsole::LuaPrint(lua_State* lua_state)
{
	std::string str = lua::ToString(lua_state, -1);
	
	long s, e;
	m_output.LineScroll(-m_output.GetFirstVisibleLine());
	m_output.SetSel(0, -1);
	m_output.GetSel(s, e);
	m_output.SetSel(e, e);
	m_output.ReplaceSel(str.c_str());
	return 1;
}

// CLuaInputEdit ***************************************************
CLuaInputEdit::CLuaInputEdit(LuaConsole* parent)
:CCodeEdit()
,m_parent(parent)
{}		

BEGIN_MESSAGE_MAP(CLuaInputEdit, CCodeEdit)
	ON_WM_KEYDOWN()
END_MESSAGE_MAP()

void CLuaInputEdit::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	bool ctrl = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
	bool rtn  = (GetKeyState(VK_RETURN ) & 0x8000) != 0;
	if( ctrl && rtn )
	{
		CString str, err_msg;
		GetWindowText(str);
		m_parent->DoString(str, err_msg);
		return;
	}
	CCodeEdit::OnKeyDown(nChar, nRepCnt, nFlags);
}

#pragma warning (pop)