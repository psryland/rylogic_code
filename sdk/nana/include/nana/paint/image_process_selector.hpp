#ifndef NANA_PAINT_IMAGE_PROCESS_SELECTOR_HPP
#define NANA_PAINT_IMAGE_PROCESS_SELECTOR_HPP
#include "detail/image_process_provider.hpp"

namespace nana
{
	namespace paint
	{
		namespace image_process
		{
			class selector
			{
			public:
				//stretch
				void stretch(const std::string& name);

				template<typename ImageProcessor>
				void add_stretch(const std::string& name)
				{
					detail::image_process_provider & p = detail::image_process_provider::instance();
					p.add<ImageProcessor>(p.ref_stretch_tag(), name);
				}

				//alpha_blend
				void alpha_blend(const std::string& name);
				
				template<typename ImageProcessor>
				void add_alpha_blend(const std::string& name)
				{
					detail::image_process_provider& p = detail::image_process_provider::instance();
					p.add<ImageProcessor>(p.ref_alpha_blend_tag(), name);
				}

				//blend
				void blend(const std::string& name);

				template<typename ImageProcessor>
				void add_blend(const std::string& name)
				{
					detail::image_process_provider& p = detail::image_process_provider::instance();
					p.add<ImageProcessor>(p.ref_blend_tag(), name);
				}

				//line
				void line(const std::string& name);

				template<typename ImageProcessor>
				void add_line(const std::string& name)
				{
					detail::image_process_provider & p = detail::image_process_provider::instance();
					p.add<ImageProcessor>(p.ref_line_tag(), name);
				}
				
				//blur
				void blur(const std::string& name);
				template<typename ImageProcessor>
				void add_blur(const std::string& name)
				{
					detail::image_process_provider & p = detail::image_process_provider::instance();
					p.add<ImageProcessor>(p.ref_blur_tag(), name);
				}
			};
		}
	}
}

#endif
