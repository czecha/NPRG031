using System;
using System.Collections.Generic;

class InvalidPositionException: Exception { }

class Move { 
    public (int row, int col) from;
    public (int row, int col) to;
    public Move( (int x, int y) from, (int x, int y) to) {
        this.from = from;
        this.to = to;
    }
}

enum Player { WHITE, BLACK, NO_ONE }

abstract class Tile { }

abstract class Piece : Tile {
    public ( int row, int col ) position;
    public Player owner;

    protected Piece( (int row, int col ) position, Player owner ) {
        this.owner = owner;
        this.position = position;
    }

    public abstract Piece GetCopy();

    public abstract List<Move> PossibleMoves( Tile[,] board);
    // filter out following rule violations:
    //      1. piece must not capture another piece owned by same player 
    //      2. for infinite movers they may only move untill they hit first piece
    //      3. after the move is made, generate the board and check if players king got into check 
    //              if true the move is obviously not valid 
    // helper methods for leafe class of object tree:
    protected void AddMove(List<Move> result, int toX, int toY) {
        ( int, int ) from = position ;
        Move move = new ( from, (toX, toY) );
        result.Add(move);
    }

    protected bool IsInsideBoard( int x, int y ) => x >= 0 && x <= 7 && y >= 0 && y <= 7;
    protected bool IsEnemy( Piece p ) => ( owner == Player.WHITE && p.owner == Player.BLACK ) || ( owner == Player.BLACK && p.owner == Player.WHITE );
    public bool IsWhite() => owner == Player.WHITE;
    public bool IsBlack() => owner == Player.BLACK;
}

abstract class InfiniteMover : Piece {
    protected abstract (int, int)[] vectors { get; }
    public InfiniteMover( (int, int) position, Player owner) : base( position, owner) { }

    public override List<Move> PossibleMoves( Tile[,] board ) {
        List<Move> result = new();
        int x, y;

        foreach( (int vX, int vY) in vectors) {
            // foreach vector, keep on adding options, untill you 'run' over edge of board or some other piece
            x = position.row + vX;
            y = position.col + vY;
            while( IsInsideBoard(x, y) ) {
                if( board[x,y] is Piece p && owner == p.owner )   
                    break; // if you ran into piece of same player, break right here

                AddMove(result, x, y);
                if( board[x,y] is Piece pp && IsEnemy( pp ) )
                    break; // if you ran into piece of the other player, break here, since capturing is possible
                x += vX;
                y += vY;  
            }
        }
        return result;
    }
}

abstract class OneStepMover : Piece {
    protected abstract (int, int)[] steps { get; }
    public OneStepMover( (int, int) position, Player owner) : base( position, owner) { }

    public override List<Move> PossibleMoves( Tile[,] board ) {
        List<Move> result = new ();
        foreach( (int x, int y) in steps) {
            // foreach step, add it, if it is inside board and it is either empty or enemy piece
            int toX = position.row + x;
            int toY = position.col + y;
            if( IsInsideBoard( toX, toY ) ) {
                if(board[toX, toY] == null || ( board[toX, toY] is Piece p && IsEnemy( p ) ) )
                    AddMove(result, toX, toY);
            }
        }
        return result;
    }
}

class Queen : InfiniteMover {
    public Queen( (int, int) position, Player owner) : base( position, owner) { }

    readonly (int, int)[] _vectors = new (int, int)[] { // possible directions of queen movement
        (  1,  1 ),
        (  1,  0 ),
        (  1, -1 ),
        (  0,  1 ),
        (  0, -1 ),
        ( -1,  1 ),
        ( -1,  0 ),
        ( -1, -1 )
    };

    protected override (int, int)[] vectors  { 
        get => _vectors;
    }

    public override Queen GetCopy() => new (position, owner);
}

class Bishop : InfiniteMover {
    public Bishop( (int, int) position, Player owner) : base( position, owner) { }

    readonly (int, int)[] _vectors = new (int, int)[] { // possible directions of bishop movement
        (  1,  1 ),
        (  1, -1 ),
        ( -1,  1 ),
        ( -1, -1 )
    };

    protected override (int, int)[] vectors { 
        get => _vectors;
    }

    public override Bishop GetCopy() => new (position, owner);
}

class Rook : InfiniteMover {
    public Rook( (int, int) position, Player owner) : base( position, owner) { }

    readonly (int, int)[] _vectors = new (int, int)[] { // possible directions of rook movement
        (  1,  0 ),
        (  0,  1 ),
        (  0, -1 ),
        ( -1,  0 )
    };

    protected override (int, int)[] vectors {
        get => _vectors;
    }

    public override Rook GetCopy() => new (position, owner);
}

class King : OneStepMover {
    public King( (int, int) position, Player owner) : base( position, owner) { }

    readonly (int, int)[] _steps = new (int, int)[] { // possible steps of king
        (  1,  1 ),
        (  1,  0 ),
        (  1, -1 ),
        (  0,  1 ),
        (  0, -1 ),
        ( -1,  1 ),
        ( -1,  0 ),
        ( -1, -1 )
    };

    protected override (int, int)[] steps {
        get => _steps;
    }

    public override King GetCopy() => new (position, owner);
}

class Knight : OneStepMover {
    public Knight( (int, int) position, Player owner) : base( position, owner) { }

    readonly (int, int)[] _steps = new (int, int)[] { // possible steps of knight
        (  2,  1 ),
        (  1,  2 ),
        ( -2,  1 ),
        ( -1,  2 ),
        (  2, -1 ),
        (  1, -2 ),
        ( -2, -1 ),
        ( -1, -2 )
    };

    protected override (int, int)[] steps {
        get => _steps;
    }

    public override Knight GetCopy() => new (position, owner);
}

// ! transformations into queen will be dealt with at the level of class chess, class Pawn is unaware of these transformations
class Pawn : Piece {
    public Pawn( (int, int) position, Player owner) : base( position, owner) { }

    public override List<Move> PossibleMoves( Tile[,] board ) {
        int rowVector = IsBlack() ? 1 : -1;
        int baseRow = IsBlack() ? 1 : 6;
        int twoSteps = IsWhite() ? 4 : 3; // destination row of two steps jump
        List<Move> result = new ();
        int r = position.row;
        int c = position.col;

        // horizontal two steps, in case pawn is at baseRow, and both tiles are empty
        if( r == baseRow && board[ r+rowVector , c] == null && board[ r+rowVector+rowVector , c ] == null)
            AddMove( result, twoSteps, c );
        
        // horizontal one step, in case that tile is empty (pawn may not capture horizontaly)
        if( board[ r+rowVector , c] == null)
            AddMove(result, r+rowVector, c);

        // -1 axis in case there is enemy pawn abd is inside board
        if( c != 0 && board[r+rowVector,  c-1 ] is Piece p &&  IsEnemy( p ) ) 
            AddMove( result, r+rowVector , c-1 );

        // +- axis in case there is enemy pawn and is inside board
        if( c != 7 && board[r+rowVector, c+1 ] is Piece pp &&  IsEnemy( pp ) ) 
            AddMove( result, r+rowVector , c+1 );

        return result;
    }

    public override Pawn GetCopy() => new (position, owner);
}