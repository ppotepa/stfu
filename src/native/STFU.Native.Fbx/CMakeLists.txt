cmake_minimum_required(VERSION 3.20)

project(STFU.Native.Fbx C)

add_library(stfu_fbx SHARED
    stfu_fbx.c
    ../../../third_party/ufbx/ufbx.c)

target_include_directories(stfu_fbx PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/../../../third_party/ufbx)

target_compile_features(stfu_fbx PRIVATE c_std_99)

if (MSVC)
    target_compile_options(stfu_fbx PRIVATE /W3)
else()
    target_compile_options(stfu_fbx PRIVATE -Wall -Wextra -Wno-unused-parameter)
endif()
