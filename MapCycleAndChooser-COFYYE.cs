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

namespace MapCycleAndChooser_COFYYE;

public class MapCycleAndChooser : BasePlugin, IPluginConfig<Config.Config>
{
    public override string ModuleName => "Map Cycle and Chooser";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "cofyye";
    public override string ModuleDescription => "https://github.com/cofyye";

    public static MapCycleAndChooser Instance { get; set; } = new();
    public Config.Config Config { get; set; } = new();

    private static List<Map> _cycleMaps = [];
    private static List<Map> _maps = [];
    private static readonly List<Map> _mapForVotes = [];
    private static bool _voteStarted = false;
    private static bool _votedForCurrentMap = false;
    private static Map? _nextmap = null;
    private static float _timeleft = 0; // in seconds
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
            _nextmap = new Map(Server.MapName, Server.MapName, false, false, false, 0, 64);
        }

        if (Config?.DependsOnTheRound == null ||
            Config?.VoteMapDuration == null ||
            Config?.VoteMapEnable == null ||
            Config?.EnablePlayerVotingInChat == null ||
            Config?.Maps == null)
        {
            Logger.LogError("Config fields are null.");
            throw new ArgumentNullException(nameof(config));
        }

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

        Server.ExecuteCommand("mp_match_restart_delay 8");
        Logger.LogInformation("mp_match_restart_delay are set to 8.");

        Logger.LogInformation("Initialized {MapCount} cycle maps.", _cycleMaps.Count);
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        Instance = this;

        AddCommand("css_nextmap", "Set a next map", OnSetNextMap);

        RegisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        RegisterEventHandler<EventRoundStart>(RoundStartHandler);
        RegisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        if(Config?.VoteMapEnable == true && Config?.VoteMapOnFreezeTime == true)
        {
            _freezeTime = ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 5;
            _votedForCurrentMap = false;
            RegisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        if (Config?.VoteMapEnable == true)
        {
            RegisterListener<Listeners.OnTick>(OnTick);
            if(!Timers.IsRunning) Timers.Start();
        }

        AddTimer(300.0f, () =>
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                player.PrintToChat(Localizer.ForPlayer(player, "nextmap.set.command.info"));
            }
        }, TimerFlags.REPEAT);
    }

    public override void Unload(bool hotReload)
    {
        base.Load(hotReload);

        DeregisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        DeregisterEventHandler<EventRoundStart>(RoundStartHandler);
        DeregisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        DeregisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        DeregisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        if (Config?.VoteMapEnable == true && Config?.VoteMapOnFreezeTime == true)
        {
            DeregisterEventHandler<EventRoundEnd>(RoundEndHandler);
        }

        RemoveListener<Listeners.OnMapStart>(OnMapStart);
        RemoveListener<Listeners.OnMapEnd>(OnMapEnd);

        if (Config?.VoteMapEnable == true)
        {
            RemoveListener<Listeners.OnTick>(OnTick);
            if(Timers.IsRunning) Timers.Stop();
        }
    }

    [RequiresPermissions("@css/changemap")]
    public void OnSetNextMap(CCSPlayerController? caller, CommandInfo command)
    {
        if (!PlayerUtil.IsValidPlayer(caller)) return;

        if (command.ArgString == "")
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "nextmap.set.command.expected.value"));
            return;
        }

        Map? map = _cycleMaps.Find(m => m.MapValue == command.GetArg(1));

        if (map == null)
        {
            caller?.PrintToConsole(Localizer.ForPlayer(caller, "nextmap.set.command.not.exist"));
            return;
        }

        _nextmap = map;

        Server.PrintToChatAll(Localizer.ForPlayer(caller, "nextmap.set.command.new.map").Replace("{ADMIN_NAME}", caller?.PlayerName).Replace("{MAP_NAME}", _nextmap?.MapValue));

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

        AddTimer(7.0f, () =>
        {
            if (_nextmap != null)
            {
                if (_nextmap.MapIsWorkshop)
                {
                    // Soon
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
                var roundLeft = maxRounds - gameRules.TotalRoundsPlayed;

                if (roundLeft <= 3)
                {
                    var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                    foreach (var player in players)
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "vote.started"));
                    }

                    _voteStarted = true;

                    float duration = (float)(Config?.VoteMapDuration ?? 15);

                    AddTimer(duration, () => {
                        _voteStarted = false;
                        _votedForCurrentMap = true;

                        var winningMap = MapUtil.GetWinningMap(_mapForVotes, _votes);

                        if (winningMap != null)
                        {
                            _nextmap = winningMap;
                        }
                        else
                        {
                            Logger.LogError("Winning map is null.");
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
                var timeLeft = _timeleft - Server.CurrentTime;

                if (timeLeft <= 180)
                {
                    var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                    foreach (var player in players)
                    {
                        player.PrintToChat(Localizer.ForPlayer(player, "vote.started"));
                    }

                    _voteStarted = true;

                    float duration = (float)(Config?.VoteMapDuration ?? 15);

                    AddTimer(duration, () => {
                        _voteStarted = false;
                        _votedForCurrentMap = true;

                        var winningMap = MapUtil.GetWinningMap(_mapForVotes, _votes);

                        if (winningMap != null)
                        {
                            _nextmap = winningMap;
                        }
                        else
                        {
                            Logger.LogError("Winning map is null.");
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
        if(Config.VoteMapOnFreezeTime != true) return HookResult.Continue;

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

                    Server.ExecuteCommand($"mp_freezetime {(Config?.VoteMapDuration ?? _freezeTime) + 2}");
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

                    Server.ExecuteCommand($"mp_freezetime {(Config?.VoteMapDuration ?? _freezeTime) + 2}");
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

        if(@event.Text.Trim() == "!nextmap" || @event.Text.Trim() == "/nextmap")
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).ToList();

            foreach (var player in players)
            {
                player.PrintToChat(Localizer.ForPlayer(player, "nextmap.get.command").Replace("{MAP_NAME}", _nextmap?.MapValue));
            }
        }

        return HookResult.Continue;
    }

    public void OnMapStart(string mapName)
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
        if (Config?.VoteMapEnable != true) return;

        if (_voteStarted)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

            foreach (var player in players)
            {
                if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened = true;

                MenuUtil.CreateAndOpenHtmlVoteMenu(player, _mapForVotes, _votes, Timers);
            }
        }
    }
}
