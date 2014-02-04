using System;
using System.Diagnostics;

namespace Glimpse.Ado.Message
{
    public class CommandStackTraceMessage : AdoCommandPayloadMessage<StackTrace>
    {
        public CommandStackTraceMessage(Guid connectionId, Guid commandId, StackTrace stackTrace, bool isAsync = false)
            : base(connectionId, commandId, stackTrace, isAsync)
        {
        }
    }
}