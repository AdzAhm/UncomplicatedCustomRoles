/*
 * This file is a part of the UncomplicatedCustomRoles project.
 * 
 * Copyright (c) 2023-present FoxWorn3365 (Federico Cosma) <me@fcosma.it>
 * 
 * This file is licensed under the GNU Affero General Public License v3.0.
 * You should have received a copy of the AGPL license along with this file.
 * If not, see <https://www.gnu.org/licenses/>.
 */

using HarmonyLib;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps;
using System;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Manager;
using static HarmonyLib.AccessTools;

namespace UncomplicatedCustomRoles.Patches
{

    [HarmonyPatch(typeof(Player), nameof(Player.Team), MethodType.Getter)]
    internal class TeamPatch
    {
        static bool Prefix(Player __instance, ref Team __result) => !DisguiseTeam.List.TryGetValue(__instance.PlayerId, out __result);
    }

    [HarmonyPatch(typeof(HumanRole), nameof(HumanRole.Team), MethodType.Getter)]
    internal class HumanRolePatch
    {
        private static bool Prefix(PlayerRoleBase __instance, ref Team __result)
        {
            if (__instance.TryGetOwner(out ReferenceHub owner) && DisguiseTeam.List.TryGetValue(owner.PlayerId, out Team team))
            {
                __result = team;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(FpcStandardScp), nameof(FpcStandardScp.Team), MethodType.Getter)]
    internal class FpcStandardScpPatch
    {
        private static bool Prefix(PlayerRoleBase __instance, ref Team __result)
        {
            if (__instance.TryGetOwner(out ReferenceHub owner) && DisguiseTeam.List.TryGetValue(owner.PlayerId, out Team team))
            {
                __result = team;
                return false;
            }

            return true;
        }
    }
}
