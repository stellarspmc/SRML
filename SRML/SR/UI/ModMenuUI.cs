using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SRML.SR.UI { // TODO: in need of a rewrite 
    internal class ModMenuUI : BaseUI {
        //public GameObject infoButtonPrefab;
        private GameObject modScrollbarContent;
        private SRToggleGroup modScrollbarGroup;

        private Text modNamePanelText;
        private Text authorNameText;
        private Text descriptionText;
        private Text versionText;
        private Text dependenciesText;

        public override void Awake() {
            var contentTransform = transform.Find("MainPanel/ListPanel/ContentPanel/Content");
            if (contentTransform != null) {
                modScrollbarContent = contentTransform.gameObject;
                modScrollbarGroup = modScrollbarContent.GetComponent<SRToggleGroup>();
            }

            var modsButton = transform.Find("MainPanel/ListPanel/Buttons/LogsButton");
            if (modsButton != null) modsButton.GetComponent<Button>()?.onClick.AddListener(() => Process.Start(Path.GetFullPath(FileSystem.ModPath)));

            var modInfoPanel = transform.Find("MainPanel/InfoPanel/ContentPanel/Content/ModInfoPanel");
            if (modInfoPanel == null) return;
            modNamePanelText = SafeGetText(modInfoPanel, "NameLabel");
            authorNameText = SafeGetText(modInfoPanel, "AuthorLabel");
            descriptionText = SafeGetText(modInfoPanel, "DescriptionLabel");
            dependenciesText = SafeGetText(modInfoPanel, "DependenciesLabel");
            versionText = SafeGetText(modInfoPanel, "DownloadLabel");

            var closeButton = transform.Find("MainPanel/CloseButton").GetComponent<Button>(); // use java casting? <T extends ILayoutElement> and reuse SafeGet
            closeButton.onClick.AddListener(this.Destroy);
        }

        private static Text SafeGetText(Transform t, string name) {
            var instance = t.Find(name);
            return instance != null ? instance.GetComponent<Text>() : throw new NullReferenceException($"{name} not found in {t.name}");
        }

        public void Start() {
            foreach (var mod in SRModLoader.GetMods()) AddModInfo(mod.ModInfo);
        }

        public void AddModInfo(SRModInfo info) {
            if (modScrollbarContent == null) return;
            var o = Instantiate(modScrollbarContent, modScrollbarContent.transform, false); // init buttons instead of ts
            var button = new ModButton(info);

            var toggle = o.GetComponent<SRToggle>();
            if (toggle != null) {
                toggle.onValueChanged.AddListener(x => { if (x) OnModSelect(button); });
                if (modScrollbarGroup != null) toggle.group = modScrollbarGroup;
            }

            if (o.transform.childCount > 0) {
                var nameTxt = o.transform.GetChild(0).GetComponent<Text>();
                if (nameTxt != null) nameTxt.text = info.Name;
            }

            if (o.transform.childCount <= 1) return;
            var verTxt = o.transform.GetChild(1).GetComponent<Text>();
            if (verTxt != null) verTxt.text = $"Version: {info.Version}";
        }

        public void OnModSelect(ModButton button) {
            if (modNamePanelText != null) modNamePanelText.text = button.name;
            if (authorNameText != null) authorNameText.text = button.author;
            if (versionText != null) versionText.text = button.version;
            if (dependenciesText != null) dependenciesText.text = button.dependencies;
            if (descriptionText != null) descriptionText.text = button.description;
        }

        internal class ModButton {
            public readonly string name;
            public readonly string description;
            public readonly string dependencies;
            public readonly string author;
            public readonly string version;

            public ModButton(SRModInfo info) {
                name = info.Name;
                description = $"Description: {(string.IsNullOrEmpty(info.Description) ? "No info provided" : info.Description)}";
                if (info.URL != null) description += $"\n\n{info.URL}";
                version = $"Version: {info.Version}";
                author = $"Author: {info.Author}";
                dependencies = "Dependencies: ";
                if (info.Dependencies?.Count > 0) {
                    var i = 0;
                    foreach (var dep in info.Dependencies) {
                        dependencies += $"{dep.Key} {dep.Value}";
                        if (i + 1 != info.Dependencies.Count) dependencies += ", ";
                        i++;
                    }
                }
                else dependencies += "None";
            }
        }
    }
}