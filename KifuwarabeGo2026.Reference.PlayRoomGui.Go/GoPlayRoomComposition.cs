namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.Launching;

/// <summary>囲碁Play Roomの公開起動Handlerだけを登録する、Lobby非依存の構成点です。</summary>
public static class GoPlayRoomComposition
{
    public static InProcessPlayRoomLauncher CreateInProcessLauncher(
        Func<PlayRoomLaunchRequest, PlayRoomLaunchResult> launchMatch,
        Func<PlayRoomLaunchRequest, PlayRoomLaunchResult> launchBoardEditor,
        Func<PlayRoomLaunchRequest, PlayRoomLaunchResult> launchReview)
    {
        ArgumentNullException.ThrowIfNull(launchMatch);
        ArgumentNullException.ThrowIfNull(launchBoardEditor);
        ArgumentNullException.ThrowIfNull(launchReview);

        var launcher = new InProcessPlayRoomLauncher();
        launcher.Register(PlayRoomIds.Match, GameOasisOfficialNames.Go, launchMatch);
        launcher.Register(PlayRoomIds.BoardEditor, GameOasisOfficialNames.Go, launchBoardEditor);
        launcher.Register(PlayRoomIds.Review, GameOasisOfficialNames.Go, launchReview);
        return launcher;
    }
}
