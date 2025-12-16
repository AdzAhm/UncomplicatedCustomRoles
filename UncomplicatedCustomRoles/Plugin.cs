/*
 * This file is a part of the UncomplicatedCustomRoles project.
 * 
 * Copyright (c) 2023-present FoxWorn3365 (Federico Cosma) <me@fcosma.it>
 * 
 * This file is licensed under the GNU Affero General Public License v3.0.
 * You should have received a copy of the AGPL license along with this file.
 * If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using UncomplicatedCustomRoles.Integrations;
using UncomplicatedCustomRoles.Manager;
using UncomplicatedCustomRoles.API.Features;
using HarmonyLib;
using UncomplicatedCustomRoles.Manager.NET;
using System.Threading.Tasks;
using UncomplicatedCustomRoles.Events;
using LabApi.Events.CustomHandlers;
using System.Collections.Generic;

namespace UncomplicatedCustomRoles
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomRoles";

        public override string Prefix => "UncomplicatedCustomRoles";

        public override string Author => "FoxWorn3365, Dr.Agenda";

        public override Version Version { get; } = new(9, 2, 0, 1);

        public override Version RequiredExiledVersion { get; } = new(9, 1, 0);

        public override PluginPriority Priority => PluginPriority.Higher;

        internal static Plugin Instance;

        internal LabApiEventHandler LabApiEventHandler;

        internal static HttpManager HttpManager;

        internal Harmony _harmony;

        public override void OnEnabled()
        {
            Instance = this;

            // QoL things
            LogManager.History.Clear();
            API.Features.Escape.Bucket.Clear();
            HttpManager = new("ucr");

            CustomRole.CustomRoles.Clear();
            CustomRole.NotLoadedRoles.Clear();

            EventHandlerBase.Register(new List<EventHandlerBase>()
            {
                new ServerEventHandler(),
                new PlayerEventHandler(),
                new ScpEventHandler()
            });


            LabApiEventHandler = new();
            CustomHandlersManager.RegisterEventsHandler(LabApiEventHandler);

            Task.Run(delegate
            {
                if (HttpManager.LatestVersion.CompareTo(Version) > 0)
                    LogManager.Warn($"You are NOT using the latest version of UncomplicatedCustomRoles!\nCurrent: v{Version} | Latest available: v{HttpManager.LatestVersion}\nDownload it from GitHub: https://github.com/FoxWorn3365/UncomplicatedCustomRoles/releases/latest");

                VersionManager.Init();
            });

            FileConfigs.Welcome();
            FileConfigs.Welcome(Server.Port.ToString());
            FileConfigs.LoadAll();
            FileConfigs.LoadAll(Server.Port.ToString());

            // Start communicating with the endpoint API
            SpawnPointApiCommunicator.Init();

            // Patch with Harmony
            _harmony = new($"com.ucs.ucr_exiled-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            _harmony.PatchAll();
            //PlayerInfoPatch.TryPatchCedMod();

            RespawnTimer.Enable();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            _harmony.UnpatchAll();

            CustomHandlersManager.UnregisterEventsHandler(LabApiEventHandler);
            LabApiEventHandler = null;

            EventHandlerBase.UnregisterAll();

            HttpManager.UnregisterEvents();

            Instance = null;

            base.OnDisabled();
        }

        /// <summary>
        /// Invoked after the server finish to load every plugin
        /// </summary>
        public void OnFinishedLoadingPlugins()
        {
            // Register ScriptedEvents integration
            ScriptedEvents.RegisterCustomActions();

            // Run the import managet
            ImportManager.Init();

            if (Config.EnableBasicLogs)
            {
                LogManager.Info($"Thanks for using UncomplicatedCustomRoles v{Version.ToString(3)} by {Author}!", ConsoleColor.Blue);
                LogManager.Info("For support and to remain updated please join our Discord: https://discord.gg/5StRGu8EJV", ConsoleColor.DarkYellow);
            }
        }

        /// <summary>
        /// Invoked before EXILED starts to unload every plugin
        /// </summary>
        public void OnStartingUnloadingPlugins()
        {
            ScriptedEvents.UnregisterCustomActions();
        }
    }
}