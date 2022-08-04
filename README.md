## Triviador

The goal of this repository was to create a very simple desktop remake of the game "Triviador" which is available only for smartphones.
This version supports only 2 players.

### Game description
Two players connect to the game server. After connection, a map of the Czech Republic with its 14 regions is shown for each of the clients.
Every client gets 1000 points and one of the regions assigned. This assigned region is referred to as a "base region" and it has 3 health points.

The game consists of following stages:
1. Region occupation
   
Players try to answer the numeric question as precisely as possible. In case the answers from all players are the same, the winner is decided based on their answer speeds.
The winner selects two new free (if possible, neigboring) regions and occupies them. For each region the player gets 200 points. The round loser selects one region the same way.
In total, 4 questions are answered in this stage.

2. Fight
   
The map is split into 14 regions with respective owners based on the previous answers. The players attack each other. The current attacker may choose any neighboring enemy region to attack. Both players receive a trivia question with four possible answers.
- If both players answer correctly, a new numeric question is sent to both of them. The winner is selected the same way as in the first round.
- If the attacker is the only one to answer correctly, they get the region, 400 points and the region value is subtracted from the defender's points.
- If the defender is the only one to answer correctly, they get 100 points.
- If nobody answers correctly, nothing happens.

Attacking the enemy's base region is considered special. Players have to consider that the base regions have 3 health points. If the attacker wins the question, one health point is subtracted from the defender and a new question is shown.

In total, there are 4 rounds, which means there are at least 8 attacks.

3. Game over
   
The player which succesfully destroys all enemy bases wins. If there is no winner after all the attacks, the winner is based on total points. A tie may occur as well.

<hr style="height: 5px; border: 0; box-shadow: inset 0 12px 12px -12px rgba(0, 0, 0, 0.5);"/>

### Special thanks/credits
- original Triviador authors
- <a href="https://www.tiktok.com/@mahanlankarani">@mahanlankarani</a> on TikTok for questions inspiration
- hudry for helping with questions
- <a href="https://mapsvg.com/maps/czech-republic">MapSvg</a> for SVG version of the Czech Republic map
- Application icon by <a href="https://freeicons.io/profile/75801">Hilmy Abiyyu Asad</a> on <a href="https://freeicons.io">freeicons.io</a>