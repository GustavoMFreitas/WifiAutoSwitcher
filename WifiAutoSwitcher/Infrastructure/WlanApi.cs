using System.Runtime.InteropServices;

namespace WifiAutoSwitcher.Infrastructure;

internal static class WlanApi
{
    private const uint ClientVersion = 2;
    private const uint Success = 0;

    public static bool TryTriggerScan()
    {
        IntPtr clientHandle = IntPtr.Zero;
        IntPtr interfaceListPtr = IntPtr.Zero;

        try
        {
            var openResult = WlanOpenHandle(ClientVersion, IntPtr.Zero, out _, out clientHandle);
            if (openResult != Success || clientHandle == IntPtr.Zero)
            {
                return false;
            }

            var enumResult = WlanEnumInterfaces(clientHandle, IntPtr.Zero, out interfaceListPtr);
            if (enumResult != Success || interfaceListPtr == IntPtr.Zero)
            {
                return false;
            }

            var count = Marshal.ReadInt32(interfaceListPtr, 0);
            var itemSize = Marshal.SizeOf<WlanInterfaceInfo>();
            var offset = 8; // dwNumberOfItems + dwIndex
            var anyTriggered = false;

            for (var i = 0; i < count; i++)
            {
                var itemPtr = IntPtr.Add(interfaceListPtr, offset + (i * itemSize));
                var info = Marshal.PtrToStructure<WlanInterfaceInfo>(itemPtr);
                var scanResult = WlanScan(clientHandle, ref info.InterfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (scanResult == Success)
                {
                    anyTriggered = true;
                }
            }

            return anyTriggered;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (interfaceListPtr != IntPtr.Zero)
            {
                WlanFreeMemory(interfaceListPtr);
            }

            if (clientHandle != IntPtr.Zero)
            {
                _ = WlanCloseHandle(clientHandle, IntPtr.Zero);
            }
        }
    }

    [DllImport("wlanapi.dll")]
    private static extern uint WlanOpenHandle(
        uint dwClientVersion,
        IntPtr pReserved,
        out uint pdwNegotiatedVersion,
        out IntPtr phClientHandle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(
        IntPtr hClientHandle,
        IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(
        IntPtr hClientHandle,
        IntPtr pReserved,
        out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanScan(
        IntPtr hClientHandle,
        ref Guid pInterfaceGuid,
        IntPtr pDot11Ssid,
        IntPtr pIeData,
        IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr pMemory);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo
    {
        public Guid InterfaceGuid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string InterfaceDescription;

        public uint IsState;
    }
}
