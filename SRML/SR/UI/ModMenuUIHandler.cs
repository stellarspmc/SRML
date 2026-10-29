using SRML.SR.UI.Utils;
using UnityEngine;

namespace SRML.SR.UI {
    internal static class ModMenuUIHandler {
        private static GameObject mainMenuUIPrefab;

        internal static void OnMainMenuLoaded(MainMenuUI ui) {
            MainMenuUtils.AddMainMenuButtonWithTranslation(ui, "ModsButton", "b.mods", () => {
                if (mainMenuUIPrefab == null) return;
                var instance = Object.Instantiate(mainMenuUIPrefab, ui.transform.parent, false);
                instance.SetActive(true);
            }).transform.SetSiblingIndex(5);

            var watermark = GameObject.Find("TitleUI/Watermark");
            watermark.GetComponent<TMPro.TMP_Text>().alignment = TMPro.TextAlignmentOptions.Midline;
            watermark.GetComponent<TMPro.TMP_Text>().text += $"\nSRML v {Main.VERSION_STRING}";
            ((RectTransform)watermark.transform).anchoredPosition = new Vector2(0f, 12f);

            if (mainMenuUIPrefab) return;

            var h = Main.uiBundle.LoadAsset<GameObject>("ModUI");
            //h.AddComponent<ModMenuUI>().infoButtonPrefab = Main.uiBundle.LoadAsset<GameObject>("ModInfoButton");
            mainMenuUIPrefab = h;
        }
    }
}
