using System;
using Godot;

public class TileSetConnections : TileSet {
    public override bool _IsTileBound(int drawnId, int neighborId) {
        if (neighborId != -1 && neighborId != 5) return true;

        return false;
    }
}