namespace KifuwarabeGo2026.GameOasis.Application.Storage;

/// <summary>常用カタログ文書の物理配置をApplication層から隠す保存境界です。</summary>
public interface ICatalogDocumentStore
{
    bool Exists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
}
