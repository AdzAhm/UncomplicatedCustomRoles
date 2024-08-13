﻿using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomRoles.Interfaces;
using UnityEngine;
using System;
using UncomplicatedCustomRoles.Extensions;
using MEC;
using PluginAPI.Core;
using UncomplicatedCustomRoles.API.Features;
using MapGeneration;
using System.Reflection;
using Mirror;
using UncomplicatedCustomRoles.API.Helpers.Imports.EXILED.Extensions;
using InventorySystem.Disarming;
using System.Threading.Tasks;
using UnityEngine.UI;

// Mormora, la gente mormora
// falla tacere praticando l'allegria

namespace UncomplicatedCustomRoles.Manager
{
    internal class SpawnManager
    {
        public static void ClearCustomTypes(Player player)
        {
            if (SummonedCustomRole.TryGet(player, out SummonedCustomRole role))
                role.Destroy();
        }

        public static void SummonCustomSubclass(Player player, int id, bool doBypassRoleOverwrite = true)
        {
            // Does the role exists?
            if (!CustomRole.CustomRoles.ContainsKey(id))
            {
                LogManager.Warn($"Sorry but the role with the Id {id} is not registered inside UncomplicatedCustomRoles!", "CR0092");
                return;
            }

            ICustomRole Role = CustomRole.CustomRoles[id];

            if (Role.SpawnSettings is null)
            {
                LogManager.Warn($"Tried to spawn a custom role without spawn_settings, aborting the SummonCustomSubclass(...) action!\nRole: {Role.Name} ({Role.Id})", "CR0093");
                return;
            }

            if (!doBypassRoleOverwrite && !Role.SpawnSettings.CanReplaceRoles.Contains(player.Role))
            {
                LogManager.Debug($"Can't spawn the player {player.Nickname} as UCR custom role {Role.Name} because it's role is not in the overwrittable list of custom role!\nStrange because this should be managed correctly by the plugin!");
                return;
            }

            // This will allow us to avoid the loop of another OnSpawning
            Spawn.Spawning.TryAdd(player.PlayerId);

            Vector3 BasicPosition = player.Position;

            RoleSpawnFlags SpawnFlag = RoleSpawnFlags.None;

            if (Role.SpawnSettings.Spawn == SpawnLocationType.KeepRoleSpawn)
                SpawnFlag = RoleSpawnFlags.UseSpawnpoint;

            player.ReferenceHub.roleManager.ServerSetRole(Role.Role, RoleChangeReason.Respawn, SpawnFlag);

            if (Role.SpawnSettings.Spawn == SpawnLocationType.KeepCurrentPositionSpawn)
                player.Position = BasicPosition;

            List<RoomIdentifier> OffLimitsRooms = new();

            foreach (TeslaGate Gate in Map.TeslaGates)
                OffLimitsRooms.Add(Gate.Room);

            if (SpawnFlag == RoleSpawnFlags.None)
            {
                switch (Role.SpawnSettings.Spawn)
                {
                    case SpawnLocationType.ZoneSpawn:
                        player.Position = RoomIdentifier.AllRoomIdentifiers.Where(room => room.Zone == Role.SpawnSettings.SpawnZones.RandomItem() && !OffLimitsRooms.Contains(room)).RandomValue().ApiRoom.Position.AddY(1f);
                        break;
                    case SpawnLocationType.CompleteRandomSpawn:
                        player.Position = RoomIdentifier.AllRoomIdentifiers.Where(room => !OffLimitsRooms.Contains(room)).RandomValue().ApiRoom.Position.AddY(1f);
                        break;
                    case SpawnLocationType.RoomsSpawn:
                        player.Position = Map.Rooms.Where(room => Role.SpawnSettings.SpawnRooms.Contains(room.Name)).RandomValue().ApiRoom.Position.AddY(1.5f);
                        break;
                    case SpawnLocationType.PositionSpawn:
                        player.Position = Role.SpawnSettings.SpawnPosition;
                        break;
                };

                if (Role.SpawnSettings.SpawnOffset != new Vector3())
                    player.Position += Role.SpawnSettings.SpawnOffset;
            }

            SummonSubclassApplier(player, Role);
        }

        public static void SummonSubclassApplier(Player Player, ICustomRole Role)
        {
            Player.ClearInventory();

            LogManager.Silent("Assigning normal inventory");
            foreach (ItemType Item in Role.Inventory)
                Player.AddItem(Item);

            LogManager.Silent($"Normal inventory assigned, found {Player.ReferenceHub.inventory.UserInventory.Items.Count()} --> Evaluating CustomInventory (cc: {Role.CustomItemsInventory.Count()})");

            /*if (Role.CustomItemsInventory.Count() > 0)
                foreach (uint ItemId in Role.CustomItemsInventory)
                    if (!Player.IsInventoryFull)
                        try
                        {
                            if (PluginAPI.Loader.AssemblyLoader.InstalledPlugins.Where(pl => pl.PluginName == "UncomplicatedCustomItems").Count() > 0)
                            {
                                PluginHandler PluginType = PluginAPI.Loader.AssemblyLoader.InstalledPlugins.Where(pl => pl.PluginName == "UncomplicatedCustomItems").First();
                                Assembly PluginAssembly = PluginAPI.Loader.AssemblyLoader.Plugins.Where(pl => pl.Value.ContainsKey(PluginType.GetType())).FirstOrDefault().Key;
                                if (PluginAssembly is not null && (bool)PluginAssembly.GetType("UncomplicatedCustomItems.API.Utilities")?.GetMethod("IsCustomItem")?.Invoke(null, new object[] { ItemId }))
                                {
                                    object CustomItem = PluginAssembly.GetType("UncomplicatedCustomItems.API.Utilities")?.GetMethod("GetCustomItem")?.Invoke(null, new object[] { ItemId });

                                    PluginAssembly.GetType("UncomplicatedCustomItems.API.Features.SummonedCustomItem")?.GetMethods().Where(method => method.Name == "Summon" && method.GetParameters().Length == 2).FirstOrDefault()?.Invoke(null, new object[]
                                    {
                                        CustomItem,
                                        Player
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Debug($"Error while giving a custom item.\nError: {ex.Message}");
                        }
            */

            LogManager.Silent($"Evaluating ammo: {Role.Ammo.Count()}");
            if (Role.Ammo.GetType() == typeof(Dictionary<ItemType, ushort>) && Role.Ammo.Count() > 0)
                foreach (KeyValuePair<ItemType, ushort> Ammo in Role.Ammo)
                    Player.AddAmmo(Ammo.Key, Ammo.Value);

            LogManager.Silent($"Assigning CustomIndo");
            if (Role.CustomInfo != null && Role.CustomInfo != string.Empty)
                Player.CustomInfo += $"\n{Role.CustomInfo}";

            LogManager.Silent("Assigining health stats");
            // Apply every required stats
            Role.Health?.Apply(Player);

            LogManager.Silent("Assigining ahp stats");
            Role.Ahp?.Apply(Player);

            LogManager.Silent("Assigining stamina stats");
            Role.Stamina?.Apply(Player);

            LogManager.Silent("Adding permanent effects");
            List<IEffect> PermanentEffects = new();
            if (Role.Effects.Count() > 0 && Role.Effects != null)
            {
                foreach (IEffect effect in Role.Effects)
                {
                    if (effect.Duration < 0)
                    {
                        effect.Duration = 15f;
                        PermanentEffects.Add(effect);
                        continue;
                    }
                    Player.ReferenceHub.playerEffectsController.ChangeState(effect.EffectType, effect.Intensity, effect.Duration);
                }
            }

            LogManager.Silent("Assigining scale");
            if (Role.Scale != Vector3.zero && Role.Scale != Vector3.one)
            {
                Player.ReferenceHub.transform.localScale = Role.Scale;
                foreach (Player player in Player.GetPlayers())
                    NetworkServer.SendSpawnMessage(player.ReferenceHub.networkIdentity, player.Connection);
            }

            LogManager.Silent("Assigining SpawnBroadcast");
            if (Role.SpawnBroadcast != string.Empty)
            {
                Player.ClearBroadcasts();
                Player.SendBroadcast(Role.SpawnBroadcast, Role.SpawnBroadcastDuration);
            }

            LogManager.Silent("Assigining SpawnHint");
            if (Role.SpawnHint != string.Empty)
                Player.ReceiveHint(Role.SpawnHint, Role.SpawnHintDuration);

            LogManager.Silent("Assigining Badge");
            KeyValuePair<string, string>? Badge = null;
            if (Role.BadgeName is not null && Role.BadgeName.Length > 1 && Role.BadgeColor is not null && Role.BadgeColor.Length > 2)
            {
                Badge = new(Player.ReferenceHub.serverRoles.Network_myText ?? "", Player.ReferenceHub.serverRoles.Network_myColor ?? "");
                LogManager.Debug($"Badge detected, putting {Role.BadgeName}@{Role.BadgeColor} to player {Player.PlayerId}");

                Player.ReferenceHub.serverRoles.SetText(Role.BadgeName);
                Player.ReferenceHub.serverRoles.SetColor(Role.BadgeColor);
            }

            LogManager.Silent("Assigining Nickname");
            // Changing nickname if needed
            bool ChangedNick = false;
            if (Plugin.Instance.Config.AllowNicknameEdit && Role.Nickname is not null && Role.Nickname != string.Empty)
            {
                Role.Nickname = Role.Nickname.Replace("%dnumber%", new System.Random().Next(1000, 9999).ToString()).Replace("%nick%", Player.Nickname).Replace("%rand%", new System.Random().Next(0, 9).ToString()).Replace("%unitid%", Player.UnitId.ToString());
                if (Role.Nickname.Contains(","))
                    Player.DisplayNickname = Role.Nickname.Split(',').RandomItem();
                else
                    Player.DisplayNickname = Role.Nickname;

                ChangedNick = true;
            }

            LogManager.Silent("Assigining appeareance");
            if (Role.RoleAppearance != Role.Role)
            {
                LogManager.Debug($"Changing the appearance of the role {Role.Id} [{Role.Name}] to {Role.RoleAppearance}");
                Timing.CallDelayed(1f, () =>
                {
                    Player.ChangeAppearance(Role.RoleAppearance, true);
                });
            }

            LogManager.Debug($"{Player} successfully spawned as {Role.Name} ({Role.Id})!");

            //new SummonedCustomRole(Player, Role, Badge, PermanentEffects, ChangedNick);
            new SummonedCustomRole(Player, Role, Badge, PermanentEffects, ChangedNick);

            LogManager.Debug($"{Player} successfully spawned as {Role.Name} ({Role.Id})! [2VDS]");

            Task.Run(() =>
            {
                foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>().Where(obj => obj.TryGetComponent(out Text _)))
                {
                    Text text = obj.GetComponent<Text>();
                    LogManager.Debug($"{obj.name} - {obj.GetInstanceID()} - {obj.tag} >> {text.text}");
                    LogManager.Silent(text.text);
                    GetChildFromGameObject(obj.transform);
                    GetChildFromGameObject(text.transform, "TRSF_TEXT");
                }

                /*LogManager.Debug($"{Player.GameObject.transform.childCount} chd - nm: {Player.GameObject.name}");
                GetChildFromGameObject(Player.GameObject.transform);*/
            });
        }

        private static void GetChildFromGameObject(Transform transform, string prefix = "")
        {
            foreach (Transform gameObject in transform)
            {
                LogManager.Debug($"{prefix} CHILD OF {transform.name} >> {gameObject.name} - {gameObject.GetInstanceID()} - {gameObject.tag}");
                if (gameObject.TryGetComponent(out Text text))
                    LogManager.Debug($"IS TEXT COMPONENT --> {text.text}");
                else
                    LogManager.Debug($"IS NOT TEXT COMPONENT");

                if (gameObject.gameObject.TryGetComponent(out Text text2))
                    LogManager.Debug($"IS TEXT COMPONENT --> {text2.text}");
                else
                    LogManager.Debug($"IS NOT TEXT COMPONENT");
                GetChildFromGameObject(gameObject.transform);
            }
        }

        public static KeyValuePair<bool, object> ParseEscapeRole(string roleAfterEscape, Player player)
        {
            List<string> Role = new();

            if (roleAfterEscape is not null && roleAfterEscape != string.Empty)
            {
                if (roleAfterEscape.Contains(","))
                {
                    string[] roles = roleAfterEscape.Split(',');
                    foreach (string role in roles)
                        foreach (string rolePart in role.Split(':')) 
                            Role.Add(rolePart);
                }

                int SearchIndex = 0;

                Player Cuffer = Player.Get(DisarmedPlayers.Entries.FirstOrDefault(entry => entry.DisarmedPlayer == player.NetworkId).Disarmer);

                if (player.IsDisarmed && Cuffer is not null)
                    SearchIndex = Cuffer.Team switch
                    {
                        Team.FoundationForces => 2,
                        Team.ChaosInsurgency => 4,
                        Team.Scientists => 6,
                        Team.ClassD => 8,
                        _ => 0
                    };

                // Let's proceed
                if (Role.Count >= SearchIndex + 2)
                    if (Role[SearchIndex] is "IR")
                        return new(false, Role[SearchIndex + 1]);
                    else if (Role[SearchIndex] is "CR")
                        return new(true, Role[SearchIndex + 1]);
                    else
                        LogManager.Error($"Error while parsing role_after_escape for player {player.Nickname} ({player.PlayerId}): the first string was not 'IR' nor 'CR', found '{Role[SearchIndex]}'!\nPlease see our documentation: https://github.com/UncomplicatedCustomServer/UncomplicatedCustomRoles/wiki/Specifics#role-after-escape");
                else
                    LogManager.Debug($"Error while parsing role_after_escape: index is out of range!\nExpected to found {SearchIndex}, total: {Role.Count}!");
            }

            return new(false, null);
        }

#nullable enable
#pragma warning disable CS8602 // <Element> can be null at this point! (added a check!)
        public static ICustomRole? DoEvaluateSpawnForPlayer(Player player, RoleTypeId? role = null)
        {
            role ??= player.Role;

            RoleTypeId NewRole = (RoleTypeId)role;

            Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage = new()
            {
                { RoleTypeId.ClassD, new() },
                { RoleTypeId.Scientist, new() },
                { RoleTypeId.NtfPrivate, new() },
                { RoleTypeId.NtfSergeant, new() },
                { RoleTypeId.NtfCaptain, new() },
                { RoleTypeId.NtfSpecialist, new() },
                { RoleTypeId.ChaosConscript, new() },
                { RoleTypeId.ChaosMarauder, new() },
                { RoleTypeId.ChaosRepressor, new() },
                { RoleTypeId.ChaosRifleman, new() },
                { RoleTypeId.Tutorial, new() },
                { RoleTypeId.Scp049, new() },
                { RoleTypeId.Scp0492, new() },
                { RoleTypeId.Scp079, new() },
                { RoleTypeId.Scp173, new() },
                { RoleTypeId.Scp939, new() },
                { RoleTypeId.Scp096, new() },
                { RoleTypeId.Scp106, new() },
                { RoleTypeId.Scp3114, new() },
                { RoleTypeId.FacilityGuard, new() }
            };

            foreach (ICustomRole Role in CustomRole.CustomRoles.Values.Where(cr => cr.SpawnSettings is not null))
                if (!Role.IgnoreSpawnSystem && Player.GetPlayers().Count >= Role.SpawnSettings.MinPlayers && SummonedCustomRole.Count(Role) < Role.SpawnSettings.MaxPlayers)
                    foreach (RoleTypeId RoleType in Role.SpawnSettings.CanReplaceRoles)
                        for (int a = 0; a < Role.SpawnSettings.SpawnChance; a++)
                            RolePercentage[RoleType].Add(Role);

            if (player.HasCustomRole())
            {
                LogManager.Debug("Was evalutating role select for an already custom role player, stopping");
                return null;
            }

            if (RolePercentage.ContainsKey(player.Role))
                if (new System.Random().Next(0, 99) < RolePercentage[NewRole].Count())
                    return CustomRole.CustomRoles[RolePercentage[NewRole].RandomItem().Id];

            return null;
        }
    }  
}