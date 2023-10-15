#!/bin/bash

[ -z "${CORECLR_PATH:-}" ] && CORECLR_PATH=./coreclr
[ -z "${BuildOS:-}"      ] && BuildOS=Linux
[ -z "${BuildArch:-}"    ] && BuildArch=x64
[ -z "${BuildType:-}"    ] && BuildType=Debug
[ -z "${Output:-}"       ] && Output=libvsharpCoverage.so

printf '  CORECLR_PATH : %s\n' "$CORECLR_PATH"
printf '  BuildOS      : %s\n' "$BuildOS"
printf '  BuildArch    : %s\n' "$BuildArch"
printf '  BuildType    : %s\n' "$BuildType"

printf '  Building %s ... ' "$Output"

CXX_FLAGS="$CXX_FLAGS --no-undefined -Wno-invalid-noreturn -fPIC -fms-extensions -D_UNIX -D_DEBUG -DHOST_64BIT -DBIT64 -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -std=c++11"

PROFILER_SRC_PATH="./profiler"
CORE_CLR_INCLUDES="-I $CORECLR_PATH/pal/inc/rt -I $CORECLR_PATH/pal/prebuilt/inc -I $CORECLR_PATH/pal/inc -I $CORECLR_PATH/inc"
CORE_CLR_SRC="$CORECLR_PATH/pal/prebuilt/idl/corprof_i.cpp"
PROJECT_SRC=(
  "$PROFILER_SRC_PATH/classFactory.cpp"
  "$PROFILER_SRC_PATH/corProfiler.cpp"
  "$PROFILER_SRC_PATH/dllmain.cpp"
  "$PROFILER_SRC_PATH/ILRewriter.cpp"
  "$PROFILER_SRC_PATH/instrumenter.cpp"
  "$PROFILER_SRC_PATH/logging.cpp"
  "$PROFILER_SRC_PATH/memory.cpp"
  "$PROFILER_SRC_PATH/probes.cpp"
)

clang++ -shared -o $Output $CXX_FLAGS $CORE_CLR_INCLUDES $CORE_CLR_SRC "${PROJECT_SRC[@]}"

printf 'Done.\n'