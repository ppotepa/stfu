using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Primitives;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Messaging.Commands;
using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.Strokes;

var tests = new NprPipelineTests();
tests.SketchPipelineBuildsRichGraphAndStyledPaths();
tests.SketchPipelineIsDeterministic();
tests.PresetRegistryExposesActiveEditablePresetMetadata();
Console.WriteLine("STFU.NPR.Tests passed.");

internal sealed class NprPipelineTests
{
    public void SketchPipelineBuildsRichGraphAndStyledPaths()
    {
        var (pipeline, context) = CreatePipelineContext();
        var frame = pipeline.Execute(context);

        Assert(context.Graph.Triangles.Count > 0, "Expected projected triangles.");
        Assert(context.Graph.TopologyEdges.Count > 0, "Expected topology edges.");
        Assert(context.Graph.SurfaceSamples.Count > 0, "Expected surface samples.");
        Assert(context.Graph.FeatureLines.Count > 0, "Expected feature lines.");
        Assert(context.Graph.Strokes.Count > 0, "Expected NPR strokes.");
        Assert(frame.Paths.Count == context.Graph.Strokes.Count, "Frame path count should match graph strokes.");
        Assert(frame.Paths.Any(path => path.Points.Count > 2), "Expected humanized multi-point paths.");
        Assert(frame.Paths.Any(path => path.Style.Opacity < 1f), "Expected opacity variation.");
        Assert(frame.Paths.Any(path => path.Style.Color != StrokeColor.Black), "Expected color/shade variation.");
        Assert(context.Graph.Strokes.Any(stroke => stroke.Intent == NprStrokeIntent.Hatch), "Expected hatching strokes.");
    }

    public void SketchPipelineIsDeterministic()
    {
        var first = CreatePipelineContext();
        var firstFrame = first.Pipeline.Execute(first.Context);
        var second = CreatePipelineContext();
        var secondFrame = second.Pipeline.Execute(second.Context);

        Assert(firstFrame.Paths.Count == secondFrame.Paths.Count, "Deterministic path count mismatch.");

        for (var index = 0; index < firstFrame.Paths.Count; index++)
        {
            var a = firstFrame.Paths[index];
            var b = secondFrame.Paths[index];
            Assert(a.Points.Count == b.Points.Count, "Deterministic point count mismatch.");
            Assert(Math.Abs(a.Style.Thickness - b.Style.Thickness) < 0.0001f, "Deterministic thickness mismatch.");
            Assert(Math.Abs(a.Style.Opacity - b.Style.Opacity) < 0.0001f, "Deterministic opacity mismatch.");

            for (var pointIndex = 0; pointIndex < a.Points.Count; pointIndex++)
            {
                Assert(Math.Abs(a.Points[pointIndex].X - b.Points[pointIndex].X) < 0.0001f, "Deterministic point X mismatch.");
                Assert(Math.Abs(a.Points[pointIndex].Y - b.Points[pointIndex].Y) < 0.0001f, "Deterministic point Y mismatch.");
            }
        }
    }

    public void PresetRegistryExposesActiveEditablePresetMetadata()
    {
        INprPreset preset = new GenericSketchNprPreset();
        var registry = new NprPresetRegistry(preset);
        var metadata = registry.ActivePreset.Metadata;

        Assert(metadata.Id == "generic-sketch", "Expected generic sketch preset id.");
        Assert(metadata.IsEditable, "Expected generic sketch preset to be editable.");
        Assert(registry.TryGet(metadata.Id, out var resolved) && ReferenceEquals(resolved, preset), "Expected preset registry lookup.");
        Assert(registry.ActivePreset.CreatePipeline() is not null, "Expected preset pipeline factory.");
        Assert(registry.ActivePreset.CreateSettings() is not null, "Expected preset settings factory.");
    }

    private static (INprPipeline Pipeline, NprContext Context) CreatePipelineContext()
    {
        var engine = StfuEngineBuilder
            .Create()
            .AddModule(new MeshModule())
            .Build();
        var assets = new AssetRegistry();
        var mesh = CreateCubeMesh();
        var handle = assets.AddMesh("cube", mesh);
        var commands = new CommandBuffer();
        commands.Enqueue(new CreateEntityCommand("Cube"));
        engine.Tick(commands);
        commands.Enqueue(new AssignMeshToEntityCommand(engine.Scene.Entities[0].Id, handle));
        engine.Tick(commands);

        var settings = SketchNprPreset.CreateSettings();
        settings.HatchDensity = 1f;
        settings.SurfaceFlowDensity = 1f;
        settings.HatchShadeThreshold = 0.15f;
        settings.SurfaceFlowShadeThreshold = 0.15f;

        return (
            SketchNprPreset.CreatePipeline(),
            new NprContext
            {
                Scene = engine.Scene,
                Assets = assets,
                Camera = CameraState.Default,
                Width = 800,
                Height = 600,
                Settings = settings
            });
    }

    private static MeshData CreateCubeMesh()
    {
        var vertices = new[]
        {
            new MeshVertex(new Vector3(-1, -1, -1), Vector3.Normalize(new Vector3(-1, -1, -1))),
            new MeshVertex(new Vector3(1, -1, -1), Vector3.Normalize(new Vector3(1, -1, -1))),
            new MeshVertex(new Vector3(1, 1, -1), Vector3.Normalize(new Vector3(1, 1, -1))),
            new MeshVertex(new Vector3(-1, 1, -1), Vector3.Normalize(new Vector3(-1, 1, -1))),
            new MeshVertex(new Vector3(-1, -1, 1), Vector3.Normalize(new Vector3(-1, -1, 1))),
            new MeshVertex(new Vector3(1, -1, 1), Vector3.Normalize(new Vector3(1, -1, 1))),
            new MeshVertex(new Vector3(1, 1, 1), Vector3.Normalize(new Vector3(1, 1, 1))),
            new MeshVertex(new Vector3(-1, 1, 1), Vector3.Normalize(new Vector3(-1, 1, 1)))
        };

        var triangles = new[]
        {
            new MeshTriangle(0, 2, 1), new MeshTriangle(0, 3, 2),
            new MeshTriangle(4, 5, 6), new MeshTriangle(4, 6, 7),
            new MeshTriangle(0, 1, 5), new MeshTriangle(0, 5, 4),
            new MeshTriangle(1, 2, 6), new MeshTriangle(1, 6, 5),
            new MeshTriangle(2, 3, 7), new MeshTriangle(2, 7, 6),
            new MeshTriangle(3, 0, 4), new MeshTriangle(3, 4, 7)
        };

        return new MeshData(vertices, triangles);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
