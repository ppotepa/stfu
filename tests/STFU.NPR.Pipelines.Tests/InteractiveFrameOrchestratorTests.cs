using System.Numerics;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.Strokes;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveFrameOrchestratorTests
{
    [Fact]
    public void Diagnostics_records_stage_timing()
    {
        var diagnostics = new InteractiveFrameDiagnostics();

        diagnostics.AddStageTiming("InteractiveProjection", TimeSpan.FromMilliseconds(1.25));

        Assert.Equal(1.25, diagnostics.ProjectionMs);
        Assert.True(diagnostics.StageTimingsMs.ContainsKey("InteractiveProjection"));
        Assert.Equal(1.25, diagnostics.StageTimingsMs["InteractiveProjection"]);
    }

    [Fact]
    public void Pipeline_result_requires_reference_fallback_when_diagnostics_says_so()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            Strategy = FramePipelineStrategy.InteractivePerformance,
            UsedReferenceFallback = true
        };

        var result = new InteractivePipelineResult(diagnostics);

        Assert.True(result.RequiresReferenceFallback);
        Assert.Equal("InteractivePerformance", diagnostics.Strategy.ToString());
    }

    [Fact]
    public void StrokeCommandPlanner_maps_candidate_edges_to_draw_commands()
    {
        var candidates = new[]
        {
            new InteractiveCandidateEdge(
                SourceEdgeId: 7,
                FaceA: 1,
                FaceB: 2,
                Role: 0,
                X0: 10,
                Y0: 20,
                X1: 110,
                Y1: 120,
                ProjectedLength: 140,
                Depth: 0.5f,
                Importance: 1f)
        };

        var commands = StrokeCommandPlanner.BuildCommands(candidates);

        var command = Assert.Single(commands);
        Assert.Equal(7, command.SourceEdgeId);
        Assert.Equal(0, command.Role);
        Assert.Equal(10, command.X0);
        Assert.Equal(120, command.Y1);
        Assert.True(command.Width >= 0.5f);
        Assert.InRange(command.Opacity, 0.25f, 1f);
        Assert.Equal(command.Role, command.StyleKey);
    }

    [Fact]
    public void StrokeCommandPlanner_skips_degenerate_candidates()
    {
        var candidates = new[]
        {
            new InteractiveCandidateEdge(
                SourceEdgeId: 1,
                FaceA: 0,
                FaceB: 1,
                Role: 2,
                X0: 0,
                Y0: 0,
                X1: 0,
                Y1: 0,
                ProjectedLength: 0,
                Depth: 0,
                Importance: 1f)
        };

        var commands = StrokeCommandPlanner.BuildCommands(candidates);

        Assert.Empty(commands);
    }

    [Fact]
    public void ToneCoveragePlanner_builds_regions_from_visible_faces()
    {
        var triangles = new[]
        {
            CreateTriangle(0, shade: 0.15f),
            CreateTriangle(1, shade: 0.50f),
            CreateTriangle(2, shade: 0.90f)
        };

        var regions = ToneCoveragePlanner.BuildRegions(
            triangles,
            [0, 1, 2],
            InteractiveQualityMode.QualityViewport);

        Assert.Equal(3, regions.Length);
        Assert.Contains(regions, region => region.Bucket == InteractiveToneBucket.Shadow);
        Assert.Contains(regions, region => region.Bucket == InteractiveToneBucket.Midtone);
        Assert.Contains(regions, region => region.Bucket == InteractiveToneBucket.Highlight);
    }

    [Fact]
    public void ToneCoveragePlanner_applies_fast_preview_budget()
    {
        var triangles = Enumerable.Range(0, 900)
            .Select(index => CreateTriangle(index, shade: 0.45f))
            .ToArray();
        var sourceFaces = Enumerable.Range(0, triangles.Length).ToArray();

        var fastRegions = ToneCoveragePlanner.BuildRegions(
            triangles,
            sourceFaces,
            InteractiveQualityMode.FastPreview);
        var qualityRegions = ToneCoveragePlanner.BuildRegions(
            triangles,
            sourceFaces,
            InteractiveQualityMode.QualityViewport);

        Assert.True(fastRegions.Length <= 512);
        Assert.True(fastRegions.Length < qualityRegions.Length);
        Assert.Equal(triangles.Length, qualityRegions.Length);
    }

    [Fact]
    public void InteractiveFrameHasher_changes_when_float_changes()
    {
        var a = InteractiveFrameHasher.Mix(InteractiveFrameHasher.Empty, 1.0f);
        var b = InteractiveFrameHasher.Mix(InteractiveFrameHasher.Empty, 2.0f);

        Assert.NotEqual(a, b);
    }


    private static ProjectedTriangle CreateTriangle(int stableId, float shade, bool visible = true)
    {
        return new ProjectedTriangle(
            StableId: stableId,
            ProjectedMeshIndex: 0,
            MeshTriangleIndex: stableId,
            A: 0,
            B: 1,
            C: 2,
            Normal: Vector3.UnitZ,
            WorldCenter: Vector3.Zero,
            ScreenCenter: new Point2D(stableId * 3f, stableId * 2f),
            Depth: 0.5f,
            ScreenArea: 24f,
            Shade: shade,
            IsFrontFacing: true,
            IsVisible: visible);
    }
}
