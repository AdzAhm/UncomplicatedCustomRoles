﻿using HarmonyLib;
using PlayerRoles.PlayableScps.Scp3114;
using UncomplicatedCustomRoles.API.Features;

namespace UncomplicatedCustomRoles.Patches
{
    [HarmonyPatch(typeof(Scp3114Strangle), nameof(Scp3114Strangle.ValidateTarget))]
    internal class Scp3114StranglePrefix
    {
        private static bool Prefix(ReferenceHub hub, ref bool __result, Scp3114Strangle __instance)
        {
            if (hub.roleManager.CurrentRole is null)
                return true;

            if (SummonedCustomRole.TryGet(hub, out SummonedCustomRole playerRole) && playerRole.Role.IsFriendOf is not null && playerRole.Role.IsFriendOf.Contains(__instance.Owner.roleManager.CurrentRole.Team))
            {
                // Attacked player can't be strangled by SCP-3114 as it's his friend :)
                __result = false;
                return false; // Skip
            } else if (SummonedCustomRole.TryGet(__instance.Owner, out SummonedCustomRole scpRole) && scpRole.Role.IsFriendOf is not null && scpRole.Role.IsFriendOf.Contains(hub.roleManager.CurrentRole.Team))
            {
                // Attacked player can't be strangled by SCP-3114 as it's his friend :)
                __result = false;
                return false; // Skip
            }

            return true;
        }
    }
}