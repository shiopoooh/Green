using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Unity と WebGL テンプレート内の WebAR 実装を接続します。
/// JavaScript の SendMessage が参照するため、このコンポーネントを持つ
/// GameObject の名前は "WebARBridge" のままにしてください。
/// </summary>
public sealed class WebARBridge : MonoBehaviour
{
    [System.Serializable]
    public sealed class MarkerDetectedEvent : UnityEvent<string>
    {
    }

    [SerializeField]
    private MarkerDetectedEvent onMarkerDetected = new MarkerDetectedEvent();

    public MarkerDetectedEvent OnMarkerDetectedEvent => onMarkerDetected;




#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartWebAR_Internal();
#endif

    /// <summary>ブラウザ側の WebAR を起動します。</summary>
    public void StartWebAR()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        StartWebAR_Internal();
#else
        Debug.Log("WebARはWebGLビルドで実行してください。");
#endif
    }

    /// <summary>JavaScript の SendMessage から呼び出されます。</summary>
    public void OnMarkerDetected(string markerName)
    {
        Debug.Log("認識したマーカー: " + markerName);
        onMarkerDetected.Invoke(markerName);
    }
}
