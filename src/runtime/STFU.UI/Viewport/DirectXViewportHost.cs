using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Platform;
using STFU.Common.Math;
using STFU.Logging;
using STFU.UI.Bridge.Session;

namespace STFU.UI;

internal sealed class DirectXViewportHost : NativeControlHost
{
    private readonly DirectXViewportPresenter _presenter;
    private readonly ViewportInputController _inputController;
    private readonly Action _requestFrame;
    private readonly WndProc _wndProc;
    private readonly DispatcherTimer _renderPump;
    private IntPtr _childHandle;
    private IntPtr _previousWndProc;
    private Point _lastPointerPosition;
    private bool _isOrbiting;
    private bool _loggedNativeInput;
    private bool _loggedAvaloniaInput;
    private int _requestRenderLogCounter;
    private int _lastNativeWidth;
    private int _lastNativeHeight;
    private bool _renderQueued;
    private bool _isPresentationPumpEnabled = true;

    public DirectXViewportHost(
        DirectXViewportPresenter presenter,
        UiEngineSession session,
        Action requestFrame,
        ViewportInputController? inputController = null)
    {
        _presenter = presenter;
        _inputController = inputController ?? new ViewportInputController(session);
        _requestFrame = requestFrame;
        _wndProc = ChildWndProc;
        _renderPump = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        IsHitTestVisible = true;
        Focusable = false;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += (_, _) => _isOrbiting = false;
        PointerWheelChanged += OnPointerWheelChanged;
        AttachedToVisualTree += (_, _) => StartRenderPump();
        DetachedFromVisualTree += (_, _) => StopRenderPump();
    }

    public bool IsPresentationPumpEnabled
    {
        get => _isPresentationPumpEnabled;
        set
        {
            if (_isPresentationPumpEnabled == value)
            {
                return;
            }

            _isPresentationPumpEnabled = value;
            UpdateNativeVisibility();
        }
    }

    public int PixelWidth => NumericMath.AtLeast(_lastNativeWidth, 1);

    public int PixelHeight => NumericMath.AtLeast(_lastNativeHeight, 1);

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DirectX viewport host is Windows-only.");
        }

        var width = NumericMath.AtLeast((int)Bounds.Width, 1);
        var height = NumericMath.AtLeast((int)Bounds.Height, 1);
        var hwnd = CreateWindowExW(
            0,
            "STATIC",
            string.Empty,
            WsChild | WsVisible | WsClipChildren | WsClipSiblings | SsNotify,
            0,
            0,
            width,
            height,
            parent.Handle,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"CreateWindowExW failed with {Marshal.GetLastWin32Error()}.");
        }

        _childHandle = hwnd;
        _lastNativeWidth = width;
        _lastNativeHeight = height;
        UpdateNativeVisibility();
        _presenter.Attach(hwnd, width, height);
        StfuLog.Write(
            StfuLogDomain.Viewport,
            "direct_host.attached",
            $"hwnd={hwnd} size={width}x{height}",
            StfuLogLevel.Debug,
            new Dictionary<string, object?>
            {
                ["hwnd"] = hwnd,
                ["width"] = width,
                ["height"] = height,
                ["isVisible"] = IsVisible,
                ["presenterAttached"] = _presenter.IsAttached
            });
        HookWndProc(hwnd);
        return new PlatformHandle(hwnd, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        StfuLog.Write(
            StfuLogDomain.Viewport,
            "direct_host.detaching",
            $"hwnd={_childHandle}",
            StfuLogLevel.Debug,
            new Dictionary<string, object?>
            {
                ["hwnd"] = _childHandle,
                ["isVisible"] = IsVisible,
                ["presenterAttached"] = _presenter.IsAttached
            });
        _presenter.Detach();
        if (_childHandle != IntPtr.Zero)
        {
            if (_previousWndProc != IntPtr.Zero)
            {
                SetWindowLongPtrW(_childHandle, GwlpWndProc, _previousWndProc);
                _previousWndProc = IntPtr.Zero;
            }

            DestroyWindow(_childHandle);
            _childHandle = IntPtr.Zero;
        }
    }

    private void UpdateNativeVisibility()
    {
        if (_childHandle == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(_childHandle, _isPresentationPumpEnabled ? SwShow : SwHide);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        ResizeNativeHost();
        return arranged;
    }

    private void ResizeNativeHost()
    {
        if (_childHandle == IntPtr.Zero)
        {
            return;
        }

        var width = NumericMath.AtLeast((int)Bounds.Width, 1);
        var height = NumericMath.AtLeast((int)Bounds.Height, 1);
        if (width == _lastNativeWidth && height == _lastNativeHeight)
        {
            return;
        }

        _lastNativeWidth = width;
        _lastNativeHeight = height;
        MoveWindow(_childHandle, 0, 0, width, height, true);
        _presenter.Resize(width, height);
    }

    private void HookWndProc(IntPtr hwnd)
    {
        if (_previousWndProc != IntPtr.Zero)
        {
            return;
        }

        _previousWndProc = SetWindowLongPtrW(hwnd, GwlpWndProc, Marshal.GetFunctionPointerForDelegate(_wndProc));
        if (_previousWndProc == IntPtr.Zero)
        {
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "directx.input_hook.failed",
                $"SetWindowLongPtrW failed: {Marshal.GetLastWin32Error()}",
                StfuLogLevel.Warning);
            return;
        }

        StfuLog.Write(
            StfuLogDomain.Viewport,
            "directx.input_hook.attached",
            $"hwnd=0x{hwnd.ToInt64():X}");
    }

    private IntPtr ChildWndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case WmNcHitTest:
                return new IntPtr(HtClient);

            case WmMouseActivate:
                return new IntPtr(MaActivate);

            case WmLButtonDown:
                LogNativeInputOnce("down");
                _isOrbiting = true;
                _lastPointerPosition = GetPoint(lParam);
                SetCapture(hwnd);
                RequestRender();
                return IntPtr.Zero;

            case WmMouseMove when _isOrbiting:
                if (((int)wParam & MkLButton) == 0)
                {
                    StopOrbit();
                    break;
                }

                MoveCamera(GetPoint(lParam));
                return IntPtr.Zero;

            case WmLButtonUp:
                StopOrbit();
                RequestRender();
                return IntPtr.Zero;

            case WmCaptureChanged:
                _isOrbiting = false;
                break;

            case WmMouseWheel:
                LogNativeInputOnce("wheel");
                ZoomCamera(GetWheelDelta(wParam));
                return IntPtr.Zero;
        }

        return _previousWndProc != IntPtr.Zero
            ? CallWindowProcW(_previousWndProc, hwnd, message, wParam, lParam)
            : DefWindowProcW(hwnd, message, wParam, lParam);
    }

    private void MoveCamera(Point position)
    {
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;
        if (NumericMath.Abs(delta.X) < 0.001 && NumericMath.Abs(delta.Y) < 0.001)
        {
            return;
        }

        if (_inputController.MoveCamera(delta, IsKeyDown(VkControl)))
        {
            RequestRender();
        }
    }

    private void ZoomCamera(int wheelDelta)
    {
        if (_inputController.ZoomCamera(wheelDelta / 120d))
        {
            RequestRender();
        }
    }

    private void RequestRender()
    {
        if (_renderQueued)
        {
            return;
        }

        _renderQueued = true;
        _requestRenderLogCounter++;
        var shouldLogRenderRequest = _requestRenderLogCounter % 120 == 0;

        if (Dispatcher.UIThread.CheckAccess())
        {
            _renderQueued = false;
            if (shouldLogRenderRequest)
            {
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "direct_host.request_render",
                    $"count={_requestRenderLogCounter}",
                    StfuLogLevel.Debug,
                    new Dictionary<string, object?>
                    {
                        ["count"] = _requestRenderLogCounter,
                        ["isVisible"] = IsVisible,
                        ["childHandle"] = _childHandle,
                        ["presenterAttached"] = _presenter.IsAttached
                    });
            }

            _requestFrame();
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _renderQueued = false;
            if (shouldLogRenderRequest)
            {
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "direct_host.request_render",
                    $"count={_requestRenderLogCounter}",
                    StfuLogLevel.Debug,
                    new Dictionary<string, object?>
                    {
                        ["count"] = _requestRenderLogCounter,
                        ["isVisible"] = IsVisible,
                        ["childHandle"] = _childHandle,
                        ["presenterAttached"] = _presenter.IsAttached
                    });
            }

            _requestFrame();
        }, DispatcherPriority.Render);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (!IsLeftButtonPressed(point))
        {
            return;
        }

        LogAvaloniaInputOnce("down");
        _isOrbiting = true;
        _lastPointerPosition = point.Position;
        e.Pointer.Capture(this);
        RequestRender();
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isOrbiting)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            _isOrbiting = false;
            e.Pointer.Capture(null);
            return;
        }

        MoveCamera(point.Position);
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isOrbiting = false;
        e.Pointer.Capture(null);
        RequestRender();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        LogAvaloniaInputOnce("wheel");
        ZoomCamera((int)(e.Delta.Y * 120));
        e.Handled = true;
    }

    private void StartRenderPump()
    {
        StfuLog.Write(
            StfuLogDomain.Viewport,
            "direct_host.shared_loop",
            "Direct GPU viewport host is attached; shared viewport frame loop owns pumping.",
            StfuLogLevel.Debug);
    }

    private void StopRenderPump()
    {
        if (_renderPump.IsEnabled)
        {
            _renderPump.Stop();
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "direct_host.pump_stop",
                "Direct GPU viewport render pump stopped.",
                StfuLogLevel.Debug);
        }
    }

    private void LogNativeInputOnce(string kind)
    {
        if (_loggedNativeInput)
        {
            return;
        }

        _loggedNativeInput = true;
        StfuLog.Write(StfuLogDomain.Viewport, "directx.input.native", kind);
    }

    private void LogAvaloniaInputOnce(string kind)
    {
        if (_loggedAvaloniaInput)
        {
            return;
        }

        _loggedAvaloniaInput = true;
        StfuLog.Write(StfuLogDomain.Viewport, "directx.input.avalonia", kind);
    }

    private void StopOrbit()
    {
        _isOrbiting = false;
        ReleaseCapture();
    }

    private static Point GetPoint(IntPtr lParam)
    {
        var value = lParam.ToInt64();
        return new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
    }

    private static int GetWheelDelta(IntPtr wParam)
    {
        return (short)((wParam.ToInt64() >> 16) & 0xFFFF);
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (GetKeyState(virtualKey) & 0x8000) != 0;
    }

    private static bool IsLeftButtonPressed(PointerPoint point)
    {
        return point.Properties.IsLeftButtonPressed ||
               point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
    }

    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const int WsClipSiblings = 0x04000000;
    private const int WsClipChildren = 0x02000000;
    private const int SsNotify = 0x00000100;
    private const int GwlpWndProc = -4;
    private const int WmNcHitTest = 0x0084;
    private const int WmMouseActivate = 0x0021;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmMouseWheel = 0x020A;
    private const int WmCaptureChanged = 0x0215;
    private const int HtClient = 1;
    private const int MaActivate = 1;
    private const int MkLButton = 0x0001;
    private const int VkControl = 0x11;
    private const int SwHide = 0;
    private const int SwShow = 5;

    private delegate IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        int exStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parentHandle,
        IntPtr menuHandle,
        IntPtr instanceHandle,
        IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, [MarshalAs(UnmanagedType.Bool)] bool repaint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr newLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProcW(IntPtr previousWndProc, IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCapture(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);
}
