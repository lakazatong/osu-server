// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens.OnlinePlay.Match.Components;

namespace osu.Game.Screens.OnlinePlay.Multiplayer
{
    public partial class CreateMultiplayerMatchButton : CreateRoomButton
    {
        private IBindable<bool> isConnected = null!;
        private IBindable<bool> operationInProgress = null!;

        [Resolved]
        private MultiplayerClient multiplayerClient { get; set; } = null!;

        [Resolved]
        private OngoingOperationTracker ongoingOperationTracker { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Text = "Create room";

            isConnected = multiplayerClient.IsConnected.GetBoundCopy();
            operationInProgress = ongoingOperationTracker.InProgress.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            isConnected.BindValueChanged(_ => Scheduler.AddOnce(updateState));
            operationInProgress.BindValueChanged(_ => Scheduler.AddOnce(updateState), true);
        }

        private void updateState() =>
            Enabled.Value = isConnected.Value && !operationInProgress.Value;
    }
}
