using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using StarlightGame.GameCore;
using StarlightNetwork;

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

            // Update the player empire
            m_gameState.PlayerEmpire = empire;
        }

        public void StartGame()
        {
            // Send POST request to /Servers/<id>/StartGame
            Task<HttpResponseMessage> response = RESTHelpers.PostAsync(m_serverURL + "/Servers/" + m_gameID + "/StartGame", new StringContent(""));

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

        public int GameID
        {
            get
            {
                return m_gameID;
            }
        }
    }
}