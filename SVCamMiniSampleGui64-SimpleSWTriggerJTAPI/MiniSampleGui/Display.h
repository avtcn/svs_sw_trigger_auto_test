#pragma once
#include "resource.h"	
#include "afxwin.h"


// Display dialog



class Display : public CDialog
{
    DECLARE_DYNAMIC(Display)

    public:
        Display(CWnd* pParent = NULL);   // standard constructor
    virtual ~Display();

    // Dialog Data
    enum { IDD = IDD_DISPLAY };

protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
    //virtual BOOL OnEraseBkgnd(CDC *pDC);
    DECLARE_MESSAGE_MAP()
    public:
        afx_msg void OnBnClickedOk();
    CStatic m_display;

    int m_display_dialogue;
    HDC hdcDisplay;
    HDC hdcSource;
    int  camwidth;
    int DisplayWidth;
    int DisplayHeight;
    int  resizeX;
    CWnd*  pWnd;



    //SV_BUFFER_INFO *ImageInfo;
    unsigned char * ImageData_RGB;

    PSECURITY_DESCRIPTOR rgbH;

    unsigned char * ImageData_MONO;
    PSECURITY_DESCRIPTOR  monh;

    unsigned char 	*display_Data;
    void ShowImageMono(size_t Width, size_t Height, unsigned char *ImageData);
    void ShowImageRGB( size_t Width, size_t Height, unsigned char *ImageData);

private:
    // A device context for displaying images

    //int DisplayWidth;
    //int DisplayHeight;

    // Function for displaying received images.
    //LRESULT WMDisplayImage(WPARAM WParam, LPARAM LParam);



public:
    //CStatic m_display_source;
    //CStatic m_display_source;
    afx_msg BOOL OnEraseBkgnd(CDC* pDC);
};
