#include "stfu_fbx.h"

#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include "../../../third_party/ufbx/ufbx.h"

struct stfu_fbx_scene {
    ufbx_scene *scene;
    ufbx_scene *evaluated_scene;
    int cached_animation_index;
    float cached_time_seconds;
};

static char g_last_error[2048];

static void stfu_set_error(stfu_fbx_error *error, int code, const char *message)
{
    if (!error) return;
    error->code = code;
    error->message = message;
}

static void stfu_set_ufbx_error(stfu_fbx_error *error, int code, const ufbx_error *ufbx_error)
{
    if (!error) return;
    g_last_error[0] = '\0';
    ufbx_format_error(g_last_error, sizeof(g_last_error), ufbx_error);
    error->code = code;
    error->message = g_last_error;
}

static int stfu_count_skinned_meshes(const ufbx_scene *scene)
{
    int count = 0;
    for (size_t i = 0; i < scene->meshes.count; i++) {
        const ufbx_mesh *mesh = scene->meshes.data[i];
        if (mesh && mesh->skin_deformers.count > 0) {
            count++;
        }
    }
    return count;
}

static const ufbx_vertex_vec3 *stfu_choose_positions(const ufbx_mesh *mesh)
{
    if (mesh->skinned_position.values.count > 0 && mesh->skinned_position.indices.count > 0) {
        return &mesh->skinned_position;
    }

    return &mesh->vertex_position;
}

static const ufbx_vertex_vec3 *stfu_choose_normals(const ufbx_mesh *mesh)
{
    if (mesh->skinned_normal.values.count > 0 && mesh->skinned_normal.indices.count > 0) {
        return &mesh->skinned_normal;
    }

    return &mesh->vertex_normal;
}

static ufbx_node *stfu_first_mesh_node(const ufbx_mesh *mesh)
{
    if (mesh->instances.count == 0) {
        return NULL;
    }

    return mesh->instances.data[0];
}

static ufbx_node *stfu_first_bone_node(const ufbx_bone *bone)
{
    if (!bone || bone->instances.count == 0) {
        return NULL;
    }

    return bone->instances.data[0];
}

static int stfu_find_bone_index_by_node(const ufbx_scene *scene, const ufbx_node *node)
{
    if (!scene || !node) {
        return -1;
    }

    for (size_t i = 0; i < scene->bones.count; i++) {
        const ufbx_bone *bone = scene->bones.data[i];
        if (!bone) continue;

        for (size_t j = 0; j < bone->instances.count; j++) {
            if (bone->instances.data[j] == node) {
                return (int)i;
            }
        }
    }

    return -1;
}

static int stfu_find_parent_bone_index(const ufbx_scene *scene, const ufbx_bone *bone)
{
    ufbx_node *node = stfu_first_bone_node(bone);
    if (!node) {
        return -1;
    }

    for (ufbx_node *parent = node->parent; parent && !parent->is_root; parent = parent->parent) {
        int index = stfu_find_bone_index_by_node(scene, parent);
        if (index >= 0) {
            return index;
        }
    }

    return -1;
}

static const char *stfu_bone_name(const ufbx_bone *bone)
{
    if (!bone) return "";
    if (bone->name.data && bone->name.length > 0) {
        return bone->name.data;
    }

    ufbx_node *node = stfu_first_bone_node(bone);
    if (node && node->name.data) {
        return node->name.data;
    }

    return "";
}

stfu_fbx_scene *stfu_fbx_load(const char *path, stfu_fbx_error *error)
{
    stfu_set_error(error, 0, NULL);

    if (!path || path[0] == '\0') {
        stfu_set_error(error, 1, "FBX path is empty.");
        return NULL;
    }

    ufbx_load_opts opts;
    memset(&opts, 0, sizeof(opts));
    opts.generate_missing_normals = true;
    opts.target_axes = ufbx_axes_right_handed_y_up;

    ufbx_error uerr;
    ufbx_scene *scene = ufbx_load_file(path, &opts, &uerr);
    if (!scene) {
        stfu_set_ufbx_error(error, 2, &uerr);
        return NULL;
    }

    stfu_fbx_scene *wrapper = (stfu_fbx_scene*)calloc(1, sizeof(stfu_fbx_scene));
    if (!wrapper) {
        ufbx_free_scene(scene);
        stfu_set_error(error, 3, "Out of memory while allocating FBX scene wrapper.");
        return NULL;
    }

    wrapper->scene = scene;
    return wrapper;
}

void stfu_fbx_free(stfu_fbx_scene *scene)
{
    if (!scene) return;
    if (scene->evaluated_scene) {
        ufbx_free_scene(scene->evaluated_scene);
    }
    if (scene->scene) {
        ufbx_free_scene(scene->scene);
    }
    free(scene);
}

int stfu_fbx_get_scene_info(stfu_fbx_scene *scene, stfu_fbx_scene_info *info)
{
    if (!scene || !scene->scene || !info) {
        return 1;
    }

    memset(info, 0, sizeof(*info));
    info->mesh_count = (int)scene->scene->meshes.count;
    info->skinned_mesh_count = stfu_count_skinned_meshes(scene->scene);
    info->skeleton_count = (int)scene->scene->bones.count;
    info->animation_count = (int)scene->scene->anim_stacks.count;
    return 0;
}

int stfu_fbx_get_bone_info(stfu_fbx_scene *scene, int bone_index, stfu_fbx_bone_info *info)
{
    if (!scene || !scene->scene || !info || bone_index < 0 || (size_t)bone_index >= scene->scene->bones.count) {
        return 1;
    }

    const ufbx_bone *bone = scene->scene->bones.data[bone_index];
    memset(info, 0, sizeof(*info));
    info->parent_index = stfu_find_parent_bone_index(scene->scene, bone);
    info->name = stfu_bone_name(bone);
    return 0;
}

int stfu_fbx_get_animation_info(stfu_fbx_scene *scene, int animation_index, stfu_fbx_animation_info *info)
{
    if (!scene || !scene->scene || !info || animation_index < 0 || (size_t)animation_index >= scene->scene->anim_stacks.count) {
        return 1;
    }

    const ufbx_anim_stack *stack = scene->scene->anim_stacks.data[animation_index];
    memset(info, 0, sizeof(*info));
    info->time_begin = stack->time_begin;
    info->time_end = stack->time_end;
    info->name = stack->name.data ? stack->name.data : "";
    return 0;
}

int stfu_fbx_bake_mesh_at_time(
    stfu_fbx_scene *scene,
    int mesh_index,
    int animation_index,
    float time_seconds,
    stfu_fbx_mesh_buffer *out_mesh)
{
    if (!scene || !scene->scene || !out_mesh) {
        return 1;
    }

    memset(out_mesh, 0, sizeof(*out_mesh));

    ufbx_scene *source_scene = scene->scene;

    if (animation_index >= 0 && (size_t)animation_index < source_scene->anim_stacks.count) {
        if (!scene->evaluated_scene ||
            scene->cached_animation_index != animation_index ||
            scene->cached_time_seconds != time_seconds) {
            if (scene->evaluated_scene) {
                ufbx_free_scene(scene->evaluated_scene);
                scene->evaluated_scene = NULL;
            }

            ufbx_anim *anim = source_scene->anim_stacks.data[animation_index]->anim;
            ufbx_evaluate_opts eval_opts;
            memset(&eval_opts, 0, sizeof(eval_opts));
            eval_opts.evaluate_skinning = true;
            eval_opts.evaluate_caches = true;

            ufbx_error eval_error;
            scene->evaluated_scene = ufbx_evaluate_scene(source_scene, anim, (double)time_seconds, &eval_opts, &eval_error);
            if (!scene->evaluated_scene) {
                return 2;
            }

            scene->cached_animation_index = animation_index;
            scene->cached_time_seconds = time_seconds;
        }

        source_scene = scene->evaluated_scene;
    }

    if (mesh_index < 0 || (size_t)mesh_index >= source_scene->meshes.count) {
        return 3;
    }

    const ufbx_mesh *mesh = source_scene->meshes.data[mesh_index];
    if (!mesh || mesh->num_indices == 0 || mesh->num_triangles == 0) {
        return 4;
    }

    if (mesh->num_indices > (size_t)INT32_MAX || mesh->num_triangles > (size_t)INT32_MAX) {
        return 5;
    }

    out_mesh->vertex_count = (int)mesh->num_indices;
    out_mesh->triangle_count = (int)mesh->num_triangles;
    out_mesh->vertices = (stfu_fbx_vertex*)calloc(mesh->num_indices, sizeof(stfu_fbx_vertex));
    out_mesh->triangles = (stfu_fbx_triangle*)calloc(mesh->num_triangles, sizeof(stfu_fbx_triangle));

    if (!out_mesh->vertices || !out_mesh->triangles) {
        stfu_fbx_free_mesh_buffer(out_mesh);
        return 6;
    }

    const ufbx_vertex_vec3 *positions = stfu_choose_positions(mesh);
    const ufbx_vertex_vec3 *normals = stfu_choose_normals(mesh);
    ufbx_node *node = stfu_first_mesh_node(mesh);
    ufbx_matrix normal_matrix = node ? ufbx_matrix_for_normals(&node->geometry_to_world) : ufbx_identity_matrix;

    for (size_t i = 0; i < mesh->num_indices; i++) {
        ufbx_vec3 position = ufbx_get_vertex_vec3(positions, i);
        ufbx_vec3 normal = normals->values.count > 0 && normals->indices.count > i
            ? ufbx_get_vertex_vec3(normals, i)
            : ufbx_zero_vec3;

        if (node) {
            position = ufbx_transform_position(&node->geometry_to_world, position);
            normal = ufbx_transform_direction(&normal_matrix, normal);
        }

        out_mesh->vertices[i].x = (float)position.x;
        out_mesh->vertices[i].y = (float)position.y;
        out_mesh->vertices[i].z = (float)position.z;
        out_mesh->vertices[i].normal_x = (float)normal.x;
        out_mesh->vertices[i].normal_y = (float)normal.y;
        out_mesh->vertices[i].normal_z = (float)normal.z;
    }

    uint32_t *tri_indices = (uint32_t*)malloc(mesh->max_face_triangles * 3u * sizeof(uint32_t));
    if (!tri_indices) {
        stfu_fbx_free_mesh_buffer(out_mesh);
        return 7;
    }

    size_t triangle_index = 0;
    for (size_t face_index = 0; face_index < mesh->faces.count; face_index++) {
        ufbx_face face = mesh->faces.data[face_index];
        uint32_t num_triangles = ufbx_triangulate_face(
            tri_indices,
            mesh->max_face_triangles * 3u,
            mesh,
            face);

        for (uint32_t i = 0; i < num_triangles; i++) {
            if (triangle_index >= mesh->num_triangles) {
                free(tri_indices);
                stfu_fbx_free_mesh_buffer(out_mesh);
                return 8;
            }

            out_mesh->triangles[triangle_index].a = (int)tri_indices[i * 3u + 0u];
            out_mesh->triangles[triangle_index].b = (int)tri_indices[i * 3u + 1u];
            out_mesh->triangles[triangle_index].c = (int)tri_indices[i * 3u + 2u];
            triangle_index++;
        }
    }

    free(tri_indices);

    return 0;
}

void stfu_fbx_free_mesh_buffer(stfu_fbx_mesh_buffer *buffer)
{
    if (!buffer) return;
    free(buffer->vertices);
    free(buffer->triangles);
    memset(buffer, 0, sizeof(*buffer));
}
