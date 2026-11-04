using System.Collections.Generic;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Tetris
{
    public partial class TetrisInputManager : RulesetInputManager<TetrisAction>
    {
        public TetrisInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.Unique)
        {
        }
    }
}
