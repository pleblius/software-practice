using System.Drawing;

namespace World;

/// <summary>
/// This class represents the game world, containing a collection of
/// all the game objects within the world.
/// </summary>
public class World
{
    /// <summary>
    /// A collection of extant snake objects.
    /// </summary>
    public readonly Dictionary<int, Snake> Snakes;

    /// <summary>
    /// A collection of extant wall objects.
    /// </summary>
    public readonly Dictionary<int, Wall> Walls;

    /// <summary>
    /// A collection of extant powerup objects.
    /// </summary>
    public readonly Dictionary<int, Powerup> Powerups;

    /// <summary>
    /// The size of the world, in pixels, to be drawn.
    /// </summary>
    public int worldSize { get; set; }

    /// <summary>
    /// Player's ID.
    /// </summary>
    public int ClientID { get; set; }

    /// <summary>
    /// How many frames have passed.
    /// </summary>
    public int FrameCounter {  get; set; }

    /// <summary>
    /// Default constructor. Creates empty ditionaries for each game object
    /// and sets the default world-size to 0px.
    /// </summary>
    public World()
    {
        Snakes = new();
        Walls = new();
        Powerups = new();
        worldSize = 0;
        FrameCounter = 0;
    }

    /// <summary>
    /// Resets the world.
    /// </summary>
    public void Reset()
    {
        Snakes.Clear();
        Walls.Clear();
        Powerups.Clear();
        worldSize = 0;
        FrameCounter = 0;
    }
}