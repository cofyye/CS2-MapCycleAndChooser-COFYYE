using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Text;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using MapCycleAndChooser_COFYYE.Classes;
using MapCycleAndChooser_COFYYE.Variables;

namespace MapCycleAndChooser_COFYYE.Utils
{
    public static class MenuUtils
    {
        public static MapCycleAndChooser Instance => MapCycleAndChooser.Instance;
        public static Dictionary<string, PlayerMenu> PlayersMenu { get; } = [];

        //private static ScreenMenu? ScreenMenu = null;

        //private static void HandleChoosedOption(CCSPlayerController player, IMenuOption option)
        //{
        //    string playerSteamId = player.SteamID.ToString();
        //    if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? pm) || Instance == null) return;
        //    if (pm.Selected) return;

        //    var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

        //    var isIgnoreVoteOption = option.Text.Split("{splitignorevote}");
        //    var isExtendMapOption = option.Text.Split("{splitextendmap}");

        //    if (Instance?.Config?.EnablePlayerVotingInChat == true)
        //    {
        //        foreach (var p in players)
        //        {
        //            if (isIgnoreVoteOption.Length > 1)
        //            {
        //                p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", isIgnoreVoteOption[1]));
        //            }
        //            else if (isExtendMapOption.Length > 1)
        //            {
        //                p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", isExtendMapOption[1]));
        //            }
        //            else
        //            {
        //                p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", option.Text));
        //            }
        //        }
        //    }

        //    if (isIgnoreVoteOption.Length > 1)
        //    {
        //        MapUtils.AddPlayerToVotes(isIgnoreVoteOption[0], playerSteamId);
        //    }
        //    else if (isExtendMapOption.Length > 1)
        //    {
        //        MapUtils.AddPlayerToVotes(isExtendMapOption[0], playerSteamId);
        //    }
        //    else
        //    {
        //        MapUtils.AddPlayerToVotes(option.Text, playerSteamId);
        //    }

        //    player.ExecuteClientCommand("play sounds/ui/item_sticker_select.vsnd_c");
        //}

        //public static void CreateAndOpenScreenVoteMenu(CCSPlayerController player)
        //{
        //    string playerSteamId = player.SteamID.ToString();
        //    if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? pm) || Instance == null) return;

        //    var titleRGB = (Instance?.Localizer.ForPlayer(player, "screen.title.vote.rgb") ?? "255,165,0").Split(",");
        //    var itemRGB = (Instance?.Localizer.ForPlayer(player, "screen.item.rgb") ?? "255,255,224").Split(",");

        //    ScreenMenu = new("test menu", Instance!)
        //    {
        //        HasExitOption = false,
        //        TextColor = Color.FromArgb(int.Parse(titleRGB[0]), int.Parse(titleRGB[1]), int.Parse(titleRGB[2])),
        //        PostSelectAction = PostSelectAction.Nothing,
        //    };

        //    if (Instance?.Config?.EnableIgnoreVote == true && Instance.Config?.IgnoreVotePosition == "top")
        //    {
        //        ScreenMenu.AddOption("{screen.item.ignore.vote}{splitignorevote}" + Instance?.Localizer.ForPlayer(player, "screen.item.ignore.vote") ?? "-", HandleChoosedOption);
        //    }

        //    if (Instance?.Config?.EnableExtendMap == true && Instance.Config?.ExtendMapPosition == "top" && GlobalVariables.VotedForExtendMap == false)
        //    {
        //        if (Instance?.Config?.DependsOnTheRound == true)
        //        {
        //            ScreenMenu.AddOption("{screen.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "screen.item.extend.map.round").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-", HandleChoosedOption);
        //        }
        //        else
        //        {
        //            ScreenMenu.AddOption("{screen.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "screen.item.extend.map.timeleft").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-", HandleChoosedOption);
        //        }
        //    }

        //    ScreenMenu.TextColor = Color.Yellow;
        //    foreach (Map map in GlobalVariables.MapForVotes)
        //    {
        //        ScreenMenu.AddOption(Instance?.Config?.DisplayMapByValue == true ? map.MapValue : map.MapDisplay, HandleChoosedOption);
        //    }
        //    ScreenMenu.TextColor = Color.Orange;

        //    if (Instance?.Config?.EnableIgnoreVote == true && Instance.Config?.IgnoreVotePosition == "bottom")
        //    {
        //        ScreenMenu.AddOption("{screen.item.ignore.vote}{splitignorevote}" + Instance?.Localizer.ForPlayer(player, "screen.item.ignore.vote") ?? "-", HandleChoosedOption);
        //    }

        //    if (Instance?.Config?.EnableExtendMap == true && Instance?.Config?.ExtendMapPosition == "bottom" && GlobalVariables.VotedForExtendMap == false)
        //    {
        //        if (Instance.Config?.DependsOnTheRound == true)
        //        {
        //            ScreenMenu.AddOption("{screen.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "screen.item.extend.map.round").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-", HandleChoosedOption);
        //        }
        //        else
        //        {
        //            ScreenMenu.AddOption("{screen.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "screen.item.extend.map.timeleft").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-", HandleChoosedOption);
        //        }
        //    }

        //    MenuAPI.OpenMenu(Instance!, player, ScreenMenu);
        //}

        //public static void CloseScreenMenu()
        //{
        //    var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

        //    foreach(var player in players)
        //    {
        //        MenuAPI.CloseActiveMenu(player);
        //    }
        //}

        public static void CreateAndOpenHtmlVoteMenu(CCSPlayerController player)
        {
            string playerSteamId = player.SteamID.ToString();
            if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? pm)) return;

            List<string> menuValues = [];

            if(Instance?.Config?.EnableIgnoreVote == true && Instance.Config?.IgnoreVotePosition == "top")
            {
                menuValues.Add("{menu.item.ignore.vote}{splitignorevote}" + Instance?.Localizer.ForPlayer(player, "menu.item.ignore.vote") ?? "-");
            }

            if (Instance?.Config?.EnableExtendMap == true && Instance.Config?.ExtendMapPosition == "top" && GlobalVariables.VotedForExtendMap == false)
            {
                if(Instance?.Config?.DependsOnTheRound == true)
                {
                    menuValues.Add("{menu.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.round").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-");
                }
                else
                {
                    menuValues.Add("{menu.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.timeleft").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-");
                }
            }

            foreach (Map map in GlobalVariables.MapForVotes)
            {
                menuValues.Add(Instance?.Config?.DisplayMapByValue == true ? map.MapValue : map.MapDisplay);
            }

            if (Instance?.Config?.EnableIgnoreVote == true && Instance.Config?.IgnoreVotePosition == "bottom")
            {
                menuValues.Add("{menu.item.ignore.vote}{splitignorevote}" + Instance?.Localizer.ForPlayer(player, "menu.item.ignore.vote") ?? "-");
            }

            if (Instance?.Config?.EnableExtendMap == true && Instance?.Config?.ExtendMapPosition == "bottom" && GlobalVariables.VotedForExtendMap == false)
            {
                if (Instance.Config?.DependsOnTheRound == true)
                {
                    menuValues.Add("{menu.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.round").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-");
                }
                else
                {
                    menuValues.Add("{menu.item.extend.map}{splitextendmap}" + Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.timeleft").Replace("{EXTEND_TIME}", Instance?.Config?.ExtendMapTime.ToString()) ?? "-");
                }
            }

            int currentIndex = PlayersMenu[playerSteamId].CurrentIndex;
            currentIndex = Math.Max(0, Math.Min(menuValues.ToArray().Length - 1, currentIndex));

            string bottomMenu = Instance?.Localizer.ForPlayer(player, "menu.bottom.vote") ?? "";
            string imageleft = Instance?.Localizer.ForPlayer(player, "menu.item.left") ?? "";
            string imageRight = Instance?.Localizer.ForPlayer(player, "menu.item.right") ?? "";

            int visibleOptions = 5;
            int startIndex = Math.Max(0, currentIndex - (visibleOptions - 1));

            if (GlobalVariables.Timers.ElapsedMilliseconds >= 70 && !pm.Selected)
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

                            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

                            var isIgnoreVoteOption = currentMenuOption.Split("{splitignorevote}");
                            var isExtendMapOption = currentMenuOption.Split("{splitextendmap}");

                            if (Instance?.Config?.EnablePlayerVotingInChat == true)
                            {
                                foreach (var p in players)
                                {
                                    if(isIgnoreVoteOption.Length > 1)
                                    {
                                        p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", isIgnoreVoteOption[1]));
                                    }
                                    else if(isExtendMapOption.Length > 1)
                                    {
                                        p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", isExtendMapOption[1]));
                                    }
                                    else
                                    {
                                        p.PrintToChat(Instance.Localizer.ForPlayer(p, "vote.player").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", currentMenuOption));
                                    }
                                }
                            }

                            if (isIgnoreVoteOption.Length > 1)
                            {
                                MapUtils.AddPlayerToVotes(isIgnoreVoteOption[0], playerSteamId);
                            }
                            else if (isExtendMapOption.Length > 1)
                            {
                                MapUtils.AddPlayerToVotes(isExtendMapOption[0], playerSteamId);
                            }
                            else
                            {
                                MapUtils.AddPlayerToVotes(currentMenuOption, playerSteamId);
                            }

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

            string menuTitle = Instance?.Localizer.ForPlayer(player, "menu.title.vote") ?? "";
            builder.AppendLine(menuTitle);

            var percentages = MapUtils.CalculateMapsVotePercentages();

            for (int i = startIndex; i < startIndex + visibleOptions && i < menuValues.ToArray().Length; i++)
            {
                string currentMenuOption = menuValues.ToArray()[i];

                int percentage = 0;
                var isIgnoreVoteOption = currentMenuOption.Split("{splitignorevote}");
                var isExtendMapOption = currentMenuOption.Split("{splitextendmap}");

                if (isIgnoreVoteOption.Length > 1)
                {
                    percentage = percentages.TryGetValue(isIgnoreVoteOption[0], out int mapPercent) ? mapPercent : 0;
                }
                else if(isExtendMapOption.Length > 1)
                {
                    percentage = percentages.TryGetValue(isExtendMapOption[0], out int mapPercent) ? mapPercent : 0;
                }
                else
                {
                    percentage = percentages.TryGetValue(currentMenuOption, out int mapPercent) ? mapPercent : 0;
                }

                if (i == currentIndex)
                {
                    string lineHtml = "";

                    if(isIgnoreVoteOption.Length > 1)
                    {
                        lineHtml = $"{imageRight} <span color='yellow'>{isIgnoreVoteOption[1]}</span> <b color='orange'>•</b> <b color='lime'>{percentage}%</b> {imageleft} <br />";
                    }
                    else if (isExtendMapOption.Length > 1)
                    {
                        lineHtml = $"{imageRight} <span color='yellow'>{isExtendMapOption[1]}</span> <b color='orange'>•</b> <b color='lime'>{percentage}%</b> {imageleft} <br />";
                    }
                    else
                    {
                        lineHtml = $"{imageRight} {Instance?.Localizer.ForPlayer(player, "menu.item.vote").Replace("{MAP_NAME}", currentMenuOption).Replace("{MAP_PERCENT}", percentage.ToString())} {imageleft} <br />";
                    }

                    builder.AppendLine(lineHtml);
                }
                else
                {
                    string lineHtml = "";

                    if (isIgnoreVoteOption.Length > 1)
                    {
                        lineHtml = $"<span color='yellow'>{isIgnoreVoteOption[1]}</span> <b color='orange'>•</b> <b color='lime'>{percentage}%</b> <br />";
                    }
                    else if (isExtendMapOption.Length > 1)
                    {
                        lineHtml = $"<span color='yellow'>{isExtendMapOption[1]}</span> <b color='orange'>•</b> <b color='lime'>{percentage}%</b> <br />";
                    }
                    else
                    {
                        lineHtml = $"{Instance?.Localizer.ForPlayer(player, "menu.item.vote").Replace("{MAP_NAME}", currentMenuOption).Replace("{MAP_PERCENT}", percentage.ToString())} <br />";
                    }

                    builder.AppendLine(lineHtml);
                }
            }

            if (startIndex + visibleOptions < menuValues.ToArray().Length)
            {
                string moreItemsIndicator = Instance?.Localizer.ForPlayer(player, "menu.more.items") ?? "";
                builder.AppendLine(moreItemsIndicator);
            }

            builder.AppendLine(bottomMenu);
            builder.AppendLine("</div>");

            string centerhtml = builder.ToString();

            if (string.IsNullOrEmpty(PlayersMenu[playerSteamId].Html)) PlayersMenu[playerSteamId].Html = centerhtml;

            if (GlobalVariables.Timers.ElapsedMilliseconds >= 70)
            {
                PlayersMenu[playerSteamId].Html = centerhtml;
                GlobalVariables.Timers.Restart();
            }

            player?.PrintToCenterHtml(PlayersMenu[playerSteamId].Html);
        }

        public static void CreateAndOpenHtmlMapsMenu(CCSPlayerController player)
        {
            string playerSteamId = player.SteamID.ToString();
            if (!PlayersMenu.TryGetValue(playerSteamId, out PlayerMenu? pm)) return;

            List<string> menuValues = [];

            foreach (Map map in GlobalVariables.Maps)
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

            if (GlobalVariables.Timers.ElapsedMilliseconds >= 70 && !pm.Selected)
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

                            Map? map = GlobalVariables.Maps.Find(map => map.MapValue == currentMenuOption || map.MapDisplay == currentMenuOption);

                            if(map != null)
                            {
                                var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

                                foreach (var p in players)
                                {
                                    p.PrintToChat(Instance.Localizer.ForPlayer(p, "admin.change.map").Replace("{PLAYER_NAME}", p.PlayerName).Replace("{MAP_NAME}", currentMenuOption));
                                }

                                Instance.AddTimer(2.0f, () =>
                                {
                                    GlobalVariables.LastMap = Server.MapName;
                                    if (map.MapIsWorkshop)
                                    {
                                        if (string.IsNullOrEmpty(map.MapWorkshopId))
                                        {
                                            Server.ExecuteCommand($"ds_workshop_changelevel {map.MapValue}");
                                        }
                                        else
                                        {
                                            Server.ExecuteCommand($"host_workshop_map {map.MapWorkshopId}");
                                        }
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

                            PlayersMenu[playerSteamId].MenuOpened = false;

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

            if (GlobalVariables.Timers.ElapsedMilliseconds >= 70)
            {
                PlayersMenu[playerSteamId].Html = centerhtml;
                GlobalVariables.Timers.Restart();
            }

            player?.PrintToCenterHtml(PlayersMenu[playerSteamId].Html);
        }
    }
}
