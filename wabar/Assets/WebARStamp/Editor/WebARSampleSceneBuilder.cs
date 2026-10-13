#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public static class WebARSampleSceneBuilder
{
    private const string ScenePath = "Assets/WebARStamp/Samples/WebARSample.unity";

    [MenuItem("Tools/WebAR Stamp/Create Sample Scene")]
    public static void CreateSampleScene()
    {
        GenerateSampleImages();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        cameraObject.GetComponent<Camera>().backgroundColor = new Color32(21, 23, 26, 255);
        cameraObject.GetComponent<Camera>().orthographic = true;

        var bridgeObject = new GameObject("WebARBridge", typeof(WebARBridge));
        var bridge = bridgeObject.GetComponent<WebARBridge>();

        var sampleObject = new GameObject("WebARSample", typeof(WebARSample));
        var sample = sampleObject.GetComponent<WebARSample>();
        UnityEventTools.AddPersistentListener(bridge.OnMarkerDetectedEvent, sample.OnMarkerDetected);

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var buttonObject = new GameObject("StartARButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(520f, 140f);
        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color32(38, 134, 220, 255);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        UnityEventTools.AddPersistentListener(button.onClick, bridge.StartWebAR);

        var serializedBridge = new SerializedObject(bridge);
        serializedBridge.FindProperty("buttonImage").objectReferenceValue = buttonImage;
        serializedBridge.ApplyModifiedPropertiesWithoutUndo();

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObject.GetComponent<Text>();
        text.text = "WebARを起動";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EnsureFolder("Assets/WebARStamp/Samples");
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.WebGL.template = "PROJECT:WebARStamp";
        EditorUtility.SetDirty(bridge);
        AssetDatabase.SaveAssets();
        Debug.Log("WebAR sample scene and WebGL settings created.");
    }

    public static void CreateSampleSceneBatch()
    {
        CreateSampleScene();
        EditorApplication.Exit(0);
    }

    public static void BuildWebGLBatch()
    {
        const string defaultOutput = "Build/WebARStampSample";
        var output = GetCommandLineValue("-buildOutput") ?? defaultOutput;
        var report = BuildPipeline.BuildPlayer(
            new[] { ScenePath },
            output,
            BuildTarget.WebGL,
            BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"WebGL build failed: {report.summary.result}");

        Debug.Log($"WebGL build succeeded: {output} ({report.summary.totalSize} bytes)");
        EditorApplication.Exit(0);
    }

    private static string GetCommandLineValue(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void GenerateSampleImages()
    {
        EnsureFolder("Assets/WebARStamp/Markers");
        EnsureFolder("Assets/WebGLTemplates/WebARStamp/Overlays");

        for (var index = 0; index < 3; index++)
        {
            WriteMarker(index, $"Assets/WebARStamp/Markers/spot_0{index + 1}_marker.png");
            WriteOverlay(index, $"Assets/WebGLTemplates/WebARStamp/Overlays/spot_0{index + 1}.png");
        }

        AssetDatabase.Refresh();
    }

    private static void WriteMarker(int index, string path)
    {
        const int size = 512;
        const int cells = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var random = new System.Random(3181 + index * 997);
        var accent = new[]
        {
            new Color32(236, 72, 86, 255),
            new Color32(43, 157, 240, 255),
            new Color32(51, 188, 128, 255)
        }[index];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var vignette = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(256f, 256f)) / 430f);
                var value = (byte)(225 + 25 * vignette);
                texture.SetPixel(x, y, new Color32(value, value, value, 255));
            }
        }

        for (var gy = 0; gy < cells; gy++)
        {
            for (var gx = 0; gx < cells; gx++)
            {
                var margin = 38;
                var cellSize = (size - margin * 2) / cells;
                var inset = random.Next(2, 8);
                var dark = random.NextDouble() > 0.48;
                var color = dark ? new Color32(22, 27, 35, 255) : accent;
                if ((gx + gy + index) % 5 == 0) color = new Color32(248, 187, 45, 255);
                FillRect(texture, margin + gx * cellSize + inset, margin + gy * cellSize + inset,
                    cellSize - inset * 2, cellSize - inset * 2, color);
            }
        }

        DrawFrame(texture, 18, new Color32(16, 18, 24, 255));
        DrawFinder(texture, 44, 44, accent);
        DrawFinder(texture, size - 116, 44, accent);
        DrawFinder(texture, 44, size - 116, accent);
        DrawBars(texture, index, accent);

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void WriteOverlay(int index, string path)
    {
        const int width = 350;
        const int height = 450;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        var colors = new[]
        {
            new Color32(245, 84, 102, 255),
            new Color32(53, 164, 245, 255),
            new Color32(57, 196, 134, 255)
        };
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                texture.SetPixel(x, y, clear);

        DrawCircle(texture, 175, 245, 145, new Color32(255, 255, 255, 242));
        DrawCircle(texture, 175, 245, 126, colors[index]);
        DrawCircle(texture, 130, 270, 18, new Color32(25, 30, 38, 255));
        DrawCircle(texture, 220, 270, 18, new Color32(25, 30, 38, 255));
        DrawCircle(texture, 124, 276, 6, Color.white);
        DrawCircle(texture, 214, 276, 6, Color.white);
        FillRect(texture, 115, 192, 120, 15, new Color32(25, 30, 38, 255));
        FillRect(texture, 148, 70, 54, 64, colors[index]);
        FillRect(texture, 78, 35, 194, 42, new Color32(255, 255, 255, 242));

        // 種類が一目で分かる 1～3 本の白いストライプ。
        for (var i = 0; i <= index; i++)
            FillRect(texture, 126 + i * 36, 105, 18, 58, Color.white);

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void DrawFrame(Texture2D texture, int thickness, Color color)
    {
        FillRect(texture, 0, 0, texture.width, thickness, color);
        FillRect(texture, 0, texture.height - thickness, texture.width, thickness, color);
        FillRect(texture, 0, 0, thickness, texture.height, color);
        FillRect(texture, texture.width - thickness, 0, thickness, texture.height, color);
    }

    private static void DrawFinder(Texture2D texture, int x, int y, Color accent)
    {
        FillRect(texture, x, y, 72, 72, new Color32(15, 18, 24, 255));
        FillRect(texture, x + 10, y + 10, 52, 52, Color.white);
        FillRect(texture, x + 20, y + 20, 32, 32, accent);
    }

    private static void DrawBars(Texture2D texture, int index, Color accent)
    {
        for (var i = 0; i < index + 1; i++)
            FillRect(texture, 190 + i * 48, 226, 28, 92, accent);
    }

    private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        var radiusSquared = radius * radius;
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSquared)
                    SetPixelSafe(texture, cx + x, cy + y, color);
            }
        }
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (var py = y; py < y + height; py++)
            for (var px = x; px < x + width; px++)
                SetPixelSafe(texture, px, py, color);
    }

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            texture.SetPixel(x, y, color);
    }
}
#endif
