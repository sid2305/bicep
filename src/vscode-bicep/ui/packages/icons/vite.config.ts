// import path from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import dts from "vite-plugin-dts";
import svgr from "vite-plugin-svgr";
import { libInjectCss } from "vite-plugin-lib-inject-css";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    svgr(),
    dts({
      exclude: ["**/*.stories.ts", "**/*.stories.tsx", "**/use-*.ts"],
      insertTypesEntry: true,
    }),
    // libInjectCss(),
    libInjectCss({
      formats: ["es"],
      entry: {
        index: "src/index.ts",
        "azure-icon": "src/azure-icon/index.ts",
        codicon: "src/codicon/index.ts",
      },
      rollupOptions: {
        external: [
          "react",
          "react-dom",
          "react/jsx-runtime",
          "styled-components",
        ],
        output: {
          entryFileNames: "[name].js",
          chunkFileNames: "chunks/[name].[hash].js",
          assetFileNames: "assets/[name][extname]",
        },
      },
    }),
  ],
});
