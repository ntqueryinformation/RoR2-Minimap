using BepInEx;
using OpenMinimap.Config;
using OpenMinimap.Tracking;
using OpenMinimap.UI;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace OpenMinimap
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Name = "OpenMinimap";
        public const string GUID = "OpenMinimap";
        public const string Version = "1.0.0";

        internal static BepInEx.Logging.ManualLogSource Log;
        internal static new MinimapConfig Config { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Config = new MinimapConfig(base.Config);
            HUD.shouldHudDisplay += OnShouldHudDisplay;
            Stage.onStageStartGlobal += OnStageStart;
            GlobalEventManager.onCharacterDeathGlobal += OnDeath;
            GlobalEventManager.OnInteractionsGlobal += OnInteraction;
            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        // Settings registration is deferred to Start(): after every plugin's Awake, so Risk of
        // Options' own initialization has settled before we hand it our config entries.
        private void Start()
        {
            Config.RegisterRiskOfOptions(
                Name,
                "Minimap (top-right) and optional compass (top-center) HUD using in-game icons. Zoom, orientation, opacity and every object category are selectable. Supports all DLC content.",
                GUID);
        }

        private void OnDestroy()
        {
            HUD.shouldHudDisplay -= OnShouldHudDisplay;
            Stage.onStageStartGlobal -= OnStageStart;
            GlobalEventManager.onCharacterDeathGlobal -= OnDeath;
            GlobalEventManager.OnInteractionsGlobal -= OnInteraction;
        }

        private static void OnShouldHudDisplay(HUD hud, ref bool shouldDisplay)
        {
            if (!hud || hud.gameObject.GetComponent<MinimapController>())
                return;
            hud.gameObject.AddComponent<MinimapController>();
        }

        private static void OnStageStart(Stage stage)
        {
            MapScanner.OnStageStarted();
        }

        private static void OnDeath(DamageReport report)
        {
            MapScanner.QueueEventRescan();
        }

        private static void OnInteraction(Interactor interactor, IInteractable interactable, GameObject gameObject)
        {
            MapScanner.QueueEventRescan();
        }

        // Debug helper: run `openminimap_scan` in the dev console near untracked DLC content and
        // the log will contain every interactable's name, tokens and component list.
        [ConCommand(commandName = "openminimap_scan", helpText = "Dumps all interactables (names, tokens, components) to the log for reporting missed objects.", flags = ConVarFlags.None)]
        private static void CmdScan(ConCommandArgs args)
        {
            foreach (var pi in UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>())
            {
                if (!pi)
                    continue;
                Log.LogInfo($"[scan] purchase '{pi.gameObject.name}' context='{pi.contextToken}' display='{pi.displayNameToken}' available={pi.available} components={Describe(pi.gameObject)}");
            }
            foreach (var gi in UnityEngine.Object.FindObjectsOfType<GenericInteraction>())
            {
                if (!gi)
                    continue;
                Log.LogInfo($"[scan] generic '{gi.gameObject.name}' context='{gi.contextToken}' components={Describe(gi.gameObject)}");
            }
            Log.LogInfo("[scan] done");
        }

        private static string Describe(GameObject go)
        {
            var names = new System.Collections.Generic.List<string>(8);
            foreach (var c in go.GetComponents<Component>())
            {
                if (c)
                    names.Add(c.GetType().Name);
            }
            return "[" + string.Join(",", names) + "]";
        }
    }
}
