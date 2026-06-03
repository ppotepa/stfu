const canvas = document.querySelector("#previewCanvas");
const ctx = canvas.getContext("2d");
const state = {
  mode: "npr",
  tab: "load",
  showGrid: true,
  fov: 60,
  orbit: 0,
  crease: 34,
  hiddenBias: 0.025,
  featureDensity: 0.86,
  hatchDensity: 0.48,
  jitter: 0.95,
  thickness: 1.28,
  overshoot: 1.85,
  seed: 1337,
};

const panels = {
  load: ["Load", "Assets and scene input."],
  general: ["General", "Preset and deterministic behavior."],
  camera: ["Camera", "Viewport camera controls."],
  npr: ["NPR", "Feature extraction and visibility."],
  strokes: ["Strokes", "Hatching, humanization and mark style."],
  debug: ["Debug", "Graph counters and pipeline output."],
};

function resizeCanvas() {
  const rect = canvas.getBoundingClientRect();
  const scale = window.devicePixelRatio || 1;
  canvas.width = Math.max(1, Math.floor(rect.width * scale));
  canvas.height = Math.max(1, Math.floor(rect.height * scale));
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  draw();
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

function drawGrid(width, height) {
  if (!state.showGrid) return;
  ctx.lineWidth = 1;
  for (let x = 0; x < width; x += 24) {
    ctx.strokeStyle = x % 96 === 0 ? "#c9c9c1" : "#deded8";
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, height);
    ctx.stroke();
  }
  for (let y = 0; y < height; y += 24) {
    ctx.strokeStyle = y % 96 === 0 ? "#c9c9c1" : "#deded8";
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
    ctx.stroke();
  }
}

function suzanneLines(width, height) {
  const cx = width * 0.5;
  const cy = height * 0.48;
  const s = Math.min(width, height) * 0.34;
  const orbit = state.orbit * Math.PI / 180;
  const squash = 0.9 + Math.cos(orbit) * 0.08;
  return [
    [[-0.46, -0.34], [-0.70, -0.06], [-0.64, 0.34], [-0.26, 0.54], [0.26, 0.54], [0.64, 0.34], [0.70, -0.06], [0.46, -0.34], [0.10, -0.44], [-0.28, -0.42], [-0.46, -0.34]],
    [[-0.52, -0.04], [-0.96, -0.10], [-1.18, 0.14], [-1.04, 0.38], [-0.64, 0.34]],
    [[0.52, -0.04], [0.96, -0.10], [1.18, 0.14], [1.04, 0.38], [0.64, 0.34]],
    [[-0.32, 0.04], [-0.14, -0.04], [0.04, 0.02]],
    [[0.18, 0.02], [0.38, -0.04], [0.54, 0.08]],
    [[-0.12, 0.28], [0.08, 0.34], [0.30, 0.28]],
    [[-0.22, 0.64], [0.00, 0.72], [0.26, 0.62]],
    [[-0.38, -0.20], [-0.10, -0.12], [0.20, -0.16], [0.48, -0.26]],
    [[-0.54, 0.22], [-0.22, 0.34], [0.18, 0.36], [0.52, 0.18]],
  ].map(path => path.map(([x, y]) => [cx + x * s * squash, cy + y * s]));
}

function drawPolyline(points, style) {
  if (points.length < 2) return;
  ctx.save();
  ctx.strokeStyle = style.color;
  ctx.globalAlpha = style.alpha;
  ctx.lineWidth = style.width;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  ctx.beginPath();
  ctx.moveTo(points[0][0], points[0][1]);
  for (let i = 1; i < points.length; i += 1) {
    ctx.lineTo(points[i][0], points[i][1]);
  }
  ctx.stroke();
  ctx.restore();
}

function drawMesh(width, height) {
  const lines = suzanneLines(width, height);
  for (const line of lines) {
    drawPolyline(line, { color: "#191917", alpha: 0.95, width: 1 });
  }
  const cx = width * 0.5;
  const cy = height * 0.48;
  const s = Math.min(width, height) * 0.34;
  ctx.strokeStyle = "rgba(25,25,23,.22)";
  ctx.lineWidth = 1;
  for (let i = -6; i <= 6; i += 1) {
    ctx.beginPath();
    ctx.moveTo(cx - s * 0.72, cy + i * s * 0.12);
    ctx.lineTo(cx + s * 0.72, cy - i * s * 0.10);
    ctx.stroke();
  }
}

function drawNpr(width, height) {
  const random = seeded(state.seed + Math.round(state.orbit * 13));
  const base = suzanneLines(width, height);
  const jitter = state.jitter;
  for (const line of base) {
    const path = line.map(([x, y], index) => {
      const amount = index === 0 || index === line.length - 1 ? jitter * 2.2 : jitter * 1.2;
      return [
        x + (random() - 0.5) * amount,
        y + (random() - 0.5) * amount,
      ];
    });
    drawPolyline(path, { color: "#171715", alpha: 0.88, width: state.thickness * 1.45 });
  }

  const hatchCount = Math.round(22 + state.hatchDensity * 70);
  const cx = width * 0.5;
  const cy = height * 0.55;
  const s = Math.min(width, height) * 0.31;
  for (let i = 0; i < hatchCount; i += 1) {
    const a = random() * Math.PI * 2;
    const r = Math.sqrt(random()) * s * 0.9;
    const x = cx + Math.cos(a) * r * 0.95;
    const y = cy + Math.sin(a) * r * 0.68;
    const len = 12 + random() * 22;
    const angle = -0.72 + (random() - 0.5) * 0.42;
    const dx = Math.cos(angle) * len;
    const dy = Math.sin(angle) * len;
    drawPolyline([[x - dx, y - dy], [x + dx, y + dy]], {
      color: "#5b5b54",
      alpha: 0.16 + state.hatchDensity * 0.28,
      width: Math.max(0.45, state.thickness * 0.62),
    });
  }
}

function updateMetrics() {
  const hatch = Math.round(18 + state.hatchDensity * 62);
  const features = Math.round(320 + state.featureDensity * 150);
  const paths = Math.max(0, features - Math.round(state.hiddenBias * 700) + hatch);
  const samples = Math.round(220 + state.hatchDensity * 190);
  document.querySelector("#hatchMetric").textContent = hatch;
  document.querySelector("#featuresMetric").textContent = features;
  document.querySelector("#pathsMetric").textContent = paths;
  document.querySelector("#samplesMetric").textContent = samples;
}

function draw() {
  const rect = canvas.getBoundingClientRect();
  const width = rect.width;
  const height = rect.height;
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = "#efefea";
  ctx.fillRect(0, 0, width, height);
  drawGrid(width, height);
  if (state.mode === "mesh") {
    drawMesh(width, height);
  } else {
    drawNpr(width, height);
  }
  updateMetrics();
}

function setTab(name) {
  state.tab = name;
  document.querySelectorAll(".tab").forEach(tab => tab.classList.toggle("is-active", tab.dataset.tab === name));
  document.querySelectorAll(".panel-section").forEach(panel => panel.classList.toggle("is-active", panel.dataset.panel === name));
  document.querySelector("#panelTitle").textContent = panels[name][0];
  document.querySelector("#panelSubtitle").textContent = panels[name][1];
}

function setMode(mode) {
  state.mode = mode;
  document.querySelectorAll("[data-mode]").forEach(button => button.classList.toggle("is-active", button.dataset.mode === mode));
  document.querySelector("#modeBadge").textContent = mode.toUpperCase();
  draw();
}

function bindSlider(id, key, formatter) {
  const input = document.querySelector(`#${id}`);
  const output = document.querySelector(`#${id.replace("Slider", "Value")}`);
  const update = () => {
    const value = Number(input.value);
    state[key] = formatter.toState(value);
    output.textContent = formatter.toLabel(state[key]);
    draw();
  };
  input.addEventListener("input", update);
  update();
}

document.querySelectorAll(".tab").forEach(tab => {
  tab.addEventListener("click", () => setTab(tab.dataset.tab));
});

document.querySelectorAll("[data-mode]").forEach(button => {
  button.addEventListener("click", () => setMode(button.dataset.mode));
});

document.querySelector("#showGrid").addEventListener("change", event => {
  state.showGrid = event.target.checked;
  draw();
});

document.querySelector("#seedInput").addEventListener("input", event => {
  state.seed = Number(event.target.value) || 0;
  draw();
});

document.querySelector("#resetCamera").addEventListener("click", () => {
  document.querySelector("#fovSlider").value = "60";
  document.querySelector("#orbitSlider").value = "0";
  state.fov = 60;
  state.orbit = 0;
  document.querySelector("#fovValue").textContent = "60";
  document.querySelector("#orbitValue").textContent = "0";
  draw();
});

document.querySelector("#exportSvg").addEventListener("click", () => {
  setTab("debug");
});

bindSlider("fovSlider", "fov", {
  toState: value => value,
  toLabel: value => String(Math.round(value)),
});
bindSlider("orbitSlider", "orbit", {
  toState: value => value,
  toLabel: value => String(Math.round(value)),
});
bindSlider("creaseSlider", "crease", {
  toState: value => value,
  toLabel: value => String(Math.round(value)),
});
bindSlider("hiddenSlider", "hiddenBias", {
  toState: value => value / 1000,
  toLabel: value => value.toFixed(3),
});
bindSlider("featureSlider", "featureDensity", {
  toState: value => value / 100,
  toLabel: value => value.toFixed(2),
});
bindSlider("hatchSlider", "hatchDensity", {
  toState: value => value / 100,
  toLabel: value => value.toFixed(2),
});
bindSlider("jitterSlider", "jitter", {
  toState: value => value / 47,
  toLabel: value => value.toFixed(2),
});
bindSlider("thicknessSlider", "thickness", {
  toState: value => value / 100,
  toLabel: value => value.toFixed(2),
});
bindSlider("overshootSlider", "overshoot", {
  toState: value => value / 30,
  toLabel: value => value.toFixed(2),
});

window.addEventListener("resize", resizeCanvas);
resizeCanvas();
setTab("load");
setMode("npr");
