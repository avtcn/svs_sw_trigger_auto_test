// SVSCamDLL.cpp: implementation of the SVSCamDLL class.
//
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "SVSCamDLL.h"
#include <string> 

using   namespace   std;

#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[]=__FILE__;
#define new DEBUG_NEW
#endif

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

SVSCamDLL::SVSCamDLL()
{


}


SVSCamDLL::~SVSCamDLL()
{

}

int SVSCamDLL::Open()
{

    return 0;
}

void SVSCamDLL::Close()
{
}


BYTE* SVSCamDLL::GetImageData()
{

	return NULL;
}


void SVSCamDLL::SoftTrigger()
{
	
	return ;
}

BOOL SVSCamDLL::WaitTrigerFinish()
{
	
	return true;
}



void SVSCamDLL::Display(CDC *pDC)
{

}



void SVSCamDLL::SetTrigerSource(int nTrigerSource)
{

}

int SVSCamDLL::GetTrigerSource()
{

    return 0;
}

void SVSCamDLL::TriggerPrepare()
{

}


// 采集完成，发送消息
void SVSCamDLL::OnSVSCamMessage(WPARAM wParam, LPARAM lParam)
{

}



UINT SVSCamDLL::ImageProcessThread(LPVOID pParam)
{
    /*
    SVSCamDLL* pThread = NULL;

    while (!bExit)
    {
        dwStatus = WaitForMultipleObjects(2, hWait, FALSE, 1000);	//wlx20130923
        {
            if (dwStatus == WAIT_OBJECT_0)
            {
                pThread->m_bWaitGrabImage = TRUE;
                pThread->GrabImage();
                pThread->m_bWaitGrabImage = FALSE;
            }
            else if (dwStatus == WAIT_OBJECT_0 + 1)
            {
                bExit = TRUE;
            }
        }
    }
    */ 
    return 0;
}


void SVSCamDLL::GrabImage()
{
		
/*
    SV_BUFFER_HANDLE hBuffer = NULL;
    SV_RETURN ret = SVStreamWaitForNewBuffer(hDS, NULL, &hBuffer, 1000);
    ret = SVStreamBufferGetInfo(hDS, hBuffer, m_bufferInfo);

    string  filename = "1.png";
    SV_RETURN  ret = SVUtilSaveImageToPNGFile(*m_bufferInfo, filename.c_str());

    SVUtilBufferBayerToRGB(m_bufferInfo, m_pImage, m_bufferInfo->iImageSize * 3);

    SVStreamQueueBuffer(hDS, hBuffer);

    m_bImageReady = TRUE;
    OnSVSCamMessage(IS_FRAME, ::GetTickCount() - timeStart); 
    //OnSVSCamMessage(IS_TRANSFER_FAILED, 0);
*/



}

void SVSCamDLL::TerminateThread()
{
	
	return;
}

