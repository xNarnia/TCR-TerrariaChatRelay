using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TerrariaChatRelay.Clients.Discord
{
    public class DiscordMessageQueue : Dictionary<ulong, Queue<string>>
    {
        public bool PreparingToSend { get { return preparingToSend; } }

        private bool preparingToSend { get; set; }
        private Timer queueTimer { get; set; }

        /// <summary>
        /// Initializes a new queue manager for Discord messages.
        /// </summary>
        /// <param name="QueueTime">Amount of time to queue messages before firing the OnReadyToSend event.</param>
        public DiscordMessageQueue(double QueueTime)
        {
            preparingToSend = false;

            queueTimer = new Timer(QueueTime);
            queueTimer.Elapsed += delegate (object sender, ElapsedEventArgs e)
            {
                PrepareSend(false);
                OnReadyToSend?.Invoke(this);
                Clear();
            };
        }

        /// <summary>
        /// Queues message to add to send when queue is ready to send.
        /// </summary>
        /// <param name="channelToSendTo">Channel ID of target channel.</param>
        /// <param name="message">Message to send.</param>
        public void QueueMessage(ulong channelToSendTo, string message)
            => QueueMessage(new List<ulong> { channelToSendTo }, message);

        /// <summary>
        /// Queues messages to add to send when queue is ready to send.
        /// </summary>
        /// <param name="channelToSendTo">Channel IDs of target channels.</param>
        /// <param name="message">Message to send.</param>
        public void QueueMessage(IEnumerable<ulong> channelsToSendTo, string message)
        {
            foreach (var channelId in channelsToSendTo)
            {
                if (!ContainsKey(channelId))
                    Add(channelId, new Queue<string>());

                this[channelId].Enqueue(message);
            }

            PrepareSend(true);
        }

        /// <summary>
        /// Starts or stops the queue timer.
        /// </summary>
        /// <param name="Prepare"></param>
        private void PrepareSend(bool Prepare = true)
        {
            if (Prepare && !preparingToSend)
            {
                preparingToSend = true;
                queueTimer.Start();
            }
            else if (!Prepare && preparingToSend)
            {
                preparingToSend = false;
                queueTimer.Stop();
            }
        }

        /// <summary>
        /// <para>Fires when queue timer has elapsed. The timer begins when a message is queued. </para>
        /// If a message is queued while the timer is running, it will be queued.
        /// When the timer elapses, it will fire this event with a queue for each channel.
        /// </summary>
        public event Action<Dictionary<ulong, Queue<string>>> OnReadyToSend;
    }
}