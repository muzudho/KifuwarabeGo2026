namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>CGOSの黒番・白番ログイン情報と、ポップアップ編集状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public string CgosBlackLoginName { get; private set; } = "";
    public string CgosBlackPassword { get; private set; } = "";
    public string CgosWhiteLoginName { get; private set; } = "";
    public string CgosWhitePassword { get; private set; } = "";
    public GoStone? ActiveCgosCredentialStone { get; private set; }
    public CgosPlayerCredentialField? ActiveCgosCredentialField { get; private set; }
    public int CgosCredentialCaretIndex { get; private set; }
    public int CgosCredentialSelectionStart { get; private set; }
    public int CgosCredentialSelectionLength { get; private set; }

    public void SetCgosPlayerCredentials(GoStone stone, string loginName, string password)
    {
        if (stone == GoStone.Black)
        {
            CgosBlackLoginName = loginName;
            CgosBlackPassword = password;
            return;
        }

        if (stone == GoStone.White)
        {
            CgosWhiteLoginName = loginName;
            CgosWhitePassword = password;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS credentials can be set only for black or white.");
    }

    public string GetCgosCredential(GoStone stone, CgosPlayerCredentialField field) =>
        (stone, field) switch
        {
            (GoStone.Black, CgosPlayerCredentialField.LoginName) => CgosBlackLoginName,
            (GoStone.Black, CgosPlayerCredentialField.Password) => CgosBlackPassword,
            (GoStone.White, CgosPlayerCredentialField.LoginName) => CgosWhiteLoginName,
            (GoStone.White, CgosPlayerCredentialField.Password) => CgosWhitePassword,
            _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS credentials can be read only for black or white."),
        };

    public void BeginCgosCredentialEdit(GoStone stone, CgosPlayerCredentialField field, int caretIndex)
    {
        ActiveCgosCredentialStone = stone;
        ActiveCgosCredentialField = field;
        CgosCredentialCaretIndex = Math.Clamp(caretIndex, 0, GetCgosCredential(stone, field).Length);
    }

    public void SetCgosCredential(GoStone stone, CgosPlayerCredentialField field, string text, int caretIndex)
    {
        var login = field == CgosPlayerCredentialField.LoginName ? text : GetCgosCredential(stone, CgosPlayerCredentialField.LoginName);
        var password = field == CgosPlayerCredentialField.Password ? text : GetCgosCredential(stone, CgosPlayerCredentialField.Password);
        SetCgosPlayerCredentials(stone, login, password);
        CgosCredentialCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
    }

    public void EndCgosCredentialEdit()
    {
        ActiveCgosCredentialStone = null;
        ActiveCgosCredentialField = null;
        CgosCredentialCaretIndex = 0;
    }

    public void SetCgosCredentialSelection(int start, int length) =>
        (CgosCredentialSelectionStart, CgosCredentialSelectionLength) = (start, length);
}
