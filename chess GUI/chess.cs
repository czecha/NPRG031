using System;
using System.Collections.Generic;
using static System.Console;

class Chess
{
    public Tile[,] board = new Tile[8, 8];
    public Player turn = Player.WHITE;
    public Player winner = Player.NO_ONE;
    public bool check = false;
    Tile lastCapture = null;
    bool lastMoveWasCapture = false; // !! works only for one move backwards

    public Chess() { 
        ResetChessBoard(); 
    }

    public Chess( Tile[,] b )
    {
       for(int i = 0; i < 8; i++)
           for( int j = 0; j < 8; j++)
                if( b[ i,j ] is Piece p)
                    board[ i,j ] = p.GetCopy();
    }

    public void ResetChessBoard()
    {
        // set the tiles to contain initial configuration of chess board
        for (int j = 0; j < 8; j++)
        {
            board[1, j] = new Pawn((1, j), Player.BLACK);
            board[6, j] = new Pawn((6, j), Player.WHITE);
        }

        board[0, 0] = new Rook((0, 0), Player.BLACK);
        board[0, 7] = new Rook((0, 7), Player.BLACK);
        board[7, 0] = new Rook((7, 0), Player.WHITE);
        board[7, 7] = new Rook((7, 7), Player.WHITE);
        board[0, 1] = new Knight((0, 1), Player.BLACK);
        board[0, 6] = new Knight((0, 6), Player.BLACK);
        board[7, 1] = new Knight((7, 1), Player.WHITE);
        board[7, 6] = new Knight((7, 6), Player.WHITE);
        board[0, 2] = new Bishop((0, 2), Player.BLACK);
        board[0, 5] = new Bishop((0, 5), Player.BLACK);
        board[7, 2] = new Bishop((7, 2), Player.WHITE);
        board[7, 5] = new Bishop((7, 5), Player.WHITE);
        board[0, 3] = new Queen((0, 3), Player.BLACK);
        board[7, 3] = new Queen((7, 3), Player.WHITE);
        board[0, 4] = new King((0, 4), Player.BLACK);
        board[7, 4] = new King((7, 4), Player.WHITE);
    }

    public bool MakeLegitMove(Move move)
    {
        // return false if:
        //          a. the game is over
        //          b. (from or to) coordinates are outside of board
        //          c. from is emptyTile
        //          d. if to is not in the set of legitimate moves
        // if it is capture, remember captured piece 
        // make the move 
        // return true
        WriteLine("\nMake move initiated\n");
        if (winner != Player.NO_ONE || OutsideOfBoard(move) || board[move.from.row, move.from.col] == null)
            return false;
        WriteLine("Passed first checks");
        if (!IsLegit(move))
            return false;
        WriteLine("Passed second check");

        if (IsCapture(move)) {
            lastCapture = board[move.to.row, move.to.col];
            lastMoveWasCapture = true;
        } else {
            lastMoveWasCapture = false;
        }

        Move(move);
        
        PawnToQueen(move);

        Player prevturn = turn;
        turn = (turn == Player.WHITE) ? Player.BLACK : Player.WHITE;

        check = IsPlayerInCheck();
        WriteLine("Check status: " + check);
        if ( check && IsCheckMate() )
            winner = prevturn;

        return true;
    }

    bool IsCapture(Move move) => board[move.to.row, move.to.col] == null; 

    void Move(Move move)
    {
        board[move.to.row, move.to.col] = board[move.from.row, move.from.col];
        if (board[move.to.row, move.to.col] is Piece p)
        {
            p.position.row = move.to.row; // update position inside piece , this must be redone in architecture of object
            p.position.col = move.to.col; // position is being tracked in two different places
        }
        board[move.from.row, move.from.col] = null;
    }

    bool IsCheckMate()
    {
        // pseudocode:
        // 1. foreach possible Move of current player
        // 2.     init new instance of chess and make this move 
        // 3.     if the board configuration is not check current player (from now)
        // 4.         return false
        // 5. return true
        //
        // --> if there is no move current player can make to escape check, than it is checkmate
        Chess deepcopy = GetDeepCopy();

        foreach (Move move in GetAllPlayersMoves() )
        {
            deepcopy.Move( move );
            if ( !deepcopy.IsPlayerInCheck() )
                return false;

            deepcopy.Unmove(new Move(move.to, move.from));
        }

        return true;
    }

    List<Move> GetAllPlayersMoves()
    {
        // List of moves
        // for all pieces of inturn player 
        //      add all moves of the piece
        // return list of moves
        List<Move> result = new();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if ( board[i, j] is Piece p && p.owner == turn )
                    foreach ( Move move in p.PossibleMoves(board) )
                        result.Add(move);
        return result;
    }

    Chess GetDeepCopy() 
    {
        // create new instance of chess board
        // set all tiles to null;
        // for all tiles if there is a piece create new instance of it
        // and set it to coresponding board tile
        // copy properties of chess 
        // return chess
        Chess deepcopy = new( board );
        // copy properties:
        deepcopy.turn = turn;
        deepcopy.check = check;
        deepcopy.lastCapture = lastCapture;
        deepcopy.lastMoveWasCapture = lastMoveWasCapture;
        deepcopy.winner = winner;
        return deepcopy;
    }

    public void Unmove( Move move )
    {
        Move(move);
        if(lastMoveWasCapture && lastCapture is Piece p) 
            board[p.position.row, p.position.col] = p;
    }

    bool OutsideOfBoard(Move move)
    {
        if (OutsideOfBoard( move.from ))
            return true;
        if (OutsideOfBoard( move.to ))
            return true;
        return false;
    }

    bool OutsideOfBoard( ( int row, int col ) position ) => position.row < 0 || position.row > 7 || position.col < 0 || position.col > 7;

    bool IsLegit(Move move)
    {
        // called already after checking that move from is not empty and coordinates are inside board and winner is null
        // check if move.to is contains in getLegitMoves(move.from) result
        List<Move> validMoves = GetLegitMoves(move.from);
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

    bool MovesAreEqual(Move a, Move b) =>  a.from == b.from && a.to == b.to ;

    void PawnToQueen(Move move)
    {
        if (board[move.to.row, move.to.col] is Pawn pawn && ((pawn.owner == Player.BLACK && move.to.row == 7) || (pawn.owner == Player.WHITE && move.to.row == 0)))
            board[move.to.row, move.to.col] = new Queen( (move.to.row, move.to.col), pawn.owner);
    }

    bool IsPlayerInCheck()
    {
        // pseudocode: 
        // for all notInTurn's pieces 
        //      for all moves of piece
        //          if the move 'steps' onto inTurn king 
        //              return true
        // return false
        List<Piece> notInTurnPieces = new ();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (board[i, j] is Piece p)
                    if (( turn  == Player.WHITE && p.owner == Player.BLACK) || ( turn == Player.BLACK && p.owner == Player.WHITE))
                        notInTurnPieces.Add(p);

        (int x, int y) = GetKingsCoordinates( turn );

        foreach (Piece p in notInTurnPieces)
            foreach (Move move in p.PossibleMoves(board))
                if (move.to.row == x && move.to.col == y)
                    return true;

        return false;
    }

    (int, int) GetKingsCoordinates(Player player)
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (board[i, j] is King king && player == king.owner)
                    return (i, j);
        WriteLine("Ouch");
        throw new Exception("FATAL ERROR, one of the kings is not on board");
    }

    public List<Move> GetLegitMoves( (int row, int col) from)
    {
        // return empty list if: 
        //          a. game is over (winner is not null)
        //          b. position's coordinate is outside of board
        //          c. board[position] is emptyTile
        // else get the list of legitimate moves
        List<Move> result = new ();
        if (winner != Player.NO_ONE || OutsideOfBoard(from) || board[from.row, from.col] == null)
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
            result = p.PossibleMoves(board);

        WriteLine(result.Count);

        return result;
    }
}