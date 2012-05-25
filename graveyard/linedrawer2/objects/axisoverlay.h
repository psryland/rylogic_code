#ifndef AXISOVERLAY_H
#define AXISOVERLAY_H

#include "pr/maths/maths.h"
#include "LineDrawer/Objects/LdrInstance.h"

class AxisOverlay
{
public:
	void	Create(Renderer& renderer, const Colour32& Xcolour, const Colour32& Ycolour, const Colour32& Zcolour);
	void	SetPositionAndScale(const v4& position, float scale);
	void	SetTransform(const m4x4& txfm);
	void	SetProjection(float width, float height, float Near, float Far, bool righthanded);
	void	Render(rdr::Viewport& viewport);

private:
	LdrInstance m_instance;
};

#endif//AXISOVERLAY_H
