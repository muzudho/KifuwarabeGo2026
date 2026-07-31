namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using KifuwarabeGo2026.Gui;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Gtp;
using KifuwarabeGo2026.GtpExtensions;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.GtpExtensions.Strategies;
using KifuwarabeGo2026.Match;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

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
        VerifyDefaultCgosConnection();
        VerifyTournamentRulesJsonCompatibility();
        VerifyComposition();
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
        Require(passSession.CurrentMode.Kind == GoAppModeKind.GameOver, "The local wrapper must enter its result screen after result waiting begins.");
        Require(passSession.Winner is null, "Match must not infer a winner from two passes on an empty board.");

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
        Require(editor.PagePairIndex == 1, "Order editor must open the page pair containing the selection.");

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
            platform);
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
}
