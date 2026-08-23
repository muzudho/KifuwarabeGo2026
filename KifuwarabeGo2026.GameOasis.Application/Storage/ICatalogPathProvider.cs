namespace KifuwarabeGo2026.GameOasis.Application.Storage;

/// <summary>Provides physical locations used by the persistent catalog use cases.</summary>
public interface ICatalogPathProvider
{
    string GtpEngineListPath { get; }
    string EntryListPath { get; }
    string ClientIdentityListPath { get; }
    string? FindDevelopmentGtpEngineListPath();
}
