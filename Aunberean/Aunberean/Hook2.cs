using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UtilityBelt.Common.Messages.Types;
using UtilityBelt.Service;

namespace AcClient;

//
// Summary:
//     New improved Hooker
public unsafe class Hook2
{
    internal IntPtr Entrypoint;
    public Delegate Original;
    internal Delegate Del;

    internal int call;

    internal static List<Hook2> hookers = new List<Hook2>();

    public Hook2(int entrypoint, int call_location)
    {
        Entrypoint = (IntPtr)entrypoint;
        call = call_location;
    }

    public bool Setup(Delegate del, Type originalType)
    {
        if (!hookers.Contains(this))
        {
            Del = del;
            if (ReadCall(call) != (int)Entrypoint)
            {
                UBService.WriteLog($"Failed to detour 0x{call:X8}. expected 0x{(int)Entrypoint:X8}, received 0x{ReadCall(call):X8}", LogLevel.Error);
                return false;
            }

            if (!PatchCall(call, Marshal.GetFunctionPointerForDelegate(Del)))
            {
                return false;
            }

            hookers.Add(this);
            UBService.WriteLog($"Hooking {(int)Entrypoint:X8}", LogLevel.Trace);

            Original = Marshal.GetDelegateForFunctionPointer(
                Entrypoint,
                originalType
            );
            return true;
        }

        return false;
    }

    public bool Remove()
    {
        if (hookers.Contains(this))
        {
            hookers.Remove(this);
            if (PatchCall(call, Entrypoint))
            {
                UBService.WriteLog($"Un-Hooking {(int)Entrypoint:X8}", LogLevel.Trace);
                return true;
            }
        }

        return false;
    }

    [DllImport("kernel32.dll")]
    internal static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, int flNewProtect, out int lpflOldProtect);

    internal unsafe static void Write(IntPtr address, int newValue)
    {
        VirtualProtectEx(Process.GetCurrentProcess().Handle, address, (UIntPtr)4uL, 64, out var lpflOldProtect);
        *(int*)(void*)address = newValue;
        VirtualProtectEx(Process.GetCurrentProcess().Handle, address, (UIntPtr)4uL, lpflOldProtect, out lpflOldProtect);
    }

    internal unsafe static bool PatchCall(int callLocation, IntPtr newPointer)
    {
        if ((*(byte*)callLocation & 0xFE) != 232)
        {
            return false;
        }

        int num = *(int*)(callLocation + 1);
        int num2 = num + (callLocation + 5);
        int newValue = (int)newPointer - (callLocation + 5);
        Write((IntPtr)(callLocation + 1), newValue);
        return true;
    }

    internal unsafe static int ReadCall(int callLocation)
    {
        if ((*(byte*)callLocation & 0xFE) != 232)
        {
            return 0;
        }

        int num = *(int*)(callLocation + 1);
        return num + (callLocation + 5);
    }

    internal static void Cleanup()
    {
        for (int num = hookers.Count - 1; num > -1; num--)
        {
            hookers[num].Remove();
        }
    }
}