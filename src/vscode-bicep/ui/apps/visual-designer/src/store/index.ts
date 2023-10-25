import { StoreApi, UseBoundStore, create } from "zustand";
import { immer } from "zustand/middleware/immer";
import { GraphModel, GraphState } from "./types";
import { nodeConfig } from "../core/configs";
import { createGraphActions } from "./actions";

const { dimension } = nodeConfig.leaf;

const initialState: GraphModel = {
  position: { x: 0, y: 0 },
  scale: 1,
  dimension: {
    width: 2000,
    height: 2000,
  },
  nodes: {
    A: {
      id: "A",
      position: { x: 10, y: 10 },
      edgeIds: ["A->B"],
      zIndex: 0,
      dimension,
    },
    B: {
      id: "B",
      parentId: "F",
      childIds: ["C", "D"],
      edgeIds: ["A->B"],
      zIndex: 10,
      position: { x: 400, y: 500 },
      dimension,
    },
    C: {
      id: "C",
      parentId: "B",
      zIndex: 100,
      position: { x: 300, y: 100 },
      dimension,
    },
    D: {
      id: "D",
      parentId: "B",
      zIndex: 100,
      position: { x: 600, y: 200 },
      dimension,
    },
    E: {
      id: "E",
      parentId: "F",
      zIndex: 10,
      position: { x: 1000, y: 200 },
      dimension,
    },
    F: {
      id: "F",
      childIds: ["B", "E"],
      zIndex: 0,
      position: { x: 800, y: 600 },
      dimension,
    },
  },
  edges: {
    "A->B": {
      id: "A->B",
      sourceId: "A",
      targetId: "B",
      sourceIntersection: { x: 0, y: 0 },
      targetIntersection: { x: 0, y: 0 },
    },
  },
};

type WithSelectors<S> = S extends { getState: () => infer T }
  ? S & { use: { [K in keyof T]: () => T[K] } }
  : never;

const withSelectors = <S extends UseBoundStore<StoreApi<object>>>(
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
    immer<GraphState>((set) => ({
      ...initialState,
      ...createGraphActions(set),
    })),
  ),
);
