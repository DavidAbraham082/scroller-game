﻿using System.Text;

class Program_copy
{
    private const bool DEV_MODE = false;
    private static char m_playerCharacter = '\u2588';
    private static char m_obstacleCharacter = '\u2580';
    private static char[] m_playerLine = new[] {' ', ' ', ' ', ' ', ' ', ' ', ' '};
    private static int m_playerPosition = 3;

    private static ConsoleKeyInfo m_Key = new();
    private static int m_RefreshRate = 16; // ~60fps
    private static bool m_GameEnded;
    private static int m_height = 15;
    private static int m_width = 7;
    private static char[,] m_gameWorld = new char[15,7]
    {
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '},
        {' ', ' ', ' ', ' ', ' ', ' ', ' '}
    };
    private static List<bool[]> m_obstacleCharacterInputTape = new();
    private static int m_obstacleCharacterInputTapeReadHead = 0;
    private static bool[] m_obstacleCharacterReadLine = new bool[7];

    private static List<Obstacle> m_obstacles = new();
    private static bool m_collision;

    private static void Main2(string[] args)
    {
        int currentGameTick = 0;
        m_obstacleCharacterInputTape = GenerateGameWorld();

        Thread watchKeyThread = new(WatchKeys);
        Thread gameThread = new(GameLoop);

        Console.Clear();
        watchKeyThread.Start();
        gameThread.Start();

        void WatchKeys()
        {
            while (m_Key.Key != ConsoleKey.Q && !m_GameEnded)
            {
                m_Key = Console.ReadKey(true);
            }
        }

        void GameLoop()
        {
            while (m_Key.Key != ConsoleKey.Q && !m_GameEnded)
            {
                Console.WriteLine($"Key: {m_Key.Key} | ticks: {currentGameTick} | ReadHeadValue: {m_obstacleCharacterInputTapeReadHead}");

                if (m_Key.Key == ConsoleKey.H && m_playerPosition != 0) {
                    m_playerLine[m_playerPosition] = ' ';
                    m_playerPosition--;
                }
                else if (m_Key.Key == ConsoleKey.L && m_playerPosition != 6)
                {
                    m_playerLine[m_playerPosition] = ' ';
                    m_playerPosition++;
                }
                m_Key = new();

                // Obstacle Insertion
                if (ShouldUpdateGameWorld(currentGameTick) 
                        && m_obstacleCharacterInputTapeReadHead != m_obstacleCharacterInputTape.Count)
                {
                    m_obstacleCharacterReadLine = m_obstacleCharacterInputTape[m_obstacleCharacterInputTapeReadHead];
                    for (int i = 0; i < m_obstacleCharacterReadLine.Length; i++)
                    {
                        if (m_obstacleCharacterReadLine[i])
                        {
                            m_obstacles.Add(new Obstacle(i));
                        }
                    }

                    if (m_obstacleCharacterInputTape.Count > 0)
                    {
                        m_obstacleCharacterInputTapeReadHead++;
                    }
                }

                // Obstacle management
                if (ShouldUpdateGameWorld(currentGameTick))
                {
                    for (int i = 0; i < m_obstacles.Count; i++) 
                    {
                        if (m_obstacles[i].m_firstSpawn)
                        {
                            m_obstacles[i].m_firstSpawn = false;
                            m_obstacles[i].m_yPosition++;
                            m_gameWorld[m_obstacles[i].m_yPosition, m_obstacles[i].m_xPosition] = m_obstacleCharacter;
                        }
                        else if (!m_obstacles[i].m_firstSpawn 
                                && m_obstacles[i].m_yPosition == m_height - 1)
                        {
                            m_gameWorld[m_obstacles[i].m_yPosition ,m_obstacles[i].m_xPosition] = ' ';
                            m_obstacles.Remove(m_obstacles[i]);
                        }
                        else if (!m_obstacles[i].m_firstSpawn)
                        {
                            m_obstacles[i].m_yPosition++;
                            m_gameWorld[m_obstacles[i].m_yPosition, m_obstacles[i].m_xPosition] = m_obstacleCharacter;
                            m_gameWorld[m_obstacles[i].m_yPosition - 1, m_obstacles[i].m_xPosition] = ' ';
                        }
                    }
                }


                // Gameworld Drawing Logic
                StringBuilder builder = new();
                for (int i = 0; i < m_height; i++)
                {
                    builder.Append('|');
                    for (int j = 0; j < m_width; j++)
                    {
                        builder.Append(m_gameWorld[i,j]);
                    }
                    builder.Append("|");
                    Console.WriteLine(builder);
                    builder.Clear();
                }

                m_playerLine[m_playerPosition] = m_playerCharacter;
                Console.WriteLine("|-------|");
                Console.WriteLine("|" + string.Concat(m_playerLine)+ "|");

                currentGameTick++;

                // Game End State
                if (ShouldUpdateGameWorld(currentGameTick) 
                        && m_obstacles.Count > 0
                        && m_obstacles.First().m_yPosition == m_height - 1
                        && m_playerPosition == m_obstacles.First().m_xPosition) 
                {
                    Console.WriteLine("Collision Occurred!");
                    m_GameEnded = true;
                }

                    if (m_obstacleCharacterInputTapeReadHead == 240) // kinda bung logic but eh... 
                {
                    Console.WriteLine("Game Over! You Win!");
                }
                Thread.Sleep(m_RefreshRate);
                Console.Clear();
            }
            m_GameEnded = true;
        }

        bool ShouldUpdateGameWorld(int currentTick)
        {
            return currentTick % 5 == 0;
        }

    }

    class Obstacle
    {
        public int m_yPosition { get; set; }
        public int m_xPosition { get; set; }
        public bool m_firstSpawn { get; set; }

        public Obstacle(int xPosition)
        {
            m_xPosition = xPosition;
            m_yPosition = -1;
            m_firstSpawn = true;
        }
    }

    static List<bool[]> GenerateGameWorld()
    {
        Random rng = new Random();
        List<bool[]> gameWorld = new List<bool[]>();

        int obstacleCooldown = 0;

        // 240 to set the max game time to 1 minute
        for (int i = 0; i < 240; i++)
        {
            bool[] row = new bool[7];
            if (obstacleCooldown <= 0)
            {
                int obstacleCount = rng.Next(1, 4); // Up to 3 obstacles per line
                for (int j = 0; j < obstacleCount; j++)
                {
                    int obstaclePos;
                    do
                    {
                        obstaclePos = rng.Next(7);
                    } while (row[obstaclePos]); // Ensure we don't overwrite an existing obstacle

                    row[obstaclePos] = true;
                }
                obstacleCooldown = 3;
            }
            else
            {
                obstacleCooldown--;
            }

            gameWorld.Add(row);
        }

        return gameWorld;
    }
}
