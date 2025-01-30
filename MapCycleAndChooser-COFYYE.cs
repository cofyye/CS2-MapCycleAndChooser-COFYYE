using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using Utils;
using Classes;
using System.Diagnostics;

namespace MapCycleAndChooser_COFYYE;

public class MapCycleAndChooser : BasePlugin, IPluginConfig<Config.Config>
{
    public override string ModuleName => "Map Cycle and Chooser";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "cofyye";
    public override string ModuleDescription => "https://github.com/cofyye";

    public Config.Config Config { get; set; } = new();

    private static List<Map> _cycleMaps = [];
    private static readonly List<Map> _mapForVotes = [];
    private static bool _voteStarted = false;
    private static Map? _nextmap = null;
    private static Dictionary<string, List<string>> _votes = [];
    private new static readonly Stopwatch Timers = new();

    public void OnConfigParsed(Config.Config config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        _cycleMaps = Config?.MapCycle.Maps ?? [];
        _nextmap = _cycleMaps[new Random().Next(_cycleMaps.Count)];

        Logger.LogInformation("Initialized {MapCount} cycle maps.", _cycleMaps.Count);
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        RegisterEventHandler<EventRoundEnd>(RoundEndHandler);
        RegisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        RegisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        RegisterListener<Listeners.OnTick>(OnTick);

        Timers.Start();
    }

    public override void Unload(bool hotReload)
    {
        DeregisterEventHandler<EventCsWinPanelMatch>(CsWinPanelMatchHandler);
        DeregisterEventHandler<EventRoundEnd>(RoundEndHandler);
        DeregisterEventHandler<EventPlayerChat>(PlayerChatHandler);
        DeregisterEventHandler<EventPlayerConnectFull>(PlayerConnectFullHandler);
        DeregisterEventHandler<EventPlayerDisconnect>(PlayerDisconnectHandler);

        RemoveListener<Listeners.OnTick>(OnTick);

        Timers.Stop();
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
            ForceMenuOpened = false,
            Html = ""
        });

        return HookResult.Continue;
    }

    public HookResult PlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var steamId = @event?.Userid?.SteamID.ToString();

        if (string.IsNullOrEmpty(steamId)) return HookResult.Continue;

        //MenuUtil.PlayersMenu.Remove(steamId);

        return HookResult.Continue;
    }

    public HookResult CsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var matchRestartDelay = ConVar.Find("mp_match_restart_delay")?.GetPrimitiveValue<float>() ?? 5.0f;

        AddTimer(matchRestartDelay, () =>
        {
            if (_nextmap != null)
            {
                if (_nextmap.MapWorkshop)
                {
                    // Soon
                }
                else
                {
                    Server.PrintToChatAll("menja se mapa");
                    //Server.ExecuteCommand($"changelevel {_nextmap.MapValue}");
                }
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    public HookResult RoundEndHandler(EventRoundEnd @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if(gameRules == null)
        {
            Logger.LogError("Game rules not found.");
            return HookResult.Continue;
        }

        var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;

        if(maxRounds > 0 && !_voteStarted && !gameRules.WarmupPeriod)
        {
            var roundLeft = maxRounds - gameRules.TotalRoundsPlayed;

            if (roundLeft >= 3)
            {
                Server.PrintToChatAll("Vote started...");
                MapUtil.PopulateMapsForVotes(Server.MapName, _cycleMaps, _mapForVotes);
                _voteStarted = true;

                foreach (var map in _mapForVotes)
                {
                    if (!_votes.ContainsKey(map.MapValue))
                    {
                        _votes[map.MapValue] = [];
                    }
                }

                AddTimer(15.0f, ()  => {
                    _voteStarted = false;
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
                        if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                        MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened = false;
                        MenuUtil.PlayersMenu[player.SteamID.ToString()].ForceMenuOpened = false;
                        MenuUtil.PlayersMenu[player.SteamID.ToString()].Html = "";
                    }
                    Server.PrintToChatAll($"Vote finished... Nextmap is {_nextmap?.MapValue}");
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        return HookResult.Continue;
    }

    public HookResult PlayerChatHandler(EventPlayerChat @event, GameEventInfo info)
    {
        if(@event == null) return HookResult.Continue;

        if(@event.Text.Trim() == "!nextmap" || @event.Text.Trim() == "/nextmap")
        {
            var player = Utilities.GetPlayerFromUserid(@event.Userid);
            player?.PrintToChat($"Nextmap is: {_nextmap?.MapValue}");
        }

        return HookResult.Continue;
    }

    public void OnTick()
    {
        if (_voteStarted)
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

            foreach (var player in players)
            {
                if (!MenuUtil.PlayersMenu.ContainsKey(player.SteamID.ToString())) continue;
                //if (!MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened) continue;

                MenuUtil.PlayersMenu[player.SteamID.ToString()].ForceMenuOpened = true;
                MenuUtil.PlayersMenu[player.SteamID.ToString()].MenuOpened = true;

                MenuUtil.CreateAndOpenHtmlMenu(player, _mapForVotes, Timers);
            }
        }
    }
}
