using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SRML.SR.UI.Utils;
using TMPro;
using UnityEngine;

namespace SRML.SR.UI
{
    /// <summary>
    /// Class used to generate an error message
    /// </summary>
    public class ErrorGUI : BaseUI
    {
        internal static GameObject errorUI;
        internal static GameObject initErrorUI;

        public IndividualExceptionUI errorInfo;
        [SerializeField]
        private Transform errorContainer;

        private bool continueAfterClose = false;
        internal bool fallback;

        internal static bool TryCreateExtendedError(MainMenuUI ui, GameObject uiToUse, IEnumerable<SRMod> erroring, bool createBasicOnFail = true)
        {
            /*if (SRMLConfig.FORCE_BASIC_ERROR_HANDLER)
            {
                CreateBasicError($"{errors.First().Value.Item2}", ui);
                return;
            }*/

            GameObject gui = null;
            var srMods = erroring.ToList();
            try
            {
                gui = ui ? ui.InstantiateAndWaitForDestroy(uiToUse) : Instantiate(uiToUse);
                var eg = gui.GetComponent<ErrorGUI>();

                foreach (var exception in srMods)
                    Instantiate(eg.errorInfo, eg.errorContainer).GetComponent<IndividualExceptionUI>().GenerateMessage(exception);

                return true;
            }
            catch (Exception e)
            {
                if (gui?.GetComponent<ErrorGUI>() != null)
                    gui.GetComponent<ErrorGUI>().fallback = true;
                gui?.Destroy();

                UnityEngine.Debug.LogError(e);
                UnityEngine.Debug.LogError("Fatal error encountered, unable to display extended error information. Displaying basic information ...");
                UnityEngine.Debug.LogError(srMods[0].exception);

                try
                {
                    CreateBasicError(srMods[0].exception.ToString(), ui);
                }
                catch { Application.Quit(); }

                return false;
            }
        }

        internal static void CreateBasicError(string error, MainMenuUI mainMenu, bool doAbort = true)
        {
            var menu = GameContext.Instance?.MessageDirector != null
                ? MainMenuUtils.DisplayBlankPanelWithTranslation<ErrorGUI>(mainMenu, "ErrorUI", "t.srml_error", Application.Quit)
                : MainMenuUtils.DisplayBlankPanel<ErrorGUI>(mainMenu, "SRML ERROR", Application.Quit);

            var panel = menu.transform.GetChild(0);
            var title = panel.Find("Title").gameObject;
            var g = Instantiate(title, panel, false);
            g.name = "ErrorText";
            Destroy(g.GetComponent<XlateText>());
            g.GetComponent<TMP_Text>().text = error;
            g.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Top;
            g.GetComponent<TMP_Text>().fontSize *= .8f;
            g.GetComponent<TMP_Text>().enableWordWrapping = true;

            if (doAbort)
            {
                var h = Instantiate(title, panel, false);
                h.name = "AbortText";

                h.GetComponent<XlateText>().SetKey("t.srml_error.abort");
                if (GameContext.Instance?.MessageDirector != null)
                    h.GetComponent<XlateText>().SetKey("t.srml_error.abort");
                else
                {
                    Destroy(h.GetComponent<XlateText>());
                    h.GetComponent<TMP_Text>().text = "Aborting mod loading...";
                }
                h.GetComponent<TMP_Text>().enableWordWrapping = true;
                h.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Bottom;

                var rect2 = h.GetComponent<RectTransform>();
                rect2.anchorMin = new Vector2(0, 0);
                rect2.anchorMax = new Vector2(1, 1);
                rect2.offsetMax = new Vector2(-50, -100);
                rect2.offsetMin = new Vector2(50, 30);
            }

            var rect = g.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMax = new Vector2(-50, -100);
            rect.offsetMin = new Vector2(50, 30);
        }

        internal static void CreateBasicErrorOnMenu(string error, bool doAbort = true)
        {
            SRCallbacks.OnMainMenuLoaded += u => CreateBasicError(error, u, doAbort);
        }

        public void OpenModsFolder() => Process.Start(Path.GetFullPath(FileSystem.ModPath));

        public void OpenLogsFolder() => Process.Start(Path.GetFullPath(Main.StorageProvider.SavePath()));

        public void Continue()
        {
            continueAfterClose = true;

            if (SRModLoader.CurrentLoadingStep == SRModLoader.LoadingStep.INITIALIZATION)
                Main.moveForwardAfterInit = true;
            UnityEngine.Debug.LogWarning("User has chosen to continue despite errors. Don't trust anything past this point!");

            Close();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!fallback && !continueAfterClose)
                Application.Quit();
        }
    }

    public class IndividualExceptionUI : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text title;
        [SerializeField]
        private TMP_Text extended;

        [SerializeField]
        private RectTransform arrow;
        [SerializeField]
        private Vector3 retractedRot;
        [SerializeField]
        private Vector3 extendedRot;

        private bool isExtended = false;
        private string titleText;

        internal void GenerateMessage(SRMod generateFrom)
        {
            /*title.text = GameContext.Instance.MessageDirector.GetBundle("ui").Xlate(MessageUtil.Compose("e.srml_error_title_base", MessageUtil.Taint(generateFrom.ModInfo.Id ?? "<unknown>"),
                $"e.{generateFrom.ModInfo.LoadState.ToString().ToLowerInvariant()}"));*/
            title.text =
                $"{generateFrom.ModInfo.Id ?? "<unknown>"} during {generateFrom.ModInfo.LoadState.ToString().ToLowerInvariant().Replace("_error", "")}";
            extended.SetText(generateFrom.exception.ToString());
        }

        public void ExtendOrRetract()
        {
            isExtended = !isExtended;
            extended.gameObject.SetActive(isExtended);

            arrow.localRotation = Quaternion.Euler(isExtended ? extendedRot : retractedRot);
        }
    }
}