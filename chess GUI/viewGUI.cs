using Cairo;
using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using CairoHelper = Gdk.CairoHelper;
using Window = Gtk.Window;
using static System.Console;

class ViewGTK : Window {
    readonly Chess chess;
    //int pressCounter = 0;
    int fromX = -1;
    int fromY = -1;
    bool highlight = true;
    int clickedOnWinner = 0;

    public ViewGTK(Chess chess) : base("Chess graphical interface: white's turn") {
        this.chess = chess;
        Resize(800, 800);
        AddEvents((int) EventMask.ButtonPressMask);
    }

    public static void Run(Chess chess) {
        Application.Init();
        ViewGTK v = new(chess);
        v.ShowAll();
        Application.Run();
    }
    
    protected override bool OnDeleteEvent(Event ev) {
        Application.Quit();
        return true;
    }
    
    static void DrawImage(Context c, Pixbuf pixbuf, int x, int y) {
        CairoHelper.SetSourcePixbuf(c, pixbuf, x, y);
        c.Paint();
    }

    protected override bool OnDrawn (Context c) {
        DrawBoard(c);
        DrawPieces(c);
        if(highlight)
            Highlight(c, fromX, fromY);

        return true;
    }

    void DrawBoard(Context c) { 
        string filePath = "img/100wood.png";
        Pixbuf board = new(filePath);
        DrawImage(c, board, 0, 0);
    }

    void DrawPieces(Context c) {
        Pixbuf board;
        string filePath, owner, Ptype;
        for(int i = 0; i < 8; i++) {
            for(int j = 0; j < 8; j++) {
                if(chess.board[i, j] is Piece piece) {
                    owner = piece.owner == Player.WHITE ? "w" : "b";

                    Ptype = "k";
                    if(piece is Queen) Ptype = "q";
                    if(piece is Rook) Ptype = "r";
                    if(piece is Knight) Ptype = "n";
                    if(piece is Bishop) Ptype = "b";
                    if(piece is Pawn) Ptype = "p";

                    filePath = $"img/{owner}{Ptype}.png";
                    board = new Pixbuf(filePath);
                    DrawImage(c, board, j*100, i*100);

                }
            }
        }
    }

    protected override bool OnButtonPressEvent (EventButton e) {
        if( CheckWin() )
            return true;

        //pressCounter++;
        (int x, int y) = ConvertToTiles( e.X, e.Y);
        WriteLine($"Registered coordinates x, y: {e.X}, {e.Y}\nWhich translates to tiles: {x}, {y}\n");
        if(fromX == -1) {
            fromX = x;
            fromY = y;
            highlight = true;

        } else {
            highlight = false;
            WriteLine($"Intended move is from: {fromX}, {fromY}, to: {x},{y}");
            if(fromX == x && fromY == y) { // troll the user:
                WriteLine("Interesting idea, true chess Grandmaster\n");
            } else {
                Move move = new ( (fromX, fromY), (x, y) );
                if(chess.MakeLegitMove(move)) {
                    WriteLine("This move is legitimate according to code rules! :) ");
                    WriteLine($"It is {chess.turn}'s move now.");
                } else {
                    WriteLine("Sorry, invalid move.");
                }
            }
            WriteLine();
            fromX = -1;
            fromY = -1;
        }

        string checkMSG = chess.check ? ", king is in CHECK!" : "";
        string title = "Chess graphical interface";
        Title = title + ": white's turn" + checkMSG;
        if(chess.turn == Player.BLACK )
            Title = title + ": black's turn" + checkMSG;

        InformWinner();
        
        QueueDraw();
        return true;
    }

    void InformWinner()
    {
        if (chess.winner != Player.NO_ONE)
            Title = "Game over! The winner is " + chess.winner + ".";
    }

    bool CheckWin() {

        if( chess.winner == Player.NO_ONE )
            return false;
        
        clickedOnWinner++;

        if(clickedOnWinner == 1) {
            // change title informing about the win and queuedraw
            InformWinner();
            QueueDraw();
        } else {
            // exit the game
            Application.Quit();
        }

        return true;
    }

    (int, int) ConvertToTiles(double mouseX, double mouseY) => ( (int) mouseY/100 , (int) mouseX/100);
    
    void Highlight(Context c, int x, int y) {
        RGBA color = new ();
        color.Alpha = 0.5;
        color.Red = 1;
        color.Green = 1;
        color.Blue = 1;
        FillRectangle(c, color, 100 * y, 100 * x , 100, 100);
    }

    void FillRectangle(Context c, RGBA color, int x, int y, int width, int height) {
        CairoHelper.SetSourceRgba(c, color);
        c.Rectangle(x, y, width, height);
        c.Fill();
    }
}

/*
missing features: 

3. class King is capable of giving Castleing moves correctly 
4. all piece classes correctly disallow move, if it would put their own King into check position  
5. ANIMACE prechodu figurky

Done    1. class chess evaluates after move whether it is check, view informs about this
Done?   2. class chess evaluates after move whether is is check-mate and view falls to end game state 
Done    6. pawn se po prichodu na posledni row transformuje na kralovnu */


/*
 Castleing architecture:
 six booleans in separate class to not populate chess namespace 
    white_A_RookMoved >> A stands for column 
    white_H_RookMoved >> H stands for column 
    whiteKingMoved
    black_A_RookMoved
    black_H_RookMoved
    blackKingMoved
 
       booleans will initiate to false 
       
    1. find good place that will turn them to true once the coresponding pieces move 
    2. write method add castleing moves that will add moves to possible moves in all needed places 
    3. add function inside makemove that will check whether king moves as in casteling (two tiles distance)
    4.      if it does, it will also move the coresponding rook 
 */