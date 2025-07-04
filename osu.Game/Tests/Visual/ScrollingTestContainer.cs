// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Lists;
using osu.Game.Configuration;
using osu.Game.Rulesets.Timing;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Rulesets.UI.Scrolling.Algorithms;

namespace osu.Game.Tests.Visual
{
    /// <summary>
    /// A container which provides a <see cref="IScrollingInfo"/> to children.
    /// This should only be used when testing
    /// </summary>
    public partial class ScrollingTestContainer : Container
    {
        public SortedList<MultiplierControlPoint> ControlPoints =>
            scrollingInfo.Algorithm.ControlPoints;

        public ScrollVisualisationMethod ScrollAlgorithm
        {
            set => scrollingInfo.Algorithm.Algorithm = value;
        }

        public double TimeRange
        {
            set => scrollingInfo.TimeRange.Value = value;
        }

        public ScrollingDirection Direction
        {
            set => scrollingInfo.Direction.Value = value;
        }

        public IScrollingInfo ScrollingInfo => scrollingInfo;

        [Cached(Type = typeof(IScrollingInfo))]
        private readonly TestScrollingInfo scrollingInfo = new TestScrollingInfo();

        public ScrollingTestContainer(ScrollingDirection direction)
        {
            scrollingInfo.Direction.Value = direction;
        }

        public void Flip() =>
            scrollingInfo.Direction.Value =
                scrollingInfo.Direction.Value == ScrollingDirection.Up
                    ? ScrollingDirection.Down
                    : ScrollingDirection.Up;

        public class TestScrollingInfo : IScrollingInfo
        {
            public readonly Bindable<ScrollingDirection> Direction =
                new Bindable<ScrollingDirection>();
            IBindable<ScrollingDirection> IScrollingInfo.Direction => Direction;

            public readonly Bindable<double> TimeRange = new BindableDouble(1000) { Value = 1000 };
            IBindable<double> IScrollingInfo.TimeRange => TimeRange;

            public readonly TestScrollAlgorithm Algorithm = new TestScrollAlgorithm();
            IBindable<IScrollAlgorithm> IScrollingInfo.Algorithm =>
                new Bindable<IScrollAlgorithm>(Algorithm);
        }

        public class TestScrollAlgorithm : IScrollAlgorithm
        {
            public readonly SortedList<MultiplierControlPoint> ControlPoints =
                new SortedList<MultiplierControlPoint>();

            private IScrollAlgorithm implementation;

            public TestScrollAlgorithm()
            {
                Algorithm = ScrollVisualisationMethod.Constant;
            }

            public ScrollVisualisationMethod Algorithm
            {
                set
                {
                    switch (value)
                    {
                        case ScrollVisualisationMethod.Constant:
                            implementation = new ConstantScrollAlgorithm();
                            break;

                        case ScrollVisualisationMethod.Overlapping:
                            implementation = new OverlappingScrollAlgorithm(ControlPoints);
                            break;

                        case ScrollVisualisationMethod.Sequential:
                            implementation = new SequentialScrollAlgorithm(ControlPoints);
                            break;
                    }
                }
            }

            public double GetDisplayStartTime(
                double originTime,
                float offset,
                double timeRange,
                float scrollLength
            ) => implementation.GetDisplayStartTime(originTime, offset, timeRange, scrollLength);

            public float GetLength(
                double startTime,
                double endTime,
                double timeRange,
                float scrollLength
            ) => implementation.GetLength(startTime, endTime, timeRange, scrollLength);

            public float PositionAt(
                double time,
                double currentTime,
                double timeRange,
                float scrollLength,
                double? originTime = null
            ) => implementation.PositionAt(time, currentTime, timeRange, scrollLength, originTime);

            public double TimeAt(
                float position,
                double currentTime,
                double timeRange,
                float scrollLength
            ) => implementation.TimeAt(position, currentTime, timeRange, scrollLength);

            public void Reset() => implementation.Reset();
        }
    }
}
