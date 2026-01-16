using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using MapManager_COFYYE.Classes;
using MapManager_COFYYE.Utils;
using MapManager_COFYYE.Variables;
using Microsoft.Extensions.Logging;

namespace MapManager_COFYYE;

public class MapManager : BasePlugin, IPluginConfig<Config.Config>
{
    public override string ModuleName => "MapManager";
    public override string ModuleVersion => "1.4";
    public override string ModuleAuthor => "cofyye";
    public override string ModuleDescription => "https://github.com/cofyye";

    public static MapManager Instance { get; set; } = new();
    public Config.Config Config { get; set; } = new();

    public void OnConfigParsed(Config.Config config)
    {
        Instance = this;

        Config = config ?? throw new ArgumentNullException(nameof(config));

        ServerUtils.CheckAndValidateConfig();

        GlobalVariables.Maps = Config?.Maps ?? [];
        GlobalVariables.CycleMaps =
            Config?.Maps?.Where(map => map.MapCycleEnabled == true).ToList() ?? [];

        ServerUtils.RunCvars();

        Logger.LogInformation(
            "Initialized {MapCount} cycle maps.",
            GlobalVariables.CycleMaps.Count
        );
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        AddCommand("css_nextmap", "Set a next map", OnSetNextMap);
        AddCommand("css_maps", "List of all maps", OnMapsList);

        RegisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        RegisterEventHandler<EventRoundStart>(RoundStartHandler);
        RegisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        RegisterEventHandler<EventWarmupEnd>(WarmupEndHandler);
        RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        if (Config?.VoteMapEnable == true)
        {
            GlobalVariables.FreezeTime =
                ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
            GlobalVariables.VotedForCurrentMap = false;
            RegisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        if (!GlobalVariables.Timers.IsRunning)
            GlobalVariables.Timers.Start();

        if (Config?.EnableCommandAdsInChat == true)
        {
            AddTimer(
                180.0f,
                () =>
                {
                    var players = Utilities
                        .GetPlayers()
                        .Where(p => PlayerUtils.IsValidPlayer(p))
                        .ToList();

                    foreach (var player in players)
                    {
                        switch (GlobalVariables.MessageIndex)
                        {
                            case 0:
                            {
                                player.PrintToChat(
                                    Localizer.ForPlayer(player, "nextmap.get.command.info")
                                );
                                break;
                            }
                            case 1:
                            {
                                player.PrintToChat(
                                    Localizer.ForPlayer(player, "currentmap.get.command.info")
                                );
                                break;
                            }
                            case 2:
                            {
                                player.PrintToChat(
                                    Localizer.ForPlayer(player, "lastmap.get.command.info")
                                );
                                break;
                            }
                            case 3:
                            {
                                player.PrintToChat(
                                    Localizer.ForPlayer(player, "timeleft.get.command.info")
                                );
                                break;
                            }
                            case 4:
                            {
                                if (Config?.RtvEnable == true)
                                {
                                    player.PrintToChat(Localizer.ForPlayer(player, "rtv.info"));
                                }
                                break;
                            }
                            default:
                            {
                                GlobalVariables.MessageIndex = 0;
                                break;
                            }
                        }
                    }

                    if (GlobalVariables.MessageIndex + 1 >= 4)
                    {
                        GlobalVariables.MessageIndex = 0;
                    }
                    else
                    {
                        GlobalVariables.MessageIndex += 1;
                    }
                },
                TimerFlags.REPEAT
            );
        }
    }

    public override void Unload(bool hotReload)
    {
        base.Load(hotReload);

        DeregisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        DeregisterEventHandler<EventRoundStart>(RoundStartHandler);
        DeregisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        DeregisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        DeregisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);
        DeregisterEventHandler<EventWarmupEnd>(WarmupEndHandler);

        if (Config?.VoteMapEnable == true)
        {
            DeregisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RemoveListener<Listeners.OnMapStart>(OnMapStart);
        RemoveListener<Listeners.OnMapEnd>(OnMapEnd);

        if (GlobalVariables.Timers.IsRunning)
            GlobalVariables.Timers.Stop();
    }

    public void OnSetNextMap(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtils.IsValidPlayer(caller))
            return;

        if (
            !AdminManager.PlayerHasPermissions(caller, "@css/changemap")
            || !AdminManager.PlayerHasPermissions(caller, "@css/root")
        )
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "command.no.perm"));
            return;
        }

        if (command.ArgString == "")
        {
            caller?.PrintToConsole(
                Localizer.ForPlayer(caller, "nextmap.set.command.expected.value")
            );
            return;
        }

        Map? map = GlobalVariables.CycleMaps.Find(m => m.MapValue == command.GetArg(1));

        if (map == null)
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "map.not.found"));
            return;
        }

        GlobalVariables.NextMap = map;

        Server.PrintToChatAll(
            Localizer
                .ForPlayer(caller, "nextmap.set.command.new.map")
                .Replace("{ADMIN_NAME}", caller?.PlayerName)
                .Replace("{MAP_NAME}", GlobalVariables.NextMap?.MapValue)
        );

        return;
    }

    public void OnMapsList(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtils.IsValidPlayer(caller))
            return;

        if (
            !AdminManager.PlayerHasPermissions(caller, "@css/changemap")
            || !AdminManager.PlayerHasPermissions(caller, "@css/root")
        )
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "command.no.perm"));
            return;
        }

        MenuUtils.OpenMapsMenu(caller!);
    }

    public HookResult PlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event == null)
            return HookResult.Continue;

        return HookResult.Continue;
    }

    public HookResult PlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event == null)
            return HookResult.Continue;

        var player = @event.Userid;
        if (PlayerUtils.IsValidPlayer(player))
        {
            RtvUtils.HandlePlayerDisconnect(player!.SteamID.ToString());
        }

        return HookResult.Continue;
    }

    public HookResult WarmupEndHandler(EventWarmupEnd @event, GameEventInfo info)
    {
        if (@event == null)
            return HookResult.Continue;

        ServerUtils.InitializeCvars();

        GlobalVariables.TimeLeftTimer ??= AddTimer(
            1.0f,
            () =>
            {
                var timeLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>();

                GlobalVariables.TimeLeft = (timeLimit ?? 5.0f) * 60; // in seconds
                GlobalVariables.CurrentTime += 1;
            },
            TimerFlags.REPEAT
        );

        if (Config?.DependsOnTheRound == false)
        {
            GlobalVariables.VotingTimer ??= AddTimer(
                3.0f,
                () =>
                {
                    if (GlobalVariables.IsVotingInProgress)
                        return;

                    MapUtils.CheckAndPickMapsForVoting();
                    MapUtils.CheckAndStartMapVoting();
                },
                TimerFlags.REPEAT
            );
        }

        return HookResult.Continue;
    }

    public HookResult CsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (@event == null)
            return HookResult.Continue;

        if (GlobalVariables.NextMap != null)
        {
            ServerUtils.ChangeMap(
                GlobalVariables.NextMap,
                (Config?.DelayToChangeMapInTheEnd ?? 10.0f) - 3.0f
            );
        }

        return HookResult.Continue;
    }

    public HookResult RoundStartHandler(EventRoundStart @event, GameEventInfo info)
    {
        ServerUtils.RunCvars();

        if (@event == null)
            return HookResult.Continue;

        if (Config?.DependsOnTheRound == true)
        {
            // Check if RTV was triggered - if so, start voting immediately
            if (GlobalVariables.RtvTriggered)
            {
                MapUtils.StartMapVoting();
                return HookResult.Continue;
            }

            // Only check for natural vote if RTV hasn't been triggered
            if (!GlobalVariables.RtvTriggered)
            {
                return MapUtils.CheckAndStartMapVoting();
            }
        }

        return HookResult.Continue;
    }

    public HookResult RoundEndHandler(EventRoundEnd @event, GameEventInfo info)
    {
        ServerUtils.RunCvars();

        if (@event == null)
            return HookResult.Continue;

        if (Config?.DependsOnTheRound == true && !GlobalVariables.RtvTriggered)
        {
            return MapUtils.CheckAndPickMapsForVoting();
        }

        return HookResult.Continue;
    }

    public HookResult PlayerChatHandler(EventPlayerChat @event, GameEventInfo info)
    {
        if (@event == null)
            return HookResult.Continue;

        if (Config?.CommandsNextMap?.Contains(@event.Text.Trim()) == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                if (Config?.EnableNextMapCommand != true)
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "nextmap.get.command.disabled"));
                }
                else
                {
                    player.PrintToChat(
                        Localizer
                            .ForPlayer(player, "nextmap.get.command")
                            .Replace("{MAP_NAME}", GlobalVariables.NextMap?.MapValue)
                    );
                }
            }
        }

        if (Config?.CommandsCurrentMap?.Contains(@event.Text.Trim()) == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                if (Config?.EnableCurrentMapCommand != true)
                {
                    player.PrintToChat(
                        Localizer.ForPlayer(player, "currentmap.get.command.disabled")
                    );
                }
                else
                {
                    player.PrintToChat(
                        Localizer
                            .ForPlayer(player, "currentmap.get.command")
                            .Replace("{MAP_NAME}", Server.MapName)
                    );
                }
            }
        }

        if (Config?.CommandsTimeLeft?.Contains(@event.Text.Trim()) == true)
        {
            var gameRules = ServerUtils.GetGameRules();

            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                if (Config?.EnableTimeLeftCommand != true)
                {
                    player.PrintToChat(
                        Localizer.ForPlayer(player, "timeleft.get.command.disabled")
                    );
                }
                else
                {
                    if (Config?.DependsOnTheRound == true)
                    {
                        var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
                        var roundLeft = maxRounds - gameRules.TotalRoundsPlayed;

                        if (roundLeft > 0)
                        {
                            player.PrintToChat(
                                Localizer
                                    .ForPlayer(player, "timeleft.get.command.round")
                                    .Replace("{TIME_LEFT}", roundLeft.ToString())
                            );
                        }
                        else
                        {
                            player.PrintToChat(
                                Localizer.ForPlayer(player, "timeleft.get.command.expired")
                            );
                        }
                    }
                    else
                    {
                        var timeLeft = GlobalVariables.TimeLeft - GlobalVariables.CurrentTime;
                        var minutes = Math.Ceiling(timeLeft / 60);

                        if (minutes > 0)
                        {
                            player.PrintToChat(
                                Localizer
                                    .ForPlayer(player, "timeleft.get.command.timeleft")
                                    .Replace("{TIME_LEFT}", minutes.ToString())
                            );
                        }
                        else
                        {
                            player.PrintToChat(
                                Localizer.ForPlayer(player, "timeleft.get.command.expired")
                            );
                        }
                    }
                }
            }
        }

        if (Config?.CommandsRtv?.Contains(@event.Text.Trim()) == true)
        {
            var player = (CCSPlayerController)
                Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));
            if (player != null)
            {
                RtvUtils.HandleRtvCommand(player);
            }
            else
            {
                Server.PrintToChatAll("Player not found.");
            }
        }

        if (Config?.CommandsLastMap?.Contains(@event.Text.Trim()) == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                if (Config?.EnableLastMapCommand != true)
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command.disabled"));
                }
                else
                {
                    if (string.IsNullOrEmpty(GlobalVariables.LastMap))
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command.null"));
                    }
                    else
                    {
                        player.PrintToChat(
                            Localizer
                                .ForPlayer(player, "lastmap.get.command")
                                .Replace("{MAP_NAME}", GlobalVariables.LastMap)
                        );
                    }
                }
            }
        }

        return HookResult.Continue;
    }

    public void OnMapStart(string mapName)
    {
        if (Config?.VoteMapEnable == true)
        {
            GlobalVariables.VotedForCurrentMap = false;
            GlobalVariables.VotedForExtendMap = false;
            if (!GlobalVariables.Timers.IsRunning)
                GlobalVariables.Timers.Start();
        }

        MapUtils.AutoSetNextMap();
        RtvUtils.ResetRtv();

        GlobalVariables.MapsPicked = false;

        if (Config?.DependsOnTheRound == true)
        {
            GlobalVariables.FreezeTime =
                ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
        }

        GlobalVariables.Votes.Clear();
        GlobalVariables.MapForVotes.Clear();
        GlobalVariables.CurrentTime = 0.0f;
        GlobalVariables.TimeLeftTimer?.Kill();
        GlobalVariables.TimeLeftTimer = null;
        GlobalVariables.VotingTimer?.Kill();
        GlobalVariables.VotingTimer = null;
    }

    public void OnMapEnd()
    {
        if (Config?.VoteMapEnable == true)
        {
            GlobalVariables.VotedForCurrentMap = false;
            GlobalVariables.VotedForExtendMap = false;
            if (!GlobalVariables.Timers.IsRunning)
                GlobalVariables.Timers.Start();
        }

        GlobalVariables.Votes.Clear();
        GlobalVariables.MapForVotes.Clear();
        GlobalVariables.MapsPicked = false;
        GlobalVariables.CurrentTime = 0.0f;
        GlobalVariables.NextMap = null;
        GlobalVariables.TimeLeftTimer?.Kill();
        GlobalVariables.TimeLeftTimer = null;
        GlobalVariables.VotingTimer?.Kill();
        GlobalVariables.VotingTimer = null;
    }
}
