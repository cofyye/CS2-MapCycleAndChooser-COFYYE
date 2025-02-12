using CounterStrikeSharp.API.Core;
using MapCycleAndChooser_COFYYE.Classes;
using System.Text.Json.Serialization;

namespace MapCycleAndChooser_COFYYE.Config
{
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("vote_map_enable")]
        public bool VoteMapEnable { get; init; } = true;

        [JsonPropertyName("vote_map_duration")]
        public int VoteMapDuration { get; init; } = 15;

        [JsonPropertyName("vote_map_on_freezetime")]
        public bool VoteMapOnFreezeTime { get; init; } = true;

        [JsonPropertyName("vote_map_on_next_round")]
        public bool VoteMapOnNextRound { get; init; } = true;

        [JsonPropertyName("depends_on_the_round")]
        public bool DependsOnTheRound { get; init; } = true;

        [JsonPropertyName("enable_player_freeze_in_menu")]
        public bool EnablePlayerFreezeInMenu { get; init; } = true;

        [JsonPropertyName("enable_player_voting_in_chat")]
        public bool EnablePlayerVotingInChat { get; init; } = true;

        [JsonPropertyName("enable_nextmap_command")]
        public bool EnableNextMapCommand { get; init; } = true;

        [JsonPropertyName("enable_lastmap_command")]
        public bool EnableLastMapCommand { get; init; } = true;

        [JsonPropertyName("enable_currentmap_command")]
        public bool EnableCurrentMapCommand { get; init; } = true;

        [JsonPropertyName("enable_timeleft_command")]
        public bool EnableTimeLeftCommand { get; init; } = true;

        [JsonPropertyName("enable_command_ads_in_chat")]
        public bool EnableCommandAdsInChat { get; init; } = true;

        [JsonPropertyName("enable_dont_vote")]
        public bool EnableDontVote { get; init; } = true;

        [JsonPropertyName("dont_vote_position")]
        public string DontVotePosition { get; init; } = "top";

        [JsonPropertyName("enable_extend_map")]
        public bool EnableExtendMap { get; init; } = true;

        [JsonPropertyName("extend_map_time")]
        public int ExtendMapTime { get; init; } = 8;

        [JsonPropertyName("extend_map_position")]
        public string ExtendMapPosition { get; init; } = "bottom";

        [JsonPropertyName("display_map_by_value")]
        public bool DisplayMapByValue { get; init; } = true;

        [JsonPropertyName("commands_css_nextmap")]
        public List<string> CommandsCSSNextmap { get; init; } =
        [
            "css_nextmap",
            "css_sledecamapa"
        ];

        [JsonPropertyName("commands_css_maps")]
        public List<string> CommandsCSSMaps { get; init; } =
        [
            "css_maps",
            "css_mape"
        ];

        [JsonPropertyName("commands_nextmap")]
        public List<string> CommandsNextMap { get; init; } =
        [
            "!nextmap",
            "!sledecamapa"
        ];

        [JsonPropertyName("commands_lastmap")]
        public List<string> CommandsLastMap { get; init; } =
        [
            "!lastmap",
            "!proslamapa"
        ];

        [JsonPropertyName("commands_currentmap")]
        public List<string> CommandsCurrentMap { get; init; } =
        [
            "!currentmap",
            "!trenutnamapa"
        ];

        [JsonPropertyName("commands_timeleft")]
        public List<string> CommandsTimeLeft { get; init; } =
        [
            "!timeleft",
            "!preostalovreme"
        ];

        [JsonPropertyName("sounds")]
        public List<string> Sounds { get; init; } = 
        [
            "sounds/voice/gman_choose1.vsnd_c",
            "sounds/voice/gman_choose2.vsnd_c"
        ];

        [JsonPropertyName("maps")]
        public List<Map> Maps { get; init; } =
        [
            new Map("de_dust2", "De Dust2", false, "", true, true, 0, 64),
            new Map("de_mirage", "De Mirage", false, "", true, true, 0, 64),
            new Map("de_vertigo", "De Vertigo", false, "", true, true, 0, 64),
            new Map("de_overpass", "De Overpass", false, "", true, true, 0, 64),
            new Map("de_train", "De Train", false, "", true, true, 0, 64),
            new Map("de_nuke", "De Nuke", false, "", true, true, 0, 64),
            new Map("de_anubis", "De Anubis", false, "", true, true, 0, 64),
            new Map("de_ancient", "De Ancient", false, "", true, true, 0, 64),
            new Map("de_inferno", "De Inferno", false, "", true, true, 0, 64)
        ];
    }
}