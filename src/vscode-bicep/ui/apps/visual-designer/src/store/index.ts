import { StoreApi, UseBoundStore, create } from "zustand";
import { immer } from "zustand/middleware/immer";
import { createCanvasSlice } from "./canvas-slice";
import { createGraphSlice } from "./graph-slice";

import type { AppState } from "./types";

type WithSelectors<S> = S extends { getState: () => infer T }
  ? S & { use: { [K in keyof T]: () => T[K] } }
  : never;


export const withSelectors = <S extends UseBoundStore<StoreApi<object>>>(
  boundStore: S,
) => {
  const store = boundStore as WithSelectors<S>;
  store.use = {};
  for (const k of Object.keys(store.getState())) {
    (store.use as Record<string, unknown>)[k] = () =>
      store((s) => s[k as keyof typeof s]);
  }

  return store;
};


export const store = withSelectors(
  create(
    immer<AppState>((...args) => ({
      canvas: createCanvasSlice(...args),
      graph: createGraphSlice(...args),
    })),
  ),
);
