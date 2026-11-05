using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SVCamApi
{
    public partial class DisplayForm : Form
    {
        private readonly object m_lockBitmap = new object();
        private Bitmap m_currentBitmap = null;
        private int m_isRendering = 0;

        private byte[] m_rgbBuffer = null;
        private byte[] m_monoBuffer = null;

        private static readonly ColorPalette s_grayPalette;

        static DisplayForm()
        {
            using (Bitmap dummy = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                s_grayPalette = dummy.Palette;
                for (int i = 0; i < 256; i++)
                {
                    s_grayPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
            }
        }

        public DisplayForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void UpdateFrame(SVcamApi._SV_BUFFER_INFO info, byte[] rawBytes, SVcamApi api)
        {
            if (rawBytes == null || rawBytes.Length == 0 || info.iSizeX == IntPtr.Zero || info.iSizeY == IntPtr.Zero)
                return;

            // Frame skipping / throttling: if the previous frame hasn't completed paint, drop this one
            if (Interlocked.CompareExchange(ref m_isRendering, 1, 0) != 0)
                return;

            try
            {
                int width = (int)info.iSizeX.ToInt64();
                int height = (int)info.iSizeY.ToInt64();
                bool isBayer = (info.iPixelType & (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_ID_MASK) >= 8;
                ulong pixelOccupy = info.iPixelType & (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_EFFECTIVE_PIXELSIZE_MASK;

                Bitmap newBmp = null;

                if (isBayer)
                {
                    int rgbLength = width * height * 3;
                    if (m_rgbBuffer == null || m_rgbBuffer.Length != rgbLength)
                        m_rgbBuffer = new byte[rgbLength];

                    // Pin raw bytes to info.pImagePtr for SVUtil call
                    GCHandle rawPin = GCHandle.Alloc(rawBytes, GCHandleType.Pinned);
                    try
                    {
                        info.pImagePtr = rawPin.AddrOfPinnedObject();
                        api.SVS_UtilBufferBayerToRGB(info, ref m_rgbBuffer[0], rgbLength);
                    }
                    finally
                    {
                        rawPin.Free();
                    }

                    newBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    BitmapData bd = newBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        int rowBytes = width * 3;
                        if (bd.Stride == rowBytes)
                        {
                            Marshal.Copy(m_rgbBuffer, 0, bd.Scan0, rgbLength);
                        }
                        else
                        {
                            for (int y = 0; y < height; y++)
                            {
                                IntPtr destRow = new IntPtr(bd.Scan0.ToInt64() + y * bd.Stride);
                                Marshal.Copy(m_rgbBuffer, y * rowBytes, destRow, rowBytes);
                            }
                        }
                    }
                    finally
                    {
                        newBmp.UnlockBits(bd);
                    }
                }
                else if (pixelOccupy == (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_OCCUPY12BIT ||
                         pixelOccupy == (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_OCCUPY16BIT)
                {
                    int monoLength = width * height;
                    if (m_monoBuffer == null || m_monoBuffer.Length != monoLength)
                        m_monoBuffer = new byte[monoLength];

                    GCHandle rawPin = GCHandle.Alloc(rawBytes, GCHandleType.Pinned);
                    try
                    {
                        info.pImagePtr = rawPin.AddrOfPinnedObject();
                        if (pixelOccupy == (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_OCCUPY12BIT)
                            api.SVS_UtilBuffer12BitTo8Bit(info, ref m_monoBuffer[0], monoLength);
                        else
                            api.SVS_UtilBuffer16BitTo8Bit(info, ref m_monoBuffer[0], monoLength);
                    }
                    finally
                    {
                        rawPin.Free();
                    }

                    newBmp = Create8bppBitmap(width, height, m_monoBuffer);
                }
                else // 8-bit Mono
                {
                    newBmp = Create8bppBitmap(width, height, rawBytes);
                }

                if (newBmp != null)
                {
                    Bitmap oldBmp = null;
                    lock (m_lockBitmap)
                    {
                        oldBmp = m_currentBitmap;
                        m_currentBitmap = newBmp;
                    }

                    if (oldBmp != null)
                        oldBmp.Dispose();

                    if (!IsDisposed && IsHandleCreated)
                    {
                        BeginInvoke(new Action(Invalidate));
                    }
                    else
                    {
                        m_isRendering = 0;
                    }
                }
                else
                {
                    m_isRendering = 0;
                }
            }
            catch
            {
                m_isRendering = 0;
            }
        }

        private Bitmap Create8bppBitmap(int width, int height, byte[] pixelData)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            bmp.Palette = s_grayPalette;
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            try
            {
                if (bd.Stride == width)
                {
                    Marshal.Copy(pixelData, 0, bd.Scan0, width * height);
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr destRow = new IntPtr(bd.Scan0.ToInt64() + y * bd.Stride);
                        Marshal.Copy(pixelData, y * width, destRow, width);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                e.Graphics.Clear(Color.Black);

                Bitmap bmpToDraw = null;
                lock (m_lockBitmap)
                {
                    if (m_currentBitmap != null)
                    {
                        bmpToDraw = m_currentBitmap;
                    }
                }

                if (bmpToDraw != null)
                {
                    double canvasW = ClientSize.Width;
                    double canvasH = ClientSize.Height;
                    if (canvasW > 0 && canvasH > 0)
                    {
                        double imgW = bmpToDraw.Width;
                        double imgH = bmpToDraw.Height;

                        double scaleX = canvasW / imgW;
                        double scaleY = canvasH / imgH;

                        double dstW, dstH, dstX, dstY;

                        if (scaleX < scaleY)
                        {
                            dstW = canvasW;
                            dstH = imgH * scaleX;
                            dstX = 0;
                            dstY = (canvasH - dstH) / 2.0;
                        }
                        else
                        {
                            dstW = imgW * scaleY;
                            dstH = canvasH;
                            dstX = (canvasW - dstW) / 2.0;
                            dstY = 0;
                        }

                        Rectangle dstRect = new Rectangle((int)dstX, (int)dstY, (int)dstW, (int)dstH);
                        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        e.Graphics.DrawImage(bmpToDraw, dstRect, 0, 0, bmpToDraw.Width, bmpToDraw.Height, GraphicsUnit.Pixel);
                    }
                }
            }
            finally
            {
                m_isRendering = 0;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
