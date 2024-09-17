namespace World;
using SnakeGame;

/// <summary>
/// This class represents a Powerup object in the snake game. It stores the object's
/// ID number, location in the game world, and its alive/dead status.
/// </summary>
public class Powerup
{
    /// <summary>
    /// This object's game ID.
    /// </summary>
    public int power { get; set; }

    /// <summary>
    /// This object's game location, (x, y).
    /// </summary>
    public Vector2D loc { get; set; }

    /// <summary>
    /// Whether or not this object has been "killed."
    /// True represents a dead object, false represents an alive object.
    /// </summary>
    public bool died { get; set; }

    /// <summary>
    /// Default Powerup constructor. Used by JSON deserializer to create a new Powerup object.
    /// If called naturally, will result in an unusable Powerup object.
    /// </summary>
    public Powerup()
    {
        power = -1;
        loc = new Vector2D();
        died = false;
    }
}