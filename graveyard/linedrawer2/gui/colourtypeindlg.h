#ifndef COLOUR_TYPEIN_H
#define COLOUR_TYPEIN_H

#include "LineDrawer/Resource.h"
#include "pr/geometry/colour.h"

class ColourTypein : public CDialog
{
public:
	ColourTypein(CWnd* pParent);
	virtual ~ColourTypein();

	void SetColour32(Colour32 col);
	pr::Colour32 GetColour32() const;

	// Dialog Data
	enum { IDD = IDD_COLOUR_TYPEIN };
	Colour m_colour;

	DECLARE_DYNAMIC(ColourTypein)
	afx_msg void OnPaint();
	afx_msg void OnEnChangeColourTypein();

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()
};

#endif//COLOUR_TYPEIN_H