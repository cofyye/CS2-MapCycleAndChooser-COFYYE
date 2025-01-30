using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Config
{
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("mapcycle")]
        public MapCycle MapCycle { get; init; } = new();
    }
}
