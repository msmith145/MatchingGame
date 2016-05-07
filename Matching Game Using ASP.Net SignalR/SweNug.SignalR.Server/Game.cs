using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SweNug.SignalR.Server
{
    public class Game : Hub
    {
        private static object _syncRoot = new object();
        private static int _gamesPlayed = 0;
        /// <summary>
        /// The list of clients is used to keep track of registered clients and clients that are looking for games
        /// The client will be removed from this list as soon as the client is in a game or has left the game
        /// </summary>
        private static readonly List<Client> clients = new List<Client>();

        /// <summary>
        /// This list is used to keep track of games and their states
        /// </summary>
        private static readonly List<MatchingGame> games = new List<MatchingGame>();

        /// <summary>
        /// Used for fair dice rolls
        /// </summary>
        private static readonly Random random = new Random();

        /// <summary>
        /// When a client disconnects remove the game and announce a walk-over if there's a game in place then the client is removed from the clients and game list
        /// </summary>
        /// <returns>If the operation takes long, run it asynchronously and return the task in which it runs</returns>
        public override Task OnDisconnected()
        {
            var gameInstance = games.FirstOrDefault(x => x.Player1.ConnectionId == Context.ConnectionId || x.Player2.ConnectionId == Context.ConnectionId);
            if (gameInstance == null)
            {
                // Client without game?
                var clientWithoutGame = clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (clientWithoutGame != null)
                {
                    clients.Remove(clientWithoutGame);

                    SendStatsUpdateToAllClients();
                }
                return null;
            }

            if (gameInstance != null)
            {
                games.Remove(gameInstance);
            }

            var client = gameInstance.Player1.ConnectionId == Context.ConnectionId ? gameInstance.Player1 : gameInstance.Player2;

            if (client == null) return null;

            clients.Remove(client);
            if (client.Opponent != null)
            {
                SendStatsUpdateToAllClients();
                return Clients.Client(client.Opponent.ConnectionId).opponentDisconnected(client.Name);
            }
            return null;
        }

        public override Task OnConnected()
        {
            return SendStatsUpdateToAllClients();
        }

        public Task SendStatsUpdateToAllClients()
        {
            return Clients.All.refreshAmountOfPlayers(new { totalGamesPlayed = _gamesPlayed, amountOfGames = games.Count, amountOfClients = clients.Count });
        }
        /// <summary>
        /// registering a new client will add the client to the current list of clients and save the connection id which will be used to communicate with the client
        /// </summary>
        public void RegisterClient(string data, string sizeof_game)
        {
            lock (_syncRoot)
            {
                var client = clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (client == null)
                {
                    client = new Client { ConnectionId = Context.ConnectionId, Name = data, size_of_game = int.Parse(sizeof_game) };
                    clients.Add(client);
                }

                client.IsPlaying = false;
            }

            SendStatsUpdateToAllClients();
            Clients.Client(Context.ConnectionId).registerComplete();
        }

        /// <summary>
        /// Play a marker at a given positon
        /// </summary>
        /// <param name="position">The position which card has been clicked</param>
        public void Play(int position)
        {
            // Find the game where there is a player1 and player2 and either of them have the current connection id
            var game = games.FirstOrDefault(x => x.Player1.ConnectionId == Context.ConnectionId || x.Player2.ConnectionId == Context.ConnectionId);

            if (game == null || game.IsGameOver()) return;

            if (position == game.previous_clicked_position) return;
            int marker = 0;

            // Detect if the player connected is player 1 or player 2
            if (game.Player2.ConnectionId == Context.ConnectionId)
            {
                marker = 1;
            }
            var player = marker == 0 ? game.Player1 : game.Player2;

            // If the player is waiting for the opponent but still tried to make a move, just return
            if (player.WaitingForMove) return;
            
            // Notify both players that a marker has been placed
            Clients.Client(game.Player1.ConnectionId).addMarkerPlacement(new GameInformationPacket { OpponentName = player.Name, MarkerPosition = position });
            Clients.Client(game.Player2.ConnectionId).addMarkerPlacement(new GameInformationPacket { OpponentName = player.Name, MarkerPosition = position });

            //If pairing is right then we need to send the message to clients otherwise need to send them message that no 
            //pairing has happened.
            int previous_position = game.previous_clicked_position;
            int paired_position = game.Match(marker, position);
            if (paired_position != -1)
            {
                Clients.Client(game.Player1.ConnectionId).cardover(position);
                Clients.Client(game.Player2.ConnectionId).cardover(position);
                Clients.Client(game.Player1.ConnectionId).cardover(paired_position);
                Clients.Client(game.Player2.ConnectionId).cardover(paired_position);
            }
            else {
                Clients.Client(game.Player1.ConnectionId).closeallexcept(position, previous_position );
                Clients.Client(game.Player2.ConnectionId).closeallexcept(position, previous_position);
            }

            // If game is over let the clients know and update games list
            if (game.IsGameOver())
            {
                games.Remove(game);
                _gamesPlayed += 1;
                Clients.Client(game.Player1.ConnectionId).gameOver(game.total_attempted_clicks);
                Clients.Client(game.Player2.ConnectionId).gameOver(game.total_attempted_clicks);
                game.reset_total_attempted_clicks();
            }
            else  // swap roles
            {
                player.WaitingForMove = !player.WaitingForMove;
                player.Opponent.WaitingForMove = !player.Opponent.WaitingForMove;

                Clients.Client(player.Opponent.ConnectionId).waitingForMarkerPlacement(player.Name);
                Clients.Client(player.ConnectionId).waitingForOpponent(player.Opponent.Name);
            }
            Clients.Client(game.Player1.ConnectionId).updatecounter(game.total_attempted_clicks);
            Clients.Client(game.Player2.ConnectionId).updatecounter(game.total_attempted_clicks);
            SendStatsUpdateToAllClients();
            
        }

        /// <summary>
        /// Mark the client as lookiner for opponent. This will use the current connection id to identify the current client and mark it as ready for battle.
        /// Once two clients are looking for opponents these two will be matched together and a fair dice roll for whos turn it is will be done.
        /// </summary>
        public void FindOpponent()
        {
            var player = clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null) return;
            player.LookingForOpponent = true;

            // Look for a random opponent if there's more than one looking for a game
            var opponent = clients.Where(x => x.ConnectionId != Context.ConnectionId && 
                                         x.size_of_game == player.size_of_game && 
                                         x.LookingForOpponent && 
                                         !x.IsPlaying).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            if (opponent == null)
            {
                Clients.Client(Context.ConnectionId).noOpponents();
                return;
            }

            // Set both players as busy
            player.IsPlaying = true;
            player.LookingForOpponent = false;
            
            opponent.IsPlaying = true;
            opponent.LookingForOpponent = false;
            
            player.Opponent = opponent;
            opponent.Opponent = player;
            
            
            var randomNumGenerator = new Random();
            // print random integer >= 0 and  < 53
            bool[]picked = new bool[53];
            for (int i = 0; i < 53; i++) picked[i] = false;
            int[] shuffled_cards = new int[player.size_of_game];
            int cards_generated = 0;
            while (cards_generated < player.size_of_game)
            {
                int cur = randomNumGenerator.Next(53);
                if (picked[cur]) continue;
                shuffled_cards[cards_generated++] = cur;
                shuffled_cards[cards_generated++] = cur;
                picked[cur] = true;
            }
            shuffled_cards = shuffled_cards.OrderBy(x =>randomNumGenerator.Next()).ToArray();

            // Notify both players that a game was found
            Clients.Client(Context.ConnectionId).foundOpponent(opponent.Name, shuffled_cards);
            Clients.Client(opponent.ConnectionId).foundOpponent(player.Name,  shuffled_cards);

            // Fair dice roll
            if (random.Next(0, 5000) % 2 == 0)
            {
                player.WaitingForMove = false;
                opponent.WaitingForMove = true;

                Clients.Client(player.ConnectionId).waitingForMarkerPlacement(opponent.Name);
                Clients.Client(opponent.ConnectionId).waitingForOpponent(opponent.Name);
            }
            else
            {
                player.WaitingForMove = true;
                opponent.WaitingForMove = false;

                Clients.Client(opponent.ConnectionId).waitingForMarkerPlacement(opponent.Name);
                Clients.Client(player.ConnectionId).waitingForOpponent(opponent.Name);
            }

            lock (_syncRoot)
            {
                games.Add(new MatchingGame { Player1 = player, Player2 = opponent, card_at_position = shuffled_cards, game_size = player.size_of_game });
            }

            SendStatsUpdateToAllClients();
        }
    }
}