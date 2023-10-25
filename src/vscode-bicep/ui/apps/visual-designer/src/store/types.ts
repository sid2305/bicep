import { Dimension, Position } from "../core/types";

export interface NodeModel {
  id: string;
  parentId?: string;
  childIds?: string[];
  edgeIds?: string[];
  position: Position;
  zIndex: number;
  dimension: Dimension;
}

export interface EdgeModel {
  id: string;
  sourceId: string;
  targetId: string;
  sourceIntersection: Position;
  targetIntersection: Position;
}

export interface GraphModel {
  position: Position;
  scale: number;
  dimension: Dimension;
  nodes: Record<string, NodeModel>;
  edges: Record<string, EdgeModel>;
}

export interface GraphActions {
  translateTo: (position: Position) => void;
  scaleTo: (factor: number) => void;
  
  moveNode: (nodeId: string, dx: number, dy: number) => void;
  addNode: (nodeId: string, position: Position) => void;
}

export type GraphState = GraphModel & GraphActions;

export type Getter = () => GraphState;

export type Setter = (
  updater: (state: GraphState) => void,
  shouldUpdate?: boolean,
) => void;
