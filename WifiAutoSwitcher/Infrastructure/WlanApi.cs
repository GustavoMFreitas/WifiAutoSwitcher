using System.Runtime.InteropServices;
using System.Threading;

namespace WifiAutoSwitcher.Infrastructure;

internal static class WlanApi
{
    private const uint ClientVersion = 2;
    private const uint Success = 0;
    private const uint NotificationSourceNone = 0;
    private const uint NotificationSourceAcm = 0x00000008;
    private const uint NotificationAcmScanComplete = 7;
    private const uint NotificationAcmScanFail = 8;

    public static bool TryTriggerScanAndWait(TimeSpan timeout)
    {
        IntPtr clientHandle = IntPtr.Zero;
        IntPtr interfaceListPtr = IntPtr.Zero;
        var sync = new object();
        using var signal = new AutoResetEvent(false);
        var pendingInterfaces = new HashSet<Guid>();

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

            WlanNotificationCallback callback = (ref WlanNotificationData data, IntPtr _) =>
            {
                if (data.NotificationSource != NotificationSourceAcm)
                {
                    return;
                }

                if (data.NotificationCode != NotificationAcmScanComplete && data.NotificationCode != NotificationAcmScanFail)
                {
                    return;
                }

                lock (sync)
                {
                    if (pendingInterfaces.Remove(data.InterfaceGuid))
                    {
                        signal.Set();
                    }
                }
            };

            _ = WlanRegisterNotification(
                clientHandle,
                NotificationSourceAcm,
                false,
                callback,
                IntPtr.Zero,
                IntPtr.Zero,
                out _);

            var count = Marshal.ReadInt32(interfaceListPtr, 0);
            var itemSize = Marshal.SizeOf<WlanInterfaceInfo>();
            var offset = 8; // dwNumberOfItems + dwIndex

            for (var i = 0; i < count; i++)
            {
                var itemPtr = IntPtr.Add(interfaceListPtr, offset + (i * itemSize));
                var info = Marshal.PtrToStructure<WlanInterfaceInfo>(itemPtr);
                var scanResult = WlanScan(clientHandle, ref info.InterfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (scanResult == Success)
                {
                    pendingInterfaces.Add(info.InterfaceGuid);
                }
            }

            if (pendingInterfaces.Count == 0)
            {
                return false;
            }

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                lock (sync)
                {
                    if (pendingInterfaces.Count == 0)
                    {
                        _ = WlanRegisterNotification(
                            clientHandle,
                            NotificationSourceNone,
                            false,
                            null,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            out _);
                        GC.KeepAlive(callback);
                        return true;
                    }
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                signal.WaitOne(remaining > TimeSpan.FromMilliseconds(700)
                    ? TimeSpan.FromMilliseconds(700)
                    : remaining);
            }

            _ = WlanRegisterNotification(
                clientHandle,
                NotificationSourceNone,
                false,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                out _);
            GC.KeepAlive(callback);
            return false;
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
    private static extern uint WlanRegisterNotification(
        IntPtr hClientHandle,
        uint dwNotifSource,
        [MarshalAs(UnmanagedType.Bool)] bool bIgnoreDuplicate,
        WlanNotificationCallback? funcCallback,
        IntPtr pCallbackContext,
        IntPtr pReserved,
        out uint pdwPrevNotifSource);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr pMemory);

    [StructLayout(LayoutKind.Sequential)]
    private struct WlanNotificationData
    {
        public uint NotificationSource;
        public uint NotificationCode;
        public Guid InterfaceGuid;
        public uint DataSize;
        public IntPtr DataPtr;
    }

    private delegate void WlanNotificationCallback(ref WlanNotificationData data, IntPtr context);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo
    {
        public Guid InterfaceGuid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string InterfaceDescription;

        public uint IsState;
    }
}
