using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using MapManager_COFYYE.Variables;

namespace MapManager_COFYYE.Utils
{
    public static class RtvUtils
    {
        public static MapManager Instance => MapManager.Instance;

        public static void HandleRtvCommand(CCSPlayerController player)
        {
            if (!PlayerUtils.IsValidPlayer(player))
                return;

            // Check if RTV is enabled
            if (Instance?.Config?.RtvEnable != true)
            {
                player.PrintToChat(Instance?.Localizer.ForPlayer(player, "rtv.disabled") ?? "");
                return;
            }

            // Check if vote already happened
            if (
                GlobalVariables.VotedForCurrentMap == true
                || GlobalVariables.VotedForExtendMap == true
                || GlobalVariables.RtvTriggered == true
            )
            {
                player.PrintToChat(
                    Instance?.Localizer.ForPlayer(player, "rtv.vote.already.done") ?? ""
                );
                return;
            }

            // Check minimum players
            var validPlayers = Utilities
                .GetPlayers()
                .Where(p => PlayerUtils.IsValidPlayer(p))
                .ToList();
            int currentPlayerCount = validPlayers.Count;

            if (currentPlayerCount < (Instance?.Config?.RtvMinPlayers ?? 0))
            {
                var msgNotEnough =
                    Instance?.Localizer.ForPlayer(player, "rtv.not.enough.players") ?? "";
                msgNotEnough = msgNotEnough.Replace(
                    "{MIN_PLAYERS}",
                    Instance?.Config?.RtvMinPlayers.ToString() ?? "0"
                );
                player.PrintToChat(msgNotEnough);
                return;
            }

            // Check time after map start
            float timeSinceMapStart =
                (GlobalVariables.Timers.ElapsedMilliseconds / 1000.0f)
                - GlobalVariables.MapStartTime;
            float requiredTime = (Instance?.Config?.RtvTimeAfterMapStart ?? 5) * 60; // convert to seconds

            if (timeSinceMapStart < requiredTime)
            {
                int minutesLeft = (int)Math.Ceiling((requiredTime - timeSinceMapStart) / 60);
                var msgTooEarly = Instance?.Localizer.ForPlayer(player, "rtv.too.early") ?? "";
                msgTooEarly = msgTooEarly.Replace("{TIME_LEFT}", minutesLeft.ToString());
                player.PrintToChat(msgTooEarly);
                return;
            }

            string playerSteamId = player.SteamID.ToString();

            // Check if player already voted
            if (GlobalVariables.RtvVotes.Contains(playerSteamId))
            {
                player.PrintToChat(
                    Instance?.Localizer.ForPlayer(player, "rtv.already.voted") ?? ""
                );
                return;
            }

            // Add player vote
            GlobalVariables.RtvVotes.Add(playerSteamId);

            // Calculate percentage
            int votesNeeded = CalculateVotesNeeded(currentPlayerCount);
            int currentVotes = GlobalVariables.RtvVotes.Count;

            // Broadcast vote
            var message = Instance?.Localizer["rtv.player.voted"] ?? "";
            message = message
                .Replace("{PLAYER_NAME}", player.PlayerName)
                .Replace("{CURRENT_VOTES}", currentVotes.ToString())
                .Replace("{NEEDED_VOTES}", votesNeeded.ToString());
            Server.PrintToChatAll(message);

            // Check if threshold reached
            if (currentVotes >= votesNeeded)
            {
                TriggerRtvVote();
            }
        }

        public static void HandlePlayerDisconnect(string steamId)
        {
            if (GlobalVariables.RtvTriggered)
                return;

            // Remove player's vote
            if (GlobalVariables.RtvVotes.Remove(steamId))
            {
                // Recalculate percentage with new player count
                var validPlayers = Utilities
                    .GetPlayers()
                    .Where(p => PlayerUtils.IsValidPlayer(p))
                    .ToList();
                int currentPlayerCount = validPlayers.Count;

                if (currentPlayerCount < (Instance?.Config?.RtvMinPlayers ?? 0))
                {
                    // Not enough players anymore, clear votes
                    GlobalVariables.RtvVotes.Clear();
                    return;
                }

                int votesNeeded = CalculateVotesNeeded(currentPlayerCount);
                int currentVotes = GlobalVariables.RtvVotes.Count;

                // Check if threshold still reached after disconnect
                if (currentVotes >= votesNeeded && currentVotes > 0)
                {
                    TriggerRtvVote();
                }
            }
        }

        private static int CalculateVotesNeeded(int playerCount)
        {
            int percentage = Instance?.Config?.RtvMinimumVotesPercent ?? 60;
            return (int)Math.Ceiling((playerCount * percentage) / 100.0);
        }

        private static void TriggerRtvVote()
        {
            GlobalVariables.RtvTriggered = true;

            Server.PrintToChatAll(Instance?.Localizer["rtv.triggered"] ?? "RTV vote triggered!");

            // Check if vote should happen on freezetime or instantly
            if (Instance?.Config?.DependsOnTheRound == true)
            {
                // Vote will happen on next round freezetime
                // Check if we're close to the normal vote trigger
                var gameRules = ServerUtils.GetGameRules();
                var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
                var roundsLeft = maxRounds - gameRules.TotalRoundsPlayed;
                var voteTriggerRounds = Instance?.Config?.VoteTriggerTimeBeforeMapEnd ?? 3;

                if (roundsLeft <= voteTriggerRounds)
                {
                    // Too close to normal vote, let the normal vote happen
                    GlobalVariables.RtvTriggered = false;
                    GlobalVariables.RtvVotes.Clear();
                    Server.PrintToChatAll(Instance?.Localizer["rtv.cancelled.normal.vote"] ?? "");
                    return;
                }

                // Force vote on next round
                GlobalVariables.VotedForCurrentMap = false;
                GlobalVariables.VotedForExtendMap = false;
                MapUtils.CheckAndPickMapsForVoting();

                Server.PrintToChatAll(
                    Instance?.Localizer["rtv.vote.next.round"] ?? "Vote will start next round!"
                );
            }
            else
            {
                // Instant vote (timeleft mode)
                var timeLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>() ?? 0;
                var timeLeft = (timeLimit * 60) - GlobalVariables.CurrentTime;
                var voteTriggerTime = (Instance?.Config?.VoteTriggerTimeBeforeMapEnd ?? 3) * 60; // in seconds

                if (timeLeft <= voteTriggerTime)
                {
                    // Too close to normal vote, let the normal vote happen
                    GlobalVariables.RtvTriggered = false;
                    GlobalVariables.RtvVotes.Clear();
                    Server.PrintToChatAll(Instance?.Localizer["rtv.cancelled.normal.vote"] ?? "");
                    return;
                }

                // Start vote instantly
                MapUtils.CheckAndPickMapsForVoting();
                MapUtils.StartMapVoting();
            }
        }

        public static void ResetRtv()
        {
            GlobalVariables.RtvVotes.Clear();
            GlobalVariables.RtvTriggered = false;
            GlobalVariables.MapStartTime = GlobalVariables.Timers.ElapsedMilliseconds / 1000.0f;
        }
    }
}
