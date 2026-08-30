namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using System.Diagnostics;
using System.IO;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

/// <summary>同じGUI配布ディレクトリーにある囲碁Play Room Hostを解決します。</summary>
internal static class GoPlayRoomHostProcessStartInfoFactory
{
    private const string HostName = "KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows";

    public static ProcessStartInfo Create(PlayRoomLaunchRequest request)
    {
        if (request.RoomTypeId != PlayRoomIds.Match || request.GameId != GameOasisOfficialNames.Go)
            throw new InvalidOperationException($"No process Play Room Host is registered for {request.RoomTypeId}/{request.GameId}.");

        var executablePath = Path.Combine(AppContext.BaseDirectory, HostName + ".exe");
        if (File.Exists(executablePath)) return new ProcessStartInfo(executablePath);

        var assemblyPath = Path.Combine(AppContext.BaseDirectory, HostName + ".dll");
        if (File.Exists(assemblyPath))
            return CreateDotnetStartInfo(assemblyPath);

        var configurationDirectory = Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var repositoryDirectory = configurationDirectory?.Parent?.Parent?.Parent;
        if (configurationDirectory is not null && repositoryDirectory is not null)
        {
            var developmentAssemblyPath = Path.Combine(
                repositoryDirectory.FullName,
                HostName,
                "bin",
                configurationDirectory.Name,
                "net8.0",
                HostName + ".dll");
            if (File.Exists(developmentAssemblyPath)) return CreateDotnetStartInfo(developmentAssemblyPath);
        }

        return new ProcessStartInfo(executablePath);
    }

    private static ProcessStartInfo CreateDotnetStartInfo(string assemblyPath)
    {
        var dotnet = new ProcessStartInfo("dotnet");
        dotnet.ArgumentList.Add(assemblyPath);
        return dotnet;
    }
}
