import { defineConfig } from "vite";
import fs from "node:fs";
import path from "node:path";

export default defineConfig({
  build: {
    outDir: "../dist/HuSignal",
    emptyOutDir: true,

    lib: {
      entry: "src/bundle.manifests.ts",
      formats: ["es"],
      fileName: () => "hu-signal",
    },

    rollupOptions: {
      external: [/^@umbraco/],
      output: {
        entryFileNames: "hu-signal.js",
        chunkFileNames: "chunks/[name].[hash].js",
        assetFileNames: "assets/[name].[hash][extname]",
      },
    },
  },

  plugins: [
    {
      name: "generate-umbraco-package",

      closeBundle() {
        const sourcePackagePath = path.resolve(
          process.cwd(),
          "public/umbraco-package.json"
        );

        const outputPackagePath = path.resolve(
          process.cwd(),
          "../dist/HuSignal/umbraco-package.json"
        );

        const umbracoPackage = JSON.parse(
          fs.readFileSync(sourcePackagePath, "utf-8")
        );

        const buildVersion = Date.now();

        umbracoPackage.extensions[0].js =
          `/App_Plugins/HuSignal/hu-signal.js?v=${buildVersion}`;

        fs.writeFileSync(
          outputPackagePath,
          JSON.stringify(umbracoPackage, null, 2)
        );
      },
    },
  ],
});