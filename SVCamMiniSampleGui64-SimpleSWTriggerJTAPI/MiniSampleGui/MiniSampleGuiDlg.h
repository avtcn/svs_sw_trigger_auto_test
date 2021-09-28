
// MiniSample DisplayDlg.h : header file
//

#pragma once
#include "sv_gen_sdk.h"
#include "Display.h"
#include "afxwin.h"
#include "Cameracontainer.h"

#include "stdafx.h"
#define WM_DISPLAY_IMAGE	(WM_APP + 400)


// CMiniSampleDisplayDlg dialog
class CMiniSampleDisplayDlg : public CDialogEx
{
    // Construction
public:
    CMiniSampleDisplayDlg(CWnd* pParent = NULL);	// standard constructor
    ~CMiniSampleDisplayDlg( );	// standard constructor

    // Dialog Data
    enum { IDD = IDD_MINISAMPLEDISPLAY_DIALOG };
protected:
    virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
    //virtual void PostNcDestroy();


public:


    // Joe:
    long m_nSWTriggerCount;
    SV_BUFFER_INFO m_newBuffer= { 0 };
    bool m_bNewBufferReady = false;

    Camera   *currentCam;
    int   currentCamSize;
    CameraContainer *cam_container;
    Display displg;

    bool acqTerminated;
    bool isStoping;
    bool SaveTerminated;
    bool SaveImg;

    HANDLE m_thread;
    CWinThread *m_save_thread;
    HANDLE m_acquisitionstopThread;
    HANDLE m_SaveStopThread;
    
    vector< SV_BUFFER_INFO  *>pive_buffer;
    deque <SV_BUFFER_INFO* > save_list;
    bool sdk_init_done;


    bool piv_mode_nabled ;
    unsigned int  save_img_count;
    unsigned int save_img_buffer_count;


    int startAcqThread();
    CRITICAL_SECTION csacquisition;

    //

    // Implementation
protected:
    HICON m_hIcon;



    // Generated message map functions
    virtual BOOL OnInitDialog();
    afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
    afx_msg void OnPaint();
    afx_msg HCURSOR OnQueryDragIcon();
    DECLARE_MESSAGE_MAP()
    public:
        afx_msg void OnBnClickedCheck1();
    afx_msg void OnBnClickedButton2();
    afx_msg void OnBnClickedButtonStart();
    afx_msg void OnBnClickedButtonStop();
    afx_msg void OnBnClickedButton1();
    CComboBox m_cameras;
    CStatic m_cam_found;
    afx_msg void OnCbnSelchangeCombo1();
    // Function for displaying received images.
    LRESULT WMDisplayImage(WPARAM WParam, LPARAM LParam);
    afx_msg void OnBnClickedOk();
    CComboBox m_acq_mode;
    afx_msg void OnBnClickedCheck4();
    CButton m_trigger_mode;
    afx_msg void OnBnClickedButtonStop2();
    CButton m_aoi;
    afx_msg void OnBnClickedCheck3();
    CComboBox m_auto_white_balance;
    afx_msg void OnCbnSelchangeCombo2();
    afx_msg void OnCbnSelchangeCombo3();
    CComboBox m_piv_mode;
    CButton m_save_img;


    afx_msg void OnClose();
    afx_msg void OnBnClickedSave();
    CStatic m_frame_counter;
    CStatic m_saved_frame_counter;
    afx_msg void OnBnClickedButtonSofttrigger();
    afx_msg void OnBnClickedBtnOpenSvsCamera();
    afx_msg void OnBnClickedBtnSoftwareTriggerOnePhoto();
    afx_msg void OnBnClickedBtnCloseSvsCamera();
    afx_msg void OnBnClickedBtnSwTriggerTimer1fps();
    bool m_bAutoTrigger = false;
    long m_nAutoTriggerCount = 0;
    CString strAutoTriggerText;
    afx_msg void OnTimer(UINT_PTR nIDEvent);
};
