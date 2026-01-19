using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameSystemsTMP : MonoBehaviour
{
    public static GameSystemsTMP I { get; private set; }

    [Header("Scene Names")]
    public string menuScene = "MenuScene";
    public string level1Scene = "Level_01";
    public string winScene = "WinScene";

    [Header("Global UI Root Names (for auto-bind)")]
    public string globalCanvasName = "Canvas_Global";
    public string pausePanelName = "PausePanel";
    public string winPanelName = "WinPanel";
    public string deathPanelName = "DeathPanel";

    [Header("Common Child Names (for auto-bind)")]
    public string timeTextName = "TimeText";
    public string returnMenuButtonName = "Button_ReturnMenu";
    public string sensitivityButtonName = "Button_Sensitivity";
    public string soundButtonName = "Button_Sound";
    public string buttonLabelTMPName = "Text (TMP)"; // Unity 默认按钮文字名字

    [Header("Disable SRP Debug Updater (fix stutter)")]
    public bool disableDebugUpdater = true;
    public string debugUpdaterObjectName = "[Debug Updater]";

    [Header("Panels (Global Canvas)")]
    public GameObject pausePanel;
    public GameObject winPanel;
    public GameObject deathPanel;

    [Header("Timer TMP Text")]
    public TMP_Text winTimeText;
    public TMP_Text deathTimeText;
    public TMP_Text pauseTimeText;

    [Header("Bottom Buttons")]
    public Button winReturnMenuButton;
    public Button deathReturnMenuButton;
    public Button pauseReturnMenuButton;

    [Header("Settings TMP Text (Menu)")]
    public TMP_Text menuSensitivityText;
    public TMP_Text menuSoundText;

    [Header("Settings TMP Text (Pause)")]
    public TMP_Text pauseSensitivityText;
    public TMP_Text pauseSoundText;

    [Header("Auto Return (Win)")]
    public float winAutoReturnSeconds = 60f;

    [Header("Sensitivity (3 levels)")]
    public float[] sensitivityLevels = new float[] { 1.5f, 2.5f, 4.0f };

    // ---- runtime ----
    float startUnscaled;
    float endUnscaled;
    bool runStarted = false;
    bool runEnded = false;
    bool paused = false;

    // cached player scripts (for disable/enable)
    SimpleFPSMovement cachedMove;
    PlayerHands cachedHands;
    AimCrosshair cachedCrosshair;

    const string PREF_SENS = "sensIndex";
    const string PREF_SOUND = "soundOn";

    int SensIndex
    {
        get => PlayerPrefs.GetInt(PREF_SENS, 1);
        set { PlayerPrefs.SetInt(PREF_SENS, value); PlayerPrefs.Save(); }
    }

    bool SoundOn
    {
        get => PlayerPrefs.GetInt(PREF_SOUND, 1) == 1;
        set { PlayerPrefs.SetInt(PREF_SOUND, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        TryDisableDebugUpdater();

        AutoBindGlobalUI();
        BindAllButtonsOnce();

        // ✅ 新增：菜单按钮也自动绑定一次（不依赖 MenuUIBinder）
        TryBindMenuButtonsAndTexts();

        HideAllPanels();
        ApplySound();
        RefreshSettingTexts();

        LockCursor(false);
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            string s = SceneManager.GetActiveScene().name;
            if (s != menuScene && !runEnded)
            {
                TogglePause();
            }
        }
    }

    // ----------------- Public API (给按钮用) -----------------

    public void StartGame()
    {
        runStarted = true;
        runEnded = false;
        paused = false;

        startUnscaled = Time.unscaledTime;
        endUnscaled = startUnscaled;

        HideAllPanels();
        Time.timeScale = 1f;
        LockCursor(true);

        SceneManager.LoadScene(level1Scene);
    }

    // 菜单场景用：如果你还在用 MenuUIBinder，这个接口仍然保留
    public void BindMenuTexts(TMP_Text sens, TMP_Text sound)
    {
        menuSensitivityText = sens;
        menuSoundText = sound;
        RefreshSettingTexts();
    }

    public void ReturnMenu()
    {
        Time.timeScale = 1f;
        paused = false;
        runEnded = false;

        HideAllPanels();
        LockCursor(false);

        SceneManager.LoadScene(menuScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void CycleSensitivity()
    {
        int idx = SensIndex;
        idx = (idx + 1) % sensitivityLevels.Length;
        SensIndex = idx;

        ApplySensitivityToPlayer();
        RefreshSettingTexts();
    }

    public void ToggleSound()
    {
        SoundOn = !SoundOn;
        ApplySound();
        RefreshSettingTexts();
    }

    public void ShowWin()
    {
        if (runEnded) return;
        runEnded = true;
        paused = false;

        if (runStarted) endUnscaled = Time.unscaledTime;

        DisablePlayerInput();
        Time.timeScale = 0f;
        LockCursor(false);

        AutoBindGlobalUI();
        HideAllPanels();

        if (winPanel) winPanel.SetActive(true);
        if (winTimeText) winTimeText.text = FormatElapsed(GetElapsedSeconds());

        StopAllCoroutines();
        StartCoroutine(WinAutoReturn());
    }

    public void ShowDeath()
    {
        if (runEnded) return;
        runEnded = true;
        paused = false;

        if (runStarted) endUnscaled = Time.unscaledTime;

        DisablePlayerInput();
        Time.timeScale = 0f;
        LockCursor(false);

        AutoBindGlobalUI();
        HideAllPanels();

        if (deathPanel) deathPanel.SetActive(true);
        if (deathTimeText) deathTimeText.text = FormatElapsed(GetElapsedSeconds());
    }

    // ----------------- Pause -----------------

    void TogglePause()
    {
        paused = !paused;

        if (paused)
        {
            DisablePlayerInput();
            Time.timeScale = 0f;
            LockCursor(false);

            AutoBindGlobalUI();
            HideAllPanels();

            if (pausePanel) pausePanel.SetActive(true);
            if (pauseTimeText) pauseTimeText.text = FormatElapsed(GetElapsedSeconds());

            RefreshSettingTexts();
        }
        else
        {
            EnablePlayerInput();
            Time.timeScale = 1f;
            LockCursor(true);

            if (pausePanel) pausePanel.SetActive(false);
        }
    }

    // ----------------- Scene Loaded -----------------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryDisableDebugUpdater();

        AutoBindGlobalUI();
        BindAllButtonsOnce();

        // ✅ 新增：每次进菜单都尝试自动绑定菜单按钮 + 文本
        if (scene.name == menuScene)
        {
            TryBindMenuButtonsAndTexts();
        }

        ApplySound();
        ApplySensitivityToPlayer();
        CachePlayerScripts();

        if (scene.name == menuScene)
        {
            Time.timeScale = 1f;
            HideAllPanels();
            LockCursor(false);
            RefreshSettingTexts();
        }

        if (scene.name == winScene)
        {
            ShowWin();
        }
    }

    // ----------------- Auto Bind Global UI (NO SCAN) -----------------

    void AutoBindGlobalUI()
    {
        Transform canvas = transform.Find(globalCanvasName);

        if (canvas == null)
        {
            GameObject canvasGO = GameObject.Find(globalCanvasName);
            if (canvasGO != null)
            {
                canvas = canvasGO.transform;
                canvas.SetParent(transform, true);
            }
        }

        if (canvas == null) return;

        if (pausePanel == null) pausePanel = canvas.Find(pausePanelName)?.gameObject;
        if (winPanel == null) winPanel = canvas.Find(winPanelName)?.gameObject;
        if (deathPanel == null) deathPanel = canvas.Find(deathPanelName)?.gameObject;

        BindPanelInternals();
    }

    void BindPanelInternals()
    {
        if (pausePanel != null)
        {
            Transform p = pausePanel.transform;

            if (pauseTimeText == null)
                pauseTimeText = p.Find(timeTextName)?.GetComponent<TMP_Text>();

            if (pauseReturnMenuButton == null)
                pauseReturnMenuButton = p.Find(returnMenuButtonName)?.GetComponent<Button>();

            if (pauseSensitivityText == null)
            {
                Transform btn = p.Find(sensitivityButtonName);
                if (btn != null) pauseSensitivityText = btn.Find(buttonLabelTMPName)?.GetComponent<TMP_Text>();
            }

            if (pauseSoundText == null)
            {
                Transform btn = p.Find(soundButtonName);
                if (btn != null) pauseSoundText = btn.Find(buttonLabelTMPName)?.GetComponent<TMP_Text>();
            }
        }

        if (winPanel != null)
        {
            Transform w = winPanel.transform;

            if (winTimeText == null)
                winTimeText = w.Find(timeTextName)?.GetComponent<TMP_Text>();

            if (winReturnMenuButton == null)
                winReturnMenuButton = w.Find(returnMenuButtonName)?.GetComponent<Button>();
        }

        if (deathPanel != null)
        {
            Transform d = deathPanel.transform;

            if (deathTimeText == null)
                deathTimeText = d.Find(timeTextName)?.GetComponent<TMP_Text>();

            if (deathReturnMenuButton == null)
                deathReturnMenuButton = d.Find(returnMenuButtonName)?.GetComponent<Button>();
        }
    }

    // ----------------- ✅ 新增：自动绑定菜单按钮（解决你现在“点了没反应”） -----------------

    void TryBindMenuButtonsAndTexts()
    {
        // 找菜单里的两个按钮（按名字）
        Button menuSensBtn = FindSceneButtonByName(sensitivityButtonName);
        Button menuSoundBtn = FindSceneButtonByName(soundButtonName);

        if (menuSensBtn != null)
        {
            menuSensBtn.onClick.RemoveListener(CycleSensitivity);
            menuSensBtn.onClick.AddListener(CycleSensitivity);

            // 自动抓按钮文字
            TMP_Text label = menuSensBtn.transform.Find(buttonLabelTMPName)?.GetComponent<TMP_Text>();
            if (label != null) menuSensitivityText = label;
        }

        if (menuSoundBtn != null)
        {
            menuSoundBtn.onClick.RemoveListener(ToggleSound);
            menuSoundBtn.onClick.AddListener(ToggleSound);

            TMP_Text label = menuSoundBtn.transform.Find(buttonLabelTMPName)?.GetComponent<TMP_Text>();
            if (label != null) menuSoundText = label;
        }

        RefreshSettingTexts();
    }

    Button FindSceneButtonByName(string goName)
    {
        // 先排除 Global Canvas（避免找到 PausePanel 的按钮）
        Transform globalCanvas = transform.Find(globalCanvasName);

        GameObject go = GameObject.Find(goName);
        if (go == null) return null;

        if (globalCanvas != null && go.transform.IsChildOf(globalCanvas))
        {
            // 如果找到的是 Global Canvas 里的同名对象，就返回 null，让别的方式处理
            return null;
        }

        return go.GetComponent<Button>();
    }

    // ----------------- Button Binding -----------------

    void BindAllButtonsOnce()
    {
        if (winReturnMenuButton != null)
        {
            winReturnMenuButton.onClick.RemoveListener(ReturnMenu);
            winReturnMenuButton.onClick.AddListener(ReturnMenu);
        }

        if (deathReturnMenuButton != null)
        {
            deathReturnMenuButton.onClick.RemoveListener(ReturnMenu);
            deathReturnMenuButton.onClick.AddListener(ReturnMenu);
        }

        if (pauseReturnMenuButton != null)
        {
            pauseReturnMenuButton.onClick.RemoveListener(ReturnMenu);
            pauseReturnMenuButton.onClick.AddListener(ReturnMenu);
        }

        if (pausePanel != null)
        {
            Transform p = pausePanel.transform;

            Button sensBtn = p.Find(sensitivityButtonName)?.GetComponent<Button>();
            if (sensBtn != null)
            {
                sensBtn.onClick.RemoveListener(CycleSensitivity);
                sensBtn.onClick.AddListener(CycleSensitivity);
            }

            Button soundBtn = p.Find(soundButtonName)?.GetComponent<Button>();
            if (soundBtn != null)
            {
                soundBtn.onClick.RemoveListener(ToggleSound);
                soundBtn.onClick.AddListener(ToggleSound);
            }
        }
    }

    // ----------------- Player input enable/disable -----------------

    void CachePlayerScripts()
    {
        cachedMove = FindFirstObjectByType<SimpleFPSMovement>();
        cachedHands = FindFirstObjectByType<PlayerHands>();
        cachedCrosshair = FindFirstObjectByType<AimCrosshair>();
    }

    void DisablePlayerInput()
    {
        if (cachedMove != null) cachedMove.enabled = false;
        if (cachedHands != null) cachedHands.enabled = false;
        if (cachedCrosshair != null) cachedCrosshair.enabled = false;
    }

    void EnablePlayerInput()
    {
        if (runEnded) return;

        if (cachedMove != null) cachedMove.enabled = true;
        if (cachedHands != null) cachedHands.enabled = true;
        if (cachedCrosshair != null) cachedCrosshair.enabled = true;
    }

    // ----------------- Settings apply -----------------

    void ApplySensitivityToPlayer()
    {
        var p = FindFirstObjectByType<SimpleFPSMovement>();
        if (p == null) return;

        int idx = Mathf.Clamp(SensIndex, 0, sensitivityLevels.Length - 1);
        p.mouseSensitivity = sensitivityLevels[idx];
    }

    void ApplySound()
    {
        AudioListener.pause = !SoundOn;
        AudioListener.volume = SoundOn ? 1f : 0f;

        if (AudioManager.I != null)
        {
            AudioManager.I.SetSfxEnabled(SoundOn);
            AudioManager.I.SetMusicEnabled(SoundOn);
        }
    }

    void RefreshSettingTexts()
    {
        int idx = Mathf.Clamp(SensIndex, 0, sensitivityLevels.Length - 1);
        string sensStr = $"Sensitivity: {idx + 1}/3";
        string soundStr = SoundOn ? "Sound: ON" : "Sound: OFF";

        if (menuSensitivityText) menuSensitivityText.text = sensStr;
        if (pauseSensitivityText) pauseSensitivityText.text = sensStr;

        if (menuSoundText) menuSoundText.text = soundStr;
        if (pauseSoundText) pauseSoundText.text = soundStr;
    }

    // ----------------- Timer -----------------

    float GetElapsedSeconds()
    {
        if (!runStarted) return 0f;
        float end = runEnded ? endUnscaled : Time.unscaledTime;
        return Mathf.Max(0f, end - startUnscaled);
    }

    string FormatElapsed(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int hh = total / 3600;
        int mm = (total % 3600) / 60;
        int ss = total % 60;
        return hh > 0 ? $"{hh:00}:{mm:00}:{ss:00}" : $"{mm:00}:{ss:00}";
    }

    void HideAllPanels()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    IEnumerator WinAutoReturn()
    {
        float t = 0f;
        while (t < winAutoReturnSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        ReturnMenu();
    }

    // ----------------- Fix Stutter: Disable Debug Updater -----------------

    void TryDisableDebugUpdater()
    {
        if (!disableDebugUpdater) return;

        GameObject go = GameObject.Find(debugUpdaterObjectName);
        if (go != null && go.activeSelf)
        {
            go.SetActive(false);
        }
    }
}
