#ifndef ASTERIX_H
#define ASTERIX_H

#include "pr/maths/maths.h"
#include "LineDrawer/Objects/LdrInstance.h"

class Asterix
{
public:
	void	Create(Renderer& renderer, const Colour32& Xcolour, const Colour32& Ycolour, const Colour32& Zcolour);
	void	SetPositionAndScale(const v4& position, float scale);
	void	Render(rdr::Viewport& viewport);

private:
	LdrInstance m_instance;
};

#endif//ASTERIX_H
