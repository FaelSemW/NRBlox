/*
 * ============================================================================
 * NRBlox (Linux Native Edition) — Open-Source Win64 Runtime & NTDLL Bridge
 * Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
 * Community Discord: https://discord.gg/M8j8JnQSCx
 * License: MIT (100% Open Source)
 * ============================================================================
 * Reconstructed open-source runtime bridge for Linux:
 *  1. Implements open-source WebView2Loader.dll exports so RobloxPlayerBeta.exe
 *     does not crash when Edge WebView2 Runtime is absent in a Linux prefix.
 *  2. Normalizes Windows 10 (Build 19045) environment & registry keys inside
 *     the isolated NRBlox Linux prefix (~/.local/share/nrblox-linux/prefix).
 *  3. Launches RobloxPlayerBeta.exe with Vulkan/DXVK and Esync/Fsync enabled.
 * ============================================================================
 */

#ifdef _WIN32
#include <windows.h>

extern "C" {

__declspec(dllexport) HRESULT __stdcall CreateCoreWebView2EnvironmentWithOptions(
    PCWSTR browserExecutableFolder,
    PCWSTR userDataFolder,
    void* environmentOptions,
    void* environmentCreatedHandler)
{
    (void)browserExecutableFolder;
    (void)userDataFolder;
    (void)environmentOptions;
    (void)environmentCreatedHandler;
    return S_OK;
}

__declspec(dllexport) HRESULT __stdcall CreateCoreWebView2Environment(
    void* environmentCreatedHandler)
{
    (void)environmentCreatedHandler;
    return S_OK;
}

__declspec(dllexport) HRESULT __stdcall GetAvailableCoreWebView2BrowserVersionString(
    PCWSTR browserExecutableFolder,
    LPWSTR* versionInfo)
{
    (void)browserExecutableFolder;
    if (versionInfo) {
        const wchar_t* ver = L"124.0.2478.80";
        size_t bytes = (wcslen(ver) + 1) * sizeof(wchar_t);
        *versionInfo = (LPWSTR)CoTaskMemAlloc(bytes);
        if (*versionInfo) {
            memcpy(*versionInfo, ver, bytes);
        }
    }
    return S_OK;
}

__declspec(dllexport) HRESULT __stdcall CompareBrowserVersions(
    PCWSTR version1,
    PCWSTR version2,
    int* result)
{
    (void)version1;
    (void)version2;
    if (result) *result = 0;
    return S_OK;
}

} /* extern "C" */

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    (void)lpReserved;
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
        /* Scrub Wine environment variables inside the child process */
        SetEnvironmentVariableA("WINEUSERNAME", NULL);
        SetEnvironmentVariableA("WINELOADERNOEXEC", NULL);
        SetEnvironmentVariableA("WINEPRELOADRESERVE", NULL);
    }
    return TRUE;
}
#endif
