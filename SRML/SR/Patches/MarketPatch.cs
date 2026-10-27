using HarmonyLib;
using SRML.SR.UI;
using SRML.Utils;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SRML.SR.Patches;

[HarmonyPatch(typeof(MarketUI), "Start")]
internal static class MarketPatch
{
    public static void Prefix(MarketUI __instance)
    {
        __instance.plorts = __instance.plorts.Where(x => !PlortRegistry.plortsToPatch.Any(y => y.id == x.id)).ToArray().AddRangeToArray(PlortRegistry.plortsToPatch.ToArray());
        if (SRMLConfig.CREATE_MARKET_BUTTON)
        {
            var ui = PrefabUtils.CopyPrefab(Object.FindObjectsOfType<ScienceUIActivator>().First(x => x.uiPrefab.name == "RefineryUI").uiPrefab);
            ui.name = "ExtendedMarketUI";
            var marketUI = ui.AddComponent<ExtendedMarketUI>();
            var refineryUI = ui.GetComponent<RefineryUI>();
            marketUI.inventoryGridPanel = refineryUI.inventoryGridPanel;
            marketUI.plorts = __instance.plorts;

            var entry = PrefabUtils.CopyPrefab(refineryUI.inventoryEntryPrefab);
            entry.name = "ExtendedMarketUIEntry";
            entry.transform.Find("CountsOuterPanel/CountsPanel/Counts").GetComponent<TMP_Text>().enableWordWrapping = false;
            entry.transform.Find("CountsOuterPanel/CountsPanel/Counts").gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            entry.transform.Find("CountsOuterPanel/CountsPanel/Counts").GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            entry.transform.Find("CountsOuterPanel/CountsPanel").gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            entry.transform.Find("CountsOuterPanel/CountsPanel").GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            entry.transform.Find("CountsOuterPanel/CountsPanel").GetComponent<HorizontalLayoutGroup>().spacing = 1;
            entry.transform.Find("CountsOuterPanel/CountsPanel").GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            var icon = new GameObject("ChangeIcon");
            icon.transform.SetParent(entry.transform.Find("CountsOuterPanel/CountsPanel"));
            icon.transform.SetSiblingIndex(0);
            icon.AddComponent<Image>();
            var element = icon.AddComponent<LayoutElement>();
            element.minWidth = 24;
            element.minHeight = 24;
            element.preferredHeight = 24;
            element.preferredWidth = 24;
            marketUI.inventoryEntryPrefab = entry;
            marketUI.increasingPriceIcon = __instance.upImg;
            marketUI.decreasingPriceIcon = __instance.downImg;
            marketUI.staticPriceIcon = __instance.unchImg;

            ui.transform.Find("MainPanel/TitlePanel/Title").GetComponent<XlateText>().SetKey("t.market");
            ui.transform.Find("MainPanel/TitlePanel/TitleIcon").GetComponent<Image>().sprite = SceneContext.Instance.PediaDirector.entries.First(x => x.id == PediaDirector.Id.PLORT_MARKET).icon;
            ui.transform.Find("MainPanel/Status").gameObject.Destroy();
            ui.RemoveComponent<RefineryUI>();

            var techActivator = Object.Instantiate(__instance.transform.parent.Find("SlimePedia").Find("techActivator").gameObject);
            techActivator.transform.parent = __instance.transform.parent;
            techActivator.GetChild(1).RemoveComponentImmediate<PediaUIActivator>();
            var activator = techActivator.GetChild(1).AddComponent<UIActivator>();
            activator.uiPrefab = ui;
            techActivator.transform.localPosition = new Vector3(-1f, 0f, 0.5f);
            techActivator.transform.localEulerAngles = new Vector3(0f, -5f, 0f);
        }
        __instance.plorts = __instance.plorts.Take(22).ToArray();
    }
}