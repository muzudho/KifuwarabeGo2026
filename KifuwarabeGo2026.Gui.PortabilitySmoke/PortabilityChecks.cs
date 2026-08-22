namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using KifuwarabeGo2026.Gui;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.GameOasis;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Gtp;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls;
using KifuwarabeGo2026.Gui.Sgf;
using KifuwarabeGo2026.GtpExtensions;
using KifuwarabeGo2026.GtpExtensions.Capabilities;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.GtpExtensions.Protocol;
using KifuwarabeGo2026.GtpExtensions.Sgf;
using KifuwarabeGo2026.GtpExtensions.Strategies;
using KifuwarabeGo2026.Match;
using KifuwarabeGo2026.Shared.Domain;
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
        var matchAssembly = typeof(MatchSession).Assembly;

        VerifyTargetFramework(coreAssembly);
        VerifyAssemblyReferences(coreAssembly);
        VerifyNoPlatformInvokes(coreAssembly);
        VerifyGtpExtensionsAssembly(gtpExtensionsAssembly);
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
        VerifyMatchAssembly(matchAssembly);
        VerifyGuiMatchIntegration();
        VerifyGtpMatchAdapter();
        VerifyPortableFallbacks();
        VerifyScoreAxisScaling();
        VerifyCommentNavigation();
        VerifyCatalogOrderEditor();
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
    }

    private static void VerifyGameOasisGuiComposition()
    {
        var composition = GameOasisGuiComposition.CreateAsync().AsTask().GetAwaiter().GetResult();
        Require(composition.Client.State.PlaySpaces.Count == 2,
            "The current GUI composition must discover normal Go and Ponnuki through Protocol G.");
        Require(composition.Client.State.PlaySpaces.All(entry =>
                entry.TypeId.Value is GameOasisOfficialNames.Go or GameOasisOfficialNames.Ponnuki),
            "The current GUI composition exposed an unexpected play-space.");
        Require(!composition.GetActiveBoard().IsSuccess,
            "The current GUI composition must not expose a board before a Protocol G session opens.");
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
            KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.GetTryAnotherButtonHit(new Point(1170, 455)) == GoStone.Black,
            "The black-engine fallback button hit area is incorrect.");
        Require(
            KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.GetContinueButtonHit(new Point(1500, 781)) == GoStone.White,
            "The white-engine continue button hit area is incorrect.");
        Require(
            KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.GetEngineCardHit(new Point(1200, 550)) == GoStone.White,
            "The white-engine card selection hit area is incorrect.");
        Require(
            KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.IsCancelButtonHit(new Point(1200, 940)) &&
            KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge.IsLogButtonHit(new Point(1550, 940)),
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
            KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine.GtpEngineRenderer.GetGtpEngineEditPanelInitialPositionProfileButtonHit(new Point(800, 680)) &&
            KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine.GtpEngineRenderer.GetGtpEngineEditPanelInitialPositionMethodButtonHit(new Point(1050, 680)),
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
            var catalog = GtpEngineCatalog.Load(listPath);
            catalog.Save([profile]);
            var restored = GtpEngineCatalog.Load(listPath).Profiles.Single();
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

    private static void VerifyGtpExtensionsAssembly(Assembly gtpExtensionsAssembly)
    {
        VerifyTargetFramework(gtpExtensionsAssembly, "GtpExtensions");
        VerifyNoPlatformInvokes(gtpExtensionsAssembly, "GtpExtensions");

        var references = gtpExtensionsAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Require(
            !references.Contains("KifuwarabeGo2026.Gui.Core"),
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
            global::KifuwarabeGo2026.GtpExtensions.Protocol.GtpCoordinate.FormatVertex(new GoPoint(8, 8), 9) == "J1",
            "GTP formatting must skip the I column.");
        Require(
            global::KifuwarabeGo2026.GtpExtensions.Protocol.GtpCoordinate.TryParseVertex("J1", 9, out var parsed) && parsed == new GoPoint(8, 8),
            "GTP parsing must reverse formatted coordinates.");
        Require(
            !global::KifuwarabeGo2026.GtpExtensions.Protocol.GtpCoordinate.TryParseVertex("I1", 9, out _),
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

    private static void VerifyMatchAssembly(Assembly matchAssembly)
    {
        VerifyTargetFramework(matchAssembly, "Match");
        VerifyNoPlatformInvokes(matchAssembly, "Match");

        var references = matchAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Require(
            references.Contains("KifuwarabeGo2026.Shared"),
            "Match must reference KifuwarabeGo2026.Shared.");
        Require(
            !references.Contains("KifuwarabeGo2026.Gui.Core"),
            "Match must not reference the GUI assembly.");
        Require(
            !references.Contains("MonoGame.Framework"),
            "Match must not reference MonoGame.");

        foreach (var forbiddenReference in ForbiddenAssemblyReferences)
        {
            Require(
                !references.Contains(forbiddenReference),
                $"Match directly references Windows-only assembly '{forbiddenReference}'.");
        }

        VerifyMatchStateTransitions();
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
