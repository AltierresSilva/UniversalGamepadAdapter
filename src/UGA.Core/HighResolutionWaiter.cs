using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace UGA.Core;

// Windows 10 1803+ per-process high-resolution wait; does not change the global timer resolution.
internal sealed class HighResolutionWaiter : WaitHandle
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWaitableTimerExW(nint attributes, string? name, uint flags, uint access);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWaitableTimer(nint timer, ref long dueTime, int period, nint completion, nint argument, bool resume);
    private readonly WaitHandle[] handles;
    private readonly WaitHandle cancellation;
    public HighResolutionWaiter(WaitHandle cancellation)
    {
        this.cancellation = cancellation;
        SafeWaitHandle = new SafeWaitHandle(CreateWaitableTimerExW(0, null, 2, 0x100002), true);
        handles = [this, cancellation];
    }
    public void Wait(TimeSpan duration)
    {
        long due = -Math.Max(1, duration.Ticks);
        if (!SafeWaitHandle.IsInvalid && SetWaitableTimer(SafeWaitHandle.DangerousGetHandle(), ref due, 0, 0, 0, false)) WaitAny(handles);
        else cancellation.WaitOne(duration);
    }
}
