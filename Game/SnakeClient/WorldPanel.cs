using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using World;
using Windows.Data.Text;
using System.Security.Cryptography;

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    private IImage wall;
    private IImage background;

    private World.World world;

    private readonly float viewSize = 900.0F;

    private bool initializedForDrawing = false;

    // A delegate for DrawObjectWithTransform
    // Methods matching this delegate can draw whatever they want onto the canvas
    public delegate void ObjectDrawer(object o, ICanvas canvas);

    /// <summary>
    /// Loads an image from the given filename.
    /// </summary>
    /// <param name="name">Filename to load image from.</param>
    /// <returns>An IImage object with the loaded image.</returns>
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
            return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public WorldPanel()
    {
    }

    /// <summary>
    /// Sets the world object being drawn.
    /// </summary>
    /// <param name="w">The world being drawn.</param>
    public void SetWorld(World.World w)
    {
        world = w;
    }

    /// <summary>
    /// Loads any necessary sprites for drawing the game.
    /// </summary>
    private void InitializeDrawing()
    {
        wall = loadImage( "wallsprite.png" );
        background = loadImage( "background.png" );
        initializedForDrawing = true;
    }

    /// <summary>
    /// Draw's the game state to the canvas.
    /// </summary>
    /// <param name="canvas">The canvas to be drawn on.</param>
    /// <param name="dirtyRect">The client's view frame.</param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if ( !initializedForDrawing )
        {
            InitializeDrawing();
        }
        


        // undo previous transformations from last frame
        canvas.ResetState();

        // If no snakes, data hasn't been received from server. (Used as handshake checker.)
        if (world.Snakes.Count == 0)
        {
            return;
        }

        //// Fix view on player snake's head here
        Vector2D playerPos;
        lock (world.Snakes)
        {
            playerPos = world.Snakes[world.ClientID].body.Last<Vector2D>();
        }
        canvas.Translate((float)-playerPos.X + (viewSize / 2), (float)-playerPos.Y + (viewSize / 2));

        // Draw background
        canvas.DrawImage(background, -world.worldSize/2, -world.worldSize/2, world.worldSize, world.worldSize);

        // Draw each game object
        lock (world.Snakes)
        {
            foreach (Snake snek in world.Snakes.Values)
            {
                // Only draw living snakes.
                if (!snek.alive)
                {
                    continue;
                }
                List<Vector2D> segments = snek.body;

                // Drawn from tail to head
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    Vector2D p1 = segments[i];
                    Vector2D p2 = segments[i + 1];

                    double length;
                    double angle;

                    // Faster than Pythagoras'.
                    if (p1.X == p2.X)
                    {
                        length = Math.Abs(p1.Y - p2.Y);
                    }
                    else
                    {
                        length = Math.Abs(p1.X - p2.X);
                    }

                    // If wraparound occurs, skip drawing this segment.
                    if (length >= world.worldSize -1)
                    {
                        continue;
                    }

                    angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X)* 180.0/Math.PI;

                    // Choose snake color based on id.
                    int IDmod = snek.snake % 8;

                    switch (IDmod)
                    {
                        case 0:
                        {
                            canvas.FillColor = Colors.MediumPurple;
                        } break;
                        case 1:
                        {
                            canvas.FillColor = Colors.Maroon;
                        } break;
                        case 2:
                        {
                            canvas.FillColor = Colors.MidnightBlue;
                        } break;
                        case 3:
                        {
                            canvas.FillColor= Colors.Red;
                        } break;
                        case 4:
                        {
                            canvas.FillColor = Colors.Teal;
                        } break;
                        case 5:
                        {
                            canvas.FillColor = Colors.Green;
                        } break;
                        case 6:
                        {
                            canvas.FillColor = Colors.Blue;
                        } break;
                        case 7:
                        {
                            canvas.FillColor = Colors.Gold;
                        } break;
                    }

                    DrawObjectWithTransform(canvas, length, p1.X, p1.Y, angle, SnakeDrawer);
                }

                // Once head is drawn, draw player's name beneath it
                Vector2D head = segments.Last<Vector2D>();

                DrawObjectWithTransform(canvas, snek, head.X, head.Y, 0, NameDrawer);
            }
        }
        lock (world.Walls)
        {
            foreach (Wall wall in world.Walls.Values)
            {
                //How many segments and the direction we need to draw them. 
                int numSegments = 0;
                int sign;

                //If the wall is horizontal...
                if (wall.p2.X != wall.p1.X)
                {
                    //Number of segments is just difference / 50 and abs valued.
                    numSegments = (int) (Math.Abs(wall.p2.X - wall.p1.X) / 50) + 1;
                    
                    if (wall.p2.X > wall.p1.X)
                    {
                        sign = 1;
                    }
                    else
                    {
                        sign = -1;
                    }

                    for (int i = 0; i < numSegments; i++)
                    {
                        //Draw at the X location; which offset by which segment we're at; at whichver Y location. 
                        DrawObjectWithTransform(canvas, wall, wall.p1.X + (sign * i * 50), wall.p1.Y, 0, WallDrawer);
                    }
                }
                //If the wall is vertical...
                else if (wall.p2.Y != wall.p1.Y)
                {
                    numSegments = (int) (Math.Abs(wall.p2.Y - wall.p1.Y) / 50) + 1;

                    if (wall.p2.Y > wall.p1.Y)
                    {
                        sign = 1;
                    }
                    else
                    {
                        sign = -1;
                    }

                    for (int i = 0; i < numSegments; i++)
                    {
                        DrawObjectWithTransform(canvas, wall, wall.p1.X, wall.p1.Y +  (sign * i * 50), 0, WallDrawer);
                    }
                }

                //Default case, one wall segment.
                DrawObjectWithTransform(canvas, wall, world.worldSize / 2, world.worldSize / 2, 0, WallDrawer);
            }
        }

        lock (world.Powerups)
        {
            foreach (Powerup power in world.Powerups.Values)
            {
                DrawObjectWithTransform(canvas, power, power.loc.X, power.loc.Y, 0, PowerupDrawer);
            }
        }
    }

    /// <summary>
    /// Draws an object using the ObjectDrawer <paramref name="drawer"/> delegate. Transforms the object
    /// into its given world coordinates and angle, so the object can be drawn as a simple drawing around the point (0,0).
    /// <paramref name="o"/>Is any object that can be passed to the drawer delegate.
    /// </summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="o">Any object used by the <paramref name="drawer"/>delegate.</param>
    /// <param name="worldX">The object's x position in world-coordinates.</param>
    /// <param name="worldY">The object's y position in world-coordinates.</param>
    /// <param name="angle">The object's angle in the world (in degrees).</param>
    /// <param name="drawer">An ObjectDrawer delegate used to draw the desired object.</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float) worldX, (float) worldY);
        canvas.Rotate((float) angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// Simple wall drawer, draws a 50/50 wall. 
    /// </summary>
    /// <param name="o">The wall being drawn.</param>
    /// <param name="canvas">The canvas being drawn on.</param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        canvas.DrawImage(wall, -25, -25, 50, 50);
    }

    /// <summary>
    /// Draws the body segment of a snake.
    /// </summary>
    /// <param name="o">The length of the body segment as a double.</param>
    /// <param name="canvas">The canvas object to draw on.</param>
    private void SnakeDrawer(object o, ICanvas canvas)
    {
        // You can't cast a double object directly to a float for some reason
        double l = (double) o;
        float length = (float) l;

        // Shift the drawing pivot in by five on each side
        canvas.FillRectangle(-5, -5, length+10, 10);
    }

    /// <summary>
    /// Pretty much just code from Lab11, used to draw powerups. Fairly unchanged.
    /// </summary>
    /// <param name="o">The powerup object being drawn.</param>
    /// <param name="canvas">The canvas being drawn on.</param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 16; //Width needed to be changed to meet spec.
        if (p.power % 2 == 0)
        {
            canvas.FillColor = Colors.Red;
        }
        else
        {
            canvas.FillColor = Colors.Blue;
        }

        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
    }

    /// <summary>
    /// Draws a name just below the snake's head. 
    /// </summary>
    /// <param name="o">The snake object whose name is being drawn.</param>
    /// <param name="canvas">The canvas being drawn on.</param>
    private void NameDrawer(object o, ICanvas canvas)
    {
        Snake snek = o as Snake;

        canvas.FontColor = Colors.White;
        canvas.FontSize = 14;
        canvas.Font = Font.Default;

        string text = snek.name + ": " + snek.score;

        canvas.DrawString(text, -20, 20, HorizontalAlignment.Left);
    }
}
