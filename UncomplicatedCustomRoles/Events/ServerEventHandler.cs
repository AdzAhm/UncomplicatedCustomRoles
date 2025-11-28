using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using PlayerRoles;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Manager;

using ServerHandler = Exiled.Events.Handlers.Server;
using WarheadHandler = Exiled.Events.Handlers.Warhead;

namespace UncomplicatedCustomRoles.Events
{
    internal class ServerEventHandler : EventHandlerBase
    {
        internal override void OnRegistered()
        {
            ServerHandler.RespawningTeam += OnRespawningWave;
            ServerHandler.RoundStarted += OnRoundStarted;
            ServerHandler.RoundEnded += OnRoundEnded;
            ServerHandler.WaitingForPlayers += OnWaitingForPlayers;

            // Warhead
            WarheadHandler.Starting += OnWarheadLever;
        }

        internal override void OnUnregistered()
        {
            ServerHandler.RespawningTeam -= OnRespawningWave;
            ServerHandler.RoundStarted -= OnRoundStarted;
            ServerHandler.RoundEnded -= OnRoundEnded;
            ServerHandler.WaitingForPlayers -= OnWaitingForPlayers;

            // Warhead
            WarheadHandler.Starting -= OnWarheadLever;
        }

        public void OnWaitingForPlayers()
        {
            Started = false;
            Plugin.Instance.OnFinishedLoadingPlugins();
        }

        public void OnRoundStarted()
        {
            Started = true;
            FirstRoundPlayers.Clear();

            // Starts the infinite effect thing
            InfiniteEffect.Stop();
            InfiniteEffect.EffectAssociationAllowed = true;
            InfiniteEffect.Start();
        }
        public void OnRoundEnded(RoundEndedEventArgs _)
        {
            Started = false;
            InfiniteEffect.Terminate();
        }

        public void OnRespawningWave(RespawningTeamEventArgs ev)
        {
            LogManager.Silent("Respawning wave");
            if (Spawn.DoHandleWave)
                foreach (Player player in ev.Players)
                    Spawn.SpawnQueue.Add(player.Id);
            else
                Spawn.DoHandleWave = true;
        }

        public void OnWarheadLever(StartingEventArgs ev)
        {
            if (ev.Player.ReferenceHub.GetTeam() == Team.SCPs)
                ev.IsAllowed = false;
        }
    }
}
