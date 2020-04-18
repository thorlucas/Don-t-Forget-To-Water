using Godot;
using System;

public class World : Node2D
{
    private Level level;
    private Timer tickTimer;
    private Label waterLevelDebugLabel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        level = FindNode("Level") as Level;
        tickTimer = FindNode("TickTimer") as Timer;
        waterLevelDebugLabel = GetNode<Label>("WaterLevelDebugLabel");

        tickTimer.Connect("timeout", this, nameof(Tick));
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.Pressed && (ButtonList)mouseEvent.ButtonIndex == ButtonList.Left) {
                Vector2 clickedTile = level.WorldToMap(level.ToLocal(mouseEvent.Position));
                GD.Print(clickedTile);
                ChiselTile((int)clickedTile.x, (int)clickedTile.y);
            }
        }
    }

    public override void _Process(float delta) {
        Vector2 tileHover = level.WorldToMap(level.ToLocal(GetGlobalMousePosition()));
        if (level.TryGetWaterLevel((int)tileHover.x, (int)tileHover.y, out int waterLevel)) {
            waterLevelDebugLabel.Text = waterLevel.ToString();
        }
    }

    public void Tick() {
        level.Tick();
    }

    public void ChiselTile(int x, int y) {
        //if (staticTiles[x, y] == TileType.STONE) {
        //    staticTiles[x, y] = TileType.AIR;
        //    UpdateTileMapStatic();
        //}
    }

}
