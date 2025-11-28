using Exiled.API.Enums;
using Exiled.Events.EventArgs.Interfaces;
using Exiled.Events.EventArgs.Player;
using System.Collections.Generic;

namespace UncomplicatedCustomRoles.API.Features.CustomModules
{
    internal class DamageResistance : CustomModule
    {
        public override List<string> RequiredArgs => new()
        {
            "damages"
        };

        public override List<string> TriggerOnEvents => new()
        {
            "Hurting"
        };

        internal Dictionary<DamageType, uint> DamageTypes = null;

        public override void OnAdded()
        {
            DamageTypes = TryGetValue("damages", new Dictionary<DamageType, uint>()) as Dictionary<DamageType, uint>;

            if (DamageTypes is null)
                ThrowError($"DamageResistance CustomFlag/CustomModule expected a Dictionary<DamageType, uint> in 'damages', got a {TryGetValue("damages", null)?.GetType().FullName}");
        }

        public override bool OnEvent(string name, IPlayerEvent ev)
        {
            if (DamageTypes is null)
                return true;

            if (ev is not HurtingEventArgs hurting)
                return true;

            if (DamageTypes.TryGetValue(hurting.DamageHandler.Type, out uint reduction))
                hurting.DamageHandler.Damage *= (100-reduction) / 100;

            return true;
        }

        public override void OnRemoved()
        {
            DamageTypes = null;
        }
    }
}
