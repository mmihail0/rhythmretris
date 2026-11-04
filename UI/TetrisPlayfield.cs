using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tetris.Objects;
using osu.Game.Rulesets.Tetris.Objects.Drawables;
using osu.Game.Rulesets.Tetris.Scoring;
using osu.Game.Rulesets.UI;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Tetris.UI
{
    public partial class TetrisPlayfield : Playfield, IKeyBindingHandler<TetrisAction>
    {
        public const int Columns = 10;
        public const int Rows = 20;
        public const float CellSize = 24f;
        public const float BorderThickness = 3f;

        [Resolved(CanBeNull = true)]
        private IBindable<WorkingBeatmap>? workingBeatmap { get; set; }

        [Resolved(CanBeNull = true)]
        private HealthProcessor? healthProcessor { get; set; }

        public IBeatmap? Beatmap { get; set; }

        public bool IsHidden { get; set; }

        private bool isToppedOut;
        private double densityGravityMultiplier = 1.0;
        private bool multiplierCalculated;

        private readonly Container boardContainer;
        private readonly Container gridLinesContainer;
        private readonly Container blockContainer;
        private readonly Box[,] visualGrid = new Box[Columns, Rows];
        private readonly int[,] grid = new int[Columns, Rows];

        private readonly Random rng = new Random();
        private readonly List<int> bag = new List<int>();

        private int currentPiece = 1;
        private int currentRotation = 0;
        private Vector2I piecePos;

        private double lastDropTime = double.MinValue;

        private static readonly int[][][,] Tetrominoes = new int[][][,]
        {
            new int[][,] { },
            // I (Cyan)
            new int[][,] {
                new int[,] { {0,0,0,0}, {1,1,1,1}, {0,0,0,0}, {0,0,0,0} },
                new int[,] { {0,0,1,0}, {0,0,1,0}, {0,0,1,0}, {0,0,1,0} },
                new int[,] { {0,0,0,0}, {0,0,0,0}, {1,1,1,1}, {0,0,0,0} },
                new int[,] { {0,1,0,0}, {0,1,0,0}, {0,1,0,0}, {0,1,0,0} }
            },
            // J (Blue)
            new int[][,] {
                new int[,] { {1,0,0}, {1,1,1}, {0,0,0} },
                new int[,] { {0,1,1}, {0,1,0}, {0,1,0} },
                new int[,] { {0,0,0}, {1,1,1}, {0,0,1} },
                new int[,] { {0,1,0}, {0,1,0}, {1,1,0} }
            },
            // L (Orange)
            new int[][,] {
                new int[,] { {0,0,1}, {1,1,1}, {0,0,0} },
                new int[,] { {0,1,0}, {0,1,0}, {0,1,1} },
                new int[,] { {0,0,0}, {1,1,1}, {1,0,0} },
                new int[,] { {1,1,0}, {0,1,0}, {0,1,0} }
            },
            // O (Yellow)
            new int[][,] {
                new int[,] { {1,1}, {1,1} },
                new int[,] { {1,1}, {1,1} },
                new int[,] { {1,1}, {1,1} },
                new int[,] { {1,1}, {1,1} }
            },
            // S (Green)
            new int[][,] {
                new int[,] { {0,1,1}, {1,1,0}, {0,0,0} },
                new int[,] { {0,1,0}, {0,1,1}, {0,0,1} },
                new int[,] { {0,0,0}, {0,1,1}, {1,1,0} },
                new int[,] { {1,0,0}, {1,1,0}, {0,1,0} }
            },
            // T (Purple)
            new int[][,] {
                new int[,] { {0,1,0}, {1,1,1}, {0,0,0} },
                new int[,] { {0,1,0}, {0,1,1}, {0,1,0} },
                new int[,] { {0,0,0}, {1,1,1}, {0,1,0} },
                new int[,] { {0,1,0}, {1,1,0}, {0,1,0} }
            },
            // Z (Red)
            new int[][,] {
                new int[,] { {1,1,0}, {0,1,1}, {0,0,0} },
                new int[,] { {0,0,1}, {0,1,1}, {0,1,0} },
                new int[,] { {0,0,0}, {1,1,0}, {0,1,1} },
                new int[,] { {0,1,0}, {1,1,0}, {1,0,0} }
            }
        };

        private static readonly Color4[] PieceColors = new[]
        {
            Color4.Transparent,
            Color4.Cyan,
            Color4.Blue,
            Color4.Orange,
            Color4.Yellow,
            Color4.LimeGreen,
            Color4.Purple,
            Color4.Red
        };

        public TetrisPlayfield()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            float boardWidth = Columns * CellSize;
            float boardHeight = Rows * CellSize;

            InternalChildren = new Drawable[]
            {
                HitObjectContainer,
                boardContainer = new Container
                {
                    Size = new Vector2(boardWidth, boardHeight),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black,
                            Alpha = 0.85f
                        },
                        gridLinesContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        blockContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        // Left white outline (TETR.IO style open-top or full frame)
                        new Box
                        {
                            Position = new Vector2(-BorderThickness, 0),
                            Size = new Vector2(BorderThickness, boardHeight + BorderThickness),
                            Colour = Color4.White
                        },
                        // Right white outline
                        new Box
                        {
                            Position = new Vector2(boardWidth, 0),
                            Size = new Vector2(BorderThickness, boardHeight + BorderThickness),
                            Colour = Color4.White
                        },
                        // Bottom white outline
                        new Box
                        {
                            Position = new Vector2(-BorderThickness, boardHeight),
                            Size = new Vector2(boardWidth + BorderThickness * 2, BorderThickness),
                            Colour = Color4.White
                        },
                        // Top subtle threshold line
                        new Box
                        {
                            Position = new Vector2(0, -1f),
                            Size = new Vector2(boardWidth, 1f),
                            Colour = Color4.White,
                            Alpha = 0.35f
                        }
                    }
                }
            };

            // Subtle inner grid lines like TETR.IO
            for (int x = 1; x < Columns; x++)
            {
                gridLinesContainer.Add(new Box
                {
                    Position = new Vector2(x * CellSize, 0),
                    Size = new Vector2(1f, boardHeight),
                    Colour = Color4.White,
                    Alpha = 0.08f
                });
            }

            for (int y = 1; y < Rows; y++)
            {
                gridLinesContainer.Add(new Box
                {
                    Position = new Vector2(0, y * CellSize),
                    Size = new Vector2(boardWidth, 1f),
                    Colour = Color4.White,
                    Alpha = 0.08f
                });
            }

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    var cell = new Box
                    {
                        Size = new Vector2(CellSize - 1),
                        Position = new Vector2(x * CellSize, y * CellSize),
                        Colour = Color4.Transparent,
                        Alpha = 0f
                    };
                    visualGrid[x, y] = cell;
                    blockContainer.Add(cell);
                }
            }

            SpawnNextPiece();
        }

        public bool OnPressed(KeyBindingPressEvent<TetrisAction> e)
        {
            if (isToppedOut)
                return false;

            switch (e.Action)
            {
                case TetrisAction.MoveLeft:
                    Move(-1);
                    return true;

                case TetrisAction.MoveRight:
                    Move(1);
                    return true;

                case TetrisAction.RotateCW:
                    Rotate(1);
                    return true;

                case TetrisAction.RotateCCW:
                    Rotate(-1);
                    return true;

                case TetrisAction.SoftDrop:
                    SoftDrop();
                    return true;

                case TetrisAction.HardDrop:
                    HardDrop();
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<TetrisAction> e)
        {
        }

        private void RefillBag()
        {
            var pieces = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
            while (pieces.Count > 0)
            {
                int index = rng.Next(pieces.Count);
                bag.Add(pieces[index]);
                pieces.RemoveAt(index);
            }
        }

        private void SpawnNextPiece()
        {
            if (bag.Count == 0) RefillBag();
            currentPiece = bag[0];
            bag.RemoveAt(0);

            currentRotation = 0;
            piecePos = new Vector2I(3, 0);

            if (CheckCollision(piecePos, currentRotation))
            {
                if (healthProcessor is TetrisHealthProcessor tetrisHealth)
                {
                    tetrisHealth.TriggerTopOut();
                }
                else if (healthProcessor != null)
                {
                    healthProcessor.Health.Value = 0;
                }

                JudgeNearestHitObject();

                if (healthProcessor?.HasFailed == true)
                {
                    isToppedOut = true;
                    return;
                }

                Array.Clear(grid, 0, grid.Length);
            }

            RedrawGrid();
        }

        private void EnsureDifficultyMultiplier()
        {
            if (multiplierCalculated)
                return;

            var activeBeatmap = Beatmap ?? workingBeatmap?.Value?.Beatmap;
            if (activeBeatmap == null)
                return;

            multiplierCalculated = true;

            // Base 1.5x speed boost across all difficulties, plus scaling with beatmap note density (NPS)
            // so higher star difficulties of the same song fall noticeably faster than Easy/Normal.
            double npsScale = 1.0;
            if (activeBeatmap.HitObjects.Count > 1)
            {
                double durationSeconds = Math.Max((activeBeatmap.HitObjects[^1].StartTime - activeBeatmap.HitObjects[0].StartTime) / 1000.0, 10.0);
                double nps = activeBeatmap.HitObjects.Count / durationSeconds;
                // e.g. 1 NPS (Easy) -> ~1.12x, 3 NPS (Normal/Hard) -> ~1.36x, 5 NPS (Insane) -> ~1.6x, 8 NPS (Extra) -> ~1.96x
                npsScale = 1.0 + Math.Clamp(nps * 0.12, 0.05, 1.5);
            }

            densityGravityMultiplier = 1.5 * npsScale;
        }

        protected override void Update()
        {
            base.Update();

            if (isToppedOut)
                return;

            EnsureDifficultyMultiplier();

            var controlPointInfo = Beatmap?.ControlPointInfo ?? workingBeatmap?.Value?.Beatmap?.ControlPointInfo;
            TimingControlPoint timingPoint = controlPointInfo?.TimingPointAt(Time.Current) ?? TimingControlPoint.DEFAULT;
            double stepInterval = timingPoint.BeatLength / densityGravityMultiplier;

            if (Time.Current - lastDropTime >= stepInterval)
            {
                StepGravity();
                lastDropTime = Time.Current;
            }
        }

        public void Move(int dx)
        {
            var target = new Vector2I(piecePos.X + dx, piecePos.Y);
            if (!CheckCollision(target, currentRotation))
            {
                piecePos = target;
                RedrawGrid();
            }
        }

        public void Rotate(int dir)
        {
            int nextRot = (currentRotation + dir + 4) % 4;
            if (!CheckCollision(piecePos, nextRot))
            {
                currentRotation = nextRot;
                RedrawGrid();
            }
        }

        public void SoftDrop()
        {
            StepGravity();
        }

        public void HardDrop()
        {
            while (!CheckCollision(new Vector2I(piecePos.X, piecePos.Y + 1), currentRotation))
                piecePos.Y++;

            LockPiece();
        }

        private void StepGravity()
        {
            var target = new Vector2I(piecePos.X, piecePos.Y + 1);
            if (!CheckCollision(target, currentRotation))
            {
                piecePos = target;
                RedrawGrid();
            }
            else
            {
                LockPiece();
            }
        }

        private void LockPiece()
        {
            var shape = Tetrominoes[currentPiece][currentRotation];
            int size = shape.GetLength(0);

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        int gx = piecePos.X + c;
                        int gy = piecePos.Y + r;
                        if (gx >= 0 && gx < Columns && gy >= 0 && gy < Rows)
                            grid[gx, gy] = currentPiece;
                    }
                }
            }

            JudgeNearestHitObject();
            ClearLines();
            SpawnNextPiece();
        }

        private void JudgeNearestHitObject()
        {
            DrawableTetrisHitObject? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var alive in HitObjectContainer.AliveObjects)
            {
                if (alive is DrawableTetrisHitObject dho && !dho.Result.HasResult)
                {
                    double dist = Math.Abs(Time.Current - dho.HitObject.StartTime);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = dho;
                    }
                }
            }

            nearest?.TriggerUserHit();
        }

        private void ClearLines()
        {
            for (int y = Rows - 1; y >= 0; y--)
            {
                bool full = true;
                for (int x = 0; x < Columns; x++)
                {
                    if (grid[x, y] == 0)
                    {
                        full = false;
                        break;
                    }
                }

                if (full)
                {
                    for (int pullY = y; pullY > 0; pullY--)
                        for (int x = 0; x < Columns; x++)
                            grid[x, pullY] = grid[x, pullY - 1];

                    for (int x = 0; x < Columns; x++)
                        grid[x, 0] = 0;

                    y++;
                }
            }
        }

        private bool CheckCollision(Vector2I pos, int rot)
        {
            var shape = Tetrominoes[currentPiece][rot];
            int size = shape.GetLength(0);

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (shape[r, c] == 0) continue;

                    int gx = pos.X + c;
                    int gy = pos.Y + r;

                    if (gx < 0 || gx >= Columns || gy >= Rows) return true;
                    if (gy >= 0 && grid[gx, gy] != 0) return true;
                }
            }
            return false;
        }

        private void RedrawGrid()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int val = grid[x, y];
                    visualGrid[x, y].Colour = PieceColors[val];
                    if (val == 0)
                    {
                        visualGrid[x, y].Alpha = 0f;
                    }
                    else
                    {
                        visualGrid[x, y].Alpha = IsHidden ? 0.08f : 1f;
                    }
                }
            }

            var shape = Tetrominoes[currentPiece][currentRotation];
            int size = shape.GetLength(0);

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        int gx = piecePos.X + c;
                        int gy = piecePos.Y + r;

                        if (gx >= 0 && gx < Columns && gy >= 0 && gy < Rows)
                        {
                            visualGrid[gx, gy].Colour = PieceColors[currentPiece];
                            visualGrid[gx, gy].Alpha = 1f;
                        }
                    }
                }
            }
        }
    }

    public struct Vector2I
    {
        public int X;
        public int Y;
        public Vector2I(int x, int y) { X = x; Y = y; }
    }
}
