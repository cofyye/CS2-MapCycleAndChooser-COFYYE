using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Text;
using Classes;
using System.Diagnostics;

namespace Utils
{
    public static class MenuUtil
    {
        public static Dictionary<string, PlayerMenu> PlayersMenu { get; } = [];
        public static void CreateAndOpenHtmlMenu(CCSPlayerController player, List<Map> mapsForVote, Stopwatch timers)
        {
            string playerSteamId = player.SteamID.ToString();
            if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? value)) return;

            List<string> menuValues = [];

            foreach (Map map in mapsForVote)
            {
                menuValues.Add(map.MapValue);
            }

            int currentIndex = PlayersMenu[playerSteamId].CurrentIndex;
            currentIndex = Math.Max(0, Math.Min(menuValues.ToArray().Length - 1, currentIndex));
            string BottomMenu = "";
            string Imageleft = ">";
            string ImageRight = "<";
            int visibleOptions = 5;
            int startIndex = Math.Max(0, currentIndex - (visibleOptions - 1));
            if (timers.ElapsedMilliseconds >= 100)
            {
                switch (player.Buttons)
                {
                    case 0:
                        {
                            value.ButtonPressed = false;
                            break;
                        }
                    case PlayerButtons.Back:
                        {
                            currentIndex = Math.Min(menuValues.ToArray().Length - 1, currentIndex + 1);
                            value.CurrentIndex = currentIndex;
                            player.ExecuteClientCommand("play sounds/ui/csgo_ui_contract_type4.vsnd_c");
                            value.ButtonPressed = true;
                            break;
                        }
                    case PlayerButtons.Forward:
                        {
                            currentIndex = Math.Max(0, currentIndex - 1);
                            value.CurrentIndex = currentIndex;
                            player.ExecuteClientCommand("play sounds/ui/csgo_ui_contract_type4.vsnd_c");
                            value.ButtonPressed = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            StringBuilder builder = new();

            for (int i = startIndex; i < startIndex + visibleOptions && i < menuValues.ToArray().Length; i++)
            {
                string currentMenuOption = menuValues.ToArray()[i];

                if (i == currentIndex)
                {
                    string lineHtml = $"<font color='orange'> {Imageleft} {currentMenuOption} : [10%] {ImageRight} </font><br>";
                    builder.AppendLine(lineHtml);
                }
                else
                {
                    string lineHtml = $"<font color='white' class='fontSize-sm'>  {currentMenuOption} : [10%]  </font><br>";
                    builder.AppendLine(lineHtml);
                }
            }
            
            if (startIndex + visibleOptions < menuValues.ToArray().Length)
            {
                string moreItemsIndicator = "more down";
                builder.AppendLine(moreItemsIndicator);
            }

            builder.AppendLine("<br>" + BottomMenu);
            builder.AppendLine("</div>");

            var centerhtml = builder.ToString();

            if (string.IsNullOrEmpty(PlayersMenu[playerSteamId].Html)) PlayersMenu[playerSteamId].Html = centerhtml;

            if (timers.ElapsedMilliseconds >= 100)
            {
                PlayersMenu[playerSteamId].Html = centerhtml;
                timers.Restart();
            }

            player?.PrintToCenterHtml(PlayersMenu[playerSteamId].Html);
        }
    }
}
