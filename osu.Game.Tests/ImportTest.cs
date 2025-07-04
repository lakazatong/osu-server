﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests
{
    public abstract partial class ImportTest
    {
        protected virtual TestOsuGameBase LoadOsuIntoHost(GameHost host, bool withBeatmap = false)
        {
            var osu = new TestOsuGameBase(withBeatmap);
            Task.Factory.StartNew(() => host.Run(osu), TaskCreationOptions.LongRunning)
                .ContinueWith(
                    t => Assert.Fail($"Host threw exception {t.Exception}"),
                    TaskContinuationOptions.OnlyOnFaulted
                );

            waitForOrAssert(
                () => osu.IsLoaded,
                @"osu! failed to start in a reasonable amount of time"
            );

            bool ready = false;
            // wait for two update frames to be executed. this ensures that all components have had a change to run LoadComplete and hopefully avoid
            // database access (GlobalActionContainer is one to do this).
            host.UpdateThread.Scheduler.Add(() =>
                host.UpdateThread.Scheduler.Add(() => ready = true)
            );

            waitForOrAssert(() => ready, @"osu! failed to start in a reasonable amount of time");

            return osu;
        }

        private void waitForOrAssert(Func<bool> result, string failureMessage, int timeout = 60000)
        {
            Task task = Task.Run(() =>
            {
                while (!result())
                    Thread.Sleep(200);
            });

            Assert.IsTrue(task.Wait(timeout), failureMessage);
        }

        public partial class TestOsuGameBase : OsuGameBase
        {
            public RealmAccess Realm => Dependencies.Get<RealmAccess>();
            public new IAPIProvider API => base.API;

            private readonly bool withBeatmap;

            public TestOsuGameBase(bool withBeatmap)
            {
                this.withBeatmap = withBeatmap;

                base.API = new DummyAPIAccess();
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // Beatmap must be imported before the collection manager is loaded.
                if (withBeatmap)
                    BeatmapManager.Import(TestResources.GetTestBeatmapForImport()).WaitSafely();

                // the logic for setting the initial ruleset exists in OsuGame rather than OsuGameBase.
                // the ruleset bindable is not meant to be nullable, so assign any ruleset in here.
                Ruleset.Value = RulesetStore.AvailableRulesets.First();
            }
        }
    }
}
