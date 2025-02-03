﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Text;
using System.Diagnostics;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using MapCycleAndChooser_COFYYE.Classes;

namespace MapCycleAndChooser_COFYYE.Utils
{
    public static class MenuUtil
    {
        public static readonly MapCycleAndChooser Instance = MapCycleAndChooser.Instance;
        public static Dictionary<string, PlayerMenu> PlayersMenu { get; } = [];

        public static void CreateAndOpenHtmlVoteMenu(
            CCSPlayerController player, 
            List<Map> mapsForVote,
            Dictionary<string, List<string>> _votes,
            Stopwatch timers)
        {
            string playerSteamId = player.SteamID.ToString();
            if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? pm)) return;

            List<string> menuValues = [];

            foreach (Map map in mapsForVote)
            {
                menuValues.Add(Instance.Config?.DisplayMapByValue == true ? map.MapValue : map.MapDisplay);
            }

            int currentIndex = PlayersMenu[playerSteamId].CurrentIndex;
            currentIndex = Math.Max(0, Math.Min(menuValues.ToArray().Length - 1, currentIndex));

            string bottomMenu = Instance.Localizer.ForPlayer(player, "menu.bottom.vote");
            string imageleft = Instance.Localizer.ForPlayer(player, "menu.item.left");
            string imageRight = Instance.Localizer.ForPlayer(player, "menu.item.right");

            int visibleOptions = 5;
            int startIndex = Math.Max(0, currentIndex - (visibleOptions - 1));

            if (timers.ElapsedMilliseconds >= 70 && !pm.Selected)
            {
                switch (player.Buttons)
                {
                    case 0:
                        {
                            pm.ButtonPressed = false;
                            break;
                        }
                    case PlayerButtons.Back:
                        {
                            currentIndex = Math.Min(menuValues.ToArray().Length - 1, currentIndex + 1);
                            pm.CurrentIndex = currentIndex;
                            player.ExecuteClientCommand("play sounds/ui/csgo_ui_contract_type4.vsnd_c");
                            pm.ButtonPressed = true;
                            break;
                        }
                    case PlayerButtons.Forward:
                        {
                            currentIndex = Math.Max(0, currentIndex - 1);
                            pm.CurrentIndex = currentIndex;
                            player.ExecuteClientCommand("play sounds/ui/csgo_ui_contract_type4.vsnd_c");
                            pm.ButtonPressed = true;
                            break;
                        }
                    case PlayerButtons.Use:
                        {
                            string currentMenuOption = menuValues.ToArray()[currentIndex];

                            var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                            if(Instance.Config?.EnablePlayerVotingInChat == true)
                            {
                                foreach (var p in players)
                                {
                                    p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", currentMenuOption));
                                }
                            }

                            MapUtil.AddPlayerToVotes(_votes, currentMenuOption, playerSteamId);

                            player.ExecuteClientCommand("play sounds/ui/item_sticker_select.vsnd_c");
                            pm.ButtonPressed = true;
                            pm.Selected = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            StringBuilder builder = new();

            string menuTitle = Instance.Localizer.ForPlayer(player, "menu.title.vote");
            builder.AppendLine(menuTitle);

            var percentages = MapUtil.CalculateMapsVotePercentages(_votes);

            for (int i = startIndex; i < startIndex + visibleOptions && i < menuValues.ToArray().Length; i++)
            {
                string currentMenuOption = menuValues.ToArray()[i];

                var percentage = percentages.TryGetValue(currentMenuOption, out int mapPercent) ? mapPercent : 0;

                if (i == currentIndex)
                {
                    string lineHtml = $"{imageRight} {Instance.Localizer.ForPlayer(player, "menu.item.vote").Replace("{MAP_NAME}", currentMenuOption).Replace("{MAP_PERCENT}", percentage.ToString())} {imageleft} <br />";
                    builder.AppendLine(lineHtml);
                }
                else
                {
                    string lineHtml = $"{Instance.Localizer.ForPlayer(player, "menu.item.vote").Replace("{MAP_NAME}", currentMenuOption).Replace("{MAP_PERCENT}", percentage.ToString())} <br />";
                    builder.AppendLine(lineHtml);
                }
            }

            builder.AppendLine(bottomMenu);
            builder.AppendLine("</div>");

            string centerhtml = builder.ToString();

            if (string.IsNullOrEmpty(PlayersMenu[playerSteamId].Html)) PlayersMenu[playerSteamId].Html = centerhtml;

            if (timers.ElapsedMilliseconds >= 70)
            {
                PlayersMenu[playerSteamId].Html = centerhtml;
                timers.Restart();
            }

            player?.PrintToCenterHtml(PlayersMenu[playerSteamId].Html);
        }

        public static void CreateAndOpenHtmlMapsMenu(
            CCSPlayerController player,
            List<Map> maps,
            Stopwatch timers)
        {
            string playerSteamId = player.SteamID.ToString();
            if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? pm)) return;

            List<string> menuValues = [];

            foreach (Map map in maps)
            {
                menuValues.Add(Instance.Config?.DisplayMapByValue == true ? map.MapValue : map.MapDisplay);
            }

            int currentIndex = PlayersMenu[playerSteamId].CurrentIndex;
            currentIndex = Math.Max(0, Math.Min(menuValues.ToArray().Length - 1, currentIndex));

            string bottomMenu = Instance.Localizer.ForPlayer(player, "menu.bottom.maps");
            string imageleft = Instance.Localizer.ForPlayer(player, "menu.item.left");
            string imageRight = Instance.Localizer.ForPlayer(player, "menu.item.right");

            int visibleOptions = 4;
            int startIndex = Math.Max(0, currentIndex - (visibleOptions - 1));

            if (timers.ElapsedMilliseconds >= 70 && !pm.Selected)
            {
                switch (player.Buttons)
                {
                    case 0:
                        {
                            pm.ButtonPressed = false;
                            break;
                        }
                    case PlayerButtons.Back:
                        {
                            currentIndex = Math.Min(menuValues.ToArray().Length - 1, currentIndex + 1);
                            pm.CurrentIndex = currentIndex;
                            player.ExecuteClientCommand("play sounds/ui/csgo_ui_contract_type4.vsnd_c");
                            pm.ButtonPressed = true;
                            break;
                        }
                    case PlayerButtons.Forward:
                        {
                            currentIndex = Math.Max(0, currentIndex - 1);
                            pm.CurrentIndex = currentIndex;
                            player.ExecuteClientCommand("play sounds/ui/csgo_ui_contract_type4.vsnd_c");
                            pm.ButtonPressed = true;
                            break;
                        }
                    case PlayerButtons.Reload:
                        {
                            PlayersMenu[playerSteamId].MenuOpened = false;
                            pm.ButtonPressed = true;
                            break;
                        }
                    case PlayerButtons.Use:
                        {
                            string currentMenuOption = menuValues.ToArray()[currentIndex];

                            player.ExecuteClientCommand("play sounds/ui/item_sticker_select.vsnd_c");

                            pm.ButtonPressed = true;
                            pm.Selected = true;

                            Map? map = maps.Find(map => map.MapValue == currentMenuOption || map.MapDisplay == currentMenuOption);

                            if(map != null)
                            {
                                var players = Utilities.GetPlayers().Where(p => PlayerUtil.IsValidPlayer(p));

                                foreach (var p in players)
                                {
                                    p.PrintToChat(Instance.Localizer.ForPlayer(p, "admin.change.map").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", currentMenuOption));
                                }

                                Instance.AddTimer(2.0f, () =>
                                {
                                    if (map.MapIsWorkshop)
                                    {
                                        Server.ExecuteCommand("");
                                    }
                                    else
                                    {
                                        Server.ExecuteCommand($"changelevel {map.MapValue}");
                                    }
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            }
                            else
                            {
                                player.PrintToChat(Instance.Localizer.ForPlayer(player, "map.not.found"));
                            }

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            StringBuilder builder = new();

            string menuTitle = Instance.Localizer.ForPlayer(player, "menu.title.maps");
            builder.AppendLine(menuTitle);

            for (int i = startIndex; i < startIndex + visibleOptions && i < menuValues.ToArray().Length; i++)
            {
                string currentMenuOption = menuValues.ToArray()[i];

                if (i == currentIndex)
                {
                    string lineHtml = $"{imageRight} {Instance.Localizer.ForPlayer(player, "menu.item.map").Replace("{MAP_NAME}", currentMenuOption)} {imageleft} <br />";
                    builder.AppendLine(lineHtml);
                }
                else
                {
                    string lineHtml = $"{Instance.Localizer.ForPlayer(player, "menu.item.map").Replace("{MAP_NAME}", currentMenuOption)} <br />";
                    builder.AppendLine(lineHtml);
                }
            }

            if (startIndex + visibleOptions < menuValues.ToArray().Length)
            {
                string moreItemsIndicator = Instance.Localizer.ForPlayer(player, "menu.more.items");
                builder.AppendLine(moreItemsIndicator);
            }

            builder.AppendLine(bottomMenu);
            builder.AppendLine("</div>");

            string centerhtml = builder.ToString();

            if (string.IsNullOrEmpty(PlayersMenu[playerSteamId].Html)) PlayersMenu[playerSteamId].Html = centerhtml;

            if (timers.ElapsedMilliseconds >= 70)
            {
                PlayersMenu[playerSteamId].Html = centerhtml;
                timers.Restart();
            }

            player?.PrintToCenterHtml(PlayersMenu[playerSteamId].Html);
        }
    }
}
