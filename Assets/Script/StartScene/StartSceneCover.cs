using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StartSceneCover : MonoBehaviour
{
    private const string BackgroundResourcePath = "Art/StartMenu/StartBackground";
    private const float FadeDuration = 0.9f;
    private const float MenuDelay = 0.3f;
    private const float MenuItemDuration = 0.48f;
    private const float MenuStagger = 0.09f;
    private const float HoverSpeed = 8f;

    private readonly string[] menuButtonNames =
    {
        "StartButton",
        "GameInstruction",
        "Credit",
        "LeaderboardButton",
        "ExitButton"
    };

    private Camera mainCamera;
    private SpriteRenderer backgroundRenderer;
    private AudioSource buttonAudioSource;
    private ParticleSystem glowSpecks;
    private Material glowSpeckMaterial;
    private Texture2D glowSpeckTexture;
    private RectTransform[] menuButtons;
    private Vector3[] menuBaseScales;
    private CanvasGroup[] menuCanvasGroups;
    private bool[] menuHovered;
    private float[] menuHoverAmounts;
    private Image fadeImage;
    private CanvasGroup fadeCanvasGroup;
    private bool menuEntranceComplete;
    private Vector2 backgroundReferenceOffset;

    private void Awake()
    {
        mainCamera = Camera.main;
        buttonAudioSource = GetComponent<AudioSource>();
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null || mainCamera == null)
        {
            enabled = false;
            return;
        }

        ConfigureCanvas(canvas);
        ConfigureReferenceBackground(canvas.transform);
        CreateBackground();
        CreateGlowSpecks();
        FindMenuElements(canvas.transform);
        CreateFadePanel(canvas.transform);
    }

    private void OnEnable()
    {
        StartCoroutine(PlayEntrance());
    }

    private void Update()
    {
        UpdateBackground();
        UpdateMenuHover();
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigureReferenceBackground(Transform canvasTransform)
    {
        Transform oldBackground = canvasTransform.Find("Image");
        if (oldBackground != null)
        {
            RectTransform rect = oldBackground as RectTransform;
            if (rect != null)
            {
                backgroundReferenceOffset = rect.anchoredPosition;
            }

            Image image = oldBackground.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;
                image.raycastTarget = false;
            }
        }
    }

    private void CreateBackground()
    {
        Sprite sprite = Resources.Load<Sprite>(BackgroundResourcePath);
        if (sprite == null)
        {
            Debug.LogError("StartSceneCover: missing Resources/" + BackgroundResourcePath + ".png");
            return;
        }

        GameObject background = new GameObject("DynamicBackground");
        background.transform.SetParent(transform, false);
        backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = sprite;
        backgroundRenderer.sortingOrder = -100;
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null || mainCamera == null)
        {
            return;
        }

        float viewHeight = mainCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * mainCamera.aspect;
        Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
        float coverScale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
        backgroundRenderer.transform.localScale =
            Vector3.one * coverScale * 1.018f;

        float referenceOffsetX = backgroundReferenceOffset.x / 1920f * viewWidth;
        float referenceOffsetY = backgroundReferenceOffset.y / 1080f * viewHeight;
        Vector3 cameraPosition = mainCamera.transform.position;
        backgroundRenderer.transform.position =
            new Vector3(
                cameraPosition.x + referenceOffsetX,
                cameraPosition.y + referenceOffsetY,
                cameraPosition.z + 20f);
    }

    private void CreateGlowSpecks()
    {
        GameObject dustObject = new GameObject("CaveGlowSpecks");
        dustObject.transform.SetParent(transform, false);
        glowSpecks = dustObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = glowSpecks.main;
        main.loop = true;
        main.prewarm = true;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 36;
        float viewHeight = mainCamera.orthographicSize * 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.8f, 6.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.002f, 0.012f);
        main.startSize = new ParticleSystem.MinMaxCurve(
            viewHeight * 0.004f,
            viewHeight * 0.011f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.20f, 1f, 0.84f, 1f),
            new Color(1f, 0.62f, 0.20f, 1f));

        ParticleSystem.EmissionModule emission = glowSpecks.emission;
        emission.rateOverTime = 3.2f;

        ParticleSystem.ShapeModule shape = glowSpecks.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(viewHeight * mainCamera.aspect, viewHeight, 0.1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = glowSpecks.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.008f, 0.008f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.004f, 0.014f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.82f, 0.18f),
                new GradientAlphaKey(0.58f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            glowSpecks.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fadeGradient);

        AnimationCurve glowSize = new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.72f, 0.88f),
            new Keyframe(1f, 0.48f));
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
            glowSpecks.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, glowSize);

        ParticleSystem.CollisionModule collision = glowSpecks.collision;
        collision.enabled = false;
        ParticleSystem.TrailModule trails = glowSpecks.trails;
        trails.enabled = false;
        ParticleSystem.LightsModule lights = glowSpecks.lights;
        lights.enabled = false;

        ParticleSystemRenderer renderer = dustObject.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -50;
        renderer.sharedMaterial = CreateGlowSpeckMaterial();
        dustObject.transform.position =
            new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0f);

        glowSpecks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        glowSpecks.Play(true);
        glowSpecks.Emit(12);
    }

    private Material CreateGlowSpeckMaterial()
    {
        Shader glowShader = Shader.Find("Sprites/Default");
        if (glowShader == null)
        {
            Debug.LogWarning("StartSceneCover: Sprites/Default shader is unavailable.");
            return null;
        }

        const int textureSize = 32;
        glowSpeckTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false,
            true)
        {
            name = "StartSceneGlowSpeckTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float normalizedX = ((x + 0.5f) / textureSize) * 2f - 1f;
                float normalizedY = ((y + 0.5f) / textureSize) * 2f - 1f;
                float distance = Mathf.Sqrt(
                    normalizedX * normalizedX + normalizedY * normalizedY);
                float glow = Mathf.Clamp01(1f - distance);
                float alpha = glow * glow;
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        glowSpeckTexture.SetPixels(pixels);
        glowSpeckTexture.Apply(false, true);

        glowSpeckMaterial = new Material(glowShader)
        {
            name = "StartSceneGlowSpeckMaterial",
            mainTexture = glowSpeckTexture,
            hideFlags = HideFlags.DontSave
        };
        return glowSpeckMaterial;
    }

    private void OnDestroy()
    {
        if (glowSpeckMaterial != null)
        {
            Destroy(glowSpeckMaterial);
        }

        if (glowSpeckTexture != null)
        {
            Destroy(glowSpeckTexture);
        }
    }

    private void FindMenuElements(Transform canvasTransform)
    {
        menuButtons = new RectTransform[menuButtonNames.Length];
        menuBaseScales = new Vector3[menuButtonNames.Length];
        menuCanvasGroups = new CanvasGroup[menuButtonNames.Length];
        menuHovered = new bool[menuButtonNames.Length];
        menuHoverAmounts = new float[menuButtonNames.Length];

        for (int i = 0; i < menuButtonNames.Length; i++)
        {
            Transform buttonTransform = canvasTransform.Find(menuButtonNames[i]);
            RectTransform button = buttonTransform as RectTransform;
            menuButtons[i] = button;
            if (button == null)
            {
                continue;
            }

            menuBaseScales[i] = button.localScale;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = button.gameObject.AddComponent<CanvasGroup>();
            }

            menuCanvasGroups[i] = group;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            button.localScale = menuBaseScales[i] * 0.96f;
            AddHoverTriggers(button, i);
        }
    }

    private void AddHoverTriggers(RectTransform button, int index)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new List<EventTrigger.Entry>();
        }

        EventTrigger.Entry enter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener(_ =>
        {
            menuHovered[index] = true;
            PlayButtonAudio();
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener(_ => menuHovered[index] = false);
        trigger.triggers.Add(exit);
    }

    private void PlayButtonAudio()
    {
        if (buttonAudioSource != null && buttonAudioSource.clip != null)
        {
            buttonAudioSource.PlayOneShot(buttonAudioSource.clip);
        }
    }

    private void CreateFadePanel(Transform canvasTransform)
    {
        GameObject fadePanel = new GameObject(
            "FadePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        fadePanel.transform.SetParent(canvasTransform, false);
        fadePanel.transform.SetAsLastSibling();

        RectTransform rect = (RectTransform)fadePanel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = fadePanel.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        fadeCanvasGroup = fadePanel.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator PlayEntrance()
    {
        float elapsed = 0f;
        float menuEndTime =
            MenuDelay + MenuStagger * (menuButtons.Length - 1) + MenuItemDuration;
        float totalDuration = Mathf.Max(FadeDuration, menuEndTime);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float fadeT = Mathf.Clamp01(elapsed / FadeDuration);
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f - SmoothStep(fadeT);
            }

            UpdateMenuEntrance(elapsed);
            yield return null;
        }

        CompleteMenuEntrance();
        menuEntranceComplete = true;
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void UpdateMenuEntrance(float elapsed)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null || menuCanvasGroups[i] == null)
            {
                continue;
            }

            float itemT = Mathf.Clamp01(
                (elapsed - MenuDelay - MenuStagger * i) / MenuItemDuration);
            float positionT = EaseOutBack(itemT);
            float alphaT = SmoothStep(itemT);

            menuButtons[i].localScale =
                Vector3.LerpUnclamped(menuBaseScales[i] * 0.96f, menuBaseScales[i], positionT);
            menuCanvasGroups[i].alpha = alphaT;
            menuCanvasGroups[i].interactable = itemT >= 1f;
            menuCanvasGroups[i].blocksRaycasts = itemT >= 1f;
        }
    }

    private void CompleteMenuEntrance()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null || menuCanvasGroups[i] == null)
            {
                continue;
            }

            menuButtons[i].localScale = menuBaseScales[i];
            menuCanvasGroups[i].alpha = 1f;
            menuCanvasGroups[i].interactable = true;
            menuCanvasGroups[i].blocksRaycasts = true;
        }
    }

    private void UpdateMenuHover()
    {
        if (!menuEntranceComplete || menuButtons == null)
        {
            return;
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            float target = menuHovered[i] ? 1f : 0f;
            menuHoverAmounts[i] = Mathf.MoveTowards(
                menuHoverAmounts[i],
                target,
                HoverSpeed * Time.unscaledDeltaTime);

            menuButtons[i].localScale =
                menuBaseScales[i] * (1f + 0.018f * menuHoverAmounts[i]);
        }
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.15f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted
            + overshoot * shifted * shifted;
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
