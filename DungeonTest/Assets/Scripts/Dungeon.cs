using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Dungeon : MonoBehaviour
{
    private const int roomCountCenter = 6;
    private const int roomCountRange = 4;

    private const int gridSize = 4;
    private const int maxRoomCount = gridSize * gridSize;

    private const int numRndConn = 4;

    private enum RoomType
    {
        RT_NONE,
        RT_EMPTY,
        RT_CORRIDOR,
        RT_ROOM
    }

    private class RoomData
    {
        public RoomType type;

        public bool startingPos;
        public bool hasTeleport;

        // Directions: 0 UP, 1 RIGHT, 3 DOWN, 4 LEFT.
        public bool[] conn = new bool[4];

        public bool IsConnected()
        {
            return (conn[0] || conn[1] || conn[2] || conn[3]);
        }

        public void Clear()
        {
            type = RoomType.RT_EMPTY;
            System.Array.Clear(conn, 0, conn.Length);
        }
    }

    private RoomData[,] roomMap = new RoomData[gridSize, gridSize];

    // This thing uses normal distribution to get the number or rooms.
    // I suspect Azure Dreams does something similar (https://en.wikipedia.org/wiki/Normal_distribution).
    public int GetRandomNumberOfRooms()
    {
        double mean = roomCountCenter;
        double sigma = roomCountRange;

        int numberOfRooms = 0;
        while (numberOfRooms < mean - sigma || numberOfRooms > mean + sigma)
        {
            numberOfRooms = (int)(mean + sigma * System.Math.Sqrt(-2.0 * System.Math.Log(1.0 - Random.value)) * System.Math.Sin(2.0 * System.Math.PI * 1.0 - Random.value));
        }

        return numberOfRooms;
    }

    public bool AreAllRoomsConnected()
    {
        bool connected = true;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (roomMap[i, j].type == RoomType.RT_ROOM)
                {
                    if (roomMap[i, j].IsConnected() == false)
                    {
                        connected = false;
                        break;
                    }
                }
            }
        }

        return connected;
    }

    public bool IsValidDirection(int x, int y, int connDir)
    {
        // Check there's other room in that direction.
        if (connDir == 0)
        {
            if (y > 0)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        if (roomMap[i, j].type == RoomType.RT_ROOM)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        else if (connDir == 1)
        {
            if (x + 1 < gridSize)
            {
                for (int i = x + 1; i < gridSize; i++)
                {
                    for (int j = 0; j < gridSize; j++)
                    {
                        if (roomMap[i, j].type == RoomType.RT_ROOM)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        else if (connDir == 2)
        {
            if (y + 1 < gridSize)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    for (int j = y + 1; j < gridSize; j++)
                    {
                        if (roomMap[i, j].type == RoomType.RT_ROOM)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        else if (connDir == 3)
        {
            if (x > 0)
            {
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < gridSize; j++)
                    {
                        if (roomMap[i, j].type == RoomType.RT_ROOM)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public bool HasAvailableConnections(int x, int y)
    {
        return ((roomMap[x, y].conn[0] == false && IsValidDirection(x, y, 0)) ||
            (roomMap[x, y].conn[1] == false && IsValidDirection(x, y, 1)) ||
            (roomMap[x, y].conn[2] == false && IsValidDirection(x, y, 2)) ||
            (roomMap[x, y].conn[3] == false && IsValidDirection(x, y, 3)));
    }

    private void CalculateValidPath(int posX, int posY)
    {
        int connDir;

        // Repeat while there's an available connection to be made.
        while (HasAvailableConnections(posX, posY) == true)
        {
            // Pick a valid direction to connect.
            do
            {
                connDir = Random.Range(0, 4);
            } while (roomMap[posX, posY].conn[connDir] == true || IsValidDirection(posX, posY, connDir) == false);

            // Update connections between rooms.
            roomMap[posX, posY].conn[connDir] = true;

            if (connDir == 0) // up.
            {
                posY--;
                roomMap[posX, posY].conn[2] = true;
            }
            else if (connDir == 1) // right.
            {
                posX++;
                roomMap[posX, posY].conn[3] = true;
            }
            else if (connDir == 2) // down.
            {
                posY++;
                roomMap[posX, posY].conn[0] = true;
            }
            else if (connDir == 3) // left.
            {
                posX--;
                roomMap[posX, posY].conn[1] = true;
            }

            // Update room state.
            if (roomMap[posX, posY].type == RoomType.RT_EMPTY)
            {
                roomMap[posX, posY].type = RoomType.RT_CORRIDOR;
            }
        }
    }

    // Use this for initialization
    private void Start()
    {
        // Get random number of rooms for this level.
        int numberOfRooms = GetRandomNumberOfRooms();

        Debug.Log("Number of Rooms: " + numberOfRooms);

        // Init room map.
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                roomMap[i, j] = new RoomData();
                roomMap[i, j].Clear();
            }
        }

        // Create the rooms.
        int posX, posY;

        for (int i = 0; i < numberOfRooms; i++)
        {
            do
            {
                posX = Random.Range(0, gridSize);
                posY = Random.Range(0, gridSize);
            } while (roomMap[posX, posY].type != RoomType.RT_EMPTY);

            roomMap[posX, posY].type = RoomType.RT_ROOM;
        }

        // Generate connection graph.
        while (AreAllRoomsConnected() == false) // Until all rooms are connected...
        {
            // Get a random unconnected room.
            do
            {
                posX = Random.Range(0, gridSize);
                posY = Random.Range(0, gridSize);
            } while (roomMap[posX, posY].type != RoomType.RT_ROOM || roomMap[posX, posY].IsConnected() == true);

            CalculateValidPath(posX, posY);
        }

        // Add some additional random connections.
        //for (int i = Random.Range(0, numRndConn + 1); i > 0; i--)
        //{
        //    // Get a random room/corridor who has available connections.
        //    posX = Random.Range(0, gridSize);
        //    posY = Random.Range(0, gridSize);

        //    if ((roomMap[posX, posY].type == RoomType.RT_ROOM || roomMap[posX, posY].type == RoomType.RT_CORRIDOR)
        //        && HasAvailableConnections(posX, posY) == true)
        //    {
        //        CalculateValidPath(posX, posY);
        //    }
        //}

        // Select random initial position.
        do
        {
            posX = Random.Range(0, gridSize);
            posY = Random.Range(0, gridSize);
        } while (roomMap[posX, posY].type != RoomType.RT_ROOM);

        roomMap[posX, posY].startingPos = true;

        // Select random teleport position.
        do
        {
            posX = Random.Range(0, gridSize);
            posY = Random.Range(0, gridSize);
        } while (roomMap[posX, posY].type != RoomType.RT_ROOM);

        roomMap[posX, posY].hasTeleport = true;

        Debug.Log("Dungeon generation finished");

        // Draw the room map.
        string[,] asciiMap = new string[gridSize * 3, gridSize * 3];

        for (int i = 0; i < gridSize * 3; i++)
        {
            for (int j = 0; j < gridSize * 3; j++)
            {
                asciiMap[i, j] = "·";
            }
        }

        int mapX, mapY;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                mapX = i * 3 + 1;
                mapY = j * 3 + 1;

                //Draw up.
                if (roomMap[i, j].conn[0] == true)
                {
                    asciiMap[mapX, mapY - 1] = "#";
                }
                else
                {
                    asciiMap[mapX, mapY - 1] = "·";
                }

                //Draw right.
                if (roomMap[i, j].conn[1] == true)
                {
                    asciiMap[mapX + 1, mapY] = "#";
                }
                else
                {
                    asciiMap[mapX + 1, mapY] = "·";
                }

                //Draw down.
                if (roomMap[i, j].conn[2] == true)
                {
                    asciiMap[mapX, mapY + 1] = "#";
                }
                else
                {
                    asciiMap[mapX, mapY + 1] = "·";
                }

                //Draw left.
                if (roomMap[i, j].conn[3] == true)
                {
                    asciiMap[mapX - 1, mapY] = "#";
                }
                else
                {
                    asciiMap[mapX - 1, mapY] = "·";
                }

                // Draw center.
                if (roomMap[i, j].startingPos && roomMap[i, j].hasTeleport)
                {
                    asciiMap[mapX, mapY] = "&";
                }
                else if (roomMap[i, j].startingPos)
                {
                    asciiMap[mapX, mapY] = "@";
                }
                else if (roomMap[i, j].hasTeleport)
                {
                    asciiMap[mapX, mapY] = "T";
                }
                else if (roomMap[i, j].type == RoomType.RT_ROOM)
                {
                    asciiMap[mapX, mapY] = "▓";
                }
                else if (roomMap[i, j].type == RoomType.RT_CORRIDOR)
                {
                    asciiMap[mapX, mapY] = "#";
                }
                else
                {
                    asciiMap[mapX, mapY] = "·";
                }
            }
        }

        // Draw the entire string.
        string output = string.Empty;
        for (int i = 0; i < gridSize * 3; i++)
        {
            for (int j = 0; j < gridSize * 3; j++)
            {
                output += asciiMap[i, j];
            }
            output += "\n";
        }

        Debug.Log(output);
    }

    // Update is called once per frame
    private void Update()
    {
    }
}
