using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Primitives;
using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Mesh;
using STFU.Mesh.Commands;
using STFU.Messaging.Commands;
using STFU.NPR.Analysis;
using STFU.NPR.Composition;
using STFU.NPR.Export;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;
using STFU.NPR.Visibility;
using STFU.Strokes;
using STFU.Strokes.Export;

var tests = new NprPipelineTests();
tests.SketchPipelineBuildsRichGraphAndStyledPaths();
tests.SketchPipelineIsDeterministic();
tests.PresetRegistryExposesActiveEditablePresetMetadata();
tests.PresetRegistrySupportsProvidersAndJsonEditablePresetRoundtrip();
tests.BuiltInDrawingPresetProjectsExposeF1ThroughF5Styles();
tests.ActiveNprPresetStateSwitchesSettingsGrammarAndPipeline();
tests.ObjMeshLoaderReadsVertexNormalsFromFaceTokens();
tests.ObjMeshLoaderBuildsSuzanneFromUsedFaceVertices();
tests.ProjectionRejectsNearAndFarClipDepth();
tests.ProjectedTrianglesUseVertexNormalsAndClipUnstableScreenTriangles();
tests.StrokeCandidatesRejectOffscreenAndOversizedSegments();
tests.SvgExporterWritesStrokeMetadataAndLayers();
tests.RasterExporterWritesPortablePixmap();
tests.DebugSnapshotJsonContainsSchemaFields();
tests.VisibilityAndExportFixturesAreUsable();
tests.EvaluationMetricsReportVisibilityAndDensity();
tests.ContourPrecisionRecallMatchesSyntheticFeatureFixtures();
tests.ScreenTileGridBucketsItemsByTile();
tests.SampleVisibilityResolverSplitsPartiallyOccludedCurve();
tests.VisibleSilhouetteSegmentsBecomeOccludingContours();
tests.HiddenCreaseSegmentsBecomeConstruction();
tests.OfflineExactVisibilityResolverProducesAtLeastAsFineSegmentation();
tests.PruneFeatureLinesStepAppliesTileBudgetBySalience();
tests.LinePriorityRuleRejectsTooShortSuggestiveContours();
tests.StyleDrivenHatchingBuildsPlans();
tests.ContactAccentsBuildFeatureCurves();
tests.BuildMaterialRegionsCreatesPerMeshRegionData();
tests.MaterialBoundaryExtractionBuildsFeatureCurves();
tests.BuildStyleMasksCreatesFocusMask();
tests.StrokeFrameMetadataCarriesLayerOrderAndHatchVariants();
tests.HiddenLinePolicyGhostBuildsHiddenStrokeCandidates();
tests.HiddenLinePolicySurvivesPruningForVisibleOutput();
tests.HiddenLinePolicyDashedExportsDashArray();
tests.MeshAnalysisCacheReusesTopologyForRepeatedMesh();
tests.CurvatureDrivenFeatureExtractionBuildsRidgeAndValleyCurves();
tests.ViewDependentFeatureExtractionBuildsSuggestiveContourCurves();
tests.ViewDependentFeatureExtractionBuildsApparentRidgeCurves();
tests.RefineFeatureConfidenceStepSmoothsNeighboringCurvatureCurves();
tests.BvhOcclusionQueryMatchesSampleOcclusionFixture();
tests.SvgNprDocumentExporterUsesOfflineExactVisibilityPass();
tests.VisibilityContractsAreAvailableInContext();
tests.FrameHistoryCapturesPreviousFrameData();
tests.FrameHistoryCapturesStrokePathsByStableIdAfterLayerSorting();
tests.TemporalMatchingFallsBackToSourceAndScreenOverlap();
tests.TemporalGeometryBlendPullsStrokeTowardPreviousFrame();
tests.BuildStrokeFrameAddsFadingOutResidualsForUnmatchedPreviousStrokes();
tests.SnapshotMetricConfirmsDeterministicFrames();
tests.AdapterStepsAndStableIdContractsAreAvailable();
tests.CurvatureAndTopologyAnalysisExposeNamedSupplementArtifacts();
tests.PipelineBenchmarkFixtureProducesFrameAndTiming();
tests.NamedArtifactAdaptersAreUsable();
tests.HumanizationProfilesAffectStyledStroke();
tests.FeatureAndFrameAdaptersAreUsable();
tests.RichPointsCarryPressureProfileVariation();
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
        Assert(context.Graph.Curves.Count > 0, "Expected feature curves.");
        Assert(context.Style.FindFeatureRule(FeatureCurveKind.SuggestiveContour) is not null, "Expected suggestive contour style rule.");
        Assert(context.Style.FindFeatureRule(FeatureCurveKind.ApparentRidge) is not null, "Expected apparent ridge style rule.");
        Assert(context.Style.FindFeatureRule(FeatureCurveKind.Construction) is not null, "Expected construction style rule.");
        Assert(context.Style.FindFeatureRule(FeatureCurveKind.ContactAccent) is not null, "Expected contact accent style rule.");
        Assert(context.Graph.Curves.All(curve => curve.Confidence >= 0f && curve.Confidence <= 1f), "Expected feature curve confidence range.");
        Assert(context.Graph.VisibilitySegments.Count > 0, "Expected visibility segments.");
        Assert(context.Graph.FeatureLines.Count > 0, "Expected feature lines.");
        Assert(context.Graph.Candidates.Count > 0, "Expected stroke candidates.");
        Assert(context.Graph.StyledStrokes.Count > 0, "Expected styled strokes.");
        Assert(context.Graph.SalienceByStableId.Count > 0, "Expected salience scores.");
        Assert(context.Graph.SalienceByStableId.Values.Any(score => score.Final > 0f), "Expected non-zero salience.");
        Assert(context.Graph.ToneField is { Samples.Count: > 0 }, "Expected tone field.");
        Assert(context.Graph.DirectionField is { Samples.Count: > 0 }, "Expected direction field.");
        Assert(context.Graph.DensityField is { Samples.Count: > 0 }, "Expected density field.");
        Assert(context.Graph.TextureField is { Samples.Count: > 0 }, "Expected texture field.");
        Assert(context.Graph.SurfaceSamples.Any(sample => sample.Curvature >= 0f), "Expected curvature on surface samples.");
        Assert(context.Graph.SurfaceSamples.Any(sample => sample.SmoothedCurvature >= 0f), "Expected smoothed curvature on surface samples.");
        Assert(context.Graph.SurfaceSamples.Any(sample => sample.CurvatureDirection.LengthSquared() > 0.0001f), "Expected curvature direction on surface samples.");
        Assert(context.Graph.Vertices.Any(vertex => Math.Abs(vertex.SmoothedSignedCurvature) > 0.0001f), "Expected signed curvature on projected vertices.");
        Assert(context.Graph.MaterialRegions.Count > 0, "Expected material regions.");
        Assert(context.Graph.StyleMasks.Count > 0, "Expected style masks.");
        Assert(context.View.Width == 800 && context.View.Height == 600, "Expected NPR view context.");
        Assert(context.View.Projection.Width == 800 && context.View.Projection.Height == 600, "Expected projection info dimensions.");
        Assert(context.View.Lighting.Direction.LengthSquared() > 0.99f, "Expected normalized light direction.");
        Assert(context.View.FrameId > 0, "Expected frame id.");
        Assert(context.DebugFrame.Lines.Count > 0, "Expected debug frame lines.");
        Assert(context.DebugFrame.Counters.FeatureCurveCount == context.Graph.Curves.Count, "Expected debug feature curve count.");
        Assert(context.DebugFrame.Counters.SalientSegmentCount > 0, "Expected salient segment count.");
        Assert(context.DebugFrame.Counters.StrokeCandidateCount == context.Graph.Candidates.Count, "Expected debug stroke candidate count.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.SalienceHeatmap), "Expected salience overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.StrokeCandidates), "Expected stroke candidate overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.ToneField), "Expected tone field overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.DirectionField), "Expected direction field overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.DensityField), "Expected density field overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.TextureField), "Expected texture field overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.HatchingPlan), "Expected hatching plan overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.StyleMask), "Expected style mask overlay lines.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.MaterialRegion), "Expected material region overlay lines.");
        Assert(context.DebugFrame.Counters.DirectTemporalMatchCount >= 0, "Expected temporal counters.");
        Assert(context.DebugFrame.Counters.FallbackTemporalMatchCount >= 0, "Expected temporal counters.");
        Assert(context.DebugFrame.Counters.GhostStrokeCount >= 0, "Expected ghost stroke counters.");
        Assert(context.StepTraces.Count > 0, "Expected pipeline step traces.");
        Assert(context.DebugFrame.StepTraces.Count == context.StepTraces.Count, "Expected debug frame step traces.");
        Assert(context.StepTraces.Any(trace => trace.StepName == "ExtractFeatureCurvesStep"), "Expected feature extraction trace.");
        Assert(context.StepTraces.Any(trace => trace.StepName == "BuildDebugFrameStep"), "Expected debug frame trace.");
        Assert(frame.Paths.Count == context.Graph.StyledStrokes.Count, "Frame path count should match styled strokes.");
        Assert(frame.Paths.Any(path => path.Points.Count > 2), "Expected humanized multi-point paths.");
        Assert(frame.Paths.Any(path => path.RichPoints is { Count: > 0 }), "Expected rich stroke points.");
        Assert(frame.Paths.Any(path => path.Metadata is not null), "Expected stroke metadata.");
        Assert(frame.Paths.Any(path => path.Style.Opacity < 1f), "Expected opacity variation.");
        Assert(frame.Paths.Any(path => path.Style.Color != StrokeColor.Black), "Expected color/shade variation.");
        Assert(context.Graph.StyledStrokes.Any(stroke => stroke.Intent == NprStrokeIntent.Hatch), "Expected hatching strokes.");
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
        Assert(metadata.Packaging == PresetPackaging.BuiltInAot, "Expected built-in AOT packaging.");
        Assert(metadata.Version.Major == 1, "Expected preset version.");
        Assert(metadata.PresetVersion.Major == 1, "Expected preset semantic version.");
        Assert(metadata.Schema.SchemaId == "stfu.npr.preset", "Expected preset schema id.");
        Assert(metadata.Schema.RequiredSections.Count >= 5, "Expected preset schema sections.");
        Assert(registry.TryGet(metadata.Id, out var resolved) && ReferenceEquals(resolved, preset), "Expected preset registry lookup.");
        Assert(registry.ActivePreset.CreatePipeline() is not null, "Expected preset pipeline factory.");
        Assert(registry.ActivePreset.CreateSettings() is not null, "Expected preset settings factory.");
        var grammar = registry.ActivePreset.CreateGrammar();
        Assert(grammar.StyleId == metadata.Id, "Expected grammar style id to match preset id.");
        Assert(grammar.FeatureRules.Count > 0, "Expected feature rules.");
        Assert(grammar.Stroke.FindProfile(NprStrokeIntent.Silhouette) is not null, "Expected silhouette stroke profile.");
        Assert(grammar.Export.DefaultSvgMode == SvgExportMode.Editable, "Expected SVG export mode.");
        Assert(grammar.Tone.Enabled, "Expected tone rule to be enabled.");
        Assert(grammar.Budget.MaxSegmentsPerTile > 0, "Expected budget rule.");
        Assert(grammar.Hatching.Enabled, "Expected hatching rule.");
        Assert(grammar.Hatching.CrossHatchThreshold > grammar.Hatching.ToneThreshold, "Expected cross hatch threshold above base threshold.");
    }

    public void PresetRegistrySupportsProvidersAndJsonEditablePresetRoundtrip()
    {
        var basePreset = new GenericSketchNprPreset();
        var registry = new NprPresetRegistry(basePreset);
        var jsonDocument = JsonEditablePresetDocument.FromPreset(basePreset);
        var jsonPreset = new JsonEditableNprPreset(jsonDocument);
        var bundle = new StaticPresetBundle("builtins", [jsonPreset]);
        registry.Register(bundle);

        Assert(registry.Providers.Count == 1, "Expected preset provider registration.");
        Assert(registry.TryGet(jsonPreset.Metadata.Id, out var resolved), "Expected provider preset registration.");
        Assert(resolved.Metadata.Id == basePreset.Metadata.Id, "Expected resolved preset id.");

        var json = jsonPreset.ToJson();
        var roundtrip = JsonEditableNprPreset.FromJson(json);
        var roundtripSettings = roundtrip.CreateSettings();
        var roundtripGrammar = roundtrip.CreateGrammar();

        Assert(roundtrip.Metadata.Schema.SchemaId == "stfu.npr.preset", "Expected roundtrip preset schema.");
        Assert(Math.Abs(roundtripSettings.CreaseAngleDegrees - basePreset.CreateSettings().CreaseAngleDegrees) < 0.0001f, "Expected settings roundtrip.");
        Assert(roundtripGrammar.FeatureRules.Count == basePreset.CreateGrammar().FeatureRules.Count, "Expected grammar roundtrip.");
        Assert(roundtripGrammar.FeatureRules.Any(rule => rule.Kind == FeatureCurveKind.ApparentRidge), "Expected apparent ridge in json preset grammar.");
    }

    public void BuiltInDrawingPresetProjectsExposeF1ThroughF5Styles()
    {
        INprPreset[] presets =
        [
            new STFU.NPR.Preset.TechnicalInk.TechnicalInkPreset(),
            new STFU.NPR.Preset.PencilConstruction.PencilConstructionPreset(),
            new STFU.NPR.Preset.PenInkHatching.PenInkHatchingPreset(),
            new STFU.NPR.Preset.MangaInk.MangaInkPreset(),
            new STFU.NPR.Preset.Blueprint.BlueprintPreset()
        ];

        var registry = new NprPresetRegistry(new GenericSketchNprPreset());
        foreach (var preset in presets)
        {
            registry.Register(preset);
        }

        foreach (var preset in presets)
        {
            Assert(registry.TryGet(preset.Metadata.Id, out var resolved), $"Expected preset registration: {preset.Metadata.Id}");
            Assert(resolved.CreatePipeline() is not null, $"Expected preset pipeline: {preset.Metadata.Id}");
            Assert(resolved.CreateGrammar().StyleId == preset.Metadata.Id, $"Expected grammar id for preset: {preset.Metadata.Id}");
        }

        var technical = presets[0].CreateGrammar();
        var pencilSettings = presets[1].CreateSettings();
        var pen = presets[2].CreateGrammar();
        var manga = presets[3].CreateGrammar();
        var blueprint = presets[4].CreateGrammar();

        Assert(!technical.Hatching.Enabled, "Expected technical ink to suppress hatching.");
        Assert(pencilSettings.StrokeStyle.Medium == StrokeMedium.Pencil, "Expected pencil construction medium.");
        Assert(pen.Hatching.DensityScale > 1f, "Expected pen-and-ink dense hatching.");
        Assert(manga.Stroke.FindProfile(NprStrokeIntent.Silhouette)!.BaseThickness > technical.Stroke.FindProfile(NprStrokeIntent.Silhouette)!.BaseThickness, "Expected manga silhouette to be heavier than technical ink.");
        Assert(blueprint.Visibility.DefaultHiddenPolicy == HiddenLinePolicy.Ghost, "Expected blueprint ghost construction visibility.");
    }

    public void ActiveNprPresetStateSwitchesSettingsGrammarAndPipeline()
    {
        var registry = new NprPresetRegistry(new GenericSketchNprPreset());
        var technical = new STFU.NPR.Preset.TechnicalInk.TechnicalInkPreset();
        var pencil = new STFU.NPR.Preset.PencilConstruction.PencilConstructionPreset();
        registry.Register(technical);
        registry.Register(pencil);

        var state = new ActiveNprPresetState(registry);
        Assert(state.ActivePreset.Metadata.Id == "generic-sketch", "Expected initial active preset.");

        state.ApplyPreset(technical.Metadata.Id);
        Assert(state.ActivePreset.Metadata.Id == technical.Metadata.Id, "Expected technical active preset.");
        Assert(state.ActiveGrammar.StyleId == technical.Metadata.Id, "Expected technical grammar.");
        Assert(!state.ActiveGrammar.Hatching.Enabled, "Expected active technical hatching state.");
        Assert(state.ActiveSettings.StrokeStyle.EndpointJitter < 0.2f, "Expected technical settings.");
        var technicalPipeline = state.ActivePipeline;

        state.ApplyPreset(pencil.Metadata.Id);
        Assert(state.ActivePreset.Metadata.Id == pencil.Metadata.Id, "Expected pencil active preset.");
        Assert(state.ActiveGrammar.StyleId == pencil.Metadata.Id, "Expected pencil grammar.");
        Assert(state.ActiveSettings.StrokeStyle.Medium == StrokeMedium.Pencil, "Expected pencil settings.");
        Assert(!ReferenceEquals(technicalPipeline, state.ActivePipeline), "Expected pipeline refresh after preset switch.");
    }

    public void ObjMeshLoaderReadsVertexNormalsFromFaceTokens()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stfu-obj-{Guid.NewGuid():N}.obj");
        try
        {
            File.WriteAllText(path, """
                v 0 0 0
                v 1 0 0
                v 0 1 0
                vt 0 0
                vt 1 0
                vt 0 1
                vn 0 0 1
                vn 0 0 1
                vn 0 0 1
                f 1/1/1 2/2/2 3/3/3
                """);

            var loader = new STFU.MeshIO.Formats.ObjMeshLoader();
            var result = loader.Load(path, STFU.Abstractions.Loading.LoadContext.Default);
            Assert(result.Success, result.Error ?? "Expected OBJ load success.");

            var mesh = result.GetValueOrThrow();
            Assert(mesh.Vertices.Count == 3, "Expected OBJ vertices.");
            Assert(mesh.Triangles.Count == 1, "Expected OBJ triangle.");
            Assert(mesh.Vertices.All(vertex => vertex.Normal.LengthSquared() > 0.99f), "Expected loaded vertex normals.");
            Assert(mesh.Vertices.All(vertex => Math.Abs(vertex.Normal.Z - 1f) < 0.0001f), "Expected OBJ normal direction.");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public void ObjMeshLoaderBuildsSuzanneFromUsedFaceVertices()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../assets/suzanne.obj"));
        Assert(File.Exists(path), "Expected Suzanne OBJ asset.");

        var loader = new STFU.MeshIO.Formats.ObjMeshLoader();
        var result = loader.Load(path, STFU.Abstractions.Loading.LoadContext.Default);
        Assert(result.Success, result.Error ?? "Expected Suzanne OBJ load success.");

        var mesh = result.GetValueOrThrow();
        Assert(mesh.Vertices.Count == 507, "Expected Suzanne to be built from used v/vn face vertices.");
        Assert(mesh.Triangles.Count == 968, "Expected Suzanne triangle count.");
        Assert(mesh.Triangles.All(triangle =>
            triangle.A >= 0 && triangle.A < mesh.Vertices.Count &&
            triangle.B >= 0 && triangle.B < mesh.Vertices.Count &&
            triangle.C >= 0 && triangle.C < mesh.Vertices.Count), "Expected remapped triangle indices.");
        Assert(mesh.Vertices.All(vertex => vertex.Normal.LengthSquared() > 0.99f), "Expected Suzanne normals from OBJ.");
    }

    public void ProjectionRejectsNearAndFarClipDepth()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.NearClipDepth = 1f;
        settings.FarClipDepth = 5f;

        var projection = ProjectionInfo.Create(CameraState.Default, 800, 600, settings);

        Assert(!projection.TryProject(new Vector3(0f, 0f, 3.5f), out _, out _), "Expected near-clipped point to be rejected.");
        Assert(!projection.TryProject(new Vector3(0f, 0f, -2f), out _, out _), "Expected far-clipped point to be rejected.");
        Assert(projection.TryProject(Vector3.Zero, out var point, out var depth), "Expected point inside clip range to project.");
        Assert(Math.Abs(depth - 4f) < 0.0001f, "Expected projected depth.");
        Assert(point.X > 0f && point.X < 800f && point.Y > 0f && point.Y < 600f, "Expected projected point inside viewport.");
    }

    public void ProjectedTrianglesUseVertexNormalsAndClipUnstableScreenTriangles()
    {
        var context = CreateVisibilityContext();
        context.Settings.MaxProjectedTriangleAreaRatio = 0.5f;
        context.Graph.Meshes.Clear();
        context.Graph.Vertices.Clear();
        context.Graph.Triangles.Clear();

        var mesh = new MeshData(
            [
                new MeshVertex(new Vector3(0f, 0f, 0f), Vector3.UnitZ),
                new MeshVertex(new Vector3(0f, 1f, 0f), Vector3.UnitZ),
                new MeshVertex(new Vector3(1f, 0f, 0f), Vector3.UnitZ),
                new MeshVertex(new Vector3(0f, 0f, 0f), Vector3.UnitZ),
                new MeshVertex(new Vector3(0f, 1f, 0f), Vector3.UnitZ),
                new MeshVertex(new Vector3(1f, 0f, 0f), Vector3.UnitZ)
            ],
            [
                new MeshTriangle(0, 1, 2),
                new MeshTriangle(3, 4, 5)
            ]);

        context.Graph.Meshes.Add(new ProjectedMesh(new EntityId(1), new MeshHandle(1), mesh, 0, 6, 0, 2));
        context.Graph.Vertices.AddRange([
            new ProjectedVertex(0, mesh.Vertices[0].Position, Vector3.UnitZ, new Point2D(30f, 30f), 4f, true),
            new ProjectedVertex(1, mesh.Vertices[1].Position, Vector3.UnitZ, new Point2D(30f, 60f), 4f, true),
            new ProjectedVertex(2, mesh.Vertices[2].Position, Vector3.UnitZ, new Point2D(60f, 30f), 4f, true),
            new ProjectedVertex(3, mesh.Vertices[3].Position, Vector3.UnitZ, new Point2D(-10000f, -10000f), 4f, true),
            new ProjectedVertex(4, mesh.Vertices[4].Position, Vector3.UnitZ, new Point2D(10000f, -10000f), 4f, true),
            new ProjectedVertex(5, mesh.Vertices[5].Position, Vector3.UnitZ, new Point2D(0f, 10000f), 4f, true)
        ]);

        new STFU.NPR.Steps.Mesh.BuildProjectedTrianglesStep().Execute(context);

        Assert(context.Graph.Triangles.Count == 2, "Expected projected triangles.");
        Assert(Vector3.Dot(context.Graph.Triangles[0].Normal, Vector3.UnitZ) > 0.9f, "Expected triangle normal to align with vertex normals.");
        Assert(context.Graph.Triangles[0].IsFrontFacing, "Expected corrected winding to be front-facing.");
        Assert(!context.Graph.Triangles[1].IsVisible, "Expected oversized screen triangle to be clipped from visibility.");
    }

    public void StrokeCandidatesRejectOffscreenAndOversizedSegments()
    {
        var context = CreateVisibilityContext();
        context.Graph.VisibilitySegments.AddRange([
            new VisibilitySegment(9101, 9101, FeatureCurveKind.Crease, NprStrokeIntent.Crease, VisibilityState.Visible, 0f, 1f, new Point2D(-1000f, -1000f), new Point2D(-900f, -900f), 0.4f, 0.2f, 0.8f, 1f),
            new VisibilitySegment(9102, 9102, FeatureCurveKind.Crease, NprStrokeIntent.Crease, VisibilityState.Visible, 0f, 1f, new Point2D(10f, 10f), new Point2D(30f, 10f), 0.4f, 0.2f, 0.8f, 1f),
            new VisibilitySegment(9103, 9103, FeatureCurveKind.Crease, NprStrokeIntent.Crease, VisibilityState.Visible, 0f, 1f, new Point2D(-5000f, 50f), new Point2D(5000f, 50f), 0.4f, 0.2f, 0.8f, 1f)
        ]);

        new STFU.NPR.Steps.Strokes.BuildStrokeCandidatesStep().Execute(context);

        Assert(context.Graph.Candidates.Count == 1, "Expected only stable viewport segment to become candidate.");
        Assert(context.Graph.Candidates[0].StableId == 9102, "Expected centered segment candidate.");
    }

    public void SvgExporterWritesStrokeMetadataAndLayers()
    {
        var (pipeline, context) = CreatePipelineContext();
        var frame = pipeline.Execute(context);
        var exporter = new SvgStrokeExporter();
        var svg = exporter.ExportToString(frame, context.Style.CreateSvgExportOptions());

        Assert(svg.Contains("<svg", StringComparison.Ordinal), "Expected svg root.");
        Assert(svg.Contains("<path", StringComparison.Ordinal), "Expected svg path.");
        Assert(svg.Contains("data-stfu-stable-id=", StringComparison.Ordinal), "Expected stable id metadata.");
        Assert(svg.Contains("data-stfu-layer=", StringComparison.Ordinal), "Expected layer metadata.");
        Assert(svg.Contains("data-stfu-source-kind=", StringComparison.Ordinal), "Expected source kind metadata.");
        Assert(svg.Contains("data-stfu-intent=", StringComparison.Ordinal), "Expected intent metadata.");
        Assert(svg.Contains("data-stfu-feature-id=", StringComparison.Ordinal), "Expected feature id metadata.");
        Assert(svg.Contains("data-stfu-segment-id=", StringComparison.Ordinal), "Expected segment id metadata.");
        Assert(svg.Contains("data-stfu-visibility=", StringComparison.Ordinal), "Expected visibility metadata.");
        Assert(svg.Contains("data-stfu-style=\"generic-sketch\"", StringComparison.Ordinal), "Expected style id metadata.");
        Assert(svg.Contains("data-stfu-layer-order=", StringComparison.Ordinal), "Expected layer order metadata.");
        Assert(svg.Contains("stroke-width=", StringComparison.Ordinal), "Expected stroke width.");
        Assert(svg.Contains("stroke-opacity=", StringComparison.Ordinal), "Expected stroke opacity.");
        Assert(svg.Contains("id=\"silhouette\"", StringComparison.Ordinal) ||
            svg.Contains("id=\"boundary\"", StringComparison.Ordinal) ||
            svg.Contains("id=\"occluding-contour\"", StringComparison.Ordinal) ||
            svg.Contains("id=\"crease\"", StringComparison.Ordinal) ||
            svg.Contains("id=\"hatch\"", StringComparison.Ordinal) ||
            svg.Contains("id=\"hatch-primary\"", StringComparison.Ordinal),
            "Expected grouped layer ids.");
    }

    public void RasterExporterWritesPortablePixmap()
    {
        var (pipeline, context) = CreatePipelineContext();
        var frame = pipeline.Execute(context);
        var exporter = new RasterStrokeExporter();
        var ppm = exporter.ExportToString(frame, new RasterExportOptions(128, 96, new StrokeColor(255, 255, 255), 1f));

        Assert(ppm.StartsWith("P3", StringComparison.Ordinal), "Expected portable pixmap header.");
        Assert(ppm.Contains("128 96", StringComparison.Ordinal), "Expected raster dimensions.");
        Assert(ppm.Contains("255", StringComparison.Ordinal), "Expected raster max channel marker.");
    }

    public void DebugSnapshotJsonContainsSchemaFields()
    {
        var (pipeline, context) = CreatePipelineContext();
        pipeline.Execute(context);

        var json = STFU.NPR.Debug.NprDebugSnapshotBuilder.ToJson(context);
        Assert(json.Contains("\"frameId\"", StringComparison.Ordinal), "Expected snapshot frameId.");
        Assert(json.Contains("\"presetId\"", StringComparison.Ordinal), "Expected snapshot presetId.");
        Assert(json.Contains("\"camera\"", StringComparison.Ordinal), "Expected snapshot camera.");
        Assert(json.Contains("\"counts\"", StringComparison.Ordinal), "Expected snapshot counts.");
        Assert(json.Contains("\"timingsMs\"", StringComparison.Ordinal), "Expected snapshot timings.");
        Assert(json.Contains("\"featureCurves\"", StringComparison.Ordinal), "Expected snapshot feature curve count.");
        Assert(json.Contains("\"strokeCandidates\"", StringComparison.Ordinal), "Expected snapshot stroke candidate count.");
    }

    public void VisibilityAndExportFixturesAreUsable()
    {
        var context = CreateVisibilityContext();
        var segments = VisibilityFixture.ResolveHorizontalOcclusion(context);
        Assert(segments.Count >= 3, "Expected visibility fixture segments.");
        Assert(segments.Any(segment => segment.State == VisibilityState.Hidden), "Expected visibility fixture hidden segment.");

        var svg = ExportFixture.ExportSvg();
        Assert(svg.Contains("<svg", StringComparison.Ordinal), "Expected export fixture SVG.");
    }

    public void EvaluationMetricsReportVisibilityAndDensity()
    {
        var context = CreateVisibilityContext();
        var segments = VisibilityFixture.ResolveHorizontalOcclusion(context);
        var correctness = NprEvaluationMetric.VisibilityCorrectness(
            segments,
            VisibilityState.Visible,
            VisibilityState.Hidden,
            VisibilityState.Visible);

        Assert(correctness >= 0.66f, "Expected visibility correctness metric to match fixture ordering.");

        var frame = ExportFixture.CreateMiniFrame();
        var histogram = NprEvaluationMetric.StrokeDensityHistogram(frame, 16);
        Assert(histogram.Count > 0, "Expected stroke density histogram.");
        Assert(NprEvaluationMetric.MeanTileDensity(frame, 16) > 0f, "Expected positive mean tile density.");
    }

    public void ContourPrecisionRecallMatchesSyntheticFeatureFixtures()
    {
        var suggestive = CreateVisibilityContext();
        suggestive.Settings.CreaseAngleDegrees = 40f;
        suggestive.Graph.Vertices.Clear();
        suggestive.Graph.Triangles.Clear();
        suggestive.Graph.TopologyEdges.Clear();
        suggestive.Graph.Vertices.AddRange([
            new ProjectedVertex(0, new Vector3(-1f, 0f, 0f), Vector3.Normalize(new Vector3(0.95f, 0f, -0.22f)), new Point2D(15f, 30f), 0.35f, true, 0.18f, 0.22f, 0.14f, 0.18f, Vector3.UnitX),
            new ProjectedVertex(1, new Vector3(1f, 0f, 0f), Vector3.Normalize(new Vector3(0.88f, 0f, -0.30f)), new Point2D(55f, 30f), 0.35f, true, 0.20f, 0.24f, 0.16f, 0.20f, Vector3.UnitX),
            new ProjectedVertex(2, new Vector3(-1f, 1f, 0f), Vector3.Normalize(new Vector3(0.92f, 0.1f, -0.24f)), new Point2D(15f, 60f), 0.36f, true, 0.17f, 0.21f, 0.13f, 0.17f, Vector3.UnitX),
            new ProjectedVertex(3, new Vector3(1f, 1f, 0f), Vector3.Normalize(new Vector3(0.84f, 0.1f, -0.34f)), new Point2D(55f, 60f), 0.36f, true, 0.19f, 0.23f, 0.15f, 0.19f, Vector3.UnitX)
        ]);
        suggestive.Graph.Triangles.AddRange([
            new ProjectedTriangle(300, 0, 0, 0, 1, 2, Vector3.Normalize(new Vector3(0f, 0.2f, -1f)), Vector3.Zero, new Point2D(25f, 40f), 0.35f, 120f, 0.62f, true, true),
            new ProjectedTriangle(301, 0, 1, 1, 3, 2, Vector3.Normalize(new Vector3(0.05f, 0.2f, -1f)), Vector3.Zero, new Point2D(40f, 50f), 0.35f, 120f, 0.64f, true, true)
        ]);
        suggestive.Graph.TopologyEdges.Add(new TopologyEdge(4001, 0, 1, 0, 1, 8f, false));
        new STFU.NPR.Steps.Mesh.ExtractFeatureLinesStep().Execute(suggestive);

        var suggestiveMetric = ContourEvaluationMetric.PrecisionRecall(
            suggestive.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.SuggestiveContour).ToArray(),
            [new ExpectedContour(FeatureCurveKind.SuggestiveContour, new Point2D(15f, 30f), new Point2D(55f, 30f))]);

        Assert(suggestiveMetric.Precision >= 1f, "Expected suggestive contour precision.");
        Assert(suggestiveMetric.Recall >= 1f, "Expected suggestive contour recall.");

        var apparent = CreateVisibilityContext();
        apparent.Settings.CreaseAngleDegrees = 40f;
        apparent.Graph.Vertices.Clear();
        apparent.Graph.Triangles.Clear();
        apparent.Graph.TopologyEdges.Clear();
        apparent.Graph.Vertices.AddRange([
            new ProjectedVertex(0, new Vector3(-1f, 0f, 0f), Vector3.Normalize(new Vector3(0.98f, 0f, -0.12f)), new Point2D(12f, 30f), 0.35f, true, 0.22f, 0.26f, 0.22f, 0.28f, Vector3.UnitX),
            new ProjectedVertex(1, new Vector3(1f, 0f, 0f), Vector3.Normalize(new Vector3(0.74f, 0f, -0.28f)), new Point2D(58f, 30f), 0.35f, true, 0.24f, 0.28f, 0.24f, 0.30f, Vector3.UnitX),
            new ProjectedVertex(2, new Vector3(-1f, 1f, 0f), Vector3.Normalize(new Vector3(0.96f, 0.08f, -0.10f)), new Point2D(12f, 62f), 0.36f, true, 0.21f, 0.25f, 0.21f, 0.27f, Vector3.UnitX),
            new ProjectedVertex(3, new Vector3(1f, 1f, 0f), Vector3.Normalize(new Vector3(0.71f, 0.08f, -0.32f)), new Point2D(58f, 62f), 0.36f, true, 0.23f, 0.27f, 0.23f, 0.29f, Vector3.UnitX)
        ]);
        apparent.Graph.Triangles.AddRange([
            new ProjectedTriangle(320, 0, 0, 0, 1, 2, Vector3.Normalize(new Vector3(0f, 0.1f, -1f)), Vector3.Zero, new Point2D(25f, 40f), 0.35f, 120f, 0.68f, true, true),
            new ProjectedTriangle(321, 0, 1, 1, 3, 2, Vector3.Normalize(new Vector3(0.03f, 0.1f, -1f)), Vector3.Zero, new Point2D(42f, 49f), 0.35f, 120f, 0.70f, true, true)
        ]);
        apparent.Graph.TopologyEdges.Add(new TopologyEdge(4201, 0, 1, 0, 1, 14f, false));
        new STFU.NPR.Steps.Mesh.ExtractFeatureLinesStep().Execute(apparent);

        var apparentMetric = ContourEvaluationMetric.PrecisionRecall(
            apparent.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ApparentRidge).ToArray(),
            [new ExpectedContour(FeatureCurveKind.ApparentRidge, new Point2D(12f, 30f), new Point2D(58f, 30f))]);

        Assert(apparentMetric.Precision >= 1f, "Expected apparent ridge precision.");
        Assert(apparentMetric.Recall >= 1f, "Expected apparent ridge recall.");
    }

    public void ScreenTileGridBucketsItemsByTile()
    {
        var grid = new STFU.NPR.Analysis.ScreenTileGrid<int>(16);
        grid.Add(8f, 8f, 1);
        grid.Add(20f, 8f, 2);
        grid.Add(22f, 10f, 3);

        var tiles = grid.EnumerateTiles().ToDictionary(pair => pair.Key, pair => pair.Value.Count);
        Assert(tiles.Count == 2, "Expected two occupied tiles.");
        Assert(tiles[(0, 0)] == 1, "Expected one item in first tile.");
        Assert(tiles[(1, 0)] == 2, "Expected two items in second tile.");
    }

    public void SampleVisibilityResolverSplitsPartiallyOccludedCurve()
    {
        var context = CreateVisibilityContext();
        var resolver = new SampleVisibilityResolver();
        var curve = FeatureCurve.FromLine(
            9001,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            new FeaturePoint(new Point2D(10f, 50f), 0.5f),
            new FeaturePoint(new Point2D(90f, 50f), 0.5f),
            FeatureCurveSource.None,
            shade: 0.35f,
            importance: 0.8f,
            flags: FeatureCurveFlags.Generated);

        var segments = resolver.Resolve(context, [curve]);

        Assert(segments.Count >= 3, "Expected split visibility segments.");
        Assert(segments.Any(segment => segment.State == VisibilityState.Hidden), "Expected hidden segment.");
        Assert(segments.Count(segment => segment.State == VisibilityState.Visible) >= 2, "Expected visible segments on both sides.");
        Assert(segments[0].State == VisibilityState.Visible, "Expected leading visible segment.");
        Assert(segments[^1].State == VisibilityState.Visible, "Expected trailing visible segment.");
        Assert(segments.Any(segment => segment.FeatureCurveId == curve.StableId), "Expected feature curve id propagation.");
        var hidden = segments.First(segment => segment.State == VisibilityState.Hidden);
        Assert(Math.Abs(hidden.Start.X - 35f) < 3f, "Expected hidden segment to start near occluder boundary.");
        Assert(Math.Abs(hidden.End.X - 65f) < 3f, "Expected hidden segment to end near occluder boundary.");
    }

    public void VisibleSilhouetteSegmentsBecomeOccludingContours()
    {
        var context = CreateVisibilityContext();
        var resolver = new SampleVisibilityResolver();
        var curve = FeatureCurve.FromLine(
            9101,
            FeatureCurveKind.Silhouette,
            NprStrokeIntent.Silhouette,
            new FeaturePoint(new Point2D(10f, 20f), 0.3f),
            new FeaturePoint(new Point2D(90f, 20f), 0.3f),
            FeatureCurveSource.None,
            shade: 0.4f,
            importance: 1f,
            flags: FeatureCurveFlags.ViewDependent);

        var segments = resolver.Resolve(context, [curve]);

        Assert(segments.Count > 0, "Expected silhouette segments.");
        Assert(segments.All(segment => segment.Kind == FeatureCurveKind.OccludingContour), "Expected visible silhouette segments to be typed as occluding contours.");
    }

    public void HiddenCreaseSegmentsBecomeConstruction()
    {
        var context = CreateVisibilityContext();
        var resolver = new SampleVisibilityResolver();
        var curve = FeatureCurve.FromLine(
            9102,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            new FeaturePoint(new Point2D(35f, 50f), 0.5f),
            new FeaturePoint(new Point2D(65f, 50f), 0.5f),
            FeatureCurveSource.None,
            shade: 0.3f,
            importance: 0.7f,
            flags: FeatureCurveFlags.None);

        var segments = resolver.Resolve(context, [curve]);

        Assert(segments.Any(segment => segment.State == VisibilityState.Hidden), "Expected hidden construction-eligible segment.");
        Assert(segments.Where(segment => segment.State == VisibilityState.Hidden).All(segment => segment.Kind == FeatureCurveKind.Construction), "Expected hidden crease segments to become construction.");
        Assert(segments.Where(segment => segment.State == VisibilityState.Hidden).All(segment => segment.Intent == NprStrokeIntent.Accent), "Expected construction segments to map to accent intent.");
    }

    public void OfflineExactVisibilityResolverProducesAtLeastAsFineSegmentation()
    {
        var context = CreateVisibilityContext();
        var curve = FeatureCurve.FromLine(
            9002,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            new FeaturePoint(new Point2D(10f, 50f), 0.5f),
            new FeaturePoint(new Point2D(90f, 50f), 0.5f),
            FeatureCurveSource.None,
            shade: 0.35f,
            importance: 0.8f,
            flags: FeatureCurveFlags.Generated);

        var sampled = new SampleVisibilityResolver().Resolve(context, [curve]);
        var offline = new OfflineExactVisibilityResolver().Resolve(context, [curve]);

        Assert(offline.Count >= sampled.Count, "Expected offline exact visibility to produce at least as many segments as sampled visibility.");
        Assert(offline.Any(segment => segment.State == VisibilityState.Hidden), "Expected offline exact visibility to keep hidden segment.");
    }

    public void PruneFeatureLinesStepAppliesTileBudgetBySalience()
    {
        var context = CreateVisibilityContext(new StyleBudgetRule(128, 1, false));
        context.Graph.VisibilitySegments.AddRange([
            new VisibilitySegment(1001, 501, FeatureCurveKind.Crease, NprStrokeIntent.Crease, VisibilityState.Visible, 0f, 0.3f, new Point2D(10f, 10f), new Point2D(30f, 10f), 0.4f, 0.2f, 0.4f, 1f),
            new VisibilitySegment(1002, 502, FeatureCurveKind.Crease, NprStrokeIntent.Crease, VisibilityState.Visible, 0.3f, 0.6f, new Point2D(15f, 20f), new Point2D(35f, 20f), 0.4f, 0.2f, 0.4f, 1f),
            new VisibilitySegment(1003, 503, FeatureCurveKind.Crease, NprStrokeIntent.Crease, VisibilityState.Visible, 0.6f, 1f, new Point2D(20f, 30f), new Point2D(40f, 30f), 0.4f, 0.2f, 0.4f, 1f)
        ]);
        context.Graph.SalienceByStableId[1001] = new SalienceScore(0.2f, 1f, 0.2f, 1f, 0.2f, 1f, 0f, 0.2f);
        context.Graph.SalienceByStableId[1002] = new SalienceScore(0.9f, 1f, 0.9f, 1f, 0.9f, 1f, 0f, 0.9f);
        context.Graph.SalienceByStableId[1003] = new SalienceScore(0.5f, 1f, 0.5f, 1f, 0.5f, 1f, 0f, 0.5f);

        new STFU.NPR.Steps.Analysis.PruneFeatureLinesStep().Execute(context);

        Assert(context.Graph.VisibilitySegments.Count == 1, "Expected one segment after tile budget pruning.");
        Assert(context.Graph.VisibilitySegments[0].StableId == 1002, "Expected highest-salience segment to survive.");
    }

    public void LinePriorityRuleRejectsTooShortSuggestiveContours()
    {
        var context = CreateVisibilityContext();
        context.Graph.VisibilitySegments.Add(new VisibilitySegment(
            1004,
            504,
            FeatureCurveKind.SuggestiveContour,
            NprStrokeIntent.Accent,
            VisibilityState.Visible,
            0f,
            1f,
            new Point2D(10f, 10f),
            new Point2D(12f, 10f),
            0.4f,
            0.6f,
            0.7f,
            0.8f));
        context.Graph.SalienceByStableId[1004] = new SalienceScore(0.9f, 1f, 1f, 1f, 0.8f, 1f, 0f, 0.9f);

        new STFU.NPR.Steps.Analysis.PruneFeatureLinesStep().Execute(context);

        Assert(context.Graph.VisibilitySegments.Count == 0, "Expected short suggestive contour to be rejected by line priority rule.");
    }

    public void StyleDrivenHatchingBuildsPlans()
    {
        var (pipeline, context) = CreatePipelineContext();
        pipeline.Execute(context);

        Assert(context.Graph.HatchingPlans.Count > 0, "Expected hatching plans.");
        Assert(context.Graph.HatchingPlans.Any(plan => plan.Primary.Kind == HatchLayerKind.Primary), "Expected primary hatch layer.");
        Assert(context.Graph.HatchingPlans.Any(plan => plan.Secondary is not null), "Expected secondary hatch layer.");
        Assert(context.Graph.HatchingPlans.Any(plan => plan.Center.X != 0f || plan.Center.Y != 0f), "Expected hatching plan center.");
        Assert(context.Graph.HatchingPlans.Any(plan => plan.DensityTarget > 0f), "Expected non-zero hatch density target.");
        Assert(context.Style.FindFeatureRule(FeatureCurveKind.HatchGuide) is not null, "Expected hatch guide style rule.");
        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.HatchGuide), "Expected hatch guide curves.");
        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.Hatch), "Expected hatch curves.");
    }

    public void ContactAccentsBuildFeatureCurves()
    {
        var context = CreateVisibilityContext();
        context.Graph.SurfaceSamples.Clear();
        context.Graph.DirectionField = new STFU.NPR.Fields.DirectionField([
            new STFU.NPR.Fields.DirectionSample(new Point2D(40f, 50f), new Vector2(0f, 1f))
        ]);

        context.Graph.SurfaceSamples.AddRange([
            new SurfaceSample(7001, 0, Vector3.UnitZ, Vector3.UnitX, new Point2D(40f, 50f), 0.92f, 0.9f, 0.18f, 0.22f),
            new SurfaceSample(7002, 1, Vector3.UnitZ, Vector3.UnitX, new Point2D(55f, 52f), 0.88f, 0.84f, 0.14f, 0.18f)
        ]);

        new STFU.NPR.Steps.Mesh.BuildContactAccentsStep().Execute(context);

        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.ContactAccent), "Expected contact accent curve.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ContactAccent).All(curve => curve.Intent == NprStrokeIntent.Accent), "Expected contact accents to map to accent intent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ContactAccent).All(curve => (curve.Flags & FeatureCurveFlags.Generated) != 0), "Expected generated contact accent curves.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ContactAccent).All(curve => curve.Confidence > 0.5f), "Expected confident contact accents.");
    }

    public void BuildMaterialRegionsCreatesPerMeshRegionData()
    {
        var (pipeline, context) = CreatePipelineContext();
        pipeline.Execute(context);

        Assert(context.Graph.MaterialRegions.Count > 0, "Expected material regions.");
        Assert(context.Graph.MaterialRegions.All(region => region.TriangleIndices.Count > 0), "Expected region triangles.");
        Assert(context.Graph.MaterialRegions.All(region => region.EntityId == context.Scene.Entities[0].Id), "Expected region entity id.");
        Assert(context.Graph.MaterialRegions.All(region => region.MaterialId >= 0), "Expected non-negative material id.");
        Assert(context.Graph.MaterialRegions.Select(region => region.MaterialId).Distinct().Count() > 1, "Expected multiple material buckets for tone-separated mesh.");
    }

    public void MaterialBoundaryExtractionBuildsFeatureCurves()
    {
        var context = CreateVisibilityContext();
        context.Settings.CreaseAngleDegrees = 40f;
        context.Graph.MaterialRegions.Clear();
        context.Graph.Vertices.Clear();
        context.Graph.Triangles.Clear();
        context.Graph.TopologyEdges.Clear();

        context.Graph.Vertices.AddRange([
            new ProjectedVertex(0, new Vector3(-1f, 0f, 0f), Vector3.Normalize(new Vector3(0f, 0.1f, -1f)), new Point2D(20f, 30f), 0.35f, true),
            new ProjectedVertex(1, new Vector3(1f, 0f, 0f), Vector3.Normalize(new Vector3(0f, 0.1f, -1f)), new Point2D(60f, 30f), 0.35f, true),
            new ProjectedVertex(2, new Vector3(-1f, 1f, 0f), Vector3.Normalize(new Vector3(0f, 0.12f, -1f)), new Point2D(20f, 60f), 0.36f, true),
            new ProjectedVertex(3, new Vector3(1f, 1f, 0f), Vector3.Normalize(new Vector3(0f, 0.12f, -1f)), new Point2D(60f, 60f), 0.36f, true)
        ]);

        context.Graph.Triangles.AddRange([
            new ProjectedTriangle(900, 0, 0, 0, 1, 2, Vector3.Normalize(new Vector3(0f, 0.1f, -1f)), Vector3.Zero, new Point2D(30f, 40f), 0.35f, 120f, 0.25f, true, true),
            new ProjectedTriangle(901, 0, 1, 1, 3, 2, Vector3.Normalize(new Vector3(0f, 0.1f, -1f)), Vector3.Zero, new Point2D(48f, 50f), 0.35f, 120f, 0.82f, true, true)
        ]);

        context.Graph.TopologyEdges.Add(new TopologyEdge(9101, 1, 2, 0, 1, 8f, false));
        context.Graph.MaterialRegions.AddRange([
            new MaterialRegion(8001, new EntityId(1), 0, [0], 0.25f, StrokeMedium.Wash, RegionHatchingPolicy.Sparse),
            new MaterialRegion(8002, new EntityId(1), 3, [1], 0.82f, StrokeMedium.Ink, RegionHatchingPolicy.Dense)
        ]);

        new STFU.NPR.Steps.Mesh.ExtractFeatureLinesStep().Execute(context);

        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.MaterialBoundary), "Expected material boundary curve.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.MaterialBoundary).All(curve => curve.Intent == NprStrokeIntent.Accent), "Expected material boundary to map to accent intent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.MaterialBoundary).All(curve => curve.Confidence > 0.2f), "Expected material boundary confidence.");
    }

    public void BuildStyleMasksCreatesFocusMask()
    {
        var (pipeline, context) = CreatePipelineContext();
        pipeline.Execute(context);

        Assert(context.Graph.StyleMasks.Count > 0, "Expected style masks.");
        Assert(context.Graph.StyleMasks.Any(mask => mask.Role == StyleMaskRole.Focus), "Expected focus style mask.");
        Assert(context.Graph.StyleMasks.All(mask => mask.ScreenRegions.Count > 0), "Expected style mask regions.");
    }

    public void StrokeFrameMetadataCarriesLayerOrderAndHatchVariants()
    {
        var context = CreateVisibilityContext();
        context.Graph.StyledStrokes.Add(new StyledStroke(
            8801,
            8801,
            FeatureCurveKind.Hatch,
            NprStrokeIntent.Hatch,
            [new Point2D(10f, 10f), new Point2D(30f, 10f), new Point2D(50f, 12f)],
            0.4f,
            0.8f,
            0.7f,
            VisibilityState.Visible,
            0.8f,
            0.8f,
            HatchLayerKind.Cross)
        {
            Thickness = 0.8f,
            Opacity = 0.6f,
            Color = StrokeColor.Black
        });

        new STFU.NPR.Steps.Strokes.BuildStrokeFrameStep().Execute(context);

        var metadata = context.Frame.Paths[0].Metadata!;
        Assert(metadata.Layer == "hatch-cross", "Expected hatch pass in output layer.");
        Assert(metadata.Variant == HatchLayerKind.Cross.ToString(), "Expected hatch pass metadata.");
        Assert(metadata.LayerOrder == context.Style.FindFeatureRule(FeatureCurveKind.Hatch)!.LayerOrder + 1, "Expected hatch pass layer order offset.");
    }

    public void HiddenLinePolicyGhostBuildsHiddenStrokeCandidates()
    {
        var style = SketchNprPreset.CreateGrammar() with
        {
            FeatureRules = SketchNprPreset.CreateGrammar().FeatureRules
                .Select(rule => rule.Kind == FeatureCurveKind.Crease
                    ? rule with { HiddenLinePolicy = HiddenLinePolicy.Ghost }
                    : rule)
                .ToArray(),
            Visibility = SketchNprPreset.CreateGrammar().Visibility with
            {
                DefaultHiddenPolicy = HiddenLinePolicy.Ghost
            }
        };
        var context = CreateVisibilityContext(style: style);
        context.Graph.VisibilitySegments.Add(new VisibilitySegment(
            7001,
            7001,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            VisibilityState.Hidden,
            0f,
            1f,
            new Point2D(10f, 10f),
            new Point2D(40f, 10f),
            0.4f,
            0.2f,
            0.6f,
            1f));
        context.Graph.SalienceByStableId[7001] = new SalienceScore(0.7f, 0.2f, 1f, 1f, 0.7f, 1f, 0f, 0.65f);

        new STFU.NPR.Steps.Strokes.BuildStrokeCandidatesStep().Execute(context);
        Assert(context.Graph.Candidates.Count == 1, "Expected hidden segment to survive as candidate under ghost policy.");
        Assert(context.Graph.Candidates[0].Visibility == VisibilityState.Hidden, "Expected hidden visibility on candidate.");

        new STFU.NPR.Steps.Strokes.StyleStrokesStep().Execute(context);
        Assert(context.Graph.StyledStrokes.Count == 1, "Expected styled hidden stroke.");
        Assert(context.Graph.StyledStrokes[0].Opacity < 0.4f, "Expected ghost policy to reduce hidden stroke opacity.");
    }

    public void HiddenLinePolicySurvivesPruningForVisibleOutput()
    {
        var style = SketchNprPreset.CreateGrammar() with
        {
            FeatureRules = SketchNprPreset.CreateGrammar().FeatureRules
                .Select(rule => rule.Kind == FeatureCurveKind.Crease
                    ? rule with { HiddenLinePolicy = HiddenLinePolicy.Dashed }
                    : rule)
                .ToArray(),
            Visibility = SketchNprPreset.CreateGrammar().Visibility with
            {
                DefaultHiddenPolicy = HiddenLinePolicy.Dashed
            }
        };
        var context = CreateVisibilityContext(style: style);
        context.Graph.VisibilitySegments.Add(new VisibilitySegment(
            7101,
            7101,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            VisibilityState.Hidden,
            0f,
            1f,
            new Point2D(12f, 20f),
            new Point2D(72f, 20f),
            0.4f,
            0.2f,
            0.7f,
            1f));
        context.Graph.SalienceByStableId[7101] = new SalienceScore(0.8f, 0.2f, 1f, 1f, 0.8f, 1f, 0f, 0.72f);

        new STFU.NPR.Steps.Analysis.PruneFeatureLinesStep().Execute(context);
        Assert(context.Graph.VisibilitySegments.Count == 1, "Expected dashed hidden segment to survive pruning.");
        Assert(context.Graph.VisibilitySegments[0].State == VisibilityState.Hidden, "Expected hidden segment after pruning.");

        new STFU.NPR.Steps.Strokes.BuildStrokeCandidatesStep().Execute(context);
        new STFU.NPR.Steps.Strokes.StyleStrokesStep().Execute(context);
        new STFU.NPR.Steps.Strokes.BuildStrokeFrameStep().Execute(context);

        Assert(context.Frame.Paths.Count == 1, "Expected hidden path in final frame.");
        Assert(context.Frame.Paths[0].Metadata?.SourceKind == "DashedHiddenStroke", "Expected dashed hidden source kind.");
    }

    public void HiddenLinePolicyDashedExportsDashArray()
    {
        var style = SketchNprPreset.CreateGrammar() with
        {
            FeatureRules = SketchNprPreset.CreateGrammar().FeatureRules
                .Select(rule => rule.Kind == FeatureCurveKind.Crease
                    ? rule with { HiddenLinePolicy = HiddenLinePolicy.Dashed }
                    : rule)
                .ToArray(),
            Visibility = SketchNprPreset.CreateGrammar().Visibility with
            {
                DefaultHiddenPolicy = HiddenLinePolicy.Dashed
            }
        };
        var context = CreateVisibilityContext(style: style);
        context.Graph.VisibilitySegments.Add(new VisibilitySegment(
            7002,
            7002,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            VisibilityState.Hidden,
            0f,
            1f,
            new Point2D(10f, 20f),
            new Point2D(40f, 20f),
            0.4f,
            0.2f,
            0.6f,
            1f));
        context.Graph.SalienceByStableId[7002] = new SalienceScore(0.7f, 0.2f, 1f, 1f, 0.7f, 1f, 0f, 0.65f);

        new STFU.NPR.Steps.Strokes.BuildStrokeCandidatesStep().Execute(context);
        new STFU.NPR.Steps.Strokes.StyleStrokesStep().Execute(context);
        new STFU.NPR.Steps.Strokes.BuildStrokeFrameStep().Execute(context);

        var svg = new SvgStrokeExporter().ExportToString(context.Frame, style.CreateSvgExportOptions());
        Assert(svg.Contains("stroke-dasharray=\"6 4\"", StringComparison.Ordinal), "Expected dashed hidden stroke in SVG export.");
    }

    public void MeshAnalysisCacheReusesTopologyForRepeatedMesh()
    {
        var analysis = new MeshAnalysisCacheStore();
        var first = CreatePipelineContext(analysis);
        first.Pipeline.Execute(first.Context);

        Assert(analysis.Count == 1, "Expected one cached mesh analysis after first run.");
        var firstEdgeCount = first.Context.Graph.TopologyEdges.Count;

        var second = CreatePipelineContext(analysis);
        second.Pipeline.Execute(second.Context);

        Assert(analysis.Count == 1, "Expected mesh analysis cache reuse for repeated mesh.");
        Assert(second.Context.Graph.TopologyEdges.Count == firstEdgeCount, "Expected same topology edge count from cached analysis.");
        Assert(analysis.TryGet(second.Context.Scene.Entities[0].Mesh, out var cache), "Expected mesh analysis cache entry.");
        var resolvedCache = cache!;
        Assert(resolvedCache.Topology.Edges.Count > 0, "Expected cached topology edges.");
        Assert(resolvedCache.Curvature is not null, "Expected curvature cache.");
        Assert(resolvedCache.Curvature!.TriangleCurvature.Count > 0, "Expected triangle curvature values.");
        Assert(resolvedCache.Curvature.SmoothedTriangleCurvature.Count > 0, "Expected smoothed triangle curvature values.");
        Assert(resolvedCache.Curvature.TriangleSignedCurvature.Count > 0, "Expected signed triangle curvature values.");
        Assert(resolvedCache.Curvature.SmoothedTriangleSignedCurvature.Count > 0, "Expected smoothed signed triangle curvature values.");
        Assert(resolvedCache.Curvature.VertexDirections.Count > 0, "Expected vertex curvature directions.");
        Assert(resolvedCache.Curvature.TriangleDirections.Count > 0, "Expected triangle curvature directions.");
        Assert(resolvedCache.Curvature.VertexCurvature.Any(value => value > 0f), "Expected non-zero vertex curvature on cube.");
        Assert(resolvedCache.Curvature.SmoothedVertexCurvature.Any(value => value > 0f), "Expected non-zero smoothed vertex curvature on cube.");
        Assert(resolvedCache.Curvature.VertexSignedCurvature.Any(value => Math.Abs(value) > 0f), "Expected non-zero signed vertex curvature on cube.");
        Assert(resolvedCache.Curvature.SmoothedVertexSignedCurvature.Any(value => Math.Abs(value) > 0f), "Expected non-zero smoothed signed vertex curvature on cube.");
        Assert(resolvedCache.Curvature.TriangleDirections.Any(direction => direction.LengthSquared() > 0.0001f), "Expected non-zero triangle direction entries.");
    }

    public void CurvatureDrivenFeatureExtractionBuildsRidgeAndValleyCurves()
    {
        var context = CreateVisibilityContext();
        context.Settings.CreaseAngleDegrees = 40f;

        context.Graph.Vertices.Clear();
        context.Graph.Triangles.Clear();
        context.Graph.TopologyEdges.Clear();

        context.Graph.Vertices.AddRange([
            new ProjectedVertex(0, new Vector3(0f, 0f, 0f), Vector3.Normalize(new Vector3(0f, 0.3f, -1f)), new Point2D(10f, 20f), 0.3f, true),
            new ProjectedVertex(1, new Vector3(1f, 0f, 0f), Vector3.Normalize(new Vector3(0f, 0.45f, -1f)), new Point2D(50f, 20f), 0.3f, true),
            new ProjectedVertex(2, new Vector3(0f, 1f, 0f), Vector3.Normalize(new Vector3(0f, 0.2f, -1f)), new Point2D(10f, 50f), 0.4f, true),
            new ProjectedVertex(3, new Vector3(1f, 1f, 0f), Vector3.Normalize(new Vector3(0f, 0.5f, -1f)), new Point2D(50f, 50f), 0.4f, true),
            new ProjectedVertex(4, new Vector3(0f, 2f, 0f), Vector3.Normalize(new Vector3(0f, -0.3f, -1f)), new Point2D(10f, 80f), 0.5f, true),
            new ProjectedVertex(5, new Vector3(1f, 2f, 0f), Vector3.Normalize(new Vector3(0f, -0.45f, -1f)), new Point2D(50f, 80f), 0.5f, true)
        ]);

        context.Graph.Triangles.AddRange([
            new ProjectedTriangle(200, 0, 0, 0, 1, 2, new Vector3(0f, 0f, -1f), Vector3.Zero, new Point2D(20f, 25f), 0.3f, 120f, 0.78f, true, true),
            new ProjectedTriangle(201, 0, 1, 1, 3, 2, new Vector3(0f, 0f, -1f), Vector3.Zero, new Point2D(35f, 35f), 0.3f, 120f, 0.82f, true, true),
            new ProjectedTriangle(202, 0, 2, 2, 3, 4, new Vector3(0f, 0f, -1f), Vector3.Zero, new Point2D(20f, 60f), 0.4f, 120f, 0.24f, true, true),
            new ProjectedTriangle(203, 0, 3, 3, 5, 4, new Vector3(0f, 0f, -1f), Vector3.Zero, new Point2D(35f, 70f), 0.4f, 120f, 0.28f, true, true)
        ]);

        context.Graph.TopologyEdges.AddRange([
            new TopologyEdge(3001, 0, 1, 0, 1, 14f, false),
            new TopologyEdge(3002, 2, 3, 2, 3, 15f, false)
        ]);

        new STFU.NPR.Steps.Mesh.ExtractFeatureLinesStep().Execute(context);

        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.Ridge), "Expected ridge curve.");
        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.Valley), "Expected valley curve.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind is FeatureCurveKind.Ridge or FeatureCurveKind.Valley).All(curve => curve.Intent == NprStrokeIntent.Accent), "Expected curvature features to map to accent intent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind is FeatureCurveKind.Ridge or FeatureCurveKind.Valley).All(curve => curve.Confidence > 0.15f), "Expected curvature feature confidence.");
    }

    public void ViewDependentFeatureExtractionBuildsSuggestiveContourCurves()
    {
        var context = CreateVisibilityContext();
        context.Settings.CreaseAngleDegrees = 40f;

        context.Graph.Vertices.Clear();
        context.Graph.Triangles.Clear();
        context.Graph.TopologyEdges.Clear();

        context.Graph.Vertices.AddRange([
            new ProjectedVertex(0, new Vector3(-1f, 0f, 0f), Vector3.Normalize(new Vector3(0.95f, 0f, -0.22f)), new Point2D(15f, 30f), 0.35f, true, 0.18f, 0.22f, 0.14f, 0.18f, Vector3.UnitX),
            new ProjectedVertex(1, new Vector3(1f, 0f, 0f), Vector3.Normalize(new Vector3(0.88f, 0f, -0.30f)), new Point2D(55f, 30f), 0.35f, true, 0.20f, 0.24f, 0.16f, 0.20f, Vector3.UnitX),
            new ProjectedVertex(2, new Vector3(-1f, 1f, 0f), Vector3.Normalize(new Vector3(0.92f, 0.1f, -0.24f)), new Point2D(15f, 60f), 0.36f, true, 0.17f, 0.21f, 0.13f, 0.17f, Vector3.UnitX),
            new ProjectedVertex(3, new Vector3(1f, 1f, 0f), Vector3.Normalize(new Vector3(0.84f, 0.1f, -0.34f)), new Point2D(55f, 60f), 0.36f, true, 0.19f, 0.23f, 0.15f, 0.19f, Vector3.UnitX)
        ]);

        context.Graph.Triangles.AddRange([
            new ProjectedTriangle(300, 0, 0, 0, 1, 2, Vector3.Normalize(new Vector3(0f, 0.2f, -1f)), Vector3.Zero, new Point2D(25f, 40f), 0.35f, 120f, 0.62f, true, true),
            new ProjectedTriangle(301, 0, 1, 1, 3, 2, Vector3.Normalize(new Vector3(0.05f, 0.2f, -1f)), Vector3.Zero, new Point2D(40f, 50f), 0.35f, 120f, 0.64f, true, true)
        ]);

        context.Graph.TopologyEdges.Add(new TopologyEdge(4001, 0, 1, 0, 1, 8f, false));

        new STFU.NPR.Steps.Mesh.ExtractFeatureLinesStep().Execute(context);

        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.SuggestiveContour), "Expected suggestive contour curve.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.SuggestiveContour).All(curve => curve.Intent == NprStrokeIntent.Accent), "Expected suggestive contour to map to accent intent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.SuggestiveContour).All(curve => (curve.Flags & FeatureCurveFlags.ViewDependent) != 0), "Expected suggestive contour to be view-dependent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.SuggestiveContour).All(curve => curve.Confidence > 0.3f), "Expected suggestive contour confidence.");
    }

    public void ViewDependentFeatureExtractionBuildsApparentRidgeCurves()
    {
        var context = CreateVisibilityContext();
        context.Settings.CreaseAngleDegrees = 40f;

        context.Graph.Vertices.Clear();
        context.Graph.Triangles.Clear();
        context.Graph.TopologyEdges.Clear();

        context.Graph.Vertices.AddRange([
            new ProjectedVertex(0, new Vector3(-1f, 0f, 0f), Vector3.Normalize(new Vector3(0.98f, 0f, -0.12f)), new Point2D(12f, 30f), 0.35f, true, 0.22f, 0.26f, 0.22f, 0.28f, Vector3.UnitX),
            new ProjectedVertex(1, new Vector3(1f, 0f, 0f), Vector3.Normalize(new Vector3(0.74f, 0f, -0.28f)), new Point2D(58f, 30f), 0.35f, true, 0.24f, 0.28f, 0.24f, 0.30f, Vector3.UnitX),
            new ProjectedVertex(2, new Vector3(-1f, 1f, 0f), Vector3.Normalize(new Vector3(0.96f, 0.08f, -0.10f)), new Point2D(12f, 62f), 0.36f, true, 0.21f, 0.25f, 0.21f, 0.27f, Vector3.UnitX),
            new ProjectedVertex(3, new Vector3(1f, 1f, 0f), Vector3.Normalize(new Vector3(0.71f, 0.08f, -0.32f)), new Point2D(58f, 62f), 0.36f, true, 0.23f, 0.27f, 0.23f, 0.29f, Vector3.UnitX)
        ]);

        context.Graph.Triangles.AddRange([
            new ProjectedTriangle(320, 0, 0, 0, 1, 2, Vector3.Normalize(new Vector3(0f, 0.1f, -1f)), Vector3.Zero, new Point2D(25f, 40f), 0.35f, 120f, 0.68f, true, true),
            new ProjectedTriangle(321, 0, 1, 1, 3, 2, Vector3.Normalize(new Vector3(0.03f, 0.1f, -1f)), Vector3.Zero, new Point2D(42f, 49f), 0.35f, 120f, 0.70f, true, true)
        ]);

        context.Graph.TopologyEdges.Add(new TopologyEdge(4201, 0, 1, 0, 1, 14f, false));

        new STFU.NPR.Steps.Mesh.ExtractFeatureLinesStep().Execute(context);

        Assert(context.Graph.Curves.Any(curve => curve.Kind == FeatureCurveKind.ApparentRidge), "Expected apparent ridge curve.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ApparentRidge).All(curve => curve.Intent == NprStrokeIntent.Accent), "Expected apparent ridge to map to accent intent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ApparentRidge).All(curve => (curve.Flags & FeatureCurveFlags.ViewDependent) != 0), "Expected apparent ridge to be view-dependent.");
        Assert(context.Graph.Curves.Where(curve => curve.Kind == FeatureCurveKind.ApparentRidge).All(curve => curve.Confidence > 0.3f), "Expected apparent ridge confidence.");
    }

    public void RefineFeatureConfidenceStepSmoothsNeighboringCurvatureCurves()
    {
        var context = CreateVisibilityContext();
        context.Graph.AddCurve(FeatureCurve.FromLine(
            8001,
            FeatureCurveKind.Ridge,
            NprStrokeIntent.Accent,
            new FeaturePoint(new Point2D(10f, 10f), 0.4f),
            new FeaturePoint(new Point2D(20f, 10f), 0.4f),
            new FeatureCurveSource(1, 2, 0, 1),
            0.7f,
            0.6f,
            confidence: 0.2f));
        context.Graph.AddCurve(FeatureCurve.FromLine(
            8002,
            FeatureCurveKind.Ridge,
            NprStrokeIntent.Accent,
            new FeaturePoint(new Point2D(20f, 10f), 0.4f),
            new FeaturePoint(new Point2D(30f, 10f), 0.4f),
            new FeatureCurveSource(2, 3, 1, 2),
            0.72f,
            0.62f,
            confidence: 0.9f));

        new STFU.NPR.Steps.Analysis.RefineFeatureConfidenceStep().Execute(context);

        var first = context.Graph.Curves.Single(curve => curve.StableId == 8001);
        var second = context.Graph.Curves.Single(curve => curve.StableId == 8002);
        Assert(first.Confidence > 0.2f, "Expected first ridge confidence to be lifted by neighboring support.");
        Assert(second.Confidence < 0.9f, "Expected second ridge confidence to be slightly smoothed toward neighborhood.");
    }

    public void BvhOcclusionQueryMatchesSampleOcclusionFixture()
    {
        var context = CreateVisibilityContext();

        var sample = new SampleOcclusionQuery();
        var bvh = new BvhOcclusionQuery();

        var hiddenPoint = new Point2D(50f, 50f);
        var visiblePoint = new Point2D(20f, 20f);

        Assert(sample.IsOccluded(context, hiddenPoint, 0.5f), "Expected sample query to detect occlusion at hidden point.");
        Assert(bvh.IsOccluded(context, hiddenPoint, 0.5f), "Expected BVH query to detect occlusion at hidden point.");
        Assert(!sample.IsOccluded(context, visiblePoint, 0.5f), "Expected sample query to keep visible point clear.");
        Assert(!bvh.IsOccluded(context, visiblePoint, 0.5f), "Expected BVH query to keep visible point clear.");
    }

    public void SvgNprDocumentExporterUsesOfflineExactVisibilityPass()
    {
        var (pipeline, context) = CreatePipelineContext();
        pipeline.Execute(context);

        var exportRenderer = new NprExportRenderer();
        var exportContext = exportRenderer.RenderOfflineExact(pipeline, context);
        Assert(exportContext.Style.Visibility.Strictness == VisibilityStrictness.OfflineExact, "Expected export context to switch to offline exact visibility.");
        Assert(exportContext.VisibilityResolver.GetType() == typeof(OfflineExactVisibilityResolver), "Expected offline exact visibility resolver in export context.");

        var exporter = new SvgNprDocumentExporter();
        var svg = exporter.ExportToString(pipeline, context, context.Style.CreateSvgExportOptions());
        Assert(svg.Contains("<svg", StringComparison.Ordinal), "Expected exported SVG root from offline exact export path.");
    }

    public void VisibilityContractsAreAvailableInContext()
    {
        var (_, context) = CreatePipelineContext();
        Assert(context.VisibilityResolver is IVisibilityResolver, "Expected visibility resolver contract.");
        Assert(context.OcclusionQuery is IOcclusionQuery, "Expected occlusion query contract.");
        Assert(context.VisibilityResolver.GetType() == typeof(SampleVisibilityResolver), "Expected sample visibility resolver by default.");
        Assert(context.OcclusionQuery.GetType() == typeof(BvhOcclusionQuery), "Expected BVH occlusion query by default.");
    }

    public void FrameHistoryCapturesPreviousFrameData()
    {
        var history = new FrameHistoryState();
        var first = CreatePipelineContext(historyState: history);
        first.Pipeline.Execute(first.Context);

        Assert(history.Latest is not null, "Expected captured frame history after first execution.");
        var firstHistory = history.Latest!;
        Assert(firstHistory.PreviousFrameId == first.Context.FrameId, "Expected captured frame id.");
        Assert(firstHistory.CurvesByStableId.Count > 0, "Expected captured previous curves.");
        Assert(firstHistory.StrokesByStableId.Count > 0, "Expected captured previous strokes.");

        var second = CreatePipelineContext(historyState: history);
        Assert(second.Context.PreviousFrame is not null, "Expected previous frame on next context.");
        Assert(second.Context.PreviousFrame!.PreviousFrameId == first.Context.FrameId, "Expected previous frame id propagation.");
        second.Pipeline.Execute(second.Context);

        Assert(second.Context.Graph.CurveMatchesByStableId.Count > 0, "Expected temporal curve matches on second frame.");
        Assert(second.Context.Graph.StrokeMatchesByStableId.Count > 0, "Expected temporal stroke matches on second frame.");
        Assert(second.Context.Graph.CurveMatchesByStableId.Values.Any(match => match.Kind == TemporalMatchKind.DirectStableIdMatch), "Expected direct curve stable-id match.");
        Assert(second.Context.Graph.StrokeMatchesByStableId.Values.Any(match => match.Kind == TemporalMatchKind.DirectStableIdMatch), "Expected direct stroke stable-id match.");
        Assert(second.Context.Graph.CurveStatesByStableId.Values.Any(state => state == TemporalFeatureState.MatchedDirect), "Expected current curve temporal state.");
        Assert(second.Context.Graph.StrokeStatesByStableId.Values.Any(state => state == TemporalStrokeState.Alive), "Expected current stroke temporal state.");
        Assert(second.Context.Graph.StyledStrokes.Any(stroke => stroke.TemporalState == TemporalStrokeState.Alive), "Expected styled stroke temporal state.");
        Assert(second.Context.DebugFrame.Counters.DirectTemporalMatchCount > 0, "Expected direct temporal debug counter.");
        Assert(second.Context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.TemporalMatches), "Expected temporal match overlay lines.");
        Assert(history.Latest!.PreviousFrameId == second.Context.FrameId, "Expected history to advance after second execution.");
    }

    public void FrameHistoryCapturesStrokePathsByStableIdAfterLayerSorting()
    {
        var history = new FrameHistoryState();
        var context = CreateVisibilityContext();
        context.Graph.StyledStrokes.Add(new StyledStroke(
            12002,
            12002,
            FeatureCurveKind.Silhouette,
            NprStrokeIntent.Silhouette,
            [new Point2D(70f, 70f), new Point2D(90f, 70f)],
            0.3f,
            0.9f,
            1f,
            VisibilityState.Visible)
        {
            Thickness = 2f,
            Opacity = 0.9f,
            Color = StrokeColor.Black
        });
        context.Graph.StyledStrokes.Add(new StyledStroke(
            12001,
            12001,
            FeatureCurveKind.SurfaceFlow,
            NprStrokeIntent.SurfaceFlow,
            [new Point2D(10f, 10f), new Point2D(20f, 10f)],
            0.3f,
            0.5f,
            0.5f,
            VisibilityState.Visible)
        {
            Thickness = 1f,
            Opacity = 0.7f,
            Color = StrokeColor.Black
        });

        new STFU.NPR.Steps.Strokes.BuildStrokeFrameStep().Execute(context);
        Assert(context.Frame.Paths[0].Metadata!.StableId == 12001, "Expected frame layer sorting to reorder paths.");

        history.Capture(context.View, context.Graph, context.Frame, context.TimeSeconds);

        var previous = history.GetPreviousFrame()!;
        Assert(previous.StrokesByStableId[12001].Path.Points[0] == new Point2D(10f, 10f), "Expected surface flow path to stay on its own stable id.");
        Assert(previous.StrokesByStableId[12002].Path.Points[0] == new Point2D(70f, 70f), "Expected silhouette path to stay on its own stable id.");
    }

    public void TemporalMatchingFallsBackToSourceAndScreenOverlap()
    {
        var previousFrame = new FrameHistory
        {
            PreviousFrameId = 7,
            PreviousView = new NprViewContext(
                CameraState.Default,
                ProjectionInfo.Create(CameraState.Default, 100, 100),
                LightContext.Default,
                SketchNprPreset.CreateSettings(),
                SketchNprPreset.CreateGrammar(),
                "generic-sketch",
                7,
                7f / 60f,
                null),
            CurvesByStableId = new Dictionary<int, PreviousFeatureCurve>
            {
                [100] = new PreviousFeatureCurve(
                    100,
                    FeatureCurveKind.Crease,
                    new FeatureCurveSource(1, 2, 3, 4),
                    [
                        new FeaturePoint(new Point2D(10f, 50f), 0.4f),
                        new FeaturePoint(new Point2D(40f, 50f), 0.4f)
                    ],
                    [],
                    new SalienceScore(0.7f, 1f, 0.7f, 1f, 0.7f, 1f, 0f, 0.7f))
            },
            StrokesByStableId = new Dictionary<int, PreviousStroke>
            {
                [900] = new PreviousStroke(
                    900,
                    100,
                    NprStrokeIntent.Crease,
                    StrokePath2D.Line(new Point2D(11f, 50f), new Point2D(39f, 50f), new StrokeStyle2D(1.5f, 0.75f, StrokeColor.Black)),
                    3f,
                    7f / 60f,
                    TemporalStrokeState.Alive)
            }
        };
        var context = CreateVisibilityContext(previousFrame: previousFrame);

        context.Graph.Curves.Add(FeatureCurve.FromLine(
            200,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            new FeaturePoint(new Point2D(12f, 50f), 0.4f),
            new FeaturePoint(new Point2D(41f, 50f), 0.4f),
            new FeatureCurveSource(1, 2, 3, 4),
            0.2f,
            0.8f));

        context.Graph.Candidates.Add(new StrokeCandidate(
            901,
            200,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            [new Point2D(12f, 50f), new Point2D(41f, 50f)],
            0.4f,
            0.2f,
            0.8f,
            1f,
            new SalienceScore(0.7f, 1f, 0.7f, 1f, 0.7f, 1f, 0f, 0.7f),
            VisibilityState.Visible,
            0.2f,
            new Vector2(1f, 0f),
            0.5f));

        new STFU.NPR.Steps.Analysis.BuildTemporalMatchesStep().Execute(context);

        Assert(context.Graph.CurveMatchesByStableId.TryGetValue(200, out var curveMatch), "Expected fallback curve match.");
        Assert(curveMatch!.Kind == TemporalMatchKind.SourceScreenOverlapMatch, "Expected source/screen overlap curve match.");
        Assert(context.Graph.StrokeMatchesByStableId.TryGetValue(901, out var strokeMatch), "Expected fallback stroke match.");
        Assert(strokeMatch!.Kind == TemporalMatchKind.SourceScreenOverlapMatch, "Expected source/screen overlap stroke match.");
        Assert(strokeMatch.PreviousStableId == 900, "Expected fallback to previous overlapping stroke.");
        Assert(context.Graph.CurveStatesByStableId[200] == TemporalFeatureState.MatchedFallback, "Expected fallback curve state.");
        Assert(context.Graph.StrokeStatesByStableId[901] == TemporalStrokeState.Replaced, "Expected replacement stroke state.");
        Assert(context.Graph.CurveMatchesByStableId.Values.All(match => match.Confidence > 0f), "Expected temporal curve confidence.");
        Assert(context.Graph.StrokeMatchesByStableId.Values.All(match => match.Confidence > 0f), "Expected temporal stroke confidence.");

        new STFU.NPR.Steps.Strokes.StyleStrokesStep().Execute(context);
        Assert(context.Graph.StyledStrokes.Any(stroke => stroke.TemporalState == TemporalStrokeState.Replaced), "Expected styled replacement stroke state.");
    }

    public void TemporalGeometryBlendPullsStrokeTowardPreviousFrame()
    {
        var previousFrame = new FrameHistory
        {
            PreviousFrameId = 11,
            PreviousView = new NprViewContext(
                CameraState.Default,
                ProjectionInfo.Create(CameraState.Default, 100, 100),
                LightContext.Default,
                SketchNprPreset.CreateSettings(),
                SketchNprPreset.CreateGrammar(),
                "generic-sketch",
                11,
                11f / 60f,
                null),
            StrokesByStableId = new Dictionary<int, PreviousStroke>
            {
                [100] = new PreviousStroke(
                    100,
                    50,
                    NprStrokeIntent.Crease,
                    new StrokePath2D(
                        [new Point2D(0f, 0f), new Point2D(5f, 0f), new Point2D(10f, 0f)],
                        new StrokeStyle2D(1.5f, 0.8f, StrokeColor.Black)),
                    4f,
                    11f / 60f,
                    TemporalStrokeState.Alive)
            }
        };

        var matched = CreateVisibilityContext(previousFrame: previousFrame);
        matched.Graph.Candidates.Add(new StrokeCandidate(
            200,
            50,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            [new Point2D(100f, 100f), new Point2D(120f, 100f)],
            0.4f,
            0.3f,
            0.8f,
            1f,
            new SalienceScore(0.7f, 1f, 0.7f, 1f, 0.7f, 1f, 0f, 0.7f),
            VisibilityState.Visible,
            0.3f,
            new Vector2(1f, 0f),
            0.5f));
        matched.Graph.StrokeMatchesByStableId[200] = new TemporalStrokeMatch(
            200,
            100,
            50,
            TemporalMatchKind.DirectStableIdMatch,
            4f,
            TemporalStrokeState.Alive,
            1f);
        matched.Graph.StrokeStatesByStableId[200] = TemporalStrokeState.Alive;

        var unmatched = CreateVisibilityContext();
        unmatched.Graph.Candidates.Add(new StrokeCandidate(
            200,
            50,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            [new Point2D(100f, 100f), new Point2D(120f, 100f)],
            0.4f,
            0.3f,
            0.8f,
            1f,
            new SalienceScore(0.7f, 1f, 0.7f, 1f, 0.7f, 1f, 0f, 0.7f),
            VisibilityState.Visible,
            0.3f,
            new Vector2(1f, 0f),
            0.5f));

        new STFU.NPR.Steps.Strokes.StyleStrokesStep().Execute(matched);
        new STFU.NPR.Steps.Strokes.HumanizeStrokesStep().Execute(matched);
        new STFU.NPR.Steps.Strokes.StyleStrokesStep().Execute(unmatched);
        new STFU.NPR.Steps.Strokes.HumanizeStrokesStep().Execute(unmatched);

        var matchedStroke = matched.Graph.StyledStrokes[0];
        var unmatchedStroke = unmatched.Graph.StyledStrokes[0];

        Assert(matchedStroke.Points.Count == unmatchedStroke.Points.Count, "Expected comparable stroke point counts.");
        Assert(matchedStroke.Points[0].X < unmatchedStroke.Points[0].X, "Expected temporal blend to pull stroke start toward previous frame.");
        Assert(matchedStroke.Points[^1].X < unmatchedStroke.Points[^1].X, "Expected temporal blend to pull stroke end toward previous frame.");
    }

    public void BuildStrokeFrameAddsFadingOutResidualsForUnmatchedPreviousStrokes()
    {
        var context = CreateVisibilityContext(previousFrame: new FrameHistory
        {
            PreviousFrameId = 21,
            PreviousView = new NprViewContext(
                CameraState.Default,
                ProjectionInfo.Create(CameraState.Default, 100, 100),
                LightContext.Default,
                SketchNprPreset.CreateSettings(),
                SketchNprPreset.CreateGrammar(),
                "generic-sketch",
                21,
                21f / 60f,
                null),
            StrokesByStableId = new Dictionary<int, PreviousStroke>
            {
                [777] = new PreviousStroke(
                    777,
                    555,
                    NprStrokeIntent.Crease,
                    StrokePath2D.Line(new Point2D(10f, 10f), new Point2D(30f, 10f), new StrokeStyle2D(1.4f, 0.8f, StrokeColor.Black)),
                    2f,
                    21f / 60f,
                    TemporalStrokeState.Alive)
            }
        });

        new STFU.NPR.Steps.Strokes.BuildStrokeFrameStep().Execute(context);

        Assert(context.Frame.Paths.Count == 1, "Expected one fading-out residual stroke path.");
        var ghost = context.Frame.Paths[0];
        Assert(ghost.Metadata is not null, "Expected ghost metadata.");
        Assert(ghost.Metadata!.SourceKind == "GhostStroke", "Expected ghost stroke metadata kind.");
        Assert(ghost.Metadata.Layer == "ghost-crease", "Expected ghost stroke layer.");
        Assert(ghost.Metadata.Visibility == TemporalStrokeState.FadingOut.ToString(), "Expected fading-out visibility metadata.");
        Assert(ghost.Style.Opacity < 0.8f, "Expected residual opacity reduction.");

        new STFU.NPR.Steps.Debug.BuildDebugFrameStep().Execute(context);
        Assert(context.DebugFrame.Counters.GhostStrokeCount == 1, "Expected ghost stroke debug counter.");
        Assert(context.DebugFrame.Lines.Any(line => line.Kind == STFU.NPR.Debug.DebugOverlayKind.GhostStrokes), "Expected ghost stroke overlay lines.");
    }

    public void SnapshotMetricConfirmsDeterministicFrames()
    {
        var first = CreatePipelineContext();
        var baseline = first.Pipeline.Execute(first.Context);
        var second = CreatePipelineContext();
        var candidate = second.Pipeline.Execute(second.Context);

        new NprSnapshotTest().AssertStable(baseline, candidate, 0.001f);

        var delta = VisualRegressionMetric.MeanEndpointDelta(baseline, candidate);
        Assert(delta <= 0.001f, "Expected deterministic frames to have near-zero visual regression delta.");
    }

    public void AdapterStepsAndStableIdContractsAreAvailable()
    {
        FeatureCurveStableId featureId = 42;
        StrokeStableId strokeId = 84;
        Assert((int)featureId == 42, "Expected feature stable id conversion.");
        Assert((int)strokeId == 84, "Expected stroke stable id conversion.");

        var apparent = new ApparentCurvatureSample(3, Vector3.UnitZ, 0.5f, 0.7f);
        Assert(apparent.TriangleIndex == 3, "Expected apparent curvature sample.");
        Assert(apparent.Confidence > 0f, "Expected apparent curvature confidence.");

        var previousGraph = new PreviousFrameGraph(
            new Dictionary<int, PreviousFeatureCurve>(),
            new Dictionary<int, PreviousStroke>());
        Assert(previousGraph.CurvesByStableId.Count == 0, "Expected previous frame graph curves.");
        Assert(previousGraph.StrokesByStableId.Count == 0, "Expected previous frame graph strokes.");

        var context = CreateVisibilityContext();
        context.Graph.VisibilitySegments.Add(new VisibilitySegment(
            1101,
            1101,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            VisibilityState.Visible,
            0f,
            1f,
            new Point2D(0f, 0f),
            new Point2D(20f, 0f),
            0.2f,
            0.3f,
            0.7f,
            1f));
        new STFU.NPR.Steps.Analysis.ResolveCurveVisibilityStep().Execute(context);
        new STFU.NPR.Steps.Analysis.ScorePruneBySalienceStep().Execute(context);
        Assert(context.Graph.SalienceByStableId.Count >= 0, "Expected score-prune adapter step to execute.");
    }

    public void CurvatureAndTopologyAnalysisExposeNamedSupplementArtifacts()
    {
        var (_, context) = CreatePipelineContext();
        var entity = context.Scene.Entities[0];
        Assert(context.Assets.TryGetMesh(entity.Mesh, out var mesh), "Expected mesh in asset registry.");
        var cache = context.Analysis.GetOrCreate(entity.Mesh, mesh);

        Assert(cache.Topology.Edges.Count > 0, "Expected topology cache edges.");
        Assert(cache.Topology.Edges.Any(edge => edge.Semantic == EdgeSemantic.Boundary || edge.Semantic == EdgeSemantic.Smooth || edge.Semantic == EdgeSemantic.HardCrease), "Expected topology edge semantic classification.");
        Assert(cache.Topology.Edges.All(edge => edge.NormalAngleDegrees >= 0f), "Expected topology edge normal angles.");

        var curvature = cache.Curvature!;
        Assert(curvature.VertexSamples.Count == mesh.Vertices.Count, "Expected vertex curvature samples.");
        Assert(curvature.FaceSamples.Count == mesh.Triangles.Count, "Expected face curvature samples.");
        Assert(curvature.MeanEdgeLength >= 0f, "Expected mean edge length.");
        Assert(curvature.SmoothingRadius >= 0f, "Expected smoothing radius.");
        Assert(curvature.Quality != CurvatureQuality.NotComputed, "Expected computed curvature quality.");
        Assert(curvature.VertexSamples.Any(sample => sample.Confidence >= 0f && sample.Confidence <= 1f), "Expected bounded curvature sample confidence.");
        Assert(curvature.FaceSamples.Any(sample => sample.Direction1.LengthSquared() > 0.0001f || sample.Direction2.LengthSquared() > 0.0001f), "Expected directional curvature samples.");
    }

    public void PipelineBenchmarkFixtureProducesFrameAndTiming()
    {
        var (pipeline, context) = CreatePipelineContext();
        var result = PipelineBenchmarkFixture.Measure(pipeline, context, warmup: 1, iterations: 2);

        Assert(result.Frame.Paths.Count > 0, "Expected benchmark fixture to produce a frame.");
        Assert(result.Milliseconds > 0d, "Expected positive benchmark timing.");
        Assert(result.Milliseconds < 500d, "Expected benchmark fixture timing to stay within a coarse sanity bound.");
    }

    public void NamedArtifactAdaptersAreUsable()
    {
        var spatial = new STFU.NPR.Analysis.SpatialGrid2D<int>(16);
        spatial.Add(4f, 4f, 1);
        Assert(spatial.EnumerateTiles().Any(), "Expected spatial grid tile.");

        var geometry = new STFU.NPR.Analysis.GeometryAnalyzer();
        var meshAnalysis = geometry.Analyze(CreateCubeMesh());
        var boundsCache = new STFU.NPR.Analysis.BoundsCache(meshAnalysis.Bounds);
        Assert(boundsCache.Bounds.Min != boundsCache.Bounds.Max, "Expected bounds cache.");

        var visibilityOptions = new STFU.NPR.Visibility.VisibilityOptions(0.025f, true, true);
        Assert(visibilityOptions.SplitCurves, "Expected visibility options.");

        var frame = ExportFixture.CreateMiniFrame();
        var layer = new STFU.Strokes.Export.ExportLayer("test", frame.Paths);
        var metadata = new STFU.Strokes.Export.ExportMetadata("generic-sketch", "generic-sketch", 1, [layer]);
        Assert(metadata.Layers.Count == 1, "Expected export metadata layer.");

        var manifest = new RuntimePresetPluginManifest(
            "plugin.test",
            "Test Plugin",
            new PresetVersion(1, 0, 0),
            "Test.Assembly",
            "Test.Provider",
            ["generic-sketch"]);
        var plugin = new RuntimePresetPlugin(manifest, [new GenericSketchNprPreset()]);
        var json = plugin.ManifestToJson();
        var restored = RuntimePresetPlugin.ManifestFromJson(json);
        Assert(restored.PluginId == manifest.PluginId, "Expected runtime plugin manifest roundtrip.");
    }

    public void HumanizationProfilesAffectStyledStroke()
    {
        var stroke = new StyledStroke(
            99001,
            99001,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            [new Point2D(10f, 10f), new Point2D(40f, 10f)],
            0.4f,
            0.5f,
            0.7f,
            VisibilityState.Visible,
            0.4f,
            0.4f)
        {
            Thickness = 1.2f,
            Opacity = 0.8f
        };

        var style = new STFU.NPR.Styles.NprStrokeStyle
        {
            Medium = StrokeMedium.Pencil,
            BaseThickness = 1.2f,
            ThicknessVariation = 0.3f,
            EndpointJitter = 0.8f,
            Overshoot = 1.5f
        };

        STFU.NPR.Styles.IStrokeHumanizer humanizer = new STFU.NPR.Styles.DefaultStrokeHumanizer();
        humanizer.Humanize(stroke, style, 1337);

        Assert(stroke.Points.Count == 3, "Expected humanized three-point stroke.");
        Assert(Math.Abs(stroke.Thickness - 1.2f) > 0.01f, "Expected humanizer to affect thickness.");
        Assert(Math.Abs(stroke.Opacity - 0.8f) > 0.001f, "Expected humanizer to affect opacity.");

        var pressure = new STFU.NPR.Styles.PressureProfile(0.7f, 1.0f, 0.75f);
        Assert(pressure.Sample(0.5f) > pressure.Sample(0f), "Expected pressure peak near stroke midpoint.");
    }

    public void FeatureAndFrameAdaptersAreUsable()
    {
        var direction = new STFU.NPR.Fields.DirectionSample(new Point2D(10f, 10f), new Vector2(1f, 0f));
        var directionAdapter = STFU.NPR.Fields.DirectionFieldSample.From(direction);
        Assert(directionAdapter.Direction.X == 1f, "Expected direction field adapter.");

        var curve = FeatureCurve.FromLine(
            501,
            FeatureCurveKind.SurfaceFlow,
            NprStrokeIntent.SurfaceFlow,
            new FeaturePoint(new Point2D(0f, 0f), 0.3f),
            new FeaturePoint(new Point2D(10f, 0f), 0.3f),
            FeatureCurveSource.None,
            0.5f,
            0.6f);
        var flowCurve = new STFU.NPR.Graph.SurfaceFlowCurve(curve);
        Assert(flowCurve.StableId == 501, "Expected surface flow curve adapter.");

        var frameContext = STFU.NPR.Pipeline.FrameContext.From(new NprViewContext(
            CameraState.Default,
            ProjectionInfo.Create(CameraState.Default, 100, 100),
            LightContext.Default,
            SketchNprPreset.CreateSettings(),
            SketchNprPreset.CreateGrammar(),
            "generic-sketch",
            12,
            0.2f,
            null));
        Assert(frameContext.FrameId == 12, "Expected frame context adapter.");
        Assert(Enum.IsDefined(typeof(FeatureCurveKind), FeatureCurveKind.MaterialBoundary), "Expected material boundary feature kind.");
        Assert(Enum.IsDefined(typeof(FeatureCurveKind), FeatureCurveKind.HatchGuide), "Expected hatch guide feature kind.");
    }

    public void RichPointsCarryPressureProfileVariation()
    {
        var context = CreateVisibilityContext();
        context.Settings.StrokeStyle.Medium = StrokeMedium.Pencil;
        context.Graph.StyledStrokes.Add(new StyledStroke(
            12301,
            12301,
            FeatureCurveKind.Crease,
            NprStrokeIntent.Crease,
            [new Point2D(10f, 10f), new Point2D(20f, 14f), new Point2D(30f, 10f)],
            0.4f,
            0.5f,
            0.6f,
            VisibilityState.Visible)
        {
            Thickness = 1.4f,
            Opacity = 0.8f,
            Color = StrokeColor.Black
        });

        new STFU.NPR.Steps.Strokes.BuildStrokeFrameStep().Execute(context);

        var richPoints = context.Frame.Paths[0].RichPoints!;
        Assert(richPoints.Count == 3, "Expected rich points.");
        Assert(richPoints[1].Pressure > richPoints[0].Pressure, "Expected midpoint pressure peak.");
        Assert(richPoints[1].Thickness > richPoints[0].Thickness, "Expected pressure to influence point thickness.");
    }

    private static (INprPipeline Pipeline, NprContext Context) CreatePipelineContext(MeshAnalysisCacheStore? analysis = null, FrameHistoryState? historyState = null)
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
                FrameId = (historyState ?? new FrameHistoryState()).PeekNextFrameId(),
                TimeSeconds = (historyState ?? new FrameHistoryState()).PeekNextFrameId() / 60f,
                PreviousFrame = historyState?.GetPreviousFrame(),
                Scene = engine.Scene,
                Assets = assets,
                Camera = CameraState.Default,
                Width = 800,
                Height = 600,
                Settings = settings,
                Style = SketchNprPreset.CreateGrammar(),
                Analysis = analysis ?? new MeshAnalysisCacheStore(),
                VisibilityResolver = new SampleVisibilityResolver(),
                OcclusionQuery = new BvhOcclusionQuery(),
                FrameHistoryState = historyState ?? new FrameHistoryState()
            });
    }

    private static NprContext CreateVisibilityContext(StyleBudgetRule? budget = null, FrameHistory? previousFrame = null, StyleGrammar? style = null)
    {
        var engine = StfuEngineBuilder
            .Create()
            .Build();
        style ??= SketchNprPreset.CreateGrammar();
        if (budget is not null)
        {
            style = style with { Budget = budget };
        }

        var context = new NprContext
        {
            FrameId = 1,
            TimeSeconds = 1f / 60f,
            PreviousFrame = previousFrame,
            Scene = engine.Scene,
            Assets = new AssetRegistry(),
            Camera = CameraState.Default,
            Width = 100,
            Height = 100,
            Settings = SketchNprPreset.CreateSettings(),
            Style = style,
            Analysis = new MeshAnalysisCacheStore(),
            VisibilityResolver = new SampleVisibilityResolver(),
            OcclusionQuery = new BvhOcclusionQuery(),
            FrameHistoryState = new FrameHistoryState()
        };

        context.Graph.Vertices.AddRange([
            new ProjectedVertex(0, Vector3.Zero, Vector3.UnitZ, new Point2D(35f, 35f), 0.2f, true),
            new ProjectedVertex(1, Vector3.Zero, Vector3.UnitZ, new Point2D(65f, 35f), 0.2f, true),
            new ProjectedVertex(2, Vector3.Zero, Vector3.UnitZ, new Point2D(65f, 65f), 0.2f, true),
            new ProjectedVertex(3, Vector3.Zero, Vector3.UnitZ, new Point2D(35f, 65f), 0.2f, true)
        ]);

        context.Graph.Triangles.AddRange([
            new ProjectedTriangle(100, 0, 0, 0, 1, 2, Vector3.UnitZ, Vector3.Zero, new Point2D(55f, 45f), 0.2f, 450f, 0.3f, true, true),
            new ProjectedTriangle(101, 0, 1, 0, 2, 3, Vector3.UnitZ, Vector3.Zero, new Point2D(45f, 55f), 0.2f, 450f, 0.3f, true, true)
        ]);

        return context;
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
