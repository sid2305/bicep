import type { Dimension, Position } from "../core/types";
import type { StateCreator } from "zustand";

// type ExcludeUndefined<T> = Exclude<T, undefined>;

// // prettier-ignore
// export type ExcludeFunctions<T> = {
//   [K in keyof T as T[K] extends (...args: never[]) => void ? never : K]:
//     ExcludeUndefined<T[K]> extends Array<infer E>
//       ? Array<ExcludeFunctions<E>>
//       : ExcludeUndefined<T[K]> extends object
//         ? ExcludeFunctions<T[K]>
//         : T[K];
// };

export type ImmerStateCreator<T> = StateCreator<
  AppState,
  [["zustand/immer", never], never],
  [],
  T
>;

export interface NodeState {
  id: string;
  parentId?: string;
  childIds?: string[];
  edgeIds?: string[];
  position: Position;
  zIndex: number;
  dimension: Dimension;
}

export interface EdgeState {
  id: string;
  sourceId: string;
  targetId: string;
  sourceIntersection: Position;
  targetIntersection: Position;
}

export interface GraphState {
  position: Position;
  scale: number;
  nodes: Record<string, NodeState>;
  edges: Record<string, EdgeState>;

  translateTo: (position: Position) => void;
  scaleTo: (factor: number) => void;

  moveNode: (nodeId: string, dx: number, dy: number) => void;
  addNode: (nodeId: string, position: Position) => void;
}

export interface CanvasState {
  dimension: Dimension;
  setDimension: (dimension: Dimension) => void;
}

export interface AppState {
  canvas: CanvasState;
  graph: GraphState;
}

// export type AppStateWithoutActions = ExcludeFunctions<AppState>;

// export type Getter = () => AppStateWithoutActions;

// export type Setter = (
//   updater: (state: AppStateWithoutActions) => void,
//   shouldUpdate?: boolean,
// ) => void;
