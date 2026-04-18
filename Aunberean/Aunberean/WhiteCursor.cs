using AcClient;
using CommandLine;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using ImGuiNET;
using Microsoft.DirectX.PrivateImplementationDetails;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Common.Enums;
using UtilityBelt.Scripting.Enums;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service.Lib.Settings;
using ObjectClass = UtilityBelt.Scripting.Enums.ObjectClass;

namespace Aunberean
{
    public class WhiteCursor : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate byte SetCursorFromIcon(IntPtr hNewIcon);

        internal static Hook2 hookSetCursorFromIcon = new Hook2((int)Entrypoint.Device__SetCursorFromIcon, (int)Call.Device__SetCursorFromIcon_0x00439F56);

        private static IntPtr replacementCursor = IntPtr.Zero;
        private static IntPtr replacementCursorHot = IntPtr.Zero;
        private static IntPtr replacementCursorUse = IntPtr.Zero;
        private static IntPtr replacementCursorInspect = IntPtr.Zero;
        private static IntPtr replacementCursorHourglass = IntPtr.Zero;
        private static IntPtr replacementCursorHourglassHot = IntPtr.Zero;
        private static IntPtr replacementCursorCraft = IntPtr.Zero;

        private readonly PluginCore _plugin;

        public WhiteCursor(PluginCore plugin)
        {
            _plugin = plugin;

            replacementCursor = LoadCursorFromResource("Aunberean.Resources.06004D68new.png");
            replacementCursorHot = LoadCursorFromResource("Aunberean.Resources.06004D69new.png");
            replacementCursorUse = LoadCursorFromResource("Aunberean.Resources.06004D72new.png");

            replacementCursorInspect = LoadCursorFromResource("Aunberean.Resources.06004D71new.png");
            replacementCursorHourglass = LoadCursorFromResource("Aunberean.Resources.06004D74mod.png");
            replacementCursorHourglassHot = LoadCursorFromResource("Aunberean.Resources.06004D75mod.png");
            replacementCursorCraft = LoadCursorFromResource("Aunberean.Resources.06004D73new.png");

            if (_plugin.whiteCursors.Value)
                hookSetCursorFromIcon.Setup(new SetCursorFromIcon(myhookSetCursorFromIcon), typeof(SetCursorFromIcon));

        }

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);
        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;         // false = cursor, true = icon
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO iconInfo);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private byte myhookSetCursorFromIcon(IntPtr hNewIcon)
        {
            try { 
                uint hash = ComputeCursorHash(hNewIcon);
                var orig = (SetCursorFromIcon)hookSetCursorFromIcon.Original;
                IntPtr hReplacement = IntPtr.Zero;

                if (hash == 0x29ED4B7D || hash == 0xDBCCB086 || hash == 0x3E4CE0AE)
                {
                    hReplacement = CopyIcon(replacementCursor);// LoadCursor(IntPtr.Zero, 32512); // IDC_ARROW
                }

                if (hash == 0x4C1F18CB || hash == 0x11E1E6EB || hash == 0xA1416146)
                {
                    hReplacement = CopyIcon(replacementCursorHot);// LoadCursor(IntPtr.Zero, 32649); // IDC_HAND
                }

                if (hash == 3306831191)
                {
                    hReplacement = CopyIcon(replacementCursorHourglass); //LoadCursor(IntPtr.Zero, 32650); // IDC_APPSTARTING
                }

                if (hash == 631039743)
                {
                    hReplacement = CopyIcon(replacementCursorHourglassHot); //LoadCursor(IntPtr.Zero, 32650); // IDC_APPSTARTING
                }

                
                if (hash == 2287092938)
                {
                    hReplacement = CopyIcon(replacementCursorUse);
                }

                if (hash == 2595743234)
                {
                    hReplacement = CopyIcon(replacementCursorInspect);
                }
                //todo set cursor point on this
                //if (hash == 4145837074)
                //{
                //    hReplacement = CopyIcon(replacementCursorCraft);
                //}

                if (hReplacement != IntPtr.Zero)
                {
                    DestroyIcon(hNewIcon);
                    return orig(hReplacement);
                }

                return orig(hNewIcon);
            }
            catch (Exception ex)
            {
                Log(ex);
                var orig = (SetCursorFromIcon)hookSetCursorFromIcon.Original;
                return orig(hNewIcon);
            }
        }

        private static IntPtr LoadCursorFromResource(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream stream = asm.GetManifestResourceStream(resourceName);
            using Bitmap bmp = new Bitmap(stream);
            return LoadCursorFromBitmap(bmp, xHotspot: 0, yHotspot: 0);
        }

        public static IntPtr LoadCursorFromBitmap(Bitmap bmp, int xHotspot = 0, int yHotspot = 0)
        {

            Bitmap clone = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(clone))
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);


            IntPtr hbmColor = clone.GetHbitmap(Color.FromArgb(0));

            IntPtr hbmMask = clone.GetHbitmap(Color.FromArgb(0));

            ICONINFO info = new ICONINFO
            {
                fIcon = false,
                xHotspot = xHotspot,
                yHotspot = yHotspot,
                hbmColor = hbmColor,
                hbmMask = hbmMask
            };

            IntPtr hCursor = CreateIconIndirect(ref info);


            DeleteObject(hbmColor);
            DeleteObject(hbmMask);

            return hCursor;
        }

        private static uint ComputeCursorHash(IntPtr hIcon)
        {
            uint hash = 2166136261;

            using (Icon icon = Icon.FromHandle(hIcon))
            using (Bitmap bmp = icon.ToBitmap())
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

                try
                {
                    int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
                    byte[] pixelBytes = new byte[bytes];
                    Marshal.Copy(bmpData.Scan0, pixelBytes, 0, bytes);

                    for (int i = 0; i < pixelBytes.Length; i++)
                    {
                        hash ^= pixelBytes[i];
                        hash *= 16777619;
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }
            }

            return hash;
        }

        public void Dispose()
        {
            hookSetCursorFromIcon.Remove();
        }

        internal static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        /// <summary>
        /// Log a string to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(System.IO.Path.Combine(PluginCore.AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
    }
}
