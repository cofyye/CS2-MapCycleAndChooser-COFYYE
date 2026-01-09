using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;
using MapManager_COFYYE.Classes;
using MapManager_COFYYE.Variables;

namespace MapManager_COFYYE.Utils
{
    public static class MenuUtils
    {
        public static MapManager Instance => MapManager.Instance;

        // Constants for special menu options
        private const string IgnoreVoteKey = "{menu.item.ignore.vote}";
        private const string ExtendMapKey = "{menu.item.extend.map}";

        // Store vote key mappings for each option
        private static readonly Dictionary<string, string> OptionToVoteKey = [];

        public static void OpenVoteMenu(CCSPlayerController player)
        {
            if (!PlayerUtils.IsValidPlayer(player))
                return;

            var menu = new WasdMenu(
                Instance.Localizer.ForPlayer(player, "menu.title.vote"),
                Instance
            )
            {
                ExitButton = false,
                MenuTime = 0,
            };

            OptionToVoteKey.Clear();
            List<string> menuOptions = [];

            // Add ignore vote at top if enabled
            if (
                Instance?.Config?.EnableIgnoreVote == true
                && Instance.Config?.IgnoreVotePosition == "top"
            )
            {
                menuOptions.Add(IgnoreVoteKey);
            }

            // Add extend map at top if enabled
            if (
                Instance?.Config?.EnableExtendMap == true
                && Instance.Config?.ExtendMapPosition == "top"
                && GlobalVariables.VotedForExtendMap == false
            )
            {
                menuOptions.Add(ExtendMapKey);
            }

            // Add maps for voting
            foreach (Map map in GlobalVariables.MapForVotes)
            {
                string displayValue =
                    Instance?.Config?.DisplayMapByValue == true ? map.MapValue : map.MapDisplay;
                menuOptions.Add(displayValue);
            }

            // Add ignore vote at bottom if enabled
            if (
                Instance?.Config?.EnableIgnoreVote == true
                && Instance.Config?.IgnoreVotePosition == "bottom"
            )
            {
                menuOptions.Add(IgnoreVoteKey);
            }

            // Add extend map at bottom if enabled
            if (
                Instance?.Config?.EnableExtendMap == true
                && Instance?.Config?.ExtendMapPosition == "bottom"
                && GlobalVariables.VotedForExtendMap == false
            )
            {
                menuOptions.Add(ExtendMapKey);
            }

            var percentages = MapUtils.CalculateMapsVotePercentages();

            foreach (var option in menuOptions)
            {
                string displayText;
                string voteKey;

                if (option == IgnoreVoteKey)
                {
                    displayText =
                        Instance?.Localizer.ForPlayer(player, "menu.item.ignore.vote")
                        ?? "Ignore Vote";
                    voteKey = IgnoreVoteKey;
                }
                else if (option == ExtendMapKey)
                {
                    if (Instance?.Config?.DependsOnTheRound == true)
                    {
                        displayText = (
                            Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.round")
                            ?? "Extend Map"
                        ).Replace(
                            "{EXTEND_TIME}",
                            Instance?.Config?.ExtendMapTime.ToString() ?? ""
                        );
                    }
                    else
                    {
                        displayText = (
                            Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.timeleft")
                            ?? "Extend Map"
                        ).Replace(
                            "{EXTEND_TIME}",
                            Instance?.Config?.ExtendMapTime.ToString() ?? ""
                        );
                    }
                    voteKey = ExtendMapKey;
                }
                else
                {
                    displayText = option;
                    voteKey = option;
                }

                int percentage = percentages.TryGetValue(voteKey, out int mapPercent)
                    ? mapPercent
                    : 0;
                string menuItemText = $"{displayText} • {percentage}%";

                var item = menu.AddItem(
                    menuItemText,
                    (selectedPlayer, selectedOption) =>
                    {
                        // Extract original text before " • " to get the display text
                        string originalText = selectedOption.Text.Split(" • ")[0];

                        if (OptionToVoteKey.TryGetValue(originalText, out string? mappedVoteKey))
                        {
                            HandleVoteSelection(selectedPlayer, mappedVoteKey);
                        }
                    }
                );

                item.PostSelectAction = PostSelectAction.Nothing;

                // Store mapping from display text to vote key
                OptionToVoteKey[displayText] = voteKey;
            }

            menu.Display(player, 0);
        }

        private static void HandleVoteSelection(CCSPlayerController player, string voteKey)
        {
            if (!PlayerUtils.IsValidPlayer(player))
                return;

            string playerSteamId = player.SteamID.ToString();

            // Check if player has already voted - if yes, block further voting
            bool hasVoted = GlobalVariables.Votes.Values.Any(voteList =>
                voteList.Contains(playerSteamId)
            );

            if (hasVoted)
            {
                // Player has already voted, cannot vote again
                return;
            }

            if (Instance?.Config?.EnablePlayerVotingInChat == true)
            {
                var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

                string displayName;
                if (voteKey == IgnoreVoteKey)
                {
                    displayName =
                        Instance?.Localizer.ForPlayer(player, "menu.item.ignore.vote")
                        ?? "Ignore Vote";
                }
                else if (voteKey == ExtendMapKey)
                {
                    if (Instance?.Config?.DependsOnTheRound == true)
                    {
                        displayName = (
                            Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.round")
                            ?? "Extend Map"
                        ).Replace(
                            "{EXTEND_TIME}",
                            Instance?.Config?.ExtendMapTime.ToString() ?? ""
                        );
                    }
                    else
                    {
                        displayName = (
                            Instance?.Localizer.ForPlayer(player, "menu.item.extend.map.timeleft")
                            ?? "Extend Map"
                        ).Replace(
                            "{EXTEND_TIME}",
                            Instance?.Config?.ExtendMapTime.ToString() ?? ""
                        );
                    }
                }
                else
                {
                    displayName = voteKey;
                }

                foreach (var p in players)
                {
                    p.PrintToChat(
                        Instance
                            ?.Localizer.ForPlayer(p, "vote.player")
                            .Replace("{PLAYER_NAME}", player.PlayerName)
                            .Replace("{MAP_NAME}", displayName)
                            ?? ""
                    );
                }
            }

            MapUtils.AddPlayerToVotes(voteKey, playerSteamId);

            // Update menu percentages for all players
            RefreshVoteMenuPercentages();
        }

        public static void RefreshVoteMenuPercentages()
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));
            var percentages = MapUtils.CalculateMapsVotePercentages();

            foreach (var player in players)
            {
                var activeMenu = MenuManager.GetActiveMenu(player);
                if (activeMenu?.Menu is WasdMenu wasdMenu)
                {
                    // Update text for each option with new percentages
                    foreach (var option in wasdMenu.ItemOptions)
                    {
                        // Extract original display text (before " • ")
                        string originalText = option.Text.Split(" • ")[0];

                        // Find corresponding vote key
                        if (OptionToVoteKey.TryGetValue(originalText, out string? voteKey))
                        {
                            int percentage = percentages.TryGetValue(voteKey, out int mapPercent)
                                ? mapPercent
                                : 0;

                            option.Text = $"{originalText} • {percentage}%";
                        }
                    }
                }
            }
        }

        public static void OpenMapsMenu(CCSPlayerController player)
        {
            if (!PlayerUtils.IsValidPlayer(player))
                return;

            var menu = new WasdMenu(
                Instance.Localizer.ForPlayer(player, "menu.title.maps"),
                Instance
            )
            {
                ExitButton = true,
                MenuTime = 0,
            };

            foreach (Map map in GlobalVariables.Maps)
            {
                string displayValue =
                    Instance?.Config?.DisplayMapByValue == true ? map.MapValue : map.MapDisplay;

                var item = menu.AddItem(
                    displayValue,
                    (selectedPlayer, selectedOption) =>
                    {
                        HandleMapSelection(selectedPlayer, map);
                    }
                );

                item.PostSelectAction = PostSelectAction.Close;
            }

            menu.Display(player, 0);
        }

        private static void HandleMapSelection(CCSPlayerController player, Map selectedMap)
        {
            if (!PlayerUtils.IsValidPlayer(player))
                return;

            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

            string mapDisplayName =
                Instance?.Config?.DisplayMapByValue == true
                    ? selectedMap.MapValue
                    : selectedMap.MapDisplay;

            foreach (var p in players)
            {
                p.PrintToChat(
                    Instance
                        ?.Localizer.ForPlayer(p, "admin.change.map")
                        .Replace("{PLAYER_NAME}", player.PlayerName)
                        .Replace("{MAP_NAME}", mapDisplayName)
                        ?? ""
                );
            }

            Instance?.AddTimer(
                2.0f,
                () =>
                {
                    GlobalVariables.LastMap = Server.MapName;
                    if (selectedMap.MapIsWorkshop)
                    {
                        if (string.IsNullOrEmpty(selectedMap.MapWorkshopId))
                        {
                            Server.ExecuteCommand(
                                $"ds_workshop_changelevel {selectedMap.MapValue}"
                            );
                        }
                        else
                        {
                            Server.ExecuteCommand($"host_workshop_map {selectedMap.MapWorkshopId}");
                        }
                    }
                    else
                    {
                        Server.ExecuteCommand($"changelevel {selectedMap.MapValue}");
                    }
                },
                TimerFlags.STOP_ON_MAPCHANGE
            );
        }

        public static void OpenVoteMenuForAll()
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

            foreach (var player in players)
            {
                OpenVoteMenu(player);
            }

            // Start timer to refresh vote percentages every 1 second
            GlobalVariables.MenuRefreshTimer?.Kill();
            GlobalVariables.MenuRefreshTimer = Instance.AddTimer(
                1.0f,
                () =>
                {
                    if (GlobalVariables.IsVotingInProgress)
                    {
                        RefreshVoteMenuPercentages();
                    }
                },
                TimerFlags.REPEAT
            );
        }

        public static void CloseMenuForAll()
        {
            var players = Utilities.GetPlayers().Where(p => PlayerUtils.IsValidPlayer(p));

            foreach (var player in players)
            {
                MenuManager.CloseActiveMenu(player);
                player.PrintToCenterHtml("");
            }

            // Stop menu refresh timer
            GlobalVariables.MenuRefreshTimer?.Kill();
            GlobalVariables.MenuRefreshTimer = null;
        }
    }
}
