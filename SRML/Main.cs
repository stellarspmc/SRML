using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SRML.Config;
using SRML.Console;
using SRML.Editor;
using SRML.SR;
using SRML.SR.UI;
using SRML.SR.Utils;
using SRML.SR.Utils.BaseObjects;
using SRML.Utils;
using TMPro;
using UnityEngine;

namespace SRML
{
    internal static class Main
    {
        public const string VERSION_STRING = "0.3.0";

        private static bool isInitialized;
        private static bool isPreLoaded;
        private static bool isLoaded;
        private static bool isPostLoaded;

        internal static bool moveForwardAfterInit = true;

        internal static GameObject context;
        internal static Transform prefabParent;
        internal static FileStorageProvider StorageProvider;
        internal static ConfigFile config;
        internal static AssetBundle uiBundle;

        internal static bool InitializeSRMLThenBeginLoad(StandaloneStartScreen __instance)
        {
            // run after first frame, but before loading commences
            if (isInitialized || !__instance.pastFirstFrame || __instance.isLoading)
                return moveForwardAfterInit;

            Initialize();
            return false;
        }

        private static void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;

            Debug.Log("SRML has successfully invaded the game!");

            // Sentry SDK reports errors to Monomi. we don't want to flood them with BS mod errors
            // vital to be done before Anything
            var sentrySdk = UnityEngine.Object.FindObjectOfType<SentrySdk>();
            if (sentrySdk != null)
            {
                sentrySdk.Dsn = string.Empty;
                var field = sentrySdk.GetType().GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
                field?.SetValue(null, null);
                sentrySdk.StopAllCoroutines();
                Application.logMessageReceived -= sentrySdk.OnLogMessageReceived;
                UnityEngine.Object.Destroy(sentrySdk, 1f);
                Debug.Log("Disabling Sentry SDK");
            }

            context = new GameObject("SRMLContext");
            UnityEngine.Object.DontDestroyOnLoad(context);

            prefabParent = new GameObject("PrefabParent").transform;
            prefabParent.gameObject.SetActive(false);
            prefabParent.SetParent(context.transform);

            StorageProvider = new FileStorageProvider();
            StorageProvider.Initialize();

            FileLogger.Init();
            Console.Console.Init();

            config = ConfigFile.GenerateConfig(typeof(SRMLConfig));
            config.TryLoadFromFile();

            uiBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("srml"));

            ErrorGUI.errorUI = uiBundle.LoadAsset<GameObject>("SRMLErrorUI");
            var ui = ErrorGUI.errorUI.GetComponent<ErrorGUI>();

            ErrorGUI.initErrorUI = uiBundle.LoadAsset<GameObject>("InitializationErrorUI");
            var ui2 = ErrorGUI.initErrorUI.GetComponent<ErrorGUI>();

            // assetbundles don't serialize TMP_Text alignment for some reason
            foreach (var text in ui.GetComponentsInChildren<TMP_Text>(true))
                text.alignment = TextAlignmentOptions.Midline;
            foreach (var text in ui2.GetComponentsInChildren<TMP_Text>(true))
                text.alignment = TextAlignmentOptions.Midline;
            foreach (var text in ui.errorInfo.GetComponentsInChildren<TMP_Text>(true))
                text.alignment = TextAlignmentOptions.MidlineLeft;
            foreach (var text in ui2.errorInfo.GetComponentsInChildren<TMP_Text>(true))
                text.alignment = TextAlignmentOptions.MidlineLeft;

            foreach (var c in ui2.GetComponentsInChildren<Component>())
            {
                if (c.GetType().Name.Contains("Styler"))
                    UnityEngine.Object.DestroyImmediate(c);
            }

            // things break if this doesn't exist for reasons
            foreach (var v in Assembly.GetExecutingAssembly().GetTypes())
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(v.TypeHandle);

            HarmonyPatcher.PatchAll();

            // crashes game, removed
            /*HarmonyPatcher.Instance.Patch(typeof(GameContext).GetMethod("Awake"),
                prefix: new HarmonyMethod(typeof(Main).GetMethod("PreLoad", BindingFlags.NonPublic | BindingFlags.Static)));
            HarmonyPatcher.Instance.Patch(typeof(GameContext).GetMethod("Start"),
                prefix: new HarmonyMethod(typeof(Main).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static)));
            HarmonyPatcher.Instance.Patch(typeof(GameContext).GetMethod("Start"),
                postfix: new HarmonyMethod(typeof(Main).GetMethod("PostLoad", BindingFlags.NonPublic | BindingFlags.Static)));*/

            // this patch ensures that steam doesn't try to add modded achievements, because it wouldn't like that
            var sm = typeof(GameContext).Assembly.GetType("SteamManager", false, true);
            if (sm != null)
            {
                HarmonyPatcher.Instance.Patch(sm.GetMethod("AddAchievement"),
                    prefix: new HarmonyMethod(typeof(AchievementRegistry).GetMethod("ModdedAchievementPatch", BindingFlags.NonPublic | BindingFlags.Static)));
            }

            SRModLoader.InitializeMods();

            HarmonyOverrideHandler.PatchAll(); // I Don't know what this is; as far as I can tell, it's a system that was never finished and does Nothing

            var erroring = SRModLoader.Mods.Values.Where(x => x.ModInfo.EncounteredError);
            var srMods = erroring.ToList();
            foreach (var mod in srMods)
                Debug.LogError(mod.exception);

            moveForwardAfterInit = srMods.Count == 0;
            if (!moveForwardAfterInit && !ErrorGUI.TryCreateExtendedError(null, ErrorGUI.initErrorUI, srMods, false))
                Application.Quit();
        }

        /// <summary>
        /// Called before GameContext.Awake()
        /// </summary>
        public static void PreLoad()
        {
            if (isPreLoaded) return;
            isPreLoaded = true;

            SRModLoader.PreLoadMods();
            IdentifiableRegistry.CategorizeAllIds();
            GadgetRegistry.CategorizeAllIds();
            ReplacerCache.ClearCache();
        }

        /// <summary>
        /// Called before GameContext.Start()
        /// </summary>
        public static void Load()
        {
            if (isLoaded)
                return;
            isLoaded = true;

            BaseObjects.Populate();
            SRCallbacks.OnLoad();
            KeyBindManager.ReadBinds();
            SlimeRegistry.Initialize(GameContext.Instance.SlimeDefinitions);

            context.AddComponent<ModManager>();
            context.AddComponent<KeyBindManager.ProcessAllBindings>();

            SRModLoader.LoadMods();
            GameContext.Instance.SlimeDefinitions.RefreshEatmaps();
        }

        /// <summary>
        /// Called after Load
        /// </summary>
        public static void PostLoad()
        {
            if (isPostLoaded)
                return;
            isPostLoaded = true;

            SRModLoader.PostLoadMods();
        }

        public static void Reload()
        {
            try
            {
                SRModLoader.ReloadMods();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        internal static void Unload()
        {
            try
            {
                SRModLoader.UnloadMods();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        internal static void Update()
        {
            try
            {
                SRModLoader.UpdateMods();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        internal static void FixedUpdate()
        {
            try
            {
                SRModLoader.UpdateModsFixed();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        internal static void LateUpdate()
        {
            try
            {
                SRModLoader.UpdateModsLate();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    internal class ModManager : MonoBehaviour
    {
        void Update() => Main.Update();

        void FixedUpdate() => Main.FixedUpdate();

        void LateUpdate() => Main.LateUpdate();

        void OnApplicationQuit() => Main.Unload();
    }
}
