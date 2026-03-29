using Decal.Adapter;
using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;

namespace Aunberean
{
    public class WindowUI : IDisposable
    {
        private readonly Hud hud;
        private readonly PluginCore _plugin;
        public WindowUI(PluginCore plugin)
        {
            _plugin = plugin;
            // Create a new UBService Hud
            hud = UBService.Huds.CreateHud("Window Position");

            hud.WindowSettings = ImGuiWindowFlags.AlwaysAutoResize;
            // set to show our icon in the UBService HudBar
            hud.ShowInBar = true;

            // subscribe to the hud render event so we can draw some controls
            hud.OnRender += Hud_OnRender;
        }

        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {

                if(ImGui.Button("Window position " + _plugin.windowPosition1.Value.x + "," + _plugin.windowPosition1.Value.y))
                {
                    MoveWindow(_plugin.windowPosition1.Value.x, _plugin.windowPosition1.Value.y);
                }

                if (ImGui.Button("Right Monitor"))
                {
                    MoveWindow(2560, -1);
                }

                if (ImGui.Button("Left Monitor Lower"))
                {
                    MoveWindow(0, 0);
                }

                if (ImGui.Button("Right Monitor Lower"))
                {
                    MoveWindow(2560, 0);
                }
            }
            catch (Exception ex)
            {
                PluginCore.Log(ex);
            }
        }

        private void MoveWindow(int x, int y)
        {
            RECT rect = new RECT();

            GetWindowRect(CoreManager.Current.Decal.Hwnd, ref rect);

            MoveWindow(CoreManager.Current.Decal.Hwnd, x, y, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
        }

        public (int x, int y) GetWindow()
        {
            RECT rect = new RECT();

            GetWindowRect(CoreManager.Current.Decal.Hwnd, ref rect);

            return (rect.Left, rect.Top);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        public void setVisibility(bool visibility)
        { 
            hud.Visible = visibility;
            hud.ShowInBar = visibility;
        }
        public void Dispose()
        {
            hud.Dispose();
        }
    }
}