using Godot;
using System;

enum TileType {
    AIR,
    STONE,
}

[Flags]
enum FlowDirection {
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Both = Left | Right,
}

enum FlowMomentumDirection {
    None,
    Left,
    Right
}

struct WaterCell {
    public int Level;
    public bool NoCalc;
    public FlowMomentumDirection Direction;
}

public class Level : TileMap {
    private TileType[,] staticTiles;
    private WaterCell[,] waterCells;

    private const int LevelWidth = 16;
    private const int LevelHeight = 16;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        staticTiles = new TileType[LevelWidth, LevelHeight];
        waterCells = new WaterCell[LevelWidth, LevelHeight];

        LoadFromTileMap();

        UpdateTileMapStatic();
        UpdateTileMapWater();
    }

    public void Tick() {
        // Bottom to top
        for (int y = LevelHeight - 1; y >= 0; --y) {
            for (int x = 0; x < LevelWidth; ++x) {
                ref WaterCell cell = ref waterCells[x, y];
                if (cell.Level == 0 || cell.NoCalc == true) {
                    continue;
                }

                FlowDirection flow = FlowDirection.None;


                // TODO: Check out of bounds!
                ref WaterCell bottomCell = ref waterCells[x, y + 1];
                if (staticTiles[x, y + 1] == TileType.AIR && bottomCell.Level < 8) {
                    flow |= FlowDirection.Bottom;
                }

                ref WaterCell rightCell = ref waterCells[x + 1, y];
                if (staticTiles[x + 1, y] == TileType.AIR && rightCell.Level < cell.Level) {
                    flow |= FlowDirection.Right;
                }

                ref WaterCell leftCell = ref waterCells[x - 1, y];
                if (staticTiles[x - 1, y] == TileType.AIR && leftCell.Level < cell.Level) {
                    flow |= FlowDirection.Left;
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

        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                waterCells[x, y].NoCalc = false;
            }
        }

        UpdateTileMapWater();
    }

    public bool IndexInRange(int x, int y) {
        if (x >= 0 && x < LevelWidth && y >= 0 && y < LevelHeight) {
            return true;
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

    private int FlowBottom(ref WaterCell cell, ref WaterCell bottomCell) {
        int spaceAvailable = 8 - bottomCell.Level;
        int spread = Math.Min(cell.Level, spaceAvailable);

        bottomCell.Level += spread;
        cell.Level -= spread;

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
            // TODO: If direction is none, pick a random one
            if (cell.Direction == FlowMomentumDirection.Left) {
                leftCell.Level += remainder;
                leftCell.Direction = FlowMomentumDirection.Left;
            } else {
                rightCell.Level += remainder;
                rightCell.Direction = FlowMomentumDirection.Right;
            }
        }


        cell.NoCalc = true;
        leftCell.NoCalc = true;
        rightCell.NoCalc = true;
        return spread;
    }

    private int GetTileMapIndex(TileType tileType) {
        switch (tileType) {
        case TileType.AIR: return -1;
        case TileType.STONE: return 0;
        default: return -1;
        }
    }

    private Vector2 GetWaterAutotileCoords(int level) {
        return new Vector2(8 - level, 0);
    }

    private void LoadFromTileMap() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                switch (GetCell(x, y)) {
                case -1: staticTiles[x, y] = TileType.AIR; break;
                case 0: staticTiles[x, y] = TileType.STONE; break;
                case 1:
                    int waterLevel = 8 - (int)GetCellAutotileCoord(x, y).x;
                    waterCells[x, y].Level = waterLevel;
                    break;
                }
            }
        }
    }

    private void UpdateTileMapStatic() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                SetCell(x, y, GetTileMapIndex(staticTiles[x, y]));
            }
        }
    }

    private void UpdateTileMapWater() {
        for (int y = 0; y < LevelHeight; ++y) {
            for (int x = 0; x < LevelWidth; ++x) {
                // TODO: Seperate tilemap for water tiles?
                // This way we can apply shaders and also not have to worry about overwriting tiles
                if (staticTiles[x, y] == TileType.AIR) {
                    int level = waterCells[x, y].Level;
                    if (level == 0) {
                        SetCell(x, y, -1);
                    } else {
                        SetCell(x, y, 1, autotileCoord: GetWaterAutotileCoords(waterCells[x, y].Level));
                    }
                }
            }
        }
    }

}
