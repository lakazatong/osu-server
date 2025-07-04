// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    public partial class OsuDistanceSnapProvider : ComposerDistanceSnapProvider
    {
        public override double ReadCurrentDistanceSnap(HitObject before, HitObject after)
        {
            // If the pair of hit objects in question here could feasibly be on the same stack, do not provide a distance snap value -
            // they're likely too close to one another for the distance snap value to be useful anyway even if they somehow are not.
            if (
                Vector2.Distance(((OsuHitObject)before).EndPosition, ((OsuHitObject)after).Position)
                < OsuBeatmapProcessor.STACK_DISTANCE
            )
                return 0;

            var lastObjectWithVelocity = EditorBeatmap
                .HitObjects.TakeWhile(ho => ho != after)
                .OfType<IHasSliderVelocity>()
                .LastOrDefault();

            float expectedDistance = DurationToDistance(
                after.StartTime - before.GetEndTime(),
                before.StartTime,
                lastObjectWithVelocity
            );
            float actualDistance = Vector2.Distance(
                ((OsuHitObject)before).StackedEndPosition,
                ((OsuHitObject)after).StackedPosition
            );

            return actualDistance / expectedDistance;
        }

        protected override bool AdjustDistanceSpacing(GlobalAction action, float amount)
        {
            // To allow better visualisation, ensure that the spacing grid is visible before adjusting.
            DistanceSnapToggle.Value = TernaryState.True;

            return base.AdjustDistanceSpacing(action, amount);
        }
    }
}
