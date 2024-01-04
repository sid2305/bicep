import type { CanvasState, ImmerStateCreator } from "./types";

export const createCanvasSlice: ImmerStateCreator<CanvasState> = (set) => ({
  dimension: { width: 2000, height: 2000 },
  setDimension: (dimension) =>
    set((state) => {
      state.canvas.dimension = dimension;
    }),
});
