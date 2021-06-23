using System;
using static System.Console;
using Gdk;
using Gtk;

namespace chees_GUI {
    static class Program {
        static void Main() {
            Chess chess = new Chess();
            ViewGTK.run( chess );
        }
    }   
}
