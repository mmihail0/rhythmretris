using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tetris.Beatmaps;
using osu.Game.Rulesets.Tetris.Difficulty;
using osu.Game.Rulesets.Tetris.Scoring;
using osu.Game.Rulesets.Tetris.UI;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Tetris
{
    public class TetrisRuleset : Ruleset
    {
        public override string Description => "osu!tetris";
        public override string ShortName => "tetris";
        public override string PlayingVerb => "Stacking blocks";

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            => new DrawableTetrisRuleset(this, beatmap, mods);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap)
            => new TetrisBeatmapConverter(beatmap, this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap)
            => new TetrisDifficultyCalculator(RulesetInfo, beatmap);

        public override ScoreProcessor CreateScoreProcessor()
            => new TetrisScoreProcessor(this);

        public override IEnumerable<Mod> GetModsFor(ModType type) => System.Array.Empty<Mod>();

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => new[]
        {
            new KeyBinding(InputKey.Left, TetrisAction.MoveLeft),
            new KeyBinding(InputKey.Right, TetrisAction.MoveRight),
            new KeyBinding(InputKey.Down, TetrisAction.SoftDrop),
            new KeyBinding(InputKey.Space, TetrisAction.HardDrop),
            new KeyBinding(InputKey.Up, TetrisAction.RotateCW),
            new KeyBinding(InputKey.Z, TetrisAction.RotateCCW),
            new KeyBinding(InputKey.C, TetrisAction.Hold)
        };

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.ThLarge
        };
    }

    public enum TetrisAction
    {
        MoveLeft,
        MoveRight,
        SoftDrop,
        HardDrop,
        RotateCW,
        RotateCCW,
        Hold
    }
}
