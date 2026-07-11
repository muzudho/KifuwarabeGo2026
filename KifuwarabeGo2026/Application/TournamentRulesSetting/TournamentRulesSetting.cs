namespace KifuwarabeGo2026.Application.TournamentRulesSetting;

using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

/// <summary>
/// ［大会ルール設定］画面の処理
/// </summary>
public sealed class TournamentRulesSetting
{
    private readonly GoAppSession _session;
    private readonly TournamentRulesCatalog _catalog;
    private readonly Action _browseTournamentRules;

    public TournamentRulesSetting(GoAppSession session, TournamentRulesCatalog catalog, Action browseTournamentRules)
    {
        _session = session;
        _catalog = catalog;
        _browseTournamentRules = browseTournamentRules;
    }

    public void UpdateByKeyboard(KeyboardState keyboard)
    {
        UpdateBoardSizeByKeyboard(keyboard);

        if (keyboard.IsKeyDown(Keys.F5))
        {
            SaveCurrentTournamentRules();
        }
    }

    public bool TryHandleMouseClick(Point point)
    {
        if (_session.IsTournamentRulesSelectionDialogOpen)
        {
            return TryHandleTournamentRulesSelectionDialogClick(point);
        }

        if (GoScreenRenderer.GetTournamentRulesBrowseButtonHit(point))
        {
            _browseTournamentRules();
            return true;
        }

        if (GoScreenRenderer.GetRuleKindButtonHit(point) is { } ruleKind)
        {
            _session.ChangeRuleKind(ruleKind);
            return true;
        }

        if (GoScreenRenderer.GetBoardSizeButtonHit(point, _session.CurrentMode.Kind) is { } boardSize)
        {
            _session.ChangeBoardSize(boardSize);
            return true;
        }

        if (GoScreenRenderer.GetKomiStepButtonHit(point) is { } komiStep)
        {
            _session.ChangeKomi(komiStep);
            return true;
        }

        if (GoScreenRenderer.GetMainTimeStepButtonHit(point) is { } mainTimeStep)
        {
            _session.ChangeMainTime(mainTimeStep);
            return true;
        }

        if (GoScreenRenderer.GetSaveTournamentRulesButtonHit(point))
        {
            SaveCurrentTournamentRules();
            return true;
        }

        return false;
    }

    private bool TryHandleTournamentRulesSelectionDialogClick(Point point)
    {
        if (GoScreenRenderer.GetTournamentRulesSelectionDialogCloseButtonHit(point))
        {
            _session.CloseTournamentRulesSelectionDialog();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogPreviousPageButtonHit(point))
        {
            _session.MoveTournamentRulesSelectionPage(-1);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogNextPageButtonHit(point))
        {
            _session.MoveTournamentRulesSelectionPage(1);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogListItemHit(point, _session) is { } index)
        {
            _session.SelectTournamentRules(index);
            return true;
        }

        return true;
    }

    private void UpdateBoardSizeByKeyboard(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1))
        {
            _session.ChangeBoardSize(9);
        }
        else if (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2))
        {
            _session.ChangeBoardSize(13);
        }
        else if (keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3))
        {
            _session.ChangeBoardSize(19);
        }
    }

    private void SaveCurrentTournamentRules()
    {
        _catalog.Save(_session.CurrentTournamentRules);
        _session.MarkTournamentRulesSaved();
    }
}
