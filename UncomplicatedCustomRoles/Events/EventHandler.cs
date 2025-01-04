﻿using Exiled.API.Features;
using System.Collections.Generic;
using UncomplicatedCustomRoles.Manager;
using MEC;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Scp049;
using UncomplicatedCustomRoles.Extensions;
using System;
using UncomplicatedCustomRoles.API.Features;
using Exiled.Events.EventArgs.Scp330;
using CustomPlayerEffects;
using UncomplicatedCustomRoles.API.Interfaces;
using UncomplicatedCustomRoles.API.Features.CustomModules;
using Exiled.Events.EventArgs.Warhead;
using PlayerRoles.Ragdolls;
using Exiled.Events.EventArgs.Scp096;
using UncomplicatedCustomRoles.API.Features.CustomModules.ItemBan;

namespace UncomplicatedCustomRoles.Events
{
    internal class EventHandler
    {
        private static List<int> RagdollAppearanceQueue { get; } = new();

        private static List<int> FirstRoundPlayers { get; } = new();

        private static bool Initialized { get; set; } = false;

        public void OnWaitingForPlayers()
        {
            Plugin.Instance.OnFinishedLoadingPlugins();

        }

        public void OnRoundStarted()
        {
            Initialized = true;
            FirstRoundPlayers.Clear();

            // Starts the infinite effect thing
            InfiniteEffect.Stop();
            InfiniteEffect.EffectAssociationAllowed = true;
            InfiniteEffect.Start();
        }

        public void OnVerified(VerifiedEventArgs ev)
        {
            if (Initialized)
                return;

            FirstRoundPlayers.Add(ev.Player.Id);
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

        public void OnReceivingEffect(ReceivingEffectEventArgs ev)
        {
            if (ev.Player is null)
                return;

            if (!ev.IsAllowed)
                return;

            if (ev.Player.TryGetSummonedInstance(out SummonedCustomRole role))
                if (ev.Effect is SeveredHands && role.Role.MaxScp330Candies >= role.Scp330Count)
                {
                    LogManager.Debug($"Tried to add the {ev.Effect.name} but was not allowed due to {role.Scp330Count} <= {role.Role.MaxScp330Candies}");
                    ev.IsAllowed = false;
                }
                else if (ev.Effect is CardiacArrest && role.Role.IsFriendOf is not null && role.Role.IsFriendOf.Contains(Team.SCPs))
                    ev.IsAllowed = false;
        }

        public void OnPlayerSpawned(SpawnedEventArgs _) { }

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

        public void OnGenerator(ActivatingGeneratorEventArgs ev)
        {
            if (ev.Player.ReferenceHub.GetTeam() == Team.SCPs)
                ev.IsAllowed = false;
        }

        public void OnWarheadLever(StartingEventArgs ev)
        {
            if (ev.Player.ReferenceHub.GetTeam() == Team.SCPs)
                ev.IsAllowed = false;
        }

        public void OnScp079Recontainment(DamagingWindowEventArgs ev)
        {
            if (ev.Player.ReferenceHub.GetTeam() == Team.SCPs && (ev.Window.Type == Exiled.API.Enums.GlassType.Scp079Trigger || ev.Window.Type == Exiled.API.Enums.GlassType.Scp079))
                ev.IsAllowed = false;
        }

        public void OnDying(DyingEventArgs ev)
        {
            if (ev.Player.TryGetSummonedInstance(out SummonedCustomRole customRole) && customRole.HasModule<TutorialRagdoll>())
                RagdollAppearanceQueue.Add(ev.Player.Id);
        }

        public void OnDied(DiedEventArgs ev) => SpawnManager.ClearCustomTypes(ev.Player);

        public void OnRagdollSpawn(SpawningRagdollEventArgs ev)
        {
            if (ev.Player is null) 
                return;

            if (!RagdollAppearanceQueue.Contains(ev.Player.Id))
                return;

            ev.IsAllowed = false;
            RagdollAppearanceQueue.Remove(ev.Player.Id);

            RagdollData data = new(ev.Player.ReferenceHub, ev.DamageHandlerBase, RoleTypeId.Tutorial, ev.Position, ev.Rotation, ev.Nickname, ev.CreationTime);
            Ragdoll.CreateAndSpawn(data);
        }

        public void OnRoundEnded(RoundEndedEventArgs _) => InfiniteEffect.Terminate();

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player is null)
                return;

            // Let's clear for custom types
            SpawnManager.ClearCustomTypes(ev.Player);

            if (!ev.IsAllowed)
                return;

            if (Round.IsLobby)
                return;

            if (ev.NewRole is RoleTypeId.Spectator || ev.NewRole is RoleTypeId.None || ev.NewRole is RoleTypeId.Filmmaker)
                return;

            if (Spawn.Spawning.Contains(ev.Player.Id))
                return;

            if (ev.Player.HasCustomRole())
                return;


            if (ev.Player.IsNPC)
                return;

            if (!FirstRoundPlayers.Contains(ev.Player.Id))
            {
                if (Plugin.Instance.Config.AllowOnlyNaturalSpawns && !Spawn.SpawnQueue.Contains(ev.Player.Id))
                {
                    LogManager.Debug("The player is not in the queue for respawning!");
                    return;
                }
                else if (Spawn.SpawnQueue.Contains(ev.Player.Id))
                {
                    Spawn.SpawnQueue.Remove(ev.Player.Id);
                }
            }

            ICustomRole Role = SpawnManager.DoEvaluateSpawnForPlayer(ev.Player, ev.NewRole);

            if (Role is not null)
            {
                LogManager.Debug($"Summoning player {ev.Player.Nickname} ({ev.Player.Id}) as {Role.Name} ({Role.Id})");
                SpawnManager.SummonCustomSubclass(ev.Player, Role.Id);
                ev.IsAllowed = false;
            }

            LogManager.Debug($"No CustomRole found for player {ev.Player.Nickname}, allowing natural spawn with {ev.NewRole}");
        }

        public void OnHurting(HurtingEventArgs Hurting)
        {
            if (!Hurting.IsAllowed)
                return;

            LogManager.Silent($"DamageHandler of Hurting: {Hurting.Player} {Hurting.Attacker}");

            if (Hurting.Player is not null && Hurting.Attacker is not null && Hurting.Player.IsAlive && Hurting.Attacker.IsAlive)
            {
                if (Hurting.Attacker.TryGetSummonedInstance(out SummonedCustomRole attackerCustomRole))
                {
                    if (attackerCustomRole.Role.IsFriendOf is not null && attackerCustomRole.Role.IsFriendOf.Contains(Hurting.Player.ReferenceHub.GetTeam()))
                    {
                        Hurting.IsAllowed = false;
                        LogManager.Silent("Rejected the event request of Hurting because of is_friend_of - FROM ATTACKER");
                        return;
                    }
                    else if (attackerCustomRole?.HasModule<PacifismUntilDamage>() ?? false)
                        attackerCustomRole.RemoveModules<PacifismUntilDamage>();

                    Hurting.DamageHandler.Damage *= attackerCustomRole.Role.DamageMultiplier;
                }
                else if (Hurting.Player.TryGetSummonedInstance(out SummonedCustomRole playerCustomRole))
                {
                    if (playerCustomRole.Role.IsFriendOf is not null && playerCustomRole.Role.IsFriendOf.Contains(Hurting.Attacker.ReferenceHub.GetTeam()))
                    {
                        Hurting.IsAllowed = false;
                        LogManager.Silent("Rejected the event request of Hurting because of is_friend_of - FROM HURTED");
                        return;
                    }

                    if (attackerCustomRole?.HasModule<PacifismUntilDamage>() ?? false)
                        Hurting.IsAllowed = false;
                }
            }
        }

        public void OnHurt(HurtEventArgs ev)
        {
            if (ev.Player is not null && ev.Attacker is not null && ev.Attacker.IsAlive && ev.Player.IsAlive && ev.Player.TryGetSummonedInstance(out SummonedCustomRole summonedCustomRole))
            {
                summonedCustomRole.LastDamageTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (summonedCustomRole.GetModule(out LifeStealer lifeStealer))
                {
                    lifeStealer.Amount = ev.Amount;
                    lifeStealer.Execute();
                }

                if (summonedCustomRole.GetModule(out HalfLifeStealer halfLifeStealer))
                {
                    halfLifeStealer.Amount = ev.Amount;
                    halfLifeStealer.Execute();
                }
            }
        }

        public void OnEscaping(EscapingEventArgs Escaping)
        {
            if summoned.Role.CanEscape 
                {    
                LogManager.Debug($"Player {Escaping.Player.Nickname} triggered the escaping event as {Escaping.Player.Role.Name}");
                }
            if (Escaping.Player.TryGetSummonedInstance(out SummonedCustomRole summoned))
            {
                LogManager.Debug($"Player IS a custom role: {summoned.Role.Name}");

                if (!summoned.Role.CanEscape)
                {
                    LogManager.Debug($"Player with the role {summoned.Role.Id} ({summoned.Role.Name}) can't escape, so nuh uh!");
                    Escaping.IsAllowed = false;
                    return;
                }

                if (summoned.Role.CanEscape && (summoned.Role.RoleAfterEscape is null || summoned.Role.RoleAfterEscape.Count < 1))
                {
                    LogManager.Debug($"Player with the role {summoned.Role.Id} ({summoned.Role.Name}) evaluated for a natural respawn!");
                    Escaping.IsAllowed = true;
                    return;
                }

                // Try to set the role
                KeyValuePair<bool, object> NewRole = SpawnManager.ParseEscapeRole(summoned.Role.RoleAfterEscape, Escaping.Player);

                if (NewRole.Value is null)
                {
                    Escaping.IsAllowed = true;
                    return;
                }

                if (!NewRole.Key)
                {
                    // Natural role, let's try to parse it
                    if (Enum.TryParse(NewRole.Value.ToString(), out RoleTypeId role)) 
                    {
                        if (role is not RoleTypeId.None)
                        {
                            Escaping.NewRole = role;
                            Escaping.IsAllowed = true;
                        }
                    }
                } 
                else
                {
                    if (int.TryParse(NewRole.Value.ToString(), out int id) && CustomRole.TryGet(id, out ICustomRole role))
                    {
                        Escaping.IsAllowed = false;
                        if (!API.Features.Escape.Bucket.Contains(Escaping.Player.Id))
                        {
                            LogManager.Silent($"Successfully activated the call to method SpawnManager::SummonCustomSubclass(<...>) as the player is not inside the Escape::Bucket bucket! - Adding it...");
                            API.Features.Escape.Bucket.Add(Escaping.Player.Id);
                            SpawnManager.SummonCustomSubclass(Escaping.Player, role.Id);
                        }
                        else
                            LogManager.Silent($"Canceled call to method SpawnManager::SummonCustomSubclass(<...>) due to the presence of the player inside the Escape::Bucket! - Event already fired!");
                    }

                }
            }
        }

        public void OnMakingNoise(MakingNoiseEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (ev.Player is not null && ev.Player.TryGetSummonedInstance(out SummonedCustomRole customRole) && customRole.HasModule<SilentWalker>())
                ev.IsAllowed = false;
        }

        public void OnTriggeringTeslaGate(TriggeringTeslaEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (ev.Player is not null && ev.Player.TryGetSummonedInstance(out SummonedCustomRole customRole) && customRole.HasModule<DoNotTriggerTeslaGates>())
                ev.IsAllowed = false;
        }

        public void OnRespawningWave(RespawningTeamEventArgs Respawn)
        {
            LogManager.Silent("Respawning wave");
            if (Spawn.DoHandleWave)
                foreach (Player Player in Respawn.Players)
                    Spawn.SpawnQueue.Add(Player.Id);
            else
                Spawn.DoHandleWave = true;
        }

        public void OnItemUsed(UsedItemEventArgs UsedItem)
        {
            if (UsedItem.Player is not null && UsedItem.Player.TryGetSummonedInstance(out SummonedCustomRole summoned) && UsedItem.Item.Type is ItemType.SCP500)
                summoned.InfiniteEffects.RemoveAll(effect => effect.Removable);
        }

        public void OnAddingTarget(AddingTargetEventArgs ev)
        {
            if (ev.Player.TryGetSummonedInstance(out SummonedCustomRole summonedInstance))
            { 
                if (ev.Player.ReferenceHub.GetTeam() is Team.SCPs)
                    ev.IsAllowed = false;

                if (summonedInstance.HasModule<DoNotTrigger096>())
                    ev.IsAllowed = false;

                if (summonedInstance.HasModule<PacifismUntilDamage>())
                    ev.IsAllowed = false;
            }
        }

        public void OnPickingUp(PickingUpItemEventArgs ev)
        {
            if (ev.Player.TryGetSummonedInstance(out SummonedCustomRole summonedInstance))
                ev.IsAllowed = ItemBanBase.CheckPickup(summonedInstance, ev.Pickup);
        }

        public static IEnumerator<float> DoSpawnPlayer(Player Player, int Id, bool DoBypassRoleOverwrite = true)
        {
            yield return Timing.WaitForSeconds(0.1f);
            SpawnManager.SummonCustomSubclass(Player, Id, DoBypassRoleOverwrite);
        }
    }
}
