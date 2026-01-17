using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using MapManager_COFYYE.Classes;
using MapManager_COFYYE.Variables;
using Microsoft.Extensions.Logging;

namespace MapManager_COFYYE.Utils
{
    public static class ServerUtils
    {
        public static MapManager Instance => MapManager.Instance;

        public static void ChangeMap(Map map, float delay = 0.0f)
        {
            if (map == null)
                return;

            GlobalVariables.LastMap = Server.MapName;

            if (delay > 0)
            {
                Instance?.AddTimer(
                    delay,
                    () => ExecuteMapChange(map),
                    TimerFlags.STOP_ON_MAPCHANGE
                );
            }
            else
            {
                ExecuteMapChange(map);
            }
        }

        private static void ExecuteMapChange(Map map)
        {
            if (map.MapIsWorkshop)
            {
                if (string.IsNullOrEmpty(map.MapWorkshopId))
                {
                    Server.ExecuteCommand($"ds_workshop_changelevel {map.MapValue}");
                }
                else
                {
                    Server.ExecuteCommand($"host_workshop_map {map.MapWorkshopId}");
                }
            }
            else
            {
                Server.ExecuteCommand($"changelevel {map.MapValue}");
            }
        }

        public static void RunCvars()
        {
            Server.ExecuteCommand(
                $"mp_match_restart_delay {Instance?.Config?.DelayToChangeMapInTheEnd ?? 10}"
            );
            Instance?.Logger.LogInformation(
                "mp_match_restart_delay are set to {RestartDelay}.",
                Instance?.Config?.DelayToChangeMapInTheEnd ?? 10
            );

            Server.ExecuteCommand("mp_match_can_clinch 0");
            Instance?.Logger.LogInformation("mp_match_can_clinch are set to 0.");

            Server.ExecuteCommand("mp_endmatch_votenextmap 0");
            Instance?.Logger.LogInformation("mp_endmatch_votenextmap are set to 0.");

            Server.ExecuteCommand("mp_endmatch_votenextleveltime 0");
            Instance?.Logger.LogInformation("mp_endmatch_votenextleveltime are set to 0.");

            Server.ExecuteCommand("mp_halftime 0");
            Instance?.Logger.LogInformation("mp_halftime are set to 0.");
        }

        public static void InitializeCvars()
        {
            GlobalVariables.FreezeTime =
                ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;

            if (Instance?.Config?.DependsOnTheRound == true)
            {
                var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>();

                if (maxRounds <= 4)
                {
                    Server.ExecuteCommand("mp_maxrounds 5");
                    Instance?.Logger.LogInformation(
                        "mp_maxrounds are set to a value less than 5. I set it to 5."
                    );
                }

                Server.ExecuteCommand("mp_timelimit 0");
            }
            else
            {
                var timeLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>();

                if (timeLimit <= 4.0f)
                {
                    Server.ExecuteCommand("mp_timelimit 5");
                    Instance?.Logger.LogInformation(
                        "mp_timelimit are set to a value less than 5. I set it to 5."
                    );
                    GlobalVariables.TimeLeft = 5 * 60; // in seconds
                }
                else
                {
                    GlobalVariables.TimeLeft = (timeLimit ?? 5.0f) * 60; // in seconds
                }

                Server.ExecuteCommand("mp_maxrounds 0");
            }
        }

        public static CCSGameRules GetGameRules()
        {
            var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>(
                "cs_gamerules"
            );
            var gameRules = gameRulesEntities.First().GameRules;

            if (gameRules == null)
            {
                throw new ArgumentNullException(nameof(gameRules));
            }

            return gameRules;
        }

        public static void CheckAndValidateConfig()
        {
            if (Instance?.Config == null)
            {
                Instance?.Logger.LogError("Config fields are null.");
                throw new ArgumentNullException(nameof(Instance.Config));
            }

            // VoteMapDuration
            if (Instance?.Config?.VoteMapDuration < 0 || Instance?.Config?.VoteMapDuration > 45)
            {
                Instance?.Logger.LogError(
                    "vote_map_duration has bad value. Value must be between 0 and 45"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // IgnoreVotePosition
            if (
                Instance?.Config?.IgnoreVotePosition != "top"
                && Instance?.Config?.IgnoreVotePosition != "bottom"
            )
            {
                Instance?.Logger.LogError(
                    "ignore_vote_position has bad value. Value must be top or bottom"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // ExtendMapTime
            if (Instance?.Config?.ExtendMapTime < 0)
            {
                Instance?.Logger.LogError(
                    "extend_map_time has bad value. Value must be greater than 0"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // ExtendMapPosition
            if (
                Instance?.Config?.ExtendMapPosition != "top"
                && Instance?.Config?.ExtendMapPosition != "bottom"
            )
            {
                Instance?.Logger.LogError(
                    "extend_map_position has bad value. Value must be top or bottom"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // DelayToChangeMapInTheEnd
            if (Instance?.Config?.DelayToChangeMapInTheEnd < 4)
            {
                Instance?.Logger.LogError(
                    "delay_to_change_map_in_the_end has bad value. Value must be greater than 4"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // VoteTriggerTimeBeforeMapEnd
            if (Instance?.Config?.VoteTriggerTimeBeforeMapEnd < 2)
            {
                Instance?.Logger.LogError(
                    "vote_trigger_time_before_map_end has bad value. Value must be greater than 2"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // RtvMinPlayers
            if (Instance?.Config?.RtvMinPlayers < 0 || Instance?.Config?.RtvMinPlayers > 64)
            {
                Instance?.Logger.LogError(
                    "rtv_min_players has bad value. Value must be between 0 and 64"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // RtvTimeAfterMapStart
            if (
                Instance?.Config?.RtvTimeAfterMapStart < 0
                || Instance?.Config?.RtvTimeAfterMapStart > 300
            )
            {
                Instance?.Logger.LogError(
                    "rtv_time_after_map_start has bad value. Value must be between 0 and 300"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }

            // RtvMinimumVotesPercent
            if (
                Instance?.Config?.RtvMinimumVotesPercent < 0
                || Instance?.Config?.RtvMinimumVotesPercent > 100
            )
            {
                Instance?.Logger.LogError(
                    "rtv_minimum_votes_percent has bad value. Value must be between 0 and 100"
                );
                throw new ArgumentException(nameof(Instance.Config));
            }
        }
    }
}
