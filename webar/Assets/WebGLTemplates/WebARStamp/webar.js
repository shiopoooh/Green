(() => {
    "use strict";

    let arSystem = null;
    let arStarted = false;
    let arStarting = false;
    let stopRequested = false;
    let markerDetected = false;
    let initialized = false;
    let mindARBackendReady = false;
    let pendingARStartReject = null;

    // マーカー上へ対応する透過PNGを追従表示します。
    const ENABLE_AR_OVERLAY = true;
    const markerNames = ["spot_01", "spot_02", "spot_03"];

    function showWebARError(message, error) {
        if (error) console.error(message, error);
        else console.error(message);

        const warningBanner = document.getElementById("unity-warning");
        if (!warningBanner) return;

        warningBanner.querySelectorAll("[data-webar-error]").forEach(item => item.remove());
        const item = document.createElement("div");
        item.className = "error";
        item.dataset.webarError = "true";
        item.textContent = message;
        warningBanner.appendChild(item);
    }

    function clearWebARError() {
        document.querySelectorAll("[data-webar-error]").forEach(item => item.remove());
    }

    function isMindARError(error) {
        const details = String(error?.stack || error?.message || error || "");
        return details.includes("mindar-image") ||
            details.includes("texData") ||
            details.includes("MINDAR");
    }

    // Unityより先にMindAR/TensorFlow.jsのWebGLバックエンドを確保します。
    function prewarmMindARBackend() {
        let controller = null;

        try {
            const Controller = window.MINDAR?.IMAGE?.Controller;
            if (!Controller) throw new Error("MindAR Controller が読み込まれていません。");

            controller = new Controller({
                inputWidth: 16,
                inputHeight: 16,
                maxTrack: 1,
                debugMode: false
            });
            mindARBackendReady = true;
            console.log("MindAR WebGL backend prewarmed.");
            return true;
        } catch (error) {
            mindARBackendReady = false;
            showWebARError(
                "この端末ではWebAR用のWebGLを初期化できませんでした。ほかのタブを閉じて、ページを再読み込みしてください。",
                error
            );
            return false;
        } finally {
            try {
                controller?.dispose();
            } catch (error) {
                console.warn("MindARの事前初期化用Controllerを終了できませんでした。", error);
            }
        }
    }

    window.webARPrewarmPromise = Promise.resolve()
        .then(prewarmMindARBackend)
        .finally(() => {
            window.dispatchEvent(new CustomEvent("webar-prewarm-ready", {
                detail: { success: mindARBackendReady }
            }));
        });

    function initializeWebAR() {
        const scene = document.querySelector("#ar-scene");
        if (!scene) {
            showWebARError("AR Sceneが見つかりません。");
            return;
        }

        const setup = () => {
            if (initialized) return;

            arSystem = scene.systems["mindar-image-system"];
            if (!arSystem) {
                showWebARError("MindAR Systemが見つかりません。");
                return;
            }

            initialized = true;
            setupTargetEvents();
            setupCloseButton();
            resetAllOverlays();
            console.log("WebAR initialized.");
        };

        if (scene.hasLoaded) setup();
        else scene.addEventListener("loaded", setup, { once: true });
    }

    function setupTargetEvents() {
        markerNames.forEach((markerName, index) => {
            const target = document.getElementById("target-" + index);
            if (!target) {
                console.warn("target-" + index + " が見つかりません。");
                return;
            }

            target.addEventListener("targetFound", () => {
                if (markerDetected || !arStarted) return;

                markerDetected = true;
                console.log("Marker detected: " + markerName);
                sendMarkerToUnity(markerName);

                if (ENABLE_AR_OVERLAY) showOverlay(index);
                else stopWebAR();
            });
        });
    }

    function showOverlay(index) {
        resetAllOverlays();
        const overlay = document.getElementById("overlay-" + index);
        if (overlay) overlay.setAttribute("visible", true);
        else console.warn("overlay-" + index + " が見つかりません。");
    }

    function resetAllOverlays() {
        markerNames.forEach((_, index) => {
            const overlay = document.getElementById("overlay-" + index);
            if (overlay) overlay.setAttribute("visible", false);
        });
    }

    function setupCloseButton() {
        const closeButton = document.getElementById("ar-close-button");
        if (closeButton) closeButton.addEventListener("click", stopWebAR);
    }

    function startARAndWait(scene) {
        return new Promise((resolve, reject) => {
            const cleanup = () => {
                scene.removeEventListener("arReady", onReady);
                scene.removeEventListener("arError", onError);
                pendingARStartReject = null;
            };
            const onReady = () => {
                cleanup();
                resolve();
            };
            const onError = event => {
                cleanup();
                reject(event.detail?.error || new Error("カメラを開始できませんでした。"));
            };

            pendingARStartReject = error => {
                cleanup();
                reject(error);
            };
            scene.addEventListener("arReady", onReady, { once: true });
            scene.addEventListener("arError", onError, { once: true });

            try {
                arSystem.start();
            } catch (error) {
                pendingARStartReject(error);
            }
        });
    }

    function cleanupFailedARStart() {
        try {
            const video = arSystem?.video;
            video?.srcObject?.getTracks().forEach(track => track.stop());
            video?.remove();
            if (arSystem) arSystem.video = null;
        } catch (error) {
            console.warn("失敗したWebAR起動処理を終了できませんでした。", error);
        }
    }

    window.addEventListener("unhandledrejection", event => {
        if (!arStarting || !pendingARStartReject || !isMindARError(event.reason)) return;

        // Unity Loaderの汎用エラーダイアログへ伝播させず、WebAR側で案内します。
        event.preventDefault();
        event.stopImmediatePropagation();
        pendingARStartReject(event.reason);
    }, true);

    window.webARStart = async function () {
        if (!mindARBackendReady) {
            showWebARError(
                "WebARを開始できません。ほかのタブを閉じて、ページを再読み込みしてください。"
            );
            return;
        }
        if (!arSystem) {
            showWebARError("WebARの初期化が完了していません。ページを再読み込みしてください。");
            return;
        }
        if (arStarted || arStarting) return;

        const scene = document.querySelector("#ar-scene");
        arStarting = true;
        stopRequested = false;
        markerDetected = false;
        clearWebARError();
        resetAllOverlays();
        setARContainerVisible(true);

        try {
            await startARAndWait(scene);
            arStarted = true;
            if (stopRequested) stopWebAR();
        } catch (error) {
            cleanupFailedARStart();
            setARContainerVisible(false);
            showWebARError(
                "WebARを開始できませんでした。カメラの許可とブラウザのWebGL設定を確認してください。",
                error
            );
        } finally {
            arStarting = false;
        }
    };

    window.webARStop = stopWebAR;

    function stopWebAR() {
        stopRequested = true;
        resetAllOverlays();

        if (arSystem && arStarted) {
            try {
                arSystem.stop();
            } catch (error) {
                console.error("WebARの停止時にエラーが発生しました。", error);
            }
        }

        arStarted = false;
        setARContainerVisible(false);
    }

    function setARContainerVisible(visible) {
        const container = document.getElementById("ar-container");
        if (!container) return;
        container.classList.toggle("active", visible);
        container.setAttribute("aria-hidden", visible ? "false" : "true");
    }

    function sendMarkerToUnity(markerName) {
        if (!window.unityGameInstance) {
            console.error("Unity Instanceがまだ利用できません。");
            return;
        }

        window.unityGameInstance.SendMessage("WebARBridge", "OnMarkerDetected", markerName);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeWebAR, { once: true });
    } else {
        initializeWebAR();
    }
})();
