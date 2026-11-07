using System.Runtime.InteropServices;
using System.Text;

namespace UGA.Infrastructure.Input;

public sealed record DeviceIdentity(string InstancePath, bool IsVirtual, bool Resolved, bool IsBluetooth = false);

public static class WindowsDeviceIdentity
{
    [StructLayout(LayoutKind.Sequential)] private struct InterfaceData { public int Size; public Guid ClassGuid; public int Flags; public nuint Reserved; }
    [StructLayout(LayoutKind.Sequential)] private struct DeviceData { public int Size; public Guid ClassGuid; public uint DevInst; public nuint Reserved; }
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetClassDevsW", SetLastError = true)]
    private static extern nint GetClassDevs(nint guid, string? enumerator, nint hwnd, uint flags);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiOpenDeviceInterfaceW", SetLastError = true)]
    private static extern bool OpenInterface(nint set, string path, uint flags, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetDeviceInterfaceDetailW", SetLastError = true)]
    private static extern bool GetDetail(nint set, ref InterfaceData data, nint detail, uint size, out uint required, ref DeviceData device);
    [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(nint set);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_IDW")]
    private static extern int GetId(uint devInst, StringBuilder buffer, int length, uint flags);
    [DllImport("cfgmgr32.dll", EntryPoint = "CM_Get_Parent")] private static extern int GetParent(out uint parent, uint child, uint flags);
    public static DeviceIdentity Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return new("", false, false);
        var set = GetClassDevs(0, null, 0, 0x14); // ALLCLASSES | DEVICEINTERFACE, no modifications.
        if (set == -1) return new("", false, false);
        try
        {
            var iface = new InterfaceData { Size = Marshal.SizeOf<InterfaceData>() };
            if (!OpenInterface(set, path, 0, ref iface)) return new("", false, false);
            var dev = new DeviceData { Size = Marshal.SizeOf<DeviceData>() };
            GetDetail(set, ref iface, 0, 0, out var needed, ref dev);
            if (needed == 0 || needed > 65536) return new("", false, false);
            var detail = Marshal.AllocHGlobal((int)needed);
            try
            {
                Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                if (!GetDetail(set, ref iface, detail, needed, out _, ref dev)) return new("", false, false);
                var current = dev.DevInst; string instance = ""; bool isVirtual = false, bluetooth = false;
                for (int i = 0; i < 20; i++)
                {
                    var buffer = new StringBuilder(1024);
                    if (GetId(current, buffer, buffer.Capacity, 0) != 0) break;
                    var id = buffer.ToString(); if (i == 0) instance = id;
                    if (id.Contains("VIGEM", StringComparison.OrdinalIgnoreCase)) isVirtual = true;
                    if (id.StartsWith("BTH", StringComparison.OrdinalIgnoreCase) || id.Contains("{00001124-0000-1000-8000-00805F9B34FB}", StringComparison.OrdinalIgnoreCase)) bluetooth = true;
                    if (GetParent(out var parent, current, 0) != 0) break;
                    current = parent;
                }
                return new(instance, isVirtual, instance.Length > 0, bluetooth);
            }
            finally { Marshal.FreeHGlobal(detail); }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
    }
}
