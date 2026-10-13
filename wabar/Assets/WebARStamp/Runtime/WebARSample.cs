using UnityEngine;
using UnityEngine.UI;

/// <summary>学生が認識後の処理を追加する最小サンプルです。</summary>
public sealed class WebARSample : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Text testText;

    public void OnMarkerDetected(string markerName)
    {
        Debug.Log("読み取り結果: " + markerName);
        if (buttonImage == null)
        {
            Debug.LogWarning("WebARBridge の Button Image が設定されていません。");
            return;
        }

        testText.text = markerName;
        if (markerName == "spot_01")
        {
            // スタンプ1を取得したときの処理
            buttonImage.color = Color.black;
        }
        else if (markerName == "spot_02")
        {
            // スタンプ2を取得したときの処理
            buttonImage.color = Color.red;
        }
        else if (markerName == "spot_03")
        {
            // スタンプ3を取得したときの処理
            buttonImage.color = Color.blue;
        }
    }
}
