# Unity WebGL + WebAR スタンプラリー 最小テンプレート実装手順

## 1. 目的

本資料では、Unity WebGL上からWebARを起動し、2D画像マーカーを認識した結果をUnityへ返す、演習用の最小テンプレートを実装する。

配布するサンプルシーンでは、以下の処理のみを実装する。

1. Unity WebGL画面に「WebARを起動」ボタンを1つ表示する。
2. ボタン押下でJavaScript側のWebARを起動する。
3. スマートフォンのカメラで2D画像マーカーを認識する。
4. 認識したマーカー名をUnityへ返す。
5. Unity側でマーカー名を `Debug.Log()` に出力する。
6. 初期設定では、認識後にWebARを停止してUnity画面へ戻る。
7. 発展機能として、認識したマーカー上に透過PNGキャラクターを追従表示できる機能をテンプレート内部に実装しておく。
8. AR上への透過PNG表示は初期状態ではOFFとし、設定値を変更した場合のみ有効にする。

スタンプ取得・Unity画面での画像表示・保存などの機能はテンプレートには実装せず、最初の演習課題として学生が追加する。

発展時にはWebAR側の設定をONにすることで、次の動作へ切り替えられるようにする。

```text
マーカー認識
    ↓
UnityへmarkerNameを通知
    ↓
マーカー位置に透過PNGキャラクターを表示
    ↓
マーカーの位置・傾きに追従
    ↓
「閉じる」ボタンでWebAR終了
    ↓
Unity画面へ戻る
```

---

## 2. 想定環境

- Unity 6 系
- Build Target: WebGL / Web
- スマートフォンブラウザ
  - Android: Chrome
  - iOS: Safari
- HTTPSで配信可能なWebサーバ
- WebAR: MindAR Image Tracking
- 認識対象: 2D画像のみ

> [!IMPORTANT]
> スマートフォンのカメラ取得にはブラウザの `getUserMedia()` を利用するため、原則としてHTTPS環境で実行する。

---

## 3. 全体構成

```text
スマートフォン
    │
    ▼
Unity WebGL
    │
    │ StartWebAR()
    ▼
WebARBridge.jslib
    │
    ▼
JavaScript / MindAR
    │
    ├─ カメラ起動
    ├─ 2D画像認識
    └─ markerName取得
            │
            │ SendMessage()
            ▼
Unity WebGL
    │
    ▼
WebARBridge.OnMarkerDetected(markerName)
    │
    ▼
Debug.Log(markerName)
```

役割を次の2層に分ける。

### 配布側が実装し、学生は原則編集しない層

- MindAR
- カメラ起動
- 画像認識
- `.mind` ファイル
- JavaScript
- `.jslib`
- WebGL Template
- UnityとJavaScriptの連携処理

### 学生が演習で編集する層

- 認識後のUI
- 画像表示
- スタンプ取得
- スタンプ帳
- 効果音
- Animator
- PlayerPrefs等による保存
- ゲームロジック

---

# 4. Unityプロジェクトを作成する

## 4.1 新規プロジェクト

Unity Hubから新しいUnityプロジェクトを作成する。

テンプレートは2Dまたは3Dのどちらでもよいが、今回のサンプルではUnity側で3D描画を行わないため、2Dで十分である。

例:

```text
Project Name:
WebARStampTemplate
```

---

## 4.2 フォルダ構成

`Assets` 以下に次の構成を作成する。

```text
Assets/
└─ WebARStamp/
   ├─ Runtime/
   │  ├─ WebARBridge.cs
   │  └─ WebARSample.cs
   │
   ├─ Plugins/
   │  └─ WebGL/
   │     └─ WebARBridge.jslib
   │
   ├─ WebGLTemplates/
   │  └─ WebARStamp/
   │     ├─ index.html
   │     ├─ webar.js
   │     ├─ style.css
   │     ├─ targets.mind
   │     ├─ Overlays/
   │     │  ├─ spot_01.png
   │     │  ├─ spot_02.png
   │     │  └─ spot_03.png
   │     └─ Libraries/
   │
   ├─ Samples/
   │  └─ WebARSample.unity
   │
   └─ Documentation/
      └─ README.md
```

> UnityPackageとして配布するときは、`WebARStamp` フォルダ以下をまとめてExportする。

---

# 5. Unity側のWebARブリッジを実装する

## 5.1 `WebARBridge.cs`

`Assets/WebARStamp/Runtime/WebARBridge.cs` を作成する。

```csharp
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class WebARBridge : MonoBehaviour
{
    [System.Serializable]
    public class MarkerDetectedEvent : UnityEvent<string> { }

    [SerializeField]
    private MarkerDetectedEvent onMarkerDetected = new MarkerDetectedEvent();

    public MarkerDetectedEvent OnMarkerDetectedEvent => onMarkerDetected;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartWebAR_Internal();
#endif

    /// <summary>
    /// JavaScript側のWebARを起動する。
    /// </summary>
    public void StartWebAR()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        StartWebAR_Internal();
#else
        Debug.Log("WebARはWebGLビルドで実行してください。");
#endif
    }

    /// <summary>
    /// JavaScript側からSendMessageで呼び出される。
    /// GameObject名とメソッド名はJavaScript側と一致させること。
    /// </summary>
    public void OnMarkerDetected(string markerName)
    {
        Debug.Log("認識したマーカー: " + markerName);
        onMarkerDetected.Invoke(markerName);
    }
}
```

このクラスの責務は次の2点だけとする。

- UnityからWebARを起動する。
- JavaScriptから返されたマーカー名をUnityイベントとして通知する。

学生がWebAR内部の処理を編集する必要はない。

---

# 6. サンプル用スクリプトを作成する

`Assets/WebARStamp/Runtime/WebARSample.cs` を作成する。

```csharp
using UnityEngine;

public class WebARSample : MonoBehaviour
{
    /// <summary>
    /// 演習で学生が最初に編集するメソッド。
    /// </summary>
    public void OnMarkerDetected(string markerName)
    {
        Debug.Log("読み取り結果: " + markerName);
    }
}
```

テンプレート配布時点では、マーカー名をログに表示するだけにする。

最初の課題では、このメソッドにスタンプ取得処理を追加させる。

---

# 7. UnityからJavaScriptを呼び出す `.jslib` を作成する

`Assets/WebARStamp/Plugins/WebGL/WebARBridge.jslib` を作成する。

```javascript
mergeInto(LibraryManager.library, {

    StartWebAR_Internal: function () {
        if (window.webARStart) {
            window.webARStart();
        } else {
            console.error("webARStart() が見つかりません。");
        }
    }

});
```

Unity側では、

```csharp
[DllImport("__Internal")]
private static extern void StartWebAR_Internal();
```

を通してこの関数を呼び出す。

処理の流れは次の通り。

```text
Unity Button
    ↓
WebARBridge.StartWebAR()
    ↓
StartWebAR_Internal()
    ↓
WebARBridge.jslib
    ↓
window.webARStart()
```

---

# 8. MindARを準備する

## 8.1 採用方式

今回はMindARの Image Tracking を使用する。

用途は2D画像認識を基本とする。さらに発展用として、認識したマーカー上に透過PNG画像を1枚重ねて表示する機能のみ実装しておく。3Dモデル、動画、複雑なAR演出は扱わない。

透過PNG表示機能は初期状態ではOFFとし、最小サンプルの挙動には影響させない。

MindARには、

- 画像ターゲット認識
- 複数ターゲット
- `targetFound`
- `targetLost`
- `start()`
- `stop()`

等の機能が用意されている。

今回使用する主な機能は、

```text
start()
targetFound
targetLost
stop()
mindar-image-target
```

である。

`targetFound` は認識結果のUnity通知に使用する。発展機能をONにした場合は、`mindar-image-target` 配下の `<a-image>` によって透過PNGをマーカーへ追従表示する。

---

## 8.2 MindARのバージョンを固定する

授業用テンプレートでは、`latest` や無指定のCDN参照は避ける。

例としてMindAR 1.2.5を使う場合は、バージョンを明示する。

```html
<script src="https://aframe.io/releases/1.5.0/aframe.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/mind-ar@1.2.5/dist/mindar-image-aframe.prod.js"></script>
```

授業環境をより安定させたい場合は、必要なJavaScriptファイルを `Libraries` フォルダへ保存し、テンプレート内からローカル参照する。

例:

```text
WebGLTemplates/WebARStamp/Libraries/
├─ aframe.min.js
└─ mindar-image-aframe.prod.js
```

この場合、`index.html` は次のようにする。

```html
<script src="Libraries/aframe.min.js"></script>
<script src="Libraries/mindar-image-aframe.prod.js"></script>
```

授業配布物としてはローカル同梱を推奨する。

---

# 9. 認識用画像を準備する

## 9.1 サンプルマーカー

最低2～3枚のサンプル画像を用意する。

例:

```text
marker01.png
marker02.png
marker03.png
```

対応するUnity側の名称は次のようにする。

```text
marker01.png → spot_01
marker02.png → spot_02
marker03.png → spot_03
```

学生には `targetIndex` ではなく、`spot_01` 等の意味のある文字列だけを渡す。

---

## 9.2 マーカー画像選定時の注意

画像認識では、次のような画像を使用する。

- 特徴点が多い
- 全体に模様が分散している
- コントラストがある
- 単色部分が多すぎない
- 同じ模様の繰り返しが少ない

避ける例:

- 真っ白な背景に文字だけ
- 単純な丸や四角だけ
- 左右対称すぎる画像
- 同じパターンが繰り返される画像

---

# 10. `.mind` ファイルを作成する

MindAR Image Targets Compilerを使用して、認識対象画像を事前コンパイルする。

複数画像をまとめて登録できる。

登録順が `targetIndex` になる。

例:

```text
1番目 marker01.png → targetIndex 0
2番目 marker02.png → targetIndex 1
3番目 marker03.png → targetIndex 2
```

コンパイル後、次のファイルを取得する。

```text
targets.mind
```

取得したファイルを次へ配置する。

```text
Assets/WebARStamp/WebGLTemplates/WebARStamp/targets.mind
```

---

# 10.5 発展表示用の透過PNGを準備する

発展用として、各マーカーに対応する透過PNGキャラクターを用意する。

例:

```text
Overlays/
├─ spot_01.png
├─ spot_02.png
└─ spot_03.png
```

配置先:

```text
Assets/WebARStamp/WebGLTemplates/WebARStamp/Overlays/
```

画像はPNGのアルファチャンネルを使用し、背景を透明にする。

AR表示では画像をマーカー面とほぼ同一平面に置くため、Z-fightingを避ける目的で `z = 0.01` 程度だけ手前に配置する。

> [!NOTE]
> `<a-image>` の `width` と `height` はAR空間上の表示サイズである。キャラクター画像の縦横比を維持するよう、元画像のアスペクト比に合わせて設定する。MindARの画像ターゲット座標ではターゲット幅がおおむね `1` の基準となるため、サンプルではキャラクター幅を `0.6～0.9` 程度から調整すると扱いやすい。

この画像は最初の演習では表示されない。WebAR側の設定値を変更した場合のみ使用する。

---

# 11. WebGL Templateを作成する

## 11.1 テンプレートフォルダ

Unity標準WebGL Templateをコピーして、次へ配置する。

```text
Assets/WebARStamp/WebGLTemplates/WebARStamp/
```

テンプレート名はUnityのPlayer Settingsから選択できる名前になる。

---

# 12. `index.html` を編集する

Unity標準テンプレートのUnity起動処理を残したまま、MindAR用の要素を追加する。

重要なのは、UnityのCanvasとWebAR表示領域を分離することである。

概念構造:

```html
<body>

    <div id="unity-container">
        <canvas id="unity-canvas"></canvas>
    </div>

    <div id="ar-container">
        <a-scene id="ar-scene">
        </a-scene>
    </div>

</body>
```

---

## 12.1 ライブラリの読み込み

`<head>` 内でMindARを読み込む。

```html
<script src="Libraries/aframe.min.js"></script>
<script src="Libraries/mindar-image-aframe.prod.js"></script>
```

その後、独自処理を読み込む。

```html
<script src="webar.js"></script>
```

---

## 12.2 AR Sceneを追加する

`body` 内に次のAR Sceneを追加する。

```html
<div id="ar-container">

    <button id="ar-close-button" type="button">×</button>

    <a-scene
        id="ar-scene"
        mindar-image="
            imageTargetSrc: targets.mind;
            autoStart: false;
            uiLoading: no;
            uiError: no;
            uiScanning: no;
            maxTrack: 1;
        "
        embedded
        color-space="sRGB"
        vr-mode-ui="enabled: false"
        device-orientation-permission-ui="enabled: false">

        <a-assets>
            <img id="overlay-image-0" src="Overlays/spot_01.png">
            <img id="overlay-image-1" src="Overlays/spot_02.png">
            <img id="overlay-image-2" src="Overlays/spot_03.png">
        </a-assets>

        <a-camera
            position="0 0 0"
            look-controls="enabled: false">
        </a-camera>

        <a-entity
            id="target-0"
            mindar-image-target="targetIndex: 0">
            <a-image
                id="overlay-0"
                src="#overlay-image-0"
                position="0 0 0.01"
                width="0.70"
                height="0.90"
                visible="false"
                material="transparent: true; alphaTest: 0.01">
            </a-image>
        </a-entity>

        <a-entity
            id="target-1"
            mindar-image-target="targetIndex: 1">
            <a-image
                id="overlay-1"
                src="#overlay-image-1"
                position="0 0 0.01"
                width="0.70"
                height="0.90"
                visible="false"
                material="transparent: true; alphaTest: 0.01">
            </a-image>
        </a-entity>

        <a-entity
            id="target-2"
            mindar-image-target="targetIndex: 2">
            <a-image
                id="overlay-2"
                src="#overlay-image-2"
                position="0 0 0.01"
                width="0.70"
                height="0.90"
                visible="false"
                material="transparent: true; alphaTest: 0.01">
            </a-image>
        </a-entity>

    </a-scene>

</div>
```

各ターゲットには発展用の `<a-image>` をあらかじめ配置しておく。

ただし `visible="false"` としているため、初期状態では何も表示されない。

通常の最小サンプルでは画像認識イベントを受け取る用途だけで動作する。発展設定をONにした場合のみ、JavaScriptから対応する `<a-image>` の `visible` を `true` にする。

透過PNGは `mindar-image-target` の子要素であるため、表示中はマーカーの位置・回転・遠近に追従する。

---

# 13. UnityインスタンスをJavaScriptから参照できるようにする

Unity標準WebGL Templateでは、通常 `createUnityInstance()` によりUnityが生成される。

そのUnityインスタンスをグローバルに保持する。

例:

```javascript
var unityGameInstance = null;

createUnityInstance(canvas, config, (progress) => {
    // Unity標準のprogress処理
}).then((unityInstance) => {

    unityGameInstance = unityInstance;
    window.unityGameInstance = unityInstance;

}).catch((message) => {
    alert(message);
});
```

JavaScriptからUnityへメッセージを送るときは、

```javascript
window.unityGameInstance.SendMessage(...)
```

を使用する。

---

# 14. `webar.js` を実装する

`Assets/WebARStamp/WebGLTemplates/WebARStamp/webar.js` を作成する。

AR上の透過PNG表示は、次の設定値で切り替える。

```javascript
const ENABLE_AR_OVERLAY = false;
```

配布時は必ず `false` とする。

- `false`: 最小サンプル。認識後すぐARを終了してUnityへ戻る。
- `true`: 発展モード。認識後もARを維持し、マーカー上に透過PNGを表示する。

実装例を以下に示す。

```javascript
let arSystem = null;
let arStarted = false;
let markerDetected = false;

// 初期配布時はfalse。発展時のみtrueへ変更する。
const ENABLE_AR_OVERLAY = false;

const markerNames = [
    "spot_01",
    "spot_02",
    "spot_03"
];

function initializeWebAR() {

    const scene = document.querySelector("#ar-scene");

    if (!scene) {
        console.error("AR Sceneが見つかりません。");
        return;
    }

    const setup = () => {

        arSystem = scene.systems["mindar-image-system"];

        if (!arSystem) {
            console.error("MindAR Systemが見つかりません。");
            return;
        }

        setupTargetEvents();
        setupCloseButton();
        resetAllOverlays();

        console.log("WebAR initialized.");
    };

    if (scene.hasLoaded) {
        setup();
    } else {
        scene.addEventListener("loaded", setup, { once: true });
    }
}

function setupTargetEvents() {

    for (let i = 0; i < markerNames.length; i++) {

        const target = document.getElementById("target-" + i);

        if (!target) {
            console.warn("target-" + i + " が見つかりません。");
            continue;
        }

        target.addEventListener("targetFound", () => {

            if (markerDetected) {
                return;
            }

            markerDetected = true;

            const markerName = markerNames[i];

            console.log("Marker detected: " + markerName);

            // Unityへの通知は、AR表示ON/OFFに関係なく必ず行う。
            sendMarkerToUnity(markerName);

            if (ENABLE_AR_OVERLAY) {
                showOverlay(i);
            } else {
                stopWebAR();
            }
        });
    }
}

function showOverlay(index) {

    resetAllOverlays();

    const overlay = document.getElementById("overlay-" + index);

    if (!overlay) {
        console.warn("overlay-" + index + " が見つかりません。");
        return;
    }

    overlay.setAttribute("visible", true);
}

function resetAllOverlays() {

    for (let i = 0; i < markerNames.length; i++) {

        const overlay = document.getElementById("overlay-" + i);

        if (overlay) {
            overlay.setAttribute("visible", false);
        }
    }
}

function setupCloseButton() {

    const closeButton = document.getElementById("ar-close-button");

    if (!closeButton) {
        return;
    }

    closeButton.addEventListener("click", () => {
        stopWebAR();
    });
}

window.webARStart = async function () {

    if (!arSystem) {
        console.error("WebARの初期化が完了していません。");
        return;
    }

    if (arStarted) {
        return;
    }

    markerDetected = false;
    resetAllOverlays();

    const arContainer = document.getElementById("ar-container");

    if (arContainer) {
        arContainer.classList.add("active");
    }

    try {
        await arSystem.start();
        arStarted = true;
    } catch (error) {
        console.error("WebARの起動に失敗しました。", error);

        if (arContainer) {
            arContainer.classList.remove("active");
        }
    }
};

window.webARStop = function () {
    stopWebAR();
};

function stopWebAR() {

    resetAllOverlays();

    if (!arSystem || !arStarted) {
        hideARContainer();
        return;
    }

    try {
        arSystem.stop();
    } catch (error) {
        console.error("WebARの停止時にエラーが発生しました。", error);
    }

    arStarted = false;

    hideARContainer();
}

function hideARContainer() {

    const arContainer = document.getElementById("ar-container");

    if (arContainer) {
        arContainer.classList.remove("active");
    }
}

function sendMarkerToUnity(markerName) {

    if (!window.unityGameInstance) {
        console.error("Unity Instanceがまだ利用できません。");
        return;
    }

    window.unityGameInstance.SendMessage(
        "WebARBridge",
        "OnMarkerDetected",
        markerName
    );
}

document.addEventListener("DOMContentLoaded", () => {
    initializeWebAR();
});
```

## 14.1 初期状態（AR表示OFF）の動作

```text
ENABLE_AR_OVERLAY = false

マーカー認識
    ↓
markerNameをUnityへ通知
    ↓
stopWebAR()
    ↓
Unity画面へ戻る
```

この挙動が配布時の標準である。最初のスタンプ作成演習はこの状態で実施する。

## 14.2 発展状態（AR表示ON）の動作

```text
ENABLE_AR_OVERLAY = true

マーカー認識
    ↓
markerNameをUnityへ通知
    ↓
対応するoverlay-Nをvisible=true
    ↓
透過PNGがマーカー上に表示される
    ↓
マーカーの位置・向きに追従
    ↓
ユーザーが「×」を押す
    ↓
stopWebAR()
    ↓
Unity画面へ戻る
```

AR表示ON時もUnityへの `markerName` 通知は認識直後に行う。したがってUnity側のスタンプ処理とAR上のキャラクター表示を同時に成立させられる。

---

# 15. Unity側GameObject名を固定する

JavaScriptでは、

```javascript
window.unityGameInstance.SendMessage(
    "WebARBridge",
    "OnMarkerDetected",
    markerName
);
```

としている。

そのため、Unity Scene内のGameObject名を必ず、

```text
WebARBridge
```

にする。

このGameObjectには `WebARBridge.cs` をアタッチする。

> [!WARNING]
> GameObject名を変更するとJavaScriptから呼び出せなくなる。
> 授業用テンプレートでは、このオブジェクトは編集禁止とすることを推奨する。

---

# 16. CSSでUnity画面とAR画面を切り替える

`Assets/WebARStamp/WebGLTemplates/WebARStamp/style.css` にAR用スタイルを追加する。

```css
#unity-container {
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100%;
}

#ar-container {
    display: none;
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100%;
    z-index: 1000;
    background: black;
}

#ar-container.active {
    display: block;
}

#ar-container a-scene {
    width: 100%;
    height: 100%;
}

#ar-close-button {
    position: fixed;
    top: max(12px, env(safe-area-inset-top));
    right: 12px;
    width: 48px;
    height: 48px;
    z-index: 1100;
    border: none;
    border-radius: 24px;
    font-size: 32px;
    line-height: 48px;
}
```

通常時:

```text
Unity Canvas
```

AR起動時:

```text
AR Container
  └─ カメラ映像
```

AR表示OFFで認識後:

```text
AR Container非表示
↓
Unity Canvasへ戻る
```

AR表示ONで認識後:

```text
AR Containerを維持
↓
透過PNGをマーカー上に表示
↓
「×」を押す
↓
AR Container非表示
↓
Unity Canvasへ戻る
```

最初の教材ではUnity UIをカメラ映像へ重ねず、完全切替方式とする。AR表示中の「×」ボタンだけはHTML側のUIとして用意する。

---

# 17. サンプルSceneを作成する

次のSceneを作成する。

```text
Assets/WebARStamp/Samples/WebARSample.unity
```

Hierarchyは最小限とする。

```text
Main Camera

Canvas
└─ StartARButton
   └─ Text

EventSystem

WebARBridge

WebARSample
```

---

## 17.1 `WebARBridge` GameObject

空のGameObjectを作成し、名前を必ず、

```text
WebARBridge
```

にする。

`WebARBridge.cs` をアタッチする。

---

## 17.2 `WebARSample` GameObject

空のGameObjectを作成して、

```text
WebARSample
```

とする。

`WebARSample.cs` をアタッチする。

---

## 17.3 UnityEventを接続する

`WebARBridge` のInspectorで、

```text
On Marker Detected Event
```

へ `WebARSample` GameObjectを登録する。

呼び出す関数を、

```text
WebARSample.OnMarkerDetected(string)
```

に設定する。

これにより、

```text
JavaScript
↓
WebARBridge
↓ UnityEvent<string>
WebARSample
```

となる。

学生は `WebARSample.cs` 以降だけを編集すればよい。

---

# 18. WebAR起動ボタンを設定する

CanvasにButtonを1つ作成する。

表示文字列:

```text
WebARを起動
```

Buttonの `OnClick()` に、

```text
WebARBridge
  → WebARBridge.StartWebAR()
```

を登録する。

この時点でUnity側のサンプル実装は完了である。

---

# 19. サンプルの想定動作

## 19.1 初期画面

```text
┌────────────────────┐
│                    │
│                    │
│   [WebARを起動]    │
│                    │
│                    │
└────────────────────┘
```

---

## 19.2 ボタンを押す

UnityからJavaScriptを呼び出す。

```text
StartWebAR()
↓
StartWebAR_Internal()
↓
window.webARStart()
↓
MindAR.start()
```

ブラウザが初回のみカメラ使用許可を求める。

---

## 19.3 AR画面

```text
┌────────────────────┐
│                    │
│     カメラ映像     │
│                    │
│     [マーカー]     │
│                    │
└────────────────────┘
```

---

## 19.4 マーカーを認識

例えば `targetIndex: 1` を認識した場合、

```text
targetIndex 1
↓
markerNames[1]
↓
spot_02
```

となる。

JavaScriptから、

```javascript
SendMessage(
    "WebARBridge",
    "OnMarkerDetected",
    "spot_02"
);
```

を呼ぶ。

---

## 19.5 Unityへ戻る

初期設定 `ENABLE_AR_OVERLAY = false` では、MindARを停止してAR Containerを非表示にする。

Unity側では、

```text
認識したマーカー: spot_02
読み取り結果: spot_02
```

がログへ表示される。

テンプレートのサンプル機能はここまでとする。

---

## 19.6 発展機能をONにした場合

`webar.js` の、

```javascript
const ENABLE_AR_OVERLAY = false;
```

を、

```javascript
const ENABLE_AR_OVERLAY = true;
```

へ変更する。

この場合、マーカー認識後もAR画面を維持し、対応する透過PNGがマーカー上へ表示される。

```text
spot_01 認識
    ↓
Unityへ "spot_01" を通知
    +
Overlays/spot_01.png を表示
    ↓
マーカーを動かすとPNGも追従
    ↓
画面右上の「×」
    ↓
Unityへ戻る
```

---

# 20. Build TargetをWebGLに変更する

UnityのBuild Profiles / Build Settingsから、Webを選択する。

Unityのバージョンによって表記が異なる場合があるが、WebGL出力を使用する。

Scene Listへ、

```text
WebARSample.unity
```

を追加する。

---

# 21. WebGL Templateを指定する

Player SettingsのWeb設定から、テンプレートを、

```text
WebARStamp
```

に変更する。

Unityが、

```text
Assets/WebARStamp/WebGLTemplates/WebARStamp/
```

を認識すると選択肢に表示される。

---

# 22. スマートフォン向け画面設定

WebGL TemplateのHTMLに、最低限次のViewport設定を入れる。

```html
<meta
    name="viewport"
    content="width=device-width, initial-scale=1.0, user-scalable=no">
```

必要に応じてUnity Canvasも画面いっぱいに表示する。

---

# 23. WebGLをBuildする

例:

```text
Build/
└─ WebARStampSample/
```

へBuildする。

ビルド後は、ローカルファイルを直接ダブルクリックして起動しない。

```text
file:///...
```

ではなく、Webサーバ経由で実行する。

---

# 24. HTTPSサーバへ配置する

実機スマートフォンのカメラを使用するため、HTTPSで配信する。

例:

```text
https://example.ac.jp/webar-stamp/
```

ブラウザでURLを開き、

```text
カメラの使用を許可
```

する。

---

# 25. 動作確認項目

## PCブラウザ

- [ ] Unity WebGLが起動する
- [ ] 「WebARを起動」ボタンが表示される
- [ ] ボタンからJavaScript関数が呼ばれる
- [ ] ConsoleにJavaScriptエラーがない

## Android Chrome

- [ ] HTTPSでページが開く
- [ ] カメラ使用許可が表示される
- [ ] 背面カメラが起動する
- [ ] marker01を認識できる
- [ ] marker02を認識できる
- [ ] marker03を認識できる
- [ ] AR表示OFFでは認識後にUnity画面へ戻る
- [ ] マーカー名がUnityへ渡される
- [ ] AR表示ONでは対応する透過PNGがマーカー上に表示される
- [ ] AR表示ONで透過PNGがマーカーの位置・傾きに追従する
- [ ] AR表示ONでは「×」ボタンでカメラを停止してUnityへ戻れる

## iPhone Safari

- [ ] HTTPSでページが開く
- [ ] カメラ使用許可が表示される
- [ ] カメラ映像が正常に表示される
- [ ] 各マーカーを認識できる
- [ ] `ENABLE_AR_OVERLAY` の初期値が `false` である
- [ ] AR表示OFFでは認識後にカメラが停止する
- [ ] AR表示ONでは透過PNGが正しく表示される
- [ ] AR表示ONでは透過PNGがマーカーの位置・向きに追従する
- [ ] AR表示ONでは「×」ボタンでカメラを停止できる
- [ ] Unity画面へ戻る

---

# 26. デバッグ方法

## Unity側

Unity Editor上ではWebAR自体は起動しない。

Editorでは、

```text
WebARはWebGLビルドで実行してください。
```

というログのみ出力する。

WebGL動作確認はブラウザで行う。

---

## JavaScript側

ブラウザConsoleへ次のログを出す。

```text
WebAR initialized.
Marker detected: spot_01
Marker detected: spot_02
Marker detected: spot_03
```

問題発生時はUnity側より先にJavaScript Consoleを確認する。

---

# 27. 想定される問題

## カメラが起動しない

確認事項:

- HTTPSか
- カメラ使用を拒否していないか
- 他アプリがカメラを占有していないか
- `arSystem.start()` がユーザー操作から呼ばれているか

今回、

```text
UnityのButtonタップ
↓
JavaScript
↓
MindAR.start()
```

という流れにしているのは、モバイルブラウザのカメラ権限制約とも相性がよい。

---

## `webARStart()` が見つからない

確認事項:

- `webar.js` が読み込まれているか
- `window.webARStart = ...` になっているか
- `.jslib` の関数名が一致しているか

---

## Unityへ結果が返らない

確認事項:

```javascript
window.unityGameInstance.SendMessage(
    "WebARBridge",
    "OnMarkerDetected",
    markerName
);
```

とUnity側の、

```text
GameObject名: WebARBridge
Method名: OnMarkerDetected
```

が一致しているか確認する。

---

## 一度の認識で複数回イベントが発生する

今回のテンプレートでは、

```javascript
if (markerDetected) {
    return;
}
```

によって二重通知を防止する。

AR表示OFFでは認識後すぐ `stopWebAR()` を実行する。

AR表示ONではWebARを維持するが、`markerDetected` が `true` のままとなるため、同じ起動中に別のマーカーを読み取ってもUnityへ二重通知しない。

したがって、どちらのモードでも1回のWebAR起動につき1個のマーカーだけ取得する仕様となる。

---

# 28. UnityPackage化する

動作確認が完了したら、Unity側で、

```text
Assets/WebARStamp
```

を右クリックする。

```text
Export Package...
```

を選択する。

依存ファイルを含めてExportする。

例:

```text
WebARStampTemplate.unitypackage
```

---

# 29. UnityPackageに含めるもの

最低限、次を含める。

```text
WebARStamp/
├─ Runtime/
│  ├─ WebARBridge.cs
│  └─ WebARSample.cs
│
├─ Plugins/
│  └─ WebGL/
│     └─ WebARBridge.jslib
│
├─ WebGLTemplates/
│  └─ WebARStamp/
│     ├─ index.html
│     ├─ webar.js
│     ├─ style.css
│     ├─ targets.mind
│     ├─ Overlays/
│     │  ├─ spot_01.png
│     │  ├─ spot_02.png
│     │  └─ spot_03.png
│     └─ Libraries/
│        ├─ aframe.min.js
│        └─ mindar-image-aframe.prod.js
│
├─ Samples/
│  └─ WebARSample.unity
│
└─ Documentation/
   └─ README.md
```

---

# 30. 学生向けの利用手順

学生にはWebAR内部の実装手順を教える必要はない。

配布資料では、次の程度にする。

```text
1. UnityPackageをImportする。

2. WebARSample Sceneを開く。

3. Web向けにBuildする。

4. スマートフォンでページを開く。

5. 「WebARを起動」を押す。

6. 配布された画像マーカーを読み取る。

7. 読み取ったマーカー名がUnityへ送られることを確認する。
```

学生が理解すべきインターフェースは、基本的に、

```csharp
public void OnMarkerDetected(string markerName)
{
}
```

だけとする。

---

# 31. 最初の演習課題

## 課題: ARスタンプを表示する

マーカーを読み取ったとき、マーカーに応じた画像をUnity画面へ表示する。

例:

```csharp
public void OnMarkerDetected(string markerName)
{
    if (markerName == "spot_01")
    {
        // スタンプ1を表示
    }
    else if (markerName == "spot_02")
    {
        // スタンプ2を表示
    }
    else if (markerName == "spot_03")
    {
        // スタンプ3を表示
    }
}
```

最初は次の要素だけでよい。

- `if`
- 文字列比較
- `Image`
- `Sprite`
- `SetActive()`

これにより、WebARそのものを理解していなくてもUnityの基本機能を使ったARコンテンツ制作へ入ることができる。

---

# 32. 発展課題の例

テンプレートを変更せず、Unity側だけで次の課題へ発展できる。

```text
課題1
マーカーごとに異なる画像を表示

        ↓

課題2
スタンプ帳画面を作成

        ↓

課題3
取得済みスタンプを記録

        ↓

課題4
PlayerPrefsでブラウザ再読み込み後も状態を保持

        ↓

課題5
一定数集めると特別画面を表示

        ↓

課題6
Audio / Animator / ParticleSystemで演出を追加

        ↓

課題7
AR認識をきっかけにミニゲームを開始

        ↓

発展課題
WebAR側のENABLE_AR_OVERLAYをONにし、
認識したマーカー上へ透過PNGキャラクターを表示
```

---

# 33. 教材として編集禁止にする部分

学生には、原則として次を変更させない。

```text
WebARBridge.cs
WebARBridge.jslib
WebGLTemplates/
targets.mind
MindAR関連JavaScript
Overlays/（通常授業では配布済み素材を使用）
```

学生が編集する部分は、

```text
WebARSample.cs
学生が作成した各種C#スクリプト
Unity Scene
Canvas / UI
Sprite
Audio
Animator
```

とする。

責務を分離することで、AR認識のトラブルとUnity演習上のバグを切り分けやすくなる。

---

# 34. テンプレート完成時のインターフェース

学生から見たWebARは、次の2つの操作だけである。

## ARを起動する

```csharp
webARBridge.StartWebAR();
```

通常はButtonのInspectorから呼び出すため、学生がコードを書く必要もない。

## 認識結果を受け取る

```csharp
public void OnMarkerDetected(string markerName)
{
    // 学生がここから実装する
}
```

この2点以外のWebAR技術はすべてテンプレート内部へ隠蔽する。

---

# 35. 完成条件

テンプレート完成時には、次の状態を満たすこと。

- [ ] Unity WebGLがスマートフォンで起動する
- [ ] 初期画面にWebAR起動ボタンだけが表示される
- [ ] ボタン押下でカメラが起動する
- [ ] 2D画像マーカーを認識できる
- [ ] 複数の登録済み画像を区別できる
- [ ] `targetIndex` が `spot_01` 等の文字列へ変換される
- [ ] マーカー名がUnityへ返る
- [ ] Unity Consoleへマーカー名が出力される
- [ ] 1回の起動で通知されるマーカーは1個だけである
- [ ] `ENABLE_AR_OVERLAY` の初期値が `false` である
- [ ] AR表示OFFでは認識後にカメラが停止する
- [ ] AR表示ONでは透過PNGが正しく表示される
- [ ] AR表示ONでは透過PNGがマーカーの位置・向きに追従する
- [ ] AR表示ONでは「×」ボタンでカメラを停止できる
- [ ] Unity画面へ戻る
- [ ] 学生はMindARやJavaScriptを編集しなくても演習できる
- [ ] `.unitypackage` のImportだけで必要ファイルを導入できる

---

# 36. AR透過PNG表示機能の設計方針

本テンプレートでは、透過PNG表示を最初から実装しておくが、配布時には無効化する。

```javascript
const ENABLE_AR_OVERLAY = false;
```

この設計により、最初の演習ではWebARの内部実装を意識させず、

```text
AR認識 → markerName取得 → Unity側でスタンプ処理
```

だけに集中できる。

発展時には設定値を `true` にするだけで、

```text
AR認識
    ↓
UnityへmarkerName通知
    +
マーカー上へ透過PNGキャラクター表示
```

へ拡張できる。

この段階でもキャラクター描画はWebAR側に閉じ込め、Unityとのインターフェースは `markerName` の通知から変更しない。これにより、基本課題と発展課題のUnityコードを共通化できる。

---

# 37. 参考資料

- Unity Manual: WebGL / WebからブラウザJavaScriptとの連携
  - https://docs.unity3d.com/ja/2023.2/Manual/webgl-interactingwithbrowserscripting.html

- MindAR Image Tracking
  - https://hiukim.github.io/mind-ar-js-doc/

- MindAR: Compile Target Images
  - https://hiukim.github.io/mind-ar-js-doc/quick-start/compile/

- MindAR: Multi Targets
  - https://hiukim.github.io/mind-ar-js-doc/examples/multi-targets/

- MindAR: start / stop / targetFound 使用例
  - https://github.com/hiukim/mind-ar-js/blob/master/examples/image-tracking/example3.html

---

# 38. 実装時の推奨方針まとめ

本テンプレートでは、WebARを「Unityに対する外部入力装置」として扱う。

```text
2D画像
  ↓
MindAR
  ↓
markerName
  ↓
Unity
```

Unityへ渡す情報は、画像位置・回転・カメラ映像などではなく、

```text
spot_01
spot_02
spot_03
```

というマーカー識別子だけに限定する。

この設計により、学生はWebARの内部実装を意識せず、

```csharp
public void OnMarkerDetected(string markerName)
```

を起点としてUnity側の表現・ゲームロジック・スタンプラリー機能の制作に集中できる。
