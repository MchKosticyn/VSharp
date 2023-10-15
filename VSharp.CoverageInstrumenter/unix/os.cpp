#include "./profiler/os.h"

std::string OS::unicodeToAnsi(const WCHAR *str) {
    std::basic_string<WCHAR> ws(str);
    return std::string{ws.begin(), ws.end()};
}