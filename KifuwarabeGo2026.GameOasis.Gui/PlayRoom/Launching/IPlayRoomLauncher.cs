namespace KifuwarabeGo2026.PlayRoom.Launching;

using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

/// <summary>ロビーが具象プレイルームを知らずに起動を依頼する境界です。</summary>
public interface IPlayRoomLauncher
{
    PlayRoomLaunchResult Launch(PlayRoomLaunchRequest request);
}
