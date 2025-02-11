using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using System.Diagnostics;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using MapCycleAndChooser_COFYYE.Utils;
using MapCycleAndChooser_COFYYE.Classes;
using CounterStrikeSharp.API.Modules.Memory;
namespace MapCycleAndChooser_COFYYE;

public class MapCycleAndChooser : BasePlugin, IPluginConfig<Config.Config>
{
    public override string ModuleName => "Map Cycle and Chooser";
    public override string ModuleVersion => "1.1";
    public override string ModuleAuthor => "cofyye";
    public override string ModuleDescription => "https://github.com/cofyye";

    public static MapCycleAndChooser Instance { get; set; } = new();
    public Config.Config Config { get; set; } = new();

    private static List<Map> _cycleMaps = [];
    private static List<Map> _maps = [];
    private static readonly List<Map> _mapForVotes = [];
    private static bool _voteStarted = false;
    private static bool _votedForCurrentMap = false;
    private static bool _votedForExtendMap = false;
    private static Map? _nextmap = null;
    public static string _lastmap = "";
    private static float _timeleft = 0; // in seconds
    private static int _messageIndex = 0;
    private static Dictionary<string, List<string>> _votes = [];
    private static int _freezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
    public new static readonly Stopwatch Timers = new();

    public void OnConfigParsed(Config.Config config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        _maps = Config?.Maps ?? [];
        _cycleMaps = Config?.Maps?.Where(map => map.MapCycleEnabled == true).ToList() ?? [];

        if (_cycleMaps.Count > 0)
        {
            _nextmap = _cycleMaps[new Random().Next(_cycleMaps.Count)];
        }
        else
        {
            _nextmap = new Map(Server.MapName, Server.MapName, false, "", false, false, 0, 64);
        }

        if (Config?.DependsOnTheRound == null ||
            Config?.VoteMapDuration == null ||
            Config?.VoteMapEnable == null ||
            Config?.EnablePlayerVotingInChat == null ||
            Config?.VoteMapOnFreezeTime == null ||
            Config?.VoteMapOnNextRound == null ||
            Config?.Sounds == null ||
            Config?.DisplayMapByValue == null ||
            Config?.EnablePlayerFreezeInMenu == null ||
            Config?.CommandsCSSMaps == null ||
            Config?.CommandsLastMap == null ||
            Config?.CommandsNextMap == null ||
            Config?.CommandsReVote == null ||
            Config?.CommandsCurrentMap == null ||
            Config?.EnableNextMapCommand == null ||
            Config?.EnableLastMapCommand == null ||
            Config?.EnableCurrentMapCommand == null ||
            Config?.EnableDontVote == null ||
            Config?.DontVotePosition == null ||
            Config?.EnableExtendMap == null ||
            Config?.ExtendMapPosition == null ||
            Config?.ExtendMapTime == null ||
            Config?.Maps == null)
        {
            Logger.LogError("Config fields are null.");
            throw new ArgumentNullException(nameof(config));
        }

        Server.ExecuteCommand("mp_match_restart_delay 8");
        Logger.LogInformation("mp_match_restart_delay are set to 8.");

        Logger.LogInformation("Initialized {MapCount} cycle maps.", _cycleMaps.Count);
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        base.OnAllPluginsLoaded(hotReload);

        AddTimer(3.0f, () =>
        {
            if (Config?.DependsOnTheRound == true)
            {
                var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>();

                if (maxRounds <= 4)
                {
                    Server.ExecuteCommand("mp_maxrounds 5");
                    Logger.LogInformation("mp_maxrounds are set to a value less than 5. I set it to 5.");
                }

                Server.ExecuteCommand("mp_timelimit 0");
            }
            else
            {
                var timeLimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>();

                if (timeLimit <= 4.0f)
                {
                    Server.ExecuteCommand("mp_timelimit 5");
                    Logger.LogInformation("mp_timelimit are set to a value less than 5. I set it to 5.");
                    _timeleft = 5 * 60; // in seconds
                }
                else
                {
                    _timeleft = (timeLimit ?? 5.0f) * 60; // in seconds
                }

                Server.ExecuteCommand("mp_maxrounds 0");
            }
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
        RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        if(Config?.VoteMapEnable == true)
        {
            _freezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
            _votedForCurrentMap = false;
            RegisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnTick>(OnTick);

        if(!Timers.IsRunning) Timers.Start();

        if(Config?.EnableCommandAdsInChat == true)
        {
            AddTimer(300.0f, () =>
            {
                var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).ToList();

                foreach (var player in players)
                {
                    switch (_messageIndex)
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
                        default:
                            {
                                _messageIndex = 0;
                                break;
                            }
                    }
                }

                if(_messageIndex + 1 >= 3)
                {
                    _messageIndex = 0;
                }
                else
                {
                    _messageIndex += 1;
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

        if (Config?.VoteMapEnable == true)
        {
            DeregisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RemoveListener<Listeners.OnMapStart>(OnMapStart);
        RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
        RemoveListener<Listeners.OnTick>(OnTick);

        if(Timers.IsRunning) Timers.Stop();
    }

    public void OnSetNextMap(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtil.IsValidPlayer(caller)) return;

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

        Map? map = _cycleMaps.Find(m => m.MapValue == command.GetArg(1));

        if (map == null)
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "map.not.found"));
            return;
        }

        _nextmap = map;

        Server.PrintToChatAll(Localizer.ForPlayer(caller, "nextmap.set.command.new.map").Replace("{ADMIN_NAME}", caller?.PlayerName).Replace("{MAP_NAME}", _nextmap?.MapValue));
        
        return;
    }

    public void OnMapsList(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtil.IsValidPlayer(caller)) return;

        if (!AdminManager.PlayerHasPermissions(caller, "@css/changemap") || !AdminManager.PlayerHasPermissions(caller, "@css/root"))
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "command.no.perm"));
            return;
        }

        if (!MenuUtil.PlayersMenu.ContainsKey(caller?.SteamID.ToString() ?? "")) return;

        var playerSteamId = caller?.SteamID.ToString() ?? "";

        if(!string.IsNullOrEmpty(playerSteamId))
        {
            MenuUtil.PlayersMenu[playerSteamId].MenuOpened = true;
        }

        return;
    }

    public HookResult PlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var steamId = @event?.Userid?.SteamID.ToString();

        if (string.IsNullOrEmpty(steamId)) return HookResult.Continue;

        MenuUtil.PlayersMenu.Add(steamId, new(){
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

        MenuUtil.PlayersMenu.Remove(steamId);

        return HookResult.Continue;
    }

    public HookResult CsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        AddTimer(5.0f, () =>
        {
            if (_nextmap != null)
            {
                _lastmap = Server.MapName;
                if (_nextmap.MapIsWorkshop)
                {
                    if (string.IsNullOrEmpty(_nextmap.MapWorkshopId))
                    {
                        Server.ExecuteCommand($"ds_workshop_changelevel {_nextmap.MapValue}");
                    }
                    else
                    {
                        Server.ExecuteCommand($"host_workshop_map {_nextmap.MapWorkshopId}");
                    }
                }
                else
                {
                    Server.ExecuteCommand($"changelevel {_nextmap.MapValue}");
                }
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    public HookResult RoundStartHandler(EventRoundStart @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if(gameRules == null)
        {
            Logger.LogError("Game rules not found.");
            return HookResult.Continue;
        }

        if(Config?.DependsOnTheRound == true)
        {
            var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;

            if (maxRounds > 0 && !_voteStarted && !_votedForCurrentMap && !gameRules.WarmupPeriod)
            {
                if (_mapForVotes.Count < 1)
                {
                    Logger.LogInformation("The list of voting maps is empty. I'm suspending the vote.");

                    return HookResult.Continue;
                }

                var roundLeft = maxRounds - gameRules.TotalRoundsPlayed;

                if (roundLeft <= 3)
                {
                    var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                    string soundToPlay = "";
                    if (Config?.Sounds.Count > 0) {
                        soundToPlay = Config.Sounds[new Random().Next(Config?.Sounds.Count ?? 1)];
                    }

                    foreach (var player in players)
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "vote.started"));
                        if (!string.IsNullOrEmpty(soundToPlay))
                        {
                            player.ExecuteClientCommand($"play {soundToPlay}");
                        }
                    }

                    _voteStarted = true;

                    float duration = (float)(Config?.VoteMapDuration ?? 15);

                    AddTimer(duration, () => {
                        _voteStarted = false;
                        _votedForCurrentMap = true;

                        var (winningMap, type) = MapUtil.GetWinningMap(_mapForVotes, _votes);

                        if (winningMap != null)
                        {
                            _nextmap = winningMap;
                        }
                        else if (winningMap == null && type == "extendmap")
                        {
                            Server.PrintToChatAll("Extend map rounds");
                        }
                        else if (winningMap == null && type == "dontvote")
                        {
                            _nextmap = _cycleMaps.FirstOrDefault();
                        }
                        else
                        {
                            Logger.LogWarning("Winning map is null.");
                        }

                        _votes = [];

                        var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                        foreach (var player in players)
                        {
                            player.PrintToChat(Localizer.ForPlayer(player, "vote.finished").Replace("{MAP_NAME}", _nextmap?.MapValue));

                            if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                            MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened = false;
                            MenuUtil.PlayersMenu[player.SteamID.ToString()].Selected = false;
                            MenuUtil.PlayersMenu[player.SteamID.ToString()].Html = "";

                            if (Config?.EnablePlayerFreezeInMenu == true)
                            {
                                if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                                {
                                    player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
                                    Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                                }
                            }
                        }
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        } 
        else
        {
            var timelimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>() ?? 0.0f;

            if (timelimit > 0 && !_voteStarted && !_votedForCurrentMap && !gameRules.WarmupPeriod)
            {
                if (_mapForVotes.Count < 1)
                {
                    Logger.LogInformation("The list of voting maps is empty. I'm suspending the vote.");

                    return HookResult.Continue;
                }

                var timeLeft = _timeleft - Server.CurrentTime;

                if (timeLeft <= 180)
                {
                    var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                    string soundToPlay = "";
                    if (Config?.Sounds.Count > 0)
                    {
                        soundToPlay = Config.Sounds[new Random().Next(Config?.Sounds.Count ?? 1)];
                    }

                    foreach (var player in players)
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "vote.started"));
                        if (!string.IsNullOrEmpty(soundToPlay))
                        {
                            player.ExecuteClientCommand($"play {soundToPlay}");
                        }
                    }

                    _voteStarted = true;

                    float duration = (float)(Config?.VoteMapDuration ?? 15);

                    AddTimer(duration, () => {
                        _voteStarted = false;
                        _votedForCurrentMap = true;

                        var (winningMap, type) = MapUtil.GetWinningMap(_mapForVotes, _votes);

                        if (winningMap != null)
                        {
                            _nextmap = winningMap;
                        }
                        else if (winningMap == null && type == "extendmap")
                        {
                            Server.PrintToChatAll("Extend map minutes");
                        }
                        else if (winningMap == null && type == "dontvote")
                        {
                            _nextmap = _cycleMaps.FirstOrDefault();
                        }
                        else
                        {
                            Logger.LogWarning("Winning map is null.");
                        }

                        _votes = [];

                        var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                        foreach (var player in players)
                        {
                            player.PrintToChat(Localizer.ForPlayer(player, "vote.finished").Replace("{MAP_NAME}", _nextmap?.MapValue));

                            if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                            MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened = false;
                            MenuUtil.PlayersMenu[player.SteamID.ToString()].Selected = false;
                            MenuUtil.PlayersMenu[player.SteamID.ToString()].Html = "";

                            if (Config?.EnablePlayerFreezeInMenu == true)
                            {
                                if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                                {
                                    player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
                                    Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
                                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                                }
                            }
                        }
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }

        return HookResult.Continue;
    }

    public HookResult RoundEndHandler(EventRoundEnd @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if (gameRules == null)
        {
            Logger.LogError("Game rules not found.");
            return HookResult.Continue;
        }

        if (Config?.DependsOnTheRound == true)
        {
            var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;

            if (maxRounds > 0 && !_voteStarted && !_votedForCurrentMap && !gameRules.WarmupPeriod)
            {
                var roundLeft = maxRounds - gameRules.TotalRoundsPlayed;

                if (roundLeft <= 3)
                {
                    MapUtil.PopulateMapsForVotes(Server.MapName, _cycleMaps, _mapForVotes);

                    if (_mapForVotes.Count < 1)
                    {
                        _votedForCurrentMap = true;
                        Logger.LogInformation("The list of voting maps is empty. I'm suspending the vote.");

                        return HookResult.Continue;
                    }

                    foreach (var map in _mapForVotes)
                    {
                        if (!_votes.ContainsKey(map.MapValue))
                        {
                            _votes[map.MapValue] = [];
                        }
                    }

                    if(Config?.VoteMapOnFreezeTime == true)
                    {
                        Server.ExecuteCommand($"mp_freezetime {(Config?.VoteMapDuration ?? _freezeTime) + 2}");
                    }
                }
            }
            else
            {
                Server.ExecuteCommand($"mp_freezetime {_freezeTime}");
            }
        }
        else
        {
            var timelimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>() ?? 0.0f;

            if (timelimit > 0 && !_voteStarted && !_votedForCurrentMap && !gameRules.WarmupPeriod)
            {
                var timeLeft = _timeleft - Server.CurrentTime;

                if (timeLeft <= 180)
                {
                    MapUtil.PopulateMapsForVotes(Server.MapName, _cycleMaps, _mapForVotes);

                    if (_mapForVotes.Count < 1)
                    {
                        _votedForCurrentMap = true;
                        Logger.LogInformation("The list of voting maps is empty. I'm suspending the vote.");

                        return HookResult.Continue;
                    }

                    foreach (var map in _mapForVotes)
                    {
                        if (!_votes.ContainsKey(map.MapValue))
                        {
                            _votes[map.MapValue] = [];
                        }
                    }

                    if (Config?.VoteMapOnFreezeTime == true)
                    {
                        Server.ExecuteCommand($"mp_freezetime {(Config?.VoteMapDuration ?? _freezeTime) + 2}");
                    }
                }
            }
            else
            {
                Server.ExecuteCommand($"mp_freezetime {_freezeTime}");
            }
        }

        return HookResult.Continue;
    }

    public HookResult PlayerChatHandler(EventPlayerChat @event, GameEventInfo info)
    {
        if(@event == null) return HookResult.Continue;

        if (Config?.CommandsNextMap?.Contains(@event.Text.Trim()) == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                if (Config?.EnableNextMapCommand != true)
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "nextmap.get.command.disabled"));
                }
                else
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "nextmap.get.command").Replace("{MAP_NAME}", _nextmap?.MapValue));
                }
            }
        }

        if (Config?.CommandsCurrentMap?.Contains(@event.Text.Trim()) == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).ToList();

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

        if (Config?.CommandsLastMap?.Contains(@event.Text.Trim()) == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                if (Config?.EnableLastMapCommand != true)
                {
                    player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command.disabled"));
                }
                else
                {
                    if(string.IsNullOrEmpty(_lastmap))
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command.null"));
                    }
                    else
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "lastmap.get.command").Replace("{MAP_NAME}", _lastmap));
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
            _votedForCurrentMap = false;
            if (!Timers.IsRunning) Timers.Start();
        }

        if (_cycleMaps.Count > 0)
        {
            _nextmap = _cycleMaps[new Random().Next(_cycleMaps.Count)];
        }

        if(Config?.VoteMapOnFreezeTime == true)
        {
            _freezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
        }
    }

    public void OnMapEnd()
    {
        if (Config?.VoteMapEnable == true)
        {
            _votedForCurrentMap = false;
            if (Timers.IsRunning) Timers.Stop();
        }

        _votes.Clear();
        _mapForVotes.Clear();
        MenuUtil.PlayersMenu.Clear();
        _nextmap = null;
    }

    public void OnTick()
    {
        if (_voteStarted && Config?.VoteMapEnable == true)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

            foreach (var player in players)
            {
                if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened = true;

                if (Config?.EnablePlayerFreezeInMenu == true)
                {
                    if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
                    {
                        player.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_NONE;
                        Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
                        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                    }
                }

                MenuUtil.CreateAndOpenHtmlVoteMenu(player, _mapForVotes, _votes, Timers);
            }
        }
        else if(!_voteStarted || Config?.VoteMapEnable == false)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

            foreach (var player in players)
            {
                if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                if (MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened)
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

                    MenuUtil.CreateAndOpenHtmlMapsMenu(player, _maps, Timers);
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
