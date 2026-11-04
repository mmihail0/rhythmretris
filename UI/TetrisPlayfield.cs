using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tetris.Objects;
using osu.Game.Rulesets.Tetris.Objects.Drawables;
using osu.Game.Rulesets.Tetris.Scoring;
using osu.Game.Rulesets.UI;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Rulesets.Tetris.UI
{
    public partial class TetrisPlayfield : Playfield, IKeyBindingHandler<TetrisAction>
    {
        public const int Columns = 10;
        public const int Rows = 20;
        public const float CellSize = 24f;
        public const float FrameThickness = 3f;

        private const double das_delay = 135.0;
        private const double arr_interval = 28.0;
        private const double soft_drop_interval = 35.0;
        private const double lock_delay_duration = 500.0;
        private const int max_lock_resets = 15;

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
        private readonly Container ghostContainer;
        private readonly Container blockContainer;
        private readonly Container holdPieceContainer;
        private readonly Container[] nextPieceContainers = new Container[5];

        private readonly Container[,] visualGrid = new Container[Columns, Rows];
        private readonly Box[,] cellFillGrid = new Box[Columns, Rows];
        private readonly Box[,] cellBevelGrid = new Box[Columns, Rows];
        private readonly int[,] grid = new int[Columns, Rows];

        private readonly Random rng = new Random();
        private readonly List<int> bag = new List<int>();

        private int currentPiece = 1;
        private int currentRotation = 0;
        private Vector2I piecePos;

        private int heldPiece = 0;
        private bool canHold = true;

        private bool leftHeld;
        private bool rightHeld;
        private bool softDropHeld;
        private int moveDirection;
        private double moveHoldTimer;
        private double moveRepeatTimer;
        private double softDropTimer;

        private double lastDropTime = double.MinValue;
        private double lockDelayTimer;
        private int lockResetCount;
        private int lowestRowReached;

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
            new Color4(45, 215, 185, 255),  // I - TETR.IO Cyan/Teal
            new Color4(80, 95, 215, 255),   // J - TETR.IO Indigo/Blue
            new Color4(225, 130, 60, 255),  // L - TETR.IO Orange
            new Color4(225, 195, 55, 255),  // O - TETR.IO Yellow
            new Color4(140, 210, 55, 255),  // S - TETR.IO Lime Green
            new Color4(185, 75, 195, 255),  // T - TETR.IO Purple
            new Color4(220, 70, 75, 255)    // Z - TETR.IO Red
        };

        public TetrisPlayfield()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            float boardWidth = Columns * CellSize;
            float boardHeight = Rows * CellSize;
            float panelWidth = 116f;
            float headerHeight = 22f;
            float holdBodyHeight = 76f;
            float nextBodyHeight = 330f;

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
                        // Playfield dark background
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
                        ghostContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        blockContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        // Playfield Left white outline
                        new Box
                        {
                            Position = new Vector2(-FrameThickness, 0),
                            Size = new Vector2(FrameThickness, boardHeight + FrameThickness),
                            Colour = Color4.White
                        },
                        // Playfield Right white outline
                        new Box
                        {
                            Position = new Vector2(boardWidth, 0),
                            Size = new Vector2(FrameThickness, boardHeight + FrameThickness),
                            Colour = Color4.White
                        },
                        // Playfield Bottom white outline
                        new Box
                        {
                            Position = new Vector2(-FrameThickness, boardHeight),
                            Size = new Vector2(boardWidth + FrameThickness * 2, FrameThickness),
                            Colour = Color4.White
                        },
                        // Top subtle threshold line
                        new Box
                        {
                            Position = new Vector2(0, -1f),
                            Size = new Vector2(boardWidth, 1f),
                            Colour = Color4.White,
                            Alpha = 0.35f
                        },

                        // ==========================================
                        // HOLD PANEL (Top-Left, TETR.IO style)
                        // ==========================================
                        new Container
                        {
                            Position = new Vector2(-FrameThickness - panelWidth, 0),
                            Size = new Vector2(panelWidth, headerHeight + holdBodyHeight),
                            Children = new Drawable[]
                            {
                                // White Header Bar
                                new Box
                                {
                                    Size = new Vector2(panelWidth, headerHeight),
                                    Colour = Color4.White
                                },
                                new SpriteText
                                {
                                    Text = "HOLD",
                                    Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 17),
                                    Colour = Color4.Black,
                                    Anchor = Anchor.TopLeft,
                                    Origin = Anchor.CentreLeft,
                                    Position = new Vector2(6f, headerHeight / 2f)
                                },
                                // Dark Body
                                new Box
                                {
                                    Position = new Vector2(0, headerHeight),
                                    Size = new Vector2(panelWidth, holdBodyHeight),
                                    Colour = Color4.Black,
                                    Alpha = 0.85f
                                },
                                // Left Border
                                new Box
                                {
                                    Position = new Vector2(0, headerHeight),
                                    Size = new Vector2(FrameThickness, holdBodyHeight),
                                    Colour = Color4.White
                                },
                                // Bottom Border
                                new Box
                                {
                                    Position = new Vector2(0, headerHeight + holdBodyHeight - FrameThickness),
                                    Size = new Vector2(panelWidth, FrameThickness),
                                    Colour = Color4.White
                                },
                                // Bottom-Left Chamfer Accent
                                new Box
                                {
                                    Position = new Vector2(0, headerHeight + holdBodyHeight - 10f),
                                    Size = new Vector2(10f, 10f),
                                    Colour = Color4.White
                                },
                                // Hold Piece Preview Container
                                holdPieceContainer = new Container
                                {
                                    Position = new Vector2(panelWidth / 2f, headerHeight + holdBodyHeight / 2f),
                                    Origin = Anchor.Centre
                                }
                            }
                        },

                        // ==========================================
                        // NEXT PANEL (Top-Right, TETR.IO style)
                        // ==========================================
                        new Container
                        {
                            Position = new Vector2(boardWidth + FrameThickness, 0),
                            Size = new Vector2(panelWidth, headerHeight + nextBodyHeight),
                            Children = new Drawable[]
                            {
                                // White Header Bar
                                new Box
                                {
                                    Size = new Vector2(panelWidth, headerHeight),
                                    Colour = Color4.White
                                },
                                new SpriteText
                                {
                                    Text = "NEXT",
                                    Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 17),
                                    Colour = Color4.Black,
                                    Anchor = Anchor.TopLeft,
                                    Origin = Anchor.CentreLeft,
                                    Position = new Vector2(6f, headerHeight / 2f)
                                },
                                // Dark Body
                                new Box
                                {
                                    Position = new Vector2(0, headerHeight),
                                    Size = new Vector2(panelWidth, nextBodyHeight),
                                    Colour = Color4.Black,
                                    Alpha = 0.85f
                                },
                                // Right Border
                                new Box
                                {
                                    Position = new Vector2(panelWidth - FrameThickness, headerHeight),
                                    Size = new Vector2(FrameThickness, nextBodyHeight),
                                    Colour = Color4.White
                                },
                                // Bottom Border
                                new Box
                                {
                                    Position = new Vector2(0, headerHeight + nextBodyHeight - FrameThickness),
                                    Size = new Vector2(panelWidth, FrameThickness),
                                    Colour = Color4.White
                                },
                                // Bottom-Right Chamfer Accent
                                new Box
                                {
                                    Position = new Vector2(panelWidth - 10f, headerHeight + nextBodyHeight - 10f),
                                    Size = new Vector2(10f, 10f),
                                    Colour = Color4.White
                                },
                                new Container
                                {
                                    Position = new Vector2(0, headerHeight),
                                    Size = new Vector2(panelWidth, nextBodyHeight),
                                    Children = CreateNextSlotContainers(panelWidth, nextBodyHeight)
                                }
                            }
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
                    Box fill;
                    Box bevel;
                    var cell = new Container
                    {
                        Size = new Vector2(CellSize - 1),
                        Position = new Vector2(x * CellSize, y * CellSize),
                        Alpha = 0f,
                        Children = new Drawable[]
                        {
                            fill = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.White
                            },
                            bevel = new Box
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(CellSize - 9f),
                                Colour = Color4.White,
                                Alpha = 0.16f
                            }
                        }
                    };
                    visualGrid[x, y] = cell;
                    cellFillGrid[x, y] = fill;
                    cellBevelGrid[x, y] = bevel;
                    blockContainer.Add(cell);
                }
            }

            SpawnNextPiece();
        }

        private Drawable[] CreateNextSlotContainers(float panelWidth, float bodyHeight)
        {
            var slots = new Drawable[5];
            float slotSpacing = (bodyHeight - 16f) / 5f;
            for (int i = 0; i < 5; i++)
            {
                nextPieceContainers[i] = new Container
                {
                    Position = new Vector2(panelWidth / 2f, 10f + slotSpacing * (i + 0.5f)),
                    Origin = Anchor.Centre
                };
                slots[i] = nextPieceContainers[i];
            }
            return slots;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (isToppedOut)
                return false;

            if (!e.Repeat)
            {
                if (e.Key == Key.ShiftLeft || e.Key == Key.ShiftRight || e.Key == Key.C)
                {
                    HoldPiece();
                    return true;
                }

                if (e.Key == Key.Space)
                {
                    if (Time.Current >= 0)
                        HardDrop();
                    return true;
                }
            }

            return base.OnKeyDown(e);
        }

        public bool OnPressed(KeyBindingPressEvent<TetrisAction> e)
        {
            if (isToppedOut)
                return false;

            switch (e.Action)
            {
                case TetrisAction.MoveLeft:
                    leftHeld = true;
                    moveDirection = -1;
                    moveHoldTimer = 0;
                    moveRepeatTimer = 0;
                    Move(-1);
                    return true;

                case TetrisAction.MoveRight:
                    rightHeld = true;
                    moveDirection = 1;
                    moveHoldTimer = 0;
                    moveRepeatTimer = 0;
                    Move(1);
                    return true;

                case TetrisAction.RotateCW:
                    Rotate(1);
                    return true;

                case TetrisAction.RotateCCW:
                    Rotate(-1);
                    return true;

                case TetrisAction.SoftDrop:
                    softDropHeld = true;
                    softDropTimer = 0;
                    SoftDrop();
                    return true;

                case TetrisAction.HardDrop:
                    HardDrop();
                    return true;

                case TetrisAction.Hold:
                    HoldPiece();
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<TetrisAction> e)
        {
            switch (e.Action)
            {
                case TetrisAction.MoveLeft:
                    leftHeld = false;
                    if (rightHeld)
                    {
                        moveDirection = 1;
                        moveHoldTimer = 0;
                        moveRepeatTimer = 0;
                    }
                    else
                    {
                        moveDirection = 0;
                    }
                    break;

                case TetrisAction.MoveRight:
                    rightHeld = false;
                    if (leftHeld)
                    {
                        moveDirection = -1;
                        moveHoldTimer = 0;
                        moveRepeatTimer = 0;
                    }
                    else
                    {
                        moveDirection = 0;
                    }
                    break;

                case TetrisAction.SoftDrop:
                    softDropHeld = false;
                    break;
            }
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

        private void EnsureBagHasPieces(int minCount = 7)
        {
            while (bag.Count < minCount)
                RefillBag();
        }

        private void SpawnNextPiece()
        {
            EnsureBagHasPieces(7);
            currentPiece = bag[0];
            bag.RemoveAt(0);
            EnsureBagHasPieces(7);

            canHold = true;
            ResetPieceToTop();
            UpdateQueuePreviews();
            RedrawGrid();
        }

        private void ResetPieceToTop()
        {
            currentRotation = 0;
            piecePos = new Vector2I(3, 0);
            lockDelayTimer = 0;
            lockResetCount = 0;
            lowestRowReached = 0;
            lastDropTime = Time.Current;

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
        }

        public void HoldPiece()
        {
            if (!canHold || isToppedOut)
                return;

            canHold = false;

            if (heldPiece == 0)
            {
                heldPiece = currentPiece;
                EnsureBagHasPieces(7);
                currentPiece = bag[0];
                bag.RemoveAt(0);
                EnsureBagHasPieces(7);
            }
            else
            {
                int swap = currentPiece;
                currentPiece = heldPiece;
                heldPiece = swap;
            }

            ResetPieceToTop();
            UpdateQueuePreviews();
            RedrawGrid();
        }

        private void UpdateQueuePreviews()
        {
            RenderMiniPiece(holdPieceContainer, heldPiece, canHold);

            for (int i = 0; i < 5; i++)
            {
                int nextPiece = i < bag.Count ? bag[i] : 0;
                RenderMiniPiece(nextPieceContainers[i], nextPiece, true);
            }
        }

        private static void RenderMiniPiece(Container target, int pieceId, bool activeColor)
        {
            target.Clear();
            if (pieceId <= 0 || pieceId >= Tetrominoes.Length)
                return;

            var shape = Tetrominoes[pieceId][0];
            int size = shape.GetLength(0);

            int minR = size, maxR = -1, minC = size, maxC = -1;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        if (r < minR) minR = r;
                        if (r > maxR) maxR = r;
                        if (c < minC) minC = c;
                        if (c > maxC) maxC = c;
                    }
                }
            }

            if (maxR < minR || maxC < minC)
                return;

            const float mini_cell = 18f;
            float width = (maxC - minC + 1) * mini_cell;
            float height = (maxR - minR + 1) * mini_cell;
            Color4 baseColor = activeColor ? PieceColors[pieceId] : new Color4(95, 95, 95, 255);

            for (int r = minR; r <= maxR; r++)
            {
                for (int c = minC; c <= maxC; c++)
                {
                    if (shape[r, c] == 0)
                        continue;

                    float px = (c - minC) * mini_cell - width / 2f;
                    float py = (r - minR) * mini_cell - height / 2f;

                    target.Add(new Container
                    {
                        Position = new Vector2(px, py),
                        Size = new Vector2(mini_cell - 1f),
                        Alpha = activeColor ? 1f : 0.75f,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = baseColor
                            },
                            new Box
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(mini_cell - 7f),
                                Colour = Color4.White,
                                Alpha = 0.16f
                            }
                        }
                    });
                }
            }
        }

        private void EnsureDifficultyMultiplier()
        {
            if (multiplierCalculated)
                return;

            var activeBeatmap = Beatmap ?? workingBeatmap?.Value?.Beatmap;
            if (activeBeatmap == null)
                return;

            multiplierCalculated = true;

            double npsScale = 1.0;
            if (activeBeatmap.HitObjects.Count > 1)
            {
                double durationSeconds = Math.Max((activeBeatmap.HitObjects[^1].StartTime - activeBeatmap.HitObjects[0].StartTime) / 1000.0, 10.0);
                double nps = activeBeatmap.HitObjects.Count / durationSeconds;
                npsScale = 1.0 + Math.Clamp(nps * 0.12, 0.05, 1.5);
            }

            densityGravityMultiplier = 1.7 * npsScale;
        }

        protected override void Update()
        {
            base.Update();

            if (isToppedOut)
                return;

            EnsureDifficultyMultiplier();

            double dt = Math.Max(Time.Elapsed, 0);

            if (moveDirection != 0)
            {
                moveHoldTimer += dt;
                if (moveHoldTimer >= das_delay)
                {
                    moveRepeatTimer += dt;
                    while (moveRepeatTimer >= arr_interval)
                    {
                        moveRepeatTimer -= arr_interval;
                        Move(moveDirection);
                    }
                }
            }

            if (softDropHeld)
            {
                softDropTimer += dt;
                while (softDropTimer >= soft_drop_interval)
                {
                    softDropTimer -= soft_drop_interval;
                    var nextPos = new Vector2I(piecePos.X, piecePos.Y + 1);
                    if (!CheckCollision(nextPos, currentRotation))
                    {
                        piecePos = nextPos;
                        if (piecePos.Y > lowestRowReached)
                        {
                            lowestRowReached = piecePos.Y;
                            lockResetCount = 0;
                        }
                        lockDelayTimer = 0;
                        lastDropTime = Time.Current;
                        RedrawGrid();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            var controlPointInfo = Beatmap?.ControlPointInfo ?? workingBeatmap?.Value?.Beatmap?.ControlPointInfo;
            TimingControlPoint timingPoint = controlPointInfo?.TimingPointAt(Time.Current) ?? TimingControlPoint.DEFAULT;
            double stepInterval = timingPoint.BeatLength / densityGravityMultiplier;

            if (Time.Current >= 0)
            {
                if (IsPieceGrounded())
                {
                    lockDelayTimer += dt;
                    if (lockDelayTimer >= lock_delay_duration)
                    {
                        LockPiece();
                        lastDropTime = Time.Current;
                    }
                }
                else
                {
                    lockDelayTimer = 0;
                    if (Time.Current - lastDropTime >= stepInterval)
                    {
                        StepGravity();
                        lastDropTime = Time.Current;
                    }
                }
            }
        }

        private bool IsPieceGrounded()
            => CheckCollision(new Vector2I(piecePos.X, piecePos.Y + 1), currentRotation);

        private void OnPieceManipulated()
        {
            if (piecePos.Y > lowestRowReached)
            {
                lowestRowReached = piecePos.Y;
                lockResetCount = 0;
                lockDelayTimer = 0;
            }
            else if (lockResetCount < max_lock_resets)
            {
                lockResetCount++;
                lockDelayTimer = 0;
            }
        }

        public void Move(int dx)
        {
            var target = new Vector2I(piecePos.X + dx, piecePos.Y);
            if (!CheckCollision(target, currentRotation))
            {
                piecePos = target;
                OnPieceManipulated();
                RedrawGrid();
            }
        }

        private static readonly Vector2I[] WallKickOffsets = new[]
        {
            new Vector2I(0, 0),
            new Vector2I(1, 0),
            new Vector2I(-1, 0),
            new Vector2I(2, 0),
            new Vector2I(-2, 0),
            new Vector2I(0, -1),
            new Vector2I(1, -1),
            new Vector2I(-1, -1),
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(-1, 1),
            new Vector2I(0, -2)
        };

        public void Rotate(int dir)
        {
            int nextRot = (currentRotation + dir + 4) % 4;
            foreach (var kick in WallKickOffsets)
            {
                var candidate = new Vector2I(piecePos.X + kick.X, piecePos.Y + kick.Y);
                if (!CheckCollision(candidate, nextRot))
                {
                    piecePos = candidate;
                    currentRotation = nextRot;
                    OnPieceManipulated();
                    RedrawGrid();
                    return;
                }
            }
        }

        public void SoftDrop()
        {
            var target = new Vector2I(piecePos.X, piecePos.Y + 1);
            if (!CheckCollision(target, currentRotation))
            {
                piecePos = target;
                if (piecePos.Y > lowestRowReached)
                {
                    lowestRowReached = piecePos.Y;
                    lockResetCount = 0;
                }
                lockDelayTimer = 0;
                lastDropTime = Time.Current;
                RedrawGrid();
            }
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
                if (piecePos.Y > lowestRowReached)
                {
                    lowestRowReached = piecePos.Y;
                    lockResetCount = 0;
                }
                lockDelayTimer = 0;
                RedrawGrid();
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

        private Vector2I GetGhostPiecePosition()
        {
            var ghostPos = piecePos;
            while (!CheckCollision(new Vector2I(ghostPos.X, ghostPos.Y + 1), currentRotation))
                ghostPos.Y++;
            return ghostPos;
        }

        private static Drawable CreateHollowGhostCell(float x, float y)
        {
            float size = CellSize - 1f;
            float outerBorder = 2.5f;
            float innerRingSize = size - 7f;
            float innerHoleSize = innerRingSize - 3f;
            Color4 ghostColor = new Color4(125, 125, 125, 255);

            return new Container
            {
                Position = new Vector2(x, y),
                Size = new Vector2(size),
                Alpha = 0.55f,
                Children = new Drawable[]
                {
                    // Outer frame
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ghostColor
                    },
                    // Dark cut-out inside outer frame
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(size - outerBorder * 2f),
                        Colour = Color4.Black,
                        Alpha = 0.9f
                    },
                    // Inner bevel ring
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(innerRingSize),
                        Colour = ghostColor,
                        Alpha = 0.7f
                    },
                    // Hollow center
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(innerHoleSize),
                        Colour = Color4.Black,
                        Alpha = 0.95f
                    }
                }
            };
        }

        private void RedrawGrid()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int val = grid[x, y];
                    cellFillGrid[x, y].Colour = PieceColors[val];
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

            // Render TETR.IO hollowed-out ghost piece at landing position
            ghostContainer.Clear();
            var ghostPos = GetGhostPiecePosition();
            if (ghostPos.Y != piecePos.Y)
            {
                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        if (shape[r, c] != 0)
                        {
                            int gx = ghostPos.X + c;
                            int gy = ghostPos.Y + r;

                            if (gx >= 0 && gx < Columns && gy >= 0 && gy < Rows)
                            {
                                ghostContainer.Add(CreateHollowGhostCell(gx * CellSize, gy * CellSize));
                            }
                        }
                    }
                }
            }

            // Render active falling piece
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
                            cellFillGrid[gx, gy].Colour = PieceColors[currentPiece];
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
