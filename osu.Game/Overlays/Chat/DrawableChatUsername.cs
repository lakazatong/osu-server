// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Online.Multiplayer;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Screens;
using osu.Game.Screens.Play;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;
using ChatStrings = osu.Game.Localisation.ChatStrings;

namespace osu.Game.Overlays.Chat
{
    public partial class DrawableChatUsername : OsuClickableContainer, IHasContextMenu
    {
        public Action? ReportRequested;

        /// <summary>
        /// The primary colour to use for the username.
        /// </summary>
        public Color4 AccentColour { get; init; }

        /// <summary>
        /// If set to <see langword="false"/>, the username will be drawn as plain text in <see cref="AccentColour"/>.
        /// If set to <see langword="true"/>, the username will be drawn as black text inside a rounded rectangle in <see cref="AccentColour"/>.
        /// </summary>
        public bool Inverted { get; init; }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            colouredDrawable.ReceivePositionalInputAt(screenSpacePos);

        public float FontSize
        {
            set =>
                drawableText.Font = OsuFont.GetFont(
                    size: value,
                    weight: FontWeight.Bold,
                    italics: true
                );
        }

        public LocalisableString Text
        {
            set => drawableText.Text = value;
        }

        public override float Width
        {
            get => base.Width;
            set => base.Width = drawableText.MaxWidth = value;
        }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private MultiplayerClient? multiplayerClient { get; set; }

        [Resolved]
        private IPerformFromScreenRunner? performer { get; set; }

        [Resolved(canBeNull: true)]
        private ChannelManager? chatManager { get; set; }

        [Resolved(canBeNull: true)]
        private ChatOverlay? chatOverlay { get; set; }

        [Resolved(canBeNull: true)]
        private UserProfileOverlay? profileOverlay { get; set; }

        [Resolved]
        private Bindable<Channel?>? currentChannel { get; set; }

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        private readonly APIUser user;
        private readonly OsuSpriteText drawableText;

        private Drawable colouredDrawable = null!;

        public DrawableChatUsername(APIUser user)
        {
            this.user = user;

            Action = openUserProfile;

            drawableText = new TruncatingSpriteText
            {
                Shadow = false,
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (!Inverted)
            {
                Add(colouredDrawable = drawableText);
            }
            else
            {
                Add(
                    new Container
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        AutoSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 4,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Roundness = 1,
                            Radius = 1,
                            Colour = Color4.Black.Opacity(0.3f),
                            Offset = new Vector2(0, 1),
                            Type = EdgeEffectType.Shadow,
                        },
                        Child = new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 4,
                            Children = new[]
                            {
                                colouredDrawable = new Box { RelativeSizeAxes = Axes.Both },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Left = 4,
                                        Right = 4,
                                        Bottom = 1,
                                        Top = -2,
                                    },
                                    Child = drawableText,
                                },
                            },
                        },
                    }
                );
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            drawableText.Colour = colours.ChatBlue;
            colouredDrawable.Colour = AccentColour;
        }

        public MenuItem[] ContextMenuItems
        {
            get
            {
                if (user.Equals(APIUser.SYSTEM_USER))
                    return Array.Empty<MenuItem>();

                if (user.Equals(api.LocalUser.Value))
                    return Array.Empty<MenuItem>();

                List<MenuItem> items = new List<MenuItem>();

                if (currentChannel?.Value != null)
                {
                    items.Add(
                        new OsuMenuItem(
                            ChatStrings.MentionUser,
                            MenuItemType.Standard,
                            () =>
                            {
                                currentChannel.Value.TextBoxMessage.Value += $"@{user.Username} ";
                            }
                        )
                    );
                }

                items.Add(
                    new OsuMenuItem(
                        ContextMenuStrings.ViewProfile,
                        MenuItemType.Highlighted,
                        openUserProfile
                    )
                );

                items.Add(
                    new OsuMenuItem(
                        UsersStrings.CardSendMessage,
                        MenuItemType.Standard,
                        openUserChannel
                    )
                );

                // We should probably be checking against an online state here.
                // But we can't use MetadataClient.GetPresence because we may not be requesting/receiving presences.
                // This isn't really too bad – worst case scenario the client will open spectator view and show the user as "offline".
                {
                    items.Add(new OsuMenuItemSpacer());

                    items.Add(
                        new OsuMenuItem(
                            ContextMenuStrings.SpectatePlayer,
                            MenuItemType.Standard,
                            () =>
                            {
                                performer?.PerformFromScreen(s =>
                                    s.Push(new SoloSpectatorScreen(user))
                                );
                            }
                        )
                    );

                    if (multiplayerClient?.Room?.Users.All(u => u.UserID != user.Id) == true)
                    {
                        items.Add(
                            new OsuMenuItem(
                                ContextMenuStrings.InvitePlayer,
                                MenuItemType.Standard,
                                () => multiplayerClient.InvitePlayer(user.Id)
                            )
                        );
                    }
                }

                items.Add(new OsuMenuItemSpacer());
                items.Add(
                    new OsuMenuItem(
                        UsersStrings.ReportButtonText,
                        MenuItemType.Destructive,
                        ReportRequested
                    )
                );
                items.Add(
                    api.Blocks.Any(b => b.TargetID == user.OnlineID)
                        ? new OsuMenuItem(
                            UsersStrings.BlocksButtonUnblock,
                            MenuItemType.Standard,
                            () => dialogOverlay?.Push(ConfirmBlockActionDialog.Unblock(user))
                        )
                        : new OsuMenuItem(
                            UsersStrings.BlocksButtonBlock,
                            MenuItemType.Destructive,
                            () => dialogOverlay?.Push(ConfirmBlockActionDialog.Block(user))
                        )
                );

                return items.ToArray();
            }
        }

        private void openUserChannel()
        {
            chatManager?.OpenPrivateChannel(user);
            chatOverlay?.Show();
        }

        private void openUserProfile()
        {
            profileOverlay?.ShowUser(user);
        }

        protected override bool OnHover(HoverEvent e)
        {
            colouredDrawable.FadeColour(AccentColour.Lighten(0.6f), 30, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            colouredDrawable.FadeColour(AccentColour, 800, Easing.OutQuint);
        }
    }
}
