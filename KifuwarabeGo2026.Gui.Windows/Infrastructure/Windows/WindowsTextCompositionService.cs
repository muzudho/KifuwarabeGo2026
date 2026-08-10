namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using System;
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

    private WindowProcedureCallback? _windowProcedure;
    private nint _windowHandle;
    private nint _previousWindowProcedure;

    public event Action<TextCompositionState>? CompositionChanged;

    public void Attach(nint windowHandle)
    {
        if (windowHandle == 0 || windowHandle == _windowHandle)
            return;

        Detach();
        _windowHandle = windowHandle;
        _windowProcedure = WindowProcedure;
        _previousWindowProcedure = SetWindowLongPtr(
            windowHandle,
            GwlpWndProc,
            Marshal.GetFunctionPointerForDelegate(_windowProcedure));

        if (_previousWindowProcedure == 0)
        {
            _windowProcedure = null;
            _windowHandle = 0;
        }
    }

    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }

    public void Detach()
    {
        if (_windowHandle != 0 && _previousWindowProcedure != 0)
            SetWindowLongPtr(_windowHandle, GwlpWndProc, _previousWindowProcedure);

        _windowHandle = 0;
        _previousWindowProcedure = 0;
        _windowProcedure = null;
    }

    private nint WindowProcedure(nint windowHandle, uint message, nint wParam, nint lParam)
    {
        try
        {
            switch (message)
            {
                case WmImeStartComposition:
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

    private void Publish(TextCompositionState state) => CompositionChanged?.Invoke(state);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedureCallback(nint windowHandle, uint message, nint wParam, nint lParam);

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
