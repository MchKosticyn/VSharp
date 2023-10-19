#include "./profiler/os.h"
#include <windows.h>

std::string OS::unicodeToAnsi(const WCHAR *str) {
    return std::string{""};
}

void OS::sleepSeconds(int seconds) {
    Sleep(seconds * 1000);
}