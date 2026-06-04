const canvas = document.querySelector("#previewCanvas");
const ctx = canvas.getContext("2d");

const q = selector => document.querySelector(selector);
const qa = selector => Array.from(document.querySelectorAll(selector));

const panels = {
  load: ["Load", "Assets, mesh handles, and scene entities."],
  general: ["General", "Preset metadata, render mode, seed, and command log."],
  camera: ["Camera", "CameraState and viewport camera commands."],
  npr: ["Default Pipeline", "DefaultDrawingSettings plus per-step graph counters."],
  strokes: ["Style Layers", "StyleSet roles, layers, marks, fills, and preview."],
  debug: ["Debug Graph", "NprGraph counters, parity, trace, and determinism."],
};

const presetDefinitions = {
  "default": {
    id: "default",
    name: "Default",
    description: "Built-in Default parity line-art preset using projection, face-id visibility, edge fragments, path joining, draw progress, and comic ink strokes.",
    editable: false,
    pipelineId: "default",
    pipelineProvider: "STFU.NPR.Pipeline.Default",
    settings: {
      seed: 17,
      showSilhouette: true,
      showFeature: true,
      showBoundary: true,
      featureAngleDegrees: 34,
      minSegPx: 1,
      meshStride: 1,
      occlusionCulling: true,
      depthScale: 1,
      pathSimplify: 0.6,
      drawProgress: 1,
      autoDraw: true,
      lineWidth: 2.2,
      jitter: 1.6,
      pressure: 0.32,
    },
    strokeStyle: {
      seed: 17,
      baseThickness: 2.2,
      thicknessVariation: 0.12,
      endpointJitter: 1.6,
      overshoot: 0.35,
    },
  },
  "technical-ink": {
    id: "technical-ink",
    name: "Technical Ink",
    description: "Style-only preset on the Default pipeline: cleaner contour/feature hierarchy with low jitter and reduced pressure variation.",
    editable: false,
    pipelineId: "default",
    pipelineProvider: "STFU.NPR.Pipeline.Default",
    settings: {
      seed: 2201,
      showSilhouette: true,
      showFeature: true,
      showBoundary: true,
      featureAngleDegrees: 28,
      minSegPx: 1,
      meshStride: 1,
      occlusionCulling: true,
      depthScale: 1,
      pathSimplify: 0.72,
      drawProgress: 1,
      autoDraw: true,
      lineWidth: 1.55,
      jitter: 0.28,
      pressure: 0.12,
    },
    strokeStyle: {
      seed: 2201,
      baseThickness: 1.55,
      thicknessVariation: 0.16,
      endpointJitter: 0.28,
      overshoot: 0.25,
    },
  },
  "pencil-construction": {
    id: "pencil-construction",
    name: "Pencil Construction",
    description: "Foreground/midground sketch preset on Default: softer pressure, more construction accents, and lower opacity support layers.",
    editable: true,
    pipelineId: "default",
    pipelineProvider: "STFU.NPR.Pipeline.Default",
    settings: {
      seed: 3017,
      showSilhouette: true,
      showFeature: true,
      showBoundary: true,
      featureAngleDegrees: 38,
      minSegPx: 1,
      meshStride: 1,
      occlusionCulling: true,
      depthScale: 1,
      pathSimplify: 0.52,
      drawProgress: 1,
      autoDraw: true,
      lineWidth: 1.35,
      jitter: 1.9,
      pressure: 0.42,
    },
    strokeStyle: {
      seed: 3017,
      baseThickness: 1.35,
      thicknessVariation: 0.5,
      endpointJitter: 1.9,
      overshoot: 1.2,
    },
  },
  "pen-ink-hatching": {
    id: "pen-ink-hatching",
    name: "Pen And Ink Hatching",
    description: "Style target for the next Hatching pipeline extension; currently uses Default line-art layers plus planned hatch/tone channels.",
    editable: true,
    pipelineId: "default",
    pipelineProvider: "STFU.NPR.Pipeline.Default",
    settings: {
      seed: 3761,
      showSilhouette: true,
      showFeature: true,
      showBoundary: true,
      featureAngleDegrees: 34,
      minSegPx: 1,
      meshStride: 1,
      occlusionCulling: true,
      depthScale: 1,
      pathSimplify: 0.58,
      drawProgress: 1,
      autoDraw: true,
      lineWidth: 1.9,
      jitter: 0.78,
      pressure: 0.36,
    },
    strokeStyle: {
      seed: 3761,
      baseThickness: 1.9,
      thicknessVariation: 0.34,
      endpointJitter: 0.78,
      overshoot: 0.8,
    },
  },
  "manga-ink": {
    id: "manga-ink",
    name: "Manga Ink",
    description: "Style-only preset on Default with heavier silhouettes, clean feature strokes, and graphic ink layer hierarchy.",
    editable: true,
    pipelineId: "default",
    pipelineProvider: "STFU.NPR.Pipeline.Default",
    settings: {
      seed: 4088,
      showSilhouette: true,
      showFeature: true,
      showBoundary: true,
      featureAngleDegrees: 42,
      minSegPx: 1,
      meshStride: 1,
      occlusionCulling: true,
      depthScale: 1,
      pathSimplify: 0.5,
      drawProgress: 1,
      autoDraw: true,
      lineWidth: 2.75,
      jitter: 0.58,
      pressure: 0.5,
    },
    strokeStyle: {
      seed: 4088,
      baseThickness: 2.75,
      thicknessVariation: 0.22,
      endpointJitter: 0.58,
      overshoot: 0.35,
    },
  },
  "blueprint": {
    id: "blueprint",
    name: "Blueprint Construction",
    description: "Style-only Default preset for clean visible lines, planned dashed construction layers, and SVG-friendly layer naming.",
    editable: false,
    pipelineId: "default",
    pipelineProvider: "STFU.NPR.Pipeline.Default",
    settings: {
      seed: 5102,
      showSilhouette: true,
      showFeature: true,
      showBoundary: true,
      featureAngleDegrees: 30,
      minSegPx: 1,
      meshStride: 1,
      occlusionCulling: true,
      depthScale: 1,
      pathSimplify: 0.8,
      drawProgress: 1,
      autoDraw: true,
      lineWidth: 1.25,
      jitter: 0.18,
      pressure: 0.08,
    },
    strokeStyle: {
      seed: 5102,
      baseThickness: 1.25,
      thicknessVariation: 0.08,
      endpointJitter: 0.18,
      overshoot: 0.12,
    },
  },
};

const assets = [
  {
    id: "suzanne",
    path: "assets/suzanne.obj",
    handle: 1,
    vertices: 507,
    triangles: 968,
    loader: "ObjMeshLoader",
    status: "Loaded",
  },
  {
    id: "prototype-cube",
    path: "assets/prototype-cube.obj",
    handle: 2,
    vertices: 8,
    triangles: 12,
    loader: "ObjMeshLoader",
    status: "Mock",
  },
  {
    id: "scan-study",
    path: "assets/scan-study.obj",
    handle: 3,
    vertices: 1248,
    triangles: 2380,
    loader: "ObjMeshLoader",
    status: "Mock",
  },
];

const intentNames = ["Silhouette", "Boundary", "Feature", "Crease", "SurfaceFlow", "Hatch", "Accent", "Fill", "Tones"];
const intentColors = {
  Silhouette: "#151713",
  Boundary: "#262a24",
  Feature: "#343833",
  Crease: "#444a42",
  SurfaceFlow: "#5f665e",
  Hatch: "#77746b",
  Accent: "#8a4e45",
  Fill: "#a6ada2",
  Tones: "#747d70",
};

const layerTypeLabels = {
  strokes: "Strokes",
  fill: "Fill",
  shading: "Shading",
  tones: "Tones",
};

function cloneStrokeStyle(style, overrides = {}) {
  return {
    baseThickness: style.baseThickness,
    thicknessVariation: style.thicknessVariation,
    endpointJitter: style.endpointJitter,
    overshoot: style.overshoot,
    ...overrides,
  };
}

function createPresetLayers(preset) {
  const style = preset.strokeStyle;
  const settings = preset.settings;
  return [
    {
      id: "foreground:contour",
      role: "Foreground",
      name: "Contour Ink",
      type: "strokes",
      visible: true,
      solo: false,
      locked: false,
      blend: "normal",
      opacity: 1,
      density: 1,
      color: intentColors.Silhouette,
      intents: ["Silhouette", "Boundary"],
      style: cloneStrokeStyle(style, {
        baseThickness: style.baseThickness * 1.35,
        thicknessVariation: style.thicknessVariation * 0.55,
      }),
      fillCoverage: 0,
      shadeThreshold: 0.5,
    },
    {
      id: "foreground:crease",
      role: "Foreground",
      name: "Feature / Crease",
      type: "strokes",
      visible: true,
      solo: false,
      locked: false,
      blend: "normal",
      opacity: 0.82,
      density: settings.showFeature ? 1 : 0,
      color: intentColors.Feature,
      intents: ["Feature", "Crease", "Accent"],
      style: cloneStrokeStyle(style),
      fillCoverage: 0,
      shadeThreshold: 0.5,
    },
    {
      id: "midground:hatching",
      role: "Midground",
      name: "Hatching Guide",
      type: "shading",
      visible: true,
      solo: false,
      locked: false,
      blend: "multiply",
      opacity: 0.34,
      density: 0.42,
      color: intentColors.Hatch,
      intents: ["Hatch", "SurfaceFlow", "Tones"],
      style: cloneStrokeStyle(style, {
        baseThickness: style.baseThickness * 0.52,
        endpointJitter: style.endpointJitter * 0.45,
      }),
      fillCoverage: 0.14,
      shadeThreshold: 0.58,
    },
    {
      id: "background:mainfill",
      role: "Background",
      name: "Main Fill",
      type: "fill",
      visible: true,
      solo: false,
      locked: false,
      blend: "multiply",
      opacity: 0.18,
      density: 0.45,
      color: "#a6ada2",
      intents: ["Fill", "Tones"],
      style: cloneStrokeStyle(style, {
        baseThickness: style.baseThickness * 0.2,
      }),
      fillCoverage: 0.35,
      shadeThreshold: 0.55,
    },
  ];
}

const state = {
  mode: "npr",
  tab: "load",
  presetId: "default",
  selectedAssetId: "suzanne",
  selectedEntityId: 1,
  nextEntityId: 2,
  showGrid: true,
  stableRandom: true,
  loaderStatus: "ObjMeshLoader ready",
  camera: {
    position: { x: 0, y: 0, z: 4 },
    target: { x: 0, y: 0, z: 0 },
    fov: 45,
    orbitYaw: 0,
    orbitPitch: 0,
  },
  settings: structuredClone(presetDefinitions["default"].settings),
  strokeStyle: structuredClone(presetDefinitions["default"].strokeStyle),
  selectedLayerId: "foreground:contour",
  nextLayerId: 1,
  strokeLayers: createPresetLayers(presetDefinitions["default"]),
  entities: [
    { id: 1, name: "Suzanne", meshId: "suzanne", role: "Foreground", position: { x: 0, y: 0, z: 0 } },
  ],
  overlays: {
    projectedVertices: false,
    projectedTriangles: false,
    topologyEdges: false,
    featureLines: true,
    surfaceSamples: false,
    finalStrokes: true,
    hiddenCandidates: false,
  },
  commandLog: [
    { time: "init", text: "ActiveNprPresetState.ApplyPreset(default)" },
    { time: "init", text: "NprPipelineRegistry.Resolve(default) -> STFU.NPR.Pipeline.Default" },
    { time: "init", text: "SetViewportRenderModeCommand(ViewportRenderMode.Npr)" },
  ],
};

const sliderDefs = [
  {
    id: "fovSlider",
    output: "fovValue",
    get: () => state.camera.fov,
    set: value => { state.camera.fov = value; },
    fromControl: value => value,
    toControl: value => value,
    label: value => String(Math.round(value)),
    command: () => `AdjustCameraFovCommand -> FieldOfViewDegrees ${formatScalar(state.camera.fov)}`,
  },
  {
    id: "orbitYawSlider",
    output: "orbitYawValue",
    get: () => state.camera.orbitYaw,
    set: value => { state.camera.orbitYaw = value; },
    fromControl: value => value,
    toControl: value => value,
    label: value => String(Math.round(value)),
    command: () => `OrbitCameraCommand(yaw=${formatScalar(state.camera.orbitYaw)}, pitch=${formatScalar(state.camera.orbitPitch)})`,
  },
  {
    id: "orbitPitchSlider",
    output: "orbitPitchValue",
    get: () => state.camera.orbitPitch,
    set: value => { state.camera.orbitPitch = value; },
    fromControl: value => value,
    toControl: value => value,
    label: value => String(Math.round(value)),
    command: () => `OrbitCameraCommand(yaw=${formatScalar(state.camera.orbitYaw)}, pitch=${formatScalar(state.camera.orbitPitch)})`,
  },
  {
    id: "featureAngleSlider",
    output: "featureAngleValue",
    get: () => state.settings.featureAngleDegrees,
    set: value => { state.settings.featureAngleDegrees = value; },
    fromControl: value => value,
    toControl: value => value,
    label: value => String(Math.round(value)),
    command: () => `DefaultDrawingSettings.FeatureAngleDegrees = ${formatScalar(state.settings.featureAngleDegrees)}`,
  },
  {
    id: "minSegSlider",
    output: "minSegValue",
    get: () => state.settings.minSegPx,
    set: value => { state.settings.minSegPx = value; },
    fromControl: value => value,
    toControl: value => value,
    label: value => formatScalar(value),
    command: () => `DefaultDrawingSettings.MinSegPx = ${formatScalar(state.settings.minSegPx)}`,
  },
  {
    id: "meshStrideSlider",
    output: "meshStrideValue",
    get: () => state.settings.meshStride,
    set: value => { state.settings.meshStride = Math.max(1, Math.round(value)); },
    fromControl: value => value,
    toControl: value => value,
    label: value => String(Math.round(value)),
    command: () => `DefaultDrawingSettings.MeshStride = ${Math.round(state.settings.meshStride)}`,
  },
  {
    id: "depthScaleSlider",
    output: "depthScaleValue",
    get: () => state.settings.depthScale,
    set: value => { state.settings.depthScale = value; },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `DefaultDrawingSettings.DepthScale = ${state.settings.depthScale.toFixed(2)}`,
  },
  {
    id: "pathSimplifySlider",
    output: "pathSimplifyValue",
    get: () => state.settings.pathSimplify,
    set: value => { state.settings.pathSimplify = value; },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `DefaultDrawingSettings.PathSimplify = ${state.settings.pathSimplify.toFixed(2)}`,
  },
  {
    id: "drawProgressSlider",
    output: "drawProgressValue",
    get: () => state.settings.drawProgress,
    set: value => { state.settings.drawProgress = value; },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `DefaultDrawingSettings.DrawProgress = ${state.settings.drawProgress.toFixed(2)}`,
  },
  {
    id: "lineWidthSlider",
    output: "lineWidthValue",
    get: () => state.settings.lineWidth,
    set: value => {
      state.settings.lineWidth = value;
      state.strokeStyle.baseThickness = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `DefaultDrawingSettings.LineWidth = ${state.settings.lineWidth.toFixed(2)}`,
  },
  {
    id: "jitterSlider",
    output: "jitterValue",
    get: () => state.settings.jitter,
    set: value => {
      state.settings.jitter = value;
      state.strokeStyle.endpointJitter = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `DefaultDrawingSettings.Jitter = ${state.settings.jitter.toFixed(2)}`,
  },
  {
    id: "pressureSlider",
    output: "pressureValue",
    get: () => state.settings.pressure,
    set: value => { state.settings.pressure = value; },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `DefaultDrawingSettings.Pressure = ${state.settings.pressure.toFixed(2)}`,
  },
  {
    id: "layerOpacitySlider",
    output: "layerOpacityValue",
    get: () => selectedLayer()?.opacity ?? 1,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.opacity = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, Opacity=${(selectedLayer()?.opacity ?? 0).toFixed(2)})`,
  },
  {
    id: "layerDensitySlider",
    output: "layerDensityValue",
    get: () => selectedLayer()?.density ?? 1,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.density = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, Density=${(selectedLayer()?.density ?? 0).toFixed(2)})`,
  },
  {
    id: "layerBaseThicknessSlider",
    output: "layerBaseThicknessValue",
    get: () => selectedLayer()?.style.baseThickness ?? state.strokeStyle.baseThickness,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.style.baseThickness = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, NprStrokeStyle.BaseThickness=${(selectedLayer()?.style.baseThickness ?? 0).toFixed(2)})`,
  },
  {
    id: "layerThicknessVariationSlider",
    output: "layerThicknessVariationValue",
    get: () => selectedLayer()?.style.thicknessVariation ?? state.strokeStyle.thicknessVariation,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.style.thicknessVariation = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, NprStrokeStyle.ThicknessVariation=${(selectedLayer()?.style.thicknessVariation ?? 0).toFixed(2)})`,
  },
  {
    id: "layerEndpointJitterSlider",
    output: "layerEndpointJitterValue",
    get: () => selectedLayer()?.style.endpointJitter ?? state.strokeStyle.endpointJitter,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.style.endpointJitter = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, NprStrokeStyle.EndpointJitter=${(selectedLayer()?.style.endpointJitter ?? 0).toFixed(2)})`,
  },
  {
    id: "layerOvershootSlider",
    output: "layerOvershootValue",
    get: () => selectedLayer()?.style.overshoot ?? state.strokeStyle.overshoot,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.style.overshoot = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, NprStrokeStyle.Overshoot=${(selectedLayer()?.style.overshoot ?? 0).toFixed(2)})`,
  },
  {
    id: "layerFillCoverageSlider",
    output: "layerFillCoverageValue",
    get: () => selectedLayer()?.fillCoverage ?? 0,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.fillCoverage = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, FillCoverage=${(selectedLayer()?.fillCoverage ?? 0).toFixed(2)})`,
  },
  {
    id: "layerShadeThresholdSlider",
    output: "layerShadeThresholdValue",
    get: () => selectedLayer()?.shadeThreshold ?? 0.58,
    set: value => {
      const layer = selectedLayer();
      if (layer) layer.shadeThreshold = value;
    },
    fromControl: value => value / 100,
    toControl: value => value * 100,
    label: value => value.toFixed(2),
    command: () => `UpdateNprLayerCommand(${selectedLayerIdLabel()}, ShadeThreshold=${(selectedLayer()?.shadeThreshold ?? 0).toFixed(2)})`,
  },
];

function selectedAsset() {
  return assets.find(asset => asset.id === state.selectedAssetId) || assets[0];
}

function selectedEntity() {
  return state.entities.find(entity => entity.id === state.selectedEntityId) || null;
}

function selectedLayer() {
  return state.strokeLayers.find(layer => layer.id === state.selectedLayerId) || state.strokeLayers[0] || null;
}

function selectedLayerIdLabel() {
  const layer = selectedLayer();
  return layer ? `"${layer.id}"` : "NprLayerId.None";
}

function activeLayers() {
  const soloLayers = state.strokeLayers.filter(layer => layer.visible && layer.solo);
  return soloLayers.length > 0 ? soloLayers : state.strokeLayers.filter(layer => layer.visible);
}

function layerHasIntent(layer, intent) {
  return layer.intents.includes(intent);
}

function assetById(id) {
  return assets.find(asset => asset.id === id) || null;
}

function formatScalar(value) {
  if (Number.isInteger(value)) return String(value);
  return value.toFixed(2).replace(/\.?0+$/, "");
}

function formatVector(vector) {
  return `(${formatScalar(vector.x)}, ${formatScalar(vector.y)}, ${formatScalar(vector.z)})`;
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function seeded(seed) {
  let value = seed >>> 0;
  return () => {
    value ^= value << 13;
    value ^= value >>> 17;
    value ^= value << 5;
    return ((value >>> 0) % 1000) / 1000;
  };
}

function commandTime() {
  return new Date().toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function pushCommand(text) {
  state.commandLog.unshift({ time: commandTime(), text });
  state.commandLog = state.commandLog.slice(0, 8);
  renderCommandLogs();
  q("#lastCommand").textContent = text;
}

function computeLayerStats(intentCounts) {
  const layers = activeLayers();
  const layerCounts = {};
  let strokeOutput = 0;
  let toneOutput = 0;

  for (const layer of state.strokeLayers) {
    const intentTotal = layer.intents.reduce((sum, intent) => sum + (intentCounts[intent] || 0), 0);
    const enabled = layers.includes(layer);
    const weighted = enabled ? Math.round(intentTotal * layer.density * layer.opacity) : 0;
    const count = layer.type === "fill" || layer.type === "tones"
      ? Math.max(1, Math.round(weighted * Math.max(0.05, layer.fillCoverage)))
      : weighted;
    layerCounts[layer.id] = count;

    if (!enabled) {
      continue;
    }

    if (layer.type === "fill" || layer.type === "tones") {
      toneOutput += Math.max(1, Math.round(intentTotal * layer.fillCoverage * layer.opacity));
    } else {
      strokeOutput += weighted;
      if (layer.type === "shading") {
        toneOutput += Math.round(weighted * Math.max(0.1, layer.fillCoverage));
      }
    }
  }

  const intentVisibility = Object.fromEntries(intentNames.map(intent => [
    intent,
    layers.some(layer => layer.type !== "fill" && layerHasIntent(layer, intent)),
  ]));

  return {
    activeLayers: layers,
    layerCounts,
    visibleLayerCount: layers.length,
    strokeOutput,
    toneOutput,
    intentVisibility,
  };
}

function computeMetrics() {
  let vertices = 0;
  let sourceTriangles = 0;
  let meshCount = 0;

  for (const entity of state.entities) {
    const asset = assetById(entity.meshId);
    if (!asset) continue;
    meshCount += 1;
    vertices += asset.vertices;
    sourceTriangles += asset.triangles;
  }

  if (meshCount === 0 || sourceTriangles === 0) {
    const intentCounts = Object.fromEntries(intentNames.map(intent => [intent, 0]));
    const layerStats = computeLayerStats(intentCounts);
    return {
      meshCount,
      vertices,
      sourceTriangles,
      triangles: 0,
      topologyEdges: 0,
      projectedEdges: 0,
      projectedVertices: vertices,
      rawVisibleFaces: 0,
      lineVisibleFaces: 0,
      lineVisibleMismatch: 0,
      fragments: 0,
      paths: 0,
      simplifiedPaths: 0,
      drawablePaths: 0,
      finalStrokes: 0,
      rawFeatureLines: 0,
      hiddenCandidates: 0,
      featureLines: 0,
      surfaceSamples: 0,
      surfaceFlow: 0,
      hatch: 0,
      accent: 0,
      strokeCandidates: 0,
      strokes: 0,
      visibleStrokes: 0,
      intentCounts,
      layerStats,
    };
  }

  const triangles = sourceTriangles;
  const topologyEdges = Math.round(sourceTriangles * 3);
  const projectedEdges = topologyEdges;

  const enabledKindWeight =
    (state.settings.showSilhouette ? 1 : 0) +
    (state.settings.showBoundary ? 1 : 0) +
    (state.settings.showFeature ? 1 : 0);
  const featureAngleFactor = clamp(1 + (34 - state.settings.featureAngleDegrees) / 120, 0.72, 1.25);
  const strideFactor = 1 / Math.max(1, state.settings.meshStride);
  const visibilityFactor = state.settings.occlusionCulling ? 1 : 1.32;
  const minSegFactor = clamp(1 - state.settings.minSegPx * 0.035, 0.65, 1);
  const depthScaleFactor = clamp(Math.sqrt(state.settings.depthScale), 0.55, 1.18);
  const fragmentBase = sourceTriangles * 1.735;
  const fragments = Math.max(0, Math.round(
    fragmentBase *
    featureAngleFactor *
    strideFactor *
    visibilityFactor *
    minSegFactor *
    depthScaleFactor *
    (enabledKindWeight / 3)));

  const simplifyFactor = clamp(1 - state.settings.pathSimplify * 0.22, 0.62, 1);
  const paths = Math.max(0, Math.round(fragments * 0.181 * simplifyFactor));
  const simplifiedPaths = paths;
  const drawablePaths = Math.max(0, Math.round(paths * clamp(state.settings.drawProgress, 0, 1)));
  const finalStrokes = Math.max(0, Math.round(drawablePaths * 9.22));

  const rawVisibleFaces = state.settings.occlusionCulling
    ? Math.round(sourceTriangles * 0.61 * depthScaleFactor)
    : sourceTriangles;
  const lineVisibleFaces = state.settings.occlusionCulling
    ? Math.max(0, rawVisibleFaces - Math.round(sourceTriangles * 0.002))
    : sourceTriangles;
  const lineVisibleMismatch = Math.abs(rawVisibleFaces - lineVisibleFaces);

  const silhouette = state.settings.showSilhouette ? Math.round(fragments * 0.34) : 0;
  const boundary = state.settings.showBoundary ? Math.round(fragments * 0.22) : 0;
  const feature = state.settings.showFeature ? Math.round(fragments * 0.28) : 0;
  const crease = state.settings.showFeature ? Math.round(fragments * 0.1) : 0;
  const accent = Math.round((silhouette + crease) * 0.04);
  const surfaceFlow = Math.round(paths * 0.18);
  const hatch = state.presetId === "pen-ink-hatching"
    ? Math.round(paths * 0.85)
    : Math.round(paths * 0.22);
  const fill = Math.round(rawVisibleFaces * 0.08);
  const tones = Math.round(rawVisibleFaces * 0.14);
  const rawFeatureLines = fragments;
  const hiddenCandidates = state.settings.occlusionCulling
    ? Math.max(0, projectedEdges - fragments)
    : 0;
  const featureLines = fragments;
  const surfaceSamples = rawVisibleFaces;

  const intentCounts = {
    Silhouette: silhouette,
    Boundary: boundary,
    Feature: feature,
    Crease: crease,
    SurfaceFlow: Math.max(0, surfaceFlow),
    Hatch: Math.max(0, hatch),
    Accent: Math.max(0, accent),
    Fill: Math.max(0, fill),
    Tones: Math.max(0, tones),
  };

  const strokeCandidates = Object.values(intentCounts).reduce((sum, count) => sum + count, 0);
  const layerStats = computeLayerStats(intentCounts);
  const strokes = finalStrokes;
  const visibleStrokes = finalStrokes;

  return {
    meshCount,
    vertices,
    sourceTriangles,
    triangles,
    topologyEdges,
    projectedEdges,
    projectedVertices: vertices,
    rawVisibleFaces,
    lineVisibleFaces,
    lineVisibleMismatch,
    fragments,
    paths,
    simplifiedPaths,
    drawablePaths,
    finalStrokes,
    rawFeatureLines,
    hiddenCandidates,
    featureLines,
    surfaceSamples,
    surfaceFlow,
    hatch,
    accent,
    strokeCandidates,
    strokes,
    visibleStrokes,
    intentCounts,
    layerStats,
  };
}

function pipelineRows(metrics) {
  return [
    ["ProjectMeshStep", `entities ${state.entities.length}`, `vertices ${metrics.projectedVertices}`],
    ["BuildProjectedTrianglesStep", `vertices ${metrics.projectedVertices}`, `triangles ${metrics.triangles}`],
    ["BuildMeshTopologyStep", `triangles ${metrics.triangles}`, `edges ${metrics.topologyEdges}`],
    ["DefaultBuildFaceIdVisibilityBufferStep", `triangles ${metrics.triangles}`, `faces ${metrics.rawVisibleFaces}`],
    ["DefaultClassifyEdgesToFragmentsStep", `edges ${metrics.topologyEdges}`, `fragments ${metrics.fragments}`],
    ["DefaultBuildPathsFromFragmentsStep", `fragments ${metrics.fragments}`, `paths ${metrics.paths}`],
    ["DefaultSimplifyAndSortPathsStep", `paths ${metrics.paths}`, `simplified ${metrics.simplifiedPaths}`],
    ["DefaultApplyDrawProgressStep", `progress ${state.settings.drawProgress.toFixed(2)}`, `drawable ${metrics.drawablePaths}`],
    ["DefaultBuildInkFrameStep", `paths ${metrics.drawablePaths}`, `strokes ${metrics.finalStrokes}`],
    ["DefaultBuildDebugFrameStep", `graph ${metrics.fragments}`, `overlays ${Object.values(state.overlays).filter(Boolean).length}`],
  ];
}

function graphHash(metrics) {
  const source = [
    state.presetId,
    state.mode,
    state.settings.seed,
    state.settings.showSilhouette,
    state.settings.showFeature,
    state.settings.showBoundary,
    state.settings.featureAngleDegrees.toFixed(1),
    state.settings.meshStride,
    state.settings.pathSimplify.toFixed(2),
    state.settings.drawProgress.toFixed(2),
    state.settings.lineWidth.toFixed(2),
    state.stableRandom ? "stable" : "live",
    metrics.vertices,
    metrics.triangles,
    metrics.fragments,
    metrics.finalStrokes,
    ...state.strokeLayers.map(layer => [
      layer.id,
      layer.type,
      layer.visible,
      layer.solo,
      layer.locked,
      layer.blend,
      layer.opacity.toFixed(2),
      layer.density.toFixed(2),
      layer.intents.join(","),
    ].join(":")),
  ].join("|");

  let hash = 2166136261;
  for (let index = 0; index < source.length; index += 1) {
    hash ^= source.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }

  return (hash >>> 0).toString(16).padStart(8, "0").slice(0, 6);
}

function resizeCanvas() {
  const rect = canvas.getBoundingClientRect();
  const scale = window.devicePixelRatio || 1;
  canvas.width = Math.max(1, Math.floor(rect.width * scale));
  canvas.height = Math.max(1, Math.floor(rect.height * scale));
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  q("#viewportSize").textContent = `${Math.round(rect.width)} x ${Math.round(rect.height)}`;
  draw();
}

function drawGrid(width, height) {
  if (!state.showGrid) return;
  ctx.lineWidth = 1;
  for (let x = 0; x < width; x += 24) {
    ctx.strokeStyle = x % 96 === 0 ? "#c5cec2" : "#dce2d9";
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, height);
    ctx.stroke();
  }
  for (let y = 0; y < height; y += 24) {
    ctx.strokeStyle = y % 96 === 0 ? "#c5cec2" : "#dce2d9";
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
    ctx.stroke();
  }
}

function suzannePaths(width, height) {
  const cx = width * 0.5 + state.camera.target.x * 18;
  const cy = height * 0.48 - state.camera.target.y * 18;
  const fovScale = clamp(60 / state.camera.fov, 0.45, 1.9);
  const s = Math.min(width, height) * 0.34 * fovScale;
  const yaw = state.camera.orbitYaw * Math.PI / 180;
  const pitch = state.camera.orbitPitch * Math.PI / 180;
  const squash = 0.9 + Math.cos(yaw) * 0.08;
  const yScale = 1 + Math.sin(pitch) * 0.12;

  const paths = [
    ["Silhouette", [[-0.46, -0.34], [-0.70, -0.06], [-0.64, 0.34], [-0.26, 0.54], [0.26, 0.54], [0.64, 0.34], [0.70, -0.06], [0.46, -0.34], [0.10, -0.44], [-0.28, -0.42], [-0.46, -0.34]]],
    ["Boundary", [[-0.52, -0.04], [-0.96, -0.10], [-1.18, 0.14], [-1.04, 0.38], [-0.64, 0.34]]],
    ["Boundary", [[0.52, -0.04], [0.96, -0.10], [1.18, 0.14], [1.04, 0.38], [0.64, 0.34]]],
    ["Crease", [[-0.32, 0.04], [-0.14, -0.04], [0.04, 0.02]]],
    ["Crease", [[0.18, 0.02], [0.38, -0.04], [0.54, 0.08]]],
    ["Crease", [[-0.12, 0.28], [0.08, 0.34], [0.30, 0.28]]],
    ["Accent", [[-0.22, 0.64], [0.00, 0.72], [0.26, 0.62]]],
    ["SurfaceFlow", [[-0.38, -0.20], [-0.10, -0.12], [0.20, -0.16], [0.48, -0.26]]],
    ["SurfaceFlow", [[-0.54, 0.22], [-0.22, 0.34], [0.18, 0.36], [0.52, 0.18]]],
  ];

  return paths.map(([intent, points]) => ({
    intent,
    points: points.map(([x, y]) => [cx + x * s * squash, cy + y * s * yScale]),
  }));
}

function drawPolyline(points, style) {
  if (points.length < 2) return;
  ctx.save();
  ctx.strokeStyle = style.color;
  ctx.globalAlpha = style.alpha;
  ctx.lineWidth = style.width;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  if (style.dash) ctx.setLineDash(style.dash);
  ctx.beginPath();
  ctx.moveTo(points[0][0], points[0][1]);
  for (let index = 1; index < points.length; index += 1) {
    ctx.lineTo(points[index][0], points[index][1]);
  }
  ctx.stroke();
  ctx.restore();
}

function drawMesh(width, height) {
  const paths = suzannePaths(width, height);
  for (const path of paths) {
    drawPolyline(path.points, { color: "#1b1d19", alpha: 0.95, width: 1 });
  }

  const cx = width * 0.5;
  const cy = height * 0.48;
  const s = Math.min(width, height) * 0.34 * clamp(60 / state.camera.fov, 0.45, 1.9);
  ctx.strokeStyle = "rgba(33, 39, 31, .22)";
  ctx.lineWidth = 1;
  for (let index = -6; index <= 6; index += 1) {
    ctx.beginPath();
    ctx.moveTo(cx - s * 0.72, cy + index * s * 0.12);
    ctx.lineTo(cx + s * 0.72, cy - index * s * 0.10);
    ctx.stroke();
  }
}

function drawNpr(width, height, metrics) {
  const seed = state.stableRandom ? state.settings.seed : Date.now();
  const random = seeded(seed + Math.round(state.camera.orbitYaw * 13) + Math.round(state.camera.orbitPitch * 7));
  const paths = suzannePaths(width, height);
  const intentWeights = {
    Silhouette: 1.65,
    Boundary: 1.28,
    Feature: 0.98,
    Crease: 1.02,
    SurfaceFlow: 0.62,
    Hatch: 0.5,
    Accent: 1.2,
    Fill: 0,
    Tones: 0,
  };
  const layers = metrics.layerStats.activeLayers;
  const cx = width * 0.5;
  const cy = height * 0.52;
  const modelScale = Math.min(width, height) * 0.31 * clamp(60 / state.camera.fov, 0.45, 1.9);

  for (const layer of layers.filter(item => item.type === "fill" || item.type === "tones")) {
    ctx.save();
    ctx.globalAlpha = clamp(layer.opacity * layer.fillCoverage, 0, 0.8);
    ctx.fillStyle = layer.color;
    ctx.beginPath();
    ctx.ellipse(cx, cy + modelScale * 0.12, modelScale * 1.02, modelScale * 0.74, -0.06, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }

  for (const layer of layers.filter(item => item.type === "shading")) {
    const hatchBase = layer.intents.reduce((sum, intent) => sum + (metrics.intentCounts[intent] || 0), 0);
    const hatchCount = Math.min(220, Math.round(hatchBase * layer.density * layer.opacity * 0.7));
    for (let index = 0; index < hatchCount; index += 1) {
      const angleSeed = random() * Math.PI * 2;
      const radius = Math.sqrt(random()) * modelScale * 0.9;
      const x = cx + Math.cos(angleSeed) * radius * 0.95;
      const y = cy + Math.sin(angleSeed) * radius * 0.68;
      const len = state.settings.lineWidth * 8.5 * (0.55 + random() * 0.65) * clamp(layer.density, 0.35, 1.7);
      const angle = -0.72 + (random() - 0.5) * 0.42;
      const dx = Math.cos(angle) * len;
      const dy = Math.sin(angle) * len;
      drawPolyline([[x - dx, y - dy], [x + dx, y + dy]], {
        color: layer.color,
        alpha: clamp(layer.opacity * (0.16 + layer.density * 0.28), 0.05, 0.68),
        width: Math.max(0.45, layer.style.baseThickness),
      });
    }
  }

  for (const layer of layers.filter(item => item.type === "strokes")) {
    for (const path of paths) {
      if (!layerHasIntent(layer, path.intent)) continue;
      const jitter = path.points.map(([x, y], index) => {
        const endPoint = index === 0 || index === path.points.length - 1;
        const amount = layer.style.endpointJitter * (endPoint ? 2.0 : 1.15);
        return [
          x + (random() - 0.5) * amount,
          y + (random() - 0.5) * amount,
        ];
      });
      drawPolyline(jitter, {
        color: layer.color || intentColors[path.intent],
        alpha: clamp(layer.opacity * (path.intent === "SurfaceFlow" ? 0.42 : 0.88), 0.05, 1),
        width: Math.max(0.5, layer.style.baseThickness * intentWeights[path.intent]),
      });
    }
  }
}

function drawTriangleOverlay(width, height) {
  const cx = width * 0.5;
  const cy = height * 0.5;
  const s = Math.min(width, height) * 0.2;
  ctx.save();
  ctx.strokeStyle = "rgba(46, 101, 125, .32)";
  ctx.fillStyle = "rgba(46, 101, 125, .055)";
  ctx.lineWidth = 1;
  for (let index = 0; index < 10; index += 1) {
    const offset = (index - 5) * s * 0.13;
    ctx.beginPath();
    ctx.moveTo(cx - s + offset, cy - s * 0.62);
    ctx.lineTo(cx + s * 0.78 + offset, cy - s * 0.45 + index * 2);
    ctx.lineTo(cx + offset * 0.2, cy + s * 0.7);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
  }
  ctx.restore();
}

function drawSurfaceSamples(width, height, metrics) {
  const random = seeded(state.settings.seed + 91);
  const cx = width * 0.5;
  const cy = height * 0.55;
  const s = Math.min(width, height) * 0.27;
  const count = Math.min(140, Math.round(metrics.surfaceSamples / 4));
  ctx.save();
  ctx.fillStyle = "rgba(52, 114, 87, .62)";
  for (let index = 0; index < count; index += 1) {
    const angle = random() * Math.PI * 2;
    const radius = Math.sqrt(random()) * s;
    const x = cx + Math.cos(angle) * radius * 1.1;
    const y = cy + Math.sin(angle) * radius * 0.72;
    ctx.beginPath();
    ctx.arc(x, y, 2, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
}

function drawOverlays(width, height, metrics) {
  const paths = suzannePaths(width, height);
  if (state.overlays.projectedTriangles) {
    drawTriangleOverlay(width, height);
  }

  if (state.overlays.topologyEdges) {
    for (const path of paths) {
      drawPolyline(path.points, { color: "#2e657d", alpha: 0.28, width: 1, dash: [4, 4] });
    }
  }

  if (state.overlays.featureLines) {
    for (const path of paths.filter(item => item.intent !== "SurfaceFlow")) {
      drawPolyline(path.points, { color: "#347257", alpha: 0.28, width: 2.4 });
    }
  }

  if (state.overlays.hiddenCandidates) {
    const cx = width * 0.5;
    const cy = height * 0.48;
    drawPolyline([[cx - 210, cy + 58], [cx + 196, cy + 24]], { color: "#8a4e45", alpha: 0.55, width: 1.2, dash: [8, 5] });
    drawPolyline([[cx - 182, cy - 34], [cx + 160, cy + 74]], { color: "#8a4e45", alpha: 0.38, width: 1.2, dash: [8, 5] });
  }

  if (state.overlays.surfaceSamples) {
    drawSurfaceSamples(width, height, metrics);
  }

  if (state.overlays.projectedVertices) {
    ctx.save();
    ctx.fillStyle = "rgba(138, 104, 34, .75)";
    for (const path of paths) {
      for (const [x, y] of path.points) {
        ctx.beginPath();
        ctx.arc(x, y, 2.5, 0, Math.PI * 2);
        ctx.fill();
      }
    }
    ctx.restore();
  }
}

function draw() {
  const rect = canvas.getBoundingClientRect();
  const width = rect.width;
  const height = rect.height;
  const metrics = computeMetrics();
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = "#f0f2ee";
  ctx.fillRect(0, 0, width, height);
  drawGrid(width, height);

  if (state.mode === "mesh") {
    drawMesh(width, height);
  } else {
    if (state.overlays.finalStrokes) {
      drawNpr(width, height, metrics);
    }
  }

  drawOverlays(width, height, metrics);
}

function setTab(name) {
  state.tab = name;
  qa(".tab").forEach(tab => tab.classList.toggle("is-active", tab.dataset.tab === name));
  qa(".panel-section").forEach(panel => panel.classList.toggle("is-active", panel.dataset.panel === name));
  q("#panelTitle").textContent = panels[name][0];
  q("#panelSubtitle").textContent = panels[name][1];
}

function setMode(mode, shouldLog = true) {
  state.mode = mode;
  qa("[data-mode]").forEach(button => button.classList.toggle("is-active", button.dataset.mode === mode));
  q("#modeBadge").textContent = mode.toUpperCase();
  q("#renderModeLabel").textContent = mode === "npr" ? "ViewportRenderMode.Npr" : "ViewportRenderMode.Mesh";
  if (shouldLog) {
    pushCommand(`SetViewportRenderModeCommand(ViewportRenderMode.${mode === "npr" ? "Npr" : "Mesh"})`);
  }
  refresh();
}

function syncSliders() {
  for (const def of sliderDefs) {
    const input = q(`#${def.id}`);
    const output = q(`#${def.output}`);
    if (!input || !output) continue;
    input.value = String(Math.round(def.toControl(def.get())));
    input.disabled = def.id.startsWith("layer") && Boolean(selectedLayer()?.locked);
    output.textContent = def.label(def.get());
  }
}

function syncVectorInputs() {
  const entity = selectedEntity();
  const entityPosition = entity?.position || { x: 0, y: 0, z: 0 };
  q("#entityPosX").value = formatScalar(entityPosition.x);
  q("#entityPosY").value = formatScalar(entityPosition.y);
  q("#entityPosZ").value = formatScalar(entityPosition.z);
  q("#selectedEntityLabel").textContent = entity ? `EntityId(${entity.id})` : "EntityId.None";
  q("#entityRoleSelect").value = entity?.role || "Foreground";

  q("#cameraPosX").value = formatScalar(state.camera.position.x);
  q("#cameraPosY").value = formatScalar(state.camera.position.y);
  q("#cameraPosZ").value = formatScalar(state.camera.position.z);
  q("#cameraTargetX").value = formatScalar(state.camera.target.x);
  q("#cameraTargetY").value = formatScalar(state.camera.target.y);
  q("#cameraTargetZ").value = formatScalar(state.camera.target.z);
}

function syncControls() {
  q("#presetSelect").value = state.presetId;
  q("#seedInput").value = String(state.settings.seed);
  q("#stableRandom").checked = state.stableRandom;
  q("#showGrid").checked = state.showGrid;
  q("#showSilhouette").checked = state.settings.showSilhouette;
  q("#showFeature").checked = state.settings.showFeature;
  q("#showBoundary").checked = state.settings.showBoundary;
  q("#occlusionCulling").checked = state.settings.occlusionCulling;
  q("#autoDraw").checked = state.settings.autoDraw;
  qa("[data-overlay]").forEach(input => {
    input.checked = Boolean(state.overlays[input.dataset.overlay]);
  });
  syncSliders();
  syncVectorInputs();
}

function renderAssets() {
  q("#assetList").innerHTML = assets.map(asset => {
    const selected = asset.id === state.selectedAssetId ? " is-selected" : "";
    return `
      <button class="asset-row${selected}" type="button" data-asset-id="${asset.id}">
        <span class="asset-title">
          <strong>${asset.path}</strong>
          <code>MeshHandle(${asset.handle})</code>
        </span>
        <span class="asset-meta">
          <span>${asset.vertices} vertices</span>
          <span>${asset.triangles} triangles</span>
          <span class="loader-pill">${asset.loader}</span>
          <span>${asset.status}</span>
        </span>
      </button>
    `;
  }).join("");
  q("#loaderStatus").textContent = state.loaderStatus;
}

function renderEntities() {
  q("#sceneSummary").textContent = `${state.entities.length} ${state.entities.length === 1 ? "entity" : "entities"}`;
  q("#entityList").innerHTML = state.entities.map(entity => {
    const selected = entity.id === state.selectedEntityId ? " is-selected" : "";
    const asset = assetById(entity.meshId);
    const meshLabel = asset ? `MeshHandle(${asset.handle})` : "MeshHandle.None";
    return `
      <button class="entity-row${selected}" type="button" data-entity-id="${entity.id}">
        <span class="entity-title">
          <strong>${entity.name}</strong>
          <code>EntityId(${entity.id})</code>
        </span>
        <span class="entity-meta">
          <span>${meshLabel}</span>
          <span class="role-pill">${entity.role || "Foreground"}</span>
          <span>Position ${formatVector(entity.position)}</span>
        </span>
      </button>
    `;
  }).join("");
}

function renderPresetMetadata() {
  const preset = presetDefinitions[state.presetId];
  q("#activePreset").textContent = preset.id;
  q("#presetEditBadge").textContent = preset.editable ? "editable" : "locked";
  q("#presetId").textContent = preset.id;
  q("#presetName").textContent = preset.name;
  q("#presetEditable").textContent = String(preset.editable).toLowerCase();
  q("#presetPipelineId").textContent = preset.pipelineId;
  q("#presetProvider").textContent = preset.pipelineProvider;
  q("#presetDescription").textContent = preset.description;
}

function renderLayerStack(metrics) {
  q("#layerStackSummary").textContent = `${state.strokeLayers.length} layers, ${metrics.layerStats.visibleLayerCount} visible`;
  q("#layerStack").innerHTML = state.strokeLayers.map((layer, index) => {
    const selected = layer.id === state.selectedLayerId ? " is-selected" : "";
    const count = metrics.layerStats.layerCounts[layer.id] || 0;
    return `
      <button class="layer-row${selected}" type="button" data-layer-id="${layer.id}" data-layer-type="${layer.type}">
        <span class="layer-title">
          <strong><i class="layer-swatch" style="background:${layer.color}"></i>${index + 1}. ${layer.name}</strong>
          <code>${layerTypeLabels[layer.type]}</code>
        </span>
        <span class="layer-meta">
          <span class="role-pill">${layer.role || "Style"}</span>
          <span>${layer.intents.join(" + ") || "no intents"}</span>
          <span>${count} output</span>
          <span>${layer.blend}, opacity ${layer.opacity.toFixed(2)}</span>
        </span>
        <span class="layer-controls">
          <span class="mini-button${layer.visible ? " is-on" : ""}" data-layer-action="visible">${layer.visible ? "VIS" : "HID"}</span>
          <span class="mini-button${layer.solo ? " is-on" : ""}" data-layer-action="solo">SOLO</span>
          <span class="mini-button${layer.locked ? " is-on" : ""}" data-layer-action="lock">LOCK</span>
        </span>
      </button>
    `;
  }).join("");
}

function renderLayerIntentInputs(metrics) {
  const layer = selectedLayer();
  q("#layerIntentList").innerHTML = intentNames.map(intent => {
    const checked = layer?.intents.includes(intent) ? " checked" : "";
    const disabled = layer?.locked ? " disabled" : "";
    const count = metrics.intentCounts[intent] || 0;
    return `
      <div class="intent-row">
        <label>
          <input type="checkbox" data-layer-intent="${intent}"${checked}${disabled}>
          <i class="intent-swatch" style="background:${intentColors[intent]}"></i>
          <span>${intent}</span>
        </label>
        <span class="intent-count">${count}</span>
      </div>
    `;
  }).join("");
}

function renderLayerEditor(metrics) {
  const layer = selectedLayer();
  const disabled = !layer;
  q("#activeLayerId").textContent = layer ? layer.id : "NprLayerId.None";
  q("#layerNameInput").value = layer?.name || "";
  q("#layerNameInput").disabled = disabled || layer.locked;
  q("#layerTypeSelect").value = layer?.type || "strokes";
  q("#layerTypeSelect").disabled = disabled || layer.locked;
  q("#layerBlendSelect").value = layer?.blend || "normal";
  q("#layerBlendSelect").disabled = disabled || layer.locked;
  q("#deleteLayer").disabled = disabled || layer.locked;
  q("#duplicateLayer").disabled = disabled;
  q("#visibleLayerCount").textContent = String(metrics.layerStats.visibleLayerCount);
  q("#layerStrokeCount").textContent = String(metrics.layerStats.strokeOutput);
  q("#layerToneCount").textContent = String(metrics.layerStats.toneOutput);
  q("#layerPreviewSummary").textContent = layer
    ? `${layerTypeLabels[layer.type]} layer, ${metrics.layerStats.layerCounts[layer.id] || 0} output`
    : "no active layer";
}

function previewMarksForLayer(layer, composite = false) {
  if (!layer) return "";
  const opacity = composite ? clamp(layer.opacity * 0.78, 0.05, 1) : layer.opacity;
  if (layer.type === "fill") {
    return `<i class="preview-fill" style="--c:${layer.color}; --o:${clamp(opacity * Math.max(0.1, layer.fillCoverage), 0.05, 0.72)}"></i>`;
  }
  if (layer.type === "tones") {
    return `<i class="preview-tone" style="--c:${layer.color}; --o:${clamp(opacity * Math.max(0.12, layer.fillCoverage), 0.05, 0.64)}"></i>`;
  }
  if (layer.type === "shading") {
    return `<i class="preview-shade" style="--c:${layer.color}; --o:${clamp(opacity * Math.max(0.18, layer.density), 0.08, 0.72)}"></i>`;
  }

  const marks = [
    ["12%", "25%", "62%", `${Math.max(1, layer.style.baseThickness * 2.2)}px`, "-5deg"],
    ["20%", "52%", "48%", `${Math.max(1, layer.style.baseThickness * 1.45)}px`, "4deg"],
    ["48%", "36%", "36%", `${Math.max(1, layer.style.baseThickness * 1.05)}px`, "-18deg"],
  ];

  return marks.map(([x, y, w, h, rotate]) => `
    <i class="preview-mark" style="--x:${x}; --y:${y}; --w:${w}; --h:${h}; --r:${rotate}; --c:${layer.color}; --o:${opacity}"></i>
  `).join("");
}

function renderLayerPreview() {
  const layer = selectedLayer();
  q("#activeLayerPreview").innerHTML = previewMarksForLayer(layer);
  q("#compositeLayerPreview").innerHTML = activeLayers().map(item => previewMarksForLayer(item, true)).join("");
}

function renderGraphCounters(metrics, hash) {
  const counters = [
    ["meshes", metrics.meshCount],
    ["vertices", metrics.vertices],
    ["triangles", metrics.triangles],
    ["topology edges", metrics.topologyEdges],
    ["edge fragments", metrics.fragments],
    ["paths", metrics.paths],
    ["drawable paths", metrics.drawablePaths],
    ["final strokes", metrics.finalStrokes],
    ["raw visible faces", metrics.rawVisibleFaces],
    ["line visible faces", metrics.lineVisibleFaces],
  ];

  q("#graphCounters").innerHTML = counters.map(([label, value]) => `
    <div><span>${label}</span><strong>${value}</strong></div>
  `).join("");

  q("#graphHash").textContent = `graph ${hash}`;
  q("#graphHashDebug").textContent = hash;
  q("#debugSeed").textContent = String(state.settings.seed);
  q("#debugDeterminism").textContent = state.stableRandom ? "deterministic" : "live random";
  q("#determinismStatus").textContent = state.stableRandom ? "stable" : "unstable";
}

function renderParitySummary(metrics) {
  const preset = presetDefinitions[state.presetId];
  const values = [
    ["Pipeline", `${preset.pipelineId} / 10 steps`],
    ["Face ownership", `${metrics.lineVisibleFaces}/${metrics.rawVisibleFaces}`],
    ["Visibility mismatch", metrics.lineVisibleMismatch],
    ["Path quantization", "2.5 px"],
    ["RDP epsilon", state.settings.pathSimplify.toFixed(2)],
    ["Draw progress", state.settings.drawProgress.toFixed(2)],
    ["Ink passes", state.presetId === "pencil-construction" ? "3 sketch" : "2 comic"],
    ["Frame output", `${metrics.finalStrokes} StrokePath2D`],
  ];

  q("#paritySummary").innerHTML = values.map(([label, value]) => `
    <div><span>${label}</span><strong>${value}</strong></div>
  `).join("");
}

function renderPipeline(metrics) {
  const rows = pipelineRows(metrics);
  q("#pipelineList").innerHTML = rows.map(([name, input, output], index) => `
    <div class="pipeline-row">
      <strong>${index + 1}. ${name}</strong>
      <span>${input}</span>
      <span>${output}</span>
    </div>
  `).join("");

  q("#pipelineTrace").innerHTML = rows.map(([name, input, output], index) => {
    const timing = (0.18 + index * 0.07 + metrics.strokes * 0.0007).toFixed(2);
    return `
      <div class="trace-row">
        <strong>${name}</strong>
        <span>${input} -> ${output}</span>
        <span class="timing">mock ${timing} ms</span>
      </div>
    `;
  }).join("");
}

function renderCommandLogs() {
  const html = state.commandLog.map(entry => `
    <li><strong>${entry.time}</strong> ${entry.text}</li>
  `).join("");
  q("#commandLogGeneral").innerHTML = html;
  q("#commandLogDebug").innerHTML = html;
}

function renderStatus(metrics, hash) {
  q("#cameraCompact").textContent = `Camera ${formatVector(state.camera.position)} -> ${formatVector(state.camera.target)}, ${formatScalar(state.camera.fov)} deg`;
  q("#graphHash").textContent = `graph ${hash}`;
  if (state.commandLog[0]) {
    q("#lastCommand").textContent = state.commandLog[0].text;
  }
}

function refresh() {
  const metrics = computeMetrics();
  const hash = graphHash(metrics);
  renderAssets();
  renderEntities();
  renderPresetMetadata();
  renderLayerStack(metrics);
  renderLayerIntentInputs(metrics);
  renderLayerEditor(metrics);
  renderLayerPreview();
  renderGraphCounters(metrics, hash);
  renderParitySummary(metrics);
  renderPipeline(metrics);
  renderCommandLogs();
  renderStatus(metrics, hash);
  draw();
}

function bindSlider(def) {
  const input = q(`#${def.id}`);
  const output = q(`#${def.output}`);
  if (!input || !output) return;

  input.addEventListener("input", () => {
    def.set(def.fromControl(Number(input.value)));
    output.textContent = def.label(def.get());
    refresh();
  });

  input.addEventListener("change", () => {
    pushCommand(def.command());
  });
}

function applyPreset(id) {
  const preset = presetDefinitions[id];
  if (!preset) return;
  state.presetId = id;
  state.settings = structuredClone(preset.settings);
  state.strokeStyle = structuredClone(preset.strokeStyle);
  state.strokeLayers = createPresetLayers(preset);
  state.selectedLayerId = state.strokeLayers[0]?.id || null;
  state.nextLayerId = 1;
  state.settings.seed = preset.settings.seed;
  state.strokeStyle.seed = preset.strokeStyle.seed;
  syncControls();
  pushCommand(`NprPipelineRegistry.Resolve(${preset.pipelineId}) -> ${preset.pipelineProvider}`);
  pushCommand(`ActiveNprPresetState.ApplyPreset(${id})`);
  refresh();
}

function createCustomLayer(type = "strokes") {
  const id = `custom-layer:${state.nextLayerId++}`;
  const base = state.strokeStyle;
  const typeDefaults = {
    strokes: {
      name: `Stroke Layer ${state.nextLayerId - 1}`,
      intents: ["Accent"],
      color: intentColors.Accent,
      opacity: 0.82,
      density: 0.7,
      fillCoverage: 0,
      blend: "normal",
    },
    fill: {
      name: `Fill Layer ${state.nextLayerId - 1}`,
      intents: ["SurfaceFlow", "Hatch"],
      color: "#a6ada2",
      opacity: 0.25,
      density: 0.45,
      fillCoverage: 0.38,
      blend: "multiply",
    },
    shading: {
      name: `Shading Layer ${state.nextLayerId - 1}`,
      intents: ["Hatch"],
      color: intentColors.Hatch,
      opacity: 0.48,
      density: 0.62,
      fillCoverage: 0.16,
      blend: "multiply",
    },
    tones: {
      name: `Tone Layer ${state.nextLayerId - 1}`,
      intents: ["Tones", "Fill"],
      color: intentColors.Tones,
      opacity: 0.36,
      density: 0.55,
      fillCoverage: 0.24,
      blend: "multiply",
    },
  }[type];

  return {
    id,
    role: type === "fill" || type === "tones" ? "Background" : type === "shading" ? "Midground" : "Foreground",
    type,
    visible: true,
    solo: false,
    locked: false,
    shadeThreshold: 0.58,
    style: cloneStrokeStyle(base),
    ...typeDefaults,
  };
}

function addLayer(type = "strokes") {
  const layer = createCustomLayer(type);
  state.strokeLayers.push(layer);
  state.selectedLayerId = layer.id;
  pushCommand(`AddNprLayerCommand(${layer.id}, Type=${layerTypeLabels[layer.type]})`);
  syncControls();
  refresh();
}

function duplicateSelectedLayer() {
  const layer = selectedLayer();
  if (!layer) return;
  const clone = structuredClone(layer);
  clone.id = `custom-layer:${state.nextLayerId++}`;
  clone.name = `${layer.name} Copy`;
  clone.locked = false;
  const index = state.strokeLayers.indexOf(layer);
  state.strokeLayers.splice(index + 1, 0, clone);
  state.selectedLayerId = clone.id;
  pushCommand(`DuplicateNprLayerCommand("${layer.id}") -> "${clone.id}"`);
  syncControls();
  refresh();
}

function deleteSelectedLayer() {
  const layer = selectedLayer();
  if (!layer || layer.locked) return;
  state.strokeLayers = state.strokeLayers.filter(candidate => candidate.id !== layer.id);
  state.selectedLayerId = state.strokeLayers[0]?.id || null;
  pushCommand(`DeleteNprLayerCommand("${layer.id}")`);
  syncControls();
  refresh();
}

function setSelectedLayerType(type) {
  const layer = selectedLayer();
  if (!layer || layer.locked) return;
  layer.type = type;
  if (type === "fill") {
    layer.blend = layer.blend === "normal" ? "multiply" : layer.blend;
    layer.fillCoverage = Math.max(layer.fillCoverage, 0.25);
  }
  if (type === "shading") {
    layer.blend = layer.blend === "normal" ? "multiply" : layer.blend;
    layer.intents = layer.intents.length > 0 ? layer.intents : ["Hatch"];
  }
  if (type === "tones") {
    layer.blend = layer.blend === "normal" ? "multiply" : layer.blend;
    layer.intents = layer.intents.length > 0 ? layer.intents : ["Tones"];
  }
  pushCommand(`UpdateNprLayerCommand("${layer.id}", Type=${layerTypeLabels[type]})`);
  syncControls();
  refresh();
}

function updateEntityPosition(axis, value) {
  const entity = selectedEntity();
  if (!entity) return;
  entity.position[axis] = Number.isFinite(value) ? value : 0;
  refresh();
}

function updateCameraVector(kind, axis, value) {
  state.camera[kind][axis] = Number.isFinite(value) ? value : 0;
  refresh();
}

function resetCameraState() {
  state.camera.position = { x: 0, y: 0, z: 4 };
  state.camera.target = { x: 0, y: 0, z: 0 };
  state.camera.fov = 45;
  state.camera.orbitYaw = 0;
  state.camera.orbitPitch = 0;
  syncControls();
  pushCommand("SetCameraCommand(CameraState.Default)");
  refresh();
}

function frameModel() {
  state.camera.position = { x: 0, y: 0.3, z: 3.2 };
  state.camera.target = { x: 0, y: 0.1, z: 0 };
  state.camera.fov = 52;
  syncControls();
  pushCommand(`SetCameraCommand(CameraState(Position=${formatVector(state.camera.position)}, Target=${formatVector(state.camera.target)}, FieldOfViewDegrees=${state.camera.fov}))`);
  refresh();
}

function bindEvents() {
  qa(".tab").forEach(tab => {
    tab.addEventListener("click", () => setTab(tab.dataset.tab));
  });

  qa("[data-mode]").forEach(button => {
    button.addEventListener("click", () => setMode(button.dataset.mode));
  });

  sliderDefs.forEach(bindSlider);

  q("#presetSelect").addEventListener("change", event => {
    applyPreset(event.target.value);
  });

  q("#stableRandom").addEventListener("change", event => {
    state.stableRandom = event.target.checked;
    pushCommand(`NprSettings deterministic seed ${state.stableRandom ? "enabled" : "disabled"}`);
    refresh();
  });

  q("#showGrid").addEventListener("change", event => {
    state.showGrid = event.target.checked;
    refresh();
  });

  [
    ["showSilhouette", "showSilhouette", "ShowSilhouette"],
    ["showFeature", "showFeature", "ShowFeature"],
    ["showBoundary", "showBoundary", "ShowBoundary"],
    ["occlusionCulling", "occlusionCulling", "OcclusionCulling"],
    ["autoDraw", "autoDraw", "AutoDraw"],
  ].forEach(([id, setting, commandName]) => {
    q(`#${id}`).addEventListener("change", event => {
      state.settings[setting] = event.target.checked;
      pushCommand(`DefaultDrawingSettings.${commandName} = ${event.target.checked}`);
      refresh();
    });
  });

  q("#seedInput").addEventListener("input", event => {
    const seed = Number(event.target.value) || 0;
    state.settings.seed = seed;
    state.strokeStyle.seed = seed;
    refresh();
  });

  q("#seedInput").addEventListener("change", () => {
    pushCommand(`DefaultDrawingSettings.Seed = ${state.settings.seed}`);
  });

  q("#assetList").addEventListener("click", event => {
    const row = event.target.closest("[data-asset-id]");
    if (!row) return;
    state.selectedAssetId = row.dataset.assetId;
    refresh();
  });

  q("#entityList").addEventListener("click", event => {
    const row = event.target.closest("[data-entity-id]");
    if (!row) return;
    state.selectedEntityId = Number(row.dataset.entityId);
    syncVectorInputs();
    refresh();
  });

  q("#loadAsset").addEventListener("click", () => {
    const asset = selectedAsset();
    asset.status = "Loaded";
    state.loaderStatus = `${asset.path} -> ${asset.loader}`;
    pushCommand(`LoadMeshCommand("${asset.path}") -> MeshHandle(${asset.handle})`);
    refresh();
  });

  q("#reloadAsset").addEventListener("click", () => {
    const asset = selectedAsset();
    asset.status = "Reloaded";
    state.loaderStatus = `reloaded ${asset.path}`;
    pushCommand(`Reload ${asset.path} via ${asset.loader}`);
    refresh();
  });

  q("#assignMesh").addEventListener("click", () => {
    const entity = selectedEntity();
    const asset = selectedAsset();
    if (!entity || !asset) return;
    entity.meshId = asset.id;
    pushCommand(`AssignMeshToEntityCommand(EntityId(${entity.id}), MeshHandle(${asset.handle}))`);
    refresh();
  });

  q("#createEntity").addEventListener("click", () => {
    const id = state.nextEntityId++;
    state.entities.push({
      id,
      name: `Entity ${id}`,
      meshId: null,
      role: "Foreground",
      position: { x: 0, y: 0, z: 0 },
    });
    state.selectedEntityId = id;
    pushCommand(`CreateEntityCommand("Entity ${id}")`);
    syncControls();
    refresh();
  });

  q("#deleteEntity").addEventListener("click", () => {
    const entity = selectedEntity();
    if (!entity) return;
    state.entities = state.entities.filter(candidate => candidate.id !== entity.id);
    state.selectedEntityId = state.entities[0]?.id || null;
    pushCommand(`DeleteEntityCommand(EntityId(${entity.id}))`);
    syncControls();
    refresh();
  });

  [
    ["entityPosX", "x"],
    ["entityPosY", "y"],
    ["entityPosZ", "z"],
  ].forEach(([id, axis]) => {
    const input = q(`#${id}`);
    input.addEventListener("input", () => updateEntityPosition(axis, Number(input.value)));
    input.addEventListener("change", () => {
      const entity = selectedEntity();
      if (!entity) return;
      pushCommand(`SetEntityPositionCommand(EntityId(${entity.id}), Vector3${formatVector(entity.position)})`);
    });
  });

  q("#entityRoleSelect").addEventListener("change", event => {
    const entity = selectedEntity();
    if (!entity) return;
    entity.role = event.target.value;
    pushCommand(`SetEntityStyleRoleCommand(EntityId(${entity.id}), NprSceneRole.${entity.role})`);
    refresh();
  });

  [
    ["cameraPosX", "position", "x"],
    ["cameraPosY", "position", "y"],
    ["cameraPosZ", "position", "z"],
    ["cameraTargetX", "target", "x"],
    ["cameraTargetY", "target", "y"],
    ["cameraTargetZ", "target", "z"],
  ].forEach(([id, kind, axis]) => {
    const input = q(`#${id}`);
    input.addEventListener("input", () => updateCameraVector(kind, axis, Number(input.value)));
    input.addEventListener("change", () => {
      pushCommand(`SetCameraCommand(CameraState(Position=${formatVector(state.camera.position)}, Target=${formatVector(state.camera.target)}, FieldOfViewDegrees=${state.camera.fov}))`);
    });
  });

  q("#orbitCommand").addEventListener("click", () => {
    state.camera.orbitYaw = clamp(state.camera.orbitYaw + 12, -75, 75);
    syncSliders();
    pushCommand("OrbitCameraCommand(DeltaYawRadians=0.21, DeltaPitchRadians=0)");
    refresh();
  });

  q("#panCommand").addEventListener("click", () => {
    state.camera.position.x += 0.15;
    state.camera.target.x += 0.15;
    state.camera.position.y += 0.05;
    state.camera.target.y += 0.05;
    syncVectorInputs();
    pushCommand("PanCameraCommand(DeltaRight=0.15, DeltaUp=0.05)");
    refresh();
  });

  q("#resetCamera").addEventListener("click", resetCameraState);
  q("#cameraResetPanel").addEventListener("click", resetCameraState);
  q("#frameModel").addEventListener("click", frameModel);

  q("#exportSvg").addEventListener("click", () => {
    setTab("debug");
    pushCommand("DefaultBuildInkFrameStep -> Export SVG");
  });

  q("#layerStack").addEventListener("click", event => {
    const row = event.target.closest("[data-layer-id]");
    if (!row) return;
    const action = event.target.closest("[data-layer-action]");
    const layer = state.strokeLayers.find(candidate => candidate.id === row.dataset.layerId);
    if (!layer) return;
    state.selectedLayerId = layer.id;

    if (action) {
      if (action.dataset.layerAction === "visible") layer.visible = !layer.visible;
      if (action.dataset.layerAction === "solo") layer.solo = !layer.solo;
      if (action.dataset.layerAction === "lock") layer.locked = !layer.locked;
      pushCommand(`UpdateNprLayerCommand("${layer.id}", ${action.dataset.layerAction})`);
    }

    syncControls();
    refresh();
  });

  q("#addLayer").addEventListener("click", () => {
    addLayer(q("#layerTypeSelect").value || "strokes");
  });

  q("#duplicateLayer").addEventListener("click", duplicateSelectedLayer);
  q("#deleteLayer").addEventListener("click", deleteSelectedLayer);

  q("#layerNameInput").addEventListener("input", event => {
    const layer = selectedLayer();
    if (!layer || layer.locked) return;
    layer.name = event.target.value;
    refresh();
  });

  q("#layerNameInput").addEventListener("change", () => {
    const layer = selectedLayer();
    if (!layer || layer.locked) return;
    pushCommand(`UpdateNprLayerCommand("${layer.id}", Name="${layer.name}")`);
  });

  q("#layerTypeSelect").addEventListener("change", event => {
    setSelectedLayerType(event.target.value);
  });

  q("#layerBlendSelect").addEventListener("change", event => {
    const layer = selectedLayer();
    if (!layer || layer.locked) return;
    layer.blend = event.target.value;
    pushCommand(`UpdateNprLayerCommand("${layer.id}", Blend=${layer.blend})`);
    refresh();
  });

  q("#layerIntentList").addEventListener("change", event => {
    const input = event.target.closest("[data-layer-intent]");
    const layer = selectedLayer();
    if (!input || !layer || layer.locked) return;
    if (input.checked && !layer.intents.includes(input.dataset.layerIntent)) {
      layer.intents.push(input.dataset.layerIntent);
    }
    if (!input.checked) {
      layer.intents = layer.intents.filter(intent => intent !== input.dataset.layerIntent);
    }
    pushCommand(`RouteNprStrokeIntentCommand("${layer.id}", ${input.dataset.layerIntent}, ${input.checked ? "on" : "off"})`);
    refresh();
  });

  qa("[data-overlay]").forEach(input => {
    input.addEventListener("change", () => {
      state.overlays[input.dataset.overlay] = input.checked;
      pushCommand(`Debug overlay ${input.dataset.overlay} ${input.checked ? "on" : "off"}`);
      refresh();
    });
  });

  q("#resetTab").addEventListener("click", () => {
    if (state.tab === "camera") {
      resetCameraState();
      return;
    }
    if (state.tab === "npr" || state.tab === "strokes" || state.tab === "general") {
      applyPreset(state.presetId);
      return;
    }
    pushCommand(`Reset ${state.tab} panel`);
  });

  q("#applySettings").addEventListener("click", () => {
    pushCommand("Apply active inspector state -> RequestRenderCommand()");
    refresh();
  });

  window.addEventListener("resize", resizeCanvas);
}

bindEvents();
syncControls();
setTab("load");
setMode("npr", false);
refresh();
resizeCanvas();
