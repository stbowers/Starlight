using System;
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

                // If we're the first player to wait, increment the turn
                if (m_playersWaiting == 1)
                {
                    m_state.Turn++;
                }

                m_waitNextTurn.Reset();
            }

            if (wait)
            {
                m_waitNextTurn.WaitOne();
            }
            else
            {
                lock (m_processTurnLock)
                {
                    m_waitNextTurn.Set();
                    m_playersWaiting = 0;
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

            Console.WriteLine("{0} starting new turn on {1}", data.Empire.Name, m_gameID);

            // return new state
            StreamWriter streamWriter = new StreamWriter(response.OutputStream);
            serializer.Serialize(streamWriter, m_state);
            streamWriter.Flush();
        }

        #endregion
    }
}