﻿using System;

namespace Glimpse.Ado.Message
{
    public class CommandErrorMessage : AdoCommandMessageBase<Exception>
    {
        public CommandErrorMessage(Guid connectionId, Guid commandId, Exception exception, bool isAsync = false)
            : base(connectionId, commandId, exception, isAsync)
        {
        }
    }
}