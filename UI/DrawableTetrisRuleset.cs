using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Tetris.Objects;
using osu.Game.Rulesets.Tetris.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Tetris.UI
{
    public partial class DrawableTetrisRuleset : DrawableRuleset<TetrisHitObject>, IKeyBindingHandler<TetrisAction>
    {
        public new TetrisPlayfield Playfield => (TetrisPlayfield)base.Playfield;

        public DrawableTetrisRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override Playfield CreatePlayfield() => new TetrisPlayfield { Beatmap = Beatmap };

        protected override PassThroughInputManager CreateInputManager() => new TetrisInputManager(Ruleset.RulesetInfo);

        public override DrawableHitObject<TetrisHitObject> CreateDrawableRepresentation(TetrisHitObject h)
            => new DrawableTetrisHitObject(h);

        public bool OnPressed(KeyBindingPressEvent<TetrisAction> e)
        {
            switch (e.Action)
            {
                case TetrisAction.MoveLeft:
                    Playfield.Move(-1);
                    return true;

                case TetrisAction.MoveRight:
                    Playfield.Move(1);
                    return true;

                case TetrisAction.RotateCW:
                    Playfield.Rotate(1);
                    return true;

                case TetrisAction.RotateCCW:
                    Playfield.Rotate(-1);
                    return true;

                case TetrisAction.SoftDrop:
                    Playfield.SoftDrop();
                    return true;

                case TetrisAction.HardDrop:
                    Playfield.HardDrop();
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<TetrisAction> e)
        {
        }
    }
}
