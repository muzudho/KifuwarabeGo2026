namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Logging;
using StationeryUI.Windows;

/// <summary>既存GUIの通知契約とStationeryUIのWindows合成監視を接続します。</summary>
public sealed class WindowsTextCompositionService : ITextCompositionService, IDisposable
{
    private readonly WindowsCompositionObserver observer = new();
    public WindowsTextCompositionService()
    {
        observer.CompositionChanged += state => CompositionChanged?.Invoke(new(state.Text,state.CaretIndex,state.IsActive));
        observer.DiagnosticsChanged += state => DiagnosticsChanged?.Invoke(new(state.IsSdlWindowResolved,state.IsWindowProcedureAttached));
        observer.DiagnosticMessage += (message, details) => GuiOperationLog.App(message, details ?? "");
    }
    public bool SupportsDiagnosticAdornment => true;
    public event Action<TextCompositionState>? CompositionChanged;
    public event Action<TextCompositionDiagnostics>? DiagnosticsChanged;
    public void Attach(nint windowHandle) => observer.Attach(windowHandle);
    public void Update() => observer.Update();
    public void Detach() => observer.Detach();
    public void Dispose() => observer.Dispose();
}
