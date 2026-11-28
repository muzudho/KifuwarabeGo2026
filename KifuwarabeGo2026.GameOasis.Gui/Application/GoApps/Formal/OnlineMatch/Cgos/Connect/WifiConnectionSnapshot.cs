namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Connect;

using System;

public enum WifiConnectionKind { Unavailable, Disconnected, Connected, Multiple }

/// <summary>Local association information. Does not describe the route to a remote host.</summary>
public sealed record WifiConnectionSnapshot(WifiConnectionKind Kind, string NetworkName = "")
{
    public static WifiConnectionSnapshot Unavailable { get; } = new(WifiConnectionKind.Unavailable);

    public bool AllowsConnection(string? expectedSsid) =>
        string.IsNullOrEmpty(expectedSsid) ||
        (Kind == WifiConnectionKind.Connected && string.Equals(NetworkName, expectedSsid, StringComparison.Ordinal));

    public string DisplayName => Kind switch
    {
        WifiConnectionKind.Connected => NetworkName,
        WifiConnectionKind.Multiple => $"Multiple: {NetworkName}",
        WifiConnectionKind.Disconnected => "Not connected",
        _ => "Unavailable",
    };
}
