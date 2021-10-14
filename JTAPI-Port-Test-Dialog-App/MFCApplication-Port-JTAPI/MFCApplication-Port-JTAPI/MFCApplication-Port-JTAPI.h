
// MFCApplication-Port-JTAPI.h: PROJECT_NAME 应用程序的主头文件
//


#include <vector>
#include "src\ds_commonwin.h"
#pragma once

#ifndef __AFXWIN_H__
	#error "在包含此文件之前包含 'pch.h' 以生成 PCH"
#endif

#include "sv_gen_sdk.h"
#include "resource.h"		// main symbols
//#include "Display.h"
#include "CameraContainer.h"


//#include "resource.h"		// 主符号


// CMFCApplicationPortJTAPIApp:
// 有关此类的实现，请参阅 MFCApplication-Port-JTAPI.cpp
//

class CMFCApplicationPortJTAPIApp : public CWinApp
{
public:
	CMFCApplicationPortJTAPIApp();

// 重写
public:
	virtual BOOL InitInstance();

// 实现

	DECLARE_MESSAGE_MAP()
};

extern CMFCApplicationPortJTAPIApp theApp;
