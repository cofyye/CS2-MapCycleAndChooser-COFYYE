using Classes;

namespace Utils
{
    public class MapUtil
    {
        public static void PopulateMapsForVotes(
            string currentMap,
            List<Map> cycleMaps,
            List<Map> mapForVotes)
        {
            var random = new Random();

            // Lista za preostale mape koje možemo dodati
            var eligibleMaps = cycleMaps
                .Where(map => map.MapValue != currentMap && !mapForVotes.Any(votedMap => votedMap.MapValue == map.MapValue))
                .ToList();

            // Ako nema dovoljno mapa, dodaj sve preostale
            if (eligibleMaps.Count <= 5)
            {
                mapForVotes.AddRange(eligibleMaps);
                return;
            }

            // Dodavanje mapa sa proverenim uslovima
            for (int i = 0; i < 5; i++)
            {
                if (eligibleMaps.Count == 0)
                {
                    break; // Ako ponestane mapa, prekini petlju
                }

                // Izaberi nasumičnu mapu
                var randomIndex = random.Next(eligibleMaps.Count);
                var selectedMap = eligibleMaps[randomIndex];

                // Dodaj izabranu mapu u listu za glasanje
                mapForVotes.Add(selectedMap);

                // Ukloni mapu iz dostupnih kako bi se osigurala unikatnost
                eligibleMaps.RemoveAt(randomIndex);
            }
        }
        public static Map? GetWinningMap(List<Map> mapForVotes, Dictionary<string, List<string>> votes)
        {
            if (votes == null || votes.Count == 0)
                return null;

            // Izračunaj procenat glasova za svaku mapu
            var mapPercentages = new Dictionary<Map, double>();
            foreach (var map in mapForVotes)
            {
                var mapValue = map.MapValue;
                int totalVotes = votes.TryGetValue(mapValue, out List<string>? value) ? value.Count : 0;

                if (totalVotes > 0)
                {
                    mapPercentages[map] = (votes[mapValue].Count / (double)totalVotes) * 100;
                }
                else
                {
                    mapPercentages[map] = 0; // Niko nije glasao za ovu mapu
                }
            }

            // Nađi najveći procenat
            double maxPercentage = mapPercentages.Values.Max();

            // Pronađi sve mape sa najvećim procentom glasova
            var topMaps = mapPercentages
                .Where(kvp => kvp.Value == maxPercentage)
                .Select(kvp => kvp.Key)
                .ToList();

            // Ako ima više mapa sa istim najvećim procentom, nasumično izaberi jednu
            if (topMaps.Count > 1)
            {
                var random = new Random();
                return topMaps[random.Next(topMaps.Count)];
            }

            // Ako postoji samo jedna mapa sa najvećim procentom, vrati je
            return topMaps.FirstOrDefault();
        }
    }
}