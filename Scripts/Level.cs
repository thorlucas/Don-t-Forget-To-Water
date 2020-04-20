using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Flags]
enum FlowDirection {
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Both = Left | Right,
}

public class Level : Node2D {
    [Export]
    public int Chisels;

    [Export(PropertyHint.MultilineText)]
    public String Tip;

    public int ChiselsLeft;

    private Tile[,] tiles;
    private WaterCell[,] waterCells;

    private TileMap tileMap;
    private TileMap waterMap;
    private TileMap backgroundMap;

    private List<int> bodies;

    private List<IGoal> goals;
    private bool won;

    private const int LevelWidth = 16;
    private const int LevelHeight = 16;

    private RandomNumberGenerator rand;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        rand = new RandomNumberGenerator();
        rand.Randomize();

        tileMap = FindNode("Tiles") as TileMap;
        waterMap = FindNode("Water") as TileMap;
        backgroundMap = FindNode("Background") as TileMap;

        backgroundMap.UpdateBitmaskRegion();

        tiles = new Tile[LevelWidth, LevelHeight];
        waterCells = new WaterCell[LevelWidth, LevelHeight];
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                waterCells[x, y] = new WaterCell(x, y);
            }
        }

        goals = new List<IGoal>();
        bodies = new List<int>();

        ChiselsLeft = Chisels;

        LoadFromTileMap();

        UpdateTileMapTiles();
        UpdateTileMapWater();
    }

    public void Tick() {
        UpdateTiles();
        UpdateBodies();
        FlowWater();
        
        

        UpdateTileMapTiles();
        UpdateTileMapWater();

        if (!won) {
            bool allComplete = true;
            foreach (IGoal goal in goals) {
                if (!goal.Complete) {
                    allComplete = false;
                }
            }

            if (allComplete) {
                won = true;
                EmitSignal(nameof(Completed), CalculateScore());
            }
        }
    }

    private int CalculateScore() {
        int score = 0;

        score += 1000 * ChiselsLeft;
        score += 100 * goals.Where(x => x.Complete).ToList().Count;

        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                if (tiles[x, y] is Dirt dirt) {
                    if (dirt.Hydration >= Dirt.GrassHydration) {
                        score += 10;
                    }
                }
            }
        }

        return score;
    }

    [Signal]
    public delegate void Completed(int points);

    private void AddBody(WaterCell cell, int body) {
        cell.bodyIndex = body;
        bodies[body] = Math.Max(WaterHeight(cell), bodies[body]);
        List<WaterCell> neighbors = GetWaterNeighbors(cell);

        foreach (WaterCell neighbor in neighbors) {
            if (neighbor.bodyIndex == -1) {
                AddBody(neighbor, body);
            }
        }
    }

    private void UpdateBodies() {
        bodies.Clear();

        // First determine which bodies each cell belongs to
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                if (!IsWater(x, y)) {
                    continue;
                }

                // All cells start with no body in start of the tick
                WaterCell cell = waterCells[x, y];
                if (cell.bodyIndex == -1) {
                    bodies.Add(int.MinValue);
                    AddBody(cell, bodies.Count - 1);
                }
            }
        }
    }

    private void FlowWater() {
        // Pressure Calculations
        // Static full bodies with air & nonfull fluid above them should move up
        for (int y = LevelHeight - 1; y >= 0; --y) {
            for (int x = 0; x < LevelWidth; ++x) {
                
                WaterCell cell = waterCells[x, y];
                if (!cell.staticFull) {
                    continue;
                }

                // If we a valid tile above us
                // And that water is not full
                // And our body's max height is greater than the height of the water above us
                // Then move one of our waters up
                if (IndexInRange(x, y - 1)) {
                    if (tiles[x, y - 1].Transparent && waterCells[x, y - 1].Level < WaterCell.MaxLevel && bodies[cell.bodyIndex] > WaterHeight(waterCells[x, y - 1])) {
                        cell.Level -= 1;
                        waterCells[x, y - 1].Level += 1;
                        waterCells[x, y - 1].NoCalc = true;
                        cell.staticFull = false;
                    }
                }
            }
        }

        // Flow Calculations
        // Bottom to top
        for (int y = LevelHeight - 1; y >= 0; --y) {
            for (int x = 0; x < LevelWidth; ++x) {
                WaterCell cell = waterCells[x, y];
                if (cell.Level == 0 || cell.NoCalc == true) {
                    continue;
                }

                FlowDirection flow = FlowDirection.None;

                if (cell.Level == WaterCell.MaxLevel) {
                    cell.staticFull = true;
                }


                // TODO: Check out of bounds!
                ref WaterCell bottomCell = ref waterCells[x, y + 1];
                if (tiles[x, y + 1].Transparent && bottomCell.Level < 8) {
                    flow |= FlowDirection.Bottom;
                }

                ref WaterCell rightCell = ref waterCells[x + 1, y];
                if (tiles[x + 1, y].Transparent && rightCell.Level < cell.Level) {
                    flow |= FlowDirection.Right;
                }

                ref WaterCell leftCell = ref waterCells[x - 1, y];
                if (tiles[x - 1, y].Transparent && leftCell.Level < cell.Level) {
                    flow |= FlowDirection.Left;
                }

                if (flow != FlowDirection.None) {
                    cell.staticFull = false;
                }

                switch (flow) {
                case FlowDirection.Bottom: FlowBottom(ref cell, ref bottomCell); break;
                case FlowDirection.Right: FlowRight(ref cell, ref rightCell); break;
                case FlowDirection.Left: FlowLeft(ref cell, ref leftCell); break;
                case FlowDirection.Bottom | FlowDirection.Right: {
                        int remainder = FlowBottom(ref cell, ref bottomCell);
                        if (remainder > 0) FlowRight(ref cell, ref rightCell);
                        break;
                    }
                case FlowDirection.Bottom | FlowDirection.Left: {
                        int remainder = FlowBottom(ref cell, ref bottomCell);
                        if (remainder > 0) FlowLeft(ref cell, ref leftCell);
                        break;
                    }
                case FlowDirection.Bottom | FlowDirection.Both: {
                        int remainder = FlowBottom(ref cell, ref bottomCell);
                        if (remainder > 0) FlowBoth(ref cell, ref leftCell, ref rightCell);
                        break;
                    }
                case FlowDirection.Both: FlowBoth(ref cell, ref leftCell, ref rightCell); break;
                default: continue;
                }
            }
        }
    }

    private int FlowBottom(ref WaterCell cell, ref WaterCell bottomCell) {
        int spaceAvailable = 8 - bottomCell.Level;
        int spread = Math.Min(cell.Level, spaceAvailable);

        bottomCell.Level += spread;
        cell.Level -= spread;

        bottomCell.Direction = FlowMomentumDirection.None;

        bottomCell.NoCalc = true;
        cell.NoCalc = true;

        return cell.Level;
    }

    private int FlowRight(ref WaterCell cell, ref WaterCell rightCell) {
        int total = cell.Level + rightCell.Level;
        int spread = total / 2;
        int remainder = total % 2;

        cell.Level = spread;
        rightCell.Level = spread + remainder;
        rightCell.Direction = FlowMomentumDirection.Right;

        cell.NoCalc = true;
        rightCell.NoCalc = true;

        return spread;
    }

    private int FlowLeft(ref WaterCell cell, ref WaterCell leftCell) {
        int total = cell.Level + leftCell.Level;
        int spread = total / 2;
        int remainder = total % 2;

        cell.Level = spread;
        leftCell.Level = spread + remainder;
        leftCell.Direction = FlowMomentumDirection.Left;

        cell.NoCalc = true;
        leftCell.NoCalc = true;

        return spread;
    }

    private int FlowBoth(ref WaterCell cell, ref WaterCell leftCell, ref WaterCell rightCell) {
        int total = cell.Level + leftCell.Level + rightCell.Level;
        int spread = total / 3;
        int remainder = total % 3;

        cell.Level = spread;
        leftCell.Level = spread;
        rightCell.Level = spread;

        if (remainder > 0) {
            if (cell.Direction == FlowMomentumDirection.None) {
                if (rand.Randi() % 2 == 0) {
                    cell.Direction = FlowMomentumDirection.Left;
                } else {
                    cell.Direction = FlowMomentumDirection.Right;
                }
            }


            if (cell.Direction == FlowMomentumDirection.Left) {
                leftCell.Level += remainder;
                leftCell.Direction = FlowMomentumDirection.Left;
            } else if (cell.Direction == FlowMomentumDirection.Right) {
                rightCell.Level += remainder;
                rightCell.Direction = FlowMomentumDirection.Right;
            }
        }


        cell.NoCalc = true;
        leftCell.NoCalc = true;
        rightCell.NoCalc = true;
        return spread;
    }

    private void UpdateTiles() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                tiles[x, y].Update(this);

                // Hydration calculations
                if (waterCells[x, y].Level > 0) {
                    {
                        if (IndexInRange(x, y + 1) && tiles[x, y + 1] is IHydratable tile) {
                            waterCells[x, y].Level -= tile.Hydrate(waterCells[x, y].Level);
                        }
                    }

                    // TODO: Randomize left & right hydration

                    {
                        if (IndexInRange(x + 1, y) && tiles[x + 1, y] is IHydratable tile) {
                            waterCells[x, y].Level -= tile.Hydrate(waterCells[x, y].Level);
                        }
                    }

                    {
                        if (IndexInRange(x + 1, y) && tiles[x - 1, y] is IHydratable tile) {
                            waterCells[x, y].Level -= tile.Hydrate(waterCells[x, y].Level);
                        }
                    }

                    {
                        if (waterCells[x, y].Level == WaterCell.MaxLevel && IndexInRange(x, y - 1) && tiles[x, y - 1] is IHydratable tile) {
                            waterCells[x, y].Level -= tile.Hydrate(waterCells[x, y].Level);
                        }
                    }
                }


                waterCells[x, y].NoCalc = false;
                waterCells[x, y].bodyIndex = -1;
            }
        }
    }

    // Utilities

    public Vector2 GlobalToTile(Vector2 global) {
        return tileMap.WorldToMap(tileMap.ToLocal(global));
    }

    private int WaterHeight(WaterCell cell) {
        return (LevelHeight - cell.y) * WaterCell.MaxLevel + cell.Level;
    }

    public List<WaterCell> GetWaterNeighbors(WaterCell cell) {
        List<WaterCell> neighbors = new List<WaterCell>();
        if (IsWater(cell.x + 1, cell.y)) neighbors.Add(waterCells[cell.x + 1, cell.y]);
        if (IsWater(cell.x - 1, cell.y)) neighbors.Add(waterCells[cell.x - 1, cell.y]);
        if (IsWater(cell.x, cell.y + 1)) neighbors.Add(waterCells[cell.x, cell.y + 1]);
        if (IsWater(cell.x, cell.y - 1)) neighbors.Add(waterCells[cell.x, cell.y - 1]);
        return neighbors;
    }

    public List<Tile> GetTileNeighbors(Tile tile) {
        List<Tile> neighbors = new List<Tile>();

        if (IndexInRange(tile.x + 1, tile.y)) neighbors.Add(tiles[tile.x + 1, tile.y]);
        if (IndexInRange(tile.x - 1, tile.y)) neighbors.Add(tiles[tile.x - 1, tile.y]);
        if (IndexInRange(tile.x, tile.y + 1)) neighbors.Add(tiles[tile.x, tile.y + 1]);
        if (IndexInRange(tile.x, tile.y - 1)) neighbors.Add(tiles[tile.x, tile.y - 1]);

        return neighbors;
    }

    public List<T> GetTileNeighbors<T>(Tile tile) {
        List<T> neighbors = new List<T>();

        foreach (Tile neighbor in GetTileNeighbors(tile)) {
            if (neighbor is T castTile) {
                neighbors.Add(castTile);
            }
        }

        return neighbors;
    }

    public Tile GetTile(int x, int y) {
        return tiles[x, y];
    }

    public bool SetTile(int x, int y, Tile tile) {
        if (IndexInRange(x, y)) {
            tiles[x, y] = tile;
            return true;
        } else {
            return false;
        }
    }

    public bool IndexInRange(int x, int y) {
        if (x >= 0 && x < LevelWidth && y >= 0 && y < LevelHeight) {
            return true;
        } else {
            return false;
        }
    }

    public bool IsWater(int x, int y) {
        if (IndexInRange(x, y)) {
            if (waterCells[x, y].Level > 0) {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    public bool TryGetWaterLevel(int x, int y, out int waterLevel) {
        if (IndexInRange(x, y)) {
            waterLevel = waterCells[x, y].Level;
            return true;
        }

        waterLevel = 0;
        return false;
    }

    public int AbsorbWater(int x, int y, int quantity) {
        int exchanged = Math.Min(quantity, waterCells[x, y].Level);
        waterCells[x, y].Level -= exchanged;

        return exchanged;
    }

    public int AddWater(int x, int y, int quantity) {
        if (!IndexInRange(x, y)) return 0;
        if (tiles[x, y] is Air) {
            int exchanged = Math.Min(quantity, WaterCell.MaxLevel - waterCells[x, y].Level);
            waterCells[x, y].Level += exchanged;
            return exchanged;
        } else {
            return 0;
        }

    }

    // Map Interaction

    public bool TryChisel(int x, int y) {
        if (ChiselsLeft <= 0) {
            return false;
        }

        if (!IndexInRange(x, y)) {
            return false;
        }

        if (tiles[x, y].Chiselable) {
            tiles[x, y] = new Air(x, y);
            ChiselsLeft -= 1;
            //GD.Print("Chisels left: ", ChiselsLeft);
            return true;
        }

        return false;
    }

    // Render and Loading Utilities

    private Vector2 GetWaterAutotileCoords(int x, int y) {
        int waterLevel = waterCells[x, y].Level;
        if (waterLevel == WaterCell.MaxLevel && IndexInRange(x, y - 1) && waterCells[x, y - 1].Level > 0) {
            return new Vector2(0, 1);
        } else {
            return new Vector2(8 - waterLevel, 0);
        }
    }

    private void LoadFromTileMap() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                switch (tileMap.GetCell(x, y)) {
                case -1: tiles[x, y] = new Air(x, y); break;

                case 0:  tiles[x, y] = new Stone(x, y); break;

                case 1:
                case 2:
                case 3:
                case 4:
                    tiles[x, y] = new Dirt(x, y); break;
                 

                case 5:
                    Sapling sapling = new Sapling(x, y);
                    tiles[x, y] = sapling;
                    goals.Add(sapling);
                    break;

                case 6: tiles[x, y] = new CrackedStone(x, y); break;

                case 7: tiles[x, y] = new Perlite(x, y); break;

                }

                switch (waterMap.GetCell(x, y)) {
                case 0:
                    int waterLevel = WaterCell.MaxLevel - (int)waterMap.GetCellAutotileCoord(x, y).x;
                    waterCells[x, y].Level = waterLevel;
                    break;
                }
            }
        }
    }

    private void UpdateTileMapTiles() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                tileMap.SetCell(x, y, tiles[x, y].TileMapIndex, autotileCoord: tiles[x, y].TileMapAutotileCoord);
            }
        }
        tileMap.UpdateBitmaskRegion();
    }

    private void UpdateTileMapWater() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                int level = waterCells[x, y].Level;
                if (level == 0) {
                    waterMap.SetCell(x, y, -1);
                } else {
                    waterMap.SetCell(x, y, 0, autotileCoord: GetWaterAutotileCoords(x, y));
                }
            }
        }
    }

}
