namespace KifuwarabeGo2026.Gui.Application;

/// <summary>SGF自動保存の可否・結果表示と、対局結果の保存済み状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public bool IsSgfAutoSaveAvailable { get; private set; }
    public bool IsSgfAutoSaveEnabled { get; private set; }
    public string SgfAutoSaveStatus { get; private set; } = "";
    public bool IsLocalResultSgfSaved { get; private set; }
    public bool IsCgosResultSgfSaved { get; private set; }

    public void SetSgfAutoSaveAvailability(bool available)
    {
        IsSgfAutoSaveAvailable = available;
        if (!available)
        {
            IsSgfAutoSaveEnabled = false;
            SgfAutoSaveStatus = "";
        }
    }

    public void SetSgfAutoSaveEnabled(bool enabled)
    {
        IsSgfAutoSaveEnabled = IsSgfAutoSaveAvailable && enabled;
        SgfAutoSaveStatus = "";
    }

    public void SetSgfAutoSaveStatus(string status) => SgfAutoSaveStatus = status;
    public void SetLocalResultSgfSaved(bool saved) => IsLocalResultSgfSaved = saved;
    public void SetCgosResultSgfSaved(bool saved) => IsCgosResultSgfSaved = saved;
}
