using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using MapCycleAndChooser_COFYYE.Utils;
using MapCycleAndChooser_COFYYE.Classes;
using CounterStrikeSharp.API.Modules.Memory;
using MapCycleAndChooser_COFYYE.Variables;
namespace MapCycleAndChooser_COFYYE;

public class MapCycleAndChooser : BasePlugin, IPluginConfig<Config.Config>
{
    public override string ModuleName => "Map Cycle and Chooser";
    public override string ModuleVersion => "1.1";
    public override string ModuleAuthor => "cofyye";
    public override string ModuleDescription => "https://github.com/cofyye";

    public static MapCycleAndChooser Instance { get; set; } = new();
    public Config.Config Config { get; set; } = new();

    public void OnConfigParsed(Config.Config config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        GlobalVariables.Maps = Config?.Maps ?? [];
        GlobalVariables.CycleMaps = Config?.Maps?.Where(map => map.MapCycleEnabled == true).ToList() ?? [];

        if (GlobalVariables.CycleMaps.Count > 0)
        {
            GlobalVariables.NextMap = GlobalVariables.CycleMaps[new Random().Next(GlobalVariables.CycleMaps.Count)];
        }
        else
        {
            GlobalVariables.NextMap = new Map(Server.MapName, Server.MapName, false, "", false, false, 0, 64);
        }

        if (Config?.DependsOnTheRound == null ||
            Config?.VoteMapDuration == null ||
            Config?.VoteMapEnable == null ||
            Config?.EnablePlayerVotingInChat == null ||
            Config?.VoteMapOnFreezeTime == null ||
            Config?.Sounds == null ||
            Config?.DisplayMapByValue == null ||
            Config?.EnablePlayerFreezeInMenu == null ||
            Config?.CommandsCSSMaps == null ||
            Config?.CommandsLastMap == null ||
            Config?.CommandsNextMap == null ||
            Config?.CommandsTimeLeft == null ||
            Config?.CommandsCurrentMap == null ||
            Config?.EnableNextMapCommand == null ||
            Config?.EnableLastMapCommand == null ||
            Config?.EnableCurrentMapCommand == null ||
            Config?.EnableTimeLeftCommand == null ||
            Config?.EnableIgnoreVote == null ||
            Config?.IgnoreVotePosition == null ||
            Config?.EnableExtendMap == null ||
            Config?.ExtendMapPosition == null ||
            Config?.ExtendMapTime == null ||
            Config?.DelayToChangeMapInTheEnd == null ||
            Config?.VoteTriggerTimeBeforeMapEnd == null ||
            Config?.Maps == null)
        {
            Logger.LogError("Config fields are null.");
            throw new ArgumentNullException(nameof(config));
        }

        Server.ExecuteCommand($"mp_match_restart_delay {Config?.DelayToChangeMapInTheEnd ?? 8}");
        Logger.LogInformation($"mp_match_restart_delay are set to {Config?.DelayToChangeMapInTheEnd ?? 8}.");

        Logger.LogInformation("Initialized {MapCount} cycle maps.", GlobalVariables.CycleMaps.Count);
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        base.OnAllPluginsLoaded(hotReload);

        AddTimer(3.0f, () =>
        {
            ServerUtils.InitializeCvars();
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        Instance = this;

        AddCommand("css_nextmap", "Set a next map", OnSetNextMap);
        AddCommand("css_maps", "List of all maps", OnMapsList);

        RegisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        RegisterEventHandler<EventRoundStart>(RoundStartHandler);
        RegisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        RegisterEventHandler<EventWarmupEnd>(WarmupEndHandler);
        RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        if(Config?.VoteMapEnable == true)
        {
            GlobalVariables.FreezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
            GlobalVariables.VotedForCurrentMap = false;
            RegisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnTick>(OnTick);

        if(!GlobalVariables.Timers.IsRunning) GlobalVariables.Timers.Start();

        if(Config?.EnableCommandAdsInChat == true)
        {
            AddTimer(300.0f, () =>
            {
                var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p)).ToList();

                foreach (var player in players)
                {
                    switch (GlobalVariables.MessageIndex)
                    {
                        case 0:
                            {
                                player.PrintToChat(Localizer.ForPlayer(player, "nextmap.get.command.info"));
                                break;
                            }
                        case 1:
                            {
                                player.PrintToChat(Localizer.ForPlayer(player, "currentmap.get.command.info"));
                                break;
                            }
                        case 2:
                            {
                                player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command.info"));
                                break;
                            }
                        case 3:
                            {
                                player.PrintToChat(Localizer.ForPlayer(player, "timeleft.get.command.info"));
                                break;
                            }
                        default:
                            {
                                GlobalVariables.MessageIndex = 0;
                                break;
                            }
                    }
                }

                if(GlobalVariables.MessageIndex + 1 >= 3)
                {
                    GlobalVariables.MessageIndex = 0;
                }
                else
                {
                    GlobalVariables.MessageIndex += 1;
                }
            }, TimerFlags.REPEAT);
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
        RemoveListener<Listeners.OnTick>(OnTick);

        if(GlobalVariables.Timers.IsRunning) GlobalVariables.Timers.Stop();
    }

    public void OnSetNextMap(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtils.IsValidPlayer(caller)) return;

        if (!AdminManager.PlayerHasPermissions(caller, "@css/changemap") || !AdminManager.PlayerHasPermissions(caller, "@css/root"))
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "command.no.perm"));
            return;
        }

        if (command.ArgString == "")
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "nextmap.set.command.expected.value"));
            return;
        }

        Map? map = GlobalVariables.CycleMaps.Find(m => m.MapValue == command.GetArg(1));

        if (map == null)
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "map.not.found"));
            return;
        }

        GlobalVariables.NextMap = map;

        Server.PrintToChatAll(Localizer.ForPlayer(caller, "nextmap.set.command.new.map").Replace("{ADMIN_NAME}", caller?.PlayerName).Replace("{MAP_NAME}", GlobalVariables.NextMap?.MapValue));
        
        return;
    }

    public void OnMapsList(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtils.IsValidPlayer(caller)) return;

        if (!AdminManager.PlayerHasPermissions(caller, "@css/changemap") || !AdminManager.PlayerHasPermissions(caller, "@css/root"))
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "command.no.perm"));
            return;
        }

        if (!MenuUtils.PlayersMenu.ContainsKey(caller?.SteamID.ToString() ?? "")) return;

        var playerSteamId = caller?.SteamID.ToString() ?? "";

        if(!string.IsNullOrEmpty(playerSteamId))
        {
            MenuUtils.PlayersMenu[playerSteamId].MenuOpened = true;
        }

        return;
    }

    public HookResult PlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var steamId = @event?.Userid?.SteamID.ToString();

        if (string.IsNullOrEmpty(steamId)) return HookResult.Continue;

        MenuUtils.PlayersMenu.Add(steamId, new(){
            CurrentIndex = 0,
            ButtonPressed = false,
            MenuOpened = false,
            Selected = false,
            Html = ""
        });

        return HookResult.Continue;
    }

    public HookResult PlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var steamId = @event?.Userid?.SteamID.ToString();

        if (string.IsNullOrEmpty(steamId)) return HookResult.Continue;

        MenuUtils.PlayersMenu.Remove(steamId);

        return HookResult.Continue;
    }

    public HookResult WarmupEndHandler(EventWarmupEnd @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        GlobalVariables.TimeLeftTimer ??= AddTimer(1.0f, () =>
            {
                var timeLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>();

                GlobalVariables.TimeLeft = (timeLimit ?? 5.0f) * 60; // in seconds
                GlobalVariables.CurrentTime += 1;
            }, TimerFlags.REPEAT);

        if (Config?.DependsOnTheRound == false)
        {
            GlobalVariables.VotingTimer ??= AddTimer(3.0f, () =>
                {
                    MapUtils.CheckAndPickMapsForVoting();
                    MapUtils.CheckAndStartMapVoting();
                }, TimerFlags.REPEAT);
        }

        return HookResult.Continue;
    }

    public HookResult CsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        AddTimer((Config?.DelayToChangeMapInTheEnd ?? 8.0f) - 2.0f, () =>
        {
            if (GlobalVariables.NextMap != null)
            {
                GlobalVariables.LastMap = Server.MapName;
                if (GlobalVariables.NextMap.MapIsWorkshop)
                {
                    if (string.IsNullOrEmpty(GlobalVariables.NextMap.MapWorkshopId))
                    {
                        Server.ExecuteCommand($"ds_workshop_changelevel {GlobalVariables.NextMap.MapValue}");
                    }
                    else
                    {
                        Server.ExecuteCommand($"host_workshop_map {GlobalVariables.NextMap.MapWorkshopId}");
                    }
                }
                else
                {
                    Server.ExecuteCommand($"changelevel {GlobalVariables.NextMap.MapValue}");
                }
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    public HookResult RoundStartHandler(EventRoundStart @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        if(Config?.DependsOnTheRound == true)
        {
            return MapUtils.CheckAndStartMapVoting();
        }

        return HookResult.Continue;
    }

    public HookResult RoundEndHandler(EventRoundEnd @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;
        

        if(Config?.DependsOnTheRound == true)
        {
            return MapUtils.CheckAndPickMapsForVoting();
        }

        return HookResult.Continue;
    }

    public HookResult PlayerChatHandler(EventPlayerChat @event, GameEventInfo info)
    {
        if(@event == null) return HookResult.Continue;

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
                    player.PrintToChat(Localizer.ForPlayer(player, "nextmap.get.command").Replace("{MAP_NAME}", GlobalVariables.NextMap?.MapValue));
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
                    player.PrintToChat(Localizer.ForPlayer(player, "currentmap.get.command.disabled"));
                }
                else
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "currentmap.get.command").Replace("{MAP_NAME}", Server.MapName));
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
                    player.PrintToChat(Localizer.ForPlayer(player, "timeleft.get.command.disabled"));
                }
                else
                {
                    if(Config?.DependsOnTheRound == true)
                    {
                        var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
                        var roundLeft = maxRounds - gameRules.TotalRoundsPlayed;

                        player.PrintToChat(Localizer.ForPlayer(player, "timeleft.get.command.round").Replace("{TIME_LEFT}", roundLeft.ToString()));
                    }
                    else
                    {
                        var timeLeft = GlobalVariables.TimeLeft - GlobalVariables.CurrentTime;
                        var minutes = Math.Ceiling(timeLeft / 60);
                        player.PrintToChat(Localizer.ForPlayer(player, "timeleft.get.command.timeleft").Replace("{TIME_LEFT}", minutes.ToString()));
                    }
                }
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
                    if(string.IsNullOrEmpty(GlobalVariables.LastMap))
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command.null"));
                    }
                    else
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command").Replace("{MAP_NAME}", GlobalVariables.LastMap));
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
            if (!GlobalVariables.Timers.IsRunning) GlobalVariables.Timers.Start();
        }

        if (GlobalVariables.CycleMaps.Count > 0)
        {
            GlobalVariables.NextMap = GlobalVariables.CycleMaps[new Random().Next(GlobalVariables.CycleMaps.Count)];
        }

        if(Config?.VoteMapOnFreezeTime == true)
        {
            GlobalVariables.FreezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
        }

        GlobalVariables.Votes.Clear();
        GlobalVariables.MapForVotes.Clear();
        MenuUtils.PlayersMenu.Clear();
        GlobalVariables.TimeLeft = 0.0f;
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
            if (!GlobalVariables.Timers.IsRunning) GlobalVariables.Timers.Start();
        }

        GlobalVariables.Votes.Clear();
        GlobalVariables.MapForVotes.Clear();
        MenuUtils.PlayersMenu.Clear();
        GlobalVariables.TimeLeft = 0.0f;
        GlobalVariables.CurrentTime = 0.0f;
        GlobalVariables.NextMap = null;
        GlobalVariables.TimeLeftTimer?.Kill();
        GlobalVariables.TimeLeftTimer = null;
        GlobalVariables.VotingTimer?.Kill();
        GlobalVariables.VotingTimer = null;
    }

    public void OnTick()
    {
        if (GlobalVariables.VoteStarted && Config?.VoteMapEnable == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

            foreach (var player in players)
            {
                if (!MenuUtils.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                MenuUtils.PlayersMenu[player.SteamID.ToString()].MenuOpened = true;

                if (Config?.EnablePlayerFreezeInMenu == true)
                {
                    if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                    {
                        player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
                        Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                    }
                }

                MenuUtils.CreateAndOpenHtmlVoteMenu(player);
            }
        }
        else if(!GlobalVariables.VoteStarted || Config?.VoteMapEnable == false)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

            foreach (var player in players)
            {
                if (!MenuUtils.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                if (MenuUtils.PlayersMenu[player.SteamID.ToString()].MenuOpened)
                {
                    if (Config?.EnablePlayerFreezeInMenu == true)
                    {
                        if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                        {
                            player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
                            Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                        }
                    }

                    MenuUtils.CreateAndOpenHtmlMapsMenu(player);
                }
                else
                {
                    if(Config?.EnablePlayerFreezeInMenu == true)
                    {
                        if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                        {
                            player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
                            Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                        }
                    }
                }
            }
        }
    }
}
