//*****************************************
//*****************************************
#include "test.h"
#include "pr/storage/xml.h"

namespace TestXml
{
	void Run()
	{
		pr::xml::Node settings;
		pr::xml::Load("I:/FibreMeasurement/FibreScan/bin/Debug/FibreTestSettings.xml", settings);
		
		settings;
	}
}
