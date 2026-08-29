namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public sealed partial class GoAppSession
{
    /// <summary>Lobby状態を参照せず、公開起動要求から解釈した囲碁開始Planを適用します。</summary>
    public bool TryApplyPlayRoomLaunchPlan(GoPlayRoomLaunchPlan plan, out string warning)
    {
        ArgumentNullException.ThrowIfNull(plan);
        warning = "";

        BoardSize = plan.BoardSize;
        _currentTournamentRules.BoardSize = plan.BoardSize;
        _currentTournamentRules.Komi = plan.Komi;
        var totalSeconds = checked((int)Math.Min(plan.MainTime.TotalSeconds, 999 * 3600 + 59 * 60 + 59));
        _currentTournamentRules.MainTimeMinutes = totalSeconds / 60;
        _currentTournamentRules.MainTimeSeconds = totalSeconds % 60;
        ClearBoard();
        CurrentTurn = plan.StartingPlayer;
        foreach (var setupStone in plan.SetupStones)
        {
            if (!_board.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone))
            {
                warning = $"The setup stone at ({setupStone.Point.X},{setupStone.Point.Y}) could not be applied.";
                ClearBoard();
                return false;
            }
        }

        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ClearBoardEditingHistory();
        ApplyResolvedLaunchParticipants(plan);
        return true;
    }

    private void ApplyResolvedLaunchParticipants(GoPlayRoomLaunchPlan plan)
    {
        if (plan.Participants.Count == 0)
            return;

        var connectionByRole = plan.PlayerConnections.ToDictionary(connection => connection.RoleId, StringComparer.OrdinalIgnoreCase);
        var engines = new List<GtpEngineProfile>();
        var entries = new List<EntryProfile>();
        foreach (var participant in plan.Participants)
        {
            var isComputer = string.Equals(participant.Kind, "computer", StringComparison.OrdinalIgnoreCase);
            var engineProfileId = participant.EngineProfileId;
            if (isComputer && connectionByRole.TryGetValue(participant.RoleId, out var connection))
            {
                var guiOptions = connection.EngineOptions is { MediaType: "application/json" } options
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(options.Content) ?? []
                    : [];
                engines.Add(new GtpEngineProfile
                {
                    Id = string.IsNullOrWhiteSpace(connection.EngineProfileId) ? $"launch-{connection.RoleId}" : connection.EngineProfileId,
                    DisplayName = connection.DisplayName,
                    ExecutablePath = connection.ExecutablePath,
                    WorkingDirectoryStr = connection.WorkingDirectory,
                    Arguments = connection.Arguments,
                    EnableGtpLog = connection.EnableGtpLog,
                    InitialPositionProfileId = connection.InitialPositionProfileId,
                    GuiOptions = guiOptions,
                });
                engineProfileId = engines[^1].Id;
            }

            entries.Add(new EntryProfile
            {
                Id = string.IsNullOrWhiteSpace(participant.EntryId) ? $"launch-{participant.RoleId}" : participant.EntryId,
                DisplayName = participant.DisplayName,
                Kind = isComputer ? EntryProfileKind.Computer : EntryProfileKind.Human,
                EngineProfileId = engineProfileId,
            });
        }

        if (engines.Count > 0)
            SetGtpEngineProfiles(engines);
        SetEntryProfiles(entries);
        foreach (var participant in plan.Participants)
        {
            var stone = participant.RoleId.Equals("black", StringComparison.OrdinalIgnoreCase)
                ? GoStone.Black
                : participant.RoleId.Equals("white", StringComparison.OrdinalIgnoreCase)
                    ? GoStone.White
                    : GoStone.Empty;
            if (stone != GoStone.Empty)
            {
                var entryId = string.IsNullOrWhiteSpace(participant.EntryId) ? $"launch-{participant.RoleId}" : participant.EntryId;
                TrySelectEntryProfile(stone, entryId);
            }
        }
    }
}
