using CommandSystem;
using Exiled.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.API.Interfaces;

namespace UncomplicatedCustomRoles.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class TryTeam : ParentCommand
    {
        public TryTeam() => LoadGeneratedCommands();

        public override string Command { get; } = "curteam";

        public override string[] Aliases { get; } = new string[] { };

        public override string Description { get; } = "Check your current team";

        public override void LoadGeneratedCommands()
        {
            RegisteredCommands.Add(new List());
            RegisteredCommands.Add(new Info());
            RegisteredCommands.Add(new Role());
            RegisteredCommands.Add(new Spawn());
            RegisteredCommands.Add(new Reload());
            RegisteredCommands.Add(new SpawnPoint());
            RegisteredCommands.Add(new Percentages());
            RegisteredCommands.Add(new Errors());
            RegisteredCommands.Add(new Generate());
            RegisteredCommands.Add(new Update());
            RegisteredCommands.Add(new Owner());
            RegisteredCommands.Add(new Version());
            RegisteredCommands.Add(new Debug());
        }

        public List<IUCRCommand> RegisteredCommands { get; } = new();

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (player is not null)
                response = $"ID: {player.Id} - Your team is {player.Role.Team} -- According to referencehub: {player.ReferenceHub.roleManager.CurrentRole.Team} -- Cast: {player.ReferenceHub.roleManager.CurrentRole.GetType().FullName}";
            else
                response = "Ur not a player!";

            if (DisguiseTeam.RoleBaseList.TryGetValue(player.Id, out PlayerRoleBase role))
                response += $"\nDisguised role found: {role.Team} -- Cast: {role.GetType().FullName}";
            else
                response += $"\nNo disguised role found in {DisguiseTeam.RoleBaseList.Count}!";

            return true;
        }
    }
}