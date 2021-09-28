
// MiniSample Display.h : main header file for the PROJECT_NAME application
//


#include <vector>
#include "src\ds_commonwin.h"
#pragma once

#ifndef __AFXWIN_H__
#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "sv_gen_sdk.h"
#include "resource.h"		// main symbols
#include "Display.h"
#include "CameraContainer.h"

// CMiniSampleDisplayApp:
// See MiniSample Display.cpp for the implementation of this class
//

class CMiniSampleDisplayApp : public CWinApp
{
public:
    CMiniSampleDisplayApp();

    // Overrides
public:
    virtual BOOL InitInstance();


    // Implementation

    DECLARE_MESSAGE_MAP()
};

extern CMiniSampleDisplayApp theApp;
