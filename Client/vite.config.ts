import { defineConfig } from "vite";
import fs from "node:fs";
import path from "node:path";

export default defineConfig({
  build: {
    outDir: "../wwwroot/App_Plugins/husignal",
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
          "../wwwroot/App_Plugins/husignal/umbraco-package.json"
        );

        const umbracoPackage = JSON.parse(
          fs.readFileSync(sourcePackagePath, "utf-8")
        );

        const buildVersion = Date.now();

        umbracoPackage.extensions[0].js =
          `/App_Plugins/husignal/hu-signal.js?v=${buildVersion}`;

        fs.writeFileSync(
          outputPackagePath,
          JSON.stringify(umbracoPackage, null, 2)
        );

        console.log(
          `Generated Umbraco manifest with build version ${buildVersion}`
        );
      },
    },
  ],
});