using System;
using System.Collections.Generic;
using static System.Console;

class InvalidPositionException: Exception { }

class Position { 
    public int row;
    public int col;
    public Position(int row, int col) {
        if(row < 0 || col < 0 || row > 7 || col > 7)
            throw new InvalidPositionException();
            
        this.row = row;
        this.col = col;
    }
}

class Move { 
    public Position from;
    public Position to;
    public Move(Position from, Position to) {
        this.from = from;
        this.to = to;
    }
}

abstract class Player { }
class White : Player {
    public override string ToString() => "white";
}
class Black : Player {
    public override string ToString() => "black";
}

abstract class Tile { }

abstract class Piece : Tile {
    public Position position;
    public Player owner;

    protected Piece( Position position, Player owner ) {
        this.owner = owner;
        this.position = position;
    }

    public abstract Piece getCopy();

    public abstract List<Move> possibleMoves( Tile[,] board);
    // filter out following rule violations:
    //      1. piece must not capture another piece owned by same player 
    //      2. for infinite movers they may only move untill they hit first piece
    //      3. after the move is made, generate the board and check if players king got into check 
    //              if true the move is obviously not valid 
    // helper methods for leafe class of object tree:
    protected void addMove(List<Move> result, int toX, int toY) { 
        Position from = new Position( position.row, position.col );
        Move move = new Move(from, new Position(toX, toY) );
        result.Add(move);
    }

    protected bool isInsideBoard(int x, int y) => x >= 0 && x <= 7 && y >= 0 && y <= 7;
}

abstract class InfiniteMover : Piece {
    protected abstract (int, int)[] vectors { get; }
    public InfiniteMover(Position position, Player owner) : base( position, owner) { }

    public override List<Move> possibleMoves( Tile[,] board ) {
        List<Move> result = new List<Move>();
        // foreach vector, keep on adding options, untill you 'run' over edge of board
        int x, y;

        foreach( (int vX, int vY) in vectors) {
            x = position.row + vX;
            y = position.col + vY;
            while( isInsideBoard(x, y) ) {
                // if you ran into piece of same player, break right here:
                if( board[x,y] is Piece p)  // not empty 
                    if ( (owner is White && p.owner is White) || ( owner is Black && p.owner is Black ) )
                        break;

                // if you ran into piece of the other player, break here, since capturing is possible
                if( isInsideBoard(x, y) && board[x,y] is Piece pp) { // not empty 
                    if ( ( owner is White && pp.owner is Black ) || ( owner is Black && pp.owner is White ) ) {
                        addMove(result, x, y);
                        break;
                    }
                }
                        
                addMove(result, x, y);
                x += vX;
                y += vY;  
            }
        }
        return result;
    }
}

abstract class OneStepMover : Piece {
    protected abstract (int, int)[] steps { get; }
    public OneStepMover(Position position, Player owner) : base( position, owner) { }

    public override List<Move> possibleMoves( Tile[,] board ) {
        List<Move> result = new List<Move>();
        // foreach step, add it, if it is inside board and it is either empty or enemy piece
        foreach( (int x, int y) in steps) {
            int toX = position.row + x;
            int toY = position.col + y;
            if( isInsideBoard( toX, toY ) ) {
                if(board[toX, toY] == null)
                    addMove(result, toX, toY);
                if(board[toX, toY] is Piece p) { // allow captures, disallow caputuring piece of same player
                    if( p.owner is White && owner is Black || p.owner is Black && owner is White )
                        addMove(result, toX, toY);
                }
            }
        }
        return result;
    }
}

class Queen : InfiniteMover {
    public Queen(Position position, Player owner) : base( position, owner) { }

    (int, int)[] _vectors = new (int, int)[] { // possible directions of queen movement
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

    public override Queen getCopy() => new Queen(position, owner);
}

class Bishop : InfiniteMover {
    public Bishop(Position position, Player owner) : base( position, owner) { }

    private (int, int)[] _vectors = new (int, int)[] { // possible directions of queen movement
        (  1,  1 ),
        (  1, -1 ),
        ( -1,  1 ),
        ( -1, -1 )
    };

    protected override (int, int)[] vectors { 
        get => _vectors;
    }

    public override Bishop getCopy() => new Bishop(position, owner);
}

class Rook : InfiniteMover {
    public Rook(Position position, Player owner) : base( position, owner) { }

    private (int, int)[] _vectors = new (int, int)[] { // possible directions of queen movement
        (  1,  0 ),
        (  0,  1 ),
        (  0, -1 ),
        ( -1,  0 )
    };

    protected override (int, int)[] vectors {
        get => _vectors;
    }

    public override Rook getCopy() => new Rook(position, owner);
}

class King : OneStepMover {
    public King(Position position, Player owner) : base( position, owner) { }

    private (int, int)[] _steps = new (int, int)[] { // possible steps of king
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

    public override King getCopy() => new King(position, owner);
}

class Knight : OneStepMover {
    public Knight(Position position, Player owner) : base( position, owner) { }

    private (int, int)[] _steps = new (int, int)[] { // possible steps of king
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

    public override Knight getCopy() => new Knight(position, owner);
}

// ! transformations into queen will be dealt with at the level of class chess, class Pawn is unaware of these transformations
class Pawn : Piece {
    public Pawn(Position position, Player owner) : base( position, owner) { }

    public override List<Move> possibleMoves( Tile[,] board ) {
        int rowVector = 1, baseRow = 1; // deal with differences of white and black pawns
        if(owner is White) {
            rowVector = -1;
            baseRow = 6;
        }

        List<Move> result = new List<Move>();
        int r = position.row;
        int c = position.col;

        // horizontal two steps, in case pawn is at baseRow, and both tiles are empty
        if( r == baseRow ) {
            if( owner is White ) {
                int twoSteps = 4;
                if( board[ r+rowVector , c] == null && board[ r+rowVector+rowVector , c ] == null)
                    addMove( result, twoSteps, c );
            } else { // owner is black
                int twoSteps = 3;
                if( board[ r+rowVector , c] == null && board[ r+rowVector+rowVector , c ] == null)
                    addMove( result, twoSteps, c );
            }
        }
        // horizontal one step, in case that tile is empty (pawn may not capture horizontaly)
        if( board[ r+rowVector , c] == null)
            addMove(result, r+rowVector, c);
        // -1 axis in case there is enemy pawn abd is inside board
        if( c != 0 && board[r+rowVector,  c-1 ] is Piece p) 
            if( (owner is White && p.owner is Black ) || ( owner is Black && p.owner is White ) )
                addMove( result, r+rowVector , c-1 );
        // +- axis in case there is enemy pawn and is inside board
        if( c != 7 && board[r+rowVector, c+1 ] is Piece pp) 
            if( (owner is White && pp.owner is Black ) || ( owner is Black && pp.owner is White ) )
                addMove( result, r+rowVector , c+1 );

        return result;
    }

    public override Pawn getCopy() => new Pawn(position, owner);
}