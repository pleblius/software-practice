README

Authors: Alexa Fresz, Tyler Wilcox

PS9

EXTRA GAME MODES
We have two extra game modes: poison, and venom. These are set by settings in the XML file, which expects the following
	<GameMode>default</GameMode>
	<VenomCounter>15</VenomCounter>
VenomCounter is used as a setting for the venom game mode. It defines how long a snake will be venomous in SECONDS after picking up a powerup.
GameMode is the selecetd game mode, and has three options: 'default', 'venom', and 'poison'. 
These are the default settings, so feel free to copy these into your XML

SETTINGS
Our server expects more settings in the XML file, which are as follows
  <SnakeGrowthFrames>24</SnakeGrowthFrames> 
  <MaxPowerups>20</MaxPowerups> 
  <SnakeStartingSize>120</SnakeStartingSize> 
  <PowerupDelay>75</PowerupDelay> 

  How much a snake grows when it eats a powerup (measured in frames).
  How many powerups can be spawned (measured in count).
  How big a snake is by default.
  Maximum time between spawning powerups.

You probably will need these to make the server work. Feel free to copy the above into your XML file; these are the default settings regardless.

POISON GAME MODE
Poison is the first additional game mode. In poison, snakes absorb the scores of any snake they kill and grow for that amount. 
Ie, if someone had eaten 5 powerups, and you kill them (by them running into you you'll grow and gain score as if you ate five powerups yourself.  

VENOM
In the venom game mode, instead of powerups directly increasing a snake's score, they instead result in it becoming "venomous" for a duration. A timer (in seconds)
is displayed next to a snake's name if it is venomous. Venomous snakes need to "eat" other snakes by colliding with them. When they do, they absorb the score of
the snake they've eaten. If two venomous snakes collide head-on, the snake with the higher score "wins" and eats the other. 
Non-venomous snakes play the same as the default game-mode.

SERVER
Our server is one, self contained, large file. We felt no personal need for a controller; it would only seek to complexify the code further, and increase development time.
Our liberal use of helper methods made this assignment very abstract, and allowed us to complete tasks in individual blocks, by determining what each method needed to do.

MAIN
In main, we start a server, then get the update loop started by calling run. 

SERVER 
A server is a TCPListener, a dictionary of sockets and a world, with an update event, disconnect event, and others. There is a try catch block that reads a provided XML
settings file, allowing us to change settings/add walls/do whatever we want with it. This is done on server startup. Each value has a default in case there isn't a provided
settings file (but will have no walls).

RUN
Our run code is exceptionally simple; we have a stopwatch that fires an update after the alloted time, which is our frame rate. This happens forever.

UPDATE
Our update is mostly abstracted out into a few steps, which are as follows.
1. Respawn snakes 
2. Move snakes/check collisions
3. Spawn powerups
4. Garbage collection (dced snakes, dead powerups)
5. Send clients data

Each of these use their own logic, usually contained in helper methods, to perform their needed tasks.

RESPAWN
Respawn is handled by a spawn counter; the faster the game, the faster you'll respawn. If a snake died this frame, set died to false as to not repeatedly 'kill' a snake 
on the client (for example, so they don't explode 24 times). Each frame, decrement the counter if it isn't zero. If it is zero, and the snake isn't alive, respawn the snake.
Respawning is handled in a method that randomly chooses a position, checks if that position is valid, and repeats until a valid position is found. This is done by setting
the respawn limits within a certain distance of the border of the world, checking if it collides with any snakes or walls (we don't care about powerups), then spawning the 
snake.

MOVEMENT
Moving a snake is simple for the head; simply add a vector with the proper direction and speed. Here, we check for world warps (wraparound) and take care of them. The tail 
gets moved if it's not growing. If it isn't, just subtract the tail by the speed, making sure we don't over-subtract so that if we have to delete a vertex we can simply 
decrement the next new tail.

COLLISION
All collisions are only important, technically, to a snake. A wall and powerup simply exist. So, we check for collisions after moving each individual snake. This follows 
lecture code ideas very closely, checking x and y bounds, and generally being O(S*S)/O(S*W)/O(S*P) for each type of collision. We also handle self collisions in here, using
another helper method that makes sure we aren't self colliding with the segment right behind the head. Head/head collisions go to whoever has the highest score. If there's a 
tie, one dies at random. 

GROWTH
Snake growth is handled by incrementing a timer that prevents the tail from moving. Each eaten powerup adds to that counter, so you can grow for a long while if you eat many
powerups in a row. 

POWERUP SPAWNS
Powerup spawns are similar to snake respawns. There's a timer for how often they can respawn, and when they do spawn, we grab a random valid spot (a valid spot is one that
doesn't intersect with a wall, but *can* intersect with a snake). 

GARBAGE COLLECTION
We delete dead powerups and dced snakes. Dead powerups and dced snakes are only ever sent once, then removed.

SEND DATA
Serialize the world, and then send it. Every snake and powerup object is sent every update. This is more inefficient but also simpler to implement
than only sending data when the object has updated somehow.

DISCONNECT
Disconnects are handled gracefully with an event handler that ensures we set a snake to DCed and send a message that that snake DCed.

HANDSHAKE
Our handshake is similar to that of the client's, but reversed. We send the clientID, worldsize, and the walls, and then added the socket to a dictionary.
We then create a snake for that client. 

PARSING/RECEIVING DATA
This code is extremely self explanatory, and happens each time we receive data from any socket. We always check for socket errors, and always ensure inputs are valid (ie not
allowing snakes to loop into themselves).

WORLD
Our world class, itself, is entirely unchanged from the client, as it has no need to be changed. The snake class has added functionality to handle respawns and growth, and
the wall class now has each endpoint of the wall created when the wall is created. This data is for the server only. Powerups are unchanged.

There are many more small things in this code, but they're all rather self explanatory and can be found via reading the code itself. 

PS8

To be completely honest, we didn't do anything unique for this client. We only did our best to replicate the provided one due to both of
us strongly needing anti-burnout time over break. There aren't any features that you should be aware of; our goal was for it to function
nearly exactly as the given client executable. 

Snek is used as a stand in for the word snake as a variable in most cases.

WORLD
Our World is a small, self contained class that contains a few dictionaries; one for Snakes, one for Walls, and one for Powerups. It also contains
a world size, the ID of the client, and a counter of how many frames have passed. The frame counter was supposed to solely be for random, shifting colors,
but we ran out of time and plan to possibly update the client with it later, so it has been left in. You can reset the world with Reset().

WORLD OBJECTS
The Wall, Powerup, and Snake classes are nothing special, just containers for data. There is just enough to deserialize and access data for the controller.
No actual processing happens in these classes. 

VECTOR2D
Vector2D is completely unchanged. 

GAMECONTROLLER
The GameController is, as expected, the bulk of the work. It contains a world instance, everything we need for networking, events for various events
the view or WorldPanel may need to know about, and other information. 
The networking code is actually pretty intuitive and straightforward. Sending is as simple as serializing a string and sending it to the server with 
some normalization of the process. Receiving is a bit bulkier, but also straightforward; if the handshake isn't established, establish it with the 
ClientID and the World size. After that, we receive any amount of walls, powerups, and snakes, with no order expected or required. It deseralizes, makes
a new object, and adds it to the world (or updates a current object). Furthermore, if we recieve a snake or a powerup with a true died bool, then it deletes
whatever it was from the dictionary. This parsing is done via breaking by newlines and only parsing when we have a full object. No objects sent are lost, 
as incomplete objects will be left in the buffer. We have a custom direction class which just contains which direction we are moving. 
Disconnecting unexpectedly allows you to reconnect by just clicking the connect button. This is done by resetting the controller and world. 

MAINPAGE
The MainPage.xaml.cs file is relatively simple. We use the dispatcher for any errors as well as to invalidate every frame. OnTextChanged and OnTap ensure
the player can always control. 

WORLDPANEL
World panel was where we did all of the drawing. To do this, we passed in the GameController's world, then heavily updated the Draw method 
(using Lab 11 DrawWithTransform methods and delegates) to be able to draw each object in the specified way. This is where all the logic for drawing snakes,
walls, powerups, and names is written as well. This could be considered a second controller just for the GUI. Most of the complexity is just figuring out
where to draw things relative to the player.
