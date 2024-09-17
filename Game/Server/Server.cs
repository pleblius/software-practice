using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using NetworkUtil;
using SnakeGame;
using World;

namespace Server;

public class Server
{
    TcpListener server;

    Dictionary<SocketState, int> sockets;
    int nextClientID = 0;

    World.World world;

    Random rng = new Random();

    // Game parameters
    int respawnTime = 20;
    long msPerFrame = 3000;
    
    int snakeSize = 120;
    int snakeGrowth = 24;
    int snakeSpeed = 6;
    int snakeWidth = 10;

    int maxPowerups = 20;
    int powerupDelay = 75;
    int powerupWidth = 10;
    int powerupScore = 10;
    
    int worldSize = 2000;

    int wallWidth = 50;

    bool defaultMode = true;
    bool poisonMode = false;
    bool venomMode = false;

    int venomCounter = 0;

    int powerupCounter = 0;
    int nextPowerupID = 0;


    public delegate void ServerUpdateHandler();
    public event ServerUpdateHandler ServerUpdate;

    public delegate void ClientDisconnectHandler(SocketState socketState);
    public event ClientDisconnectHandler ClientDisconnect;

    /// <summary>
    /// Simplest main method possible. Opens the console with a hello, creates a server, then starts it. 
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        Console.WriteLine("Starting server.");
        Server server = new();

        Console.WriteLine("Server open.");
        server.Run();
    }

    /// <summary>
    /// Default server constructor. Collects data from a given XML file if there is one.
    /// </summary>
    public Server()
    {
        server = Networking.StartServer(GetHandshake, 11000);

        sockets = new();
        world = new();

        ServerUpdate += Update;
        ClientDisconnect += OnClientDisconnect;

        // Get game settings from XML
        XmlSerializer serializer = new(typeof(GameSettings));
        try
        {
            StreamReader reader = new StreamReader("settings.xml");

            GameSettings? settings = (GameSettings?) serializer.Deserialize(reader);

            reader.Close();

            if (settings != null)
            {
                foreach (Wall wall in settings.Walls)
                {
                    world.Walls.Add(wall.wall, wall);
                    wall.FindEndpoints();
                }

                worldSize = settings.UniverseSize;
                msPerFrame = settings.MSPerFrame;
                respawnTime = settings.RespawnRate;
                snakeGrowth = settings.SnakeGrowthFrames;
                maxPowerups = settings.MaxPowerups;
                snakeSize = settings.SnakeStartingSize;
                snakeSpeed = settings.SnakeSpeed;
                powerupDelay = settings.PowerupDelay;

                if (settings.GameMode == "poison")
                {
                    defaultMode = false;
                    poisonMode = true;
                    venomMode = false;
                }
                else if (settings.GameMode == "venom")
                {
                    defaultMode = false;
                    poisonMode = false;
                    venomMode = true;
                    venomCounter = settings.VenomCounter;
                }
                else
                {
                    defaultMode = true;
                    poisonMode = false;
                    venomMode = false;
                }
            }
        }
        catch
        {

        }
    }

    /// <summary>
    /// Every time the stopwatch completes the time, restart it and call an update. This is the server's loop.
    /// </summary>
    private void Run()
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();

        while (true)
        {
            while (stopwatch.ElapsedMilliseconds < msPerFrame)
            { }

            stopwatch.Restart();

            ServerUpdate?.Invoke();
        }
    }

    /// <summary>
    /// Update calls every single method necessary to have a server run. Respawns, Moves, Checks Collision, Spawns Powerups, Collects Garbage, and Sends data.
    /// </summary>
    private void Update()
    {
        string sendData;

        // Entire update is locked to the world. Prevents race conditions on user disconnect.
        lock (world)
        {
            // Respawn snakes

            foreach (Snake snek in world.Snakes.Values)
            {
                // Skip dc'd snakes
                if (snek.dc)
                {
                    continue;
                }
                // Reset died flag - only applies to frame they died on
                if (snek.died)
                {
                    snek.died = false;
                }
                // Tick respawn timer
                if (snek.respawn != 0)
                {
                    snek.respawn--;
                }
                // Respawn snake
                if (snek.respawn == 0 && !snek.alive)
                {
                    SpawnSnake(snek);
                }
            }

            // Move snakes and check for collisions
            foreach (Snake snek in world.Snakes.Values)
            {
                // Move snakes
                if (snek.alive)
                {
                    MoveSnake(snek);

                    CheckPowerupCollisions(snek);
                    CheckSnakeCollisions(snek);
                    CheckWallCollisions(snek);
                }

                // Decrease venom counter after move
                if (snek.venomous)
                {
                    snek.venomCounter--;

                    if (snek.venomCounter == 0)
                    {
                        snek.venomous = false;
                    }
                }
            }

            // Spawn powerups

            if (powerupCounter == 0 && world.Powerups.Count < maxPowerups)
            {
                SpawnPowerup();
                powerupCounter = new Random().Next(powerupDelay);
            }
            else if (powerupCounter > 0)
            {
                powerupCounter--;
            }

            // Get string to send to clients
            sendData = GetDataToSend();

            // Garbage Collection
            foreach (Snake snek in world.Snakes.Values)
            {
                if (snek.dc)
                {
                    world.Snakes.Remove(snek.snake);
                }
            }

            foreach (Powerup powerup in world.Powerups.Values)
            {
                if (powerup.died)
                {
                    world.Powerups.Remove(powerup.power);
                }
            }
        }

        // Send data to each client
        lock (sockets)
        {

            foreach (SocketState socketState in sockets.Keys)
            {
                // If send fails, disconnect client.
                if (!Networking.Send(socketState.TheSocket, sendData))
                {
                    ClientDisconnect?.Invoke(socketState);
                }
            }
        }
    }

    /// <summary>
    /// Moves the chosen snake. 
    /// </summary>
    /// <param name="snek">snake to move.</param>
    private void MoveSnake(Snake snek)
    {
        // Move head
        Vector2D headDist = new(snakeSpeed*snek.dir.X, snakeSpeed*snek.dir.Y);
        Vector2D head = snek.body[^1];

        if (snek.dir != snek.prevDir)
        {
            // Add new head
            snek.body.Add(new());
            head = snek.body[^1];

            head.X = snek.body[^2].X;
            head.Y = snek.body[^2].Y;

            snek.prevDir = snek.dir;
        }

        head.X += headDist.X;
        head.Y += headDist.Y;

        snek.body[^1] = head;

        double warpDist;

        // Check for warps
        if (head.X > worldSize / 2 - snakeWidth/2)
        {
            warpDist = head.X - (worldSize / 2 - snakeWidth/2);

            snek.body.Clear();
            snek.body.Add(new(-worldSize / 2 + snakeWidth/2, head.Y));
            snek.body.Add(new(-worldSize / 2 + snakeWidth/2 + warpDist, head.Y));

            snek.growth = snakeSize/snakeSpeed + snakeGrowth * (snek.score / powerupScore);
            return;
        }
        else if (head.X < -worldSize / 2 + snakeWidth/2)
        {
            warpDist = (-worldSize / 2 - snakeWidth / 2) - head.X;

            snek.body.Clear();
            snek.body.Add(new(worldSize / 2 - snakeWidth/2, head.Y));
            snek.body.Add(new(worldSize / 2 + warpDist - snakeWidth/2, head.Y));

            snek.growth = snakeSize / snakeSpeed + snakeGrowth * (snek.score / powerupScore);
            return;
        }
        else if (head.Y > worldSize / 2 - snakeWidth/2)
        {
            warpDist = head.Y - (worldSize / 2 - snakeWidth / 2);

            snek.body.Clear();
            snek.body.Add(new(head.X, -worldSize / 2 + snakeWidth/2));
            snek.body.Add(new(head.X, -worldSize / 2 + warpDist + snakeWidth/2));

            snek.growth = snakeSize / snakeSpeed + snakeGrowth * (snek.score / powerupScore);
            return;
        }
        else if (head.Y < -worldSize / 2 + snakeWidth /2)
        {
            warpDist = (-worldSize/2 -snakeWidth / 2) -head.Y;

            snek.body.Clear();
            snek.body.Add(new(head.X, worldSize / 2 - snakeWidth/2));
            snek.body.Add(new(head.X, worldSize / 2 + warpDist - snakeWidth/2));

            snek.growth = snakeSize / snakeSpeed + snakeGrowth * (snek.score / powerupScore);
            return;
        }

        // Move Tail

        if (snek.growth > 0)
        {
            // Skip if snake is growing
            snek.growth--;
            return;
        }

        double moveToGo = snakeSpeed;

        //Makes sure we don't move too little by rechecking each new tail after deletion
        while (moveToGo > 0)
        {
            double tailLength = (snek.body[0] - snek.body[1]).Length();

            // Snip snake's tail
            if (tailLength < moveToGo)
            {
                moveToGo -= tailLength;
                snek.body.RemoveAt(0);
            }
            // Subtract movement length from remaining tail length
            else
            {
                Vector2D tail = snek.body[0];
                Vector2D tailDir = tail - snek.body[1];
                tailDir.Normalize();
                Math.Round(tailDir.X);
                Math.Round(tailDir.Y);

                tail -= new Vector2D(moveToGo*tailDir.X, moveToGo*tailDir.Y);

                snek.body[0] = tail;

                return; // I don't trust comparing a double to zero
            }
        }
    }

    /// <summary>
    /// Spawns the given snake in the world. Can either be a new snake or a respawn.
    /// </summary>
    /// <param name="snek">Snake to be spawned.</param>
    private void SpawnSnake(Snake snek)
    {
        // Placement limits
        int minY = -worldSize / 2 + snakeSize;
        int maxY = worldSize / 2;
        int minX = -worldSize / 2 + snakeWidth / 2;
        int maxX = worldSize / 2 - snakeWidth / 2;

        int snekX, snekY;
        
        // If spawn would result in collisions, repeat process
        do
        {
            // Random y value
            do
            {
                snekY = rng.Next(worldSize);
                snekY -= worldSize / 2;
            } while (snekY > maxY || snekY < minY);

            // Random x value
            do
            {
                snekX = rng.Next(worldSize);
                snekX -= worldSize / 2;
            } while (snekX > maxX || snekX < minX);

            List<Vector2D> newBody = new List<Vector2D>();

            newBody.Add(new Vector2D(snekX, snekY));
            newBody.Add(new Vector2D(snekX, snekY - snakeSize));

            snek.body = newBody;
            snek.dir = GetDirectionVector(new Direction("up"));
            snek.prevDir = snek.dir;

        } while (CheckRespawnCollisions(snekX, snekY, snakeWidth, snakeSize));

        snek.alive = true;
    }

    /// <summary>
    /// Spawns a new powerup somewhere in the world.
    /// </summary>
    private void SpawnPowerup()
    {
        // Placement limits
        int minY = (-worldSize / 2) + (powerupWidth / 2);
        int maxY = (worldSize / 2) - (powerupWidth / 2);
        int minX = minY;
        int maxX = maxY;

        int powerX, powerY;

        Powerup power;
        // If placement results in a collision, repeat
        do
        {
            // Random y
            do
            {
                powerY = rng.Next(worldSize);
                powerY -= worldSize / 2;
            } while (powerY > maxY || powerY < minY);

            // Random x
            do
            {
                powerX = rng.Next(worldSize);
                powerX -= worldSize / 2;
            } while (powerX > maxX || powerX < minX);

            power = new Powerup();
            power.loc = new Vector2D(powerX, powerY);

        } while (CheckRespawnCollisions(powerX, powerY, powerupWidth, 0));

        power.power = nextPowerupID++;

        world.Powerups.Add(power.power, power);
    }

    /// <summary>
    /// Helper to check the respawn location to make sure it doesn't collide with anything.
    /// </summary>
    /// <param name="locX">X coord of the spawm</param>
    /// <param name="locY">Y coord of the spawn</param>
    /// <param name="width">Width of the spawn</param>
    /// <param name="length">Height of the spawn</param>
    /// <returns>True if we do collide, false if we don't</returns>
    private bool CheckRespawnCollisions(int locX, int locY, int width, int length)
    {
        //We always spawn vertical snakes, so just move the minimum up by the object length.
        int bottomY = locY - length;

        //Check each point against walls
        for (int i = 0; i < (length / wallWidth) + 1; i++ )
        {
            Vector2D point = new(locX, bottomY + i* wallWidth);

            foreach (Wall wall in world.Walls.Values)
            {
                if (CheckWallCollision(point, wall, width))
                {
                    return true;
                }
            }
        }

        //Check each point against snakes.
        for (int i = 0; i < (length / snakeWidth) + 1; i++)
        {
            Vector2D point = new(locX, bottomY + (i * snakeWidth));

            foreach (Snake enemy in world.Snakes.Values)
            {
                if (!enemy.alive)
                {
                    continue;
                }

                if (CheckSnakeCollision(point, enemy, width))
                {
                    return true;
                }
            }
        }

        //Check each powerup.
        for (int i = 0; i < (length / powerupWidth) + 1; i++)
        {
            Vector2D point = new(locX, bottomY + (i * powerupWidth));

            foreach (Powerup power in world.Powerups.Values)
            {
                if (power.died)
                {
                    continue;
                }

                if (CheckPowerupCollision(point, power, width))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Kills a snake. 
    /// </summary>
    /// <param name="snek">Snake to be killed</param>
    private void KillSnake(Snake snek)
    {
        snek.died = true;
        snek.alive = false;
        snek.respawn = respawnTime;
        snek.growth = 0;
        snek.score = 0;
        snek.venomous = false;
        snek.venomCounter = 0;
    }

    /// <summary>
    /// Checks self collisions with the provided head and snake. 
    /// </summary>
    /// <param name="head">Head of snake to check collision for</param>
    /// <param name="snek">Snake to check collision for</param>
    /// <returns>True if we collide, false otherwise.</returns>
    private bool CheckSelfCollision(Vector2D head, Snake snek)
    {
        int width = snakeWidth / 2;

        Vector2D size = new(width, width);
        Vector2D segDir;
        bool reverseDirFound = false;

        // Check each snake segment not including the head
        for (int i = snek.body.Count - 2; i >= 1; i--)
        {
            double p2x = snek.body[i].X;
            double p1x = snek.body[i - 1].X;
            double p2y = snek.body[i].Y;
            double p1y = snek.body[i - 1].Y;

            // Ignore warps
            if (p2x == -p1x || p2y == -p1y)
            {
                continue;
            }

            // Check body segment direction. Collision shouldn't be checked until the snake has "wrapped around"
            segDir = new Vector2D(p2x, p2y) - new Vector2D(p1x, p1y);
            segDir.Normalize();

            if (segDir.IsOppositeCardinalDirection(snek.dir))
            {
                reverseDirFound = true;
            }

            Vector2D bl = new();
            Vector2D tr = new();

            if (p1x > p2x)
            {
                tr.X = p1x + snakeWidth / 2;
                bl.X = p2x - snakeWidth/2;
            }
            else
            {
                tr.X = p2x + snakeWidth / 2;
                bl.X = p1x - snakeWidth / 2;
            }

            if (p1y > p2y)
            {
                tr.Y = p1y + snakeWidth / 2;
                bl.Y = p2y - snakeWidth / 2;
            }
            else
            {
                tr.Y = p2y + snakeWidth / 2;
                bl.Y = p1y - snakeWidth / 2;
            }

            bl -= size;
            tr += size;

            if (head.X >= bl.X && head.X <= tr.X && head.Y >= bl.Y && head.Y <= tr.Y && reverseDirFound)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a point collides againts a snake.
    /// </summary>
    /// <param name="point">Point colliding (head, powerup)</param>
    /// <param name="snek">Snake collided against</param>
    /// <param name="size">Size of the object (different for powerups and snakes)</param>
    /// <returns>True if collided, false otherwise</returns>
    private bool CheckSnakeCollision(Vector2D point, Snake snek, int size)
    {
        Vector2D toAdd = new(size/2, size/2);
        Vector2D snakeAdd= new(snakeWidth / 2, snakeWidth / 2);

        // Check each snake segment

        for (int i = 0; i < snek.body.Count - 1; i++)
        {
            double p1x = snek.body[i].X;
            double p2x = snek.body[i + 1].X;
            double p1y = snek.body[i].Y;
            double p2y = snek.body[i + 1].Y;

            // SKip warps
            if (p1x == -p2x || p1y == -p2y)
            {
                continue;
            }

            Vector2D bl = new();
            Vector2D tr = new();

            if (p1x > p2x)
            {
                tr.X = p1x;
                bl.X = p2x;
            }
            else
            {
                tr.X = p2x;
                bl.X = p1x;
            }

            if (p1y > p2y)
            {
                tr.Y = p1y;
                bl.Y = p2y;
            }
            else
            {
                tr.Y = p2y;
                bl.Y = p1y;
            }

            bl -= toAdd;
            bl -= snakeAdd;
            tr += toAdd;
            tr += snakeAdd;

            if (point.X >= bl.X && point.X <= tr.X && point.Y >= bl.Y && point.Y <= tr.Y)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a point collides against a wall
    /// </summary>
    /// <param name="point">Point to be checked</param>
    /// <param name="wall">Wall we are checking</param>
    /// <param name="size">Size of the object</param>
    /// <returns>True if collided, otherwise false.</returns>
    private bool CheckWallCollision(Vector2D point, Wall wall, int size)
    {
        Vector2D toAdd = new Vector2D(size/2, size/2);

        Vector2D bl = wall.x1y2;
        Vector2D tr = wall.x2y1;

        bl -= toAdd;
        tr += toAdd;

        return point.X >= bl.X && point.X <= tr.X && point.Y >= bl.Y && point.Y <= tr.Y;
    }

    /// <summary>
    /// Checks if a point collides with a powerup
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <param name="powerup">Powerup we are checking</param>
    /// <param name="size">Size of the object</param>
    /// <returns>True if collided, otherwise false</returns>
    private bool CheckPowerupCollision(Vector2D point, Powerup powerup, int size)
    {
        Vector2D loc = powerup.loc;
        Vector2D limits = new(powerupWidth / 2, powerupWidth / 2);
        Vector2D toAdd = new(size/2, size/2);

        Vector2D bl = loc - limits - toAdd;
        Vector2D tr = loc + limits + toAdd;

        return point.X >= bl.X && point.X <= tr.X && point.Y >= bl.Y && point.Y <= tr.Y;
    }

    /// <summary>
    /// Overall method that checks for collisions of one snake against all other snakes
    /// </summary>
    /// <param name="snek">Snake that may be colliding</param>
    private void CheckSnakeCollisions(Snake snek)
    {
        //Head point
        Vector2D head = snek.body.Last();

        //Check each snake
        foreach (Snake enemy in world.Snakes.Values)
        {
            if (snek == enemy)
            {
                if (CheckSelfCollision(head, enemy))
                {
                    KillSnake(snek);
                }

                break; //We only care if the snake dies once
            }

            if (!enemy.alive) //Ignore dead snakes
            {
                continue;
            }

            if (CheckSnakeCollision(head, enemy, snakeWidth))
            {
                // Default mode = collisions are always deadly
                if (defaultMode)
                {
                    // If dual-collision, delete lower score one (ford f150 vs college student rule)
                    if (CheckSnakeCollision(enemy.body.Last(), snek, snakeWidth))
                    {
                        if (snek.score > enemy.score)
                        {
                            KillSnake(enemy);
                        }
                        else
                        {
                            KillSnake(snek);
                        }
                    }
                    else
                    {
                        KillSnake(snek);
                    }
                }
                // Poison mode = collision results in colliding snake being absorbed
                else if (poisonMode)
                {
                    // If dual-collision, delete lower score one (ford f150 vs college student rule)
                    if (CheckSnakeCollision(enemy.body.Last(), snek, snakeWidth))
                    {
                        if (snek.score > enemy.score)
                        {
                            snek.score += enemy.score;
                            snek.growth += enemy.score / 10 * snakeGrowth;

                            KillSnake(enemy);
                        }
                        else
                        {
                            enemy.score += snek.score;
                            enemy.growth += snek.score / 10 * snakeGrowth;
                            KillSnake(snek);
                        }
                    }
                    // This snek is absorbed
                    else
                    {
                        enemy.score += snek.score;
                        enemy.growth += snek.score / 10 * snakeGrowth;

                        KillSnake(snek);
                    }

                }
                // In venom mode, collisions result in the colliding snake eating the other if it's been envenomed
                else if (venomMode)
                {
                    if (snek.venomous)
                    {
                        // If dual-collision and both are venomous, delete lower score one (ford f150 vs college student rule)
                        if (enemy.venomous && CheckSnakeCollision(enemy.body.Last(), snek, snakeWidth))
                        {
                            if (snek.score > enemy.score)
                            {
                                if (enemy.score == 0)
                                {
                                    enemy.score = 10;
                                }

                                snek.score += enemy.score;

                                KillSnake(enemy);
                            }
                            else
                            {
                                if (snek.score == 0)
                                {
                                    snek.score = 10;
                                }

                                enemy.score += snek.score;

                                KillSnake(snek);
                            }
                        }
                        else
                        {
                            if (enemy.score == 0)
                            {
                                enemy.score = 10;
                            }

                            snek.score += enemy.score;

                            KillSnake(enemy);
                        }
                    }
                    // If snake is not envenomed, it dies normally on a collision
                    else
                    {
                        KillSnake(snek);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Overall helper method to see if a snake has collided with a powerup.
    /// </summary>
    /// <param name="snek">Snake that may be colliding</param>
    private void CheckPowerupCollisions(Snake snek)
    {
        //Check each powerup, if we collide with one, kill it, and add to the score/growth of the snake.
        foreach (Powerup power in world.Powerups.Values)
        {
            if (CheckPowerupCollision(snek.body.Last(), power, snakeWidth))
            {
                if (defaultMode || poisonMode)
                {
                    snek.score += powerupScore;
                    snek.growth += snakeGrowth;
                    power.died = true;
                }
                // In venom mode, powerups instead incrememnt venom counter
                else if (venomMode)
                {
                    power.died = true;
                    snek.venomCounter += venomCounter * 1000/msPerFrame;
                    snek.venomous = true;
                }
            }
        }
    }

    /// <summary>
    /// Overall helper method to see if a snake has collided with a wall
    /// </summary>
    /// <param name="snek">Snake that may have collided</param>
    private void CheckWallCollisions(Snake snek)
    {
        //Check each wall. If we collide, kill the snake. 
        foreach (Wall wall in world.Walls.Values)
        {
            if (CheckWallCollision(snek.body.Last(), wall, snakeWidth))
            {
                KillSnake(snek);
            }
        }
    }

    /// <summary>
    /// Gets the handshake from the client.
    /// </summary>
    /// <param name="socketState">SocketState containing the socket to handshake.</param>
    private void GetHandshake(SocketState socketState)
    {
        socketState.OnNetworkAction = SendHandshake;

        Networking.GetData(socketState);
    }

    /// <summary>
    /// Serverside confirmation of handshake, sends client id, then worldsize, then list of walls.
    /// </summary>
    /// <param name="socketState">SocketState to handshake with</param>
    private void SendHandshake(SocketState socketState)
    {
        Socket s = socketState.TheSocket;

        // If connection error occurs, write the error message to the console and end the connection attempt
        if (socketState.ErrorOccurred)
        {
            Console.WriteLine(socketState.ErrorMessage);
            s.Close();

            return;
        }

        // If connection succeeds, get the player name from the received data
        string playerName = GetPlayerName(socketState);

        // Then send the handshake, using a new client ID 
        StringBuilder dataString = new();
        int clientID;

        lock (server)
        {
            clientID = nextClientID++;
            dataString = dataString.Append(clientID.ToString() + "\n");
            dataString = dataString.Append(worldSize.ToString() + "\n");

            lock (world)
            {
                foreach (Wall wall in world.Walls.Values)
                {
                    dataString = dataString.Append(JsonSerializer.Serialize(wall) + "\n");
                }
            }

            string sendData = dataString.ToString();

            // On a successful handshake, send data and begin listening for client movement commands
            if (Networking.Send(s, sendData))
            {
                lock (sockets)
                {
                    sockets.Add(socketState, clientID);
                }

                lock (world)
                {
                    CreateSnake(clientID, playerName);
                }

                // Write connection to console
                Console.WriteLine($"Client {clientID} connected. Name: {playerName}");

                ReceiveData(socketState);
            }
        }
    }

    /// <summary>
    /// Receives data that may be sent via a SocketState
    /// </summary>
    /// <param name="socketState">SocketState to receive from</param>
    private void ReceiveData(SocketState socketState)
    {
        socketState.OnNetworkAction = ParseData;
        Networking.GetData(socketState);
    }

    /// <summary>
    /// Parse input data for a socket state. This handles parsing movement data.
    /// </summary>
    /// <param name="socketState">SocketState to parse data from</param>
    private void ParseData(SocketState socketState)
    {
        if (socketState.ErrorOccurred)
        {
            ClientDisconnect?.Invoke(socketState);
            return;
        }

        int clientID;

        lock (sockets)
        {
            clientID = sockets[socketState];
        }

        // Parse data in buffer
        string[] data = Regex.Split(socketState.GetData(), "[\n]");

        // Last element should be empty string. Use second-to-last element if it exists.
        if (data.Length < 2)
        {
            ReceiveData(socketState);
            return;
        }

        string dir = data[^2];
        Direction? direction = JsonSerializer.Deserialize<Direction>(dir);

        for (int i = 0; i < data.Length - 1; i++)
        {
            socketState.RemoveData(0, data[i].Length + 1);
        }

        // If direction is null, listen for more data
        if (direction == null)
        {
            ReceiveData(socketState);
            return;
        }

        if (direction.moving != "none")
        {
            Vector2D commandVector = GetDirectionVector(direction);

            lock (world)
            {
                Snake snek = world.Snakes[clientID];

                if (CheckDirectionCommand(snek, commandVector))
                {
                    snek.dir = commandVector;
                }
            }
        }

        ReceiveData(socketState);
    }

    /// <summary>
    /// Check the direction of the command. This prevents a snake from looping into itself/self colliding by looping into itself
    /// </summary>
    /// <param name="snek">Snake to check</param>
    /// <param name="newDirection">The new direction we're checking</param>
    /// <returns>True if the direction is valid, false otherwise</returns>
    private bool CheckDirectionCommand(Snake snek, Vector2D newDirection)
    {
        if (!snek.alive)
        {
            return false;
        }

        Vector2D headDirection = snek.body.Last() - snek.body[^2];
        double headLength = headDirection.Length();
        headDirection.Normalize();

        // Check if new command is a 180-degree flip
        if (headDirection.IsOppositeCardinalDirection(newDirection))
        {
            return false;
        }

        // Check if 180-degree rotation that would result in self-collision
        if (snek.body.Count >= 3)
        {
            Vector2D neckDirection = snek.body[^2] - snek.body[^3];
            neckDirection.Normalize();

            if (neckDirection.IsOppositeCardinalDirection(newDirection) && headLength <= snakeWidth)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a vector 2d object based on the provided direction.
    /// Vectors will point in a cardinal direction and will be normalized to a length of 1.
    /// </summary>
    /// <param name="direction">Direction to be converted into a normalized vector.</param>
    /// <returns>A normalized vector in the correct cardinal direction.</returns>
    private Vector2D GetDirectionVector(Direction direction)
    {
        switch (direction.moving)
        {
            case "up":
            {
                return new Vector2D(0, -1);
            }
            case "down":
            {
                return new Vector2D(0, 1);
            }
            case "left":
            {
                return new Vector2D(-1, 0);
            }
            case "right":
            {
                return new Vector2D(1, 0);
            }
            default:
            {
                return new Vector2D();
            }
        }
    }

    /// <summary>
    /// When a client disconnects, sets their snake's dc property to true and removes their associated socket from the socket list.
    /// </summary>
    /// <param name="clientID">The id of the disconnected client.</param>
    private void OnClientDisconnect(SocketState socketState)
    {
        int clientID;
        lock (sockets)
        {
            if (sockets.TryGetValue(socketState, out clientID))
            {
                lock (world)
                {
                    world.Snakes[clientID].dc = true;
                    world.Snakes[clientID].alive = false;
                    world.Snakes[clientID].died = true;
                }

                Console.WriteLine("Client {0} disconnected.", clientID);

                sockets.Remove(socketState);
            }
        }
    }

    /// <summary>
    /// Serializes all the data we need to send into a single string.
    /// </summary>
    /// <returns></returns>
    private string GetDataToSend()
    {
        StringBuilder data = new StringBuilder();

        foreach (Snake snek in world.Snakes.Values)
        {
            // Snake timer hack
            if (venomMode && snek.venomous)
            {
                snek.name = snek.actualName + " " + snek.venomCounter *msPerFrame/1000;
            }
            else
            {
                snek.name = snek.actualName;
            }

            data = data.Append(JsonSerializer.Serialize(snek) + "\n");
        }

        foreach (Powerup powerup in world.Powerups.Values)
        {
            data = data.Append(JsonSerializer.Serialize(powerup) + "\n");
        }

        return data.ToString();
    }

    /// <summary>
    /// Getter for a SocketState's player name
    /// </summary>
    /// <param name="socketState">SocketState to copy the name from</param>
    /// <returns>The string of the name</returns>
    private string GetPlayerName(SocketState socketState)
    {
        string[] splitStrings = Regex.Split(socketState.GetData(), "[\n]");

        return splitStrings[0];
    }

    /// <summary>
    /// Create a snake. Has up as a default direction.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    private void CreateSnake(int id, string name)
    {
        Snake s = new Snake();
        s.name = name;
        s.snake = id;
        s.alive = false;
        s.score = 0;
        s.dir = GetDirectionVector(new Direction("up"));
        s.prevDir = s.dir;
        s.respawn = 0;
        s.died = true;
        s.actualName = name;

        world.Snakes.Add(id, s);
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

        public Direction(string s)
        {
            moving = s;
        }
    }

    /// <summary>
    /// The game settings for the server, made from an XML file. 
    /// </summary>
    public class GameSettings
    {
        // ms per tick
        public int MSPerFrame { get; set; }
        
        // Frames
        public int RespawnRate { get; set; }
        
        // Pixels
        public int UniverseSize { get; set; }
       
        [XmlArray]
        public List<Wall> Walls { get; set; }
        
        // Frames per tick
        public int SnakeSpeed { get; set; }
        
        // Frames
        public int PowerupDelay { get; set; }
        public int MaxPowerups { get; set; }
        
        // Frames
        public int SnakeGrowthFrames { get; set; }
        
        // pixels
        public int SnakeStartingSize { get; set; }
        
        // "default", "poison", or "venom"
        public string GameMode { get; set; }

        // Seconds
        public int VenomCounter { get; set; }


        public GameSettings()
        {
            Walls = new();
            GameMode = "default";
        }
    }
}