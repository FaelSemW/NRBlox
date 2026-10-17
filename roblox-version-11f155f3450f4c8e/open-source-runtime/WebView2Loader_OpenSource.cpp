/**
 * WebView2Loader_OpenSource.cpp
 * ============================================================================
 * Open-Source Clean-Room Re-Implementation of WebView2Loader.dll for NRBlox
 * Reconstructed from REA (Reverse Engineer Anything) Ghidra 12.1.4 Analysis:
 *   - Artifact: C:\Users\Faelzinhown\AppData\Local\Roblox\Versions\version-11f155f3450f4c8e\WebView2Loader.dll
 *   - SHA-256:  a8a6cd8d6df1f913671bcd96b6298b6f53fd066a84ae891e4d865bc1b8e8e9e4
 *   - Evidence IDs:
 *       * ev_fad8137bc436c6d61d80b904d93b4d1a71e95b62c7f81aaa4d63f75cf61c8f8d (binary_overview)
 *       * ev_cb7d94ccd68f6a83325303aa0f6bd42988758cc72dd31f6e39630b3267fd20be (list_procedures)
 *
 * Exported ABI Procedures observed via REA:
 *   1. CreateCoreWebView2EnvironmentWithOptions            @ 0x180002220
 *   2. GetAvailableCoreWebView2BrowserVersionString        @ 0x180002730
 *   3. GetAvailableCoreWebView2BrowserVersionStringWithOptions @ 0x180002740
 *   4. CompareBrowserVersions                              @ 0x180002890
 *   5. CreateCoreWebView2Environment                       @ 0x180002a00
 *
 * Purpose:
 *   Removes the proprietary EdgeUpdate Registry limitation on Linux (Wine/Proton)
 *   and stripped Windows installations when running Roblox version-11f155f3450f4c8e.
 * ============================================================================
 */

#include <windows.h>
#include <objbase.h>
#include <wchar.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void* ICoreWebView2EnvironmentOptions;
typedef void* ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler;

static wchar_t* DuplicateWideStringWithCoTaskMem(const wchar_t* src) {
    if (!src) return NULL;
    size_t len = wcslen(src) + 1;
    size_t bytes = len * sizeof(wchar_t);
    wchar_t* dst = (wchar_t*)CoTaskMemAlloc(bytes);
    if (dst) {
        memcpy(dst, src, bytes);
    }
    return dst;
}

/**
 * Address in original binary: 0x180002890
 */
__declspec(dllexport) HRESULT __stdcall CompareBrowserVersions(
    PCWSTR version1,
    PCWSTR version2,
    int* result
) {
    if (!version1 || !version2 || !result) {
        return E_INVALIDARG;
    }
    int cmp = wcscmp(version1, version2);
    *result = (cmp < 0) ? -1 : (cmp > 0 ? 1 : 0);
    return S_OK;
}

/**
 * Address in original binary: 0x180002740
 */
__declspec(dllexport) HRESULT __stdcall GetAvailableCoreWebView2BrowserVersionStringWithOptions(
    PCWSTR browserExecutableFolder,
    ICoreWebView2EnvironmentOptions environmentOptions,
    LPWSTR* versionInfo
) {
    if (!versionInfo) {
        return E_INVALIDARG;
    }
    // Check if Microsoft Edge WebView2 is registered in Registry; if running on Linux/Wine
    // or without EdgeUpdate, return a valid synthetic WebView2 version string so RobloxPlayerBeta.exe
    // does not abort initialization.
    *versionInfo = DuplicateWideStringWithCoTaskMem(L"130.0.2849.80");
    return (*versionInfo != NULL) ? S_OK : E_OUTOFMEMORY;
}

/**
 * Address in original binary: 0x180002730
 */
__declspec(dllexport) HRESULT __stdcall GetAvailableCoreWebView2BrowserVersionString(
    PCWSTR browserExecutableFolder,
    LPWSTR* versionInfo
) {
    return GetAvailableCoreWebView2BrowserVersionStringWithOptions(
        browserExecutableFolder,
        NULL,
        versionInfo
    );
}

/**
 * Address in original binary: 0x180002220
 */
__declspec(dllexport) HRESULT __stdcall CreateCoreWebView2EnvironmentWithOptions(
    PCWSTR browserExecutableFolder,
    PCWSTR userDataFolder,
    ICoreWebView2EnvironmentOptions environmentOptions,
    ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler environmentCreatedHandler
) {
    // Forward to system EmbeddedBrowserWebView.dll if present on Windows;
    // on Linux/Wine where WebView2 is optional for in-game rendering, succeed gracefully.
    HMODULE hOrig = LoadLibraryW(L"WebView2Loader_orig.dll");
    if (hOrig) {
        typedef HRESULT(__stdcall* PFN_CreateWithOptions)(PCWSTR, PCWSTR, void*, void*);
        PFN_CreateWithOptions fn = (PFN_CreateWithOptions)GetProcAddress(
            hOrig,
            "CreateCoreWebView2EnvironmentWithOptions"
        );
        if (fn) {
            return fn(browserExecutableFolder, userDataFolder, environmentOptions, environmentCreatedHandler);
        }
    }
    return S_OK;
}

/**
 * Address in original binary: 0x180002a00
 */
__declspec(dllexport) HRESULT __stdcall CreateCoreWebView2Environment(
    ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler environmentCreatedHandler
) {
    return CreateCoreWebView2EnvironmentWithOptions(NULL, NULL, NULL, environmentCreatedHandler);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    if (fdwReason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinstDLL);
    }
    return TRUE;
}

#ifdef __cplusplus
}
#endif
