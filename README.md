# MapCycleAndChooser

MapCycleAndChooser is a CS2 plugin designed to give server admins complete control over map rotations and player interactions. With this plugin, you can manage map cycles, enable map voting, and customize various settings to optimize gameplay for all players.

## Features

- **Next Map Configuration & Display**: Set the next map in the cycle and display it for players.
- **Map Voting**: Players can vote for a new map towards the end of the current map.
- **Real-time Voting Percentages**: View the percentage of votes for each map in real time.
- **Admin Map List**: Admins can access a list of available maps and instantly change the current map.
- **Dynamic Map Cycle**: Set which maps are part of the cycle and which ones can be changed from the map list.
- **Dynamic Map Selection**: Maps are selected based on the current number of players, with options for max and min player thresholds.
- **Custom Map Display**: Show custom names instead of map values (e.g., showing "Dust II" instead of "de_dust2").
- **Round/Timelimit-Based Map Changes**: Change maps based on the number of rounds or time limits.
- **Vote Start Sound**: Play a sound when the voting for a new map begins.

## Screenshots

1. **`!nextmap` Command in Chat**: Displays the next map when the `!nextmap` command is typed in chat.

   ![Nextmap Command](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/blob/resources/nextmap.png?raw=true)

2. **Start of Map Voting**: Displays when a map vote begins.

   ![Map Voting Start](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/blob/resources/votemap1.png?raw=true)

3. **Voting Percentage After Map Selection**: Shows the percentage of votes for each map.

   ![Voting Percentage](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/blob/resources/votemap2.png?raw=true)

4. **Player Voting Logs**: Shows logs of which players voted for which maps and the next map.

   ![Player Voting Logs](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/blob/resources/votemap3.png?raw=true)

5. **Admin Map List**: Displays the map list that the admin can access and modify.

   ![Admin Map List](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/blob/resources/maps_menu.png?raw=true)

6. **Support for Local Language Change**: Demonstrates the plugin’s support for language customization.

   ![Language Support](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/blob/resources/changed_language.png?raw=true)

## Dependencies

To run this plugin, you need the following dependencies:

1. **Metamod:Source (2.x)**  
   Download from: [Metamod:Source Official Site](https://www.sourcemm.net/downloads.php/?branch=master)

2. **CounterStrikeSharp**  
   You can get it from: [CounterStrikeSharp GitHub Releases](https://github.com/roflmuffin/CounterStrikeSharp/releases)

3. **MultiAddonManager** *(optional)*  
   Download from: [MultiAddonManager GitHub Releases](https://github.com/Source2ZE/MultiAddonManager/releases)  

   - If you want to play a sound when map voting begins, this dependency is required. You can use your own custom sounds, though no tutorial is provided. Search online for guidance.  
   - Alternatively, if you'd like to use pre-configured sounds, visit this link: [Steam Workshop Sounds](https://steamcommunity.com/sharedfiles/filedetails/?id=3420306144).  

   **Setup for Sounds:**
   - To enable the sounds, you must add the `3420306144` ID to the `multiaddonmanager.cfg` file.  
   - Path to file: `game/csgo/cfg/multiaddonmanager/multiaddonmanager.cfg`.  
   - Add the ID under the `mm_extra_addons` section, for example:  
     ```json
     "....,3420306144"
     ```

## Commands and Permissions

1. **`!nextmap`**  
   - **Description**: Displays the next map in the cycle.  
   - **Access**: Available to all players.

2. **`css_nextmap`**  
   - **Description**: Sets the next map in the rotation.  
   - **Access**: Admins only, requires `@css/changemap` permission.

3. **`css_maps`**  
   - **Description**: Lists all maps and allows instant map changes.  
   - **Access**: Admins only, requires `@css/changemap` permission.

## Configuration Tutorial

Below is a step-by-step guide explaining the available configuration options for **MapCycleAndChooser**. These options allow you to customize how the plugin behaves and interacts with players.

### General Settings

1. **`vote_map_enable`**  
   - **Possible Values**: `true`, `false`  
   - **Description**: Enables or disables the voting system for a new map.  
     - `true`: Voting is enabled.  
     - `false`: Voting is disabled.

2. **`vote_map_duration`**  
   - **Possible Values**: Integer values (e.g., `30`, `60`, etc.)  
   - **Description**: Specifies the duration (in seconds) for the map voting period.

3. **`vote_map_on_freezetime`**  
   - **Possible Values**: `true`, `false`  
   - **Description**: Controls whether voting starts during freeze time.  
     - `true`: Increases the freeze time in the next round and starts voting during it.  
     - `false`: Voting starts at the beginning of the next round but does not extend freeze time. Players can vote while the round progresses.

4. **`depends_on_the_round`**  
   - **Possible Values**: `true`, `false`  
   - **Description**: Determines whether map voting is based on rounds or time.  
     - `true`: The plugin uses `mp_maxrounds` to trigger voting.  
     - `false`: The plugin uses `mp_timelimit` to trigger voting.

5. **`enable_player_freeze_in_menu`**  
   - **Possible Values**: `true`, `false`  
   - **Description**: Freezes players when the map list menu or voting menu is active.  
     - `true`: Players cannot move until the menu closes.  
     - `false`: Players can move even while voting.  
     - *Note*: For the best experience, set both `vote_map_on_freezetime` and this option to `true`. Otherwise, players may remain frozen after the round starts if only this option is enabled.

6. **`enable_player_voting_in_chat`**  
   - **Possible Values**: `true`, `false`  
   - **Description**: Logs in the chat which player voted for which map.  
     - `true`: Displays voting logs in the chat.  
     - `false`: Disables voting logs.

7. **`display_map_by_value`**  
   - **Possible Values**: `true`, `false`  
   - **Description**: Defines how maps are displayed.  
     - `true`: Displays the map by its technical name (e.g., `de_dust2`).  
     - `false`: Displays the map by its custom tag (e.g., `Dust II`).

8. **`sounds`**  
   - **Possible Values**: An array of string paths to sound files.  
   - **Description**: Specifies the sounds that play when map voting begins.  
     - Add as many sounds as you'd like, and the plugin will play one randomly.  
     - Leave this field empty (`[]`) to disable sounds.  

9. **`maps`**  
   - **Description**: A list of maps with customizable settings for each map. Each map entry contains the following:  
     - **`map_value`**: The technical name of the map (e.g., `de_dust2`).  
     - **`map_display`**: The custom display name for the map (e.g., `Dust II`).  
     - **`map_is_workshop`**: `true` if the map is from the Steam Workshop; `false` otherwise.  
     - **`map_workshop_id`**: The Workshop ID of the map (required if `map_is_workshop` is `true`, otherwise set to `""`).  
     - **`map_cycle_enabled`**: `true` if the map should be included in the map cycle; `false` if it should only appear in the admin map list (`css_maps`).  
     - **`map_can_vote`**: `true` if the map should appear in the voting system; `false` if it should not.  
     - **`map_min_players`**: Minimum number of players required for the map to be included in voting.  
     - **`map_max_players`**: Maximum number of players allowed for the map to be included in voting.

### Installation

1. Download the **[MapCycleAndChooser v1.0](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/releases/download/1.0/MapCycleAndChooser-COFYYE-v1.0.zip)** plugin as a `.zip` file.  
2. Upload the contents of the `.zip` file into the following directory on your server:  
   > game/csgo/addons/counterstrikesharp/plugins  
3. After uploading, change the map or restart your server to activate the plugin.  
4. The configuration file will be generated at:  
   > game/csgo/addons/counterstrikesharp/configs/plugins/MapCycleAndChooser-COFYYE/MapCycleAndChooser-COFYYE.json  
   Adjust all settings in this file as needed.

### Language Support
The language files are located in the following directory:
> game/csgo/addons/counterstrikesharp/plugins/MapCycleAndChooser-COFYYE/lang

Currently, there are two language files:
- `en.json` (English)
- `sr.json` (Serbian)

## Bug Reports & Suggestions

If you encounter any bugs or issues while using the plugin, please report them on the [GitHub Issues page](https://github.com/cofyye/CS2-MapCycleAndChooser-COFYYE/issues). Provide a detailed description of the problem, and I will work on resolving it as soon as possible.

Feel free to submit any suggestions for improvements or new features you'd like to see in future releases. Your feedback is highly appreciated!

## Important Notes

- **Missing Features**: The following features are not yet implemented in this version of the plugin but will be available in future updates:
  - `!rtv`
  - `!nominate`

These features are planned for inclusion, so stay tuned for upcoming versions!

## Credits

- **Code Snippets for Menu**: The menu code snippets were sourced from [oqyh's GitHub](https://github.com/oqyh). I would like to thank him for providing valuable resources that helped in building parts of this plugin.
  
- **Other Contributors**: A big thank you to all other authors and contributors of similar plugins that inspired the creation of this MapCycle and Chooser plugin. Their work was a key part of shaping the final version of this plugin.

This plugin is my version of the MapCycle and Chooser functionality, combining various elements from the community to provide a better and more customizable experience for server admins and players alike.

## Donation

If you would like to support me and help maintain and improve this plugin, you can donate via PayPal:

[Donate on PayPal](https://paypal.me/cofyye)

Your support is greatly appreciated!
