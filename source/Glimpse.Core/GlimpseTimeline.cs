using System;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Framework;
using Glimpse.Core.Message;

namespace Glimpse.Core
{
    public static class GlimpseTimeline
    {
        public static OngoingCapture Capture(string eventName)
        {
            return Capture(eventName, string.Empty);
        }

        public static OngoingCapture Capture(string eventName, string eventSubText)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (!GlimpseRuntime.IsInitialized)
            {
                return OngoingCapture.Empty();
            }

            var config = GlimpseRuntime.Instance.Configuration;
            var executionTimer = config.TimerStrategy();

            return new OngoingCapture(executionTimer, config.MessageBroker, eventName, eventSubText);
        }

        public static void CaptureMoment(string eventName)
        {
            CaptureMoment(eventName, string.Empty);
        }

        public static void CaptureMoment(string eventName, string eventSubText)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (!GlimpseRuntime.IsInitialized)
            {
                return;
            }

            var config = GlimpseRuntime.Instance.Configuration;
            var executionTimer = config.TimerStrategy();

            var timerResult = executionTimer.Point();

            config.MessageBroker.Publish(new TimelineMessage(timerResult, eventName, eventSubText));
        }

        public class OngoingCapture : IDisposable
        {
            public static OngoingCapture Empty()
            {
                return new NullOngoingCapture();
            }

            private OngoingCapture()
            {
            }

            public OngoingCapture(IExecutionTimer executionTimer, IMessageBroker messageBroker, string eventName, string eventSubText)
            {
                Offset = executionTimer.Start();
                ExecutionTimer = executionTimer;
                EventSubText = eventSubText;
                MessageBroker = messageBroker;
                EventName = eventName;
            }

            private string EventSubText { get; set; }
            private string EventName { get; set; }
            private TimeSpan Offset { get; set; }
            private IExecutionTimer ExecutionTimer { get; set; }
            private IMessageBroker MessageBroker { get; set; }

            public virtual void Stop()
            {
                var timerResult = ExecutionTimer.Stop(Offset);

                var message = new TimelineMessage(timerResult, EventName, EventSubText);

                MessageBroker.Publish(message);
            }

            public void Dispose()
            {
                Stop();
            }

            private class NullOngoingCapture : OngoingCapture
            {
                public override void Stop()
                {
                }
            }
        }
    }
}
