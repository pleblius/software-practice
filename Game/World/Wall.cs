using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SnakeGame;

namespace World;

[XmlRoot("Wall")]
public class Wall
{
    //Player ID
    [XmlElement("ID")]
    public int wall { get; set; }
    
    //One endpoint of the wall segment
    public Vector2D p1 { get; set; }
   
    //One endpoint of the wall segment
    public Vector2D p2 { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public Vector2D x1y1 { get; set; }
    [JsonIgnore]
    [XmlIgnore]
    public Vector2D x2y1 { get; set; }
    [JsonIgnore]
    [XmlIgnore]
    public Vector2D x1y2 { get; set; }
    [JsonIgnore]
    [XmlIgnore]
    public Vector2D x2y2 { get; set; }



    /// <summary>
    /// Creates a new wall object. Used by JSON deserializer.
    /// </summary>
    public Wall()
    {
        wall = -1;
        p1 = new Vector2D();
        p2 = new Vector2D();
        x1y1 = new Vector2D();
        x2y1 = new Vector2D();
        x1y2 = new Vector2D();
        x2y2 = new Vector2D();

    }

    /// <summary>
    /// Creates a wall object with the given id, start point, and end point.
    /// </summary>
    /// <param name="ID">Wall's ID.</param>
    /// <param name="p1">Wall's start point.</param>
    /// <param name="p2">Wall's end point.</param>
    public Wall(int ID, Vector2D p1, Vector2D p2)
    {
        this.wall = ID;
        this.p1 = p1;
        this.p2 = p2;
        x1y1 = new Vector2D();
        x2y1 = new Vector2D();
        x1y2 = new Vector2D();
        x2y2 = new Vector2D();
        FindEndpoints();
    }

    public void FindEndpoints()
    {
        if (p1.Equals(p2))
        {
            x1y1 = new Vector2D(p1.X - 25, p1.Y + 25);
            x2y1 = new Vector2D(p1.X + 25, p1.Y + 25);
            x1y2 = new Vector2D(p1.X - 25, p1.Y - 25);
            x2y2 = new Vector2D(p1.X + 25, p1.Y - 25);
            return;
        }

        if(p1.Y == p2.Y)
        {
            if(p1.X < p2.X)
            {
                x1y1 = new Vector2D(p1.X - 25, p1.Y + 25);
                x2y1 = new Vector2D(p2.X + 25, p1.Y + 25);
                x1y2 = new Vector2D(p1.X - 25, p1.Y - 25);
                x2y2 = new Vector2D(p2.X + 25, p1.Y - 25);
                return;
            }
            else
            {
                x1y1 = new Vector2D(p2.X - 25, p1.Y + 25);
                x2y1 = new Vector2D(p1.X + 25, p1.Y + 25);
                x1y2 = new Vector2D(p2.X - 25, p1.Y - 25);
                x2y2 = new Vector2D(p1.X + 25, p1.Y - 25);
                return;
            }
        }

        if(p1.X == p2.X)
        {
            if(p1.Y < p2.Y)
            {
                x1y1 = new Vector2D(p1.X - 25, p1.Y + 25);
                x2y1 = new Vector2D(p1.X + 25, p2.Y + 25);
                x1y2 = new Vector2D(p1.X - 25, p1.Y - 25);
                x2y2 = new Vector2D(p1.X + 25, p2.Y - 25);
                return;
            }
            else
            {
                x1y1 = new Vector2D(p1.X - 25, p2.Y + 25);
                x2y1 = new Vector2D(p1.X + 25, p1.Y + 25);
                x1y2 = new Vector2D(p1.X - 25, p2.Y - 25);
                x2y2 = new Vector2D(p1.X + 25, p1.Y - 25);
                return;
            }
        }
    }
}
