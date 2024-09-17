namespace GameController;
using World;
using NetworkUtil;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Drawing;
using SnakeGame;

/// <summary>
/// This class represents an object to control the behavior of the snake game.
/// It contains a copy of the world (model) to be adjusted as the game logic dictates.
/// It contains the client-server networking information and logic to retrieve data from the game server.
/// It contains the logic for parsing user inputs and sending the proper information to the game server.
/// </summary>
public class GameController
{
    /// <summary>
    /// The collection of objects representing the game world.
    /// </summary>
    private World world;

    /// <summary>
    /// The SocketState object containing the server-connection socket.
    /// </summary>
    private SocketState? serverSocket { get; set; }

    /// <summary>
    /// The player's current in-game name.
    /// </summary>
    public string clientName { get; private set; }
    /// <summary>
    /// The player's current in-game ID.
    /// </summary>
    public int clientID { get; private set; }

    /// <summary>
    /// Event that fires whenever the gameworld is updated.
    /// </summary>
    public event ModelChangedHandler? ModelChangedEvent;
    /// <summary>
    /// Event that fires whenever a networking error occurs.
    /// </summary>
    public event NetworkErrorHandler? NetworkErrorEvent;

    /// <summary>
    /// Event that fires whenever a snake dc's or dies.
    /// </summary>
    public event DeadSnakeHandler? SnakeDiedEvent;

    /// <summary>
    /// Delegate for model change events.
    /// </summary>
    public delegate void ModelChangedHandler();

    /// <summary>
    /// Delegate for network error events.
    /// </summary>
    /// <param name="message">Error message.</param>
    public delegate void NetworkErrorHandler(string message);

    /// <summary>
    /// Delegate for snake death.
    /// </summary>
    /// <param name="locn">Location of the snake's death.</param>
    public delegate void DeadSnakeHandler(Vector2D locn);

    /// <summary>
    /// Bool checked to see if a network connection has been established.
    /// </summary>
    public bool HandshakeEstablished { get; private set; }

    /// <summary>
    /// Direction object used to store the most-recent command input by the user.
    /// </summary>
    private Direction nextCommand { get; set; }

    /// <summary>
    /// Creates a new, empty game controller object. Client name and connection socket must be established before
    /// game can run.
    /// </summary>
    public GameController()
    {
        world = new World();

        clientName = string.Empty;
        clientID = -1;

        nextCommand = new();

        HandshakeEstablished = false;

        NetworkErrorEvent += ConnectionBroken;
    }

    /// <summary>
    /// Parses any commands input by the user, serializes them, then sends them to the server.
    /// </summary>
    /// <param name="input">The one-character string representing a user key-press.</param>
    public void CommandInput(string input)
    {
        if (input == "" || !HandshakeEstablished || serverSocket == null || !serverSocket.TheSocket.Connected)
        {
            return;
        }

        string dir;

        switch (input)
        {
            // Move up
            case "w":
            {
                dir = "up";
            }
            break;

            // Move left
            case "a":
            {
                dir = "left";
            }
            break;

            // Move down
            case "s":
            {
                dir = "down";
            }
            break;

            // Move right
            case "d":
            {
                dir = "right";
            }
            break;

            default:
            {
                dir = "none";
            }
            break;
        }

        nextCommand = new Direction(dir);
        string nextJson = JsonSerializer.Serialize<Direction>(nextCommand);
        if (nextJson != null)
        {
            Networking.Send(serverSocket.TheSocket, nextJson + "\n");
        }
    }

    /// <summary>
    /// Adds the given snake object to the world. If the snake is new,
    /// it assigns it a color (incrementing the associated color count) before adding it.
    /// It will replace any extant snake with the same ID.
    /// </summary>
    /// <param name="snek">Snake to be added to the game world.</param>
    private void AddSnake(Snake snek)
    {
        int snekID = snek.snake;

        // If snake dc'd or died this frame, Invoke death event
        if (snek.dc)
        {
            SnakeDiedEvent?.Invoke(snek.body.Last<Vector2D>());

            // If DC, remove snek from list 
            world.Snakes.Remove(snekID);

            return;
        }
        else if (snek.died)
        {
            SnakeDiedEvent?.Invoke(snek.body.Last<Vector2D>());
        }

        world.Snakes[snekID] = snek;
    }

    /// <summary>
    /// Adds the given powerup object to the world.
    /// It will replace any extant powerup with the same ID.
    /// </summary>
    /// <param name="power">Powerup to be added to the game world.</param>
    private void AddPowerup(Powerup power)
    {
        if (!world.Powerups.TryAdd(power.power, power))
        {
            world.Powerups[power.power] = power;
        }
    }

    /// <summary>
    /// Removes a powerup from the world
    /// </summary>
    /// <param name="power">Powerup to remove.</param>
    /// <returns>True if the powerup was successfully removed.</returns>
    private bool RemovePowerup(Powerup power)
    {
        return world.Powerups.Remove(power.power);
    }

    /// <summary>
    /// Adds the given wall object to the world.
    /// It will replace any extant wall with the same ID.
    /// </summary>
    /// <param name="wall">Wall to be added to the game world.</param>
    private void AddWall(Wall wall)
    {
        if (!world.Walls.TryAdd(wall.wall, wall))
        {
            world.Walls[wall.wall] = wall;
        }
    }

    /// <summary>
    /// Gets the world.
    /// </summary>
    /// <returns>This world instance</returns>
    public World GetWorld()
    {
        return world;
    }

    /// <summary>
    /// Resets the gameController. 
    /// </summary>
    public void Reset()
    {
        world.Reset();

        clientName = string.Empty;
        clientID = -1;

        nextCommand = new();

        HandshakeEstablished = false;

        serverSocket = null;

    }

    /// <summary>
    /// Attempts to connect to the game server at the provided address and port using the provided player name.
    /// If a connection is established, will begin the handshake process to initiate regular communication between
    /// the client and the server by sending the player's name to the server.
    /// </summary>
    /// <param name="address">The server IP address.</param>
    /// <param name="port">The connection port.</param>
    /// <param name="name">The player's connection name.</param>
    public void ConnectToServer(string address, int port, string name)
    {
        clientName = name;

        Networking.ConnectToServer(Handshake, address, port);
    }

    /// <summary>
    /// Performs the initial handshake between the game client in the server, passed as the callback
    /// for the initial network connection attempt.
    /// If an error occurs during the process, the connection attempt is canceled.
    /// Otherwise, after connection the client sends the server the player's name and begins a data retrieval loop.
    /// </summary>
    /// <param name="socketState">The socketstate generated by the connection to the server.</param>
    public void Handshake(SocketState socketState)
    {
        serverSocket = socketState;

        // If a connection error occurred, invoke network error event.
        if (socketState.ErrorOccurred)
        {
            string message = socketState.ErrorMessage ?? "Failed to connect to server.";
            NetworkErrorEvent?.Invoke(message);
        }
        else if (!Networking.Send(socketState.TheSocket, clientName + "\n"))
        {
            NetworkErrorEvent?.Invoke("Failed while sending client name to server. Connection closed.");
        }
        else
        {
            HandshakeEstablished = true;
            ReceiveData();
        }
    }

    /// <summary>
    /// Pings the server for additional data. If the socket has been disconnected,
    /// no further attempts at data retrieval are made.
    /// </summary>
    public void ReceiveData()
    {
        if (serverSocket != null && serverSocket.TheSocket.Connected && HandshakeEstablished)
        {
            serverSocket.OnNetworkAction = ParseData;
            Networking.GetData(serverSocket);
        }
    }

    /// <summary>
    /// Parses the data stored in <paramref name="socketState"/>'s data buffer.
    /// Splits the data along '\n' characters and passes it to the JSON parser.
    /// </summary>
    /// <param name="socketState">The socketState whose data buffer is being parsed.</param>
    private void ParseData(SocketState socketState)
    {
        if (socketState.ErrorOccurred)
        {
            string message = socketState.ErrorMessage ?? "Failed to receive data from server.";

            NetworkErrorEvent?.Invoke(message);
            
            return;
        }

        string[] splitStrings = Regex.Split(socketState.GetData(), "[\n]");

        // Last line will either be empty string or broken data - skip it
        for (int i = 0; i < splitStrings.Length-1; i++)
        {
            string str = splitStrings[i];

            ParseStrings(str);

            // Remove length +1 (to remove \n character that wasn't parsed into string).
            socketState.RemoveData(0, str.Length+1);
        }

        // Update view with new information
        ModelChangedEvent?.Invoke();
        world.FrameCounter++;
        // Reset loop
        ReceiveData();
    }

    /// <summary>
    /// Parses the string data received from the server into appropriate properties.
    /// If the clientID and world-size haven't been initialized, it 
    /// tries to parse the data as integers and sets those properties.
    /// Once the inital setup is complete, data is assumed to be in proper JSON format from the server, and is parsed
    /// into a wall, a snake, or a powerup object, which is then added to the world in a thread-safe manner.
    /// </summary>
    /// <param name="str">The received data to be parsed.</param>
    private void ParseStrings(string str)
    {
        // Check if client is in initial state
        if (clientID == -1)
        {
            if (int.TryParse(str, out int val))
            {
                clientID = val;
                world.ClientID = val;
            }
        }
        // Check if world is in initial state
        else if (world.worldSize == 0)
        {
            if (int.TryParse(str, out int val))
            {
                world.worldSize = val;
            }
        }
        else
        {
            JsonDocument doc;

            // No tryParse method available, have to catch bad parse attempts
            try
            {
                doc = JsonDocument.Parse(str);
            }
            catch
            {
                return;
            }

            // Parse is snek
            if (doc.RootElement.TryGetProperty("snake", out _))
            {
                Snake? newSnake = doc.Deserialize<Snake>();

                if (newSnake != null)
                {
                    lock (world.Snakes)
                    {
                        AddSnake(newSnake);
                    }
                }
            }
            // Parse is wall
            else if (doc.RootElement.TryGetProperty("wall", out _))
            {
                Wall? newWall = doc.Deserialize<Wall>();

                if (newWall != null)
                {
                    lock (world.Walls)
                    {
                        AddWall(newWall);
                    }
                }
            }
            // Parse is powerup
            else if (doc.RootElement.TryGetProperty("power", out _))
            {
                Powerup? newPower = doc.Deserialize<Powerup>();

                if (newPower != null)
                {
                    lock (world.Powerups)
                    {
                        if (newPower.died)
                        {
                            RemovePowerup(newPower);
                        }
                        else
                        {
                            AddPowerup(newPower);
                        }
                    }
                }
            }
            // No default case
        }
    }

    /// <summary>
    /// Sets the HandshakeEstablished flag to false to prevent attempts to send data to a server after the connection has been broken.
    /// </summary>
    /// <param name="message"></param>
    private void ConnectionBroken(string message)
    {
        Reset();
    }

    /// <summary>
    /// Class represents a serializable direction object. Used for JSON communication with server.
    /// </summary>
    public class Direction
    {
        // Direction of movement
        public string moving { get; set; }

        /// <summary>
        /// Creates a direction object with default direction "none".
        /// </summary>
        public Direction()
        {
            moving = "none";
        }

        /// <summary>
        /// Generates a direction object with the given string direction.<br><br></br></br>
        /// <p>
        /// Valid directions are:
        /// <list type="bullet">
        /// <item>"left"</item>
        /// <item>"right"</item>
        /// <item>"up"</item>
        /// <item>"down"</item>
        /// <item>"none"</item>
        /// </list>
        /// </p>
        /// </summary>
        /// <param name="str">Direction string is moving.</param>
        public Direction(string str)
        {
            moving = str;
        }
    }
}