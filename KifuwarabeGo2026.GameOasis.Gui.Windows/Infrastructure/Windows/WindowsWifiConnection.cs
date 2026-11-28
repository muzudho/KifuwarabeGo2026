namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Connect;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>Reads already-associated WLAN interfaces from Windows without scanning or sending traffic.</summary>
internal static class WindowsWifiConnection
{
    private const string WlanApi = "wlanapi.dll";
    private const int ConnectedInterfaceState = 1;
    private const int CurrentConnectionOpcode = 7;
    private static readonly Encoding SsidEncoding = new UTF8Encoding(false, true);

    public static WifiConnectionSnapshot Read()
    {
        if (WlanOpenHandle(2, IntPtr.Zero, out _, out var handle) != 0)
            return WifiConnectionSnapshot.Unavailable;

        try
        {
            if (WlanEnumInterfaces(handle, IntPtr.Zero, out var interfaces) != 0)
                return WifiConnectionSnapshot.Unavailable;
            try
            {
                return ReadInterfaces(handle, interfaces);
            }
            finally
            {
                WlanFreeMemory(interfaces);
            }
        }
        finally
        {
            WlanCloseHandle(handle, IntPtr.Zero);
        }
    }

    private static WifiConnectionSnapshot ReadInterfaces(IntPtr handle, IntPtr interfaces)
    {
        // WLAN_INTERFACE_INFO_LIST starts with a count and a current index.
        var count = Marshal.ReadInt32(interfaces);
        var entrySize = Marshal.SizeOf<InterfaceInfo>();
        var names = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var entry = Marshal.PtrToStructure<InterfaceInfo>(IntPtr.Add(interfaces, 8 + i * entrySize));
            if (entry.State != ConnectedInterfaceState) continue;
            var name = ReadAssociatedSsid(handle, entry.Id);
            if (name is null) return WifiConnectionSnapshot.Unavailable;
            names.Add(name);
        }
        return names.Count switch
        {
            0 => new(WifiConnectionKind.Disconnected),
            1 => new(WifiConnectionKind.Connected, names[0]),
            _ => new(WifiConnectionKind.Multiple, string.Join(" / ", names)),
        };
    }

    private static string? ReadAssociatedSsid(IntPtr handle, Guid id)
    {
        if (WlanQueryInterface(handle, ref id, CurrentConnectionOpcode, IntPtr.Zero, out var size, out var data, out _) != 0)
            return null;

        try
        {
            if (size < Marshal.SizeOf<ConnectionAttributesPrefix>()) return null;
            var connection = Marshal.PtrToStructure<ConnectionAttributesPrefix>(data);
            if (connection.State != ConnectedInterfaceState || connection.SsidLength is < 1 or > 32) return null;
            var ssid = SsidEncoding.GetString(connection.Ssid, 0, connection.SsidLength);
            foreach (var character in ssid)
                if (char.IsControl(character)) return null;
            return ssid;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
        finally
        {
            WlanFreeMemory(data);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct InterfaceInfo
    {
        public Guid Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Description;
        public int State;
    }

    // WLAN_CONNECTION_ATTRIBUTES, through the DOT11_SSID in WLAN_ASSOCIATION_ATTRIBUTES.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ConnectionAttributesPrefix
    {
        public int State;
        public int ConnectionMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string ProfileName;
        public int SsidLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Ssid;
    }

    [DllImport(WlanApi, ExactSpelling = true)]
    private static extern uint WlanOpenHandle(uint version, IntPtr reserved, out uint negotiatedVersion, out IntPtr handle);
    [DllImport(WlanApi, ExactSpelling = true)]
    private static extern uint WlanEnumInterfaces(IntPtr handle, IntPtr reserved, out IntPtr interfaces);
    [DllImport(WlanApi, ExactSpelling = true)]
    private static extern uint WlanQueryInterface(IntPtr handle, ref Guid id, int opcode, IntPtr reserved,
        out uint dataSize, out IntPtr data, out uint opcodeValueType);
    [DllImport(WlanApi, ExactSpelling = true)]
    private static extern uint WlanCloseHandle(IntPtr handle, IntPtr reserved);
    [DllImport(WlanApi, ExactSpelling = true)]
    private static extern void WlanFreeMemory(IntPtr memory);
}
