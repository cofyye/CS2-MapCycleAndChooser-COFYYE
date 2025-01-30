using CounterStrikeSharp.API.Core;

namespace Utils
{
    public static class PlayerUtil
    {
        public static bool IsValidPlayer(CCSPlayerController? p)
        {
            return p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected;
        }
    }
}
