## Triviador
For basic information about the game read the file README.md.

### Programmer documentation
The application consists of three projects.
The basic idea is that there is a server running, two (or potentially more) clients connect to the server and play the game. 

1. Commons

This project includes all common classes and method used by the class and the server.
File "Constants.cs" contains all of the messages exchanged between the server/client and delays, round times and other constants as well.
It also contains information about the regions and their neigbors.
File "GameInformation.cs" contains definition of the game data which is exchanged between the server and clients.

2. Server
   
Server is composed as a console application. The server is controlled with the "Program.cs" file in its specific directory. The main file tries to create the server and run it properly.
File "Server.cs" handles all of the serer logic. It controls all of the rounds by sending messages to all clients accordingly, by calling the "FirstRound" and "SecondRound" methods by the defined rules. When the game ends, the server restarts and new players may connect. If any of the clients disconnects, the sever will reset as well.

3. Client

The client app is a WPF app for Windows platform. The client connects to a server, which later communicates with the client and sends all of the needed information. The client application consists of four windows - one is the main window with connection information, the second one being the game window and two question windows. All of the windows respond accordingly to the server's instructions.

In the "GameWindow.xaml.cs", it is pretty usual to find a line like this one:
```cs
App.Current.Dispatcher.Invoke((Action)delegate { //code
});
```
This means that the code inside had to be executed from the main thread (to update the window).

### Short files description
`client/GameWindow.xaml(.cs)` - game board, controls main game logic
`client/MainWindow.xaml(.cs)` - initial login window, controls client-server connection
`client/QuestionABCDWindow.xaml(.cs)` - shows a window with some question with 4 possible answers
`client/QuestionNumericWindow.xaml(.cs)` - shows a window with a numeric question
`common/Constants.cs` - contains all common data
`common/GameInformation.cs` - contains definitons for all common game information
`server/Program.cs` - provides an entrypoint for the server
`server/Server.cs` - contains all of the server logic

### Possible extensions
The program could be extended in many ways. Some of those contain: 

- replacing the questions' file for a whole database
- map choosing
- 3-players game
- AI for singleplayer games
- different rules for a fast/long game
- player chat
- question hints, points system
- login system with a player database

### Custon program review