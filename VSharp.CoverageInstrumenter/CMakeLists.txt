cmake_minimum_required(VERSION 3.12)

project(VSharp.CoverageInstrumenter LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_COMPILER "clang++")
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wno-invalid-noreturn -Wno-pragma-pack -fPIC -fms-extensions")

if (APPLE)
    set(CMAKE_SHARED_LIBRARY_CREATE_CXX_FLAGS "${CMAKE_SHARED_LIBRARY_CREATE_CXX_FLAGS} -undefined dynamic_lookup")
endif()

set(CORECLR_PATH coreclr)
set(PROFILER_PATH profiler)

add_definitions(-D_MIDL_USE_GUIDDEF_)
add_definitions(-DBIT64 -DHOST_64BIT)
add_definitions(-DPAL_STDCPP_COMPAT)
add_definitions(-DNOMINMAX)
add_definitions(-DUNIX)
add_definitions(-DSOS_INCLUDE)

message("Build type: ${CMAKE_BUILD_TYPE}")

if( (CMAKE_BUILD_TYPE MATCHES Debug) OR (CMAKE_BUILD_TYPE MATCHES DebugMemory))
    add_definitions(-D_LOGGING)
    add_definitions(-D_PROFILER_DEBUG) # Don't use -D_DEBUG https://github.com/dotnet/runtime/issues/11419
    message("Logging enabled")
endif()

if(CMAKE_BUILD_TYPE MATCHES DebugMemory)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -static-libsan -O1 -fsanitize=address -fno-omit-frame-pointer -fsanitize=address")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -static-libsan -fsanitize=address")
    message("Sanitizing enabled, consider you set LD_PRELOAD DYLD_INSERT_LIBRARIES")
endif()

if(UNIX)
    include_directories(.
            ${CORECLR_PATH}/pal/inc/rt
            ${CORECLR_PATH}/pal/src/include
            ${CORECLR_PATH}/pal/prebuilt/inc
            ${CORECLR_PATH}/pal/inc
            ${CORECLR_PATH}/inc
    )
    set(sources
        ${PROFILER_PATH}/classFactory.cpp
        ${PROFILER_PATH}/corProfiler.cpp
        ${PROFILER_PATH}/dllmain.cpp
        ${PROFILER_PATH}/instrumenter.cpp
        ${PROFILER_PATH}/ILRewriter.cpp
        ${PROFILER_PATH}/logging.cpp
        ${PROFILER_PATH}/memory.cpp
        ${PROFILER_PATH}/probes.cpp
        ${CORECLR_PATH}/pal/prebuilt/idl/corprof_i.cpp
        ./unix/os.cpp
    )
else()
    set(sources
        ${PROFILER_PATH}/classFactory.cpp
        ${PROFILER_PATH}/corProfiler.cpp
        ${PROFILER_PATH}/dllmain.cpp
        ${PROFILER_PATH}/instrumenter.cpp
        ${PROFILER_PATH}/ILRewriter.cpp
        ${PROFILER_PATH}/logging.cpp
        ${PROFILER_PATH}/memory.cpp
        ${PROFILER_PATH}/probes.cpp
        ${CORECLR_PATH}/pal/prebuilt/idl/corprof_i.cpp
        ./win/os.cpp
    )
endif()

add_library(vsharpCoverage SHARED ${sources})

# add_link_options(--unresolved-symbols=ignore-in-object-files)
