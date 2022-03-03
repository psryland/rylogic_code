//*******************************
// Shape Generator Params
//*******************************
#pragma once
#include "PhysicsTestbed/Res/Resource.h"
#include "PhysicsTestbed/ShapeGenParams.h"

// CShapeGenParamsDlg dialog
class CShapeGenParamsDlg : public CDialog
{
	DECLARE_DYNAMIC(CShapeGenParamsDlg)
public:
	enum { IDD = IDD_DIALOG_SHAPE_GEN };
	CShapeGenParamsDlg(CWnd* pParent = 0);
	virtual ~CShapeGenParamsDlg();

	ShapeGenParams m_params;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	DECLARE_MESSAGE_MAP()
};
