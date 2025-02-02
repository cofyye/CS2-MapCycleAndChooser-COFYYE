using CounterStrikeSharp.API;
using MapCycleAndChooser_COFYYE.Classes;

namespace MapCycleAndChooser_COFYYE.Utils
{
    public class MapUtil
    {
        public static void PopulateMapsForVotes(
            string currentMap,
            List<Map> cycleMaps,
            List<Map> mapForVotes)
        {
            var random = new Random();

            int currentPlayers = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p)).Count();

            var eligibleMaps = cycleMaps
                .Where(map =>
                    map.MapValue != currentMap &&
                    !mapForVotes.Any(votedMap => votedMap.MapValue == map.MapValue) &&
                    map.MapCanVote &&
                    map.MapMinPlayers <= currentPlayers &&
                    map.MapMaxPlayers >= currentPlayers
                )
                .ToList();

            if (eligibleMaps.Count == 0)
            {
                mapForVotes.Clear();
                return;
            }

            if (eligibleMaps.Count <= 5)
            {
                mapForVotes.AddRange(eligibleMaps);
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                if (eligibleMaps.Count == 0)
                {
                    break;
                }

                var randomIndex = random.Next(eligibleMaps.Count);
                var selectedMap = eligibleMaps[randomIndex];

                mapForVotes.Add(selectedMap);

                eligibleMaps.RemoveAt(randomIndex);
            }
        }

        public static Map? GetWinningMap(List<Map> mapForVotes, Dictionary<string, List<string>> votes)
        {
            if (votes == null || votes.Count == 0)
                return null;

            var mapPercentages = CalculateMapsVotePercentages(votes);

            double maxPercentage = mapPercentages.Values.Max();

            var topMaps = mapForVotes
                .Where(map => mapPercentages.ContainsKey(map.MapValue) && mapPercentages[map.MapValue] == maxPercentage)
                .ToList();

            if (topMaps.Count > 1)
            {
                var random = new Random();
                return topMaps[random.Next(topMaps.Count)];
            }

            return topMaps.FirstOrDefault();
        }

        public static void AddPlayerToVotes(Dictionary<string, List<string>> _votes, string mapValue, string playerSteamId)
        {
            if (!_votes.TryGetValue(mapValue, out List<string>? value))
            {
                value = ([]);
                _votes[mapValue] = value;
            }

            value.Add(playerSteamId);
        }

        public static Dictionary<string, int> CalculateMapsVotePercentages(Dictionary<string, List<string>> _votes)
        {
            var percentages = new Dictionary<string, int>();

            int totalVotes = _votes.Values.SelectMany(voteList => voteList).Count();

            if (totalVotes == 0)
            {
                return percentages;
            }

            foreach (var vote in _votes)
            {
                string map = vote.Key;
                int votesForMap = vote.Value.Count;

                int percentage = (int)Math.Round((double)votesForMap / totalVotes * 100);
                percentages[map] = percentage;
            }

            return percentages;
        }
    }
}