export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Hu Signal Entrypoint",
    alias: "HuSignal.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
