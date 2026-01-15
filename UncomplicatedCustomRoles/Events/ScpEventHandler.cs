using Exiled.Events.EventArgs.Scp096;
using PlayerRoles;
using UncomplicatedCustomRoles.API.Features.CustomModules;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomRoles.API.Interfaces;
using UncomplicatedCustomRoles.Manager;
using Exiled.Events.EventArgs.Scp330;
using Exiled.Events.EventArgs.Scp049;

using Scp049Handler = Exiled.Events.Handlers.Scp049;
using Scp096Handler = Exiled.Events.Handlers.Scp096;
using Scp330Handler = Exiled.Events.Handlers.Scp330;

namespace UncomplicatedCustomRoles.Events
{
    internal class ScpEventHandler : EventHandlerBase
    {
        internal override void OnRegistered()
        {
            // SCP-049
            Scp049Handler.FinishingRecall += OnFinishingRecall;

            // SCP-096
            Scp096Handler.AddingTarget += OnAddingTarget;

            // SCP-330
            Scp330Handler.InteractingScp330 += OnInteractingScp330;
        }

        internal override void OnUnregistered()
        {
            // SCP-049
            Scp049Handler.FinishingRecall -= OnFinishingRecall;

            // SCP-096
            Scp096Handler.AddingTarget -= OnAddingTarget;

            // SCP-330
            Scp330Handler.InteractingScp330 -= OnInteractingScp330;
        }

        public void OnAddingTarget(AddingTargetEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (ev.Target.TryGetSummonedInstance(out SummonedCustomRole summonedInstance))
            {
                if (ev.Target.ReferenceHub.GetTeam() is Team.SCPs)
                    ev.IsAllowed = false;

                if (summonedInstance.HasModule<DoNotTrigger096>())
                    ev.IsAllowed = false;

                if (summonedInstance.HasModule<PacifismUntilDamage>())
                    ev.IsAllowed = false;
            }
        }

        public void OnFinishingRecall(FinishingRecallEventArgs ev)
        {
            ICustomRole Role = SpawnManager.DoEvaluateSpawnForPlayer(ev.Target, RoleTypeId.Scp0492);
            LogManager.Silent($"{ev.Target} recalled by {ev.Player}, found {Role?.Id} {Role?.Name}");

            if (Role is not null)
            {
                ev.IsAllowed = false;
                ev.Target.SetCustomRole(Role);
            }
        }

        public void OnInteractingScp330(InteractingScp330EventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (SummonedCustomRole.TryGet(ev.Player, out SummonedCustomRole role))
            {
                role.Scp330Count++;

                LogManager.Debug($"Player {ev.Player} took {role.Scp330Count} candies!");
            }
        }
    }
}
