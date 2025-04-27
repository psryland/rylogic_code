#include <iostream>
#include <eigen/Eigen/Core>
#include "src/fbx.h"
#include "pr/win32/win32.h"

//template <int i> class C;
//C<__cplusplus> c;
using namespace pr;

int main()
{
	win32::LoadDll<struct FbxSdk>("libfbxsdk.dll");

	fbx::Manager fbx;
	fbx::Settings settings(fbx);
	fbx::Importer imp(fbx, settings);
	auto scene = imp.Import("C:\\Games\\Epic\\UE_5.5\\Engine\\Content\\FbxEditorAutomation\\AnimatedCharacter.fbx");
	WriteContent(scene, std::cout);
	return 0;
}
