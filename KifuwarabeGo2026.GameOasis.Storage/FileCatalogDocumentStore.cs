namespace KifuwarabeGo2026.GameOasis.Storage;

using KifuwarabeGo2026.GameOasis.Application.Storage;
using System;
using System.IO;

/// <summary>UTF-8テキストとして常用カタログ文書を保存します。</summary>
public sealed class FileCatalogDocumentStore : ICatalogDocumentStore
{
    public bool Exists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);
        File.WriteAllText(path, content);
    }
}

/// <summary>既存GUIから段階移行中に利用する既定のStorage構成点です。</summary>
public static class CatalogDocumentStorage
{
    public static ICatalogDocumentStore Default { get; } = new FileCatalogDocumentStore();
    public static ICatalogPathProvider Paths { get; } = new DefaultCatalogPathProvider();
}
