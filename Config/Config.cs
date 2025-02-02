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

        [JsonPropertyName("depends_on_the_round")]
        public bool DependsOnTheRound { get; init; } = true;

        [JsonPropertyName("enable_player_voting_in_chat")]
        public bool EnablePlayerVotingInChat { get; init; } = true;

        [JsonPropertyName("maps")]
        public List<Map> Maps { get; init; } =
        [
            new Map("de_dust2", "De Dust2", false, true, true, 0, 64),
            new Map("de_inferno", "De Inferno", false, true, true, 0, 64)
        ];
    }
}