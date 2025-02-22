using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using MapCycleAndChooser_COFYYE.Classes;
using MapCycleAndChooser_COFYYE.Variables;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MapCycleAndChooser_COFYYE.Utils
{
    public static class MapUtils
    {
        public static MapCycleAndChooser Instance => MapCycleAndChooser.Instance;
        public static void PopulateMapsForVotes()
        {
            var random = new Random();

            int currentPlayers = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p)).Count();

            var eligibleMaps = GlobalVariables.CycleMaps
                .Where(map =>
                    map.MapValue != Server.MapName &&
                    map.MapCanVote &&
                    map.MapMinPlayers <= currentPlayers &&
                    map.MapMaxPlayers >= currentPlayers &&
                    CheckMapInCycleTime(map)
                )
                .ToList();

            if (eligibleMaps.Count == 0)
            {
                GlobalVariables.MapForVotes.Clear();
                return;
            }

            if (eligibleMaps.Count <= 5)
            {
                GlobalVariables.MapForVotes.AddRange(eligibleMaps);
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                if (eligibleMaps.Count == 0)
                {
                    break;
                }

                var randomIndex = random.Next(eligibleMaps.Count);
                var selectedMap = eligibleMaps[randomIndex];

                GlobalVariables.MapForVotes.Add(selectedMap);

                eligibleMaps.RemoveAt(randomIndex);
            }
        }

        public static (Map?, string) GetWinningMap()
        {
            if (GlobalVariables.Votes == null || GlobalVariables.Votes.Count == 0)
                return (null, "");

            var mapPercentages = CalculateMapsVotePercentages();

            if (mapPercentages == null || mapPercentages.Count == 0)
                return (null, "");

            double maxPercentage = mapPercentages.Values.Max();

            var topMaps = GlobalVariables.MapForVotes
                .Where(map =>
                {
                    var mapValuePercentageExists = mapPercentages.TryGetValue(map.MapValue, out var mapValuePercentage);
                    var mapDisplayPercentageExists = mapPercentages.TryGetValue(map.MapDisplay, out var mapDisplayPercentage);

                    return (mapValuePercentageExists && mapValuePercentage == maxPercentage) ||
                           (mapDisplayPercentageExists && mapDisplayPercentage == maxPercentage);
                })
                .ToList();

            if (GlobalVariables.Votes.ContainsKey("{menu.item.ignore.vote}"))
            {
                var ignoreVotePercentage = mapPercentages.GetValueOrDefault("{menu.item.ignore.vote}", 0);
                if (ignoreVotePercentage == maxPercentage)
                {
                    topMaps.Add(new Map("{menu.item.ignore.vote}", "Ignore Vote", false, "", true, true, 0, 64, "", ""));
                }
            }

            if (GlobalVariables.Votes.ContainsKey("{menu.item.extend.map}"))
            {
                var extendMapPercentage = mapPercentages.GetValueOrDefault("{menu.item.extend.map}", 0);
                if (extendMapPercentage == maxPercentage)
                {
                    topMaps.Add(new Map("{menu.item.extend.map}", "Extend Map", false, "", true, true, 0, 64, "", ""));
                }
            }

            var ignoreVoteOption = topMaps.FirstOrDefault(map => map.MapValue.Equals("{menu.item.ignore.vote}", StringComparison.OrdinalIgnoreCase));
            if (ignoreVoteOption != null)
            {
                if (GlobalVariables.MapForVotes.Count != 0)
                {
                    var random = new Random();
                    return (GlobalVariables.MapForVotes[random.Next(GlobalVariables.MapForVotes.Count)], "ignorevote");
                }

                return (null, "ignorevote");
            }

            var extendMapOption = topMaps.FirstOrDefault(map => map.MapValue.Equals("{menu.item.extend.map}", StringComparison.OrdinalIgnoreCase));
            if (extendMapOption != null)
            {
                return (null, "extendmap");
            }

            if (topMaps.Count > 1)
            {
                var random = new Random();
                return (topMaps[random.Next(topMaps.Count)], "");
            }

            return (topMaps.FirstOrDefault(), "");
        }

        public static void AutoSetNextMap()
        {
            if (GlobalVariables.CycleMaps.Count > 0)
            {
                if (Instance?.Config?.EnableRandomNextMap == true)
                {
                    GlobalVariables.NextMap = GlobalVariables.CycleMaps[new Random().Next(GlobalVariables.CycleMaps.Count)];
                }
                else
                {
                    if (GlobalVariables.NextMapIndex > GlobalVariables.CycleMaps.Count - 1)
                    {
                        GlobalVariables.NextMapIndex = 0;
                    }

                    GlobalVariables.NextMap = GlobalVariables.CycleMaps[GlobalVariables.NextMapIndex];

                    GlobalVariables.NextMapIndex += 1;
                }
            }
            else
            {
                GlobalVariables.NextMap = new Map(Server.MapName, Server.MapName, false, "", false, false, 0, 64, "", "");
            }
        }

        public static void AddPlayerToVotes(string mapValue, string playerSteamId)
        {
            if (!GlobalVariables.Votes.TryGetValue(mapValue, out List<string>? value))
            {
                value = ([]);
                GlobalVariables.Votes[mapValue] = value;
            }

            value.Add(playerSteamId);
        }

        public static Dictionary<string, int> CalculateMapsVotePercentages()
        {
            var percentages = new Dictionary<string, int>();

            int totalVotes = GlobalVariables.Votes.Values.SelectMany(voteList => voteList).Count();

            if (totalVotes == 0)
            {
                return percentages;
            }

            foreach (var vote in GlobalVariables.Votes)
            {
                string map = vote.Key;
                int votesForMap = vote.Value.Count;

                int percentage = (int)Math.Round((double)votesForMap / totalVotes * 100);
                percentages[map] = percentage;
            }

            return percentages;
        }

        public static HookResult CheckAndPickMapsForVoting()
        {
            float maxLimit;
            float timeLeft;
            int minValue;

            if (Instance?.Config?.DependsOnTheRound == true)
            {
                maxLimit = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
                minValue = Instance?.Config?.VoteTriggerTimeBeforeMapEnd ?? 3; // rounds
            }
            else
            {
                maxLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>() ?? 0.0f;
                minValue = (Instance?.Config?.VoteTriggerTimeBeforeMapEnd ?? 3) * 60; // from minutes to seconds
            }

            if (maxLimit > 0 && !GlobalVariables.VoteStarted && !GlobalVariables.VotedForCurrentMap && ServerUtils.GetGameRules()?.WarmupPeriod == false)
            {
                if (Instance?.Config?.DependsOnTheRound == true)
                {
                    timeLeft = maxLimit - ServerUtils.GetGameRules()?.TotalRoundsPlayed ?? 0;
                }
                else
                {
                    timeLeft = GlobalVariables.TimeLeft - GlobalVariables.CurrentTime;
                }

                if (timeLeft <= minValue)
                {
                    MapUtils.PopulateMapsForVotes();

                    if (GlobalVariables.MapForVotes.Count < 1)
                    {
                        GlobalVariables.VotedForCurrentMap = true;
                        Instance?.Logger.LogInformation("The list of voting maps is empty. I'm suspending the vote.");

                        return HookResult.Continue;
                    }

                    foreach (var map in GlobalVariables.MapForVotes)
                    {
                        if (!GlobalVariables.Votes.ContainsKey(map.MapValue))
                        {
                            GlobalVariables.Votes[map.MapValue] = [];
                        }
                    }

                    if (Instance?.Config?.DependsOnTheRound == true && Instance?.Config?.VoteMapOnFreezeTime == true)
                    {
                        Server.ExecuteCommand($"mp_freezetime {(Instance?.Config?.VoteMapDuration ?? GlobalVariables.FreezeTime) + 2}");
                    }
                }
                else
                {
                    if(Instance?.Config?.DependsOnTheRound == true)
                    {
                        Server.ExecuteCommand($"mp_freezetime {GlobalVariables.FreezeTime}");
                    }
                }
            }
            else
            {
                if (Instance?.Config?.DependsOnTheRound == true)
                {
                    Server.ExecuteCommand($"mp_freezetime {GlobalVariables.FreezeTime}");
                }
            }

            return HookResult.Continue;
        }

        public static HookResult CheckAndStartMapVoting()
        {
            float maxLimit;
            float timeLeft;
            int minValue;

            if (Instance?.Config?.DependsOnTheRound == true)
            {
                maxLimit = (float)(ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0);
                minValue = Instance?.Config?.VoteTriggerTimeBeforeMapEnd ?? 3; // rounds
            }
            else
            {
                maxLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>() ?? 0.0f;
                minValue = (Instance?.Config?.VoteTriggerTimeBeforeMapEnd ?? 3) * 60; // from minutes to seconds
            }

            if (maxLimit > 0 && !GlobalVariables.VoteStarted && !GlobalVariables.VotedForCurrentMap && ServerUtils.GetGameRules()?.WarmupPeriod == false)
            {
                if(Instance?.Config?.DependsOnTheRound == true)
                {
                    timeLeft = maxLimit - ServerUtils.GetGameRules()?.TotalRoundsPlayed ?? 0;
                }
                else
                {
                    timeLeft = GlobalVariables.TimeLeft - GlobalVariables.CurrentTime;
                }

                if (timeLeft <= minValue)
                {
                    if (GlobalVariables.MapForVotes.Count < 1)
                    {
                        Instance?.Logger.LogInformation("The list of voting maps is empty. I'm suspending the vote.");

                        return HookResult.Continue;
                    }

                    GlobalVariables.VoteStarted = true;
                    GlobalVariables.IsVotingInProgress = true;

                    var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

                    string? soundToPlay = "";
                    if (Instance?.Config?.Sounds.Count > 0)
                    {
                        soundToPlay = Instance?.Config.Sounds[new Random().Next(Instance?.Config?.Sounds.Count ?? 1)];
                    }

                    foreach (var player in players)
                    {
                        player.PrintToChat(Instance?.Localizer.ForPlayer(player, "vote.started") ?? "");

                        if (!string.IsNullOrEmpty(soundToPlay))
                        {
                            player.ExecuteClientCommand($"play {soundToPlay}");
                        }

                        //if(Instance?.Config.EnableScreenMenu == true)
                        //{
                        //    MenuUtils.CreateAndOpenScreenVoteMenu(player);
                        //}
                    }

                    float duration = (float)(Instance?.Config?.VoteMapDuration ?? 15);

                    Instance?.AddTimer(duration, () => {
                        var (winningMap, type) = MapUtils.GetWinningMap();

                        if (winningMap != null)
                        {
                            GlobalVariables.NextMap = winningMap;
                        }
                        else if (winningMap == null && type == "extendmap")
                        {
                            if(Instance?.Config?.DependsOnTheRound == true)
                            {
                                Server.ExecuteCommand($"mp_maxrounds {(int)timeLeft + Instance?.Config?.ExtendMapTime ?? 5}");
                            }
                            else
                            {
                                Server.ExecuteCommand($"mp_timelimit {Math.Ceiling((float)timeLeft / 60) + Instance?.Config?.ExtendMapTime ?? 5}");
                            }
                            GlobalVariables.VotedForExtendMap = true;
                            GlobalVariables.VotedForCurrentMap = false;
                        }
                        else if (winningMap == null && type == "ignorevote")
                        {
                            GlobalVariables.NextMap = GlobalVariables.CycleMaps.FirstOrDefault();
                        }
                        else
                        {
                            Instance?.Logger.LogInformation("Winning map is null.");
                        }

                        GlobalVariables.Votes.Clear();
                        GlobalVariables.MapForVotes.Clear();

                        var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

                        foreach (var player in players)
                        {
                            if (type == "extendmap")
                            {
                                player.PrintToChat(Instance?.Localizer.ForPlayer(player, "vote.finished.extend.map.round").Replace("{EXTENDED_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "");
                            }
                            else
                            {
                                player.PrintToChat(Instance?.Localizer.ForPlayer(player, "vote.finished").Replace("{MAP_NAME}", GlobalVariables.NextMap?.MapValue) ?? "");
                            }

                            if (!MenuUtils.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                            MenuUtils.PlayersMenu[player.SteamID.ToString()].MenuOpened = false;
                            MenuUtils.PlayersMenu[player.SteamID.ToString()].Selected = false;
                            MenuUtils.PlayersMenu[player.SteamID.ToString()].Html = "";

                            if (Instance?.Config?.EnablePlayerFreezeInMenu == true)
                            {
                                if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                                {
                                    player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
                                    Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                                }
                            }
                        }

                        GlobalVariables.VoteStarted = false;

                        Instance?.AddTimer(1.0f, () => GlobalVariables.IsVotingInProgress = false);

                        if(type != "extendmap")
                        {
                            GlobalVariables.VotedForCurrentMap = true;
                        }

                        //if (Instance?.Config?.EnableScreenMenu == true)
                        //{
                        //    MenuUtils.CloseScreenMenu();
                        //}
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }

            return HookResult.Continue;
        }

        public static bool CheckMapInCycleTime(Map map)
        {
            var start = map.CycleStartTime;
            var end = map.CycleEndTime;

            // if both are empty, lets ignore the setting
            if (string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end))
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            {
                Instance?.Logger.LogInformation("Either 'cycle_start_time' or 'cycle_end_time' is empty. map: {MapName}", map.MapValue);
                return false;
            }

            var now = DateTime.Now;
            if (!DateTime.TryParseExact(start, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
            {
                Instance?.Logger.LogInformation("The 'cycle_start_time' string is not valid. Format should be 'HH:mm'.");
                Instance?.Logger.LogInformation("Invalid 'cycle_start_time' config string. map: {MapName}", map.MapValue);
                return false;
            }
            if (!DateTime.TryParseExact(end, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
            {
                Instance?.Logger.LogInformation("The 'cycle_end_time' string is not valid. Format should be 'HH:mm'.");
                Instance?.Logger.LogInformation("Invalid 'cycle_end_time' config string. map: {MapName}", map.MapValue);
                return false;
            }

            var startTime = now.Date.Add(parsedStart.TimeOfDay);
            var endTime = now.Date.Add(parsedEnd.TimeOfDay);

            // if 'start' - 'end' into the next day
            // example: 23:00 - 05:00
            if (startTime >= endTime)
            {
                endTime.AddDays(1);
            }

            return now >= startTime && now <= endTime;
        }
    }
}