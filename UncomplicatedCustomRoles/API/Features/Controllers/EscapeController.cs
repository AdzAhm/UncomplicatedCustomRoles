/*
 * This file is a part of the UncomplicatedCustomRoles project.
 * 
 * Copyright (c) 2023-present FoxWorn3365 (Federico Cosma) <me@fcosma.it>
 * 
 * This file is licensed under the GNU Affero General Public License v3.0.
 * You should have received a copy of the AGPL license along with this file.
 * If not, see <https://www.gnu.org/licenses/>.
 */

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
