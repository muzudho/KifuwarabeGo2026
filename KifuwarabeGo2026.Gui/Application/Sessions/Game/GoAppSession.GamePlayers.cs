namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>ローカル対局のプレイヤー種別、人間名、名前編集状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public GoPlayerKind BlackPlayerKind { get; private set; } = GoPlayerKind.Human;
    public GoPlayerKind WhitePlayerKind { get; private set; } = GoPlayerKind.Computer;
    public string BlackHumanPlayerName { get; private set; } = "Black Player";
    public string WhiteHumanPlayerName { get; private set; } = "White Player";
    public GoStone? ActiveHumanPlayerNameStone { get; private set; }
    public string HumanPlayerNameDraft { get; private set; } = "";
    public int HumanPlayerNameCaretIndex { get; private set; }
    public int HumanPlayerNameSelectionStart { get; private set; }
    public int HumanPlayerNameSelectionLength { get; private set; }

    public void SetPlayerKind(GoStone stone, GoPlayerKind playerKind)
    {
        if (stone == GoStone.Black)
        {
            BlackPlayerKind = playerKind;
            return;
        }

        if (stone == GoStone.White)
        {
            WhitePlayerKind = playerKind;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player kind can be set only for black or white.");
    }

    public void BeginHumanPlayerNameEdit(GoStone stone, int caretIndex)
    {
        ActiveHumanPlayerNameStone = stone;
        HumanPlayerNameDraft = GetHumanPlayerName(stone);
        HumanPlayerNameCaretIndex = Math.Clamp(caretIndex, 0, HumanPlayerNameDraft.Length);
    }

    public void SetHumanPlayerNameDraft(string name, int caretIndex)
    {
        HumanPlayerNameDraft = name;
        HumanPlayerNameCaretIndex = Math.Clamp(caretIndex, 0, name.Length);
    }

    public void CommitHumanPlayerNameEdit()
    {
        if (ActiveHumanPlayerNameStone is not { } stone)
            return;

        var name = HumanPlayerNameDraft.Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = stone == GoStone.Black ? "Black Player" : "White Player";

        if (stone == GoStone.Black)
            BlackHumanPlayerName = name;
        else
            WhiteHumanPlayerName = name;

        CancelHumanPlayerNameEdit();
    }

    public void CancelHumanPlayerNameEdit()
    {
        ActiveHumanPlayerNameStone = null;
        HumanPlayerNameDraft = "";
        HumanPlayerNameCaretIndex = 0;
    }

    public string GetHumanPlayerName(GoStone stone) =>
        stone == GoStone.Black ? BlackHumanPlayerName : WhiteHumanPlayerName;

    /// <summary>対局画面と棋譜へ表示する対局者名を取得します。</summary>
    public string GetLocalPlayerName(GoStone stone)
    {
        if (GetPlayerKind(stone) == GoPlayerKind.Human)
            return GetHumanPlayerName(stone);

        var index = stone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex;
        return index >= 0 && index < _gtpEngineProfiles.Count
            ? _gtpEngineProfiles[index].DisplayName
            : "No engine";
    }

    public GoPlayerKind GetPlayerKind(GoStone stone) => stone switch
    {
        GoStone.Black => BlackPlayerKind,
        GoStone.White => WhitePlayerKind,
        _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player kind can be read only for black or white."),
    };

    public void SetHumanPlayerNameSelection(int start, int length) =>
        (HumanPlayerNameSelectionStart, HumanPlayerNameSelectionLength) = (start, length);
}
