using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Xenbyte_Injector.Components
{
    class Utilities
    {
        [StructLayout(LayoutKind.Sequential)]
        protected struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32.dll")]
        protected static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbfileInfo, uint uFlags);

        [DllImport("user32.dll")]
        protected static extern bool DestroyIcon(IntPtr hIcon);

        public static ImageSource GetFileIcon(string pszPath)
        {
            uint FILE_ATTRIBUTE_TEMPORARY = 0x000000100;

            uint SHGFI_ICON = 0x000000100;
            uint SHGFI_SMALLICON = 0x000000001;
            uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

            SHFILEINFO sfi = new SHFILEINFO()
            {
                hIcon = IntPtr.Zero,
                iIcon = 0,
                dwAttributes = 0,
                szDisplayName = "",
                szTypeName = ""
            };

            SHGetFileInfo(pszPath, FILE_ATTRIBUTE_TEMPORARY, out sfi, (uint)Marshal.SizeOf(sfi), SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            ImageSource image = Imaging.CreateBitmapSourceFromHIcon(sfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            
            DestroyIcon(sfi.hIcon);
            
            return image;
        }


        [DllImport("user32.dll")]
        protected static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        protected static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        protected static int GetProcessIdByHwnd(IntPtr hwnd)
        {
            uint processId = 0;
            GetWindowThreadProcessId(hwnd, out processId);
            return (int)processId;
        }

        public static int GetProcessIdByWindowName(string windowName)
        {
            return GetProcessIdByHwnd(FindWindow(null, windowName));
        }

        public static int GetProcessIdByClassName(string className)
        {
            return GetProcessIdByHwnd(FindWindow(className, null));
        }

        public static Process GetProcessByWindowName(string windowName)
        {
            try
            {
                return Process.GetProcessById(GetProcessIdByWindowName(windowName));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Process GetProcessByClassName(string className)
        {
            try
            {
                return Process.GetProcessById(GetProcessIdByClassName(className));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
