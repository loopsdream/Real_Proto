#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class LoginUICreator : EditorWindow
{
    private GameObject loginPanel;

    [MenuItem("Tools/Create Login UI")]
    static void Init()
    {
        LoginUICreator window = (LoginUICreator)EditorWindow.GetWindow(typeof(LoginUICreator));
        window.titleContent = new GUIContent("Login UI Creator");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Login UI Auto Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        loginPanel = (GameObject)EditorGUILayout.ObjectField("Login Panel", loginPanel, typeof(GameObject), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Login UI", GUILayout.Height(40)))
        {
            if (loginPanel != null)
            {
                CreateLoginUI();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign Login Panel first!", "OK");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Signup UI", GUILayout.Height(40)))
        {
            if (loginPanel != null)
            {
                CreateSignupUI();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign Login Panel first!", "OK");
            }
        }
    }

    void CreateLoginUI()
    {
        // Find or create LoginForm
        Transform loginForm = loginPanel.transform.Find("LoginForm");
        if (loginForm == null)
        {
            GameObject loginFormGO = new GameObject("LoginForm");
            loginFormGO.transform.SetParent(loginPanel.transform, false);

            RectTransform rt = loginFormGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 800);

            // Add background
            Image bg = loginFormGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            VerticalLayoutGroup vlg = loginFormGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 60, 60);
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            loginForm = loginFormGO.transform;
        }

        // Title
        CreateText(loginForm, "LoginTitle", "Email Login", 36, TextAlignmentOptions.Center, 80);

        // Email InputField
        CreateInputField(loginForm, "EmailInput", "Email", TMP_InputField.ContentType.EmailAddress, 70);

        // Password InputField
        CreateInputField(loginForm, "PasswordInput", "Password", TMP_InputField.ContentType.Password, 70);

        // Remember Me Toggle
        CreateToggle(loginForm, "RememberToggle", "Remember Me", 50);

        // Spacer
        CreateSpacer(loginForm, 20);

        // Login Button
        CreateButton(loginForm, "LoginButton", "Login", new Color(0.2f, 0.6f, 1f), 70);

        // Show Signup Button
        CreateButton(loginForm, "ShowSignupButton", "Create Account", new Color(0.4f, 0.4f, 0.4f), 60);

        // Guest Login Button
        CreateButton(loginForm, "GuestLoginButton", "Play as Guest", new Color(0.6f, 0.6f, 0.6f), 60);

        EditorUtility.DisplayDialog("Success", "Login UI created successfully!\n\nNow connect these to LoginUIManager in Inspector.", "OK");

        Selection.activeGameObject = loginPanel;
    }

    void CreateSignupUI()
    {
        // Create SignupPanel
        Transform signupPanel = loginPanel.transform.Find("SignupPanel");
        if (signupPanel == null)
        {
            GameObject signupPanelGO = new GameObject("SignupPanel");
            signupPanelGO.transform.SetParent(loginPanel.transform, false);

            RectTransform rt = signupPanelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            signupPanel = signupPanelGO.transform;
            signupPanelGO.SetActive(false); // Start hidden
        }

        // Create SignupForm
        Transform signupForm = signupPanel.Find("SignupForm");
        if (signupForm == null)
        {
            GameObject signupFormGO = new GameObject("SignupForm");
            signupFormGO.transform.SetParent(signupPanel, false);

            RectTransform rt = signupFormGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 900);

            Image bg = signupFormGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            VerticalLayoutGroup vlg = signupFormGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 60, 60);
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            signupForm = signupFormGO.transform;
        }

        // Title
        CreateText(signupForm, "SignupTitle", "Create Account", 36, TextAlignmentOptions.Center, 80);

        // Email InputField
        CreateInputField(signupForm, "SignupEmailInput", "Email", TMP_InputField.ContentType.EmailAddress, 70);

        // Password InputField
        CreateInputField(signupForm, "SignupPasswordInput", "Password", TMP_InputField.ContentType.Password, 70);

        // Confirm Password InputField
        CreateInputField(signupForm, "ConfirmPasswordInput", "Confirm Password", TMP_InputField.ContentType.Password, 70);

        // Spacer
        CreateSpacer(signupForm, 20);

        // Signup Button
        CreateButton(signupForm, "SignupButton", "Sign Up", new Color(0.2f, 0.8f, 0.2f), 70);

        // Back to Login Button
        CreateButton(signupForm, "BackToLoginButton", "Back to Login", new Color(0.4f, 0.4f, 0.4f), 60);

        EditorUtility.DisplayDialog("Success", "Signup UI created successfully!\n\nNow connect these to LoginUIManager in Inspector.", "OK");

        Selection.activeGameObject = loginPanel;
    }

    GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        return go;
    }

    GameObject CreateInputField(Transform parent, string name, string placeholder, TMP_InputField.ContentType contentType, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f);

        TMP_InputField inputField = go.AddComponent<TMP_InputField>();
        inputField.contentType = contentType;

        // Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        RectTransform textAreaRT = textArea.AddComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.sizeDelta = Vector2.zero;
        textAreaRT.offsetMin = new Vector2(10, 0);
        textAreaRT.offsetMax = new Vector2(-10, 0);

        RectMask2D mask = textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRT = placeholderGO.AddComponent<RectTransform>();
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 24;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

        // Input Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI inputText = textGO.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 24;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.MidlineLeft;

        inputField.textViewport = textAreaRT;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        return go;
    }

    GameObject CreateButton(Transform parent, string name, string text, Color color, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        Image bg = go.AddComponent<Image>();
        bg.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = bg;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        return go;
    }

    GameObject CreateToggle(Transform parent, string name, string label, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        Toggle toggle = go.AddComponent<Toggle>();

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(go.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f);
        bgRT.anchorMax = new Vector2(0, 0.5f);
        bgRT.pivot = new Vector2(0, 0.5f);
        bgRT.sizeDelta = new Vector2(40, 40);
        bgRT.anchoredPosition = new Vector2(10, 0);

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f);

        // Checkmark
        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(bgGO.transform, false);
        RectTransform checkRT = checkGO.AddComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.sizeDelta = new Vector2(-10, -10);

        Image checkImage = checkGO.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.8f, 0.2f);

        toggle.graphic = checkImage;
        toggle.targetGraphic = bgImage;

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.sizeDelta = Vector2.zero;
        labelRT.offsetMin = new Vector2(60, 0);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 24;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = Color.white;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        return go;
    }

    void CreateSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }
}
#endif