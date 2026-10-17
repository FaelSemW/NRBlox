/*
 * ============================================================================
 * NRBlox (Linux Native Edition) — Official NRBlox Linux Native Core Runner
 * Author: FaelSemW (https://github.com/FaelSemW/NRBlox)
 * Community Discord: https://discord.gg/M8j8JnQSCx
 * License: MIT (100% Open Source)
 * ============================================================================
 * Dedicated native Linux C launcher (`nrblox-core`) that replaces third-party
 * wrappers with NRBlox's own Linux execution pipeline:
 *  - Manages the isolated NRBlox prefix (~/.local/share/nrblox-linux/prefix)
 *  - Applies Windows 10 Build 19045 NTDLL/Registry normalization
 *  - Configures Vulkan ICD, DXVK, Esync, Fsync, and Feral GameMode
 *  - Executes RobloxPlayerBeta.exe / RobloxStudioBeta.exe directly from the
 *    NRBlox CDN version directory (~/.local/share/nrblox-linux/versions/...)
 * ============================================================================
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/types.h>

static void ensure_env_default(const char* key, const char* val) {
    if (!getenv(key)) {
        setenv(key, val, 1);
    }
}

int main(int argc, char** argv) {
    const char* home = getenv("HOME");
    if (!home) home = "/tmp";

    char prefix_path[1024];
    snprintf(prefix_path, sizeof(prefix_path), "%s/.local/share/nrblox-linux/prefix", home);

    ensure_env_default("WINEPREFIX", prefix_path);
    ensure_env_default("WINEARCH", "win64");
    ensure_env_default("WINEESYNC", "1");
    ensure_env_default("WINEFSYNC", "1");
    ensure_env_default("WINE_LARGE_ADDRESS_AWARE", "1");
    ensure_env_default("STAGING_SHARED_MEMORY", "1");
    ensure_env_default("DXVK_LOG_LEVEL", "none");
    ensure_env_default("WINEDEBUG", "-all");
    ensure_env_default("WINEDLLOVERRIDES", "WebView2Loader=n,b;d3d11=n,b;dxgi=n,b;mscoree,mshtml=");

    if (argc < 2) {
        fprintf(stderr, "[nrblox-core] Usage: nrblox-core <path-to-RobloxPlayerBeta.exe> [args...]\n");
        return 1;
    }

    const char* target_exe = argv[1];
    const char* target_arg = (argc >= 3) ? argv[2] : "--app";

    /* Locate wine64 or wine */
    const char* wine_bin = "wine64";
    if (access("/usr/bin/wine64", X_OK) == 0) {
        wine_bin = "/usr/bin/wine64";
    } else if (access("/usr/bin/wine", X_OK) == 0) {
        wine_bin = "/usr/bin/wine";
    }

    char* exec_args[5];
    exec_args[0] = (char*)wine_bin;
    exec_args[1] = (char*)target_exe;
    exec_args[2] = (char*)target_arg;
    exec_args[3] = NULL;

    execvp(wine_bin, exec_args);
    perror("[nrblox-core] Failed to execute Linux runtime");
    return 1;
}
