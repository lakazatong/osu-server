// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Lists;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Chat;
using osu.Game.Overlays.Chat.Listing;

namespace osu.Game.Online.Chat
{
    public class Channel
    {
        public const int MAX_HISTORY = 300;

        /// <summary>
        /// Contains every joined user except the current logged in user. Currently only returned for PM channels.
        /// </summary>
        public readonly ObservableCollection<APIUser> Users = new ObservableCollection<APIUser>();

        [JsonProperty(@"users")]
        private int[] userIds
        {
            set
            {
                foreach (int id in value)
                    Users.Add(new APIUser { Id = id });
            }
        }

        /// <summary>
        /// Contains all the messages send in the channel.
        /// </summary>
        public readonly SortedList<Message> Messages = new SortedList<Message>(
            Comparer<Message>.Default
        );

        /// <summary>
        /// Contains all the messages that weren't read by the user.
        /// </summary>
        public IEnumerable<Message> UnreadMessages => Messages.Where(m => LastReadId < m.Id);

        /// <summary>
        /// Contains all the messages that are still pending for submission to the server.
        /// </summary>
        private readonly List<LocalEchoMessage> pendingMessages = new List<LocalEchoMessage>();

        /// <summary>
        /// An event that fires when new messages arrived.
        /// </summary>
        public event Action<IEnumerable<Message>> NewMessagesArrived;

        /// <summary>
        /// An event that fires when a pending message gets resolved.
        /// </summary>
        public event Action<LocalEchoMessage, Message> PendingMessageResolved;

        /// <summary>
        /// An event that fires when a pending message gets removed.
        /// </summary>
        public event Action<Message> MessageRemoved;

        public bool ReadOnly => false; // todo: not yet used.

        public override string ToString() => Name;

        [JsonProperty(@"name")]
        public string Name;

        [JsonProperty(@"description")]
        public string Topic;

        [JsonProperty(@"type")]
        public ChannelType Type;

        [JsonProperty(@"channel_id")]
        public long Id;

        [JsonProperty(@"last_message_id")]
        public long? LastMessageId;

        [JsonProperty(@"last_read_id")]
        public long? LastReadId;

        /// <remarks>
        /// Purposefully nullable for the sake of <see cref="ChannelListing.ChannelListingChannel"/>.
        /// </remarks>
        [JsonProperty(@"message_length_limit")]
        public int? MessageLengthLimit;

        /// <summary>
        /// Signals if the current user joined this channel or not. Defaults to false.
        /// Note that this does not guarantee a join has completed. Check Id > 0 for confirmation.
        /// </summary>
        public Bindable<bool> Joined = new Bindable<bool>();

        /// <summary>
        /// Signals if there is a message to highlight.
        /// This is automatically cleared by the associated <see cref="DrawableChannel"/> after highlighting.
        /// </summary>
        public Bindable<Message> HighlightedMessage = new Bindable<Message>();

        /// <summary>
        /// The current text box message while in this <see cref="Channel"/>.
        /// </summary>
        public Bindable<string> TextBoxMessage = new Bindable<string>(string.Empty);

        [JsonConstructor]
        public Channel() { }

        /// <summary>
        /// Create a private messaging channel with the specified user.
        /// </summary>
        /// <param name="user">The user to create the private conversation with.</param>
        public Channel(APIUser user)
        {
            Type = ChannelType.PM;
            Users.Add(user);
            Name = user.Username;
        }

        /// <summary>
        /// Adds the argument message as a local echo. When this local echo is resolved <see cref="PendingMessageResolved"/> will get called.
        /// </summary>
        /// <param name="message"></param>
        public void AddLocalEcho(LocalEchoMessage message)
        {
            pendingMessages.Add(message);
            Messages.Add(message);

            NewMessagesArrived?.Invoke(new[] { message });
        }

        public bool MessagesLoaded;

        /// <summary>
        /// Adds new messages to the channel and purges old messages. Triggers the <see cref="NewMessagesArrived"/> event.
        /// </summary>
        /// <param name="messages"></param>
        public void AddNewMessages(params Message[] messages)
        {
            foreach (var m in messages)
            {
                LocalEchoMessage localEcho = pendingMessages.FirstOrDefault(local =>
                    local.Uuid == m.Uuid
                );

                if (localEcho != null)
                    ReplaceMessage(localEcho, m);
            }

            messages = messages.Except(Messages).ToArray();

            if (messages.Length == 0)
                return;

            Messages.AddRange(messages);

            long? maxMessageId = messages.Max(m => m.Id);
            if (LastMessageId == null || maxMessageId > LastMessageId)
                LastMessageId = maxMessageId;

            purgeOldMessages();

            NewMessagesArrived?.Invoke(messages);
        }

        public void RemoveMessagesFromUser(int userId)
        {
            for (int i = 0; i < Messages.Count; i++)
            {
                var message = Messages[i];

                if (message.SenderId == userId)
                {
                    Messages.RemoveAt(i--);
                    MessageRemoved?.Invoke(message);
                }
            }
        }

        /// <summary>
        /// Replace or remove a message from the channel.
        /// </summary>
        /// <param name="echo">The local echo message (client-side).</param>
        /// <param name="final">The response message, or null if the message became invalid.</param>
        public void ReplaceMessage(LocalEchoMessage echo, Message final)
        {
            if (!pendingMessages.Remove(echo))
                throw new InvalidOperationException("Attempted to remove echo that wasn't present");

            Messages.Remove(echo);

            if (final == null)
            {
                MessageRemoved?.Invoke(echo);
                return;
            }

            if (Messages.Contains(final))
                throw new InvalidOperationException("Attempted to add the same message again");

            Messages.Add(final);

            if (final.Id > LastMessageId)
                LastMessageId = final.Id;

            PendingMessageResolved?.Invoke(echo, final);
        }

        private void purgeOldMessages()
        {
            // never purge local echos
            int messageCount = Messages.Count - pendingMessages.Count;
            if (messageCount > MAX_HISTORY)
                Messages.RemoveRange(0, messageCount - MAX_HISTORY);
        }
    }
}
