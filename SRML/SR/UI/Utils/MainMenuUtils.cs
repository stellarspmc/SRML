using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SRML.SR.UI.Utils
{
    public static class MainMenuUtils
    {
        public static GameObject DisplayBlankPanel<T>(MainMenuUI mainMenu, string title, Action onClose = null) where T : BaseUI
        {
            var h = Object.Instantiate(mainMenu.optionsUI);
            h.name = title;
            Object.DestroyImmediate(h.GetComponent<OptionsUI>());

            for (var i = 0; i < h.transform.GetChild(0).childCount; i++)
            {
                var v = h.transform.GetChild(0).GetChild(i).gameObject;
                if (v.name == "CloseButton")
                {
                    v.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    v.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Object.Destroy(h);
                        onClose?.Invoke();
                    });
                }
                else if (v.name == "Title" && title != null)
                {
                    Object.DestroyImmediate(v.GetComponent<XlateText>());
                    v.GetComponent<TMP_Text>().text = title;
                }
                else
                {
                    Object.Destroy(v);
                }
            }

            mainMenu.gameObject.SetActive(false);
            var baseUI = h.AddComponent<T>();
            baseUI.onDestroy += () =>
            {
                if (mainMenu)
                {
                    mainMenu.gameObject.SetActive(true);
                }
            };
            return h;
        }

        public static GameObject DisplayBlankPanelWithTranslation<T>(MainMenuUI mainMenu, string name, string key, Action onClose = null) where T : BaseUI
        {
            var h = Object.Instantiate(mainMenu.optionsUI);
            h.name = name;
            Object.DestroyImmediate(h.GetComponent<OptionsUI>());

            for (var i = 0; i < h.transform.GetChild(0).childCount; i++)
            {
                var v = h.transform.GetChild(0).GetChild(i).gameObject;
                if (v.name == "CloseButton")
                {
                    v.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    v.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Object.Destroy(h);
                        onClose?.Invoke();
                    });
                }
                else if (v.name == "Title" && key != null)
                {
                    v.GetComponent<XlateText>().SetKey(key);
                }
                else
                {
                    Object.Destroy(v);
                }
            }

            mainMenu.gameObject.SetActive(false);
            var baseUI = h.AddComponent<T>();
            baseUI.onDestroy += () =>
            {
                if (mainMenu)
                {
                    mainMenu.gameObject.SetActive(true);
                }
            };
            return h;
        }

        public static GameObject AddMainMenuButton(MainMenuUI mainMenu, string text, Action onClicked)
        {
            var mode = mainMenu.transform.Find("StandardModePanel/OptionsButton");
            var g = Object.Instantiate(mode.gameObject);
            g.name = text;
            g.transform.SetParent(mode.parent, false);
            g.transform.localPosition = new Vector3(0, 0);
            Object.Destroy(g.GetComponent<XlateText>());
            var button = g.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(new UnityAction(onClicked));

            g.GetComponentInChildren<TMP_Text>().text = text;
            return g;
        }

        public static GameObject AddMainMenuButtonWithTranslation(MainMenuUI mainMenu, string name, string key, Action onClicked)
        {
            var mode = mainMenu.transform.Find("StandardModePanel/OptionsButton");
            var g = Object.Instantiate(mode.gameObject);
            g.name = name;
            g.transform.SetParent(mode.parent, false);
            g.transform.localPosition = new Vector3(0, 0);
            var button = g.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(new UnityAction(onClicked));

            g.GetComponentInChildren<XlateText>().SetKey(key);
            return g;
        }
    }
}
