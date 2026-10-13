using System;
using System.Collections.Generic;

public class GameState
    {
        public string[] Board { get; set; } = Array.Empty<string>();
        public bool WhiteTurn { get; set; }
        public List<string> History { get; set; } = new();
        public List<char> Captured { get; set; } = new();
        public bool WhiteKingSide { get; set; }
        public bool WhiteQueenSide { get; set; }
        public bool BlackKingSide { get; set; }
        public bool BlackQueenSide { get; set; }
        public int EnPassantRow { get; set; } = -1;
        public int EnPassantCol { get; set; } = -1;
    }
