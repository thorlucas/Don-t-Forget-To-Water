using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

public class Dirt : Tile, IHydratable {
    public static int GrassHydration = 3;
    private static int MaxHydration = 5;
    private int hydration = 0;

    public Dirt(int x, int y) : base(x, y) {
    }

    public override int TileMapIndex {
        get {
            if (hydration == 0) return 1;
            else if (hydration == 1) return 2;
            else if (hydration == 2) return 3;
            else return 4;
        }
    }
    public override bool Chiselable => true;

    public int Hydration => hydration;

    public override bool Update(Level level) {
        List<IHydratable> hydratableNeighbors = level.GetTileNeighbors<IHydratable>(this).Where(x => x.Hydration < hydration).ToList();

        foreach (IHydratable neighbor in hydratableNeighbors) {
            if (neighbor.Hydration < this.hydration - 1) {
                this.hydration -= neighbor.Hydrate(1);
            }
        }

        return true;
    }

    public int Hydrate(int max) {
        if (hydration >= MaxHydration) {
            return 0;
        }

        int used = Math.Min(max, 1);
        hydration += used;
        return used;
    }
}