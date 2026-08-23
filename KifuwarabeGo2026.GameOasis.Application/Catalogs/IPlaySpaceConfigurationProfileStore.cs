namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;

public interface IPlaySpaceConfigurationProfileStore
{
    string ListPath { get; }
    IReadOnlyList<PlaySpaceConfigurationProfile> Load();
    void Save(IEnumerable<PlaySpaceConfigurationProfile> profiles);
}
