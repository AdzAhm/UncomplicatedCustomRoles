﻿using PluginAPI.Core;

namespace UncomplicatedCustomRoles.Events.Interfaces
{
    public interface IAttackerEvent
    {
        abstract ReferenceHub AttackerHub { get; }
    }
}
