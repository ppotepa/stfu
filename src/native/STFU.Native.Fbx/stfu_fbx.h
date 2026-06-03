#pragma once

#ifdef _WIN32
#define STFU_FBX_API __declspec(dllexport)
#else
#define STFU_FBX_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct stfu_fbx_scene stfu_fbx_scene;

typedef struct stfu_fbx_error {
    int code;
    const char *message;
} stfu_fbx_error;

typedef struct stfu_fbx_scene_info {
    int mesh_count;
    int skinned_mesh_count;
    int skeleton_count;
    int animation_count;
} stfu_fbx_scene_info;

typedef struct stfu_fbx_vertex {
    float x;
    float y;
    float z;
    float normal_x;
    float normal_y;
    float normal_z;
} stfu_fbx_vertex;

typedef struct stfu_fbx_triangle {
    int a;
    int b;
    int c;
} stfu_fbx_triangle;

typedef struct stfu_fbx_mesh_buffer {
    int vertex_count;
    int triangle_count;
    stfu_fbx_vertex *vertices;
    stfu_fbx_triangle *triangles;
} stfu_fbx_mesh_buffer;

typedef struct stfu_fbx_bone_info {
    int parent_index;
    const char *name;
} stfu_fbx_bone_info;

typedef struct stfu_fbx_animation_info {
    double time_begin;
    double time_end;
    const char *name;
} stfu_fbx_animation_info;

STFU_FBX_API stfu_fbx_scene *stfu_fbx_load(const char *path, stfu_fbx_error *error);
STFU_FBX_API void stfu_fbx_free(stfu_fbx_scene *scene);
STFU_FBX_API int stfu_fbx_get_scene_info(stfu_fbx_scene *scene, stfu_fbx_scene_info *info);
STFU_FBX_API int stfu_fbx_get_bone_info(stfu_fbx_scene *scene, int bone_index, stfu_fbx_bone_info *info);
STFU_FBX_API int stfu_fbx_get_animation_info(stfu_fbx_scene *scene, int animation_index, stfu_fbx_animation_info *info);
STFU_FBX_API int stfu_fbx_bake_mesh_at_time(
    stfu_fbx_scene *scene,
    int mesh_index,
    int animation_index,
    float time_seconds,
    stfu_fbx_mesh_buffer *out_mesh);
STFU_FBX_API void stfu_fbx_free_mesh_buffer(stfu_fbx_mesh_buffer *buffer);

#ifdef __cplusplus
}
#endif
