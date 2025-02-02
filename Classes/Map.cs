using System.Text.Json.Serialization;

namespace MapCycleAndChooser_COFYYE.Classes
{
    public class Map(string mapValue, string mapDisplay, bool mapIsWorkshop, bool mapCycleEnabled, bool mapCanVote, int mapMinPlayers, int mapMaxPlayers)
    {
        [JsonPropertyName("map_value")]
        public string MapValue { get; init; } = mapValue;

        [JsonPropertyName("map_display")]
        public string MapDisplay { get; init; } = mapDisplay;

        [JsonPropertyName("map_is_workshop")]
        public bool MapIsWorkshop { get; init; } = mapIsWorkshop;

        [JsonPropertyName("map_cycle_enabled")]
        public bool MapCycleEnabled { get; init; } = mapCycleEnabled;

        [JsonPropertyName("map_can_vote")]
        public bool MapCanVote { get; init; } = mapCanVote;

        [JsonPropertyName("map_min_players")]
        public int MapMinPlayers { get; init; } = mapMinPlayers;

        [JsonPropertyName("map_max_players")]
        public int MapMaxPlayers { get; init; } = mapMaxPlayers;
    }
}