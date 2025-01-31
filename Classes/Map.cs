using System.Text.Json.Serialization;

namespace MapCycleAndChooser_COFYYE.Classes
{
    public class Map(string mapValue, string mapDisplay, bool mapWorkShop, bool? mapCanVote, int? mapMinPlayers, int? mapMaxPlayers)
    {
        [JsonPropertyName("map_value")]
        public string MapValue { get; } = mapValue;

        [JsonPropertyName("map_display")]
        public string MapDisplay { get; } = mapDisplay;

        [JsonPropertyName("map_ws")]
        public bool MapWorkshop { get; } = mapWorkShop;

        [JsonPropertyName("map_can_vote")]
        public bool? MapCanVote { get; } = mapCanVote;

        [JsonPropertyName("map_min_players")]
        public int? MapMinPlayers { get; } = mapMinPlayers;

        [JsonPropertyName("map_max_players")]
        public int? MapMaxPlayers { get; } = mapMaxPlayers;
    }
}
