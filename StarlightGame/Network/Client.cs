using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using StarlightEngine.Events;
using StarlightGame.GameCore;
using StarlightNetwork;
using Newtonsoft.Json;

namespace StarlightGame.Network
{
    class Client
    {
        string m_serverURL;
        int m_gameID;
        GameState m_gameState;
        static Client m_staticClient;

        public static Client StaticClient
        {
            get
            {
                return m_staticClient;
            }
        }

        public Client(string serverURL, GameState gameState)
        {
            m_serverURL = serverURL;
            m_gameState = gameState;
            PostNewGame();
            m_staticClient = this;
        }

        public Client(string serverURL, int gameID)
        {
            m_serverURL = serverURL;
            m_gameID = gameID;
            GetNewGame();
            m_staticClient = this;
        }

        void PostNewGame()
        {
            // Post game state to server
            Task<HttpResponseMessage> response = RESTHelpers.PostJSONObjectAsync(m_serverURL + "/Servers", m_gameState);
            response.Wait();

            if (response.Result.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Network error");
            }

            // Read game ID
            Task<System.IO.Stream> responseStream = response.Result.Content.ReadAsStreamAsync();
            responseStream.Wait();
            m_gameID = JsonHelpers.CreateFromJsonStream<int>(responseStream.Result);
        }

        void GetNewGame()
        {
            // Get game from server
            Task<GameState> gameState = RESTHelpers.GetJSONObjectAsync<GameState>(m_serverURL + "/Servers/" + m_gameID);
            gameState.Wait();
            m_gameState = gameState.Result;
            GameState.State = m_gameState;
        }

        public void JoinGame(Empire empire)
        {
            // Update the player empire
            m_gameState.PlayerEmpire = empire;

            // Send POST request to /Servers/<id>/Join with the empire as json
            Task<HttpResponseMessage> response = RESTHelpers.PostJSONObjectAsync(m_serverURL + "/Servers/" + m_gameID + "/Join", empire);
            response.Wait();

            if (response.Result.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Network error");
            }

            // Response is the game state
            Task<System.IO.Stream> responseStream = response.Result.Content.ReadAsStreamAsync();
            responseStream.Wait();

            // Update our own game state
            m_gameState.UpdateFromServer(JsonHelpers.CreateFromJsonStream<GameState>(responseStream.Result));
        }

        public void StartGame()
        {
            // Send POST request to /Servers/<id>/StartGame
            Task<HttpResponseMessage> response = RESTHelpers.PostAsync(m_serverURL + "/Servers/" + m_gameID + "/StartGame", new StringContent(""));
            response.Wait();

            if (response.Result.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Can't start game");
            }

            // Response is the game state
            Task<System.IO.Stream> responseStream = response.Result.Content.ReadAsStreamAsync();
            responseStream.Wait();

            // Update our own game state
            m_gameState.UpdateFromServer(JsonHelpers.CreateFromJsonStream<GameState>(responseStream.Result));
        }

        public void NextTurn()
        {
            Console.WriteLine("Next turn for {0}", m_gameState.PlayerEmpire.Name);
            // Send POST request to /Servers/<id>/EndTurn with modified game state and empire
            string data = string.Format("{{\"GameState\":{0},\"Empire\":{1}}}", JsonHelpers.SerializeToString(m_gameState), JsonHelpers.SerializeToString(m_gameState.PlayerEmpire));
            Task<HttpResponseMessage> response = RESTHelpers.PostStringAsync(m_serverURL + "/Servers/" + m_gameID + "/EndTurn", data, Encoding.UTF8, "application/json");

            // Blocks until the all players have ended their turn, and then the server will respond with the new game state
            response.Wait();

            if (response.Result.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error ending turn");
            }

            // Response is the game state
            Task<System.IO.Stream> responseStream = response.Result.Content.ReadAsStreamAsync();
            responseStream.Wait();

            // Update our own game state
            m_gameState.UpdateFromServer(JsonHelpers.CreateFromJsonStream<GameState>(responseStream.Result));
            m_gameState.ProcessTurn();

            Console.WriteLine("Sending next turn event");

            // Notify next turn event
            EventManager.StaticEventManager.Notify(GameEvent.NextTurnID, this, null, .1f);
        }

        public int GameID
        {
            get
            {
                return m_gameID;
            }
        }
    }
}