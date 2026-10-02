# WebAR Stamp Template

Unity WebGL から MindAR Image Tracking を起動し、認識したマーカー名を Unity に返す教材用テンプレートです。

## 使い方

1. `Assets/WebARStamp/Samples/WebARSample.unity` を開きます。
2. Build Profiles で Web を選び、WebGL ビルドを作成します。
3. HTTPS サーバーへ配置し、スマートフォンで開きます。
4. 「WebARを起動」を押して、`Markers` フォルダー内のサンプルマーカーを読み取ります。

認識結果は `WebARSample.OnMarkerDetected(string markerName)` へ `spot_01`～`spot_03` として通知されます。学生向け演習では、このメソッド以降にスタンプ処理を追加してください。

## 発展モード

`Assets/WebGLTemplates/WebARStamp/webar.js` の `ENABLE_AR_OVERLAY` を `true` にすると、認識後も WebAR を維持し、マーカーに対応する透過 PNG を追従表示します。配布時の初期値は `false` です。

## 注意

- カメラ利用には原則 HTTPS が必要です。
- `WebARBridge` GameObject の名前は JavaScript から参照されるため変更しないでください。
- マーカー画像を変更した場合は `targets.mind` も再コンパイルしてください。
