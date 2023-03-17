using System;

namespace Horseback.Core.InboxPattern
{
    internal class InboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;
    }
}
