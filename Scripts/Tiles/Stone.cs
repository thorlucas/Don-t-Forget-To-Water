using System;
using Godot;

public class Stone : Tile {
    public Stone(int x, int y) : base(x, y) {
    }

    public override int TileMapIndex => 0;
    public override bool Chiselable => true;
}