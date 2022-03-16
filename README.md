# Wills Wacky Managers

Provides 2 different managers for the community to use:
- CurseManager
- RerollManager

----
### v 1.3.5
- Bug Fixes

----
### v 1.3.4
- Optimized `CurseManager::RemoveAllCurses()` and added an additional variant.

----
### v 1.3.3
- Some of WWC's curses were moved over
- Table Flip now executes a reroll on all 4 players instead of doing its own thing.

----
### v 1.3.1
- Added the Card Manipulation category to Table Flip and Reroll

----
### v 1.3.0
- Removed RWF as a dependency.

----
### v 1.2.9
- Added a sync up operation to table flip, this should hopefully solve the issue of it breaking the game.
- Swapped to Unbound RPCs for Curse Removal, this should hopefully solve the issue of it not working.

----
### v 1.2.8
- Fixed an issue with Table Flip

----
### v 1.2.7
- Added a sync function for end of curse removal execution.

----
### v 1.2.6
- Setting Sync was working, I just had left some spaghetti code in that made it not be read.

----
### v 1.2.5
- Setting Sync should work now.

----
### v 1.2.4
- Reworked settings synchronization, hopefully it works now.

----
### v 1.2.3
- Logic patches, hopefully curse removal options should now work.

----
### v 1.2.2
- Fixed an issue where curse cards would cause following players to get null cards.

----
### v 1.2.1
- Fixes some issues with lag when taking hex and other curse granting cards.

----
### v 1.2.0
- Migrated the reroll cards over to WWM
- Added the option for table flip to become an uncommon, but only be able to appear when one player has at least half the rounds needed to win.
- Added the option to Enable or Disable cards that spawn curses. This is enabled by default.
- Added a timeout functionality to the Curse Removal Options. It take a minute to do so, but it is there.

----
### v 1.1.2
- Some patches that will hopefully help high lag players from initiating perpetual table flips and rerolls.

----
### v 1.1.1
- Fixed a bug where reroll would cause itself to trigger the next round, causing it to continue going off the rest of the game. (Thank you BYZE for finding this bug for me.)

----
### v 1.1.0
- Functionality has been added to allow for methods of removing cards outside of other cards, this has been opened up as an API for other modders to add their own methods. More information is available in the documentation below or on the github which has references to the default methods.
- A curse removal method requires 3 things:
  - The name of the method, this is what's displayed when the option is presented to the player.
  - A condition under which the method should be shown to the player.
  - The action to take when the method is chosen.
- By default this functionality is turned off, but there is a toggle to turn it on.
- By default there are 4 methods available:
  - Keep Curse: You opt to keep any curses you have. Always shows up.
  - Lose 1 round, lose 1 curse: Only shows up if you've won at least 1 round. Your number of won rounds is reduced by 1 and the newest curse incurred is removed.
  - Lose 1 Curse, give enemies an uncommon: Only shows up if you have more cards than at least 1 other player. Removes a curse and gives you enemies an uncommon.
  - Lose all cards, lose all curses: Only shows up if you have 5 or more curses. Removes all cards and curses that you have.

----
### v 1.0.3
- Added 3 new functions to the curse manager and documented them.

----
### v 1.0.2
- Fixed an issue where a curse may not always return when cursing someone.

----
### v 1.0.1
- Fixed an error in reroll's logic that would cause it to stop after rerolling a player.

----
## CurseManager

This manager provides the various utilities needed for using curses.

Any added curses must utilize `RegisterCurse()` via `CustomCard.BuildCard<MyCurse>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });`.

Additionally, they should use the `curseCategory` in order to not be pickable for players.

The `curseInteractionCategory` can be used to denote cards that should only show up when a player has a curse.

<details>
<summary>Properties</summary>

### instance
```cs
CurseManager instance { get;}
```
#### Description
A static reference of the class for accessibility from within static functions.

### curseCategory
```cs
CardCategory curseCategory { get;}
```
#### Description
The card category for every curse. If not utilized, curses may show up for regular picking.

### curseInteractionCategory
```cs
CardCategory curseInteractionCategory { get;}
```
#### Description
The card category for cards that interacted with cursed players. When utilized, cards with it will only show up when a player has a curse.

### curseSpawnerCategory
```cs
CardCategory curseSpawnerCategory { get;}
```
#### Description
The card category for cards that give players curses. Allows for toggling them on and off via settings.
</details>

<details>
<summary>Classes</summary>

### CurseRemovalOption
```cs
struct CurseRemovalOption
```
#### Fields
- readonly string name;
- readonly Func<Player, bool> condition;
- readonly Func<Player, IEnumerator> action;
 
#### Constructors
 
### CurseRemovalOption()
```cs
CurseRemovalOption CurseRemovalOption(string optionName, Func<Player, bool> optionCondition, Func<Player, IEnumerator> optionAction)
```
#### Description
Creates a Curse Removal Option

#### Parameters
- *string* `optionName` The text the player sees for choosing the option. Must be unique.
- *Func<Player, bool>* `optionCondition` A function that takes in a player object as input and outputs a bool. When true the option is available for players.
- *Func<Player, IEnumerator>* `optionAction` An IEnumerator that takes in a player object as input. Run when the option is selected. If it wishes to remove a curse, it must do so.

#### Example Usage
```CSHARP
var keepCurse = new CurseRemovalOption("Keep Curse", (player) => true, IKeepCurse);
RegisterRemovalOption(keepCurse);
var removeRound = new CurseRemovalOption("-1 round, -1 curse", CondRemoveRound, IRemoveRound);
RegisterRemovalOption(removeRound);

private IEnumerator IKeepCurse(Player player)
{
    yield break;
}



private bool CondRemoveRound(Player player)
{
    var result = false;
    // Only shows up if they have a round point to remove.
    if (GameModeManager.CurrentHandler.GetTeamScore(player.teamID).rounds > 0)
    {
        result = true;
    }

    return result;
}

private IEnumerator IRemoveRound(Player player)
{
    var score = GameModeManager.CurrentHandler.GetTeamScore(player.teamID);
    GameModeManager.CurrentHandler.SetTeamScore(player.teamID, new TeamScore(score.points, score.rounds - 1));

    var roundCounter = GameObject.Find("/Game/UI/UI_Game/Canvas/RoundCounter").GetComponent<RoundCounter>();
    roundCounter.InvokeMethod("ReDraw");

    for (var i = player.data.currentCards.Count() - 1; i >= 0; i--)
    {
        if (instance.IsCurse(player.data.currentCards[i]))
        {
            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, i);
            break;
        }
    }
    yield break;
}
```
</details>

<details>
<summary>Functions</summary>

### RandomCurse()
```cs
CardInfo RandomCurse(Player player)
```
#### Description
Returns a random curse that is valid for the target player. Respects card rarity.

#### Parameters
- *Player* `player` the player to get the curse for.

#### Example Usage
```CSHARP
var player = PlayerManager.instance.players[0];
var curse = CurseManager.instance.RandomCurse(player);
```

### CursePlayer()
```cs
void CursePlayer(Player player)
```
```cs
void CursePlayer(Player player, Action<CardInfo> callback)
```
#### Description
Adds a random valid curse to the targeted player. Respects card rarity.

#### Parameters
- *Player* `player` the player to curse.
- *Action<CardInfo>* `callback` an optional action to run with the card info of the added curse.

#### Example Usage
```CSHARP
var player = PlayerManager.instance.players[0];
CurseManager.instance.CursePlayer(player, (curse) => { ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, curse); });
```

### RegisterCurse()
```cs
void RegisterCurse(CardInfo cardInfo)
```
#### Description
Registers a card as a curse with the curse manager. The card still needs to apply `curseCategory` on its own.

#### Parameters
- *CardInfo* `cardInfo` the card to register.

#### Example Usage
```CSHARP
CustomCard.BuildCard<MyCurse>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
```

### GetRaw()
```cs
CardInfo[] GetRaw()
```
#### Description
Registers a card as a curse with the curse manager. The card still needs to apply `curseCategory` on its own.

#### Parameters

#### Example Usage
```CSHARP
var curse = CurseManager.instance.GetRaw();
```

### HasCurse()
```cs
bool HasCurse(Player player)
```
#### Description
Returns true if a player has a curse.

#### Parameters
- *Player* `player` the player to check.

#### Example Usage
```CSHARP
var player = PlayerManager.instance.players[0];
var cursed = CurseManager.instance.HasCurse(player);
```

### IsCurse()
```cs
bool IsCurse(CardInfo cardInfo)
```
#### Description
Returns true if the card is a registered curse.

#### Parameters
- *CardInfo* `cardInfo` the card to check.

#### Example Usage
```CSHARP
var card = PlayerManager.instance.players[0].data.currentCards[0];
var isCurse = CurseManager.instance.IsCurse(card);
```

### RemoveAllCurses()
```cs
void RemoveAllCurses(Player player)
```
```cs
void RemoveAllCurses(Player player, Action<CardInfo> callback)
```
#### Description
Removes all curses on the target player.

#### Parameters
- *Player* `player` the player to remove curses from.
- *Action<CardInfo>* `callback` an optional action to run with the card info of the removed curse.

#### Example Usage
```CSHARP

```

### GetAllCursesOnPlayer()
```cs
bool CardInfo[] GetAllCursesOnPlayer(Player player)
```
#### Description
Returns true if the card is a registered curse.

#### Parameters
- *Player* `player` the player whose curses to get.

#### Example Usage
```CSHARP

```

### RegisterRemovalOption()
```cs
void RegisterRemovalOption(string optionName, Func<Player, bool> optionCondition, Func<Player, IEnumerator> optionAction)
```
```cs
void RegisterRemovalOption(CurseRemovalOption option)
```
#### Description
Initiates any rerolls in the queue.

#### Parameters
- *string* `optionName` The text the player sees for choosing the option. Must be unique.
- *Func<Player, bool>* `optionCondition` A function that takes in a player object as input and outputs a bool. When true the option is available for players.
- *Func<Player, IEnumerator>* `optionAction` An IEnumerator that takes in a player object as input. Run when the option is selected. If it wishes to remove a curse, it must do so.

or 

- *CurseRemovalOption* `option` The curse removal option to implement.

#### Example Usage
```CSHARP
RegisterRemovalOption("Keep Curse", (player) => true, IKeepCurse);
var removeRound = new CurseRemovalOption("-1 round, -1 curse", CondRemoveRound, IRemoveRound);
RegisterRemovalOption(removeRound);

private IEnumerator IKeepCurse(Player player)
{
    yield break;
}

private bool CondRemoveRound(Player player)
{
    var result = false;
    // Only shows up if they have a round point to remove.
    if (GameModeManager.CurrentHandler.GetTeamScore(player.teamID).rounds > 0)
    {
        result = true;
    }

    return result;
}

private IEnumerator IRemoveRound(Player player)
{
    var score = GameModeManager.CurrentHandler.GetTeamScore(player.teamID);
    GameModeManager.CurrentHandler.SetTeamScore(player.teamID, new TeamScore(score.points, score.rounds - 1));

    var roundCounter = GameObject.Find("/Game/UI/UI_Game/Canvas/RoundCounter").GetComponent<RoundCounter>();
    roundCounter.InvokeMethod("ReDraw");

    for (var i = player.data.currentCards.Count() - 1; i >= 0; i--)
    {
        if (instance.IsCurse(player.data.currentCards[i]))
        {
            ModdingUtils.Utils.Cards.instance.RemoveCardFromPlayer(player, i);
            break;
        }
    }
    yield break;
}
```
</details>


## RerollManager

This manager provides the various utilities for rerolling a player's cards.

<details>
<summary>Properties</summary>

### instance
```cs
RerollManager instance { get;}
```
#### Description
A static reference of the class for accessibility from within static functions.

### NoFlip
```cs
CardCategory NoFlip { get;}
```
#### Description
The card category for cards that should not be given out after a table flip.

### flippingPlayer
```cs
Player flippingPlayer
```
#### Description
The player responsible for the tableflip. Used to add the table flip card to the player.

### tableFlipped
```cs
bool tableFlipped
```
#### Description
When set to true, a table flip will be initiated at the next end of a player's pick. Initiate the `FlipTable()` method if you wish to flip before then.

### tableFlipCard
```cs
CardInfo tableFlipCard
```
#### Description
The table flip card itself. It's automatically given out to the flipping player after a table flip.

### rerollPlayers
```cs
List<Player> rerollPlayers
```
#### Description
A list of players to reroll when the next reroll is initiated.

### reroll
```cs
bool reroll
```
#### Description
When set to true, a reroll will be initiated at the next end of a player's pick. Initiate the `Reroll()` method if you wish to reroll before then.

### rerollCard
```cs
CardInfo rerollCard
```
#### Description
The reroll card itself. It's automatically given out to the rerolling player after a table flip.
</details>

<details>
<summary>Functions</summary>

### FlipTable()
```cs
IEnumerator FlipTable(bool addCard = true)
```
#### Description
Initiates a table flip for all players.

#### Parameters
- *bool* `addCard` whether the flipping player (if one exists) shoul be given the Table Flip Card (if it exists).

#### Example Usage
```CSHARP


```

### InitiateRerolls()
```cs
IEnumerator InitiateRerolls(bool addCard = true)
```
#### Description
Initiates any rerolls in the queue.

#### Parameters
- *bool* `addCard` whether a player should be given the Reroll card after they reroll.

#### Example Usage
```CSHARP


```

### Reroll()
```cs
IEnumerator Reroll(Player player, bool addCard = true)
```
#### Description
Initiates any rerolls in the queue.

#### Parameters
- *Player* `player` the player whose cards to reroll
- *bool* `addCard` whether the player should be given the Reroll card afterwards.

#### Example Usage
```CSHARP


```
</details>