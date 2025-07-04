﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Database;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay;

namespace osu.Game.Tests.Visual.OnlinePlay
{
    /// <summary>
    /// Contains the basic dependencies of online play test scenes.
    /// </summary>
    public class OnlinePlayTestSceneDependencies
        : IReadOnlyDependencyContainer,
            IOnlinePlayTestSceneDependencies
    {
        public OngoingOperationTracker OngoingOperationTracker { get; }
        public TestRoomRequestsHandler RequestsHandler { get; }
        public TestUserLookupCache UserLookupCache { get; }
        public BeatmapLookupCache BeatmapLookupCache { get; }

        /// <summary>
        /// All cached dependencies which are also <see cref="Drawable"/> components.
        /// </summary>
        public IReadOnlyList<Drawable> DrawableComponents => drawableComponents;

        private readonly List<Drawable> drawableComponents = new List<Drawable>();
        private readonly DependencyContainer dependencies;

        public OnlinePlayTestSceneDependencies()
        {
            RequestsHandler = new TestRoomRequestsHandler();
            OngoingOperationTracker = new OngoingOperationTracker();
            UserLookupCache = new TestUserLookupCache();
            BeatmapLookupCache = new BeatmapLookupCache();

            dependencies = new DependencyContainer();

            CacheAs(RequestsHandler);
            CacheAs(OngoingOperationTracker);
            CacheAs(new OverlayColourProvider(OverlayColourScheme.Plum));
            CacheAs<UserLookupCache>(UserLookupCache);
            CacheAs(BeatmapLookupCache);
        }

        public object? Get(Type type) => dependencies.Get(type);

        public object? Get(Type type, CacheInfo info) => dependencies.Get(type, info);

        public void Inject<T>(T instance)
            where T : class, IDependencyInjectionCandidate => dependencies.Inject(instance);

        protected void Cache(object instance)
        {
            dependencies.Cache(instance);
            if (instance is Drawable drawable)
                drawableComponents.Add(drawable);
        }

        protected void CacheAs<T>(T instance)
            where T : class
        {
            dependencies.CacheAs(instance);
            if (instance is Drawable drawable)
                drawableComponents.Add(drawable);
        }
    }
}
