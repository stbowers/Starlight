using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using StarlightNetwork;

namespace StarlightServer
{
    class Server
    {
        RESTServer m_server;
        GameState m_state;
        GameState m_workingState; // A working copy of the state used to merge updates from clients
        int m_gameID;
        bool m_open;
        ManualResetEvent m_waitStart = new ManualResetEvent(false);

        // players waiting for next turn
        int m_playersWaiting = 0;
        object m_processTurnLock = new object();
        ManualResetEvent m_waitNextTurn = new ManualResetEvent(false);

        public Server(RESTServer server, GameState state, int gameID)
        {
            m_server = server;
            m_state = state;
            m_gameID = gameID;
            m_open = true;

            Console.WriteLine("Setting up endpoints for /Servers/{0}", m_gameID);
            server.AddRequestHandler(string.Format("GET:/Servers/{0}", m_gameID), RequestGetState);
            server.AddRequestHandler(string.Format("POST:/Servers/{0}/Join", m_gameID), RequestPostJoin);
            server.AddRequestHandler(string.Format("POST:/Servers/{0}/StartGame", m_gameID), RequestPostStartGame);
            server.AddRequestHandler(string.Format("POST:/Servers/{0}/EndTurn", m_gameID), RequestPostEndTurn);
        }

        public void JoinGame(Empire empire)
        {
            if (m_open)
            {
                // Add the empire
                m_state.Empires.Add(empire);

                // Get a random star to make the empire's start
                Random rng = new Random();
                int quadrant = rng.Next() % 4;
                Quadrant q = m_state.Field.Quadrants[quadrant];
                int starIndexX = rng.Next() % q.Stars.GetLength(0);
                int starIndexY = rng.Next() % q.Stars.GetLength(1);
                while (q.Stars[starIndexX, starIndexY] == null || q.Stars[starIndexX, starIndexY].Owner != null)
                {
                    starIndexX = rng.Next() % q.Stars.GetLength(0);
                    starIndexY = rng.Next() % q.Stars.GetLength(1);
                }
                Star s = q.Stars[starIndexX, starIndexY];
                s.Owner = empire.Name;
                s.Colonized = true;
                Console.WriteLine("Starting {0} in system: {1}", empire.Name, s.Name);
            }

            // Wait for game to start
            m_waitStart.WaitOne();
        }

        public void StartGame()
        {
            m_open = false;
            m_waitStart.Set();
        }

        public void EndTurn(GameState modifiedGameState, Empire empire)
        {
            bool wait = false;
            lock (m_processTurnLock)
            {
                m_playersWaiting++;
                wait = m_playersWaiting < m_state.Empires.Count;

                // If we're the first player to wait, increment the turn and create a new working copy of the state
                if (m_playersWaiting == 1)
                {
                    m_state.Turn++;
                    m_workingState = JsonHelpers.CreateFromJsonString<GameState>(JsonHelpers.SerializeToString(m_state));
                }

                // Apply our changes to the working state
                MergeState(modifiedGameState);

                m_waitNextTurn.Reset();
            }

            if (wait)
            {
                // Wait for turn to process (last player to end turn will merge their changes and then update the state from the working copy)
                m_waitNextTurn.WaitOne();
            }
            else
            {
                lock (m_processTurnLock)
                {
                    // Replace upstream state with working state
                    m_state = m_workingState;

                    // Notify other threads that the turn has finished processing
                    m_waitNextTurn.Set();

                    // Reset playersWaiting count
                    m_playersWaiting = 0;
                }
            }
        }

        /// <summary>
        /// Merges two game states, assuming updatedState is a modified version of the global state.
        /// </summary>
        void MergeState(GameState updatedState)
        {
            // Search through each star system to look for changes
            for (int q = 0; q < 4; q++)
            {
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        Star upstream = m_state.Field.Quadrants[q].Stars[x, y];
                        Star working = m_workingState.Field.Quadrants[q].Stars[x, y];
                        Star merge = updatedState.Field.Quadrants[q].Stars[x, y];

                        // First check if the system exists
                        if (upstream != null && working != null && merge != null)
                        {
                            // Look for changes in:
                            // Ownership
                            if (merge.Owner != upstream.Owner)
                            {
                                if (upstream.Owner != working.Owner)
                                {
                                    Console.WriteLine("Ownership conflict");
                                }
                                else
                                {
                                    working.Owner = merge.Owner;
                                }
                            }

                            // Colonized Status
                            if (merge.Colonized != upstream.Colonized)
                            {
                                if (upstream.Colonized != working.Colonized)
                                {
                                    Console.WriteLine("Colony conflict");
                                }
                                else
                                {
                                    working.Colonized = merge.Colonized;
                                }
                            }

                            // Project
                            if (merge.Project != upstream.Project)
                            {
                                // If the project has already been changed, we have a conflict, otherwise udpate it now
                                if (upstream.Project != working.Project)
                                {
                                    Console.WriteLine("Project conflict! Upstream: {0}, Working: {1}, Merge: {2}", upstream.Project, working.Project, merge.Project);
                                }
                                else
                                {
                                    working.Project = merge.Project;
                                }
                            }

                            // Project time
                            if (merge.ProjectTurnsLeft != upstream.ProjectTurnsLeft)
                            {
                                if (upstream.ProjectTurnsLeft != working.ProjectTurnsLeft)
                                {
                                    Console.WriteLine("Project turns left conflict! Upstream: {0}, Working: {1}, Merge: {2}", upstream.ProjectTurnsLeft, working.ProjectTurnsLeft, merge.ProjectTurnsLeft);
                                }
                                else
                                {
                                    working.ProjectTurnsLeft = merge.ProjectTurnsLeft;
                                }
                            }

                            // Ships
                        }
                    }
                }
            }
        }

        public bool IsOpen()
        {
            return m_open;
        }

        public GameState GetState()
        {
            return m_state;
        }

        #region RequestHandlers

        public void RequestGetState(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Request:
            // GET:/Servers/<id>
            // Response:
            // ---200 OK---
            // Game state json object


            Console.WriteLine("Request for game state {0}", m_gameID);
            JsonSerializer serializer = new JsonSerializer();
            StreamWriter streamWriter = new StreamWriter(response.OutputStream);
            serializer.Serialize(streamWriter, m_state);
            streamWriter.Flush();
        }

        public void RequestPostJoin(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Request:
            // POST:/Servers/<id>/Join
            // Content:
            // Empire json object
            // Response:
            // If game is accepting new players:
            // ---200 OK---, and the empire is added to the game
            // Game state json object
            // Else, the server is no longer accepting new players:
            // ---410 GONE---

            JsonSerializer serializer = new JsonSerializer();
            StreamReader reader = new StreamReader(request.InputStream);
            Empire empire = (Empire)serializer.Deserialize(reader, typeof(Empire));
            Console.WriteLine("Request to join game {0} ({1})", m_gameID, empire.Name);

            // Check if the game is accepting requests to join
            if (IsOpen())
            {
                // Blocks until game starts
                JoinGame(empire);
                Console.WriteLine("{0} is joining game {1}", empire.Name, m_gameID);

                // Return success code and the game state
                response.StatusCode = 200;
                StreamWriter writer = new StreamWriter(response.OutputStream);
                serializer.Serialize(writer, m_state);
                writer.Flush();
            }
            else
            {
                // Return error code
                response.StatusCode = (int)HttpStatusCode.Gone;
            }
        }

        public void RequestPostStartGame(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Request:
            // POST:/Servers/<id>/StartGame
            // Response:
            // If game is accepting new players:
            // ---200 OK---, and the game starts, and no longer accepts new players
            // Game state json object
            // Else, the server is no longer accepting new players:
            // ---410 GONE---

            JsonSerializer serializer = new JsonSerializer();

            // Check if the game is accepting requests to join
            if (IsOpen())
            {
                Console.WriteLine("Starting game {0}", m_gameID);
                StartGame();

                // Return success code and the game state
                response.StatusCode = 200;
                StreamWriter writer = new StreamWriter(response.OutputStream);
                serializer.Serialize(writer, m_state);
                writer.Flush();
            }
            else
            {
                // Return error code
                response.StatusCode = (int)HttpStatusCode.Gone;
            }
        }

        public void RequestPostEndTurn(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Request:
            // POST:/Servers/<id>/EndTurn
            // Content: the modified game state and the empire ending their turn
            // Response:
            // Once all players have ended their turn, respond with the new game state

            JsonSerializer serializer = new JsonSerializer();
            StreamReader streamReader = new StreamReader(request.InputStream);

            NextTurnData data = (NextTurnData)serializer.Deserialize(streamReader, typeof(NextTurnData));

            Console.WriteLine("{0} ending turn on game {1}", data.Empire.Name, m_gameID);

            // submit modified state, which will block until all players have ended their turn
            EndTurn(data.GameState, data.Empire);

            // return new state
            StreamWriter streamWriter = new StreamWriter(response.OutputStream);
            serializer.Serialize(streamWriter, m_state);
            streamWriter.Flush();
        }

        #endregion
    }
}