namespace SweNug.SignalR.Server
{
    public class MatchingGame
    {
        public Client Player1 { get; set; }

        public Client Player2 { get; set; }
        public int[] card_at_position { get; set; }  // The secret map of the cards
        private bool []over= new bool[100];          // tracking which cards have been paired up    
        
        public int previous_clicked_position = -1;   // remember previous clicked position to match to current click
        public int pairedup = 0;                     // no of cards which have been paired up
        public int total_attempted_clicks = 0;       // The parameter to keep track of for the game.
        public int game_size = 0;                    // Total number of cards in the game.

        public MatchingGame()
        {
            for (int i = 0; i < over.Length; i++) over[i] = false;
            total_attempted_clicks = 0;
        }

        //Function tries to match current position click with the previous one.
        public int Match(int player, int position)
        {
            if (over[position] || previous_clicked_position == position) return -1;
            
            total_attempted_clicks++;
            if (previous_clicked_position != -1 && card_at_position[previous_clicked_position] == card_at_position[position])
            {
                over[previous_clicked_position] = true;
                over[position] = true;
                int temp = previous_clicked_position;
                previous_clicked_position = -1;
                pairedup += 2;
                return temp ;
            }
           
            previous_clicked_position = position;
            return -1;
        }
        public bool IsGameOver() {
            return pairedup == game_size;
        }
        public void reset_total_attempted_clicks()
        {
            total_attempted_clicks = 0;
        }
    }
}