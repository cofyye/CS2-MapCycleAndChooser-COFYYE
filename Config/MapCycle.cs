using System.Text.Json.Serialization;
using MapCycleAndChooser_COFYYE.Classes;

namespace MapCycleAndChooser_COFYYE.Config
{
    public class MapCycle
    {
        [JsonPropertyName("maps")]
        public List<Map> Maps { get; init; } = [
            new Map("de_dust2", "De Dust2", false, true, 0, 64),
            new Map("de_inferno", "De Inferno", false, true, 0, 64)
        ];
    }
}
