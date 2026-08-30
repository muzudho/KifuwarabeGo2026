namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using KifuwarabeGo2026.GameOasis.Gui;
using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.GameOasis;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Application.Storage;
using KifuwarabeGo2026.GameOasis.Storage;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.LobbyGui.Application;
using KifuwarabeGo2026.LobbyEngine;
using KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Integration;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls;
using KifuwarabeGo2026.GameOasis.Gui.Sgf;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Capabilities;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Sgf;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Strategies;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;
using KifuwarabeGo2026.Reference.PlayRoomGui.Common;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;
using KifuwarabeGo2026.Reference.PlayerEngine;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.FormalAdapter.Cgos.Observability;
using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal static class PortabilityChecks
{
    private static readonly string[] ForbiddenAssemblyReferences =
    [
        "System.Drawing.Common",
        "System.Windows.Forms",
    ];

    public static void Run()
    {
        var coreAssembly = typeof(Game1).Assembly;
        var gtpExtensionsAssembly = typeof(GtpExtensionsAssembly).Assembly;
        var gtpFormalAdapterAssembly = typeof(IGtpCommandSession).Assembly;
        var cgosFormalAdapterAssembly = typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Protocol.CgosServerMessage).Assembly;
        var sgfFormalAdapterAssembly = typeof(KifuwarabeGo2026.FormalAdapter.Sgf.Document.SgfDocument).Assembly;
        var gtpCommunicationAssembly = typeof(KifuwarabeGtpPlayerProtocol).Assembly;
        var goMatchAssembly = typeof(MatchSession).Assembly;
        var conciergeAssembly = typeof(GameOasisConcierge).Assembly;
        var gameOasisApplicationAssembly = typeof(ICatalogDocumentStore).Assembly;
        var gameOasisStorageAssembly = typeof(FileCatalogDocumentStore).Assembly;

        VerifyTargetFramework(coreAssembly);
        VerifyAssemblyReferences(coreAssembly);
        VerifyNoPlatformInvokes(coreAssembly);
        VerifyGtpExtensionsAssembly(gtpExtensionsAssembly, gtpCommunicationAssembly);
        VerifyGtpPlayerEngineSeparation(gtpFormalAdapterAssembly, gtpCommunicationAssembly);
        Require(gtpFormalAdapterAssembly.GetName().Name == "KifuwarabeGo2026.FormalAdapter.Gtp" &&
                typeof(KifuwarabeGo2026.FormalAdapter.Gtp.Go.GtpCoordinate).Assembly == gtpFormalAdapterAssembly,
            "GTP protocol primitives and Go coordinate conversion must be owned by FormalAdapter.Gtp.");
        Require(typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Protocol.CgosServerMessageParser).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Protocol.CgosClientCommandFormatter).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Client.CgosNetworkSession).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.PlayerEngine.CgosPlayerStateMachine).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.GameMasterEngine.CgosAdminStateMachine).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Observability.CgosNotificationJsonLines).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Go.CgosGoEventProjector).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Compatibility.CgosLegacyLogNotificationAdapter).Assembly == cgosFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Cgos.Compatibility.CgosLegacyRuntimeLogAdapter).Assembly == cgosFormalAdapterAssembly,
            "CGOS protocol, sessions, state machines, notifications, Go projection, and legacy compatibility must be owned by FormalAdapter.Cgos.");
        VerifyCgosStructuredObservation();
        Require(typeof(KifuwarabeGo2026.FormalAdapter.Sgf.Go.SgfCoordinate).Assembly == sgfFormalAdapterAssembly &&
                typeof(KifuwarabeGo2026.FormalAdapter.Sgf.Go.SgfGoGameRecordConverter).Assembly == sgfFormalAdapterAssembly,
            "SGF Go coordinates and neutral game projection must be owned by FormalAdapter.Sgf.");
        Require(typeof(GtpEngineClient).Assembly == gtpFormalAdapterAssembly &&
                typeof(ProcessGtpCommandTransport).Assembly == gtpFormalAdapterAssembly &&
                typeof(GtpOptionSchemaDocument).Assembly == gtpFormalAdapterAssembly,
            "GTP client, process transport, and option documents must be owned by FormalAdapter.Gtp.");
        Require(typeof(GtpInitialPositionExecutionHost).Assembly == gtpExtensionsAssembly &&
                typeof(GtpInitialPositionSgfFile).Assembly == gtpExtensionsAssembly,
            "Go initial-position GTP adaptation must be owned by Reference.PlayerEngine.Go.GtpExtensions.");
        VerifyGtpExtensionsInitialPositionPlanning();
        VerifyGtpCapabilityProbe();
        VerifyStandardHandicapStrategies();
        VerifyLoadSgfStrategyAndTemporaryFile();
        VerifyKfaToKfwConversion();
        VerifySgfCommentEditing();
        VerifyInitialPositionConcierge();
        VerifyInitialPositionConciergeGuiModel();
        VerifyInitialPositionEngineProfiles();
        VerifyGoAppEngineSelectionCompatibility();
        VerifyKifuwarabeAtomicSetupStrategy();
        VerifyGoMatchAssembly(goMatchAssembly);
        VerifyGameAgnosticConciergeAssembly(conciergeAssembly);
        VerifyGameOasisCatalogLayering(gameOasisApplicationAssembly, gameOasisStorageAssembly);
        VerifyLobbyGuiBoundary();
        VerifyGoPlayRoomGuiBoundary();
        VerifyGuiMatchIntegration();
        VerifyGtpMatchAdapter();
        VerifyPortableFallbacks();
        VerifyScoreAxisScaling();
        VerifyCommentNavigation();
        VerifyCatalogOrderEditor();
        VerifyGameOasisProfilePolicies();
        VerifyCgosConnectionOrder();
        VerifyOptionalCgosInputs();
        VerifyTextBoxEditing();
        VerifyInitialWindowLayout();
        VerifyDefaultCgosConnection();
        VerifyCgosResultReviewRecord();
        VerifyCgosHumanMoveReflection();
        VerifyCgosPracticeUnexpectedGameState();
        VerifyLocalMatchSgfFileName();
        VerifyTournamentRulesJsonCompatibility();
        VerifyComposition();
        VerifyGameOasisGuiComposition();
        VerifyGameOasisPlayerParticipation();
    }

    private static void VerifyGoPlayRoomGuiBoundary()
    {
        var assembly = typeof(GoPlayRoomViewState).Assembly;
        Require(assembly.GetName().Name == "KifuwarabeGo2026.Reference.PlayRoomGui.Go",
            "The Go Play Room view state must be owned by Reference.PlayRoomGui.Go.");

        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        Require(!references.Contains("KifuwarabeGo2026.GameOasis.Gui") &&
                !references.Contains("KifuwarabeGo2026.LobbyGui") &&
                !references.Any(reference => reference?.StartsWith("MonoGame", StringComparison.Ordinal) == true),
            "The Go Play Room view state must not depend on the compatibility GUI, Lobby GUI, or MonoGame.");

        var monoGameAssembly = typeof(GoBoardPrimitiveRenderer).Assembly;
        var monoGameReferences = monoGameAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        Require(monoGameAssembly.GetName().Name == "KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame" &&
                monoGameReferences.Contains("KifuwarabeGo2026.Reference.PlayRoomGui.Go") &&
                monoGameReferences.Contains("MonoGame.Framework") &&
                !monoGameReferences.Contains("KifuwarabeGo2026.GameOasis.Gui") &&
                !monoGameReferences.Contains("KifuwarabeGo2026.LobbyGui"),
            "The MonoGame Go board renderer must depend inward on the Go presentation model without depending on the compatibility GUI or Lobby GUI.");
        Require(typeof(GoBoardPrimitiveRenderer).GetMethod(nameof(GoBoardPrimitiveRenderer.DrawStones)) is not null &&
                typeof(GoBoardPrimitiveRenderer).GetMethod(nameof(GoBoardPrimitiveRenderer.DrawKoMarker)) is not null &&
                typeof(GoBoardPrimitiveRenderer).GetMethod(nameof(GoBoardPrimitiveRenderer.DrawLastMoveMarker)) is not null &&
                typeof(GoBoardPrimitiveRenderer).GetMethod(nameof(GoBoardPrimitiveRenderer.DrawHoverStone)) is not null,
            "Stone, ko, last-move, and hover MonoGame drawing must be owned by the Go board primitive renderer.");
        Require(typeof(IDisposable).IsAssignableFrom(typeof(GoBoardPrimitiveRenderer)) &&
                typeof(GoBoardPrimitiveRenderer).GetMethod(nameof(GoBoardPrimitiveRenderer.DrawBoardSurface)) is not null &&
                typeof(GoBoardPrimitiveRenderer).GetMethod(nameof(GoBoardPrimitiveRenderer.DrawBoardFrameHighlights)) is not null,
            "The Go board primitive renderer must own its MonoGame resources, board surface, and frame drawing.");

        var state = GoPlayRoomViewState.Capture(
            GoPlayRoomActivity.Reviewing,
            3,
            (x, y) => (x, y) switch
            {
                (0, 0) => GoStone.Black,
                (2, 2) => GoStone.White,
                _ => GoStone.Empty,
            },
            GoStone.White,
            2,
            1,
            0,
            new GoPoint(1, 1),
            null,
            "",
            1,
            3,
            new GoPoint(2, 2));

        Require(state.Activity == GoPlayRoomActivity.Reviewing &&
                state.GetStone(0, 0) == GoStone.Black &&
                state.GetStone(2, 2) == GoStone.White &&
                state.CurrentTurn == GoStone.White &&
                state.BlackCaptures == 1 &&
                state.KoPoint == new GoPoint(1, 1) &&
                state.TimelineIndex == 1 &&
                state.TimelineMaximum == 3,
            "The Go Play Room view state must capture board, turn, result, and timeline display data.");

        var geometry = GoBoardGeometry.Create(19, new GoBoardViewport(88, 84, 912, 912));
        Require(geometry.Start == new GoBoardScreenPoint(138, 134) && geometry.Cell == 812f / 18f,
            "The Go board geometry must preserve the existing 19x19 board layout.");
        Require(geometry.TryGetIntersection(geometry.GetScreenPoint(new GoPoint(3, 4)), out var hit) &&
                hit == new GoPoint(3, 4),
            "The Go board geometry must map an intersection center back to its board coordinate.");
        Require(!geometry.TryGetIntersection(new GoBoardScreenPoint(110, 110), out _),
            "The Go board geometry must reject clicks outside the intersection hit radius.");

        var staticPresentation = GoBoardStaticPresenter.Create(
            geometry,
            new GoBoardViewport(54, 50, 980, 980));
        Require(staticPresentation.Lines.Count == 38 &&
                staticPresentation.Lines.Count(line => line.IsOuter) == 4 &&
                staticPresentation.Stars.Count == 9 &&
                staticPresentation.Stars.Any(star => star.Intersection == new GoPoint(9, 9)) &&
                staticPresentation.Coordinates.Count == 38 &&
                staticPresentation.Coordinates[0].Text == "A" &&
                staticPresentation.Coordinates[1].Text == "19" &&
                staticPresentation.Coordinates[14].Text == "H" &&
                staticPresentation.Coordinates[16].Text == "J",
            "The Go static board presenter must create lines, star points, and GTP-style coordinates without MonoGame types.");
        var nineBoardStaticPresentation = GoBoardStaticPresenter.Create(
            GoBoardGeometry.Create(9, new GoBoardViewport(0, 0, 400, 400)),
            new GoBoardViewport(0, 0, 480, 480));
        Require(nineBoardStaticPresentation.Stars.Count == 5 &&
                nineBoardStaticPresentation.Coordinates.Last().Text == "1",
            "The Go static board presenter must preserve the 9x9 star and row-label layout.");

        var presentationGeometry = GoBoardGeometry.Create(3, new GoBoardViewport(0, 0, 200, 200));
        var presentation = GoBoardPresenter.Create(state, presentationGeometry, [new GoPoint(1, 0)]);
        Require(presentation.Stones.Count == 2 &&
                presentation.Stones.Any(stone => stone.Intersection == new GoPoint(0, 0) &&
                                                 stone.Stone == GoStone.Black &&
                                                 !stone.UseWhiteboardStyle) &&
                presentation.Stones.Any(stone => stone.Intersection == new GoPoint(2, 2) &&
                                                 stone.Stone == GoStone.White) &&
                presentation.KoMarker?.Intersection == new GoPoint(1, 1) &&
                presentation.LastMoveMarker?.Intersection == new GoPoint(2, 2) &&
                presentation.SuperKoMarkers.Single() is { Intersection: { X: 1, Y: 0 }, Radius: 15f, Label: "S-KO", LabelScale: 0.24f },
            "The Go board presenter must create framework-neutral stone, ko, last-move, and super-ko visuals.");

        var hoverTarget = new GoPoint(1, 0);
        var smallGeometry = GoBoardGeometry.Create(3, new GoBoardViewport(0, 0, 200, 200));
        var hoverPointer = smallGeometry.GetScreenPoint(hoverTarget);
        var playingState = GoPlayRoomViewState.Capture(
            GoPlayRoomActivity.Playing,
            3,
            state.GetStone,
            GoStone.White,
            2,
            1,
            0,
            new GoPoint(1, 1),
            null,
            "",
            2,
            2,
            new GoPoint(2, 2));
        var forbiddenFrame = GoBoardFrameCoordinator.Create(
            playingState,
            smallGeometry,
            [hoverTarget],
            hoverPointer,
            canAcceptHumanMove: true,
            point => point == hoverTarget);
        Require(forbiddenFrame.Geometry == smallGeometry &&
                forbiddenFrame.Board.SuperKoMarkers.Single().Intersection == hoverTarget &&
                forbiddenFrame.Hover is null,
            "The Go board frame coordinator must combine board visuals and forbidden-point hover rules without GUI framework types.");
        Require(GoBoardPresenter.TryCreateMoveHover(
                    playingState,
                    smallGeometry,
                    hoverPointer,
                    true,
                    _ => false,
                    out var hover) &&
                hover.Intersection == hoverTarget &&
                hover.Stone == GoStone.White,
            "The Go board presenter must create a current-turn hover visual for an available intersection.");
        Require(!GoBoardPresenter.TryCreateMoveHover(
                    playingState,
                    smallGeometry,
                    hoverPointer,
                    true,
                    point => point == hoverTarget,
                    out _),
            "The Go board presenter must reject a hover visual for a supplied forbidden intersection.");
        Require(!GoBoardPresenter.TryCreateMoveHover(
                    playingState,
                    smallGeometry,
                    smallGeometry.GetScreenPoint(new GoPoint(1, 1)),
                    true,
                    _ => false,
                    out _),
            "The Go board presenter must reject a hover visual on the ko point.");

        var variationState = GoPlayRoomViewState.Capture(
            GoPlayRoomActivity.VariationEditing,
            2,
            (x, y) => x == 0 && y == 0 ? GoStone.Black : GoStone.Empty,
            GoStone.Black,
            0,
            0,
            0,
            null,
            null,
            "",
            0,
            0);
        var variationPresentation = GoBoardPresenter.Create(
            variationState,
            GoBoardGeometry.Create(2, new GoBoardViewport(0, 0, 200, 200)));
        Require(variationPresentation.Stones.Single().UseWhiteboardStyle,
            "The Go board presenter must retain the variation-editor stone style without using MonoGame types.");

        var protocolBoard = new GuiBoardView(
            new GameOasisSessionId("gui-board-adapter"),
            new PlaySpaceTypeId(GameOasisOfficialNames.Go),
            4,
            9,
            [new GuiBoardPoint(0, 0)],
            [new GuiBoardPoint(1, 0)],
            "black",
            2,
            1,
            new GuiBoardPoint(2, 0),
            false,
            null,
            [],
            [],
            [
                new GuiBoardMove("black", "play", new GuiBoardPoint(0, 0), null),
                new GuiBoardMove("white", "play", new GuiBoardPoint(1, 0), null),
            ],
            null,
            null,
            null);
        var protocolView = GuiBoardViewAdapter.Create(protocolBoard, GoPlayRoomActivity.BoardEditing);
        var protocolPresentation = GoBoardPresenter.Create(
            protocolView,
            GoBoardGeometry.Create(9, new GoBoardViewport(0, 0, 400, 400)));
        Require(protocolView.Activity == GoPlayRoomActivity.BoardEditing &&
                protocolView.CurrentTurn == GoStone.Black &&
                protocolView.BlackCaptures == 2 &&
                protocolView.WhiteCaptures == 1 &&
                protocolPresentation.Stones.Count == 2 &&
                protocolPresentation.KoMarker?.Intersection == new GoPoint(2, 0) &&
                protocolPresentation.LastMoveMarker?.Intersection == new GoPoint(1, 0),
            "A Protocol G board must project through the Go Play Room state and presenter boundary.");
    }

    private static void VerifyGtpPlayerEngineSeparation(Assembly gtpFormalAdapterAssembly, Assembly playerEngineAdapterAssembly)
    {
        var referenceServerAssembly = typeof(KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.GtpEngine).Assembly;
        Require(playerEngineAdapterAssembly.GetName().Name == "KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine" &&
                typeof(KifuwarabeGtpPlayerProtocol).Assembly == playerEngineAdapterAssembly,
            "The Protocol P adapter for external GTP engines must be owned by FormalAdapter.Gtp.PlayerEngine.");
        Require(referenceServerAssembly.GetName().Name == "KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp",
            "The reference Go GTP server must be owned by Reference.PlayerEngine.Go.Gtp.");

        var adapterReferences = playerEngineAdapterAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        var serverReferences = referenceServerAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        Require(adapterReferences.Contains(gtpFormalAdapterAssembly.GetName().Name) &&
                !adapterReferences.Contains(referenceServerAssembly.GetName().Name),
            "The GTP player adapter must use the GTP primitives without depending on the reference server.");
        Require(!serverReferences.Contains(playerEngineAdapterAssembly.GetName().Name),
            "The reference GTP server must not depend on the Protocol P adapter for external engines.");

        var repositoryRoot = FindRepositoryRoot();
        Require(!HasRetiredProjectSources(Path.Combine(repositoryRoot, "KifuwarabeGo2026.Reference.Communication.Gtp")) &&
                !HasRetiredProjectSources(Path.Combine(repositoryRoot, "KifuwarabeGo2026.Reference.Communication.Gtp.Host")),
            "Retired Reference.Communication.Gtp project files or sources must not return.");

        var solution = File.ReadAllText(Path.Combine(repositoryRoot, "KifuwarabeGo2026.slnx"));
        Require(solution.Contains("KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine", StringComparison.Ordinal) &&
                solution.Contains("KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host", StringComparison.Ordinal) &&
                !solution.Contains("KifuwarabeGo2026.Reference.Communication.Gtp", StringComparison.Ordinal),
            "The solution must contain only the post-migration GTP adapter, server, and host project names.");
    }

    private static bool HasRetiredProjectSources(string projectDirectory)
    {
        if (!Directory.Exists(projectDirectory)) return false;
        return Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Any(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                         path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static void VerifyGameOasisCatalogLayering(Assembly applicationAssembly, Assembly storageAssembly)
    {
        var applicationReferences = applicationAssembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet(StringComparer.Ordinal);
        var storageReferences = storageAssembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet(StringComparer.Ordinal);
        Require(!applicationReferences.Contains("KifuwarabeGo2026.GameOasis.Storage"),
            "GameOasis.Application must not depend on its Storage implementation.");
        Require(storageReferences.Contains("KifuwarabeGo2026.GameOasis.Application"),
            "GameOasis.Storage must implement the Application-owned persistence boundary.");
        Require(typeof(GtpEngineCatalog).Assembly == applicationAssembly,
            "The persistent GTP engine catalog use cases must be owned by GameOasis.Application.");
        Require(typeof(EntryCatalog).Assembly == applicationAssembly &&
                typeof(ClientIdentityCatalog).Assembly == applicationAssembly,
            "Persistent entry and client identity catalog use cases must be owned by GameOasis.Application.");
        Require(typeof(CgosConnectionCatalog).Assembly == applicationAssembly &&
                typeof(CgosConnectionProfile).Assembly == applicationAssembly,
            "Persistent CGOS connection profiles and catalog use cases must be owned by GameOasis.Application.");
        Require(typeof(PlaySpaceConfigurationCatalog).Assembly == applicationAssembly &&
                typeof(PlaySpaceConfigurationProfile).Assembly == applicationAssembly,
            "Named play-space configuration catalog use cases must be owned by GameOasis.Application.");
        Require(typeof(ICatalogPathProvider).Assembly == applicationAssembly,
            "The catalog path boundary must be owned by GameOasis.Application.");

        var paths = CatalogDocumentStorage.Paths;
        Require(paths.EntryListPath.EndsWith(Path.Combine("Players", "player-list.json"), StringComparison.Ordinal) &&
                paths.ClientIdentityListPath.EndsWith(Path.Combine("Targets", "target-list.json"), StringComparison.Ordinal),
            "GameOasis.Storage must provide the compatible entry and client identity catalog locations.");
    }

    private static void VerifyLobbyGuiBoundary()
    {
        Require(typeof(LobbyGuiController).Namespace == "KifuwarabeGo2026.LobbyGui.Application" &&
                typeof(ILobbyGuiCommands).Namespace == "KifuwarabeGo2026.LobbyGui.Application" &&
                typeof(LobbyGuiController).Assembly.GetName().Name == "KifuwarabeGo2026.LobbyGui",
            "Stable lobby GUI boundaries must be owned by the LobbyGui assembly and namespace.");
        var engine = new FakeLobbyEngine();
        ILobbyGuiCommands controller = new LobbyGuiController(engine, ApplicationSettings.FilePath);
        var state = controller.LoadViewState();

        Require(state.GtpEngines.Count == 1 &&
                state.Entries.Count == 1 && state.ClientIdentities.Count == 1 &&
                state.CgosConnections.Count == 1,
            "Lobby GUI state must project all start-before catalogs through one boundary.");
        Require(state.ApplicationSettingsPath == ApplicationSettings.FilePath &&
                state.GtpEngineSettingsPath == "/catalog/engines.json",
            "Lobby GUI state must expose settings paths without exposing storage implementations.");

        controller.SaveGtpEngines(state.GtpEngines);
        controller.SaveEntries(state.Entries);
        controller.SaveClientIdentities(state.ClientIdentities);
        controller.SaveEntriesAndClientIdentities(state.Entries, state.ClientIdentities);
        controller.SaveCgosConnections(state.CgosConnections);
        Require(engine.SaveCallCount == 5,
            "Lobby GUI commands must delegate persistence to ILobbyEngine.");

        var propertyTypes = typeof(LobbyViewState).GetProperties()
            .Select(property => property.PropertyType.FullName ?? property.PropertyType.Name)
            .ToArray();
        Require(!propertyTypes.Any(type =>
                type.Contains("MonoGame", StringComparison.Ordinal) ||
                type.Contains("Microsoft.Xna", StringComparison.Ordinal) ||
                type.Contains("GoBoard", StringComparison.Ordinal) ||
                type.Contains("GoGameRecord", StringComparison.Ordinal) ||
                type.Contains("CgosConnectionProcess", StringComparison.Ordinal)),
            "Lobby view state must not expose drawing, board, record, or process types.");
        Require(typeof(LobbyGuiController).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .All(field => !field.FieldType.Name.Contains("GoAppSession", StringComparison.Ordinal)),
            "Lobby GUI controller must not own the combined play-room session.");

        var navigation = new LobbyNavigationController();
        Require(navigation.CurrentPage == LobbyPage.Home,
            "Lobby navigation must start on the Home page.");
        navigation.OpenGameOasis();
        Require(navigation.CurrentPage == LobbyPage.GameOasis,
            "Lobby navigation must own the Game Oasis page transition.");
        Require(navigation.TryOpenCasualApp(0) && navigation.CurrentPage == LobbyPage.CaptureGame,
            "Lobby navigation must map the first casual app to Capture Game.");
        Require(navigation.TryOpenCasualApp(1) && navigation.CurrentPage == LobbyPage.Tsumego,
            "Lobby navigation must map the second casual app to Tsumego.");
        Require(navigation.TryOpenCasualApp(2) && navigation.CurrentPage == LobbyPage.NextMove,
            "Lobby navigation must map the third casual app to Next Move.");
        Require(!navigation.TryOpenCasualApp(3) && navigation.CurrentPage == LobbyPage.NextMove,
            "Lobby navigation must reject an unknown casual app without changing pages.");
        navigation.OpenHome();
        Require(navigation.CurrentPage == LobbyPage.Home,
            "Lobby navigation must return to Home without a Play Room session.");
        Require(typeof(LobbyNavigationController).Assembly == typeof(LobbyGuiController).Assembly,
            "Lobby page state and transitions must be owned by the LobbyGui assembly.");

        var home = LobbyHomePresenter.Create();
        Require(home.Items.Count == 6 &&
                home.GetItem(LobbyHomeTarget.LocalMatch).Caption == "PLAY / REVIEW" &&
                home.GetItem(LobbyHomeTarget.GamePlatform).Accent == LobbyHomeAccent.Platform,
            "Lobby Home presenter must own stable menu labels, captions, and semantic accents.");
        Require(home.GetHint(LobbyHomeTarget.FormalApps).BodyLines.Count == 5 &&
                home.GetHint(LobbyHomeTarget.EntryProfiles).Heading == "ENTRY PROFILES とは？",
            "Lobby Home presenter must own section and menu guidance independently of its renderer.");
        Require(typeof(LobbyHomePresentation).GetProperties()
                .Select(property => property.PropertyType.FullName ?? property.PropertyType.Name)
                .All(type => !type.Contains("MonoGame", StringComparison.Ordinal) &&
                             !type.Contains("Microsoft.Xna", StringComparison.Ordinal)),
            "Lobby Home presentation must not expose MonoGame drawing types.");

        var inputNavigation = new LobbyNavigationController();
        var input = new LobbyHomeInputCoordinator(inputNavigation);
        Require(input.Activate(LobbyHomeTarget.LocalMatch) == LobbyHomeAction.OpenLocalMatch &&
                input.CurrentPage == LobbyPage.Home,
            "Lobby Home input must return the Local Match intent without entering Play Room state.");
        Require(input.Activate(LobbyHomeTarget.EngineProfiles) == LobbyHomeAction.ManageEngineProfiles,
            "Lobby Home input must return the engine-management intent.");
        Require(input.Activate(LobbyHomeTarget.GamePlatform) == LobbyHomeAction.OpenGameOasis &&
                input.CurrentPage == LobbyPage.GameOasis,
            "Lobby Home input must own the Game Oasis page transition.");
        Require(input.Activate(LobbyHomeTarget.CaptureGame) == LobbyHomeAction.None,
            "Lobby Home input must reject Home targets after leaving Home.");
        input.OpenHome();
        Require(input.Activate(LobbyHomeTarget.CaptureGame) == LobbyHomeAction.OpenCaptureGame &&
                input.CurrentPage == LobbyPage.CaptureGame,
            "Lobby Home input must own the Capture Game page transition.");
        Require(typeof(LobbyHomeInputCoordinator).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .All(field => !field.FieldType.FullName!.Contains("MonoGame", StringComparison.Ordinal) &&
                              !field.FieldType.FullName.Contains("Microsoft.Xna", StringComparison.Ordinal)),
            "Lobby Home input coordinator must not depend on MonoGame hit-test types.");

        var title = TitleScreen.Default;
        Require(title.GetHomeTargetHit(title.LocalMatchButton.Bounds.Center) == LobbyHomeTarget.LocalMatch &&
                title.GetHomeTargetHit(title.CgosClientButton.Bounds.Center) == LobbyHomeTarget.OnlineMatch &&
                title.GetHomeTargetHit(title.EngineProfilesButton.Bounds.Center) == LobbyHomeTarget.EngineProfiles &&
                title.GetHomeTargetHit(title.EntryProfilesButton.Bounds.Center) == LobbyHomeTarget.EntryProfiles &&
                title.GetHomeTargetHit(title.GameOasisButton.Bounds.Center) == LobbyHomeTarget.GamePlatform &&
                title.GetHomeTargetHit(title.CaptureGameButton.Bounds.Center) == LobbyHomeTarget.CaptureGame &&
                title.GetHomeTargetHit(Point.Zero) is null,
            "The MonoGame Lobby adapter must translate Home geometry into semantic targets.");

        var playSpaces = Enumerable.Range(0, 6)
            .Select(index => new GuiPlaySpaceEntry(
                new PlaySpaceTypeId($"example.game-{index}"),
                $"Game {index}",
                $"Example.Implementation.{index}",
                "1.0.0",
                ["play"]))
            .ToArray();
        var gameOasis = LobbyGameOasisPresenter.Create(playSpaces);
        Require(gameOasis.VisibleItems.Count == LobbyGameOasisPresenter.MaximumVisibleItems &&
                gameOasis.RemainingItemCount == 2 && !gameOasis.IsLoading,
            "The Lobby Game Oasis presenter must cap visible entries and preserve the remaining count.");
        Require(gameOasis.VisibleItems[0].DisplayName == "Game 0" &&
                gameOasis.VisibleItems[3].ImplementationFirstLine == "Example" &&
                gameOasis.VisibleItems[3].ImplementationSecondLine == "Implementation.3" &&
                gameOasis.VisibleItems[3].VersionLabel == "v1.0.0",
            "The Lobby Game Oasis presenter must prepare catalog fields as complete display lines.");
        Require(gameOasis.Breadcrumb == "GAME OASIS  >  SELECT PLAY-SPACE" &&
                gameOasis.RemainingMessage == "+ 2 MORE PLAY-SPACES" &&
                gameOasis.ImplementationLabel == "IMPLEMENTATION" &&
                gameOasis.OpenLabel == "OPEN  >",
            "The Lobby Game Oasis presenter must own page and item display labels.");
        Require(gameOasis.Select(1)?.PlaySpaceTypeId == new PlaySpaceTypeId("example.game-1") &&
                gameOasis.Select(-1) is null && gameOasis.Select(4) is null,
            "The Lobby Game Oasis presentation must create selection intents only for visible entries.");
        var lobbyScreen = LobbyScreenPresenter.Create(LobbyPage.GameOasis, playSpaces);
        Require(lobbyScreen.CurrentPage == LobbyPage.GameOasis &&
                ReferenceEquals(lobbyScreen.Home, home) &&
                lobbyScreen.GameOasis.VisibleItems.Count == LobbyGameOasisPresenter.MaximumVisibleItems,
            "The Lobby screen presenter must compose page, Home, and Game Oasis state into one drawing input.");
        Require(typeof(LobbyScreenPresentation).GetProperties()
                .Select(property => property.PropertyType.FullName ?? property.PropertyType.Name)
                .All(type => !type.Contains("MonoGame", StringComparison.Ordinal) &&
                             !type.Contains("Microsoft.Xna", StringComparison.Ordinal)),
            "The Lobby screen presentation must not expose MonoGame drawing types.");
        var loadingGameOasis = LobbyGameOasisPresenter.Create([]);
        Require(loadingGameOasis.IsLoading &&
                loadingGameOasis.LoadingMessage == "CONNECTING TO GAME OASIS..." &&
                loadingGameOasis.RemainingMessage is null,
            "An empty Game Oasis catalog must remain an explicit loading presentation without a remaining label.");
        var implementationNames = LobbyGameOasisPresenter.Create([
            new GuiPlaySpaceEntry(new PlaySpaceTypeId("blank"), "Blank", " ", "1", []),
            new GuiPlaySpaceEntry(new PlaySpaceTypeId("simple"), "Simple", "SimpleName", "1", []),
        ]);
        Require(implementationNames.VisibleItems[0].ImplementationFirstLine == "-" &&
                implementationNames.VisibleItems[0].ImplementationSecondLine == "" &&
                implementationNames.VisibleItems[1].ImplementationFirstLine == "SimpleName" &&
                implementationNames.VisibleItems[1].ImplementationSecondLine == "",
            "Implementation-name presentation must handle blank and unsplittable names deterministically.");
        Require(typeof(LobbyGameOasisPresentation).GetProperties()
                .All(property => !property.PropertyType.Name.Contains(nameof(GuiPlaySpaceEntry), StringComparison.Ordinal) &&
                                 !(property.PropertyType.FullName ?? "").Contains("Microsoft.Xna", StringComparison.Ordinal)),
            "The Lobby Game Oasis presentation must not expose Protocol G catalog or MonoGame drawing types.");
    }

    private sealed class FakeLobbyEngine : ILobbyEngine
    {
        public int SaveCallCount { get; private set; }

        public LobbyState LoadState() => new(
            [new GtpEngineProfile { Id = "engine", DisplayName = "Engine" }],
            [new EntryProfile { Id = "entry", DisplayName = "Player" }],
            [new ClientIdentityProfile { Id = "identity", DisplayName = "Identity" }],
            [new CgosConnectionProfile("Connection", "localhost", 6809, "1", "") { Id = "connection" }],
            "/catalog/engines.json",
            "/catalog/entries.json",
            "/catalog/identities.json",
            "/catalog/settings.json",
            false);

        public void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles) => SaveCallCount++;
        public void SaveEntries(IEnumerable<EntryProfile> profiles) => SaveCallCount++;
        public void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles) => SaveCallCount++;
        public void SaveEntriesAndClientIdentities(
            IEnumerable<EntryProfile> entries,
            IEnumerable<ClientIdentityProfile> clientIdentities) => SaveCallCount++;
        public void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles) => SaveCallCount++;
    }

    private static void VerifyGameOasisProfilePolicies()
    {
        var normalized = EntryProfilePolicy.Normalize(new EntryProfile
        {
            Id = "entry-1",
            DisplayName = "  Human  ",
            Kind = EntryProfileKind.Human,
            EngineProfileId = "must-be-cleared",
            ClientIdentityProfileIds = ["target-1", "target-1", ""],
        });
        Require(normalized.DisplayName == "Human" && normalized.EngineProfileId == "" &&
                normalized.ClientIdentityProfileIds.SequenceEqual(["target-1"]),
            "GameOasis.Application must normalize entry identity references independently of the GUI.");
        var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Require(json.Contains("\"targetProfileIds\"", StringComparison.Ordinal),
            "The Application-owned entry profile must preserve the existing targetProfileIds JSON key.");

        var engine = GtpEngineProfilePolicy.Normalize(new GtpEngineProfile
        {
            Id = "engine-1",
            DisplayName = "  Engine  ",
            ExecutablePath = Path.Combine("bin", "engine.exe"),
            WorkingDirectoryStr = "",
            GuiOptions = [],
        }, Path.GetTempPath());
        Require(engine.DisplayName == "Engine" &&
                engine.WorkingDirectoryModel.Value == Path.Combine(Path.GetTempPath(), "bin") &&
                engine.GuiOptions.ContainsKey(GtpEngineGuiOptions.RandomMoveId),
            "GameOasis.Application must normalize persistent GTP engine settings independently of the GUI.");
        var engineJson = JsonSerializer.Serialize(engine, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Require(engineJson.Contains("\"workingDirectory\"", StringComparison.Ordinal) &&
                !engineJson.Contains("workingDirectoryModel", StringComparison.OrdinalIgnoreCase),
            "The Application-owned GTP engine profile must preserve the workingDirectory JSON schema.");

        var catalogJson = GtpEngineCatalogDocumentCodec.Serialize([engine], Path.GetTempPath());
        var catalogDocument = GtpEngineCatalogDocumentCodec.Deserialize(catalogJson, Path.GetTempPath());
        Require(catalogDocument.Profiles.Count == 1 && catalogDocument.Profiles[0].Id == "engine-1" &&
                catalogJson.Contains("\"gtpEngines\"", StringComparison.Ordinal),
            "GameOasis.Application must own the GTP engine catalog JSON document conversion.");

        var connection = CgosConnectionProfilePolicy.Normalize(new CgosConnectionProfile(
            "  Practice  ", " UEC-GO.COM. ", 70000, "  -  ", " note ") { Event = " event " });
        Require(connection.Id.Length > 0 && connection.DisplayName == "Practice" &&
                connection.Host == "UEC-GO.COM." && connection.Port == 65535 &&
                connection.EndpointKey == "cgos://uec-go.com:65535" &&
                connection.Event == "event" && connection.Note == "note",
            "GameOasis.Application must normalize CGOS connection destinations independently of the GUI.");

        const string opaqueConfiguration = "{\"gameSpecific\":{\"boardSize\":19}}";
        var configuration = PlaySpaceConfigurationProfilePolicy.Normalize(new PlaySpaceConfigurationProfile
        {
            DisplayName = "  Main tournament  ",
            PlaySpaceId = "io.example.games.unknown",
            ConfigurationDocument = opaqueConfiguration,
        });
        Require(configuration.Id.Length > 0 && configuration.DisplayName == "Main tournament" &&
                configuration.ConfigurationDocument == opaqueConfiguration,
            "GameOasis.Application must preserve an opaque play-space configuration without interpreting game rules.");
    }

    private static void VerifyGameOasisPlayerParticipation()
    {
        var unavailableSession = new GoAppSession();
        unavailableSession.SelectUseKind(GoAppUseKind.LocalPlay);
        using (var unavailableScene = new PlayingScene(unavailableSession, (_, _, _) => { }, () => { }, () => { }))
        {
            var unavailableRejected = false;
            try
            {
                unavailableScene.StartPlaying();
            }
            catch (InvalidOperationException)
            {
                unavailableRejected = true;
            }
            Require(unavailableRejected && unavailableSession.CurrentMode.Kind != GoAppModeKind.Playing,
                "A normal local match must not fall back to the legacy path while Game Oasis is unavailable.");
        }

        using var composition = GameOasisGuiComposition.CreateAsync().AsTask().GetAwaiter().GetResult();
        var projectedSession = new GoAppSession();
        using var playingScene = new PlayingScene(projectedSession, (_, _, _) => { }, () => { }, () => { });
        playingScene.AttachGameOasisPlayerBridge(composition.PlayerParticipationBridge);
        playingScene.AttachGameOasisPlayerBridge(composition.PlayerParticipationBridge);
        playingScene.AttachGameOasisPlayerBridge(composition.SecondaryPlayerParticipationBridge);
        playingScene.AttachGameOasisPlayerBridge(composition.SecondaryPlayerParticipationBridge);
        playingScene.AttachGameOasisLocalMatchLifecycle(composition.LocalMatchLifecycle);
        playingScene.AttachGameOasisLocalMatchLifecycle(composition.LocalMatchLifecycle);
        playingScene.Update();

        var localMatch = new MatchSession(new MatchConfiguration(
            19,
            startingTurn: GoStone.Black,
            setupStones: [new MatchSetupStone(GoStone.White, new GoPoint(8, 8))]));
        var localLifecycle = composition.LocalMatchLifecycle;
        projectedSession.SelectUseKind(GoAppUseKind.LocalPlay);
        projectedSession.SetPlayerKind(GoStone.White, GoPlayerKind.Human);
        Require(playingScene.StartPlaying(),
            "A human local match must be accepted while the shared lifecycle is idle.");
        Require(localLifecycle.State == LocalMatchGameOasisState.Opening,
            "A human local match must begin opening its Protocol S play-space from the current start path.");
        Require(!projectedSession.IsMatchBackedLocalGame,
            "A Game Oasis local match must not create a legacy MatchSession while Protocol S is opening.");
        Require(projectedSession.IsGameOasisLocalGame &&
                !projectedSession.TryPlaceStone(0, 0) && !projectedSession.Pass() && !projectedSession.Resign(),
            "Legacy local actions must be closed for the entire Game Oasis opening interval.");
        Require(!playingScene.BeginGameOasisLocalMatch(),
            "The local-match lifecycle must reject a second start while opening.");
        Require(SpinWait.SpinUntil(() =>
            {
                playingScene.Update();
                return !localLifecycle.IsBusy;
            }, TimeSpan.FromSeconds(5)),
            "PlayingScene must complete the Game Oasis local-match lifecycle from its frame update.");
        Require(localLifecycle.State == LocalMatchGameOasisState.Ready,
            "Protocol G must open a play-space before a Protocol P player joins.");
        Require(localLifecycle.Board is { Black.Count: 0, White.Count: 0, NextToPlay: "black" },
            "The local-match initial position must cross the Protocol S configuration boundary without losing the starting turn.");
        Require(!projectedSession.IsMatchBackedLocalGame &&
                projectedSession.CurrentTurn == GoStone.Black,
            "A Protocol G projection must replace the legacy Match state used by the current board renderer.");
        Require(!projectedSession.TryPlaceStone(0, 0) && !projectedSession.Pass() && !projectedSession.Resign(),
            "Legacy local actions must remain closed after Protocol S becomes the only game-state authority.");

        var humanGameMaster = composition.HumanGameMasterParticipation;
        var gameMasterBound = humanGameMaster.BindAsync().AsTask().GetAwaiter().GetResult();
        Require(gameMasterBound.IsSuccess && gameMasterBound.Value is not null,
            "The GUI human game master must join the active local match through Protocol M.");
        var pausedResponse = humanGameMaster.ExecuteAsync(
            GameOasisGameMasterCoordinator.PauseCommand,
            "portability-smoke-pause").AsTask().GetAwaiter().GetResult();
        Require(pausedResponse.IsSuccess && pausedResponse.Value is not null,
            "The GUI human game master must pause through Protocol M.");
        var paused = pausedResponse.Value ?? throw new InvalidOperationException("The pause response did not contain a result.");
        Require(paused.Result.WasAccepted &&
                composition.Client.State.ActiveSnapshot?.OperationalState == GameOasisOperationalState.Paused,
            "Protocol G must observe the pause selected by the GUI Protocol M adapter.");
        var resumedResponse = humanGameMaster.ExecuteAsync(
            GameOasisGameMasterCoordinator.ResumeCommand,
            "portability-smoke-resume").AsTask().GetAwaiter().GetResult();
        Require(resumedResponse.IsSuccess && resumedResponse.Value is not null,
            "The GUI human game master must resume through Protocol M.");
        var resumed = resumedResponse.Value ?? throw new InvalidOperationException("The resume response did not contain a result.");
        Require(resumed.Result.WasAccepted &&
                composition.Client.State.ActiveSnapshot?.OperationalState == GameOasisOperationalState.Running,
            "Protocol G must observe the resume selected by the GUI Protocol M adapter.");
        var initialConfiguration = LocalMatchGameOasisConfiguration.Create(
            new LocalMatchInitialPosition(
                19,
                GoStone.Black,
                [new LocalMatchSetupStone(GoStone.White, new GoPoint(8, 8))]),
            6.5m,
            TimeSpan.FromMinutes(5));
        Require(initialConfiguration.Content.Contains("\"mainTimeMilliseconds\":300000", StringComparison.Ordinal) &&
                initialConfiguration.Content.Contains("\"color\":\"white\"", StringComparison.Ordinal),
            "The Game Oasis initial-position value must preserve setup stones and main time without a Match snapshot.");

        var playerBridge = composition.PlayerParticipationBridge;
        Require(playerBridge.BeginBind(new DeterministicPlayerProtocol(), "black"),
            "The frame bridge must begin binding a Protocol P player to the active opaque session.");
        Require(!playerBridge.BeginTurn(),
            "The frame bridge must reject a turn while player binding is pending.");
        CompletePlayerBridgeOperation(playerBridge);
        Require(playerBridge.State == GameOasisPlayerParticipationState.Ready && playerBridge.BindingId is not null,
            "The frame bridge must publish the Protocol P binding on the frame thread.");

        Require(playerBridge.BeginTurn(), "The bound Protocol P player must begin selecting an action.");
        Require(!playerBridge.BeginTurn(), "The frame bridge must reject duplicate turn requests.");
        Require(SpinWait.SpinUntil(() =>
            {
                playingScene.Update();
                return !playerBridge.IsBusy;
            }, TimeSpan.FromSeconds(5)),
            "PlayingScene must complete and project the Protocol P turn from its frame update.");
        Require(playerBridge.State == GameOasisPlayerParticipationState.Ready &&
                playerBridge.Board is { Black.Count: 1 } && playerBridge.LastError is null,
            "Protocol P must change the play-space and the frame bridge must refresh it through Protocol G.");
        Require(projectedSession.GetStone(0, 0) == GoStone.Black && projectedSession.CurrentTurn == GoStone.White,
            "The current board renderer must receive the Protocol G projection after a Protocol P turn.");

        Require(localLifecycle.BeginPlay(1, 0),
            "The Game Oasis local-match lifecycle must accept a human Protocol G action after the player turn.");
        Require(SpinWait.SpinUntil(() =>
            {
                playingScene.Update();
                return !localLifecycle.IsBusy;
            }, TimeSpan.FromSeconds(5)),
            "PlayingScene must complete and project the human Protocol G action from its frame update.");
        Require(projectedSession.GetStone(1, 0) == GoStone.White && projectedSession.CurrentTurn == GoStone.Black,
            "The current board renderer must receive the Protocol G projection after a human action.");
        Require(projectedSession.CurrentGameRecord.Moves.Count == 2 &&
                projectedSession.CurrentGameRecord.Moves[0].Point == new GoPoint(0, 0) &&
                projectedSession.CurrentGameRecord.Moves[1].Point == new GoPoint(1, 0) &&
                projectedSession.CurrentGameRecord.Moves.All(move => move.TimeLeftAfterMove is not null),
            "Protocol G move history must be appended once to the current game record.");

        Require(localLifecycle.BeginPass(), "A Game Oasis pass must be submitted through Protocol G.");
        Require(SpinWait.SpinUntil(() =>
            {
                playingScene.Update();
                return !localLifecycle.IsBusy;
            }, TimeSpan.FromSeconds(5)),
            "PlayingScene must complete and project the Protocol G pass.");
        Require(projectedSession.CurrentGameRecord.Moves.Count == 3 &&
                projectedSession.CurrentGameRecord.Moves[2].Stone == GoStone.Black &&
                projectedSession.CurrentGameRecord.Moves[2].IsPass,
            "A Protocol G pass must appear exactly once in the current game record.");
        var gameOasisSgf = SgfGameRecordConverter.ToSgf(projectedSession.CurrentGameRecord);
        var gameOasisRoundTrip = SgfGameRecordConverter.FromSgf(gameOasisSgf);
        Require(gameOasisSgf.Contains(";B[aa]", StringComparison.Ordinal) &&
                gameOasisSgf.Contains(";W[ba]", StringComparison.Ordinal) &&
                gameOasisSgf.Contains(";B[]", StringComparison.Ordinal) &&
                gameOasisSgf.Contains("BL[", StringComparison.Ordinal) &&
                gameOasisSgf.Contains("WL[", StringComparison.Ordinal) &&
                gameOasisRoundTrip.Moves.Count == 3,
            "Game Oasis play, pass, and authoritative clock history must survive the existing SGF save/load boundary.");

        Require(playerBridge.BeginUnbind("portability-smoke"),
            "The frame bridge must begin ending the Protocol P participation.");
        CompletePlayerBridgeOperation(playerBridge);
        Require(playerBridge.State == GameOasisPlayerParticipationState.Idle && playerBridge.BindingId is null,
            "The frame bridge must return to idle after player participation ends.");
        var gameMasterUnbound = humanGameMaster.UnbindAsync("portability-smoke").AsTask().GetAwaiter().GetResult();
        Require(gameMasterUnbound.IsSuccess,
            "The GUI human game master must leave through Protocol M before the local match closes.");
        Require(localLifecycle.BeginResign(),
            "The Game Oasis local-match lifecycle must submit a terminal human action through Protocol G.");
        Require(SpinWait.SpinUntil(() =>
            {
                playingScene.Update();
                return !localLifecycle.IsBusy;
            }, TimeSpan.FromSeconds(5)),
            "PlayingScene must complete and project the terminal Protocol G action.");
        Require(projectedSession.CurrentMode.Kind == GoAppModeKind.GameOver &&
                projectedSession.Winner == GoStone.Black &&
                projectedSession.GameOverReason == "RESIGNATION",
            "A terminal Game Oasis outcome must drive the current result screen without consulting legacy Match state.");
        projectedSession.SeekLocalReviewTimeline(1);
        Require(projectedSession.IsLocalReplayMode &&
                projectedSession.GetDisplayStone(0, 0) == GoStone.Black &&
                projectedSession.GetDisplayStone(1, 0) == GoStone.Empty,
            "The existing local review must reconstruct an earlier Game Oasis move-history position.");
        projectedSession.SeekLocalReviewTimeline(projectedSession.LocalReviewTimelineMaximum);
        Require(projectedSession.IsLocalResultPosition,
            "The existing local review must return from Game Oasis history to the result position.");
        playingScene.CloseGameOasisLocalMatchIfNeeded();
        Require(localLifecycle.State == LocalMatchGameOasisState.Closing,
            "Leaving the result screen must begin closing the Game Oasis local-match session.");
        projectedSession.ReturnToSetup();
        Require(!playingScene.StartPlaying() && projectedSession.CurrentMode.Kind == GoAppModeKind.Resting,
            "START during the preceding session close must be rejected without moving the GUI out of the Lobby.");
        CompleteLocalMatchLifecycleOperation(localLifecycle);
        Require(localLifecycle.State == LocalMatchGameOasisState.Idle && localLifecycle.Board is null && localLifecycle.CanStart,
            "The shared Game Oasis session must close cleanly and return the local-match lifecycle to idle.");
        Require(playingScene.StartPlaying() && localLifecycle.State == LocalMatchGameOasisState.Opening,
            "A second local match must start without restarting the GUI after the preceding close completes.");
        CompleteLocalMatchLifecycleOperation(localLifecycle);
        Require(localLifecycle.State == LocalMatchGameOasisState.Ready,
            "The second local match must open through the reused Game Oasis lifecycle.");
        playingScene.CloseGameOasisLocalMatchIfNeeded();
        CompleteLocalMatchLifecycleOperation(localLifecycle);
        Require(localLifecycle.State == LocalMatchGameOasisState.Idle,
            "The reused Game Oasis lifecycle must close the second local match cleanly.");
    }

    private static void VerifyGameOasisGuiComposition()
    {
        var composition = GameOasisGuiComposition.CreateAsync().AsTask().GetAwaiter().GetResult();
        Require(composition.Client.State.PlaySpaces.Count == 2,
            "The current GUI composition must discover normal Go and Ponnuki through Protocol G.");
        Require(composition.Client.State.PlaySpaces.All(entry =>
                entry.TypeId.Value is GameOasisOfficialNames.Go or GameOasisOfficialNames.Ponnuki),
            "The current GUI composition exposed an unexpected play-space.");
        var goEntry = composition.Client.State.PlaySpaces.Single(entry => entry.TypeId.Value == GameOasisOfficialNames.Go);
        var ponnukiEntry = composition.Client.State.PlaySpaces.Single(entry => entry.TypeId.Value == GameOasisOfficialNames.Ponnuki);
        Require(goEntry.Capabilities.Contains(GameOasisCapabilityIds.ActionPlayPoint) &&
                goEntry.Capabilities.Contains(GameOasisCapabilityIds.ActionPass) &&
                goEntry.Capabilities.Contains(GameOasisCapabilityIds.ActionResign) &&
                ponnukiEntry.Capabilities.Contains(GameOasisCapabilityIds.ActionPlayPoint) &&
                !ponnukiEntry.Capabilities.Contains(GameOasisCapabilityIds.ActionPass) &&
                !ponnukiEntry.Capabilities.Contains(GameOasisCapabilityIds.ActionResign),
            "Protocol G must expose stable action capabilities for GUI control selection.");
        Require(!composition.GetActiveBoard().IsSuccess,
            "The current GUI composition must not expose a board before a Protocol G session opens.");

        var bridge = composition.PlayingBridge;
        var goPreset = GameOasisSessionPresets.Create(new(GameOasisOfficialNames.Go));
        var ponnukiPreset = GameOasisSessionPresets.Create(new(GameOasisOfficialNames.Ponnuki));
        Require(goPreset.SchemaId == GameOasisOfficialNames.Go + ".configuration.v1" &&
                ponnukiPreset.SchemaId == GameOasisOfficialNames.Ponnuki + ".configuration.v1",
            "The title quick-start presets must use the official configuration schemas.");
        Require(!GameOasisSessionPresets.TryCreate(new("external.example.game"), out _),
            "An external play-space without a configuration UI must not receive an invented preset.");
        var externalTypeId = new PlaySpaceTypeId("external.example.game");
        var externalAdapters = new GameBoardActionAdapters(
        [
            new JsonGameBoardActionAdapter(
                externalTypeId,
                "external.example.game.action.v1",
                new HashSet<string>(StringComparer.Ordinal) { GameOasisCapabilityIds.ActionPlayPoint }),
        ]);
        var externalBoard = new GuiBoardView(
            new("external-session"), externalTypeId, 0, 9, [], [], "black", 0, 0, null, false, null, [], [], [], null, null, null);
        Require(externalAdapters.Supports(externalTypeId, GameOasisCapabilityIds.ActionPlayPoint) &&
                !externalAdapters.Supports(externalTypeId, GameOasisCapabilityIds.ActionPass),
            "An external GUI action adapter must expose only its registered capabilities.");
        var externalPlay = externalAdapters.CreatePlay(externalBoard, 3, 4);
        Require(externalPlay.IsSuccess && externalPlay.Value?.SchemaId == "external.example.game.action.v1",
            "An external GUI action adapter must be able to supply its own action schema.");
        Require(externalAdapters.CreatePass(externalBoard).Error?.Code == "unsupported-gui-action",
            "An external GUI action adapter must not gain unregistered actions.");
        Require(TitleScreen.GetGameOasisPlaySpaceHit(TitleScreen.GetGameOasisPlaySpaceBounds(0).Center, 2) == 0 &&
                TitleScreen.GetGameOasisPlaySpaceHit(TitleScreen.GetGameOasisPlaySpaceBounds(1).Center, 2) == 1,
            "The catalog-driven Game Oasis title choices must map each visible card to its catalog index.");
        var invalidConfiguration = new ContractDocument(
            "application/json",
            GameOasisOfficialNames.Ponnuki + ".configuration.v1",
            """{"version":1,"boardSize":8,"initialMoveCount":0,"captureTarget":1}""");
        Require(bridge.BeginOpen(new(GameOasisOfficialNames.Ponnuki), invalidConfiguration),
            "The GUI playing bridge must submit a configuration attempt from the idle state.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Idle && bridge.Board is null && bridge.LastError is not null,
            "A rejected configuration must return the GUI playing bridge to retryable idle state.");

        var configuration = new ContractDocument(
            "application/json",
            GameOasisOfficialNames.Ponnuki + ".configuration.v1",
            """{"version":1,"boardSize":9,"initialMoveCount":0,"randomSeed":99,"captureTarget":1,"startingPlayer":"black","setupStones":[{"x":0,"y":0,"color":"black"},{"x":2,"y":0,"color":"black"},{"x":1,"y":0,"color":"white"}]}""");
        Require(bridge.BeginOpen(new(GameOasisOfficialNames.Ponnuki), configuration),
            "The GUI playing bridge must begin opening a Protocol G session from the idle state.");
        Require(!bridge.BeginOpen(new(GameOasisOfficialNames.Ponnuki), configuration),
            "The GUI playing bridge must reject a second operation while one is pending.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Ready && bridge.Board is { IsTerminal: false },
            "The GUI playing bridge must publish the opened board on the frame thread.");

        var openedBoard = bridge.Board ?? throw new InvalidOperationException("The opened GUI board was not published.");
        var openedRevision = openedBoard.Revision;
        Require(bridge.BeginPlay(0, 0),
            "The GUI playing bridge must accept an input attempt while ready.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Ready &&
                bridge.Board is { IsTerminal: false } && bridge.Board.Revision == openedRevision &&
                bridge.LastError?.Code == "gui-point-occupied",
            "A rejected GUI input must preserve the current board and allow another input.");

        Require(bridge.BeginPlay(1, 1),
            "The GUI playing bridge must submit a legal board action.");
        Require(!bridge.BeginPlay(1, 2),
            "The GUI playing bridge must reject duplicate input while an action is pending.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Terminal && bridge.Board is { IsTerminal: true, BlackCaptures: 1 },
            "The GUI playing bridge must publish the terminal Ponnuki board.");
        Require(bridge.LastError is null,
            "A successful retry must clear the preceding local input error.");

        Require(bridge.BeginClose(),
            "The GUI playing bridge must begin closing its active session.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Idle && bridge.Board is null && bridge.LastError is null,
            "The GUI playing bridge must return to idle after closing the session.");

        var goConfiguration = new ContractDocument(
            "application/json",
            GameOasisOfficialNames.Go + ".configuration.v1",
            """{"version":1,"boardSize":9,"komi":6.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[]}""");
        Require(bridge.BeginOpen(new(GameOasisOfficialNames.Go), goConfiguration),
            "The GUI playing bridge must open normal Go after closing Ponnuki.");
        CompleteBridgeOperation(bridge);
        var goBoard = bridge.Board ?? throw new InvalidOperationException("The normal Go GUI board was not published.");
        Require(GameOasisBoardPanel.IsPassHit(GameOasisBoardPanel.PassBounds.Center) &&
                GameOasisBoardPanel.IsResignHit(GameOasisBoardPanel.ResignBounds.Center) &&
                GameOasisBoardPanel.IsCloseHit(GameOasisBoardPanel.CloseBounds.Center),
            "The Game Oasis panel must expose only the actions supported by the selected game.");
        Require(bridge.BeginPass(), "The normal Go GUI bridge must submit pass.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Ready && bridge.Board?.NextToPlay == "white",
            "A normal Go pass must advance the common GUI board turn.");
        Require(bridge.BeginResign(), "The normal Go GUI bridge must submit resign.");
        CompleteBridgeOperation(bridge);
        Require(bridge.State == GameOasisPlayingState.Terminal && bridge.Board?.IsTerminal == true,
            "A normal Go resignation must publish a terminal common GUI board.");
        Require(bridge.BeginClose(), "The terminal normal Go GUI session must be closable.");
        CompleteBridgeOperation(bridge);
        bridge.Dispose();
    }

    private static void CompleteBridgeOperation(GameOasisPlayingBridge bridge)
    {
        Require(SpinWait.SpinUntil(() => bridge.Update(), TimeSpan.FromSeconds(5)),
            "The GUI playing bridge operation did not complete within the smoke-test timeout.");
    }

    private static void CompletePlayerBridgeOperation(GameOasisPlayerParticipationBridge bridge)
    {
        Require(SpinWait.SpinUntil(() => bridge.Update(), TimeSpan.FromSeconds(5)),
            "The GUI player participation bridge operation did not complete within the smoke-test timeout.");
    }

    private static void CompleteLocalMatchLifecycleOperation(LocalMatchGameOasisLifecycle lifecycle)
    {
        Require(SpinWait.SpinUntil(() => lifecycle.Update(), TimeSpan.FromSeconds(5)),
            "The Game Oasis local-match lifecycle operation did not complete within the smoke-test timeout.");
    }

    private static void VerifyCgosResultReviewRecord()
    {
        var observation = new CgosGameObservation();
        observation.ProcessLogLine("[CGOS] > setup 123 9 7.0 300000 WhitePlayer BlackPlayer");
        observation.ProcessLogLine("[CGOS] > play black D4 290000");
        observation.ProcessLogLine("[CGOS] > gameover B+R");

        var record = observation.CreateGameRecord();
        Require(record.Result == "B+R" && record.Moves.Count == 1 &&
                record.Moves[0].TimeLeftAfterMove == TimeSpan.FromSeconds(290),
            "The CGOS result review record did not preserve the result, moves, or clock.");

        var session = new GoAppSession();
        Require(session.StartReviewingGameRecord(record, out var warning),
            $"The CGOS result record could not start a review: {warning}");
        Require(session.MoveReview(session.ReviewTimelineMaximum - session.ReviewTimelineIndex, out warning) &&
                session.IsReviewResultPosition && session.ReviewResult == "B+R",
            $"The CGOS review did not reach its terminal RESULT position: {warning}");
    }

    private static void VerifyCgosHumanMoveReflection()
    {
        var observation = new CgosGameObservation();
        observation.ProcessLogLine("[human] > setup 321 9 7.0 300000 HumanLogin Opponent");
        observation.ProcessLogLine("[human] > play black E6 290000");

        Require(observation.ApplyHumanMove(GoStone.White, "C3") &&
                observation.GetStone(2, 6) == GoStone.White &&
                observation.CurrentTurn == GoStone.Black &&
                observation.LatestMove?.TimeLeftAfterMove is { } humanTimeLeft &&
                humanTimeLeft <= observation.MainTime,
            "A submitted CGOS human move was not reflected on the local live board.");
        Require(observation.ProcessLogLine("[human] > play black D4 280000") &&
                observation.CurrentTurn == GoStone.White,
            "The opponent move after a CGOS human move was rejected by stale turn state.");
        Require(observation.ApplyHumanMove(GoStone.White, "pass") &&
                observation.CurrentTurn == GoStone.Black && observation.MoveCount == 4,
            "A submitted CGOS human pass was not reflected in the local turn state.");
    }

    private static void VerifyCgosPracticeUnexpectedGameState()
    {
        var primary = new CgosGameObservation();
        var practice = new CgosGameObservation();
        primary.ProcessLogLine("[primary] > setup 100 9 7.0 300000 Opponent PrimaryLogin");
        practice.ProcessLogLine("[practice] > setup 200 9 7.0 300000 PracticeLogin OtherOpponent");
        primary.ProcessLogLine("[primary] > play black D4 290000");
        practice.ProcessLogLine("[practice] > play black E5 280000");

        Require(primary.GameId == 100 && practice.GameId == 200 &&
                primary.GetStone(3, 5) == GoStone.Black &&
                practice.GetStone(4, 4) == GoStone.Black,
            "Primary and unexpected practice CGOS games were not kept in independent boards.");
        Require(practice.GetPlayerColor("PracticeLogin") == GoStone.White &&
                practice.GetOpponentName("PracticeLogin") == "OtherOpponent",
            "The practice player's color or opponent was not identified from CGOS setup.");

        var session = new GoAppSession();
        session.SetCgosPracticeUnexpectedGame(true, practice.GameId, "OtherOpponent", GoStone.White, practice.MoveCount, practice.WhiteRemainingTime);
        session.RequestCgosPracticeResignConfirmation();
        Require(session.IsCgosPracticeUnexpectedGameInProgress &&
                session.IsCgosPracticeResignConfirmationPending &&
                session.CgosPracticeUnexpectedGameId == 200,
            "The unexpected practice match did not expose a safe resignation confirmation state.");
        session.MarkCgosPracticeResignRequested();
        Require(session.IsCgosPracticeResignRequested && !session.IsCgosPracticeResignConfirmationPending,
            "The unexpected practice resignation state was not committed safely.");

        practice.Reset();
        Require(!practice.IsStarted && practice.GameId == 0 && practice.MoveCount == 0,
            "Reset did not clear the duplicate practice observation.");
    }

    private static void VerifyGoAppEngineSelectionCompatibility()
    {
        var session = new GoAppSession();
        session.SetGtpEngineProfiles(
        [
            new GtpEngineProfile { DisplayName = "Legacy" },
            new GtpEngineProfile { DisplayName = "Ponnuki" },
        ]);
        Require(session.SelectedAppProviderEngineIndex == -1 &&
                session.SelectedAppProviderEngineDisplayName == "未選択" &&
                !session.CanUseSelectedAppProvider,
            "The App Provider must remain unselected until the user explicitly chooses one.");
        var restoreSession = new GoAppSession();
        restoreSession.SetGtpEngineProfiles(
        [
            new GtpEngineProfile { DisplayName = "First", ExecutablePath = "first-engine" },
            new GtpEngineProfile { DisplayName = "Remembered", ExecutablePath = "remembered-engine" },
        ]);
        Require(restoreSession.RestoreAppProviderEngine("remembered-engine") &&
                restoreSession.SelectedAppProviderEngineIndex == 1 &&
                restoreSession.SelectedAppProviderEngineDisplayName == "Remembered",
            "The last App Provider selection was not restored by executable path.");
        Require(!restoreSession.RestoreAppProviderEngine("missing-engine") &&
                restoreSession.SelectedAppProviderEngineIndex == 1,
            "A missing remembered App Provider must not select a different engine.");
        restoreSession.SetAppProviderCapability(false, "CHECKING PROVIDER...");
        Require(restoreSession.IsAppProviderCapabilityCheckRunning && !restoreSession.CanStartSelectedAppProvider,
            "An automatically restored Provider must remain unavailable while its capability check is running.");
        restoreSession.SetAppProviderCapability(true, "PONNUKI v1 LIFECYCLE READY");
        Require(!restoreSession.IsAppProviderCapabilityCheckRunning && restoreSession.CanStartSelectedAppProvider,
            "A restored Provider must become available after a successful automatic capability check.");
        session.SetGtpEngineAppCompatibilities(
        [
            new(GtpEngineAppCompatibilityKind.Unsupported, "ponnuki NOT SUPPORTED"),
            new(GtpEngineAppCompatibilityKind.Supported, "ponnuki READY"),
        ]);
        session.OpenAppProviderGtpEngineSelectionDialog("ponnuki");
        session.SelectGtpEngineDialogItem(0);
        Require(session.GtpEngineDialogSelectionIndex == 0 && !session.CanCommitGtpEngineSelection,
            "An engine that omits the target app must be selectable for EDIT while SELECT remains disabled.");
        session.SelectGtpEngineDialogItem(1);
        Require(session.GtpEngineDialogSelectionIndex == 1 && session.CanCommitGtpEngineSelection,
            "An engine that publishes the target app must be selectable.");

        session.SetGtpEngineAppCompatibilities(
        [
            new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY GO PLAY"),
            new(GtpEngineAppCompatibilityKind.Unsupported, "play NOT SUPPORTED"),
        ]);
        session.OpenGtpEngineSelectionDialog(GoStone.Black);
        session.SelectGtpEngineDialogItem(0);
        Require(session.CanCommitGtpEngineSelection,
            "An engine without kfw-list-apps must remain compatible with Go Play.");
        session.SelectGtpEngineDialogItem(1);
        Require(session.GtpEngineDialogSelectionIndex == 0,
            "An engine that explicitly omits play must not replace the selectable engine.");
    }

    private static void VerifyKfaToKfwConversion()
    {
        const string legacy = "(;C[KFA[comment\\] text]KFA[{\"unknown\":true}];B[aa](;W[bb]KFA[x]))";
        const string expected = "(;C[KFA[comment\\] text]KFW[{\"unknown\":true}];B[aa](;W[bb]KFW[x]))";
        Require(
            SgfGameRecordConverter.ConvertKfaToKfw(legacy) == expected,
            "KFA properties must be renamed without changing values or variations.");

        var record = SgfGameRecordConverter.FromSgf("(;GM[1]SZ[9];B[aa]KFA[{\"unknown\":true}])");
        var upgraded = SgfGameRecordConverter.ToSgf(record);
        Require(upgraded.Contains("KFW[{\"unknown\":true}]", StringComparison.Ordinal), "Unreadable legacy KFA must be saved as KFW.");
        Require(!upgraded.Contains("KFA[", StringComparison.Ordinal), "Current SGF output must not contain KFA.");

        var current = SgfGameRecordConverter.FromSgf("(;GM[1]SZ[9];B[aa]KFW[{\"unknown\":true}])");
        Require(
            current.Moves.Count == 1 && current.Moves[0].LegacyKifuwarabeAnalysisJson == "{\"unknown\":true}",
            "KFW analysis JSON must be readable.");
    }

    private static void VerifyInitialPositionConciergeGuiModel()
    {
        var black = new InitialPositionEngineProgressView(
            GoStone.Black,
            "Black engine",
            false,
            false,
            true,
            false,
            [],
            []);
        var white = new InitialPositionEngineProgressView(
            GoStone.White,
            "White engine",
            false,
            false,
            false,
            true,
            [],
            []);
        var view = new InitialPositionConciergeView(true, false, GoStone.Black, [black, white]);

        Require(view.Engines.Count == 2, "The concierge GUI must keep two engine flows independently.");
        Require(view.Engines[0].CanTryAnotherMethod && view.Engines[1].CanContinueAsIs,
            "Each engine card must expose its own available action.");
        Require(
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.GetTryAnotherButtonHit(new Point(1170, 455)) == GoStone.Black,
            "The black-engine fallback button hit area is incorrect.");
        Require(
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.GetContinueButtonHit(new Point(1500, 781)) == GoStone.White,
            "The white-engine continue button hit area is incorrect.");
        Require(
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.GetEngineCardHit(new Point(1200, 550)) == GoStone.White,
            "The white-engine card selection hit area is incorrect.");
        Require(
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.IsCancelButtonHit(new Point(1200, 940)) &&
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.IsLogButtonHit(new Point(1550, 940)),
            "The concierge footer button hit areas are incorrect.");
    }

    private static void VerifyInitialPositionEngineProfiles()
    {
        static GtpCapabilitySet Capabilities(string name, string version) =>
            new(name, version, []);

        Require(BuiltInGtpProfiles.ResolveBase("KataGo", "auto").Id == BuiltInGtpProfiles.KataGoId,
            "KataGo must resolve to its built-in profile.");
        Require(BuiltInGtpProfiles.ResolveBase("Leela Zero 0.17", "auto").Id == BuiltInGtpProfiles.LeelaZeroId,
            "Leela Zero must resolve to its built-in profile.");
        Require(BuiltInGtpProfiles.ResolveBase("GNU Go", "auto").Id == BuiltInGtpProfiles.GnuGoId,
            "GNU Go must resolve to its built-in profile.");
        Require(BuiltInGtpProfiles.ResolveBase("KifuwarabeGo2026", "auto").Id == BuiltInGtpProfiles.KifuwarabeId,
            "Kifuwarabe must resolve to its built-in profile.");
        Require(KifuwarabeGtpProfile.Instance.Evidence == GtpProfileEvidence.BundledEngineVerified &&
                KataGoGtpProfile.Instance.Evidence == GtpProfileEvidence.OfficialDocumentationOnly &&
                LeelaZeroGtpProfile.Instance.Evidence == GtpProfileEvidence.OfficialDocumentationOnly &&
                GnuGoGtpProfile.Instance.Evidence == GtpProfileEvidence.OfficialDocumentationOnly &&
                GenericGtpProfile.Instance.Evidence == GtpProfileEvidence.ConservativeFallback,
            "Profile evidence must distinguish local verification, documentation, and safe fallback assumptions.");
        Require(BuiltInGtpProfiles.ResolveBase("Unknown experimental engine", "auto").Id == GenericGtpProfile.Instance.Id,
            "An unknown engine must safely resolve to Generic GTP.");
        Require(BuiltInGtpProfiles.ResolveBase("KataGo", "unknown-profile-id").Id == GenericGtpProfile.Instance.Id,
            "An unknown explicit profile id must safely resolve to Generic GTP.");
        Require(
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.GtpEngine.GtpEngineRenderer.GetGtpEngineEditPanelInitialPositionProfileButtonHit(new Point(800, 680)) &&
            KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.GtpEngine.GtpEngineRenderer.GetGtpEngineEditPanelInitialPositionMethodButtonHit(new Point(1050, 680)),
            "The engine editor initial-position setting button hit areas are incorrect.");

        var manuallyPreferred = BuiltInGtpProfiles.Resolve(
            Capabilities("KataGo", "1.15"),
            BuiltInGtpProfiles.AutoId,
            InitialPositionMethod.LoadSgf);
        Require(manuallyPreferred.Strategies[0].Method == InitialPositionMethod.LoadSgf,
            "A preferred method must be moved to the front without changing the base profile.");

        var profile = new GtpEngineProfile
        {
            DisplayName = "Profile persistence smoke",
            ExecutablePath = "profile-smoke-engine",
            InitialPositionProfileId = BuiltInGtpProfiles.AutoId,
            InitialPositionManualPreferredMethod = InitialPositionMethod.SequentialPlay,
        };
        profile.RememberInitialPositionDetection(
            InitialPositionMethod.LoadSgf,
            "KataGo",
            "1.15",
            BuiltInGtpProfiles.KataGoId);
        Require(profile.HasMatchingInitialPositionDetection("katago", "1.15"),
            "A saved automatic result must match the same engine identity case-insensitively.");
        Require(!profile.ClearStaleInitialPositionDetection("KataGo", "1.15"),
            "The same engine version must retain its automatic result.");
        Require(profile.ClearStaleInitialPositionDetection("KataGo", "1.16"),
            "An engine version change must invalidate the automatic result.");
        Require(profile.InitialPositionDetectedMethod is null &&
                profile.InitialPositionManualPreferredMethod == InitialPositionMethod.SequentialPlay,
            "Version invalidation must preserve the manual preference.");

        profile.RememberInitialPositionDetection(
            InitialPositionMethod.LoadSgf,
            "KataGo",
            "1.16",
            BuiltInGtpProfiles.KataGoId);
        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"kifuwarabe-profile-smoke-{Guid.NewGuid():N}");
        var listPath = Path.Combine(temporaryRoot, "gtp-engine-list.json");
        try
        {
            var catalog = GtpEngineCatalog.Load(CatalogDocumentStorage.Default, listPath);
            catalog.Save([profile]);
            var restored = GtpEngineCatalog.Load(CatalogDocumentStorage.Default, listPath).Profiles.Single();
            Require(restored.InitialPositionProfileId == BuiltInGtpProfiles.AutoId &&
                    restored.InitialPositionManualPreferredMethod == InitialPositionMethod.SequentialPlay &&
                    restored.InitialPositionDetectedMethod == InitialPositionMethod.LoadSgf &&
                    restored.InitialPositionDetectedEngineVersion == "1.16" &&
                    restored.InitialPositionDetectedProfileId == BuiltInGtpProfiles.KataGoId,
                "Manual and automatic initial-position selections must survive profile JSON round-trip.");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
                Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static void VerifyKifuwarabeAtomicSetupStrategy()
    {
        var request = new InitialPositionRequest(
            19,
            6.5m,
            GoStone.White,
            [
                new MatchSetupStone(GoStone.Black, new GoPoint(3, 15)),
                new MatchSetupStone(GoStone.White, new GoPoint(15, 3)),
            ]);
        var strategy = KifuwarabeAtomicSetupStrategy.Instance;
        var commands = strategy.BuildCommands(request);
        Require(commands.SequenceEqual(
            [
                "boardsize 19",
                "komi 6.5",
                "kfw-begin-position",
                "kfw-add-black D4",
                "kfw-add-white Q16",
                "kfw-set-to-play white",
                "kfw-commit-position",
            ]),
            "The Kifuwarabe atomic setup command sequence is incorrect.");
        Require(strategy.RequiredCommands.SequenceEqual(
            ["kfw-begin-position", "kfw-add-black", "kfw-add-white", "kfw-set-to-play", "kfw-commit-position", "kfw-abort-position"]),
            "The atomic strategy must require the complete transactional command set.");
        Require(KifuwarabeGtpProfile.Instance.Strategies[0].Method == InitialPositionMethod.KifuwarabeAtomicSetup,
            "The Kifuwarabe profile must try its verified atomic method first.");

        var capabilities = new GtpCapabilitySet(
            "Kifuwarabe Star Random GTP",
            "3.0.0",
            strategy.RequiredCommands.Select(command => new GtpCommandCapability(
                command,
                GtpCommandSupport.Supported,
                GtpCapabilityEvidence.KnownCommand,
                "smoke")));
        var host = new StubInitialPositionExecutionHost((_, _) =>
            Task.FromResult(new GtpCommandResult(true, "")));
        var result = new InitialPositionConcierge().ExecuteAsync(
            host,
            request,
            capabilities,
            KifuwarabeGtpProfile.Instance).GetAwaiter().GetResult();
        Require(result.IsVerified && result.LastAttempt?.Method == InitialPositionMethod.KifuwarabeAtomicSetup,
            "A fully accepted atomic transaction must be recorded as verified.");
        Require(host.Commands.SequenceEqual(commands),
            "The concierge did not send the complete atomic command sequence.");
    }

    private static void VerifyGtpExtensionsAssembly(Assembly gtpExtensionsAssembly, Assembly gtpCommunicationAssembly)
    {
        VerifyTargetFramework(gtpExtensionsAssembly, "GtpExtensions");
        VerifyNoPlatformInvokes(gtpExtensionsAssembly, "GtpExtensions");

        var references = gtpExtensionsAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Require(
            !references.Contains("KifuwarabeGo2026.GameOasis.Gui"),
            "GtpExtensions must not reference the GUI assembly.");
        Require(
            !references.Contains("KifuwarabeGo2026.Engine"),
            "GtpExtensions must not reference the engine executable.");
        Require(
            !references.Contains("MonoGame.Framework"),
            "GtpExtensions must not reference MonoGame.");
        Require(
            !references.Contains("System.Diagnostics.Process"),
            "GtpExtensions must not own external processes.");
        Require(references.Contains("KifuwarabeGo2026.FormalAdapter.Gtp"),
            "Go GTP extensions must consume the FormalAdapter-owned GTP protocol boundary.");
        Require(!gtpCommunicationAssembly.GetReferencedAssemblies().Any(reference =>
                reference.Name == "KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions"),
            "GTP communication must not depend back on Go play-space extensions.");

        foreach (var forbiddenReference in ForbiddenAssemblyReferences)
        {
            Require(
                !references.Contains(forbiddenReference),
                $"GtpExtensions directly references Windows-only assembly '{forbiddenReference}'.");
        }
    }

    private static void VerifyGtpExtensionsInitialPositionPlanning()
    {
        foreach (var boardSize in new[] { 9, 13, 19 })
        {
            for (var stoneCount = 2; stoneCount <= 9; stoneCount++)
            {
                var points = FixedHandicapPoints.Get(boardSize, stoneCount);
                Require(
                    FixedHandicapPoints.IsStandardPlacement(boardSize, points),
                    $"The {boardSize}x{boardSize} fixed-handicap placement for {stoneCount} stones must be recognized.");
            }
        }

        var emptyRequest = new InitialPositionRequest(9, 6.5m, GoStone.Black);
        var empty = InitialPositionClassifier.Classify(emptyRequest);
        Require(empty.Kind == InitialPositionKind.Empty, "An empty setup must be classified as empty.");
        Require(empty.BlackStoneCount == 0 && empty.WhiteStoneCount == 0, "An empty setup must have no setup stones.");

        var standardPoints = FixedHandicapPoints.Get(19, 9);
        var standardRequest = new InitialPositionRequest(
            19,
            0.5m,
            GoStone.White,
            standardPoints.Select(point => new MatchSetupStone(GoStone.Black, point)));
        var standard = InitialPositionClassifier.Classify(standardRequest);
        Require(standard.Kind == InitialPositionKind.StandardFixedHandicap, "Nine standard handicap stones must be recognized.");
        Require(standard.FixedHandicapStoneCount == 9, "The fixed handicap count is incorrect.");
        Require(standard.StartingTurn == GoStone.White, "Classification must preserve the requested starting turn.");

        var freeRequest = new InitialPositionRequest(
            9,
            6.5m,
            GoStone.White,
            [new MatchSetupStone(GoStone.Black, new GoPoint(0, 0))]);
        var free = InitialPositionClassifier.Classify(freeRequest);
        Require(free.Kind == InitialPositionKind.SpecifiedBlackHandicap, "A nonstandard black setup must be classified as specified handicap.");

        var sourceSetup = new[]
        {
            new MatchSetupStone(GoStone.Black, new GoPoint(0, 0)),
            new MatchSetupStone(GoStone.White, new GoPoint(8, 8)),
        };
        var mixedRequest = new InitialPositionRequest(9, 6.5m, GoStone.Black, sourceSetup);
        sourceSetup[0] = new MatchSetupStone(GoStone.White, new GoPoint(1, 1));
        var mixed = InitialPositionClassifier.Classify(mixedRequest);
        Require(mixed.Kind == InitialPositionKind.MixedSetup, "A black-and-white setup must be classified as mixed.");
        Require(mixed.BlackStoneCount == 1 && mixed.WhiteStoneCount == 1, "Mixed setup stone counts are incorrect.");
        Require(mixedRequest.SetupStones[0].Point == new GoPoint(0, 0), "An initial-position request must copy its setup stones.");

        var strategy = SequentialPlayStrategy.Instance;
        Require(strategy.CanApply(mixedRequest, mixed), "Sequential play must remain available for a mixed setup.");
        Require(strategy.Method == InitialPositionMethod.SequentialPlay, "The compatibility strategy method is incorrect.");
        Require(
            strategy.BuildCommands(mixedRequest).SequenceEqual(
            [
                "boardsize 9",
                "komi 6.5",
                "clear_board",
                "play black A9",
                "play white J1",
            ]),
            "The migrated sequential-play strategy changed the existing command sequence.");

        Require(
            global::KifuwarabeGo2026.FormalAdapter.Gtp.Go.GtpCoordinate.FormatVertex(new GoPoint(8, 8), 9) == "J1",
            "GTP formatting must skip the I column.");
        Require(
            global::KifuwarabeGo2026.FormalAdapter.Gtp.Go.GtpCoordinate.TryParseVertex("J1", 9, out var parsed) && parsed == new GoPoint(8, 8),
            "GTP parsing must reverse formatted coordinates.");
        Require(
            !global::KifuwarabeGo2026.FormalAdapter.Gtp.Go.GtpCoordinate.TryParseVertex("I1", 9, out _),
            "The invalid GTP I column must be rejected.");
    }

    private static void VerifyGtpCapabilityProbe()
    {
        var probe = new GtpCapabilityProbe();
        var normalSession = new StubGtpCommandSession((command, _) => Task.FromResult(command switch
        {
            "name" => new GtpCommandResult(true, "Example Engine"),
            "version" => new GtpCommandResult(true, "1.2.3"),
            "known_command fixed_handicap" => new GtpCommandResult(true, "true"),
            "known_command loadsgf" => new GtpCommandResult(true, "false"),
            "list_commands" => new GtpCommandResult(true, "fixed_handicap\ngenmove"),
            _ => new GtpCommandResult(false, "unknown command"),
        }));
        var normal = probe.ProbeAsync(normalSession, ["fixed_handicap", "loadsgf"])
            .GetAwaiter()
            .GetResult();

        Require(normal.EngineName == "Example Engine" && normal.EngineVersion == "1.2.3", "The capability probe must retain engine identity.");
        Require(normal.Get("fixed_handicap").Support == GtpCommandSupport.Supported, "A consistently supported command must be reported as supported.");
        Require(normal.Get("fixed_handicap").Evidence == GtpCapabilityEvidence.ConsistentResponses, "Consistent capability evidence is missing.");
        Require(normal.Get("loadsgf").Support == GtpCommandSupport.Unsupported, "A consistently unsupported command must be reported as unsupported.");
        Require(
            normalSession.Commands.IndexOf("known_command fixed_handicap") < normalSession.Commands.IndexOf("list_commands"),
            "known_command must be attempted before the list_commands cross-check.");

        var fallbackSession = new StubGtpCommandSession((command, _) => Task.FromResult(command switch
        {
            "name" => new GtpCommandResult(true, "Fallback Engine"),
            "version" => new GtpCommandResult(true, "1"),
            "known_command fixed_handicap" => new GtpCommandResult(false, "not implemented"),
            "list_commands" => new GtpCommandResult(true, "fixed_handicap\nplay"),
            _ => new GtpCommandResult(false, "unknown command"),
        }));
        var fallback = probe.ProbeAsync(fallbackSession, ["fixed_handicap"])
            .GetAwaiter()
            .GetResult();
        Require(fallback.Get("fixed_handicap").Support == GtpCommandSupport.Supported, "list_commands must recover a rejected known_command probe.");
        Require(fallback.Get("fixed_handicap").Evidence == GtpCapabilityEvidence.ListCommands, "Fallback evidence must identify list_commands.");

        var contradictorySession = new StubGtpCommandSession((command, _) => Task.FromResult(command switch
        {
            "name" => new GtpCommandResult(true, "Contradictory Engine"),
            "version" => new GtpCommandResult(true, "1"),
            "known_command loadsgf" => new GtpCommandResult(true, "true"),
            "list_commands" => new GtpCommandResult(true, "play\ngenmove"),
            _ => new GtpCommandResult(false, "unknown command"),
        }));
        var contradictory = probe.ProbeAsync(contradictorySession, ["loadsgf"])
            .GetAwaiter()
            .GetResult();
        Require(contradictory.Get("loadsgf").Support == GtpCommandSupport.Unknown, "Contradictory engine answers must not be guessed.");
        Require(
            contradictory.Get("loadsgf").Evidence == GtpCapabilityEvidence.ContradictoryResponses,
            "Contradictory capability evidence must be retained for diagnosis.");

        var timeoutSession = new StubGtpCommandSession((command, _) => command switch
        {
            "name" => Task.FromResult(new GtpCommandResult(true, "Slow Engine")),
            "version" => Task.FromResult(new GtpCommandResult(true, "1")),
            _ => Task.FromException<GtpCommandResult>(new TimeoutException("probe timeout")),
        });
        var timedOut = probe.ProbeAsync(timeoutSession, ["set_free_handicap"])
            .GetAwaiter()
            .GetResult();
        Require(timedOut.Get("set_free_handicap").Support == GtpCommandSupport.Unknown, "A timeout must remain unknown instead of becoming unsupported.");
        Require(timedOut.Get("set_free_handicap").Evidence == GtpCapabilityEvidence.Unavailable, "Timeout evidence must be unavailable.");
        Require(timedOut.Diagnostics.Any(message => message.Contains("TimeoutException", StringComparison.Ordinal)), "Timeout diagnostics must be retained.");

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationPropagated = false;
        try
        {
            probe.ProbeAsync(
                new StubGtpCommandSession((_, token) => Task.FromCanceled<GtpCommandResult>(token)),
                ["loadsgf"],
                cancellationSource.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            cancellationPropagated = true;
        }

        Require(cancellationPropagated, "Caller cancellation must not be converted into an unknown capability.");
    }

    private static void VerifyStandardHandicapStrategies()
    {
        var supportedCapabilities = new GtpCapabilitySet(
            "Handicap Engine",
            "1",
            [
                new GtpCommandCapability(
                    "fixed_handicap",
                    GtpCommandSupport.Supported,
                    GtpCapabilityEvidence.KnownCommand),
                new GtpCommandCapability(
                    "set_free_handicap",
                    GtpCommandSupport.Supported,
                    GtpCapabilityEvidence.KnownCommand),
            ]);
        var unsupportedFixed = new GtpCapabilitySet(
            "No Fixed Handicap",
            "1",
            [
                new GtpCommandCapability(
                    "fixed_handicap",
                    GtpCommandSupport.Unsupported,
                    GtpCapabilityEvidence.KnownCommand),
            ]);
        var unknownCapabilities = new GtpCapabilitySet("Unknown Engine", null, []);

        var fixedRequest = new InitialPositionRequest(
            19,
            0.5m,
            GoStone.White,
            FixedHandicapPoints.Get(19, 2)
                .Select(point => new MatchSetupStone(GoStone.Black, point)));
        var fixedClassification = InitialPositionClassifier.Classify(fixedRequest);
        var fixedStrategy = FixedHandicapStrategy.Instance;
        Require(fixedStrategy.CanApply(fixedRequest, fixedClassification), "A standard fixed handicap with White to play must be applicable.");
        Require(fixedStrategy.CanAttempt(fixedRequest, fixedClassification, supportedCapabilities), "A supported fixed handicap must be attempted.");
        Require(!fixedStrategy.CanAttempt(fixedRequest, fixedClassification, unsupportedFixed), "A known unsupported fixed handicap must not be attempted.");
        Require(fixedStrategy.CanAttempt(fixedRequest, fixedClassification, unknownCapabilities), "An unknown capability must remain available for a user-requested attempt.");
        Require(
            fixedStrategy.BuildCommands(fixedRequest).SequenceEqual(
            [
                "boardsize 19",
                "komi 0.5",
                "clear_board",
                "fixed_handicap 2",
            ]),
            "The fixed-handicap command sequence is incorrect.");

        var verified = fixedStrategy.VerifyResponse(fixedRequest, "Q16 D4");
        Require(verified.Status == InitialPositionVerificationStatus.Verified, "A matching fixed-handicap response must be verified regardless of vertex order.");
        var mismatch = fixedStrategy.VerifyResponse(fixedRequest, "D4");
        Require(mismatch.Status == InitialPositionVerificationStatus.PositionMismatch, "A missing fixed-handicap vertex must be reported as a mismatch.");
        Require(mismatch.ExpectedVertices.Count == 2 && mismatch.ActualVertices.Count == 1, "Mismatch diagnostics must retain expected and actual vertices.");
        var invalid = fixedStrategy.VerifyResponse(fixedRequest, "D4 I4");
        Require(invalid.Status == InitialPositionVerificationStatus.InvalidResponse, "An invalid fixed-handicap vertex must be diagnosed.");
        var duplicate = fixedStrategy.VerifyResponse(fixedRequest, "D4 D4");
        Require(duplicate.Status == InitialPositionVerificationStatus.InvalidResponse, "A duplicate fixed-handicap vertex must be diagnosed.");

        var blackToPlayRequest = new InitialPositionRequest(
            19,
            0.5m,
            GoStone.Black,
            FixedHandicapPoints.Get(19, 2)
                .Select(point => new MatchSetupStone(GoStone.Black, point)));
        Require(
            !fixedStrategy.CanApply(blackToPlayRequest, InitialPositionClassifier.Classify(blackToPlayRequest)),
            "fixed_handicap must not be used when the requested starting turn is Black.");

        var freeRequest = new InitialPositionRequest(
            9,
            6.5m,
            GoStone.White,
            [
                new MatchSetupStone(GoStone.Black, new GoPoint(0, 0)),
                new MatchSetupStone(GoStone.Black, new GoPoint(4, 4)),
            ]);
        var freeClassification = InitialPositionClassifier.Classify(freeRequest);
        var freeStrategy = SetFreeHandicapStrategy.Instance;
        Require(freeStrategy.CanApply(freeRequest, freeClassification), "A specified black setup with White to play must use set_free_handicap.");
        Require(freeStrategy.CanAttempt(freeRequest, freeClassification, supportedCapabilities), "A supported free handicap must be attempted.");
        Require(
            freeStrategy.BuildCommands(freeRequest).SequenceEqual(
            [
                "boardsize 9",
                "komi 6.5",
                "clear_board",
                "set_free_handicap A9 E5",
            ]),
            "The set-free-handicap command sequence is incorrect.");

        var mixedRequest = new InitialPositionRequest(
            9,
            6.5m,
            GoStone.White,
            [
                new MatchSetupStone(GoStone.Black, new GoPoint(0, 0)),
                new MatchSetupStone(GoStone.White, new GoPoint(8, 8)),
            ]);
        var mixedClassification = InitialPositionClassifier.Classify(mixedRequest);
        Require(!freeStrategy.CanApply(mixedRequest, mixedClassification), "set_free_handicap must reject a black-and-white setup.");

        var mixedBuildRejected = false;
        try
        {
            freeStrategy.BuildCommands(mixedRequest);
        }
        catch (InvalidOperationException)
        {
            mixedBuildRejected = true;
        }

        Require(mixedBuildRejected, "An inapplicable set_free_handicap strategy must not build commands.");
    }

    private static void VerifyLoadSgfStrategyAndTemporaryFile()
    {
        var mixedRequest = new InitialPositionRequest(
            9,
            6.5m,
            GoStone.White,
            [
                new MatchSetupStone(GoStone.Black, new GoPoint(0, 0)),
                new MatchSetupStone(GoStone.Black, new GoPoint(4, 4)),
                new MatchSetupStone(GoStone.White, new GoPoint(8, 8)),
            ]);
        var mixedClassification = InitialPositionClassifier.Classify(mixedRequest);
        var strategy = LoadSgfStrategy.Instance;
        var capabilities = new GtpCapabilitySet(
            "SGF Engine",
            "1",
            [
                new GtpCommandCapability(
                    "loadsgf",
                    GtpCommandSupport.Supported,
                    GtpCapabilityEvidence.KnownCommand),
            ]);

        Require(strategy.CanApply(mixedRequest, mixedClassification), "loadsgf must support a black-and-white setup.");
        Require(strategy.CanAttempt(mixedRequest, mixedClassification, capabilities), "A supported loadsgf strategy must be attempted.");
        var document = strategy.CreateDocument(mixedRequest);
        Require(
            document.Content == "(;GM[1]FF[4]CA[UTF-8]SZ[9]KM[6.5]PL[W]AB[aa][ee]AW[ii])\n",
            "The minimal initial-position SGF is incorrect.");
        Require(document.SuggestedFileName.EndsWith(".sgf", StringComparison.OrdinalIgnoreCase), "The initial-position document must suggest an SGF filename.");

        var blackToPlayDocument = InitialPositionSgfBuilder.Build(new InitialPositionRequest(9, 0m, GoStone.Black));
        Require(blackToPlayDocument.Content.Contains("PL[B]", StringComparison.Ordinal), "SGF must retain a Black starting turn.");
        Require(!blackToPlayDocument.Content.Contains("AB[", StringComparison.Ordinal), "An empty SGF must omit AB.");
        Require(!blackToPlayDocument.Content.Contains("AW[", StringComparison.Ordinal), "An empty SGF must omit AW.");

        const string spacedPath = @"C:\Engine Data\initial position.sgf";
        Require(
            strategy.BuildCommands(
                mixedRequest,
                new InitialPositionStrategyContext(spacedPath)).Single() ==
            "loadsgf \"C:\\Engine Data\\initial position.sgf\"",
            "An automatic loadsgf path with spaces must be double quoted.");
        Require(
            strategy.BuildCommands(
                mixedRequest,
                new InitialPositionStrategyContext(
                    spacedPath,
                    GtpFilePathArgumentStyle.DoubleQuoted,
                    12)).Single() ==
            "loadsgf \"C:\\Engine Data\\initial position.sgf\" 12",
            "A profile-selected loadsgf move number is incorrect.");
        Require(
            strategy.BuildCommands(
                mixedRequest,
                new InitialPositionStrategyContext(
                    "initial-position.sgf",
                    GtpFilePathArgumentStyle.Unquoted)).Single() ==
            "loadsgf initial-position.sgf",
            "An unquoted loadsgf path is incorrect.");
        Require(
            strategy.VerifySuccessfulResponse().Status == InitialPositionVerificationStatus.Unverified,
            "A successful loadsgf response must remain unverified without a portable board query.");

        var missingPathRejected = false;
        try
        {
            strategy.BuildCommands(mixedRequest);
        }
        catch (InvalidOperationException)
        {
            missingPathRejected = true;
        }

        Require(missingPathRejected, "loadsgf must not build a command before the host creates its document.");

        var unsafePathRejected = false;
        try
        {
            strategy.BuildCommands(
                mixedRequest,
                new InitialPositionStrategyContext("position.sgf\nquit"));
        }
        catch (ArgumentException)
        {
            unsafePathRejected = true;
        }

        Require(unsafePathRejected, "A loadsgf path must not inject another GTP line.");

        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"KifuwarabeGo2026 GTP smoke {Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        var utf8Document = new InitialPositionDocument("initial-position.sgf", "(;C[指定局面])\n");
        string temporaryPath;
        try
        {
            using (var temporaryFile = GtpInitialPositionSgfFile.Create(utf8Document, temporaryRoot))
            {
                temporaryPath = temporaryFile.FilePath;
                Require(File.Exists(temporaryPath), "The GUI host must materialize the initial-position SGF.");
                Require(File.ReadAllText(temporaryPath, Encoding.UTF8) == utf8Document.Content, "The materialized SGF content is incorrect.");
                var bytes = File.ReadAllBytes(temporaryPath);
                Require(
                    bytes.Length < 3 || bytes[0] != 0xEF || bytes[1] != 0xBB || bytes[2] != 0xBF,
                    "The temporary SGF must be UTF-8 without BOM.");
            }

            Require(!File.Exists(temporaryPath), "Disposing the GUI SGF file must delete it.");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: false);
            }
        }
    }

    private static void VerifyInitialPositionConcierge()
    {
        var concierge = new InitialPositionConcierge();
        var allSupported = new GtpCapabilitySet(
            "Concierge Engine",
            "1",
            [
                Capability("fixed_handicap", GtpCommandSupport.Supported),
                Capability("set_free_handicap", GtpCommandSupport.Supported),
                Capability("loadsgf", GtpCommandSupport.Supported),
                Capability("play", GtpCommandSupport.Supported),
            ]);
        var mixedRequest = new InitialPositionRequest(
            9,
            6.5m,
            GoStone.White,
            [
                new MatchSetupStone(GoStone.Black, new GoPoint(0, 0)),
                new MatchSetupStone(GoStone.White, new GoPoint(8, 8)),
            ]);
        var mixedHost = new StubInitialPositionExecutionHost((_, _) =>
            Task.FromResult(new GtpCommandResult(true, string.Empty)));
        var mixedResult = concierge.ExecuteAsync(mixedHost, mixedRequest, allSupported)
            .GetAwaiter()
            .GetResult();

        Require(mixedResult.Attempts.Count == 3, "The concierge must show two inapplicable handicap methods before loadsgf.");
        Require(mixedResult.Attempts[0].Status == InitialPositionAttemptStatus.NotApplicable, "fixed_handicap must be visibly inapplicable to a mixed setup.");
        Require(mixedResult.Attempts[1].Status == InitialPositionAttemptStatus.NotApplicable, "set_free_handicap must be visibly inapplicable to a mixed setup.");
        Require(mixedResult.Attempts[2].Status == InitialPositionAttemptStatus.UnverifiedSuccess, "A successful loadsgf must pause as unverified.");
        Require(mixedResult.CanTryAnotherMethod, "An unverified loadsgf result must offer another method.");
        Require(mixedResult.Continuation == new InitialPositionConciergeCursor(3, true), "The loadsgf continuation cursor is incorrect.");
        Require(mixedHost.DocumentLeases.Count == 1 && mixedHost.DocumentLeases[0].IsDisposed, "The concierge must dispose its temporary SGF lease.");
        Require(mixedHost.RecoveryModes.Count == 0, "An unverified success must wait for the user before recovery.");

        var continuedResult = concierge.ExecuteAsync(
                mixedHost,
                mixedRequest,
                allSupported,
                cursor: mixedResult.Continuation)
            .GetAwaiter()
            .GetResult();
        Require(mixedHost.RecoveryModes.SequenceEqual([InitialPositionRecoveryMode.RestartSession]), "Trying another method must recover the engine first.");
        Require(continuedResult.Attempts.Count == 1, "Continuation must resume at the next method.");
        Require(continuedResult.Attempts[0].Method == InitialPositionMethod.SequentialPlay, "Continuation must try sequential play next.");
        Require(continuedResult.Attempts[0].Status == InitialPositionAttemptStatus.UnverifiedSuccess, "Sequential play must remain visibly unverified.");
        Require(!continuedResult.CanTryAnotherMethod, "The final method must not offer a nonexistent continuation.");

        var fixedRequest = new InitialPositionRequest(
            19,
            0.5m,
            GoStone.White,
            FixedHandicapPoints.Get(19, 2)
                .Select(point => new MatchSetupStone(GoStone.Black, point)));
        var verifiedHost = new StubInitialPositionExecutionHost((command, _) =>
            Task.FromResult(command.StartsWith("fixed_handicap", StringComparison.Ordinal)
                ? new GtpCommandResult(true, "Q16 D4")
                : new GtpCommandResult(true, string.Empty)));
        var verifiedResult = concierge.ExecuteAsync(verifiedHost, fixedRequest, allSupported)
            .GetAwaiter()
            .GetResult();
        Require(verifiedResult.IsVerified, "A matching fixed_handicap response must complete the concierge as verified.");
        Require(verifiedResult.Attempts.Count == 1 && !verifiedResult.CanTryAnotherMethod, "A verified method must stop without another-method prompt.");
        Require(verifiedResult.LastAttempt?.StartedAt != default, "An executed attempt must record its start time.");
        Require(verifiedResult.LastAttempt?.Duration >= TimeSpan.Zero, "An executed attempt must record a nonnegative duration.");

        var rejectionHost = new StubInitialPositionExecutionHost((command, _) =>
            Task.FromResult(command.StartsWith("fixed_handicap", StringComparison.Ordinal)
                ? new GtpCommandResult(false, "unsupported handicap count")
                : new GtpCommandResult(true, string.Empty)));
        var clearBoardProfile = new StubCompatibilityProfile(
            [FixedHandicapStrategy.Instance, SetFreeHandicapStrategy.Instance, LoadSgfStrategy.Instance, SequentialPlayStrategy.Instance],
            InitialPositionRecoveryMode.ClearBoard);
        var rejectionResult = concierge.ExecuteAsync(
                rejectionHost,
                fixedRequest,
                allSupported,
                clearBoardProfile)
            .GetAwaiter()
            .GetResult();
        Require(rejectionResult.Attempts.Count == 2, "A rejected fixed handicap must automatically fall back to set_free_handicap.");
        Require(rejectionResult.Attempts[0].Status == InitialPositionAttemptStatus.CommandRejected, "The rejected command must remain visible.");
        Require(rejectionResult.Attempts[0].FailedCommand == "fixed_handicap 2", "The rejected command must be diagnosed.");
        Require(rejectionResult.Attempts[1].Method == InitialPositionMethod.SetFreeHandicap, "The next compatible method is incorrect.");
        Require(rejectionHost.RecoveryModes.SequenceEqual([InitialPositionRecoveryMode.ClearBoard]), "Automatic fallback must use the profile recovery policy.");

        var mismatchHost = new StubInitialPositionExecutionHost((command, _) =>
            Task.FromResult(command.StartsWith("fixed_handicap", StringComparison.Ordinal)
                ? new GtpCommandResult(true, "D4")
                : new GtpCommandResult(true, string.Empty)));
        var mismatchResult = concierge.ExecuteAsync(mismatchHost, fixedRequest, allSupported)
            .GetAwaiter()
            .GetResult();
        Require(mismatchResult.LastAttempt?.Status == InitialPositionAttemptStatus.PositionMismatch, "A fixed-handicap mismatch must pause for the user.");
        Require(mismatchHost.RecoveryModes.Count == 0, "A mismatch must not automatically fall back.");
        Require(mismatchResult.Continuation?.RecoveryRequired == true, "Another method after a mismatch must require recovery.");

        var unsupportedCapabilities = new GtpCapabilitySet(
            "Limited Engine",
            "1",
            [
                Capability("fixed_handicap", GtpCommandSupport.Unsupported),
                Capability("set_free_handicap", GtpCommandSupport.Supported),
                Capability("loadsgf", GtpCommandSupport.Supported),
                Capability("play", GtpCommandSupport.Supported),
            ]);
        var unsupportedHost = new StubInitialPositionExecutionHost((_, _) =>
            Task.FromResult(new GtpCommandResult(true, string.Empty)));
        var unsupportedResult = concierge.ExecuteAsync(unsupportedHost, fixedRequest, unsupportedCapabilities)
            .GetAwaiter()
            .GetResult();
        Require(unsupportedResult.Attempts[0].Status == InitialPositionAttemptStatus.Unsupported, "A known unsupported method must be shown without execution.");
        Require(!unsupportedHost.Commands.Any(command => command.StartsWith("fixed_handicap", StringComparison.Ordinal)), "A known unsupported method must not send its command.");

        var transportHost = new StubInitialPositionExecutionHost((command, _) =>
            command == "komi 0.5"
                ? Task.FromException<GtpCommandResult>(new TimeoutException("engine timeout"))
                : Task.FromResult(new GtpCommandResult(true, string.Empty)));
        var transportResult = concierge.ExecuteAsync(transportHost, fixedRequest, allSupported)
            .GetAwaiter()
            .GetResult();
        Require(transportResult.LastAttempt?.Status == InitialPositionAttemptStatus.TransportFailure, "A transport failure must pause instead of auto-fallback.");
        Require(transportResult.LastAttempt?.FailedCommand == "komi 0.5", "The failed transport command must be retained.");

        var recoveryFailureHost = new StubInitialPositionExecutionHost((command, _) =>
            Task.FromResult(command.StartsWith("fixed_handicap", StringComparison.Ordinal)
                ? new GtpCommandResult(false, "rejected")
                : new GtpCommandResult(true, string.Empty)))
        {
            RecoveryException = new InvalidOperationException("restart failed"),
        };
        var recoveryFailure = concierge.ExecuteAsync(recoveryFailureHost, fixedRequest, allSupported)
            .GetAwaiter()
            .GetResult();
        Require(recoveryFailure.Diagnostics.Any(detail => detail.Contains("restart failed", StringComparison.Ordinal)), "A recovery failure must be visible to the GUI.");
        Require(recoveryFailure.Continuation?.RecoveryRequired == true, "A failed recovery must remain required before continuation.");
        Require(!recoveryFailureHost.Commands.Any(command => command.StartsWith("set_free_handicap", StringComparison.Ordinal)), "No next method may run after failed recovery.");

        var nothingSupported = new GtpCapabilitySet(
            "Unsupported Engine",
            "1",
            [
                Capability("fixed_handicap", GtpCommandSupport.Unsupported),
                Capability("set_free_handicap", GtpCommandSupport.Unsupported),
                Capability("loadsgf", GtpCommandSupport.Unsupported),
                Capability("play", GtpCommandSupport.Unsupported),
            ]);
        var noCommandHost = new StubInitialPositionExecutionHost((_, _) =>
            throw new InvalidOperationException("No command should be sent."));
        var noMethodResult = concierge.ExecuteAsync(noCommandHost, fixedRequest, nothingSupported)
            .GetAwaiter()
            .GetResult();
        Require(noMethodResult.Attempts.Count == 4, "Every unavailable method must remain visible in progress.");
        Require(noMethodResult.Attempts.All(attempt => attempt.Status == InitialPositionAttemptStatus.Unsupported), "All known unsupported methods must be labeled unsupported.");
        Require(noCommandHost.Commands.Count == 0, "No GTP command may be sent when every method is unsupported.");
        Require(!noMethodResult.IsVerified && !noMethodResult.IsUnverifiedSuccess, "Exhausting all methods must not report setup success.");
        Require(!noMethodResult.CanTryAnotherMethod, "Exhausting all methods must not offer a nonexistent method.");
    }

    private static GtpCommandCapability Capability(string command, GtpCommandSupport support) =>
        new(command, support, GtpCapabilityEvidence.KnownCommand);

    private static void VerifyGoMatchAssembly(Assembly goMatchAssembly)
    {
        VerifyTargetFramework(goMatchAssembly, "Reference.PlayRoomEngine.Go");
        VerifyNoPlatformInvokes(goMatchAssembly, "Reference.PlayRoomEngine.Go");

        var references = goMatchAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Require(
            references.Contains("KifuwarabeGo2026.Reference.PlayDomain.Go"),
            "Go match support must reference the Go play-domain assembly.");
        Require(
            !references.Contains("KifuwarabeGo2026.GameOasis.Gui"),
            "Reference.PlayRoomEngine.Go must not reference the GUI assembly.");
        Require(
            !references.Contains("MonoGame.Framework"),
            "Reference.PlayRoomEngine.Go must not reference MonoGame.");
        Require(
            goMatchAssembly.GetTypes().All(type => type.Namespace is null ||
                !type.Namespace.Contains(".LegacyMatch", StringComparison.Ordinal)),
            "Reference.PlayRoomEngine.Go must not publish the retired LegacyMatch namespace.");

        foreach (var forbiddenReference in ForbiddenAssemblyReferences)
        {
            Require(
                !references.Contains(forbiddenReference),
                $"Reference.PlaySpace.Go directly references Windows-only assembly '{forbiddenReference}'.");
        }

        VerifyMatchStateTransitions();
    }

    private static void VerifyGameAgnosticConciergeAssembly(Assembly conciergeAssembly)
    {
        var references = conciergeAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Require(!references.Contains("KifuwarabeGo2026.Reference.PlayDomain.Go"),
            "Concierge must not know the Go foundation assembly.");
        Require(!references.Contains("KifuwarabeGo2026.Reference.PlayRoomEngine.Go"),
            "Concierge must not know the Go play-space implementation.");
        Require(!references.Contains("KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki"),
            "Concierge must not know the Ponnuki play-space implementation.");
    }

    private static void VerifyMatchStateTransitions()
    {
        var session = new MatchSession(9);
        var initial = session.Snapshot;
        Require(initial.BoardSize == 9, "Match must create the requested board size.");
        Require(initial.CurrentTurn == GoStone.Black, "Black must play first.");
        Require(initial.Revision == 0 && initial.MoveCount == 0, "A new match must start at revision zero.");

        var outside = session.Play(new GoPoint(-1, 0));
        Require(!outside.Succeeded && outside.Failure == MatchActionFailure.PointOutsideBoard, "An off-board play must be rejected.");
        Require(outside.Snapshot.Revision == 0, "A rejected play must not change the revision.");

        var first = session.Play(new GoPoint(4, 4));
        Require(first.Succeeded && first.PlayedBy == GoStone.Black, "Black's first play must succeed.");
        Require(first.Snapshot.GetStone(new GoPoint(4, 4)) == GoStone.Black, "The played stone is missing from the snapshot.");
        Require(first.Snapshot.CurrentTurn == GoStone.White, "A legal play must pass the turn.");

        var occupied = session.Play(new GoPoint(4, 4));
        Require(!occupied.Succeeded && occupied.Failure == MatchActionFailure.PointOccupied, "An occupied point must be rejected.");
        Require(occupied.Snapshot.Revision == first.Snapshot.Revision, "An occupied-point rejection must not change the revision.");

        var suicideSession = new MatchSession(9);
        PlayRequired(suicideSession, 8, 8);
        PlayRequired(suicideSession, 1, 0);
        PlayRequired(suicideSession, 8, 7);
        PlayRequired(suicideSession, 0, 1);
        var suicide = suicideSession.Play(new GoPoint(0, 0));
        Require(!suicide.Succeeded && suicide.Failure == MatchActionFailure.IllegalMove, "A suicide play must be rejected.");
        Require(suicide.Snapshot.Revision == 4, "A rejected suicide must not change the revision.");

        var immutableSnapshot = first.Snapshot;
        session.Play(new GoPoint(5, 4));
        Require(immutableSnapshot.GetStone(new GoPoint(5, 4)) == GoStone.Empty, "A snapshot must not change after later actions.");

        var passSession = new MatchSession(9);
        var blackPass = passSession.Pass();
        Require(blackPass.Succeeded && blackPass.Snapshot.CurrentTurn == GoStone.White, "A pass must pass the turn.");
        var whitePass = passSession.Pass();
        Require(whitePass.Snapshot.IsAwaitingResult, "Two consecutive passes must wait for result agreement.");
        Require(!whitePass.Snapshot.IsCompleted, "Two consecutive passes must not decide the result.");
        Require(whitePass.Snapshot.EndReason == MatchEndReason.ConsecutivePasses, "Two passes must report the correct end reason.");
        Require(whitePass.Snapshot.CurrentTurn == GoStone.Black, "The second pass must complete its turn before the match ends.");
        var afterPassEnd = passSession.Play(new GoPoint(0, 0));
        Require(!afterPassEnd.Succeeded && afterPassEnd.Failure == MatchActionFailure.AwaitingResult, "A match awaiting agreement must reject plays.");

        var limitedSession = new MatchSession(9, moveLimit: 1);
        var limitedMove = limitedSession.Play(new GoPoint(0, 0));
        Require(limitedMove.Snapshot.IsAwaitingResult, "A reached move limit must wait for a result.");
        Require(limitedMove.Snapshot.Winner is null, "A move limit must not infer a winner.");

        var resignSession = new MatchSession(9);
        var resignation = resignSession.Resign();
        Require(resignation.Snapshot.IsCompleted, "Resignation must complete the match.");
        Require(resignation.Snapshot.EndReason == MatchEndReason.Resignation, "Resignation must report the correct end reason.");
        Require(resignation.Snapshot.Winner == GoStone.White, "The opponent of the resigning player must win.");
        Require(resignation.Snapshot.ConfirmedResult?.Outcome == MatchOutcome.WhiteWin, "Resignation must expose a structured result.");
        Require(resignation.Snapshot.MoveCount == 0, "Resignation must not be counted as a played move.");

        VerifySimpleKo();
        VerifyMatchInitialPositionAndHistory();
        VerifyAuthoritativeClockAndObservationEvents();
        VerifyResultAgreementAndAdjudication();
    }

    private static void VerifyAuthoritativeClockAndObservationEvents()
    {
        var session = new MatchSession(9);
        var synchronizedAt = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var deadline = synchronizedAt.AddMinutes(5);
        var update = new MatchClockUpdate(
            1,
            synchronizedAt,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            deadline);

        Require(session.TryApplyAuthoritativeClock(update), "The first authoritative clock update must be accepted.");
        var clockSnapshot = session.Snapshot.Clock;
        Require(clockSnapshot?.Sequence == 1, "The authoritative clock sequence is incorrect.");
        Require(clockSnapshot?.ActiveTurnDeadline == deadline, "The active-turn deadline is incorrect.");
        Require(session.Snapshot.MoveCount == 0, "A clock synchronization must not count as a move.");
        Require(session.Snapshot.Revision == 1, "A clock synchronization must advance the Match revision.");

        var staleUpdate = update with { BlackRemaining = TimeSpan.Zero };
        Require(!session.TryApplyAuthoritativeClock(staleUpdate), "A stale clock sequence must be rejected.");
        Require(session.Snapshot.Clock?.BlackRemaining == TimeSpan.FromMinutes(5), "A stale update must not change the clock.");
        Require(session.Snapshot.Revision == 1, "A stale update must not change the revision.");

        var move = session.Play(new GoPoint(4, 4));
        Require(move.Snapshot.Revision == 2, "An action after clock synchronization must use the next revision.");
        var allEvents = session.GetEventsAfter(0);
        Require(allEvents.Count == 2, "Observers must receive clock and action events.");
        Require(allEvents[0].Kind == MatchEventKind.ClockSynchronized, "The first event must synchronize the clock.");
        Require(allEvents[1].Kind == MatchEventKind.ActionAccepted, "The second event must contain the accepted action.");
        Require(allEvents[1].Action?.Point == new GoPoint(4, 4), "The action event point is incorrect.");

        var incrementalEvents = session.GetEventsAfter(1);
        Require(incrementalEvents.Count == 1 && incrementalEvents[0].Revision == 2, "Observers must resume from a known revision.");

        var invalidClockRejected = false;
        try
        {
            session.TryApplyAuthoritativeClock(update with
            {
                Sequence = 2,
                BlackRemaining = TimeSpan.FromSeconds(-1),
            });
        }
        catch (ArgumentOutOfRangeException)
        {
            invalidClockRejected = true;
        }

        Require(invalidClockRejected, "A negative authoritative remaining time must be rejected.");
    }

    private static void VerifyMatchInitialPositionAndHistory()
    {
        var sourceSetup = new[]
        {
            new MatchSetupStone(GoStone.Black, new GoPoint(3, 3)),
            new MatchSetupStone(GoStone.White, new GoPoint(4, 4)),
        };
        var configuration = new MatchConfiguration(9, startingTurn: GoStone.White, setupStones: sourceSetup);
        var session = new MatchSession(configuration);
        sourceSetup[0] = new MatchSetupStone(GoStone.White, new GoPoint(0, 0));

        var initial = session.Snapshot;
        Require(initial.GetStone(new GoPoint(3, 3)) == GoStone.Black, "Match must copy its initial setup.");
        Require(initial.GetStone(new GoPoint(0, 0)) == GoStone.Empty, "External setup mutations must not affect Match.");
        Require(initial.CurrentTurn == GoStone.White, "Match must honor the configured starting turn.");
        Require(initial.SetupStones.Count == 2 && initial.Actions.Count == 0, "Initial setup and action history are incorrect.");

        var play = session.Play(new GoPoint(5, 5));
        var pass = session.Pass();
        Require(play.Snapshot.Actions.Count == 1, "A play must be added to Match action history.");
        Require(play.Snapshot.Actions[0].Action == MatchActionKind.Play, "The first history action must be a play.");
        Require(play.Snapshot.Actions[0].PlayedBy == GoStone.White, "History must record the acting player.");
        Require(pass.Snapshot.Actions.Count == 2 && pass.Snapshot.Actions[1].Action == MatchActionKind.Pass, "A pass must be added to history.");
        Require(play.Snapshot.Actions.Count == 1, "An earlier snapshot history must remain immutable.");

        var duplicateRejected = false;
        try
        {
            _ = new MatchConfiguration(
                9,
                setupStones:
                [
                    new MatchSetupStone(GoStone.Black, new GoPoint(1, 1)),
                    new MatchSetupStone(GoStone.White, new GoPoint(1, 1)),
                ]);
        }
        catch (ArgumentException)
        {
            duplicateRejected = true;
        }

        Require(duplicateRejected, "Duplicate setup points must be rejected.");
    }

    private static void VerifyGuiMatchIntegration()
    {
        var session = new GoAppSession();
        session.SetPlayerKind(GoStone.Black, GoPlayerKind.Human);
        session.SetPlayerKind(GoStone.White, GoPlayerKind.Human);
        session.StartPlaying();

        Require(session.IsMatchBackedLocalGame, "A normal human-versus-human game must use MatchSession.");
        Require(session.TryPlaceStone(3, 3), "The GUI session must accept a Match-backed play.");
        Require(session.GetStone(3, 3) == GoStone.Black, "The GUI board must reflect the Match snapshot.");
        Require(session.CurrentTurn == GoStone.White, "The GUI turn must follow the Match snapshot.");
        Require(session.CurrentGameRecord.Moves.Count == 1, "A Match-backed play must update the GUI game record.");

        var passSession = new GoAppSession();
        passSession.SetPlayerKind(GoStone.Black, GoPlayerKind.Human);
        passSession.SetPlayerKind(GoStone.White, GoPlayerKind.Human);
        passSession.StartPlaying();
        Require(passSession.Pass() && passSession.Pass(), "The GUI session must pass through Match.");
        Require(passSession.CurrentMode.Kind == GoAppModeKind.GameOver, "The local wrapper must enter its completed-game state after result waiting begins.");
        Require(passSession.Winner is null, "Match must not infer a winner from two passes on an empty board.");
        Require(passSession.IsLocalResultPosition && passSession.LocalReviewTimelineIndex == 3,
            "A completed two-move game must open at the RESULT position after its final move.");
        passSession.SeekLocalReviewTimeline(2);
        Require(!passSession.IsLocalResultPosition && passSession.IsLocalReplayMode && passSession.LocalDisplayMoveIndex == 2,
            "Stepping back from RESULT must expose the final move as a distinct review position.");
        passSession.SeekLocalReviewTimeline(passSession.LocalReviewTimelineMaximum);
        Require(passSession.IsLocalResultPosition,
            "Seeking to the post-game timeline end must return to RESULT without adding a record move.");
        Require(passSession.CurrentGameRecord.Moves.Count == 2,
            "The RESULT timeline position must not be stored as a fictitious game-record move.");
        var completedRecord = passSession.CurrentGameRecord.Clone();
        Require(passSession.StartReviewingGameRecord(completedRecord, out _),
            "A completed record must open in the standard review flow.");
        Require(passSession.MoveReview(passSession.ReviewTimelineMaximum, out _),
            "The standard review flow must advance beyond its final move to RESULT.");
        Require(passSession.IsReviewResultPosition && passSession.ReviewTimelineIndex == 3,
            "The standard review next button must remain enabled on move two of a two-move record.");
        Require(passSession.MoveReview(-1, out _) && !passSession.IsReviewResultPosition && passSession.ReviewMoveIndex == 2,
            "Stepping back from the standard review RESULT must return to the final move.");
        Require(passSession.ReviewMoveCount == 2,
            "The standard review RESULT must not increase the game-record move count.");

        var resignSession = new GoAppSession();
        resignSession.SetPlayerKind(GoStone.Black, GoPlayerKind.Human);
        resignSession.SetPlayerKind(GoStone.White, GoPlayerKind.Human);
        resignSession.StartPlaying();
        Require(resignSession.Resign(), "The GUI session must pass resignation through Match.");
        Require(resignSession.Winner == GoStone.White, "The GUI must reflect the Match resignation winner.");

        var editedSession = new GoAppSession();
        editedSession.SetPlayerKind(GoStone.Black, GoPlayerKind.Human);
        editedSession.SetPlayerKind(GoStone.White, GoPlayerKind.Human);
        editedSession.StartBoardEditing();
        Require(editedSession.TryEditBoardStone(3, 3), "The board editor must place a setup stone.");
        editedSession.FinishBoardEditing();
        editedSession.StartPlaying();
        Require(editedSession.IsMatchBackedLocalGame, "A human game from an edited position must use MatchSession.");
        Require(editedSession.GetStone(3, 3) == GoStone.Black, "The edited setup stone must survive Match construction.");
        Require(editedSession.TryPlaceStone(4, 4), "Match must accept a play after an edited setup.");
        Require(editedSession.CurrentGameRecord.SetupStones.Count == 1, "The GUI record must preserve edited setup stones.");
        Require(editedSession.CurrentGameRecord.Moves.Count == 1, "The GUI record must keep post-setup moves separate.");

        var computerSession = new GoAppSession();
        computerSession.SetPlayerKind(GoStone.Black, GoPlayerKind.Computer);
        computerSession.SetPlayerKind(GoStone.White, GoPlayerKind.Computer);
        computerSession.StartPlaying();
        Require(computerSession.IsMatchBackedLocalGame, "A computer-versus-computer game must use MatchSession.");
        Require(computerSession.TryPlaceStone(2, 2), "An engine move must be accepted through Match.");
        Require(computerSession.CurrentMatchSnapshot?.Actions.Count == 1, "An engine move must appear in Match history.");
    }

    private static void VerifyGtpMatchAdapter()
    {
        var match = new MatchSession(new MatchConfiguration(
            9,
            setupStones:
            [
                new MatchSetupStone(GoStone.Black, new GoPoint(0, 0)),
                new MatchSetupStone(GoStone.White, new GoPoint(8, 8)),
            ]));
        var commands = GtpInitialPositionCommandBuilder.Build(match.Snapshot, 6.5m);

        Require(
            commands.SequenceEqual(
            [
                "boardsize 9",
                "komi 6.5",
                "clear_board",
                "play black A9",
                "play white J1",
            ]),
            "The GTP adapter must send Match setup stones after clearing the engine board.");
        Require(
            typeof(GtpInitialPositionCommandBuilder).Assembly != typeof(MatchSession).Assembly,
            "The GTP adapter must remain outside Match.");
    }

    private static void VerifyResultAgreementAndAdjudication()
    {
        var agreementSession = new MatchSession(9);
        agreementSession.Pass();
        agreementSession.Pass();
        var blackClaim = new MatchResult(MatchOutcome.BlackWin, 2.5m);
        var firstDeclaration = agreementSession.DeclareResult(GoStone.Black, blackClaim);
        Require(firstDeclaration.Accepted && firstDeclaration.Changed && !firstDeclaration.Completed, "The first result declaration must wait for the opponent.");
        Require(firstDeclaration.Snapshot.BlackResultDeclaration == blackClaim, "Black's result declaration is missing.");

        var duplicateRevision = firstDeclaration.Snapshot.Revision;
        var duplicateDeclaration = agreementSession.DeclareResult(GoStone.Black, blackClaim);
        Require(duplicateDeclaration.Accepted && !duplicateDeclaration.Changed, "An identical repeated declaration must be idempotent.");
        Require(duplicateDeclaration.Snapshot.Revision == duplicateRevision, "An idempotent declaration must not advance the revision.");

        var conflictingClaim = new MatchResult(MatchOutcome.WhiteWin, 1.5m);
        var conflict = agreementSession.DeclareResult(GoStone.White, conflictingClaim);
        Require(conflict.Accepted && !conflict.Completed, "Conflicting declarations must remain unresolved.");
        Require(conflict.Snapshot.Phase == MatchPhase.AwaitingResult, "A declaration conflict must remain in result waiting.");

        var agreement = agreementSession.DeclareResult(GoStone.White, blackClaim);
        Require(agreement.Completed && agreement.Snapshot.IsCompleted, "Matching declarations must complete the match.");
        Require(agreement.Snapshot.ConfirmedResult == blackClaim, "The agreed result is incorrect.");
        Require(agreement.Snapshot.Winner == GoStone.Black, "The agreed winner is incorrect.");
        Require(
            agreementSession.GetEventsAfter(0).Any(matchEvent => matchEvent.Kind == MatchEventKind.ResultConfirmed),
            "Observers must receive a result-confirmed event.");

        var resumeSession = new MatchSession(9);
        resumeSession.Pass();
        resumeSession.Pass();
        resumeSession.DeclareResult(GoStone.Black, new MatchResult(MatchOutcome.Draw));
        var resumed = resumeSession.ResumePlay();
        Require(resumed.Accepted && resumed.Snapshot.Phase == MatchPhase.Playing, "A disputed result must be able to resume play.");
        Require(resumed.Snapshot.BlackResultDeclaration is null, "Resuming play must clear old declarations.");
        Require(resumeSession.Play(new GoPoint(0, 0)).Succeeded, "Play must continue after result waiting is resumed.");
        Require(
            resumeSession.GetEventsAfter(0).Any(matchEvent => matchEvent.Kind == MatchEventKind.PlayResumed),
            "Observers must receive a play-resumed event.");

        var adjudicationSession = new MatchSession(9);
        adjudicationSession.Pass();
        adjudicationSession.Pass();
        adjudicationSession.DeclareResult(GoStone.Black, new MatchResult(MatchOutcome.BlackWin));
        adjudicationSession.DeclareResult(GoStone.White, new MatchResult(MatchOutcome.WhiteWin));
        var adjudicated = adjudicationSession.ApplyAdjudicatedResult(new MatchResult(MatchOutcome.NoResult));
        Require(adjudicated.Completed, "An adjudicated result must complete the match.");
        Require(adjudicated.Snapshot.EndReason == MatchEndReason.Adjudication, "Adjudication must report its end reason.");
        Require(adjudicated.Snapshot.ConfirmedResult?.Outcome == MatchOutcome.NoResult, "The adjudicated result is incorrect.");
        Require(adjudicated.Snapshot.Winner is null, "A no-result adjudication must not infer a winner.");
        Require(
            adjudicationSession.GetEventsAfter(0).Any(matchEvent => matchEvent.Kind == MatchEventKind.ResultAdjudicated),
            "Observers must receive an adjudication event.");

        var invalidMarginRejected = false;
        try
        {
            _ = new MatchResult(MatchOutcome.Draw, 0.5m);
        }
        catch (ArgumentException)
        {
            invalidMarginRejected = true;
        }

        Require(invalidMarginRejected, "A draw with a winning margin must be rejected.");
    }

    private static void VerifySimpleKo()
    {
        var session = new MatchSession(9);
        PlayRequired(session, 0, 1);
        PlayRequired(session, 0, 2);
        PlayRequired(session, 1, 0);
        PlayRequired(session, 2, 2);
        PlayRequired(session, 2, 1);
        PlayRequired(session, 1, 3);
        PlayRequired(session, 8, 8);
        PlayRequired(session, 1, 1);
        var capture = session.Play(new GoPoint(1, 2));
        Require(capture.Succeeded && capture.CapturedStones == 1, "The ko capture must take one stone.");
        Require(capture.Snapshot.KoPoint == new GoPoint(1, 1), "The ko point is incorrect.");

        var recapture = session.Play(new GoPoint(1, 1));
        Require(!recapture.Succeeded && recapture.Failure == MatchActionFailure.Ko, "Immediate ko recapture must be rejected.");
        Require(recapture.Snapshot.Revision == capture.Snapshot.Revision, "Rejected ko recapture must not change the revision.");
    }

    private static void PlayRequired(MatchSession session, int x, int y)
    {
        var result = session.Play(new GoPoint(x, y));
        Require(result.Succeeded, $"Required setup play at {x},{y} failed: {result.Failure}.");
    }

    private static void VerifyTargetFramework(Assembly coreAssembly)
        => VerifyTargetFramework(coreAssembly, "Core");

    private static void VerifyTargetFramework(Assembly assembly, string assemblyLabel)
    {
        var framework = assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            ?.FrameworkName;

        Require(
            framework == ".NETCoreApp,Version=v8.0",
            $"{assemblyLabel} must target net8.0, but was '{framework ?? "(unknown)"}'.");
    }

    private static void VerifyAssemblyReferences(Assembly coreAssembly)
    {
        var references = coreAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var forbiddenReference in ForbiddenAssemblyReferences)
        {
            Require(
                !references.Contains(forbiddenReference),
                $"Core directly references Windows-only assembly '{forbiddenReference}'.");
        }
    }

    private static void VerifyNoPlatformInvokes(Assembly coreAssembly)
        => VerifyNoPlatformInvokes(coreAssembly, "Core");

    private static void VerifyNoPlatformInvokes(Assembly assembly, string assemblyLabel)
    {
        var platformInvoke = assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Static
                | BindingFlags.Instance))
            .FirstOrDefault(method =>
                (method.Attributes & MethodAttributes.PinvokeImpl) != 0);

        Require(
            platformInvoke is null,
            platformInvoke is null
                ? string.Empty
                : $"{assemblyLabel} contains P/Invoke method '{platformInvoke.DeclaringType?.FullName}.{platformInvoke.Name}'.");
    }

    private static void VerifyPortableFallbacks()
    {
        var platform = new PortablePlatformServices();

        Require(!platform.TrySetText("smoke"), "Clipboard fallback must report unsupported.");
        Require(platform.GetFileName("engine") == "engine", "Portable executable name must not add .exe.");
        Require(platform.SelectionFilters.Count > 0, "Executable selection needs at least one fallback filter.");
        Require(platform.RasterizePng("smoke", 16, false).Length > 8, "Text fallback must return PNG bytes.");
        Require(
            platform.GetWrappedPageCount("smoke", 320, 180, 16, 2) == 1,
            "Text fallback must expose one safe placeholder page.");
    }

    private static void VerifyComposition()
    {
        Func<Game1> composition = CreateGame;
        Require(composition is not null, "Game composition must compile.");
    }

    private static void VerifyScoreAxisScaling()
    {
        Require(
            MoveTrendScoreAxis.CalculateMaximum([]) == 20.0,
            "Empty score series must use the default ±20 axis.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.Black, 19.9, null)]) == 20.0,
            "Scores within ±20 must retain the default axis.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.Black, 21.0, null)]) == 30.0,
            "A score of 21 must expand the axis to ±30.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.White, -47.0, null)]) == 50.0,
            "A score of -47 must expand the axis to ±50.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.Black, 50.0, null)]) == 60.0,
            "An exact outer tick must receive headroom.");
    }

    private static void VerifyCommentNavigation()
    {
        var moves = new[]
        {
            new GoGameMove(GoStone.Black, null, "first"),
            new GoGameMove(GoStone.White, null, ""),
            new GoGameMove(GoStone.Black, null, "second"),
            new GoGameMove(GoStone.White, null, "third"),
        };

        Require(MoveCommentNavigator.Count(moves) == 3, "Comment count must ignore empty comments.");
        Require(MoveCommentNavigator.GetOrdinal(moves, 1) == 1, "First comment ordinal is incorrect.");
        Require(MoveCommentNavigator.GetOrdinal(moves, 2) == 0, "Uncommented move must not have an ordinal.");
        Require(MoveCommentNavigator.GetOrdinal(moves, 4) == 3, "Last comment ordinal is incorrect.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 0, 1) == 1,
            "Next comment from the beginning must select the first comment.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 1, 1) == 3,
            "Next comment navigation is incorrect.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 4, -1) == 3,
            "Previous comment navigation is incorrect.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 4, 1) is null,
            "Comment navigation must not wrap at the end.");
    }

    private static void VerifyCatalogOrderEditor()
    {
        var source = new[] { "A", "B", "C", "D", "E", "F", "G" };
        var editor = new CatalogOrderEditor<string>();
        editor.Open(source, 6, 3);
        Require(editor.FirstVisiblePageIndex == 2, "Order editor must open with the selected page on the left.");

        editor.MoveSelected(-3);
        Require(editor.SelectedIndex == 3, "Page move must update the selected index.");
        Require(editor.Items.SequenceEqual(["A", "B", "C", "G", "D", "E", "F"]), "Page move produced the wrong order.");

        editor.BeginDrag(3);
        editor.DragTo(1);
        editor.EndDrag();
        Require(editor.Items.SequenceEqual(["A", "G", "B", "C", "D", "E", "F"]), "Drag move produced the wrong order.");

        editor.MoveSelectedToTop();
        Require(editor.Items.SequenceEqual(["G", "A", "B", "C", "D", "E", "F"]), "Move-to-top produced the wrong order.");
        Require(editor.Commit().SequenceEqual(["G", "A", "B", "C", "D", "E", "F"]), "Commit must return the edited order.");

        editor.Open(source, 0, 3);
        editor.MoveSelected(1);
        editor.Cancel();
        Require(source.SequenceEqual(["A", "B", "C", "D", "E", "F", "G"]), "Cancel must not mutate the source list.");
    }

    private static void VerifyCgosConnectionOrder()
    {
        var session = new GoAppSession();
        session.SetCgosConnectionProfiles(
        [
            new CgosConnectionProfile("A", "a.example", 6809, "", ""),
            new CgosConnectionProfile("B", "b.example", 6809, "", ""),
            new CgosConnectionProfile("C", "c.example", 6809, "", ""),
        ]);
        session.SelectCgosConnectionProfile(1);
        session.OpenCgosConnectionOrderEditor();
        session.CgosConnectionOrderEditor.MoveSelected(-1);
        var ordered = session.CommitCgosConnectionOrderEditor();

        Require(ordered.Select(profile => profile.DisplayName).SequenceEqual(["B", "A", "C"]), "CGOS connection order was not committed.");
        Require(session.SelectedCgosConnectionProfile.DisplayName == "B", "CGOS selection must follow the same profile after reordering.");
    }

    private static void VerifyOptionalCgosInputs()
    {
        var session = new GoAppSession();
        Require(!session.IsCgosPlayer2InputEnabled, "CGOS Player 2 input must be disabled initially.");
        Require(!session.IsCgosAdminInputEnabled, "CGOS Admin input must be disabled initially.");

        session.ToggleCgosPlayer2Input();
        session.ToggleCgosAdminInput();
        Require(session.IsCgosPlayer2InputEnabled, "CGOS Player 2 input toggle did not enable the panel.");
        Require(session.IsCgosAdminInputEnabled, "CGOS Admin input toggle did not enable the panel.");
    }

    private static void VerifyTextBoxEditing()
    {
        var clipboard = new TestClipboardService();
        var controller = new TextBoxController(20);
        var frame = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

        controller.Begin("abcd", 1);
        controller.BeginMouseSelection(1, false);
        controller.UpdateMouseSelection(3);
        controller.EndMouseSelection();
        Require(controller.SelectionStart == 1 && controller.SelectionLength == 2, "Mouse selection range is incorrect.");
        controller.TryInputCharacter('X');
        Require(controller.Text == "aXd", "Typing must replace the selected text.");

        var clickTimestamp = 1d;
        var doubleClickController = new TextBoxController(20, () => clickTimestamp);
        doubleClickController.Begin("double click", 3);
        doubleClickController.BeginMouseSelection(3, false);
        doubleClickController.EndMouseSelection();
        clickTimestamp = 1.2d;
        doubleClickController.BeginMouseSelection(4, false);
        Require(
            doubleClickController.SelectionStart == 0 &&
            doubleClickController.SelectionLength == doubleClickController.Text.Length &&
            !doubleClickController.IsMouseSelecting,
            "Double-clicking a text box must select all text and preserve the selection.");

        controller.Begin("abcd", 2);
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftShift, Keys.Right),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.SelectionStart == 2 && controller.SelectionLength == 1, "Shift+Right must extend the selection.");

        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.A),
            new KeyboardState(),
            frame,
            clipboard);
        controller.HandleKeyboard(new KeyboardState(Keys.Back), new KeyboardState(), frame, clipboard);
        Require(controller.Text == "", "Backspace must delete the complete selection.");

        controller.Begin("abcd", 2);
        controller.BeginMouseSelection(2, false);
        controller.EndMouseSelection();
        controller.HandleKeyboard(new KeyboardState(Keys.Back), new KeyboardState(), frame, clipboard);
        Require(controller.Text == "acd", "Backspace must delete the character before the caret.");
        Require(controller.SelectionLength == 0, "Backspace must not create a selection after a click.");

        clipboard.Text = "paste";
        controller.Begin("ab", 1);
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.V),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "apasteb", "Ctrl+V must insert clipboard text at the caret.");

        controller.BeginMouseSelection(1, false);
        controller.UpdateMouseSelection(6);
        controller.EndMouseSelection();
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.C),
            new KeyboardState(),
            frame,
            clipboard);
        Require(clipboard.Text == "paste", "Ctrl+C must copy the selected text.");

        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.X),
            new KeyboardState(),
            frame,
            clipboard,
            allowClipboardExport: false);
        Require(controller.Text == "apasteb", "Password-style Ctrl+X must not copy or delete text.");

        controller.Begin("abc", 3);
        controller.TryInputCharacter('d');
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "abc" && controller.CaretIndex == 3, "Ctrl+Z must undo the previous text edit.");
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "abcd" && controller.CaretIndex == 4, "Ctrl+Y must redo the previous text edit.");

        controller.TryInputCharacter('e');
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new KeyboardState(),
            frame,
            clipboard);
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)),
            clipboard);
        Require(controller.Text == "abc", "Holding Ctrl+Z must repeat undo.");
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new KeyboardState(),
            frame,
            clipboard);
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)),
            clipboard);
        Require(controller.Text == "abcde", "Holding Ctrl+Y must repeat redo.");

        controller.Begin("abc", 3);
        controller.TryInputCharacter('d');
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new KeyboardState(),
            frame,
            clipboard);
        controller.TryInputCharacter('X');
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "abcX", "A new edit must discard the redo history.");

        controller.Begin("abcd", 4);
        controller.BeginMouseSelection(4, false);
        controller.UpdateMouseSelection(3);
        controller.HandleKeyboard(
            new KeyboardState(Keys.Delete),
            new KeyboardState(),
            frame,
            clipboard);
        controller.UpdateMouseSelection(4);
        controller.HandleKeyboard(
            new KeyboardState(Keys.Delete),
            new KeyboardState(Keys.Delete),
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)),
            clipboard);
        Require(controller.Text == "abc", "Delete key repeat must tolerate a selection endpoint beyond the shortened text.");
    }

    private static void VerifyDefaultCgosConnection()
    {
        Require(
            File.Exists(ReleaseDefaultSettings.FilePath),
            $"Release default settings file was not found: {ReleaseDefaultSettings.FilePath}");
        var settings = new ApplicationSettings();
        Require(
            settings.TournamentRules.Count == 3,
            "Default tournament rules must be loaded from default-settings.json.");
        Require(
            ReleaseDefaultSettings.Current.SchemaVersion == ReleaseDefaultSettings.CurrentSchemaVersion,
            "Release default settings schema version is incorrect.");
        Require(
            ReleaseDefaultSettings.Current.EngineSettings.GtpEngines.Count == 1,
            "Release default settings must contain one editable GTP engine example.");
        Require(settings.CgosConnections.Count > 0, "Default settings must contain a CGOS connection.");
        var profile = settings.CgosConnections[0];
        Require(profile.DisplayName == "Yamashita CGOS Server", "Yamashita CGOS Server must be the first default connection.");
        Require(profile.Host == "yss-aya.com" && profile.Port == 6809, "Yamashita CGOS Server endpoint is incorrect.");
    }

    private static void VerifyTournamentRulesJsonCompatibility()
    {
        const string legacyJson =
            """
            {
              "DisplayName": "Legacy",
              "Rule": "chinese",
              "MainTimeMinutes": 61,
              "MainTimeSeconds": 2,
              "MoveLimit": 400
            }
            """;
        var legacy = JsonSerializer.Deserialize<TournamentRules>(legacyJson) ??
            throw new InvalidOperationException("Legacy tournament rules JSON must deserialize.");
        Require(legacy.Rule == GoRuleKind.Chinese, "Lower-case legacy rule name must remain compatible.");
        Require(legacy.MainTime == TimeSpan.FromSeconds(3662), "Legacy minute/second time must remain compatible.");

        const string currentJson =
            """
            {
              "DisplayName": "Current",
              "Rule": "Japanese",
              "TimeControl": {
                "Main": "999:59:59"
              },
              "MoveLimit": 9999
            }
            """;
        var current = JsonSerializer.Deserialize<TournamentRules>(currentJson) ??
            throw new InvalidOperationException("Current tournament rules JSON must deserialize.");
        Require((int)current.MainTime.TotalHours == 999, "Current main time must accept 999 hours.");
        Require(!string.IsNullOrWhiteSpace(current.Id), "A missing tournament rule ID must be generated.");

        var serialized = JsonSerializer.Serialize(current);
        Require(serialized.Contains("\"Rule\":\"Japanese\"", StringComparison.Ordinal), "Rule names must serialize in PascalCase.");
        Require(
            serialized.Contains("\"TimeControl\":{\"Main\":\"999:59:59\"}", StringComparison.Ordinal),
            "Main time must serialize as TimeControl.Main.");
        Require(!serialized.Contains("MainTimeMinutes", StringComparison.Ordinal), "Legacy minute fields must not be written.");
    }

    private static Game1 CreateGame()
    {
        var platform = new PortablePlatformServices();
        return new Game1(
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform);
    }

    private static void VerifyLocalMatchSgfFileName()
    {
        var fileName = LocalMatchSgfFileNameBuilder.Create(
            "black:engine",
            "white/player",
            new DateTime(2026, 8, 12, 9, 30, 45));
        Require(
            fileName == "kifuwarabe-go-black_engine-vs-white_player-20260812-093045.sgf",
            "LocalMatch SGF file names must use the two presented names and be safe file names.");
    }

    private static void VerifySgfCommentEditing()
    {
        var record = SgfGameRecordConverter.FromSgf("(;GM[1]SZ[9]C[root\r\ncomment];B[aa]C[move\rcomment])");
        Require(record.RootComment == "root\ncomment", "Root C[] must normalize CRLF to LF.");
        Require(record.Moves[0].Comment == "move\ncomment", "Move C[] must normalize CR to LF.");
        Require(record.TrySetComment(0, "edited\r\nroot"), "Root comment must be editable.");
        Require(record.TrySetComment(1, "edited ] \\ move"), "Move comment must be editable.");
        var roundTrip = SgfGameRecordConverter.FromSgf(SgfGameRecordConverter.ToSgf(record));
        Require(roundTrip.RootComment == "edited\nroot", "Root comment must survive SGF round-trip.");
        Require(roundTrip.Moves[0].Comment == "edited ] \\ move", "Escaped move comment must survive SGF round-trip.");
    }

    private static void VerifyInitialWindowLayout()
    {
        var preferred = new WindowClientSize(1920, 1080);
        var fitBelowTaskbar = preferred.ConstrainTo(new WindowClientSize(1904, 1120));
        Require(fitBelowTaskbar == new WindowClientSize(1904, 1080),
            "The initial window layout did not preserve available vertical margin while fitting the window frame horizontally.");

        var fitBesideTaskbar = preferred.ConstrainTo(new WindowClientSize(1840, 1040));
        Require(fitBesideTaskbar == new WindowClientSize(1840, 1040),
            "The initial window layout did not fit within a work area reduced by a vertical taskbar.");
    }

    private static void VerifyCgosStructuredObservation()
    {
        var observation = new CgosGameObservation();
        var setup = CgosNotificationJsonLines.Format(new CgosSetupNotification(
            "black", 81, 9, 6.5m, 600000, "White", "Black",
            [new CgosHistoricalMove("b", "A9", 590000)]));
        observation.ProcessLogLine(setup);
        Require(observation.IsStarted && observation.GameId == 81 && observation.MoveCount == 1,
            "CGOS structured setup must initialize the GUI observation and replay history.");

        // Once the structured stream is present, the matching legacy line is compatibility noise.
        observation.ProcessLogLine("2026-01-01 [black] > play w B9 580000");
        Require(observation.MoveCount == 1,
            "CGOS legacy protocol logs must be ignored after structured notifications begin.");
        var play = CgosNotificationJsonLines.Format(new CgosPlayNotification("black", "w", "B9", 580000));
        Require(observation.ProcessLogLine(play) && observation.MoveCount == 2,
            "CGOS structured play must update the GUI observation exactly once.");

        var liveView = CgosPlayRoomViewStateAdapter.Create(observation);
        var livePresentation = GoBoardPresenter.Create(
            liveView,
            GoBoardGeometry.Create(9, new GoBoardViewport(0, 0, 400, 400)));
        Require(liveView.Activity == GoPlayRoomActivity.Playing &&
                liveView.TimelineIndex == 2 &&
                livePresentation.Stones.Count == 2 &&
                livePresentation.LastMoveMarker?.Intersection == new GoPoint(1, 0),
            "The live CGOS observation must project through the Go Play Room view and presenter boundary.");

        observation.SeekReplay(1);
        var replayView = CgosPlayRoomViewStateAdapter.Create(observation);
        var replayPresentation = GoBoardPresenter.Create(
            replayView,
            GoBoardGeometry.Create(9, new GoBoardViewport(0, 0, 400, 400)));
        Require(replayView.Activity == GoPlayRoomActivity.Reviewing &&
                replayView.TimelineIndex == 1 &&
                replayPresentation.Stones.Count == 1 &&
                replayPresentation.LastMoveMarker?.Intersection == new GoPoint(0, 0) &&
                replayPresentation.KoMarker is null,
            "The replaying CGOS observation must project its displayed position without a live ko marker.");
        observation.ReturnToLive();

        observation.ProcessLogLine(CgosNotificationJsonLines.Format(new CgosGameOverNotification("black", "W+R")));
        Require(observation.IsFinished && observation.Result == "W+R",
            "CGOS structured gameover must finish the GUI observation.");
        var resultView = CgosPlayRoomViewStateAdapter.Create(observation);
        Require(resultView.Activity == GoPlayRoomActivity.GameOver && resultView.GameOverReason == "W+R",
            "The finished CGOS observation must project its result through the Go Play Room view boundary.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class TestClipboardService : IClipboardService
    {
        public string Text { get; set; } = "";

        public bool TrySetText(string text)
        {
            Text = text;
            return true;
        }

        public bool TryGetText(out string text)
        {
            text = Text;
            return true;
        }
    }

    private sealed class StubGtpCommandSession : IGtpCommandSession
    {
        private readonly Func<string, CancellationToken, Task<GtpCommandResult>> _handler;

        public StubGtpCommandSession(Func<string, CancellationToken, Task<GtpCommandResult>> handler)
        {
            _handler = handler;
        }

        public List<string> Commands { get; } = [];

        public Task<GtpCommandResult> SendAsync(string command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return _handler(command, cancellationToken);
        }
    }

    private sealed class StubInitialPositionExecutionHost : IInitialPositionExecutionHost
    {
        private readonly Func<string, CancellationToken, Task<GtpCommandResult>> _handler;

        public StubInitialPositionExecutionHost(Func<string, CancellationToken, Task<GtpCommandResult>> handler)
        {
            _handler = handler;
        }

        public List<string> Commands { get; } = [];

        public List<InitialPositionRecoveryMode> RecoveryModes { get; } = [];

        public List<StubDocumentLease> DocumentLeases { get; } = [];

        public Exception? RecoveryException { get; init; }

        public Task<GtpCommandResult> SendAsync(string command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return _handler(command, cancellationToken);
        }

        public Task RecoverAsync(
            InitialPositionRecoveryMode recoveryMode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RecoveryModes.Add(recoveryMode);
            return RecoveryException is null
                ? Task.CompletedTask
                : Task.FromException(RecoveryException);
        }

        public Task<IInitialPositionDocumentLease> MaterializeAsync(
            InitialPositionDocument document,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lease = new StubDocumentLease(@"C:\Engine Data\initial position.sgf");
            DocumentLeases.Add(lease);
            return Task.FromResult<IInitialPositionDocumentLease>(lease);
        }
    }

    private sealed class StubDocumentLease : IInitialPositionDocumentLease
    {
        public StubDocumentLease(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }

        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StubCompatibilityProfile : IGtpEngineCompatibilityProfile
    {
        public StubCompatibilityProfile(
            IReadOnlyList<IInitialPositionStrategy> strategies,
            InitialPositionRecoveryMode recoveryMode)
        {
            Strategies = strategies;
            RecoveryAfterAttempt = recoveryMode;
        }

        public string Id => "smoke-profile";

        public string DisplayName => "Smoke Profile";

        public GtpProfileEvidence Evidence => GtpProfileEvidence.ConservativeFallback;

        public IReadOnlyList<IInitialPositionStrategy> Strategies { get; }

        public InitialPositionRecoveryMode RecoveryAfterAttempt { get; }

        public GtpFilePathArgumentStyle LoadSgfPathStyle => GtpFilePathArgumentStyle.Auto;

        public int? LoadSgfMoveNumber => null;
    }
}
