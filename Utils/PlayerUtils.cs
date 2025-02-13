using CounterStrikeSharp.API.Core;

namespace MapCycleAndChooser_COFYYE.Utils
{
    public static class PlayerUtils
    {
        public static bool IsValidPlayer(CCSPlayerController? p)
        {
            return p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected;
        }
    }
}
