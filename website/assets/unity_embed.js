// unity_embed.js
(function () {
  function qs(sel) { return document.querySelector(sel); }

  window.loadUnityWebGL = function loadUnityWebGL(opts) {
    const buildUrl = opts.buildUrl || "unity/Build";
    const name = opts.name; // e.g. "MyBuild"
    const canvas = qs(opts.canvasSelector || "#unity-canvas");
    const loading = qs(opts.loadingSelector || "#unity-loading");

    if (!canvas) throw new Error("Canvas not found");
    if (!name) throw new Error("Missing build name");

    const loaderUrl = `${buildUrl}/${name}.loader.js`;

    const config = {
      dataUrl: `${buildUrl}/${name}.data`,
      frameworkUrl: `${buildUrl}/${name}.framework.js`,
      codeUrl: `${buildUrl}/${name}.wasm`,
      streamingAssetsUrl: opts.streamingAssetsUrl || "StreamingAssets",
      companyName: opts.companyName || "",
      productName: opts.productName || "",
      productVersion: opts.productVersion || "1.0",
    };

    const script = document.createElement("script");
    script.src = loaderUrl;

    script.onload = () => {
      createUnityInstance(canvas, config, (progress) => {
        if (loading) loading.textContent = `Loading… ${Math.round(progress * 100)}%`;
      }).then(() => {
        if (loading) loading.textContent = "Ready.";
      }).catch((message) => {
        if (loading) loading.textContent = `Error: ${message}`;
        console.error(message);
      });
    };

    script.onerror = () => {
      if (loading) loading.textContent = `Error: failed to load ${loaderUrl}`;
    };

    document.body.appendChild(script);
  };
})();