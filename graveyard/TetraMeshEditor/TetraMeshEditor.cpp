//***************************************
// TetraMesh Editor
//***************************************

#include "Stdafx.h"
#include "TetraMeshEditor.h"
#include "pr/common/PRScript.h"

using namespace pr::tetramesh;

// TetraMeshEditor
BEGIN_MESSAGE_MAP(TetraMeshEditor, CWinApp)
END_MESSAGE_MAP()

// This has to be a global otherwise you get asserts in MFC when trying to 'DoModal' dialog boxes
TetraMeshEditor theApp;
TetraMeshEditor& Editor() { return theApp; }
inline bool KeyDown(int vkKey) { return (GetAsyncKeyState(vkKey) & 0x8000) != 0; }

TetraMeshEditor::TetraMeshEditor()
:m_step_return(pr::ldr::EPlugInResult_Continue)
,m_mode(EMode_View)
,m_modified(false)
,m_grid_size(1 << 4)
,m_max_edit_history_size(10)
{}

BOOL TetraMeshEditor::InitInstance()
{
	CWinApp::InitInstance();
	m_editor_dlg.Create(EditorDlg::IDD);
	m_editor_dlg.ShowWindow(SW_SHOW);
	New(true, 1, 1, 1, 1.0f, 1.0f, 1.0f);
	return TRUE;
}

int TetraMeshEditor::ExitInstance()
{
	m_editor_dlg.DestroyWindow();
	return 0;
}

pr::ldr::PlugInSettings	TetraMeshEditor::InitialisePlugin()
{
	pr::ldr::PlugInSettings settings = pr::ldr::DefaultPlugInSettings;
	settings.m_step_rate_hz = 30;
	char const init_src[] = "*GlobalWireframeMode 2";
	ldrSource(init_src, sizeof(init_src), false, false);
	return settings;
}

pr::ldr::EPlugInResult TetraMeshEditor::Step()
{
	return m_step_return;
}

void TetraMeshEditor::Shutdown()
{
	if( !AreYouSure() ) return;
	ldrUnRegisterAllObjects();
	m_step_return = pr::ldr::EPlugInResult_Terminate;
}

bool TetraMeshEditor::AreYouSure()
{
	if( !m_mesh.Empty() && m_modified )
	{
		int res = AfxMessageBox("Save existing model first?", MB_YESNOCANCEL|MB_ICONQUESTION);
		if( res == IDCANCEL ) return false;
		if( res == IDNO ) m_modified = false;
		if( res == IDYES ) m_editor_dlg.OnFileSave();
		if( m_modified ) return false;
	}
	return true;
}

void TetraMeshEditor::New(bool single, int dimX, int dimY, int dimZ, float sizeX, float sizeY, float sizeZ)
{
	if( !AreYouSure() ) return;
	m_editor_dlg.m_filename = "";
	m_selection.Clear();
	m_mesh.New(single, dimX, dimY, dimZ, sizeX, sizeY, sizeZ);

	UpdateWindowText();
	m_mesh.UpdateLdr();
	m_selection.UpdateLdr(m_mesh);
	ldrViewAll();
}

void TetraMeshEditor::Open(char const* filename)
{
	if( !AreYouSure() ) return;
	m_selection.Clear();
	m_mesh.Clear();

	pr::ScriptLoader loader;
	try
	{
		if( Failed(loader.LoadFromFile(filename)) )
			throw script::Exception(script::EResult_LoadSourceFailed);

		std::string keyword;
		while( loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "TetraMesh") )
			{
				loader.FindSectionStart();
				while( loader.GetKeyword(keyword) )
				{
					if( str::EqualNoCase(keyword, "Verts") )
					{
						loader.FindSectionStart();
						while( !loader.IsSectionEnd() )
						{
							v4 vert;
							loader.ExtractVector3(vert, 1.0f);
							m_mesh.PushBack(vert);
						}
						loader.FindSectionEnd();
					}
					else if( str::EqualNoCase(keyword, "Tetra") )
					{
						loader.FindSectionStart();
						while( !loader.IsSectionEnd() )
						{
							int cnrs[NumCnrs], nbrs[NumNbrs];
							loader.ExtractIntArray(cnrs, NumCnrs, 10);
							loader.ExtractIntArray(nbrs, NumNbrs, 10);

							tetramesh::Tetra tetra;
							for( TSize n = 0; n != 4; ++n )
							{
								tetra.m_cnrs[n] = static_cast<tetramesh::VIndex>(cnrs[n]);
								tetra.m_nbrs[n] = static_cast<tetramesh::TIndex>(nbrs[n]);
							}
							m_mesh.PushBack(tetra);
						}
						loader.FindSectionEnd();
					}
				}
				loader.FindSectionEnd();
			}
		}

		if( !Validate(m_mesh) )
			throw script::Exception(script::EResult_Failed, "Invalid tetra mesh");
	}
	catch( script::Exception const& e )
	{
		if( e.m_value == script::EResult_Failed )
		{
			AfxMessageBox(
				FmtS("Model validate error: %s", e.m_message.c_str())
				,MB_ICONEXCLAMATION|MB_OK);
		}
		else
		{
			AfxMessageBox(
				Fmt("Source file error: %s\nNear: '%.20s'"
					,ToString(static_cast<script::EResult>(e.m_value)).c_str()
					,loader.GetSourceStringAt()).c_str()
				,MB_ICONEXCLAMATION|MB_OK);
		}
		m_mesh.Clear();
	}
	UpdateWindowText();
	m_mesh.UpdateLdr();
	m_selection.UpdateLdr(m_mesh);
	ldrViewAll();
}

void TetraMeshEditor::Save(char const* filename)
{
	if( m_mode == EMode_Edit )	CancelEdit();
			
	ScriptSaver saver;
	saver.WriteKeyword("TetraMesh");
	saver.WriteSectionStart();
	{
		saver.WriteKeyword("Verts");
		saver.WriteSectionStart();
		{
			for( v4 const *i = m_mesh.m_verts, *i_end = m_mesh.m_verts + m_mesh.m_num_verts; i != i_end; ++i )
			{
				saver.WriteVector3(*i);
				saver.Newline();
			}
		}
		saver.WriteSectionEnd();
		saver.WriteKeyword("Tetra");
		saver.WriteSectionStart();
		{
			for( Tetra const *i = m_mesh.m_tetra, *i_end = m_mesh.m_tetra + m_mesh.m_num_tetra; i != i_end; ++i )
			{
				saver.WriteInt(i->m_cnrs[0]);
				saver.WriteInt(i->m_cnrs[1]);
				saver.WriteInt(i->m_cnrs[2]);
				saver.WriteInt(i->m_cnrs[3]);

				saver.WriteInt(i->m_nbrs[0]);
				saver.WriteInt(i->m_nbrs[1]);
				saver.WriteInt(i->m_nbrs[2]);
				saver.WriteInt(i->m_nbrs[3]);

				saver.Newline();
			}
		}
		saver.WriteSectionEnd();
	}
	saver.WriteSectionEnd();
	saver.Save(filename);
	m_modified = false;
	UpdateWindowText();
}

void TetraMeshEditor::Undo(int steps)
{
	for( ; steps && !m_edit_history.empty(); --steps, m_edit_history.pop_back() )
	{
		EditData const& ed = m_edit_history.back();
		if( ed.m_type == EEditType_MoveVert )
		{
			PR_ASSERT(PR_DBG_COMMON, ed.m_selection.OneVert());
			m_mesh.m_verts[ed.m_selection.Vert()] = ed.m_base_pos;
		}
	}
	if( !m_edit_history.empty() )
	{
		m_selection = m_edit_history.back().m_selection;
	}

	m_mesh.UpdateLdr();
	m_selection.UpdateLdr(m_mesh);
}

void TetraMeshEditor::UpdateWindowText()
{
	std::string text = "TetraMesh Editor";
	if( !m_editor_dlg.m_filename.empty() ) text += " - " + m_editor_dlg.m_filename;
	if( m_modified ) text += "*";
	ldrSetLDWindowText(text.c_str());
}

void TetraMeshEditor::SetMode(EMode mode)
{
	m_mode = mode;
	if( m_mode == EMode_Edit ) m_modified = true;
	UpdateWindowText();
}

int TetraMeshEditor::GetGridSize() const
{
	return m_grid_size;
}

void TetraMeshEditor::SetGridSize(int grid_size)
{
	m_grid_size = grid_size;

	ClearEditHistory();
	for( v4 *i = m_mesh.m_verts, *i_end = m_mesh.m_verts + m_mesh.m_num_verts; i != i_end; ++i )
	{
		*i = Snap(*i);
	}
	m_mesh.UpdateLdr();
	m_selection.UpdateLdr(m_mesh);
}

void TetraMeshEditor::Select(v2 const& position, bool additive, Selection& selection)
{
	IRect rect		= ldrGetMainClientRect();
	v4 world_point	= ldrScreenToWorld(v4::make(position.x / rect.Width(), position.y / rect.Height(), 1.0f, 1.0f));
	v4 camera_point = ldrGetCameraToWorld().pos;
	if( additive )
	{
		Selection sel;
		sel.Select(m_mesh, camera_point, world_point - camera_point);
		selection.Merge(sel);
	}
	else
	{
		selection.Select(m_mesh, camera_point, world_point - camera_point);
	}
}

void TetraMeshEditor::EnforcePositiveVolume(Selection& selection)
{
	PR_ASSERT(PR_DBG_COMMON, m_selection.OneVert());
	v4& vert = m_mesh.m_verts[m_selection.Vert()];
	v4 shift = v4Zero;
	for( tetramesh::TTIndices::const_iterator t = selection.m_tetra.begin(), t_end = selection.m_tetra.end(); t != t_end; ++t )
	{
		tetramesh::Tetra& tetra	= m_mesh.m_tetra[*t];
		tetramesh::Face	  face	= tetra.OppFaceByVIndex(selection.Vert());
		face.m_plane = plane::Make(m_mesh.m_verts[face.m_i[0]], m_mesh.m_verts[face.m_i[1]], m_mesh.m_verts[face.m_i[2]]);

		// Ensure 'vert' is outside all connected tetras
		float const min_dist = 1.732051f / static_cast<float>(m_grid_size);
		float dist_to_plane = Dot4(face.m_plane, vert);
		if( dist_to_plane >= -min_dist )
		{
			shift += (dist_to_plane + min_dist) * plane::GetDirection(face.m_plane);
		}
	}

	vert = Snap(vert - shift);
	
	#if PR_DBG_COMMON == 1
	for( tetramesh::TTIndices::const_iterator t = selection.m_tetra.begin(), t_end = selection.m_tetra.end(); t != t_end; ++t )
	{
		tetramesh::Face face = m_mesh.m_tetra[*t].OppFaceByVIndex(selection.Vert());
		face.m_plane = plane::Make(m_mesh.m_verts[face.m_i[0]], m_mesh.m_verts[face.m_i[1]], m_mesh.m_verts[face.m_i[2]]);
//		PR_ASSERT(PR_DBG_COMMON, Dot4(face.m_plane, vert) < 0.0f);
	}
	#endif//PR_DBG_COMMON == 1
}

// Enforce position volume over the whole mesh
void TetraMeshEditor::EnforcePositiveVolume(TetraMeshEx& mesh)
{
	bool modified = false;
	do
	{
		for( Tetra *t = mesh.m_tetra, *t_end = mesh.m_tetra + mesh.m_num_tetra; t != t_end; ++t )
		{
			tetramesh::Tetra& tetra	= *t;
			v4 const& a = mesh.m_verts[tetra.m_cnrs[0]];
			v4 const& b = mesh.m_verts[tetra.m_cnrs[1]];
			v4 const& c = mesh.m_verts[tetra.m_cnrs[2]];
			v4 const& d = mesh.m_verts[tetra.m_cnrs[3]];
			if( Dot3(a-b, Cross3(b-c, d-c)) < 0.0f )
			{
				// Move the vert that is least distant from its opposite face
				Distance_PointToPlane(a, b, c, d);
//				Distance_PointToPlane(b, 
			}

			//tetramesh::Face	  face	= tetra.OppFaceByVIndex(selection.Vert());
			//face.m_plane = plane::Make(m_mesh.m_verts[face.m_i[0]], m_mesh.m_verts[face.m_i[1]], m_mesh.m_verts[face.m_i[2]]);

			//// Ensure 'vert' is on the negative side of all connected tetras
			//float const min_dist = 1.4142135f / static_cast<float>(m_grid_size);
			//float dist_to_plane = Dot4(face.m_plane, vert);
			//if( dist_to_plane >= -min_dist )
			//{
			//	vert = Snap(vert - (dist_to_plane + min_dist) * plane::GetDirection(face.m_plane));
			//	PR_ASSERT(PR_DBG_COMMON, Dot4(face.m_plane, vert) < 0.0f);
			//}
		}
	}
	while( modified );
}

inline void TetraMeshEditor::ReplaceNbr(tetramesh::TIndex nbr_idx, tetramesh::TIndex old_idx, tetramesh::TIndex new_idx)
{
	if( nbr_idx == ExtnFace || old_idx == new_idx ) return;

	Tetra& nbr = m_mesh.m_tetra[nbr_idx];
	for( TSize m = 0; m != NumNbrs; ++m )
	{
		if( nbr.m_nbrs[m] != old_idx ) continue;
		nbr.m_nbrs[m] = new_idx;
		break;
	}
}

void TetraMeshEditor::SplitEdge()
{
	PR_ASSERT(PR_DBG_COMMON, m_selection.OneEdge());
	ClearEditHistory();
	tetramesh::VIndex	v_idx	= static_cast<VIndex>(m_mesh.m_num_verts);
	tetramesh::VIndex	v_idx0	= m_selection.Vert0();
	tetramesh::VIndex	v_idx1	= m_selection.Vert1();
	v4 const&			v0		= m_mesh.m_verts[v_idx0];
	v4 const&			v1		= m_mesh.m_verts[v_idx1];
	v4					vert	= Snap((v0 + v1) / 2.0f);
	tetramesh::Face		start	= m_selection.m_faces[0];
	tetramesh::TIndex	nbr0	= ExtnFace;
	tetramesh::TIndex	nbr1	= ExtnFace;
	m_mesh.PushBack(vert);

	// Update the selection as we go
	m_selection.Clear();
	m_selection.m_verts.push_back(v_idx);

	do
	{
		tetramesh::TIndex	t_idx0 = start.m_tetra0;
		tetramesh::TIndex	t_idx1 = static_cast<TIndex>(m_mesh.m_num_tetra);
		m_mesh.PushBack(m_mesh.m_tetra[t_idx0]);
		Tetra&				tetra0 = m_mesh.m_tetra[t_idx0];
		Tetra&				tetra1 = m_mesh.m_tetra[t_idx1];
		tetramesh::VIndex	opp_v  = tetra0.OppVIndex(start);
		for( TSize n = 0; n != NumCnrs; ++n )
		{
			if( tetra0.m_cnrs[n] == v_idx1 )
			{
				tetra0.m_cnrs[n] = v_idx;
				ReplaceNbr(tetra1.m_nbrs[n], t_idx0, t_idx0);
				tetra1.m_nbrs[n] = t_idx0;
			}
			else if( tetra0.m_cnrs[n] == v_idx0 )
			{
				tetra1.m_cnrs[n] = v_idx;
				ReplaceNbr(tetra0.m_nbrs[n], t_idx0, t_idx1);
				tetra0.m_nbrs[n] = t_idx1;
			}
			else if( tetra0.m_cnrs[n] == opp_v  )
			{
				tetra0.m_nbrs[n] = nbr0;
				tetra1.m_nbrs[n] = nbr1;
				ReplaceNbr(tetra0.m_nbrs[n], t_idx0, t_idx0);
				ReplaceNbr(tetra1.m_nbrs[n], t_idx0, t_idx1);
			}
			else
			{
				start.m_tetra0	= tetra0.m_nbrs[n];	// This is the next tetra to split
				start.m_i[0]	= v_idx0;
				start.m_i[1]	= v_idx1;
				start.m_i[2]	= opp_v;
			}
		}
		nbr0 = t_idx0;
		nbr1 = t_idx1;

		m_selection.m_tetra.push_back(t_idx0);
		m_selection.m_tetra.push_back(t_idx1);
	}
	while( start.m_tetra0 != ExtnFace );

	// Select any external faces connected to the v_idx
	tetramesh::TIndex t_idx = 0;
	for( tetramesh::TTIndices::const_iterator t = m_selection.m_tetra.begin(), t_end = m_selection.m_tetra.end(); t != t_end; ++t, ++t_idx )
	{
		Tetra const& tetra = m_mesh.m_tetra[*t];
		for( int i = 0; i != 4; ++i )
		{
			if( tetra.m_nbrs[i] != ExtnFace ) continue;
			
			tetramesh::Face face;				
			face.m_i[0] = tetra.m_cnrs[FaceIndex[i][0]];
			face.m_i[1] = tetra.m_cnrs[FaceIndex[i][1]];
			face.m_i[2] = tetra.m_cnrs[FaceIndex[i][2]];
			if( face.m_i[0] == v_idx || face.m_i[1] == v_idx || face.m_i[2] == v_idx )
			{
				face.m_tetra0 = t_idx;
				face.m_order  = GetFaceIndexOrder(face);
				m_selection.m_faces.push_back(face);
			}
		}
	}

	EnforcePositiveVolume(m_selection);
	PR_ASSERT(PR_DBG_COMMON, Validate(m_mesh));

	m_selection.UpdateLdr(m_mesh);
	m_mesh.UpdateLdr();
}

void TetraMeshEditor::SplitFace()
{
	PR_ASSERT(PR_DBG_COMMON, m_selection.OneFace());
	ClearEditHistory();
	tetramesh::Face		face	= m_selection.Face();
	tetramesh::VIndex	v_idx	= static_cast<VIndex>(m_mesh.m_num_verts);
	tetramesh::TIndex	t_idx	= static_cast<TIndex>(m_mesh.m_num_tetra);
	tetramesh::Tetra&	tetra	= m_mesh.m_tetra[face.m_tetra0];
	v4 const&			a		= m_mesh.m_verts[face.m_i[0]];
	v4 const&			b		= m_mesh.m_verts[face.m_i[1]];
	v4 const&			c		= m_mesh.m_verts[face.m_i[2]];
	v4					vert	= Snap((a + b + c) / 3.0f);

	{// Update the nbr for the tetra with this face
		TSize n;
		VIndex i = tetra.OppVIndex(face);
		for( n = 0; n != NumNbrs; ++n )
		{
			if( tetra.m_cnrs[n] != i ) continue;
			PR_ASSERT(PR_DBG_COMMON, tetra.m_nbrs[n] == ExtnFace);
			tetra.m_nbrs[n] = t_idx;
			break;
		}
		PR_ASSERT(PR_DBG_COMMON, n != NumNbrs);
	}

	// Add the new tetra to the mesh
	tetramesh::Tetra new_tetra;
	new_tetra.m_cnrs[0] = v_idx;
	new_tetra.m_cnrs[1] = face.m_i[0];
	new_tetra.m_cnrs[2] = face.m_i[1];
	new_tetra.m_cnrs[3] = face.m_i[2];
	new_tetra.m_nbrs[0] = face.m_tetra0;
	new_tetra.m_nbrs[1] = ExtnFace;
	new_tetra.m_nbrs[2] = ExtnFace;
	new_tetra.m_nbrs[3] = ExtnFace;
	m_mesh.PushBack(vert);
	m_mesh.PushBack(new_tetra);

	// Update the selection
	m_selection.Clear();
	m_selection.m_verts.push_back(v_idx);
	m_selection.m_tetra.push_back(t_idx);
	for( int i = 0; i != 3; ++i )
	{
		tetramesh::Face sel_face;
		sel_face.m_tetra0	= t_idx;
		sel_face.m_i[0]		= v_idx;
		sel_face.m_i[1]		= face.m_i[ i     ];
		sel_face.m_i[2]		= face.m_i[(i+1)%3];
		sel_face.m_order	= GetFaceIndexOrder(sel_face);
		m_selection.m_faces.push_back(sel_face);
	}
	EnforcePositiveVolume(m_selection);
	PR_ASSERT(PR_DBG_COMMON, Validate(m_mesh));

	m_selection.UpdateLdr(m_mesh);
	m_mesh.UpdateLdr();
}

void TetraMeshEditor::WeldVerts()
{
	PR_ERROR_STR(PR_DBG_COMMON, "TODO");
}

void TetraMeshEditor::DeleteSelection()
{
	PR_ASSERT(PR_DBG_COMMON, !m_selection.Empty());
	ClearEditHistory();

	PR_ERROR(1);// This needs implementing
	//for( TTIndices::const_iterator i = m_selection.m_tetra.begin(), i_end = m_selection.m_tetra.end(); i != i_end; ++i )
	//{
	//	Tetra& tetra = m_mesh.m_tetra[*i];
	//	for( TSize n = 0; n != NumNbrs; ++n )
	//	{
	//		if( tetra.m_nbrs[n] == ExtnFace ) continue;
	//		Tetra& nbr = m_mesh.m_tetra[tetra.m_nbrs[n]];
	//		for( TSize m = 0; m != NumNbrs; ++m )
	//		{
	//			if( nbr.m_nbrs[m] != *i ) continue;
	//			nbr.m_nbrs[m] = ExtnFace;
	//			break;
	//		}
	//	}
	//}

	for( TVIndices::const_iterator i = m_selection.m_verts.begin(), i_end = m_selection.m_verts.end(); i != i_end; ++i )
	{
//		m_mesh.m_verts.erase();
	}
}

void TetraMeshEditor::ClearEditHistory()
{
	m_edit_history.clear();
}

void TetraMeshEditor::CancelEdit()
{
	SetMode(EMode_View);
	Undo(1);
}

pr::ldr::EPlugInResult TetraMeshEditor::HandleKeys(UINT nChar, UINT, UINT, bool)
{
	if( !m_selection.Empty() )
	{
		switch( nChar )
		{
		case VK_ESCAPE:
			if( m_mode == EMode_Edit )	CancelEdit();
			m_selection.Clear();
			m_selection.UpdateLdr(m_mesh);
			return EPlugInResult_Handled;
		case VK_DELETE:
			DeleteSelection();
			return EPlugInResult_Handled;
		}
	}
	PR_INFO(PR_DBG_COMMON, FmtS("%d pressed\n", nChar));
	return EPlugInResult_NotHandled;
}

pr::ldr::EPlugInResult TetraMeshEditor::MouseClk(unsigned int button, v2 position)
{
	if( button == VK_LBUTTON )
	{
		Select(position, false/*KeyDown(VK_SHIFT)*/, m_selection);
		m_selection.UpdateLdr(m_mesh);
		if( !m_selection.Empty() )
		{
			return EPlugInResult_Handled;
		}
	}
	return EPlugInResult_NotHandled;
}
pr::ldr::EPlugInResult TetraMeshEditor::MouseDblClk(unsigned int button, v2)
{
	if( button == VK_LBUTTON && !m_selection.Empty() )
	{
		if( m_selection.OneFace() )		{ SplitFace(); return EPlugInResult_Handled; }
		if( m_selection.OneEdge() )		{ SplitEdge(); return EPlugInResult_Handled; }
	}
	return EPlugInResult_NotHandled;
}
pr::ldr::EPlugInResult TetraMeshEditor::MouseDown(unsigned int button, pr::v2 position)
{
	if( button != VK_LBUTTON || !KeyDown(VK_SHIFT) || m_selection.Empty() || !m_selection.OneVert() )
		return EPlugInResult_NotHandled;

	SetMode(EMode_Edit);
	while( m_edit_history.size() > m_max_edit_history_size ) m_edit_history.pop_front();
	EditData& ed		= (m_edit_history.push_back(EditData()), m_edit_history.back());
	ed.m_type			= EEditType_MoveVert;
	ed.m_selection		= m_selection;
	ed.m_selection.m_ldr= 0;
	ed.m_base_pos		= m_mesh.m_verts[ed.m_selection.Vert()];
	ed.m_mouse_base_pos	= position;
	return EPlugInResult_Handled;
}
pr::ldr::EPlugInResult TetraMeshEditor::MouseMove(pr::v2 position)
{
	if( m_mode != EMode_Edit ) return EPlugInResult_NotHandled;

	EditData&	ed	= m_edit_history.back();
	CameraData	cd  = ldrGetCameraData();
	IRect client_area = ldrGetMainClientRect();
	
	v2 vec = position - ed.m_mouse_base_pos;
	float scalex = cd.m_width  / static_cast<float>(client_area.Width());
	float scaley = cd.m_height / static_cast<float>(client_area.Height());
	if( cd.m_is_3d )
	{
		scalex *= cd.m_focus_dist / cd.m_near;
		scaley *= cd.m_focus_dist / cd.m_near;
	}
	
	v4 shift = ldrGetCameraToWorld() * v4::make(vec.x * scalex, -vec.y * scaley, 0.0f, 0.0f);
	m_mesh.m_verts[m_selection.Vert()] = Snap(ed.m_base_pos + shift);
	EnforcePositiveVolume(m_selection);

	m_selection.UpdateLdr(m_mesh);
	return EPlugInResult_Handled;
}
pr::ldr::EPlugInResult TetraMeshEditor::MouseUp(unsigned int button, pr::v2 position)
{
	if( button != VK_LBUTTON || m_mode != EMode_Edit ) return EPlugInResult_NotHandled;

	MouseMove(position);
	SetMode(EMode_View);
	m_mesh.UpdateLdr();
	return EPlugInResult_Handled;
}

