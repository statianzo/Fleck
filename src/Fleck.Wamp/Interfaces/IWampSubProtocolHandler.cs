using System;
using System.Collections.Generic;

namespace Fleck
{
    public interface IWampSubProtocolHandler : ISubProtocolHandler
    {
        Action<IWebSocketConnection> OnWelcomeMessage { get; set; }
        Action<IWebSocketConnection, string, string> OnPrefixMessage { get; set; }
        Action<IWebSocketConnection, string, Uri, string> OnCallMessage { get; set; }
        Action<IWebSocketConnection, string, object> OnCallResultMessage { get; set; }
        Action<IWebSocketConnection, string, Uri, string, string> OnCallErrorMessage { get; set; }
        Action<IWebSocketConnection, Uri> OnSubscribeMessage { get; set; }
        Action<IWebSocketConnection, Uri> OnUnsubscribeMessage { get; set; }
        Action<IWebSocketConnection, Uri, string, IEnumerable<Guid>, IEnumerable<Guid>> OnPublishMessage { get; set; }
        Action<IWebSocketConnection, Uri, object> OnEventMessage { get; set; }

        IDictionary<Uri, IList<Guid>> Subscriptions { get; }
        IDictionary<Guid, IDictionary<string, Uri>> Prefixes { get; }
        void RegisterDelegateForMessage<T>(Uri uri, Action<T> d);
        void DeregisterDelegateForMessage(Uri uri);
        void SendCallResultMessage(IWebSocketConnection connection, string callId, object result);
        void SendCallErrorMessage(IWebSocketConnection connection, string callId, Uri errorUri, string errorDescription, string errorDetails = null);
        void SendEventMessage(IWebSocketConnection connection, Uri topicUri, object eventId, IList<Guid> includes, IList<Guid> excludes);
    }
}
