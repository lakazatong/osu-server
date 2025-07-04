﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Game.Storyboards.Commands;
using osu.Game.Storyboards.Drawables;
using osuTK;

namespace osu.Game.Storyboards
{
    public class StoryboardSprite : IStoryboardElementWithDuration
    {
        private readonly List<StoryboardLoopingGroup> loopingGroups =
            new List<StoryboardLoopingGroup>();
        private readonly List<StoryboardTriggerGroup> triggerGroups =
            new List<StoryboardTriggerGroup>();

        public string Path { get; }
        public virtual bool IsDrawable => HasCommands;

        public Anchor Origin;
        public Vector2 InitialPosition;

        public readonly StoryboardCommandGroup Commands = new StoryboardCommandGroup();

        public virtual double StartTime
        {
            get
            {
                // Users that are crafting storyboards using raw osb scripting or external tools may create alpha events far before the actual display time
                // of sprites.
                //
                // To make sure lifetime optimisations work as efficiently as they can, let's locally find the first time a sprite becomes visible.
                var alphaCommands = new List<StoryboardCommand<float>>();

                foreach (var command in Commands.Alpha)
                {
                    alphaCommands.Add(command);
                    if (visibleAtStartOrEnd(command))
                        break;
                }

                foreach (var loop in loopingGroups)
                {
                    foreach (var command in loop.Alpha)
                    {
                        alphaCommands.Add(command);
                        if (visibleAtStartOrEnd(command))
                            break;
                    }
                }

                if (alphaCommands.Count > 0)
                {
                    // Special care is given to cases where there's one or more no-op transforms (ie transforming from alpha 0 to alpha 0).
                    // - If a 0->0 transform exists, we still need to check it to ensure the absolute first start value is non-visible.
                    // - After ascertaining this, we then check the first non-noop transform to get the true start lifetime.
                    var firstAlpha = alphaCommands.MinBy(c => c.StartTime);
                    var firstRealAlpha = alphaCommands
                        .Where(visibleAtStartOrEnd)
                        .MinBy(c => c.StartTime);

                    if (firstAlpha!.StartValue == 0 && firstRealAlpha != null)
                        return firstRealAlpha.StartTime;
                }

                return EarliestTransformTime;

                bool visibleAtStartOrEnd(StoryboardCommand<float> command) =>
                    command.StartValue > 0 || command.EndValue > 0;
            }
        }

        public double EarliestTransformTime
        {
            get
            {
                // If we got to this point, either no alpha commands were present, or the earliest had a non-zero start value.
                // The sprite's StartTime will be determined by the earliest command, regardless of type.
                double earliestStartTime = Commands.StartTime;
                foreach (var l in loopingGroups)
                    earliestStartTime = Math.Min(earliestStartTime, l.StartTime);
                return earliestStartTime;
            }
        }

        public double EndTime
        {
            get
            {
                double latestEndTime = Commands.EndTime;

                foreach (var l in loopingGroups)
                    latestEndTime = Math.Max(latestEndTime, l.EndTime);

                return latestEndTime;
            }
        }

        public double EndTimeForDisplay
        {
            get
            {
                double latestEndTime = Commands.EndTime;

                foreach (var l in loopingGroups)
                    latestEndTime = Math.Max(
                        latestEndTime,
                        l.StartTime + l.Duration * l.TotalIterations
                    );

                return latestEndTime;
            }
        }

        public bool HasCommands => Commands.HasCommands || loopingGroups.Any(l => l.HasCommands);

        public StoryboardSprite(string path, Anchor origin, Vector2 initialPosition)
        {
            Path = path;
            Origin = origin;
            InitialPosition = initialPosition;
        }

        public virtual Drawable CreateDrawable() => new DrawableStoryboardSprite(this);

        public StoryboardLoopingGroup AddLoopingGroup(double loopStartTime, int repeatCount)
        {
            var loop = new StoryboardLoopingGroup(loopStartTime, repeatCount);
            loopingGroups.Add(loop);
            return loop;
        }

        public StoryboardTriggerGroup AddTriggerGroup(
            string triggerName,
            double startTime,
            double endTime,
            int groupNumber
        )
        {
            var trigger = new StoryboardTriggerGroup(triggerName, startTime, endTime, groupNumber);
            triggerGroups.Add(trigger);
            return trigger;
        }

        public void ApplyTransforms<TDrawable>(TDrawable drawable)
            where TDrawable : Drawable, IFlippable, IVectorScalable
        {
            HashSet<string> appliedProperties = new HashSet<string>();

            // For performance reasons, we need to apply the commands in chronological order.
            // Not doing so will cause many functions to be interleaved, resulting in O(n^2) complexity.
            IEnumerable<IStoryboardCommand> commands = Commands.AllCommands;
            commands = commands.Concat(loopingGroups.SelectMany(l => l.AllCommands));

            foreach (var command in commands.OrderBy(c => c.StartTime))
            {
                if (appliedProperties.Add(command.PropertyName))
                    command.ApplyInitialValue(drawable);

                using (drawable.BeginAbsoluteSequence(command.StartTime))
                    command.ApplyTransforms(drawable);
            }
        }
    }
}
