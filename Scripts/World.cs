using Godot;
using System;

public class World : Node
{
    private Level currentLevel;
    private Win winGUI;
    private Label waterLevelDebugLabel;
    private Label chiselsLabel;
    private Label tipLabel;

    private AudioStreamPlayer breakSFX;
    private AudioStreamPlayer outSFX;

    private Timer tickTimer;
    private Timer winTimer;

    private PackedScene winScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        winTimer = GetNode<Timer>("WinTimer");
        winTimer.Connect("timeout", this, nameof(WinTimerComplete));

        tickTimer = FindNode("TickTimer") as Timer;
        tickTimer.Connect("timeout", this, nameof(Tick));

        waterLevelDebugLabel = GetNode<Label>("WaterLevelDebugLabel");
        chiselsLabel = FindNode("ChiselsLabel") as Label;
        tipLabel = FindNode("TipLabel") as Label;

        breakSFX = GetNode<AudioStreamPlayer>("BreakSFX");
        outSFX = GetNode<AudioStreamPlayer>("OutSFX");

        winScene = GD.Load<PackedScene>("res://Scenes/Win.tscn");

        Resource chiselCursor = ResourceLoader.Load("res://Assets/ChiselCursor.png");
        Input.SetCustomMouseCursor(chiselCursor);

        LoadLevel(LevelManager.GetNextLevel());
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.Pressed && (ButtonList)mouseEvent.ButtonIndex == ButtonList.Left) {
                Vector2 clickedTile = currentLevel.GlobalToTile(mouseEvent.Position);
                GD.Print(clickedTile);
                ChiselTile((int)clickedTile.x, (int)clickedTile.y);
            }
        }
    }

    public override void _Process(float delta) {
        if (Input.IsActionJustPressed("restart")) {
            LoadLevel(LevelManager.GetCurrentLevel());
        }

        //Vector2 tileHover = currentLevel.GlobalToTile(GetGlobalMousePosition());
        //if (currentLevel.TryGetWaterLevel((int)tileHover.x, (int)tileHover.y, out int waterLevel)) {
        //    waterLevelDebugLabel.Text = waterLevel.ToString();
        //}
    }

    public void Tick() {
        currentLevel.Tick();
    }

    public void ChiselTile(int x, int y) {
        if (currentLevel.TryChisel(x, y)) {
            breakSFX.Play();
        } else {
            outSFX.Play();
        }
        UpdateChiselsLabel();
    }

    private void UpdateChiselsLabel() {
        chiselsLabel.Text = String.Format("{0}", currentLevel.ChiselsLeft);
    }

    private void UpdateTipLabel() {
        tipLabel.Text = currentLevel.Tip;
    }

    public void CompletedLevel(int points) {
        GD.Print("Completed: ", points);

        Win winSceneInstance = winScene.Instance() as Win;
        winGUI = winSceneInstance;
        winGUI.nextLevel = LevelManager.GetNextLevel();
        winGUI.Points = points;
        winGUI.Connect(nameof(Win.NextLevel), this, nameof(LoadLevel));

        winTimer.Start();
    }

    public void WinTimerComplete() {
        if (winGUI == null) return;
        AddChild(winGUI);
    }

    public void LoadLevel(PackedScene level) {
        if (level == null) {
            return;
        }

        if (winGUI != null) {
            RemoveChild(winGUI);
            winGUI = null;
        }

        Node instance = level.Instance();
        if (instance is Level) {
            Level levelInstance = level.Instance() as Level;
            levelInstance.Connect(nameof(Level.Completed), this, nameof(CompletedLevel));

            if (currentLevel != null) {
                RemoveChild(currentLevel);
            }
            AddChild(levelInstance);

            currentLevel = levelInstance;
            UpdateChiselsLabel();
            UpdateTipLabel();
        } else {
            GetTree().ChangeSceneTo(level);
        }
    }

}
