//***************************************
// TetraMesh Editor
//***************************************
#pragma once
#include "TetraMeshEditor/Res/Resource.h"
#include "TetraMeshEditor/Forwards.h"
#include "LineDrawer/Plugin/PluginInterface.h"
#include "TetraMeshEditor/EditorDlg.h"
#include "TetraMeshEditor/TetraMeshEx.h"
#include "TetraMeshEditor/Selection.h"
#include "TetraMeshEditor/EditData.h"

// CPhysicsTestbedApp
class TetraMeshEditor : public CWinApp
{
public:
	enum EMode { EMode_View, EMode_Edit };

	TetraMeshEditor();

	pr::ldr::PlugInSettings	InitialisePlugin();
	pr::ldr::EPlugInResult	Step();
	void					Shutdown();
	void					New(bool single, int dimX, int dimY, int dimZ, float sizeX, float sizeY, float sizeZ);
	void					Open(char const* filename);
	void					Save(char const* filename);
	void					Undo(int steps);
	bool					AreYouSure();
	v4						Snap(v4 const& pos) const	{ return pr::Quantise(pos, m_grid_size); }
	void					UpdateWindowText();
	void					SetMode(EMode mode);
	int						GetGridSize() const;
	void					SetGridSize(int grid_size);
	void					Select(v2 const& position, bool additive, Selection& selection);
	void					EnforcePositiveVolume(Selection& selection);
	void					EnforcePositiveVolume(TetraMeshEx& mesh);
	void					ReplaceNbr(tetramesh::TIndex nbr_idx, tetramesh::TIndex old_idx, tetramesh::TIndex new_idx);
	void					SplitEdge();
	void					SplitFace();
	void					WeldVerts();
	void					DeleteSelection();
	void					ClearEditHistory();
	void					CancelEdit();

	pr::ldr::EPlugInResult	HandleKeys	(UINT nChar, UINT nRepCnt, UINT nFlags, bool down);
	pr::ldr::EPlugInResult	MouseClk	(unsigned int button, v2 position);
	pr::ldr::EPlugInResult	MouseDblClk	(unsigned int button, v2 position);
	pr::ldr::EPlugInResult 	MouseDown	(unsigned int button, pr::v2 position);
	pr::ldr::EPlugInResult 	MouseMove	(pr::v2 position);
	pr::ldr::EPlugInResult 	MouseUp		(unsigned int button, pr::v2 position);
	
	virtual BOOL			InitInstance();
	virtual int				ExitInstance();
	DECLARE_MESSAGE_MAP()

private:
	pr::ldr::EPlugInResult	m_step_return;
	EditorDlg				m_editor_dlg;
	TetraMeshEx				m_mesh;
	EMode					m_mode;
	bool					m_modified;
	int						m_grid_size;
	Selection				m_selection;
	TEditHistory			m_edit_history;
	std::size_t				m_max_edit_history_size;
};

TetraMeshEditor& Editor();
void CreateEditor();
void DeleteEditor();