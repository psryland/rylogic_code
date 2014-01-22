#include <string>

namespace jinhaox
{
    namespace string
    {
		std::string transform(std::string str, const char* from, const char* to)
		{
			if(strcmp(from, to) == 0)
				return str;
			    
			size_t from_len = strlen(from);
			    
			std::string::size_type pos = str.find(from);
			while(pos != std::string::npos)
				pos = str.replace(pos, from_len, to).find(from, pos);
			return str;
		}

		std::string file_root(const std::string& filename)
		{
			const char* str = filename.c_str();
			size_t	i = filename.length() - 1;
			
			for(; i > 2; --i)
			{
				char c = str[i];
				if(c != '\\' && c != '/')
					break;
			}

			for(; i> 2; --i)
			{
				char c = str[i];
				if(c == '\\' || c =='/')
					break;
			}
			return std::string(str, i + 1);
		}
    }
}
