using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnityEngine.UI;
using I2.Loc;
using HMLLibrary;
using Object = UnityEngine.Object;

public class ModeSettings : Mod
{
    static RectTransform prefabParent;
    static GameObject uiPrefab;
    static GameObject checkboxPrefab;
    static GameObject comboboxPrefab;
    static GameObject buttonPrefab;
    static GameObject inputPrefab;
    static GameObject CurrentUI;
    public static MenuType ownUI = (MenuType)21;
    public void Start()
    {
        prefabParent = new GameObject("prefabParent", typeof(RectTransform)).GetComponent<RectTransform>();
        DontDestroyOnLoad(prefabParent);
        prefabParent.gameObject.SetActive(false);

        var OptionMenuParent = Traverse.Create(ComponentManager<Settings>.Value).Field("optionsCanvas").GetValue<GameObject>().transform.Find("OptionMenuParent");
        var background = OptionMenuParent.Find("BrownBackground");
        var closeButton = OptionMenuParent.Find("CloseButton").GetComponent<Button>();
        var contentBox = OptionMenuParent.Find("TabContent/Graphics");

        uiPrefab = new GameObject("GameModeUI", typeof(RectTransform), typeof(GraphicRaycaster));
        uiPrefab.GetComponent<GraphicRaycaster>().enabled = false;
        var uiRect = uiPrefab.GetComponent<RectTransform>();
        uiRect.SetParent(prefabParent,false);
        uiRect.anchorMin = Vector2.one * 0.5f;
        uiRect.anchorMax = Vector2.one * 0.5f;
        var optionsSize = OptionMenuParent.GetComponent<RectTransform>().sizeDelta;
        uiRect.offsetMin = -optionsSize / 2;
        uiRect.offsetMax = optionsSize / 2;
        var newBackground = Instantiate(background, uiRect, false).GetComponent<RectTransform>();
        newBackground.name = background.name;
        newBackground.anchorMin = Vector2.zero;
        newBackground.anchorMax = Vector2.one;
        newBackground.offsetMin = Vector2.zero;
        newBackground.offsetMax = Vector2.zero;
        var newClose = Instantiate(closeButton, uiRect, false);
        newClose.name = closeButton.name;
        newClose.onClick = new Button.ButtonClickedEvent();
        var newCloseRect = newClose.GetComponent<RectTransform>();
        var closeSize = newCloseRect.sizeDelta;
        newCloseRect.anchorMin = Vector2.one;
        newCloseRect.anchorMax = Vector2.one;
        newCloseRect.offsetMin = -closeSize * 1.5f;
        newCloseRect.offsetMax = -closeSize / 2;
        var newContent = Instantiate(contentBox, uiRect, false).GetComponent<RectTransform>();
        newContent.name = "Container";
        newContent.gameObject.SetActive(true);
        newContent.anchorMin = Vector2.zero;
        newContent.anchorMax = Vector2.one;
        newContent.offsetMin = closeSize / 2;
        newContent.offsetMax = closeSize * new Vector2(-0.5f, -2f);
        DestroyImmediate(newContent.GetComponent<GraphicsSettingsBox>());
        var fitter = newContent.Find("Viewport/Content").gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
        var scroll = newContent.Find("Scrollbar Vertical").GetComponent<Scrollbar>();
        newContent.GetComponent<ScrollRect>().verticalScrollbar = scroll;
        scroll.value = 1;
        foreach (Transform t in newContent.Find("Viewport/Content"))
            Destroy(t.gameObject);
        checkboxPrefab = Instantiate(OptionMenuParent.Find("TabContent/General/Viewport/Content/Quick build").gameObject, prefabParent, false);
        checkboxPrefab.name = "Checkbox";
        var checkbox = checkboxPrefab.transform.Find("Toggle").GetComponent<Toggle>();
        checkbox.onValueChanged = new Toggle.ToggleEvent();
        checkbox.isOn = false;
        checkboxPrefab.transform.Find("Text").GetComponent<Text>().text = "Option Name";
        destroyLocalizations(checkboxPrefab);

        comboboxPrefab = Instantiate(OptionMenuParent.Find("TabContent/General/Viewport/Content/Language").gameObject, prefabParent, false);
        comboboxPrefab.name = "Combobox";
        var drop = comboboxPrefab.transform.Find("LanguageDropdown").GetComponent<Dropdown>();
        drop.name = "Dropdown";
        drop.onValueChanged = new Dropdown.DropdownEvent();
        foreach (var c in drop.gameObject.GetComponentsInChildren<SetLanguageDropdown>())
        {
            c.enabled = false;
            DestroyImmediate(c, true);
        }
        drop.ClearOptions();
        drop.AddOptions(new List<Dropdown.OptionData> { new Dropdown.OptionData("test") });
        drop.itemText.text = drop.options[0].text;
        comboboxPrefab.transform.Find("Text").GetComponent<Text>().text = "Option Name";
        destroyLocalizations(comboboxPrefab);

        inputPrefab = Instantiate(comboboxPrefab, prefabParent, false);
        inputPrefab.name = "Input";
        var inputF = inputPrefab.transform.Find("Dropdown");
        inputF.name = "InputField";
        DestroyImmediate(inputF.GetComponent<Dropdown>(), true);
        InputField tmp = inputF.gameObject.AddComponent<InputField>();
        tmp.textComponent = inputF.transform.Find("Label").GetComponent<Text>();
        tmp.textComponent.rectTransform.offsetMax = -tmp.textComponent.rectTransform.offsetMin;
        DestroyImmediate(inputF.Find("Arrow").gameObject, true);
        DestroyImmediate(inputF.Find("Template").gameObject, true);

        buttonPrefab = Instantiate(OptionMenuParent.Find("TabContent/Controls/Viewport/Content/ResetKeybinds").gameObject, prefabParent, false);
        buttonPrefab.name = "Button";
        Button button = buttonPrefab.transform.Find("ResetKeybindsButton").GetComponent<Button>();
        button.name = "Button";
        button.onClick = new Button.ButtonClickedEvent();
        button.transform.Find("Text").GetComponent<Text>().text = "Button Name";
        destroyLocalizations(buttonPrefab);

        if (ComponentManager<CanvasHelper>.Value) Patch_CanvasHelper_Create.Prefix(ComponentManager<CanvasHelper>.Value);

        Log("Mod has been loaded!");
    }

    public void OnModUnload()
    {
        CloseMenu();

        if (ComponentManager<CanvasHelper>.Value)
        {
            var menusT = Traverse.Create(ComponentManager<CanvasHelper>.Value).Field<GameMenu[]>("gameMenus");
            var menus = new List<GameMenu>(menusT.Value);
            menus.RemoveAll((x) => x.menuType == ownUI);
            menusT.Value = menus.ToArray();
        }

        Destroy(prefabParent.gameObject);

        Log("Mod has been unloaded!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals) && RAPI.IsCurrentSceneGame() && CanvasHelper.ActiveMenu == MenuType.None)
        {
            OpenUI(GameModeValueManager.GetCurrentGameModeValue());
        }
        if (CanvasHelper.ActiveMenu == ownUI && MyInput.GetButtonDown("Cancel"))
            CurrentUI?.transform.Find("CloseButton").GetComponent<Button>().onClick.Invoke();
    }

    public static void destroyLocalizations(GameObject gO)
    {
        foreach (Localize localize in gO.GetComponentsInChildren<Localize>())
        {
            localize.enabled = false;
            DestroyImmediate(localize, true);
        }
    }

    public static void LogTree(Transform transform)
    {
        Debug.Log(GetLogTree(transform));
    }

    public static string GetLogTree(Transform transform, string prefix = " -")
    {
        string str = "\n" + prefix + transform.name;
        foreach (Object obj in transform.GetComponents<Object>())
            str += ": " + obj.GetType().Name;
        foreach (Transform sub in transform)
            str += GetLogTree(sub, prefix + "--");
        return str;
    }

    public static void OpenUI(object value, List<object> previous = null)
    {
        if (CurrentUI)
            Destroy(CurrentUI);
        if (CanvasHelper.ActiveMenu != ownUI)
            ComponentManager<CanvasHelper>.Value.OpenMenu(ownUI);
        CurrentUI = Instantiate(uiPrefab, null, false);
        CurrentUI.transform.SetParent(ComponentManager<CanvasHelper>.Value.transform, false);
        CurrentUI.GetComponent<GraphicRaycaster>().enabled = true;
        var close = CurrentUI.transform.Find("CloseButton").GetComponent<Button>();
        if (previous == null || previous.Count == 0)
            close.onClick.AddListener(CloseMenu);
        else
            close.onClick.AddListener(() => { OpenUI(previous.Last(), previous.GetRange(0, previous.Count - 1)); });
        var content = CurrentUI.transform.Find("Container/Viewport/Content");
        var alt = false;
        foreach (var f in GetAllFields(value))
            if (!f.IsStatic && f.FieldType != typeof(GameMode))
            {
                alt = !alt;
                GameObject o = null;
                Text text = null;
                System.Action resize = null;
                if (f.FieldType == typeof(int))
                {
                    o = Instantiate(inputPrefab, content, false);
                    var i = o.transform.Find("InputField").GetComponent<InputField>();
                    i.text = f.GetValue(value).ToString();
                    i.characterValidation = InputField.CharacterValidation.Integer;
                    i.onValueChanged.AddListener((s) => f.SetValue(value, int.Parse(s)));
                    text = o.transform.Find("Text").GetComponent<Text>();
                }
                else if (f.FieldType == typeof(float))
                {
                    o = Instantiate(inputPrefab, content, false);
                    var i = o.transform.Find("InputField").GetComponent<InputField>();
                    i.text = f.GetValue(value).ToString();
                    i.characterValidation = InputField.CharacterValidation.Decimal;
                    i.onValueChanged.AddListener((s) => f.SetValue(value, float.Parse(s)));
                    text = o.transform.Find("Text").GetComponent<Text>();
                }
                else if (f.FieldType == typeof(double))
                {
                    o = Instantiate(inputPrefab, content, false);
                    var i = o.transform.Find("InputField").GetComponent<InputField>();
                    i.text = f.GetValue(value).ToString();
                    i.characterValidation = InputField.CharacterValidation.Decimal;
                    i.onValueChanged.AddListener((s) => f.SetValue(value, double.Parse(s)));
                    text = o.transform.Find("Text").GetComponent<Text>();
                }
                else if (f.FieldType == typeof(string))
                {
                    o = Instantiate(inputPrefab, content, false);
                    var i = o.transform.Find("InputField").GetComponent<InputField>();
                    i.characterValidation = InputField.CharacterValidation.None;
                    i.text = (string)f.GetValue(value);
                    i.onValueChanged.AddListener((s) => f.SetValue(value, s));
                    text = o.transform.Find("Text").GetComponent<Text>();
                }
                else if (f.FieldType == typeof(bool))
                {
                    o = Instantiate(checkboxPrefab, content, false);
                    var i = o.transform.Find("Toggle").GetComponent<Toggle>();
                    i.isOn = (bool)f.GetValue(value);
                    i.onValueChanged.AddListener((s) => f.SetValue(value, s));
                    text = o.transform.Find("Text").GetComponent<Text>();
                }
                else if (f.FieldType.IsEnum)
                {
                    o = Instantiate(comboboxPrefab, content, false);
                    var i = o.transform.Find("Dropdown").GetComponent<Dropdown>();
                    List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
                    foreach (object item in System.Enum.GetValues(f.FieldType))
                        options.Add(new Dropdown.OptionData(item.ToString()));
                    i.ClearOptions();
                    i.AddOptions(options);
                    i.value = options.FindIndex((x) => x.text == f.GetValue(value).ToString());
                    i.onValueChanged.AddListener((s) => f.SetValue(value, System.Enum.Parse(f.FieldType, options[s].text)));
                    text = o.transform.Find("Text").GetComponent<Text>();
                }
                else
                {
                    o = Instantiate(buttonPrefab, content, false);
                    var i = o.transform.Find("Button").GetComponent<Button>();
                    i.onClick.AddListener(() => {
                        if (previous == null)
                            previous = new List<object> { value };
                        else
                            previous.Add(value);
                        OpenUI(f.GetValue(value), previous);
                    });
                    text = i.transform.Find("Text").GetComponent<Text>();
                    var button = i.GetComponent<RectTransform>();
                    resize = () => button.offsetMax += new Vector2(text.preferredWidth + text.preferredHeight - button.offsetMax.x, 0);
                }

                if (o && o.GetComponent<Image>())
                    o.GetComponent<Image>().enabled = alt;
                if (text)
                {
                    text.text = GetName(f.Name);
                    if (resize == null)
                        text.rectTransform.offsetMax += new Vector2(text.preferredWidth - text.rectTransform.sizeDelta.x, 0);
                    else
                        resize();
                }
            }
    }

    static void CloseMenu()
    {
        if (CurrentUI)
            Destroy(CurrentUI);
        if (ComponentManager<CanvasHelper>.Value)
            ComponentManager<CanvasHelper>.Value.CloseMenu(ownUI);
    }
    public static List<FieldInfo> GetAllFields(object value)
    {
        var s = new List<FieldInfo>();
        if (value == null)
            return s;
        var t = value.GetType();
        while (t != typeof(object) && t != typeof(Object))
        {
            foreach (var f in t.GetFields((BindingFlags)(-1)))
                if (!f.IsStatic)
                    s.Add(f);
            t = t.BaseType;
        }
        return s;
    }
    public static string GetName(string o)
    {
        var l = new List<string>();
        var i = 1;
        while (i < o.Length)
        {
            if (o[i] == '_')
            {
                l.Add(o.Substring(0, i));
                o = o.Remove(0, i + 1);
                i = 0;
            }
            else if (char.IsUpper(o[i]))
            {
                l.Add(o.Substring(0, i));
                o = o.Remove(0, i);
                i = 0;
            }
            i++;
        }
        if (o.Length > 0)
            l.Add(o);
        return l.Join(delimiter: " ");
    }
}

[HarmonyPatch(typeof(CanvasHelper),"Awake")]
class Patch_CanvasHelper_Create
{
    public static void Prefix(CanvasHelper __instance)
    {
        var menusT = Traverse.Create(__instance).Field<GameMenu[]>("gameMenus");
        var menus = new List<GameMenu>(menusT.Value);
        menus.Add(new GameMenu()
        {
            menuType = ModeSettings.ownUI,
            canvasRaycaster = null,
            menuObjects = new List<GameObject>(),
            messageReciever = null,
            recieveEventMessages = false
        });
        menusT.Value = menus.ToArray();
    }
}