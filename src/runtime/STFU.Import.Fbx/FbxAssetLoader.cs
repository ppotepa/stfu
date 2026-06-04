using STFU.Abstractions.Loading;
using STFU.Animation.Clips;
using STFU.Animation.Skeleton;
using STFU.Assets;
using STFU.Import;
using STFU.Logging;

namespace STFU.Import.Fbx;

public sealed class FbxAssetLoader : IAssetLoader<string>
{
    public LoadResult<ImportedAsset> Load(string source, LoadContext context)
    {
        if (!File.Exists(source))
        {
            StfuLog.Write(
                StfuLogDomain.ImportFbx,
                "load.missing",
                source,
                StfuLogLevel.Warning);
            return LoadResult<ImportedAsset>.Fail($"FBX file was not found: {source}");
        }

        var options = ReadOptions(context);
        StfuLog.Write(
            StfuLogDomain.ImportFbx,
            "load.start",
            source,
            properties: new Dictionary<string, object?>
            {
                ["animationIndex"] = options.AnimationIndex,
                ["timeSeconds"] = options.TimeSeconds
            });

        try
        {
            var rawScene = FbxNative.Load(source, out var error);
            if (rawScene == 0)
            {
                StfuLog.Write(
                    StfuLogDomain.ImportFbx,
                    "load.failed",
                    error.GetMessage(),
                    StfuLogLevel.Error,
                    new Dictionary<string, object?> { ["path"] = source });
                return LoadResult<ImportedAsset>.Fail(error.GetMessage());
            }

            using var scene = NativeFbxSceneHandle.FromRaw(rawScene);
            var infoStatus = FbxNative.GetSceneInfo(scene.DangerousGetHandle(), out var info);
            if (infoStatus != 0)
            {
                StfuLog.Write(
                    StfuLogDomain.ImportFbx,
                    "scene_info.failed",
                    $"status={infoStatus}",
                    StfuLogLevel.Error,
                    new Dictionary<string, object?> { ["path"] = source });
                return LoadResult<ImportedAsset>.Fail($"FBX native scene info failed with status {infoStatus}.");
            }

            StfuLog.Write(
                StfuLogDomain.ImportFbx,
                "scene_info.loaded",
                source,
                properties: new Dictionary<string, object?>
                {
                    ["meshes"] = info.MeshCount,
                    ["skinnedMeshes"] = info.SkinnedMeshCount,
                    ["skeletons"] = info.SkeletonCount,
                    ["animations"] = info.AnimationCount
                });

            var skeletons = LoadSkeletons(scene.DangerousGetHandle(), info);
            var animations = LoadAnimations(scene.DangerousGetHandle(), info);
            var meshes = new List<ImportedMesh>(Math.Max(info.MeshCount, 0));
            for (var i = 0; i < info.MeshCount; i++)
            {
                var bakeStatus = FbxNative.BakeMeshAtTime(
                    scene.DangerousGetHandle(),
                    i,
                    options.AnimationIndex,
                    (float)options.TimeSeconds,
                    out var buffer);

                if (bakeStatus != 0)
                {
                    StfuLog.Write(
                        StfuLogDomain.ImportFbx,
                        "bake.failed",
                        $"mesh={i} status={bakeStatus}",
                        StfuLogLevel.Error,
                        new Dictionary<string, object?> { ["path"] = source });
                    return LoadResult<ImportedAsset>.Fail($"FBX native mesh bake failed for mesh {i} with status {bakeStatus}.");
                }

                try
                {
                    meshes.Add(new ImportedMesh($"fbx_mesh_{i}", buffer.ToMeshData()));
                }
                finally
                {
                    FbxNative.FreeMeshBuffer(ref buffer);
                }
            }

            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["format"] = "fbx",
                ["nativeLibrary"] = "stfu_fbx",
                ["meshCount"] = info.MeshCount.ToString(),
                ["skinnedMeshCount"] = info.SkinnedMeshCount.ToString(),
                ["skeletonCount"] = info.SkeletonCount.ToString(),
                ["animationCount"] = info.AnimationCount.ToString()
            };

            return LoadResult<ImportedAsset>.Ok(new ImportedAsset(
                source,
                meshes,
                [],
                skeletons,
                animations,
                metadata));
        }
        catch (DllNotFoundException exception)
        {
            StfuLog.Write(StfuLogDomain.ImportFbx, "native.dll_not_found", exception.Message, StfuLogLevel.Error, exception: exception);
            return LoadResult<ImportedAsset>.Fail(
                $"FBX native library 'stfu_fbx' was not found. Build src/native/STFU.Native.Fbx first. {exception.Message}");
        }
        catch (EntryPointNotFoundException exception)
        {
            StfuLog.Write(StfuLogDomain.ImportFbx, "native.entrypoint_missing", exception.Message, StfuLogLevel.Error, exception: exception);
            return LoadResult<ImportedAsset>.Fail(
                $"FBX native library 'stfu_fbx' is missing an expected entry point. {exception.Message}");
        }
        catch (BadImageFormatException exception)
        {
            StfuLog.Write(StfuLogDomain.ImportFbx, "native.bad_image", exception.Message, StfuLogLevel.Error, exception: exception);
            return LoadResult<ImportedAsset>.Fail(
                $"FBX native library 'stfu_fbx' has an incompatible architecture. {exception.Message}");
        }
    }

    private static FbxImportOptions ReadOptions(LoadContext context)
    {
        var animationIndex = context.TryGet<int>(AssetImportContextKeys.AnimationIndex, out var index)
            ? index
            : FbxImportOptions.BindPose.AnimationIndex;

        var timeSeconds = context.TryGet<double>(AssetImportContextKeys.TimeSeconds, out var seconds)
            ? seconds
            : FbxImportOptions.BindPose.TimeSeconds;

        return new FbxImportOptions(animationIndex, timeSeconds);
    }

    private static IReadOnlyList<SkeletonData> LoadSkeletons(nint scene, FbxNativeSceneInfo info)
    {
        if (info.SkeletonCount <= 0)
        {
            return [];
        }

        var bones = new List<BoneData>(info.SkeletonCount);
        for (var i = 0; i < info.SkeletonCount; i++)
        {
            var status = FbxNative.GetBoneInfo(scene, i, out var bone);
            if (status != 0)
            {
                continue;
            }

            bones.Add(new BoneData(i, bone.GetName(i), bone.ParentIndex, System.Numerics.Matrix4x4.Identity));
        }

        return bones.Count == 0
            ? []
            : [new SkeletonData(bones)];
    }

    private static IReadOnlyList<AnimationClip> LoadAnimations(nint scene, FbxNativeSceneInfo info)
    {
        if (info.AnimationCount <= 0)
        {
            return [];
        }

        var animations = new List<AnimationClip>(info.AnimationCount);
        for (var i = 0; i < info.AnimationCount; i++)
        {
            var status = FbxNative.GetAnimationInfo(scene, i, out var animation);
            if (status != 0)
            {
                continue;
            }

            var duration = Math.Max(0, animation.TimeEnd - animation.TimeBegin);
            animations.Add(new AnimationClip(animation.GetName(i), duration, 0, []));
        }

        return animations;
    }
}
