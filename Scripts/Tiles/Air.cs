using System;
using Godot;

public class Air : Tile {
    public Air(int x, int y) : base(x, y) {
    }

    public override bool Transparent => true;
}