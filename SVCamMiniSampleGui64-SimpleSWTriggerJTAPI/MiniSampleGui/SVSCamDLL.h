// SVSCamDLL.h: interface for the SVSCamDLL class.
//
//////////////////////////////////////////////////////////////////////
#include "sv_gen_sdk.h"
#include <vector>

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
	virtual int Open();
	virtual void Close();
	virtual BYTE* GetImageData();
	virtual void SoftTrigger();
	virtual BOOL WaitTrigerFinish();
	virtual void Display(CDC *pDC);
	virtual void SetTrigerSource(int nTrigerSource=0);
	virtual int GetTrigerSource();
	virtual void TriggerPrepare();

public:
	void OnSVSCamMessage(WPARAM wParam, LPARAM lParam);
	void GrabImage();
	static UINT ImageProcessThread(LPVOID pParam);
	void TerminateThread();

public:


private:




};

#endif // !defined(AFX_SVSCAMDLL_H__059A4FE6_5E1B_496A_A70A_D75B20C626F1__INCLUDED_)
