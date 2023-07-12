using System.Drawing;
using System.Runtime.InteropServices;

namespace CSharpFileExplorer
{
    internal class IconLoader
    {
        public static Icon GetStockIcon(NativeMethods.SHSTOCKICONID siid)
        {
            var stockIconInfo = new NativeMethods.SHSTOCKICONINFO();
            stockIconInfo.cbSize = (uint) Marshal.SizeOf<NativeMethods.SHSTOCKICONINFO>();
            NativeMethods.SHGetStockIconInfo(siid, (uint) NativeMethods.SHGFI.Icon, ref stockIconInfo);
            var icon = Icon.FromHandle(stockIconInfo.hIcon).ToBitmap(); //convert to bitmap to make it transparent
            icon.MakeTransparent();
            NativeMethods.DestroyIcon(stockIconInfo.hIcon);
            return Icon.FromHandle(icon.GetHicon()); //back to icon
        }

        public static Icon GetFileTypeIcon(string filetype)
        {
            var iconInfo = new NativeMethods.SHFILEINFO();
            NativeMethods.SHGetFileInfo(filetype, 256, ref iconInfo, 0,
                (uint) (NativeMethods.SHGFI.Icon | NativeMethods.SHGFI.SmallIcon |
                        NativeMethods.SHGFI.UseFileAttributes));
            var icon = Icon.FromHandle(iconInfo.hIcon).ToBitmap();
            icon.MakeTransparent();
            NativeMethods.DestroyIcon(iconInfo.hIcon);
            return Icon.FromHandle(icon.GetHicon());
        }
    }
}