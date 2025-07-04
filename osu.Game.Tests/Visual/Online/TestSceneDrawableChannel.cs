// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Overlays.Chat;

namespace osu.Game.Tests.Visual.Online
{
    [TestFixture]
    public partial class TestSceneDrawableChannel : OsuTestScene
    {
        private Channel channel = null!;
        private DrawableChannel drawableChannel = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep(
                "create channel",
                () => channel = new Channel { Id = 1, Name = "Test channel" }
            );
            AddStep(
                "create drawable channel",
                () =>
                    Child = drawableChannel =
                        new DrawableChannel(channel) { RelativeSizeAxes = Axes.Both }
            );
        }

        [Test]
        public void TestMention()
        {
            AddStep(
                "add normal message",
                () =>
                    channel.AddNewMessages(
                        new Message(1)
                        {
                            Sender = new APIUser { Id = 2, Username = "TestUser2" },
                            Content = "Hello how are you today?",
                            Timestamp = new DateTimeOffset(2021, 12, 11, 13, 33, 24, TimeSpan.Zero),
                        }
                    )
            );

            AddStep(
                "add mention",
                () =>
                    channel.AddNewMessages(
                        new Message(2)
                        {
                            Sender = new APIUser { Id = 2, Username = "TestUser2" },
                            Content = $"Hello {API.LocalUser.Value.Username} how are you today?",
                            Timestamp = new DateTimeOffset(2021, 12, 11, 13, 33, 25, TimeSpan.Zero),
                        }
                    )
            );
        }

        [Test]
        public void TestDaySeparators()
        {
            var localUser = new APIUser { Id = 3, Username = "LocalUser" };

            string uuid = Guid.NewGuid().ToString();
            AddStep(
                "add local echo message",
                () =>
                    channel.AddLocalEcho(
                        new LocalEchoMessage
                        {
                            Sender = localUser,
                            Content = "Hi there all!",
                            Timestamp = new DateTimeOffset(2022, 11, 21, 20, 11, 13, TimeSpan.Zero),
                            Uuid = uuid,
                        }
                    )
            );
            AddUntilStep(
                "one day separator present",
                () => drawableChannel.ChildrenOfType<DaySeparator>().Count() == 1
            );

            AddStep(
                "add two prior messages to channel",
                () =>
                    channel.AddNewMessages(
                        new Message(1)
                        {
                            Sender = new APIUser { Id = 1, Username = "TestUser" },
                            Content = "This is a message",
                            Timestamp = new DateTimeOffset(2021, 10, 10, 13, 33, 23, TimeSpan.Zero),
                        },
                        new Message(2)
                        {
                            Sender = new APIUser { Id = 2, Username = "TestUser2" },
                            Content = "This is another message",
                            Timestamp = new DateTimeOffset(2021, 10, 11, 13, 33, 23, TimeSpan.Zero),
                        }
                    )
            );
            AddUntilStep(
                "three day separators present",
                () => drawableChannel.ChildrenOfType<DaySeparator>().Count() == 3
            );

            AddStep(
                "resolve pending message",
                () =>
                    channel.ReplaceMessage(
                        channel.Messages.OfType<LocalEchoMessage>().Single(),
                        new Message(3)
                        {
                            Sender = localUser,
                            Content = "Hi there all!",
                            Timestamp = new DateTimeOffset(2022, 11, 22, 20, 11, 16, TimeSpan.Zero),
                            Uuid = uuid,
                        }
                    )
            );
            AddUntilStep(
                "three day separators present",
                () => drawableChannel.ChildrenOfType<DaySeparator>().Count() == 3
            );
            AddAssert(
                "last day separator is from correct day",
                () =>
                    drawableChannel.ChildrenOfType<DaySeparator>().Last().Date.Date
                    == new DateTime(2022, 11, 22)
            );
        }

        [Test]
        public void TestBackgroundAlternating()
        {
            int messageCount = 1;

            AddRepeatStep(
                "add messages",
                () =>
                {
                    channel.AddNewMessages(
                        new Message(messageCount)
                        {
                            Sender = new APIUser
                            {
                                Id = 3,
                                Username =
                                    "LocalUser " + RNG.Next(0, int.MaxValue - 100).ToString("N"),
                            },
                            Content = "Hi there all!",
                            Timestamp = new DateTimeOffset(
                                2022,
                                11,
                                21,
                                20,
                                messageCount,
                                13,
                                TimeSpan.Zero
                            ),
                            Uuid = Guid.NewGuid().ToString(),
                        }
                    );
                    messageCount++;
                },
                10
            );

            AddUntilStep(
                "10 message present",
                () => drawableChannel.ChildrenOfType<ChatLine>().Count() == 10
            );

            int checkCount = 0;

            AddRepeatStep(
                "check background",
                () =>
                {
                    // +1 because the day separator take one index
                    Assert.AreEqual(
                        (checkCount + 1) % 2 == 0,
                        drawableChannel
                            .ChildrenOfType<ChatLine>()
                            .ToList()[checkCount]
                            .AlternatingBackground
                    );
                    checkCount++;
                },
                10
            );
        }
    }
}
