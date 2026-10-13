mergeInto(LibraryManager.library, {
    StartWebAR_Internal: function () {
        if (typeof window.webARStart === "function") {
            window.webARStart();
            return;
        }

        console.error("webARStart() が見つかりません。");
    }
});
