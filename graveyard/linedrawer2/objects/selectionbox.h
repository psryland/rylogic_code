#ifndef SELECTIONBOX_H
#define SELECTIONBOX_H

#include "pr/maths/maths.h"
#include "LineDrawer/Objects/LdrInstance.h"

class SelectionBox
{
public:
	void	Create(Renderer& renderer);
	void	SetSelection(const BoundingBox& bbox);
	void	Render(rdr::Viewport& viewport);

private:
	LdrInstance	m_instance;
};

#endif//SELECTIONBOX_H