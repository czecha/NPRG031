using System;
using static System.Console;
using System.Text.RegularExpressions;

namespace chees_GUI
{
    class CommandLineView {
        Chess ch;
        Regex regex = new Regex("^[0-7][ ][0-7][ ][0-7][ ][0-7]$");
        public CommandLineView() { 
            ch = new Chess(); 
        }

        // there are two different states,
        // either the game is asking for next move
        // or it informs about game over conditions

        public void Run() {
            while(true) {
                if(ch.winner != null) {
                    informAboutWinner();
                    break;
                }

                printBoard();

                string playersMove = "white";
                if(ch.turn is Black)
                    playersMove = "black";
                WriteLine($"\n It is {playersMove}'s turn!\n");
                WriteLine("Type integers 0-7 for in the format: fromRow fromCol toRow toCol ( Digits seperated by whitespace )");
                string playersCommand = ReadLine();

                while( ! validCommand( playersCommand ) ) {
                    WriteLine("Sorry, the format of your input is not valid. Try again:");
                    playersCommand = ReadLine();
                }

                while( ! ch.makeMove( parseCommand( playersCommand ) ) ) {
                    WriteLine("Sorry, the move was in correct format, but is not valid according to chess rules. Try again.");
                    playersCommand = ReadLine();

                    while( ! validCommand( playersCommand ) ) {
                        WriteLine("Sorry, the format of your input is not valid. Try again:");
                        playersCommand = ReadLine();
                    }
                }
                // not the move has been made and was legitimate, and so we can repeat the loop for next move :) 
            }

            WriteLine("Would you like to play again? [y/n]");
            string s = ReadLine();
            while( s != "y" && s != "n" ) {
                WriteLine("Sorry, couldn't process your answer.\nWould you like to play again?\nType 'y' or 'n'.");
                s = ReadLine();
            }

            if(s == "y") {
                ch = new Chess();
                Run();
            } 
        }

        void informAboutWinner() {
            string winner = "black";
            if(ch.winner is White) 
                winner = "white";
            WriteLine($"\nGame over!\nThe winner is {winner}.\n");
        }

        void printBoard() {
            WriteLine();
            for(int i = 0; i < 8; i++) {
                string line = "";
                for(int j = 0; j < 8; j++) {
                    if(ch.board[i][j] is Empty)
                        line += "-- ";
                    if(ch.board[i][j] is Piece p) {
                        if(p.owner is White) {
                            line += "w";
                        } else {
                            line += "b";
                        }

                        if(p is Pawn) {
                            line += "P ";
                        } else if (p is Rook) {
                            line += "R ";
                        } else if (p is Knight) {
                            line += "N ";
                        } else if ( p is Bishop ) {
                            line += "B ";
                        } else if ( p is Queen ) {
                            line += "Q ";
                        } else if ( p is King ) {
                            line += "K ";
                        } else {
                            WriteLine("Error, piece is of unknown type!!");
                            throw new Exception();
                        }
                    }
                }
                WriteLine(line);
            }
        }

        // check that it is 4 digits seperated by one white space all of them in the range 0-7
        bool validCommand( string usersInput ) => regex.IsMatch(usersInput);

        Move parseCommand( string usersInput ) {
            string[] splited = usersInput.Split();
            int fromRow = int.Parse(splited[0]);
            int fromCol = int.Parse(splited[1]);
            int toRow = int.Parse(splited[2]);
            int toCol = int.Parse(splited[3]);
            Move move = new Move( new Position(fromRow, fromCol), new Position(toRow, toCol) );
            return move;
        }
    }
}