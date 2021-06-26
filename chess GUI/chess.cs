using System;
using System.Collections.Generic;
using static System.Console;



class Chess
{
    public Tile[,] board;
    public Player turn;
    public Player winner;
    public bool check;

    public Chess()
    {
        check = false;
        resetChessBoard();
    }

    private Chess(Tile[,] b, Player t)
    {
        // this constructor is private on purpose
        // because its use is to generate possible future move, without making the move on same board
        // for the purpose of checking check-mate condition
        check = false;
        winner = Player.NO_ONE;
        turn = t == Player.WHITE ? Player.WHITE : Player.BLACK;
        board = new Tile[8, 8]; // null is empty tile

        // copy all Tiles , generating new instances:
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (b[i, j] is Piece p)
                    board[i, j] = p.getCopy();
    }

    public void resetChessBoard()
    {
        turn = Player.WHITE;
        winner = Player.NO_ONE;
        board = new Tile[8, 8];

        // set the tiles to contain initial configuration of chess board
        for (int j = 0; j < 8; j++)
        {
            board[1, j] = new Pawn( (1, j) , Player.BLACK);
            board[6, j] = new Pawn( (6, j) , Player.WHITE);
        }

        board[0, 0] = new Rook(   (0, 0), Player.BLACK);
        board[0, 7] = new Rook(   (0, 7), Player.BLACK);
        board[7, 0] = new Rook(   (7, 0), Player.WHITE);
        board[7, 7] = new Rook(   (7, 7), Player.WHITE);
        board[0, 1] = new Knight( (0, 1), Player.BLACK);
        board[0, 6] = new Knight( (0, 6), Player.BLACK);
        board[7, 1] = new Knight( (7, 1), Player.WHITE);
        board[7, 6] = new Knight( (7, 6), Player.WHITE);
        board[0, 2] = new Bishop( (0, 2), Player.BLACK);
        board[0, 5] = new Bishop( (0, 5), Player.BLACK);
        board[7, 2] = new Bishop( (7, 2), Player.WHITE);
        board[7, 5] = new Bishop( (7, 5), Player.WHITE);
        board[0, 3] = new Queen(  (0, 3), Player.BLACK);
        board[7, 3] = new Queen(  (7, 3), Player.WHITE);
        board[0, 4] = new King(   (0, 4), Player.BLACK);
        board[7, 4] = new King(   (7, 4), Player.WHITE);
    }

    public bool makeMove(Move move)
    {
        // return false if:
        //          a. the game is over
        //          b. (from or to) coordinates are outside of board
        //          c. from is emptyTile
        //          d. if to is not in the set of legitimate moves
        // make the move 
        // return true

        WriteLine("\nMake move initiated\n");

        if (winner != Player.NO_ONE || outsideOfBoard(move) || board[move.from.row, move.from.col] == null)
            return false;

        WriteLine("Passed first checks");

        if (!isLegit(move))
            return false;

        WriteLine("Passed second check");

        board[move.to.row, move.to.col] = board[move.from.row, move.from.col];
        if (board[move.to.row, move.to.col] is Piece p)
        {
            p.position.row = move.to.row; // update position inside piece , this must be redone in architecture of object
            p.position.col = move.to.col; // position is being tracked in two different places
        }
        board[move.from.row, move.from.col] = null;
        pawnToQueen(move);

        turn = (turn == Player.WHITE) ? Player.BLACK : Player.WHITE;
        check = isPlayerInCheck(turn) ? true : false;

        if (check)
        {
            // can king escape to not check position?
            // if not, it is game over for not in turn player
            if (kingCannotEscape())
                winner = (turn == Player.WHITE) ? Player.BLACK : Player.WHITE;
        }

        return true;
    }

    bool kingCannotEscape()
    {
        (int x, int y) = getKingsCoordinates(turn);
        King king = (King)board[x, y];
        Player player = turn == Player.WHITE ? Player.WHITE : Player.BLACK;
        // foreach possible move of inTurnKing
        //      check if that move would be not in check 
        //              // if so return false (since you found at least one move, where the king stays alive )
        // return true

        foreach (Move move in king.possibleMoves(board))
        {
            // make the move on new instance of chess
            // look if king is in check
            // if not return false
            Chess copy = new Chess(board, player);
            copy.makeMove(move);
            if (!copy.isPlayerInCheck(player))
            {
                WriteLine("King CAN escape!");
                return false;
            }
        }

        WriteLine("King cannot ESCAPE!");
        return true;
    }


    bool isCapture(Move move)
    {
        int x = move.to.row;
        int y = move.to.col;
        if (board[x, y] == null)
            return false;
        return true;
    }

    bool outsideOfBoard(Move move)
    {
        if (outsideOfBoard( move.from ))
            return true;
        if (outsideOfBoard( move.to ))
            return true;
        return false;
    }

    bool outsideOfBoard( ( int row, int col ) position ) => position.row < 0 || position.row > 7 || position.col < 0 || position.col > 7;

    bool isLegit(Move move)
    {
        // called already after checking that move from is not empty and coordinates are inside board and winner is null
        // check if move.to is contains in getLegitMoves(move.from) result
        List<Move> validMoves = getLegitMoves(move.from);
        WriteLine("Is legit function is assesing move: from: " + move.from.row + " " + move.from.col + " to: " + move.to.row + " " + move.to.col);
        foreach (Move m in validMoves)
        {
            WriteLine("Comparing with possible move: from: " + m.from.row + " " + m.from.col + " to: " + m.to.row + " " + m.to.col);
            if (MovesAreEqual(m, move))
            {
                WriteLine("Is legit is true");
                return true;
            }
        }


        WriteLine("Is legit is false");
        return false;
    }

    bool MovesAreEqual(Move a, Move b)
    {
        if ((a.from.row != b.from.row) || (a.from.col != b.from.col) || (a.to.row != b.to.row) || (a.to.col != b.to.col))
            return false;
        return true;
    }

    void pawnToQueen(Move move)
    {
        if (board[move.to.row, move.to.col] is Pawn pawn && ((pawn.owner == Player.BLACK && move.to.row == 7) || (pawn.owner == Player.WHITE && move.to.row == 0)))
            board[move.to.row, move.to.col] = new Queen( (move.to.row, move.to.col), pawn.owner);
    }

    bool isPlayerInCheck(Player player)
    {
        // pseudocode: 
        // for all notInTurn's pieces 
        //      for all moves of piece
        //          if the move 'steps' onto inTurn king 
        //              return true
        // return false
        List<Piece> notInTurnPieces = new List<Piece>();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (board[i, j] is Piece p)
                    if ((player == Player.WHITE && p.owner == Player.BLACK) || (player == Player.BLACK && p.owner == Player.WHITE))
                        notInTurnPieces.Add(p);

        (int x, int y) = getKingsCoordinates(player);

        foreach (Piece p in notInTurnPieces)
            foreach (Move move in p.possibleMoves(board))
                if (move.to.row == x && move.to.col == y)
                    return true;

        return false;
    }

    (int, int) getKingsCoordinates(Player player)
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (board[i, j] is King king)
                    if ((player == Player.WHITE && king.owner == Player.BLACK) || (player == Player.BLACK && king.owner == Player.WHITE))
                        return (i, j);
        throw new Exception("FATAL ERROR, one of the kings is not on board");
    }

    public List<Move> getLegitMoves( (int row, int col) from)
    {
        // return empty list if: 
        //          a. game is over (winner is not null)
        //          b. position's coordinate is outside of board
        //          c. board[position] is emptyTile
        // else get the list of legitimate moves
        List<Move> result = new List<Move>();
        if (winner != Player.NO_ONE || outsideOfBoard(from) || board[from.row, from.col] == null)
            return result;

        WriteLine("GetLegitMoves 1");

        // in case user requested move of player the doesnt have the turn to move:
        if (board[from.row, from.col] is Piece pp)
        {
            if (turn != pp.owner)
                return result;
        }

        WriteLine("GetLegitMoves 2");

        WriteLine(board[from.row, from.col]);

        if (board[from.row, from.col] is Piece p)
            result = p.possibleMoves(board);

        WriteLine(result.Count);

        return result;
    }
}