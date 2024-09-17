using System.Text.Json.Serialization;
using SnakeGame; 

namespace World
{
    public class Snake
    {
        //The snake of the snake; for the model
        public int snake { get; set; }
        //The player name of the snake
        public string name { get; set; }
        //A list representing the body. The last entry is the head.
        public List<Vector2D> body { get; set; }
        //The direction of the snake
        public Vector2D dir { get; set; }
        //Mm tasty powerups
        public int score { get; set; }
        //Whether or not we died. Useful for drawing an explosion. Only died for one frame.
        public bool died { get; set; }
        //If snake died, it is not alive. 
        public bool alive { get; set; }
        //Whether or not the snake player dced, ie skill issue.
        public bool dc { get; set; }
        //Player joined on this frame, only active for one frame. Not necessarily useful.
        public bool join { get; set; }

        // Snake's growth timer
        [JsonIgnore]
        public int growth { get; set; }

        // Snake's respawn timer
        [JsonIgnore]
        public int respawn {  get; set; }

        // Previous movement direction
        [JsonIgnore]
        public Vector2D prevDir { get; set; }

        // Flag for a snake's venom status
        [JsonIgnore]
        public bool venomous { get; set; }

        // Tracks how long the snake will remain venomous in venom mode
        [JsonIgnore]
        public long venomCounter { get; set; }
        
        // Stores the snake's name so the timer can be appended for un-modified clients
        [JsonIgnore]
        public string actualName { get; set; }

        /// <summary>
        /// Creates a new snake with empty parameters. Used by JSON deserializer.
        /// </summary>
        public Snake()
        {
            snake = -1;
            name = "";
            actualName = "";
            body = new List<Vector2D>();
            dir = new Vector2D();
            prevDir = new Vector2D();
            score = -1;
            died = false;
            alive = false;
            dc = false;
            join = false;
            growth = 0;
            venomous = false;
            venomCounter = 0;
        }

        /// <summary>
        /// Creates a new snake with the given id, name, body locations, and head direction.
        /// </summary>
        /// <param name="snake">Snake's ID.</param>
        /// <param name="Name">Snake's player name.</param>
        /// <param name="body">List of coordinates for the snake's body segments.</param>
        /// <param name="dir">Snake's direction of travel.</param>
        public Snake(int snake, string Name, List<Vector2D> body, Vector2D dir)
        {
            this.snake = snake;
            this.name = Name;
            this.body = body;
            this.dir = dir;
            score = 0;
            died = false;
            alive = true;
            dc = false;
            join = true;
            growth = 0;
            actualName = Name;

            prevDir = dir;
        }
    }
}
