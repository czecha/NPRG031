using System;
using System.Collections.Generic;
using static System.Console;

/*
 Known bugs:

 Bug 1
    white epawn to col 4
    black dpawn to col 3
    white fbishop to row 3, col 1

    the bug is black knight jumps to defend king by itself also leaving black to play
    
 Bug 2
    white dpawn to col 4
    black epan to col 3
    white dpan to col 3

    the bug is white pawn captures the black pawn by this move, which is obviously not correct
    additionaly all pawns always capture in this manner
 */

class Chess
{
    public Tile[,] board = new Tile[8, 8];
    public Player turn = Player.WHITE;
    public Player winner = Player.NO_ONE;
    public bool check = false;
    Tile lastCapture = null;
    bool lastMoveWasCapture = false; // !! works only for one move backwards
    CastlingMemory castlingMemory = new();

    public Chess() {
        ResetChessBoard(); 
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
        if (!IsMoveLegit(move))
            return false;

        if (IsCapture(move))
        {
            lastCapture = board[move.to.row, move.to.col];
            lastMoveWasCapture = true;
        }
        else
        {
            lastMoveWasCapture = false;
        }

        Move(move);
        MaintainCastlingMemory(move);
        RookCastling(move);
        PawnToQueen(move);

        Player prevturn = turn;
        turn = (turn == Player.WHITE) ? Player.BLACK : Player.WHITE;

        check = IsPlayerInCheck();
        WriteLine("Check status: " + check);
        if (check && IsCheckMate())
            winner = prevturn;

        return true;
    }

    bool IsMoveLegit(Move move) {
        if (winner != Player.NO_ONE || OutsideOfBoard(move) || board[move.from.row, move.from.col] == null)
            return false;
        WriteLine("Passed first checks");
        if (!IsLegit(move))
            return false;
        WriteLine("Passed second check");
        return true;
    }

    void MaintainCastlingMemory(Move move)
    {
        // set true to castling memories if rooks or kings move 
        int toRow = move.to.row;
        int toCol = move.to.col;
        if(board[ toRow, toCol ] is King k)
        {
            if ( k.owner == Player.BLACK)
                castlingMemory.BlackKingMoved = true;
            if ( k.owner == Player.WHITE)
                castlingMemory.WhiteKingMoved = true;
        }

        if(board[toRow, toCol] is Rook r)
        {
            if(r.owner == Player.BLACK && move.from.row == 0)
            {
                if ( move.from.col == 0 )
                    castlingMemory.Black_A_RookMoved = true;
                if ( move.from.col == 7 )
                    castlingMemory.Black_H_RookMoved = true;
            }

            if(r.owner == Player.WHITE && move.from.row == 7)
            {
                if ( move.from.col == 0 )
                    castlingMemory.White_A_RookMoved = true;
                if ( move.from.col == 7 )
                    castlingMemory.White_H_RookMoved = true;
            }
        }
    }

    void RookCastling(Move move)
    {
        // if the move is castling, also make the second move of the rook
        int toRow = move.to.row;
        int toCol = move.to.col;
        int fromRow = move.from.row;
        int fromCol = move.from.col;

        if(board[toRow, toCol] is King k)
        {
            if(toRow == 7 && fromRow == 7 && fromCol == 4 && k.owner == Player.WHITE)
            {
                if (toCol == 6) // it is whites short castling
                    Move(new Move((7, 7), (7, 5)));
                if (toCol == 2) // it is whites long castling 
                    Move(new Move((7, 0), (7, 3)));
            }

            if (toRow == 0 && fromRow == 0 && fromCol == 4 && k.owner == Player.BLACK)
            {
                if (toCol == 6) // it is blacks short castling
                    Move(new Move((0, 7), (0, 5)));
                if (toCol == 2) // it is blacks long castling 
                    Move(new Move((0, 0), (0, 3)));
            }
        }
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

            deepcopy.Unmove( move );
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

    Chess GetDeepCopy() {
        Chess deepcopy = (Chess) this.MemberwiseClone();
        // now it is shallow copy, we must create new incstances of reference types:
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (board[i, j] is Piece p)
                    deepcopy.board[i, j] = p.GetCopy();
        deepcopy.lastCapture = lastCapture;
        deepcopy.castlingMemory = castlingMemory.ShallowCopy();
        return deepcopy;
    }

    public void Unmove( Move move )
    {
        Move(new Move(move.to, move.from) );
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
        if (board[from.row, from.col] is Piece pp && turn != pp.owner)
            return result;

        WriteLine("GetLegitMoves 2");

        if (board[from.row, from.col] is Piece p)
            result = p.PossibleMoves( board );

        // Now we must:
        //              a. filter out moves that would put players king into check
        //              b. in case the moving piece is king, consider adding options to do castling
        List<Move> filteredResult = FilterCheck( result );
        //foreach (Move castling in PossibleCastlings())
        //    result.Add(castling);

        WriteLine(filteredResult.Count);
        return filteredResult;
    }

    List<Move> FilterCheck( List<Move> possibleMoves )
    {
        // filter out all moves that would put players king into check
        // make deepcopy of current board
        // move all moves
        // check if king is in check (! king of current player)
        // if king is not in check add this move to result
        Chess deepcopy = GetDeepCopy();
        List<Move> result = new();
        foreach(Move move in possibleMoves)
        {
            deepcopy.Move( move );
            // is king in check checks the king whose turn it is! I must check the player that just move!!
            bool checkIsPossible = false;
            (int row, int col) = deepcopy.GetKingsCoordinates( turn ); // this needs to be here we also may be moving the king

            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    if (deepcopy.board[i, j] is Piece p && turn != p.owner)
                        foreach (Move enemyMove in p.PossibleMoves(deepcopy.board))
                            if (enemyMove.to.row == row && enemyMove.to.col == col)
                                checkIsPossible = true;

            if ( !checkIsPossible )
                result.Add(move);

            deepcopy.Unmove( move );
        }
        return result;
    }


}