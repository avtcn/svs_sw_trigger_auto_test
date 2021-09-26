// Display.cpp : implementation file
//

#include "stdafx.h"
#include "Display.h"
#include "afxdialogex.h"


// Display dialog
IMPLEMENT_DYNAMIC(Display, CDialog)

Display::Display(CWnd* pParent /*=NULL*/)
    : CDialog(Display::IDD, pParent)
    , m_display_dialogue(0)
{


    Create(IDD_DISPLAY, pWnd);
    // Obtain a device context for displaying bitmaps
    hdcDisplay = ::GetDC(m_display);
    camwidth = 0;
    //DisplayWidth = 0;
    ::SetStretchBltMode(hdcDisplay, COLORONCOLOR);
    pWnd = m_display.GetParent();

    WINDOWPLACEMENT wp;
    //GetWindowPlacement(&wp);
    wp.showCmd = SW_SHOWMAXIMIZED;
    wp.rcNormalPosition.left = 0;
    wp.rcNormalPosition.top = 0;
    wp.rcNormalPosition.right = 300;
    wp.rcNormalPosition.bottom = 300;
    SetWindowPlacement(&wp);


}

Display::~Display()
{

    ::ReleaseDC(
                m_display,
                hdcDisplay);

    if ((NULL !=  ImageData_RGB))
        GlobalFree( ImageData_RGB);

    if ((NULL !=  ImageData_MONO))
        GlobalFree(ImageData_MONO);
    pWnd->DestroyWindow();

}

void Display::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    DDX_Control(pDX, IDC_Display, m_display);
    //	DDX_Control(pDX, IDC_Display2, m_display_source);
    //DDX_Control(pDX, IDC_Display2, m_display_source);
}

BEGIN_MESSAGE_MAP(Display, CDialog)
ON_BN_CLICKED(IDOK, &Display::OnBnClickedOk)
ON_WM_ERASEBKGND()
END_MESSAGE_MAP()

// Display message handlers
void Display::OnBnClickedOk()
{
    // TODO: Add your control notification handler code here
    CDialog::OnOK();
}

void Display::ShowImageMono( size_t _Width, size_t _Height, unsigned char *ImageData)
{

    CRect rect2;

    pWnd->GetWindowRect(&rect2);

    int Width = (int)_Width;
    int Height = (int)_Height;

    // Check image alignment
    if (Width % 4 == 0)
    {
        BITMAPINFO *bitmapinfo;

        // Generate and fill a bitmap info structure
        bitmapinfo = (BITMAPINFO *)new char[sizeof(BITMAPINFOHEADER) + 256 * sizeof(RGBQUAD)];

        bitmapinfo->bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bitmapinfo->bmiHeader.biWidth = (long)Width;
        bitmapinfo->bmiHeader.biHeight = -Height;
        bitmapinfo->bmiHeader.biBitCount = 8;
        bitmapinfo->bmiHeader.biPlanes = 1;
        bitmapinfo->bmiHeader.biClrUsed = 0;
        bitmapinfo->bmiHeader.biClrImportant = 0;
        bitmapinfo->bmiHeader.biCompression = BI_RGB;
        bitmapinfo->bmiHeader.biSizeImage = 0;
        bitmapinfo->bmiHeader.biXPelsPerMeter = 0;
        bitmapinfo->bmiHeader.biYPelsPerMeter = 0;

        // Fill color table with gray levels
        for (int i = 0; i < 256; i++)
        {
            bitmapinfo->bmiColors[i].rgbRed = i;
            bitmapinfo->bmiColors[i].rgbGreen = i;
            bitmapinfo->bmiColors[i].rgbBlue = i;
            bitmapinfo->bmiColors[i].rgbReserved = 0;
        }

        double SizeX = (double)Width;
        double SizeY = (double)Height;

        double canvas_w = (double)rect2.Width();
        double canvas_h = (double)rect2.Height();

        double scale_x = canvas_w / Width;
        double scale_y = canvas_h / Height;

        double src_w, src_h;
        double dst_w, dst_h;

        double dst_x = 0;
        double dst_y = 0;

        src_w = (double)Width;
        src_h = (double)Height;

        // fit to screen
        if (scale_x < scale_y)
        {
            dst_w = canvas_w;
            dst_h = src_h * scale_x;

            dst_x = 0;
            dst_y = (canvas_h - dst_h) / 2;
            SizeX = dst_w;
            SizeY = dst_h;
        }
        else
        {
            dst_w = src_w * scale_y;
            dst_h = canvas_h;

            dst_x = (canvas_w - dst_w) / 2;
            dst_y = 0;

            SizeX = dst_w;
            SizeY = dst_h;
        }

        if ((DisplayHeight != canvas_w) || DisplayWidth != canvas_h)
        {
            if (scale_x < scale_y)
            {
                RECT rect;
                rect.left = -1;
                rect.right = (LONG)canvas_w;

                rect.top = -1;
                rect.bottom = (LONG)dst_y;
                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));
                rect.top = (LONG)(canvas_h - dst_y);
                rect.bottom = (LONG)canvas_h;
                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                DisplayWidth = (int)canvas_w;
                DisplayHeight = (int)canvas_h;

            }
            else
            {
                RECT rect;

                rect.top = -1;
                rect.bottom = (LONG)canvas_h;

                rect.left = -1;
                rect.right = (LONG)dst_x;

                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                rect.left = (LONG)(canvas_w - dst_x);
                rect.right = (LONG)canvas_w;

                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                DisplayWidth = (int)canvas_w;
                DisplayHeight = (int)canvas_h;
            }
        }
        StretchDIBits(hdcDisplay, (int)dst_x, (int)dst_y, (int)SizeX, (int)SizeY, 0, 0, Width, Height, ImageData, bitmapinfo, DIB_RGB_COLORS, SRCCOPY);

        delete[]bitmapinfo;
    }
}

void Display::ShowImageRGB(size_t Width, size_t Height, unsigned char *ImageData)
{

    CRect rect2;
    this->GetWindowRect(&rect2);
    //this->GetWindowsRect(&rect2);

    // Check image alignment
    if (Width % 4 == 0)
    {
        BITMAPINFO *bitmapinfo;

        // Generate and fill a bitmap info structure
        bitmapinfo = (BITMAPINFO *)new char[sizeof(BITMAPINFOHEADER)];

        bitmapinfo->bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bitmapinfo->bmiHeader.biWidth = (int)Width;
        bitmapinfo->bmiHeader.biHeight = -(int)Height;
        bitmapinfo->bmiHeader.biBitCount = 24;
        bitmapinfo->bmiHeader.biPlanes = 1;
        bitmapinfo->bmiHeader.biClrUsed = 0;
        bitmapinfo->bmiHeader.biClrImportant = 0;
        bitmapinfo->bmiHeader.biCompression = BI_RGB;
        bitmapinfo->bmiHeader.biSizeImage = 0;
        bitmapinfo->bmiHeader.biXPelsPerMeter = 0;
        bitmapinfo->bmiHeader.biYPelsPerMeter = 0;

        double SizeX = (double)Width;
        double SizeY = (double)Height;

        double canvas_w = (double) rect2.Width();
        double canvas_h = (double)rect2.Height();

        double scale_x = canvas_w / Width;
        double scale_y = canvas_h / Height;

        double src_w, src_h;
        double dst_w, dst_h;

        double dst_x = 0;
        double dst_y = 0;

        src_w = (double)Width;
        src_h = (double)Height;


        // fit to screen
        if (scale_x < scale_y)
        {
            dst_w = canvas_w;
            dst_h = src_h * scale_x;

            dst_x = 0;
            dst_y = (canvas_h - dst_h) / 2;
            SizeX = dst_w;
            SizeY = dst_h;
        }
        else

        {
            dst_w =  src_w * scale_y;
            dst_h = canvas_h;

            dst_x = (canvas_w - dst_w) / 2;
            dst_y = 0;

            SizeX =  dst_w;
            SizeY =  dst_h;
        }


        if ((DisplayHeight != canvas_w) || DisplayWidth != canvas_h)
        {
            if (scale_x < scale_y)
            {
                RECT rect;
                rect.left = -1;
                rect.right = (LONG)canvas_w;

                rect.top = -1;
                rect.bottom = (LONG)dst_y;
                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                rect.top = (LONG) (canvas_h - dst_y-1);
                rect.bottom = (LONG)canvas_h;
                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                DisplayWidth = (int)canvas_w;
                DisplayHeight = (int)canvas_h;
            }
            else
            {
                RECT rect;

                rect.top = -1;
                rect.bottom = (LONG)canvas_h;

                rect.left = -1;
                rect.right = (LONG)dst_x;

                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                rect.left = (LONG)(canvas_w - dst_x-1);
                rect.right = (LONG)canvas_w;

                FillRect(hdcDisplay, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

                DisplayWidth = (int)canvas_w;
                DisplayHeight = (int)canvas_h;
            }
        }
        StretchDIBits(hdcDisplay, (int)dst_x, (int)dst_y,(int) SizeX, (int)SizeY, 0, 0, (int)Width,(int) Height, ImageData, bitmapinfo, DIB_RGB_COLORS, SRCCOPY);

        delete[]bitmapinfo;
    }
}

BOOL Display::OnEraseBkgnd(CDC* pDC)
{
    return  true;
    //return CDialog::OnEraseBkgnd(pDC);
}
