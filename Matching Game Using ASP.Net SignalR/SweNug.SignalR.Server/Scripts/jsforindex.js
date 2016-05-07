$(function () {

    var back = new Image();
    back.src = 'https://m1.behance.net/rendition/modules/45018995/disp/e86ace60c0d4931bd3a89abe50b4d267.jpg';
        
    for (var i = 0 ; i <53; i++) {
        if (i!=0 && i % 8 == 0) $(document.body).append('<br/>');
        $(document.body).append('<img id =' + i + ' width = "100px" height = "150px" src ="/Content/cards/' + i + '.png" />');
        $("#"+i).hide();
        
    }
    var cardAtPosition;

    $("#register").show();
    $("#findOpponent").hide();
    $("#waitingForOpponent").hide();
    $("#game").hide();
    $("#findAnotherGame").hide();

    var game = $.connection.game;

    game.client.waitingForOpponent = function (message) {
        $("#information").html("");
        $("#information").html("<strong>Waiting for your partner to play!</strong>");
        $('#debug').append('<li>You need to wait for your partner to play!</li>');
    };
    game.client.waitingForMarkerPlacement = function (message) {
        $("#information").html("");
        $("#information").html("<strong>Your turn!</strong>");
        $('#debug').append('<li>Your turn! Make your move</li>');
    };
    game.client.foundOpponent = function (message, cards) {
        console.log(cards);
        cardAtPosition = cards;
        $("#findAnotherGame").hide();
        $("#waitingForOpponent").hide();
        $("#gameInformation").html("You are playing with: " + message);
        $("#clicks_counter").show();
        $('#debug').append('<li>You are playing against ' + message + '</li>');

        $("#game").html('<div id="information" /><br/>');
        for (var i = 0 ; i < cards.length; i++) {
            if (i != 0 && i % 8 == 0) $('#game').append('<br/>');
            $("#game").append('<img id = "' + i + '" class="card" width = "100px" height = "150px" src ="https://m1.behance.net/rendition/modules/45018995/disp/e86ace60c0d4931bd3a89abe50b4d267.jpg" />');
            
        }

        $("#game").show();
    };
    game.client.noOpponents = function (message) {
        $('#debug').append('<li>Waiting for an opponent to connect...</li>');
    };
  
    game.client.addMarkerPlacement = function (message) {
        $('#debug').append('<li>Move made ​​by ' + message.OpponentName + ' in position ' + message.MarkerPosition + '</li>');
        showcard(message.MarkerPosition);
    };
    game.client.opponentDisconnected = function (message) {
        $("#gameInformation").html("<strong>GAME OVER! The opponent quit the game! </strong>");
        $('#debug').append('<li>His opponent folded. Congratulations, you won!</li>');

        $("#findAnotherGame").show();
        $("#game").hide();
    };
    game.client.registerComplete = function (message) {
        $('#debug').append('<li>Are you ready to play Tic Tac Toe!</li>');
    };
    game.client.gameOver = function (message) {
        $("#gameInformation").html('<strong>End of game! Total Clicks : ' + message + '</strong>');
        $("#clicks_counter").hide();
        $("#clicks_counter").html("Total Number of Clicks : " + 0);
        $("#game").html("");
        $('#debug').append('<li>End of game! The winner is: ' + message + '</li>');
        $("#findAnotherGame").show();
    };
    game.client.refreshAmountOfPlayers = function (message) {
        $("#amountOfGames").html(message.amountOfGames);
        $("#amountOfClients").html(message.amountOfClients);
        $("#totalAmountOfGames").html(message.totalGamesPlayed);
    };
    game.client.cardover = function (pos) {
        $("#"+pos).css("visibility", "hidden");
    }
    game.client.closeallexcept = function (pos1, pos2) {
        for (var i = 0 ; i < cardAtPosition.length ; i++) {
            if (i == pos1 || i == pos2) continue;
            closecard(i);
        }
    }
    game.client.updatecounter = function (count) {
        $("#clicks_counter").html("Total Number of Clicks : "+ count);
    }
    function showcard (pos) {
        $("#" + pos).attr("src", "/Content/cards/" + cardAtPosition[pos] + ".png");
    };
    function closecard(pos) {
        $("#" + pos).attr("src", "https://m1.behance.net/rendition/modules/45018995/disp/e86ace60c0d4931bd3a89abe50b4d267.jpg");
    };
 
  
    $(".card").live("click", function (event) {
        game.server.play(event.target.id);
    });

    
    $("#registerName").click(function () {
        var sizeof_game = ($("select[name='D1'] option:selected").text());
        game.server.registerClient($('#gamerName').val(), sizeof_game);

        $("#register").hide();
        $("#findOpponent").show();
    });

    $(".findGame").click(function () {
        findGame();
    });
    $("#findAnotherGame").click(function () {
        $("#gameInformation").html("");
        $("#game").hide();
        $("#findAnotherGame").hide();
        
        var sizeof_game = ($("select[name='D1'] option:selected").text());
        game.server.registerClient($('#gamerName').val(), sizeof_game);
        findGame();
    });
    function findGame() {
        game.server.findOpponent();
        $("#waitingForOpponent").show();
        $("#register").hide();
        $("#findOpponent").hide();
    }

    $.connection.hub.start().done();
});