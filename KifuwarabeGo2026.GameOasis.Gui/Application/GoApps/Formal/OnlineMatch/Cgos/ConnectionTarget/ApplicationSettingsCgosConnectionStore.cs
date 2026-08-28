namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;

using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using System.Collections.Generic;

/// <summary>既存の共有GUI設定をロビーエンジンのCGOSカタログ境界へ接続します。</summary>
public sealed class ApplicationSettingsCgosConnectionStore : ICgosConnectionProfileStore
{
    public static ApplicationSettingsCgosConnectionStore Instance { get; } = new();
    public string ListPath => ApplicationSettings.FilePath;
    public IReadOnlyList<CgosConnectionProfile> Load() => ApplicationSettings.Current.CgosConnections;
    public void Save(IEnumerable<CgosConnectionProfile> profiles) => ApplicationSettings.SaveCgosConnections(profiles);
}
