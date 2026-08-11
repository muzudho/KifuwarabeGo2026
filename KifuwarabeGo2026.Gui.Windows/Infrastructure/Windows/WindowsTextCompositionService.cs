namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

/// <summary>
/// Win32 の IME メッセージから未確定文字列を取得します。
/// </summary>
public sealed class WindowsTextCompositionService : ITextCompositionService, IDisposable
{
    private const uint WmImeStartComposition = 0x010D;
    private const uint WmImeEndComposition = 0x010E;
    private const uint WmImeComposition = 0x010F;
    private const int GwlpWndProc = -4;
    private const int GcsCompStr = 0x0008;
    private const int GcsCursorPos = 0x0080;
    private const uint SdlTextEditing = 0x302;
    private const uint SdlTextInput = 0x303;
    private const int SdlTextEditingTextOffset = 12;
    private const int SdlTextEditingStartOffset = 44;

    private WindowProcedureCallback? _windowProcedure;
    private nint _windowHandle;
    private nint _previousWindowProcedure;
    private TextCompositionDiagnostics _diagnostics;
    private readonly ConcurrentQueue<TextCompositionState> _pendingCompositionStates = new();
    private SdlEventFilterCallback? _sdlEventWatch;
    private bool _isSdlEventWatchAttached;

    public event Action<TextCompositionState>? CompositionChanged;
    public event Action<TextCompositionDiagnostics>? DiagnosticsChanged;

    public void Attach(nint windowHandle)
    {
        // DesktopGL の GameWindow.Handle は HWND ではなく SDL_Window* である。
        // SDL_GetWindowWMInfo で Win32 HWND に変換しないと、WM_IME_COMPOSITION は受け取れない。
        var nativeWindowHandle = GetWindowsWindowHandle(windowHandle);
        _diagnostics = _diagnostics with { IsSdlWindowResolved = nativeWindowHandle != 0 };
        PublishDiagnostics();
        if (nativeWindowHandle == 0 || nativeWindowHandle == _windowHandle)
        {
            if (nativeWindowHandle == 0)
                GuiOperationLog.App("IME composition unavailable", "SDL_GetWindowWMInfo did not return a Win32 HWND.");
            return;
        }

        Detach();
        // Detach は前回接続の診断状態を消すため、今回成功した SDL→HWND 解決状態を復元する。
        _diagnostics = new TextCompositionDiagnostics(IsSdlWindowResolved: true, IsWindowProcedureAttached: false);
        PublishDiagnostics();
        _windowHandle = nativeWindowHandle;
        _windowProcedure = WindowProcedure;
        _previousWindowProcedure = SetWindowLongPtr(
            _windowHandle,
            GwlpWndProc,
            Marshal.GetFunctionPointerForDelegate(_windowProcedure));

        if (_previousWindowProcedure == 0)
        {
            GuiOperationLog.App("IME composition unavailable", $"SetWindowLongPtrW failed; error={Marshal.GetLastWin32Error()}.");
            _windowProcedure = null;
            _windowHandle = 0;
        }
        else
        {
            GuiOperationLog.App("IME composition attached", $"SDL window converted to HWND 0x{_windowHandle:X}.");
        }
        _diagnostics = _diagnostics with { IsWindowProcedureAttached = _previousWindowProcedure != 0 };
        PublishDiagnostics();
        AttachSdlEventWatch();
    }

    public void Update()
    {
        while (_pendingCompositionStates.TryDequeue(out var state))
            CompositionChanged?.Invoke(state);
    }

    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }

    public void Detach()
    {
        DetachSdlEventWatch();
        if (_windowHandle != 0 && _previousWindowProcedure != 0)
            SetWindowLongPtr(_windowHandle, GwlpWndProc, _previousWindowProcedure);

        _windowHandle = 0;
        _previousWindowProcedure = 0;
        _windowProcedure = null;
        _diagnostics = TextCompositionDiagnostics.Empty;
        PublishDiagnostics();
    }

    private nint WindowProcedure(nint windowHandle, uint message, nint wParam, nint lParam)
    {
        try
        {
            switch (message)
            {
                case WmImeStartComposition:
                    GuiOperationLog.App("IME composition started");
                    Publish(TextCompositionState.Empty with { IsActive = true });
                    break;
                case WmImeComposition:
                    UpdateComposition(windowHandle, lParam);
                    break;
                case WmImeEndComposition:
                    Publish(TextCompositionState.Empty);
                    break;
            }
        }
        catch
        {
            // IME の失敗でゲームのウィンドウプロシージャを止めない。
        }

        return CallWindowProc(_previousWindowProcedure, windowHandle, message, wParam, lParam);
    }

    private void AttachSdlEventWatch()
    {
        if (_isSdlEventWatchAttached)
            return;

        // DesktopGL は Win32 のウィンドウプロシージャへ WM_IME_* を配送しない場合がある。
        // 一方で SDL_TEXTEDITING は SDL が IME から受け取った変換中文字列そのものである。
        // SDL_AddEventWatch のコールバックは別スレッドで呼ばれ得るため、ここではキューに積み、
        // Update() で GUI スレッドへ通知する。
        _sdlEventWatch = SdlEventWatch;
        SdlAddEventWatch(_sdlEventWatch, nint.Zero);
        _isSdlEventWatchAttached = true;
        GuiOperationLog.App("IME SDL event watch attached", "Listening for SDL_TEXTEDITING events.");
    }

    private void DetachSdlEventWatch()
    {
        if (!_isSdlEventWatchAttached || _sdlEventWatch is null)
            return;

        SdlDelEventWatch(_sdlEventWatch, nint.Zero);
        _isSdlEventWatchAttached = false;
        _sdlEventWatch = null;
        while (_pendingCompositionStates.TryDequeue(out _))
        {
        }
    }

    private int SdlEventWatch(nint userData, nint sdlEvent)
    {
        try
        {
            var eventType = unchecked((uint)Marshal.ReadInt32(sdlEvent));
            if (eventType == SdlTextEditing)
            {
                var text = ReadSdlUtf8Text(sdlEvent + SdlTextEditingTextOffset, 32);
                var caretIndex = Math.Clamp(Marshal.ReadInt32(sdlEvent, SdlTextEditingStartOffset), 0, text.Length);
                _pendingCompositionStates.Enqueue(new TextCompositionState(text, caretIndex, true));
                GuiOperationLog.App("IME SDL composition updated", $"characters={text.Length}; caret={caretIndex}.");
            }
            else if (eventType == SdlTextInput)
            {
                _pendingCompositionStates.Enqueue(TextCompositionState.Empty);
            }
        }
        catch (Exception ex)
        {
            GuiOperationLog.App("IME SDL event watch failed", ex.Message);
        }

        return 0;
    }

    private static string ReadSdlUtf8Text(nint textBuffer, int capacity)
    {
        var byteCount = 0;
        while (byteCount < capacity && Marshal.ReadByte(textBuffer, byteCount) != 0)
            byteCount++;

        return byteCount == 0 ? "" : Marshal.PtrToStringUTF8(textBuffer, byteCount) ?? "";
    }

    private void UpdateComposition(nint windowHandle, nint lParam)
    {
        if ((lParam.ToInt64() & GcsCompStr) == 0)
            return;

        var inputContext = ImmGetContext(windowHandle);
        if (inputContext == 0)
            return;

        try
        {
            var byteCount = ImmGetCompositionString(inputContext, GcsCompStr, nint.Zero, 0);
            var text = byteCount > 0
                ? ReadCompositionString(inputContext, byteCount)
                : "";
            var caretIndex = (lParam.ToInt64() & GcsCursorPos) != 0
                ? Math.Clamp(ImmGetCompositionString(inputContext, GcsCursorPos, nint.Zero, 0), 0, text.Length)
                : text.Length;
            Publish(new TextCompositionState(text, caretIndex, true));
            GuiOperationLog.App("IME composition updated", $"characters={text.Length}; caret={caretIndex}.");
        }
        finally
        {
            ImmReleaseContext(windowHandle, inputContext);
        }
    }

    private static string ReadCompositionString(nint inputContext, int byteCount)
    {
        var buffer = Marshal.AllocHGlobal(byteCount + sizeof(char));
        try
        {
            var copiedByteCount = ImmGetCompositionString(inputContext, GcsCompStr, buffer, byteCount);
            return copiedByteCount > 0 ? Marshal.PtrToStringUni(buffer, copiedByteCount / sizeof(char)) ?? "" : "";
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void Publish(TextCompositionState state) => _pendingCompositionStates.Enqueue(state);

    private void PublishDiagnostics() => DiagnosticsChanged?.Invoke(_diagnostics);

    private static nint GetWindowsWindowHandle(nint sdlWindowHandle)
    {
        if (sdlWindowHandle == 0)
            return 0;

        // SDL_SysWMinfo は SDL_version (3 bytes)、SDL_SYSWM_TYPE (4 bytes)、
        // その後にポインター境界で配置される union から成る。64-bit Windows では
        // union の先頭（offset 8）が SDL_SysWMinfo.info.win.window (HWND) である。
        const int sysWmInfoSize = 128;
        const int windowsWindowHandleOffset = 8;
        var info = Marshal.AllocHGlobal(sysWmInfoSize);
        try
        {
            for (var index = 0; index < sysWmInfoSize; index++)
                Marshal.WriteByte(info, index, 0);

            SdlGetVersion(out var version);
            Marshal.StructureToPtr(version, info, false);
            return SdlGetWindowWMInfo(sdlWindowHandle, info)
                ? Marshal.ReadIntPtr(info, windowsWindowHandleOffset)
                : 0;
        }
        finally
        {
            Marshal.FreeHGlobal(info);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedureCallback(nint windowHandle, uint message, nint wParam, nint lParam);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SdlEventFilterCallback(nint userData, nint sdlEvent);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SdlVersion
    {
        public byte Major;
        public byte Minor;
        public byte Patch;
    }

    [DllImport("SDL2.dll", EntryPoint = "SDL_GetVersion")]
    private static extern void SdlGetVersion(out SdlVersion version);

    [DllImport("SDL2.dll", EntryPoint = "SDL_GetWindowWMInfo")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SdlGetWindowWMInfo(nint window, nint info);

    [DllImport("SDL2.dll", EntryPoint = "SDL_AddEventWatch", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlAddEventWatch(SdlEventFilterCallback filter, nint userData);

    [DllImport("SDL2.dll", EntryPoint = "SDL_DelEventWatch", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlDelEventWatch(SdlEventFilterCallback filter, nint userData);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern nint CallWindowProc(nint previousWindowProcedure, nint windowHandle, uint message, nint wParam, nint lParam);

    [DllImport("imm32.dll", EntryPoint = "ImmGetContext")]
    private static extern nint ImmGetContext(nint windowHandle);

    [DllImport("imm32.dll", EntryPoint = "ImmReleaseContext")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmReleaseContext(nint windowHandle, nint inputContext);

    [DllImport("imm32.dll", EntryPoint = "ImmGetCompositionStringW")]
    private static extern int ImmGetCompositionString(nint inputContext, int index, nint buffer, int bufferLength);
}
