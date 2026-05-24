using System.Diagnostics;
using System.Runtime.InteropServices;
using ClickLight.Windows.Core.Models;
using ClickLight.Windows.Interop;
using Microsoft.UI.Dispatching;

namespace ClickLight.Windows.Services;

public sealed class ClickCaptureService : IDisposable
{
    private const int KeyDownMask = 0x8000;
    private readonly DispatcherQueue _dispatcherQueue;
    private NativeMethods.HookProc? _hookProc;
    private IntPtr _hookHandle;
    private NativeMethods.POINT _lastDragPoint;
    private bool _hasDragPoint;

    public ClickCaptureService(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public event EventHandler<ClickEvent>? ClickReceived;

    public string StatusLabel => _hookHandle != IntPtr.Zero ? "Low-Level Mouse Hook" : "Stopped";

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        _hookProc = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(module?.ModuleName),
            0);
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
        _hasDragPoint = false;
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, code, wParam, lParam);
        }

        var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
        var point = data.pt;
        var timestamp = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        ClickEvent? clickEvent = wParam.ToInt32() switch
        {
            NativeMethods.WM_LBUTTONDOWN => new ClickEvent(ClickKind.LeftDown, point.X, point.Y, timestamp),
            NativeMethods.WM_LBUTTONUP => new ClickEvent(ClickKind.LeftUp, point.X, point.Y, timestamp),
            NativeMethods.WM_RBUTTONDOWN => new ClickEvent(ClickKind.RightDown, point.X, point.Y, timestamp),
            NativeMethods.WM_RBUTTONUP => new ClickEvent(ClickKind.RightUp, point.X, point.Y, timestamp),
            NativeMethods.WM_MOUSEMOVE when IsDragging(point) => new ClickEvent(ClickKind.Drag, point.X, point.Y, timestamp),
            _ => null
        };

        if (clickEvent is { } value)
        {
            _dispatcherQueue.TryEnqueue(() => ClickReceived?.Invoke(this, value));
        }

        return NativeMethods.CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    private bool IsDragging(NativeMethods.POINT point)
    {
        var isDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & KeyDownMask) != 0 ||
                     (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & KeyDownMask) != 0;

        if (!isDown)
        {
            _hasDragPoint = false;
            return false;
        }

        if (_hasDragPoint && point.X == _lastDragPoint.X && point.Y == _lastDragPoint.Y)
        {
            return false;
        }

        _lastDragPoint = point;
        _hasDragPoint = true;
        return true;
    }
}
