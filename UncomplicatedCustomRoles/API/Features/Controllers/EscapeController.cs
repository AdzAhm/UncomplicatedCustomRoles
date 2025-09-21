using Exiled.API.Enums;
using PlayerRoles;
using UnityEngine;

namespace UncomplicatedCustomRoles.API.Features.Controllers
{
    internal class EscapeController : MonoBehaviour
    {
        private SummonedCustomRole _role;

        public void Init(SummonedCustomRole role)
        {
            _role = role;
        }

        private void Update()
        {
            foreach (Bounds escapeZone in global::Escape.EscapeZones)
                if (escapeZone.Contains(_role.Player.Position))
                    Plugin.Instance.Handler.OnEscaping(new(_role.Player.ReferenceHub, RoleTypeId.ChaosConscript, EscapeScenario.CustomEscape));
        }

        private void OnDestroy()
        {
            _role = null;
        }
    }
}
