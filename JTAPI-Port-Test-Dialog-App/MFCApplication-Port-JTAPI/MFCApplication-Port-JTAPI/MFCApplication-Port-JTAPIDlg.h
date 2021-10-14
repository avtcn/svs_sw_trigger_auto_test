
// MFCApplication-Port-JTAPIDlg.h: 头文件
//

#pragma once

#include "sv_gen_sdk.h"
#include "afxwin.h"
#include "Cameracontainer.h"


#include "SVSCamDLL.h"


// CMFCApplicationPortJTAPIDlg 对话框
class CMFCApplicationPortJTAPIDlg : public CDialogEx
{
// 构造
public:
	CMFCApplicationPortJTAPIDlg(CWnd* pParent = nullptr);	// 标准构造函数

// 对话框数据
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_MFCAPPLICATIONPORTJTAPI_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV 支持


// 实现
protected:
	HICON m_hIcon;

	// 生成的消息映射函数
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()

	// -----------------------------------------------------------------------------
    // Joe Added:
    // SVSCamDLL.h/cpp test
    SVSCamDLL m_SVSCamDLLInst;
    long m_nPhotoCounter = 0;

	// Status label
	CStatic m_SVSCamDLL_Details;


public:
	afx_msg void OnBnClickedButtonJtapiSvsCamOpen();
	afx_msg void OnBnClickedButtonJtapiSvsCamClose();
	afx_msg void OnBnClickedButtonJtapiSvsCamCaptureAndSave();

	// -----------------------------------------------------------------------------
};
