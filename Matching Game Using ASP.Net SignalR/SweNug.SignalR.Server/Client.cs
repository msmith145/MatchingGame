using System;
using System.Linq;

namespace SweNug.SignalR.Server
{
    public class Client
    {
        public string Name { get; set; }
        public Client Opponent { get; set; }
        public bool IsPlaying { get; set; }
        public bool WaitingForMove { get; set; }
        public bool LookingForOpponent { get; set; }
        public string Simbolo { get; set; }
        public int size_of_game { get; set; }
        public string ConnectionId { get; set; }
    }
}
