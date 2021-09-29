// SVSCamDLL.h: interface for the SVSCamDLL class.
//
//////////////////////////////////////////////////////////////////////
#include "sv_gen_sdk.h"
#include <vector>

#define _CRT_SECURE_NO_WARNINGS
#include <stdlib.h>
#include <crtdbg.h>


#include "Cameracontainer.h"

#if !defined(AFX_SVSCAMDLL_H__059A4FE6_5E1B_496A_A70A_D75B20C626F1__INCLUDED_)
#define AFX_SVSCAMDLL_H__059A4FE6_5E1B_496A_A70A_D75B20C626F1__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000


using namespace std;

class SVSCamDLL  
{
public:

	SVSCamDLL(); 
    virtual ~SVSCamDLL();

public:

    // Open SVS Camera
    virtual int Open();
    // Close SVS Camera
    virtual void Close();

	// Emit Software 
	virtual void SoftTrigger(); 
	virtual BOOL WaitTrigerFinish();
	virtual BYTE* GetImageData();
	// Integrated Example: SW Trigger --> WaitFor --> Get Its Image
    virtual void SoftTriggerAndSavePhoto();

	//virtual void Display(CDC *pDC);
	//virtual void SetTrigerSource(int nTrigerSource=0);
	//virtual int GetTrigerSource();
	//virtual void TriggerPrepare();


public:
	//void OnSVSCamMessage(WPARAM wParam, LPARAM lParam);
	//void GrabImage();
	//static UINT ImageProcessThread(LPVOID pParam);
	//void TerminateThread();

public:


private:
    bool sdk_init_done;

	// Local copy of photo buffer from the background grab thread
    SV_BUFFER_INFO m_newBuffer= { 0 };

    Camera   *currentCam;
    CameraContainer *cam_container;

	// Background grab thread related
    bool acqTerminated;
    bool isStoping; 
    HANDLE m_thread;
    HANDLE m_acquisitionstopThread; // event to flag whether grab thread has been terminated

public:
    int startAcqThread(); // grab thread in backgound


};


#endif // !defined(AFX_SVSCAMDLL_H__059A4FE6_5E1B_496A_A70A_D75B20C626F1__INCLUDED_)
