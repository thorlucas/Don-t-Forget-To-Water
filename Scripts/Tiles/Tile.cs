using System;
using Godot;

public abstract class Tile {
    public int x;
    public int y;

    public Tile(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public virtual int TileMapIndex => -1;
    public virtual Vector2 TileMapAutotileCoord => Vector2.Zero;
    public virtual bool Transparent => false;
    public virtual bool Chiselable => false;

    public virtual bool Update(Level level) { return false; }
}