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
using osu.Game.Rulesets.Objects;
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

        private const double das_delay = 165.0;
        private const double arr_interval = 42.0;
        private const double soft_drop_interval = 35.0;
        private const double lock_delay_duration = 500.0;
        private const int max_lock_resets = 15;

        [Resolved(CanBeNull = true)]
        private IBindable<WorkingBeatmap>? workingBeatmap { get; set; }

        [Resolved(CanBeNull = true)]
        private HealthProcessor? healthProcessor { get; set; }

        public IBeatmap? Beatmap { get; set; }

        public bool IsHidden { get; set; }

        public bool IsHardRock { get; set; }

        private bool isToppedOut;
        private bool isQueueDepleting;
        private bool isSongFinished;
        private bool isWaitingForNoteSpawn = true;
        private double nextSpawnNoteTime = double.MinValue;
        private double densityGravityMultiplier = 1.0;
        private double baseNps = 2.0;
        private double[] noteTimes = Array.Empty<double>();
        private int lastProcessedNoteIndex = -1;
        private bool multiplierCalculated;

        private readonly Container boardContainer;
        private readonly Container gridLinesContainer;
        private readonly Container ghostContainer;
        private readonly Container blockContainer;
        private readonly Container holdPieceContainer;
        private readonly Box progressBarFill;
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
            DrawableTetrisHitObject.IsDangerState = false;
            DrawableTetrisHitObject.PendingLockResult = null;

            float boardWidth = Columns * CellSize;
            float boardHeight = Rows * CellSize;
            float meterWidth = 10f;
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
                        // LEFT VERTICAL PROGRESS METER BAR (TETR.IO slot)
                        // ==========================================
                        new Container
                        {
                            Position = new Vector2(-FrameThickness - meterWidth, 0),
                            Size = new Vector2(meterWidth, boardHeight + FrameThickness),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Position = new Vector2(FrameThickness, 0),
                                    Size = new Vector2(meterWidth - FrameThickness, boardHeight),
                                    Colour = Color4.Black,
                                    Alpha = 0.85f
                                },
                                new Box
                                {
                                    Position = new Vector2(FrameThickness, 0),
                                    Size = new Vector2(meterWidth - FrameThickness, boardHeight),
                                    Anchor = Anchor.TopLeft,
                                    Origin = Anchor.TopLeft,
                                    Alpha = 0f
                                },
                                progressBarFill = new Box
                                {
                                    Position = new Vector2(FrameThickness, boardHeight),
                                    Anchor = Anchor.TopLeft,
                                    Origin = Anchor.BottomLeft,
                                    Size = new Vector2(meterWidth - FrameThickness, 0f),
                                    Colour = new Color4(220, 240, 255, 235)
                                },
                                // Meter Left White Border
                                new Box
                                {
                                    Position = new Vector2(0, 0),
                                    Size = new Vector2(FrameThickness, boardHeight + FrameThickness),
                                    Colour = Color4.White
                                },
                                // Meter Bottom White Border
                                new Box
                                {
                                    Position = new Vector2(0, boardHeight),
                                    Size = new Vector2(meterWidth, FrameThickness),
                                    Colour = Color4.White
                                }
                            }
                        },

                        // ==========================================
                        // HOLD PANEL (Top-Left, TETR.IO style)
                        // ==========================================
                        new Container
                        {
                            Position = new Vector2(-FrameThickness - meterWidth - panelWidth, 0),
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

            currentPiece = 0;
            isWaitingForNoteSpawn = true;
            EnsureBagHasPieces(7);
            UpdateQueuePreviews();
            RedrawGrid();
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
            if (isToppedOut || isSongFinished || currentPiece <= 0)
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
            if (isToppedOut || isSongFinished || currentPiece <= 0)
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
            if (isQueueDepleting || isSongFinished)
                return;

            while (bag.Count < minCount)
                RefillBag();
        }

        private bool ShouldDeferSpawnForPause(double currentTime, out double resumeTime)
        {
            resumeTime = currentTime;
            if (noteTimes.Length == 0)
                return false;

            // 1. Pre-song intro before the very first note
            if (currentTime < noteTimes[0] - 150.0)
            {
                resumeTime = noteTimes[0] - 150.0;
                return true;
            }

            // 2. Official osu! beatmap breaks (where the mapper explicitly placed a BreakPeriod)
            var activeBeatmap = Beatmap ?? workingBeatmap?.Value?.Beatmap;
            if (activeBeatmap?.Breaks != null)
            {
                foreach (var brk in activeBeatmap.Breaks)
                {
                    if (brk.HasEffect && currentTime >= brk.StartTime && currentTime < brk.EndTime - 250.0)
                    {
                        resumeTime = brk.EndTime - 250.0;
                        return true;
                    }
                }
            }

            return false;
        }

        private void SpawnNextPiece()
        {
            if (isSongFinished)
                return;

            EnsureBagHasPieces(7);

            bool pastLastNote = Clock != null && noteTimes.Length > 0 && Time.Current >= noteTimes[^1];
            if (bag.Count == 0 || pastLastNote)
            {
                isSongFinished = true;
                isWaitingForNoteSpawn = false;
                currentPiece = 0;
                bag.Clear();
                UpdateQueuePreviews();
                RedrawGrid();
                return;
            }

            if (Clock != null && ShouldDeferSpawnForPause(Time.Current, out double resumeTime))
            {
                currentPiece = 0;
                isWaitingForNoteSpawn = true;
                nextSpawnNoteTime = resumeTime;
                UpdateQueuePreviews();
                RedrawGrid();
                return;
            }

            isWaitingForNoteSpawn = false;
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
            if (Clock != null)
                lastDropTime = Time.Current;

            if (CheckCollision(piecePos, currentRotation))
            {
                DrawableTetrisHitObject.IsDangerState = true;

                if (healthProcessor is TetrisHealthProcessor tetrisHealth)
                {
                    tetrisHealth.TriggerTopOut();
                }
                else if (healthProcessor != null)
                {
                    healthProcessor.Health.Value = 0;
                }

                TriggerNearestHitObjectWithResult(HitResult.Miss);

                if (healthProcessor?.HasFailed == true)
                {
                    isToppedOut = true;
                    return;
                }

                Array.Clear(grid, 0, grid.Length);
                UpdateDangerState();
            }
        }

        public void HoldPiece()
        {
            if (!canHold || isToppedOut || isSongFinished || currentPiece <= 0)
                return;

            if (heldPiece == 0 && bag.Count == 0 && isQueueDepleting)
                return;

            canHold = false;

            if (heldPiece == 0)
            {
                heldPiece = currentPiece;
                EnsureBagHasPieces(7);
                if (bag.Count == 0)
                {
                    isSongFinished = true;
                    currentPiece = 0;
                    UpdateQueuePreviews();
                    RedrawGrid();
                    return;
                }
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

            int visibleSlots = IsHardRock ? 3 : 5;
            for (int i = 0; i < 5; i++)
            {
                int nextPiece = (i < visibleSlots && i < bag.Count) ? bag[i] : 0;
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

            int count = activeBeatmap.HitObjects.Count;
            if (count > 0)
            {
                var expandedTimes = new List<double>(count * 2);
                for (int i = 0; i < count; i++)
                {
                    var ho = activeBeatmap.HitObjects[i];
                    expandedTimes.Add(ho.StartTime);

                    double endTime = ho is TetrisHitObject tho ? tho.EndTime : ho.GetEndTime();
                    if (endTime > ho.StartTime + 120.0)
                    {
                        double beatLen = activeBeatmap.ControlPointInfo.TimingPointAt(ho.StartTime).BeatLength;
                        double tickStep = Math.Clamp(beatLen * 0.5, 100.0, 350.0);
                        for (double t = ho.StartTime + tickStep; t <= endTime + 5.0; t += tickStep)
                            expandedTimes.Add(t);
                    }
                }

                expandedTimes.Sort();
                noteTimes = expandedTimes.ToArray();
            }

            double npsScale = 1.0;
            if (count > 1 && noteTimes.Length > 1)
            {
                double durationSeconds = Math.Max((noteTimes[^1] - noteTimes[0]) / 1000.0, 10.0);
                baseNps = count / durationSeconds;
                npsScale = 1.0 + Math.Clamp(baseNps * 0.12, 0.05, 1.5);
            }

            densityGravityMultiplier = 2.0 * npsScale;

            if (noteTimes.Length > 0 && isWaitingForNoteSpawn && nextSpawnNoteTime == double.MinValue)
                nextSpawnNoteTime = noteTimes[0] - 150.0;
        }

        private double GetDynamicGravityMultiplier(double currentTime, ControlPointInfo? controlPointInfo)
        {
            double hardRockSpeed = IsHardRock ? 1.30 : 1.0;

            if (noteTimes.Length == 0)
                return densityGravityMultiplier * hardRockSpeed;

            // Pre-first-note intro: start calm and ramp up toward the first note
            if (currentTime < noteTimes[0])
            {
                double timeUntilFirst = noteTimes[0] - currentTime;
                double introFactor = 0.40 + 0.45 * (1.0 - Math.Clamp(timeUntilFirst / 2000.0, 0.0, 1.0));
                return densityGravityMultiplier * introFactor * hardRockSpeed;
            }

            // Post-final-note outro: coast to a gentle crawl once the chart finishes
            if (currentTime >= noteTimes[^1])
            {
                double timeSinceLast = currentTime - noteTimes[^1];
                double postOutroFactor = 0.40 - 0.25 * Math.Clamp(timeSinceLast / 1200.0, 0.0, 1.0);
                return densityGravityMultiplier * postOutroFactor * hardRockSpeed;
            }

            int search = Array.BinarySearch(noteTimes, currentTime);
            int nextIdx = search >= 0 ? search : ~search;

            double prevTime = nextIdx > 0 ? noteTimes[nextIdx - 1] : noteTimes[0];
            double nextTime = nextIdx < noteTimes.Length ? noteTimes[nextIdx] : noteTimes[^1];
            double gapMs = Math.Max(nextTime - prevTime, 50.0);

            double sectionScale;

            // Break / long gap detection (>= 1.6s between notes)
            if (gapMs >= 1600.0)
            {
                double timeUntilNext = nextTime - currentTime;
                if (timeUntilNext > 600.0)
                {
                    sectionScale = 0.35;
                }
                else
                {
                    // Ramp back up over the final 600ms before the next section begins
                    sectionScale = 0.35 + 0.55 * (1.0 - Math.Clamp(timeUntilNext / 600.0, 0.0, 1.0));
                }
            }
            else
            {
                // Local note density in a 2-second rolling window around currentTime
                int winStart = Array.BinarySearch(noteTimes, currentTime - 700.0);
                if (winStart < 0) winStart = ~winStart;

                int winEnd = Array.BinarySearch(noteTimes, currentTime + 1300.0);
                if (winEnd < 0) winEnd = ~winEnd;

                double windowNps = Math.Max(winEnd - winStart, 0) / 2.0;
                double instantNps = 1000.0 / Math.Clamp(gapMs, 65.0, 2000.0);
                double localNps = windowNps * 0.65 + instantNps * 0.35;

                double intensityRatio = localNps / Math.Max(baseNps, 0.8);
                sectionScale = Math.Clamp(0.42 + 0.58 * Math.Pow(intensityRatio, 1.15), 0.42, 1.85);
            }

            // Boost intensity during Kiai chorus/climax sections
            bool isKiai = controlPointInfo?.EffectPointAt(currentTime).KiaiMode ?? false;
            if (isKiai)
                sectionScale *= 1.22;

            // End-of-song wind-down: let the player feel the beatmap concluding over the final 7 seconds / 10 notes
            double timeRemaining = noteTimes[^1] - currentTime;
            int notesRemaining = noteTimes.Length - nextIdx;
            double outroFactor = 1.0;

            if (timeRemaining <= 7000.0)
            {
                double t = Math.Clamp(timeRemaining / 7000.0, 0.0, 1.0);
                outroFactor = Math.Min(outroFactor, 0.42 + 0.58 * t);
            }

            if (notesRemaining <= 10)
            {
                double n = Math.Clamp(notesRemaining / 10.0, 0.0, 1.0);
                outroFactor = Math.Min(outroFactor, 0.38 + 0.62 * n);
            }

            sectionScale *= outroFactor;

            return densityGravityMultiplier * sectionScale * hardRockSpeed;
        }

        private void UpdateProgressBar(double currentTime, ControlPointInfo? controlPointInfo)
        {
            if (noteTimes.Length == 0)
            {
                progressBarFill.Height = 0f;
                return;
            }

            double startTime = Math.Min(0.0, noteTimes[0]);
            double endTime = Math.Max(noteTimes[^1], startTime + 1.0);
            double progress = Math.Clamp((currentTime - startTime) / (endTime - startTime), 0.0, 1.0);

            progressBarFill.Height = (float)(progress * (Rows * CellSize));

            bool isKiai = controlPointInfo?.EffectPointAt(currentTime).KiaiMode ?? false;
            if (isQueueDepleting || isSongFinished)
                progressBarFill.Colour = new Color4(65, 240, 190, 255);
            else if (isKiai)
                progressBarFill.Colour = new Color4(255, 195, 70, 255);
            else
                progressBarFill.Colour = new Color4(220, 240, 255, 235);
        }

        private int ConsumeNoteGravitySteps(double currentTime, TimingControlPoint timingPoint)
        {
            if (noteTimes.Length == 0)
                return 0;

            int search = Array.BinarySearch(noteTimes, currentTime);
            int latestPassedIdx = search >= 0 ? search : (~search) - 1;

            if (lastProcessedNoteIndex == -1)
            {
                lastProcessedNoteIndex = latestPassedIdx;
                return latestPassedIdx >= 0 && Math.Abs(currentTime - noteTimes[latestPassedIdx]) <= 50.0 ? 1 : 0;
            }

            if (latestPassedIdx > lastProcessedNoteIndex && latestPassedIdx >= 0)
            {
                int prevIdx = lastProcessedNoteIndex;
                int crossedCount = latestPassedIdx - lastProcessedNoteIndex;
                lastProcessedNoteIndex = latestPassedIdx;

                // Pure Note-Stepped Gravity: step on each beatmap note hit, with bonus steps on wider beat gaps scaled by 2.0x difficulty
                double gapSincePrev = prevIdx >= 0 ? noteTimes[latestPassedIdx] - noteTimes[prevIdx] : timingPoint.BeatLength;
                int bonusStep = gapSincePrev >= timingPoint.BeatLength * 1.35 ? 2 : (gapSincePrev >= timingPoint.BeatLength * 0.70 ? 1 : 0);
                return Math.Clamp(crossedCount + bonusStep, 1, 3);
            }

            return 0;
        }

        protected override void Update()
        {
            base.Update();

            if (isToppedOut)
                return;

            EnsureDifficultyMultiplier();

            var controlPointInfo = Beatmap?.ControlPointInfo ?? workingBeatmap?.Value?.Beatmap?.ControlPointInfo;
            TimingControlPoint timingPoint = controlPointInfo?.TimingPointAt(Time.Current) ?? TimingControlPoint.DEFAULT;

            if (noteTimes.Length > 0 && !isQueueDepleting)
            {
                double timeRemaining = noteTimes[^1] - Time.Current;
                int search = Array.BinarySearch(noteTimes, Time.Current);
                int nextIdx = search >= 0 ? search : ~search;
                int notesRemaining = noteTimes.Length - nextIdx;

                if (timeRemaining <= 7500.0 || notesRemaining <= 12)
                {
                    isQueueDepleting = true;
                    if (bag.Count > 5)
                        bag.RemoveRange(5, bag.Count - 5);
                    UpdateQueuePreviews();
                }
            }

            UpdateProgressBar(Time.Current, controlPointInfo);

            if (isSongFinished)
                return;

            if (noteTimes.Length > 0 && Time.Current > noteTimes[^1] + 350.0)
            {
                isSongFinished = true;
                isWaitingForNoteSpawn = false;
                currentPiece = 0;
                bag.Clear();
                UpdateQueuePreviews();
                RedrawGrid();
                return;
            }

            int noteSteps = ConsumeNoteGravitySteps(Time.Current, timingPoint);

            if (isWaitingForNoteSpawn || currentPiece <= 0)
            {
                if (noteTimes.Length == 0 && Time.Current >= 0)
                {
                    isWaitingForNoteSpawn = false;
                    SpawnNextPiece();
                }
                else if (noteTimes.Length > 0 && Time.Current >= nextSpawnNoteTime - 25.0)
                {
                    isWaitingForNoteSpawn = false;
                    SpawnNextPiece();
                }
                return;
            }

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

            if (Time.Current >= 0)
            {
                if (IsPieceGrounded())
                {
                    double effectiveLockDelay = IsHardRock ? 320.0 : lock_delay_duration;
                    lockDelayTimer += dt;
                    if (lockDelayTimer >= effectiveLockDelay)
                    {
                        LockPiece();
                        lastDropTime = Time.Current;
                    }
                }
                else
                {
                    lockDelayTimer = 0;

                    // Pause gravity during official beatmap breaks or pre-song intro
                    if (!ShouldDeferSpawnForPause(Time.Current, out _))
                    {
                        double dynamicMultiplier = GetDynamicGravityMultiplier(Time.Current, controlPointInfo);
                        double stepInterval = timingPoint.BeatLength / Math.Max(dynamicMultiplier, 0.15);
                        bool shouldStepOnNote = noteSteps > 0 && (Time.Current - lastDropTime) >= stepInterval * 0.45;

                        if (shouldStepOnNote || Time.Current - lastDropTime >= stepInterval)
                        {
                            StepGravity();
                            lastDropTime = Time.Current;
                        }
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
            if (isSongFinished || currentPiece <= 0)
                return;

            var shape = Tetrominoes[currentPiece][currentRotation];
            int size = shape.GetLength(0);

            Span<int> lowestPieceRowInCol = stackalloc int[Columns];
            lowestPieceRowInCol.Fill(-1);

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
                            grid[gx, gy] = currentPiece;
                            if (gy > lowestPieceRowInCol[gx])
                                lowestPieceRowInCol[gx] = gy;
                        }
                    }
                }
            }

            // Option A + Y-Level Hole Damage: Count newly buried empty holes directly underneath the locked piece
            int holesCreated = 0;
            double totalHoleDamage = 0.0;
            for (int gx = 0; gx < Columns; gx++)
            {
                int bottomY = lowestPieceRowInCol[gx];
                int holeY = bottomY + 1;
                if (bottomY >= 0 && holeY < Rows && grid[gx, holeY] == 0)
                {
                    holesCreated++;
                    totalHoleDamage += CalculateHoleDamageForYLevel(holeY);
                }
            }

            int linesCleared = ClearLines();
            UpdateDangerState();

            if (healthProcessor is TetrisHealthProcessor tetrisHealth)
            {
                if (linesCleared > 0)
                {
                    double baseHeal = linesCleared switch
                    {
                        1 => 0.08,
                        2 => 0.18,
                        3 => 0.30,
                        _ => 0.50
                    };
                    tetrisHealth.ApplyPlacementHeal(IsHardRock ? baseHeal * 0.65 : baseHeal);
                }
                else if (holesCreated == 0)
                {
                    tetrisHealth.ApplyPlacementHeal(IsHardRock ? 0.008 : 0.015);
                }
                else
                {
                    tetrisHealth.ApplyHoleDamage(totalHoleDamage);
                    if (tetrisHealth.HealthDepleted)
                    {
                        DrawableTetrisHitObject.IsDangerState = true;
                        TriggerNearestHitObjectWithResult(HitResult.Miss);

                        if (healthProcessor?.HasFailed == true)
                        {
                            isToppedOut = true;
                            return;
                        }
                    }
                }
            }

            JudgePieceLock(linesCleared, holesCreated);
            SpawnNextPiece();
        }

        private double CalculateHoleDamageForYLevel(int holeY)
        {
            // Bottom range (rows 14..19): light damage
            if (holeY >= 14)
                return IsHardRock ? 0.07 : 0.04;

            // Middle range (rows 8..13): moderate damage
            if (holeY >= 8)
                return IsHardRock ? 0.13 : 0.08;

            // Upper / Danger range (rows 0..7): heavy damage
            return IsHardRock ? 0.22 : 0.14;
        }

        private int GetHighestOccupiedRow()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (grid[x, y] != 0)
                        return y;
                }
            }
            return Rows;
        }

        private void UpdateDangerState()
        {
            // Top 3 rows (rows 0, 1, 2) are the critical danger zone
            DrawableTetrisHitObject.IsDangerState = isToppedOut || GetHighestOccupiedRow() <= 2;
        }

        private DrawableTetrisHitObject? FindNearestUnjudgedHitObject(out double minDistance)
        {
            DrawableTetrisHitObject? nearest = null;
            minDistance = double.MaxValue;

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

            return nearest;
        }

        private void TriggerNearestHitObjectWithResult(HitResult result)
        {
            var nearest = FindNearestUnjudgedHitObject(out _);
            nearest?.TriggerWithResult(result);
        }

        private void JudgePieceLock(int linesCleared, int holesCreated)
        {
            HitResult result;

            if (linesCleared > 0)
            {
                result = HitResult.Great;
            }
            else if (DrawableTetrisHitObject.IsDangerState)
            {
                result = (holesCreated > 0 || GetHighestOccupiedRow() <= 1) ? HitResult.Miss : HitResult.Ok;
            }
            else if (holesCreated == 0)
            {
                result = HitResult.Great;
            }
            else if (holesCreated == 1)
            {
                result = HitResult.Ok;
            }
            else
            {
                result = HitResult.Meh;
            }

            var nearest = FindNearestUnjudgedHitObject(out double noteDist);
            if (nearest != null && noteDist <= 250.0)
            {
                nearest.TriggerWithResult(result);
            }
            else if (result != HitResult.Great)
            {
                DrawableTetrisHitObject.PendingLockResult = result;
            }
        }

        private int ClearLines()
        {
            int cleared = 0;

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
                    cleared++;

                    for (int pullY = y; pullY > 0; pullY--)
                        for (int x = 0; x < Columns; x++)
                            grid[x, pullY] = grid[x, pullY - 1];

                    for (int x = 0; x < Columns; x++)
                        grid[x, 0] = 0;

                    y++;
                }
            }

            return cleared;
        }

        private bool CheckCollision(Vector2I pos, int rot)
        {
            if (currentPiece <= 0 || currentPiece >= Tetrominoes.Length)
                return true;

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

            ghostContainer.Clear();
            if (isSongFinished || currentPiece <= 0 || currentPiece >= Tetrominoes.Length)
                return;

            var shape = Tetrominoes[currentPiece][currentRotation];
            int size = shape.GetLength(0);

            // Render TETR.IO hollowed-out ghost piece at landing position
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
