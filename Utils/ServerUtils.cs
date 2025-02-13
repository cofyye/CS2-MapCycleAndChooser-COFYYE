using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using MapCycleAndChooser_COFYYE.Variables;
using Microsoft.Extensions.Logging;

namespace MapCycleAndChooser_COFYYE.Utils
{
    public static class ServerUtils
    {
        public static readonly MapCycleAndChooser Instance = MapCycleAndChooser.Instance;

        public static void InitializeCvarsAndGameRules()
        {
            GlobalVariables.FreezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;

            if (Instance?.Config?.DependsOnTheRound == true)
            {
                var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>();

                if (maxRounds <= 4)
                {
                    Server.ExecuteCommand("mp_maxrounds 5");
                    Instance?.Logger.LogInformation("mp_maxrounds are set to a value less than 5. I set it to 5.");
                }

                Server.ExecuteCommand("mp_timelimit 0");
            }
            else
            {
                var timeLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>();

                if (timeLimit <= 4.0f)
                {
                    Server.ExecuteCommand("mp_timelimit 5");
                    Instance?.Logger.LogInformation("mp_timelimit are set to a value less than 5. I set it to 5.");
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
            var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            var gameRules = gameRulesEntities.First().GameRules;

            if (gameRules == null)
            {
                throw new ArgumentNullException(nameof(gameRules));
            }

            return gameRules;
        }
    }
}

