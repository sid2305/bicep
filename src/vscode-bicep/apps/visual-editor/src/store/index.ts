import { create } from "zustand";
import { immer } from "zustand/middleware/immer";
import { GraphState, GraphActions } from "./types";

// const nodes: Array<Partial<NodeModel>> = [

// ];

const initialState: GraphState = {
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
    },
    B: {
      id: "B",
      parentId: "F",
      childIds: ["C", "D"],
      edgeIds: ["A->B"],
      zIndex: 10,
      position: { x: 400, y: 500 },
    },
    C: {
      id: "C",
      parentId: "B",
      zIndex: 100,
      position: { x: 300, y: 100 },
    },
    D: {
      id: "D",
      parentId: "B",
      zIndex: 100,
      position: { x: 600, y: 200 },
    },
    E: {
      id: "E",
      parentId: "F",
      zIndex: 10,
      position: { x: 1000, y: 200 },
    },
    F: {
      id: "F",
      childIds: ["B", "E"],
      zIndex: 0,
      position: { x: 800, y: 600 },
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

export const store = withSelectors(
  create(
    immer<GraphState & Partial<GraphActions>>((set) => ({
      ...initialState,
      translateTo: (position) =>
        set((graph) => {
          graph.position = position;
        }),
      scaleTo: (scale) =>
        set((graph) => {
          graph.scale = scale;
        }),
    }))
  )
);
