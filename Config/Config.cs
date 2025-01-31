using CounterStrikeSharp.API.Core;
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

        [JsonPropertyName("mapcycle")]
        public MapCycle MapCycle { get; init; } = new();
    }
}
