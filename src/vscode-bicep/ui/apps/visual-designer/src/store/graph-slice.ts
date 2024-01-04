import { nodeConfig } from "../core/configs";
import { centerOfBox, intersectionOfLineSegments } from "../core/math";
import type { Dimension, LineSegment, Position } from "../core/types";
import type { ImmerStateCreator } from "./types";

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
  scaleTo: (scale: number) => void;

  moveNode: (nodeId: string, dx: number, dy: number) => void;
  addNode: (nodeId: string, position: Position) => void;
}

const { dimension } = nodeConfig.leaf;

export const createGraphSlice: ImmerStateCreator<GraphState> = (set) => ({
  position: { x: 0, y: 0 },
  scale: 1,
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

  translateTo: (position) =>
    set((state) => {
      state.graph.position = position;
    }),

  scaleTo: (scale) =>
    set((state) => {
      state.graph.scale = scale;
    }),

  moveNode: (nodeId, dx, dy) => {
    set((state) => {
      const node = state.graph.nodes[nodeId];
      
      dx = dx / state.graph.scale;
      dy = dy / state.graph.scale;
      
      if (dx === 0 && dy === 0) {
        return;
      }
      
      node.position.x += dx;
      node.position.y += dy;
      
      adjustEdgesForNode(state.graph, node);

      for (const descendant of enumerateDescendants(state.graph, node)) {
        descendant.position.x += dx;
        descendant.position.y += dy;

        adjustEdgesForNode(state.graph, descendant);
      }
      
      // Calculate positions and dimensions of ancestor.
      for (const ancestor of enumerateAncestors(state.graph, node)) {
        let newX = Infinity;
        let newY = Infinity;

        for (const child of enumerateChildren(state.graph, ancestor)) {
          newX = Math.min(newX, child.position.x);
          newY = Math.min(newY, child.position.y);
        }

        let newWidth = 0;
        let newHeight = 0;

        for (const child of enumerateChildren(state.graph, ancestor)) {
          newWidth = Math.max(
            newWidth,
            child.position.x + child.dimension.width - newX,
          );
          newHeight = Math.max(
            newHeight,
            child.position.y + child.dimension.height - newY,
          );
        }

        newX -= nodeConfig.container.padding;
        newY -= nodeConfig.container.padding;

        newWidth += nodeConfig.container.padding * 2;
        newHeight += nodeConfig.container.padding * 2;

        if (
          ancestor.position.x === newX &&
          ancestor.position.y === newY &&
          ancestor.dimension.width === newWidth &&
          ancestor.dimension.height === newHeight
        ) {
          return;
        }

        ancestor.position.x = newX;
        ancestor.position.y = newY;

        ancestor.dimension.width = newWidth;
        ancestor.dimension.height = newHeight;

        adjustEdgesForNode(state.graph, ancestor);
      }
    });
  },

  addNode: (nodeId, position) => {
    set((state) => {
      const x = (position.x - state.graph.position.x) / state.graph.scale;
      const y = (position.y - state.graph.position.y) / state.graph.scale;

      state.graph.nodes[nodeId] = {
        id: nodeId,
        position: { x, y },
        zIndex: 0,
        dimension: nodeConfig.leaf.dimension,
      };
    });
  },
});

function* enumerateAncestors(graph: GraphState, node: NodeState) {
  while (node.parentId) {
    node = graph.nodes[node.parentId];
    yield node;
  }
}

function* enumerateChildren(graph: GraphState, node: NodeState) {
  for (const childId of node.childIds ?? []) {
    yield graph.nodes[childId];
  }
}

function* enumerateDescendants(
  graph: GraphState,
  node: NodeState,
): Generator<NodeState> {
  for (const childId of node.childIds ?? []) {
    const child = graph.nodes[childId];

    yield child;
    yield* enumerateDescendants(graph, child);
  }
}


function adjustEdgesForNode(graph: GraphState, node: NodeState) {
  if (node.edgeIds) {
    for (const edgeId of node.edgeIds) {
      const edge = graph.edges[edgeId];
      updateEdgeIntersections(graph, edge);
    }
  }
}

function updateEdgeIntersections(graph: GraphState, edge: EdgeState) {
  const sourceNode = graph.nodes[edge.sourceId];
  const targetNode = graph.nodes[edge.targetId];

  const sourceCenter = centerOfBox(sourceNode.position, sourceNode.dimension);
  const targetCenter = centerOfBox(targetNode.position, targetNode.dimension);
  const centerLineSegment: LineSegment = [sourceCenter, targetCenter];

  edge.sourceIntersection =
    findIntersection(centerLineSegment, sourceNode) ?? sourceCenter;
  edge.targetIntersection =
    findIntersection(centerLineSegment, targetNode) ?? targetCenter;
}

function findIntersection(lineSegment: LineSegment, node: NodeState) {
  const topLeft = node.position;
  const topRight = { x: topLeft.x + node.dimension.width, y: topLeft.y };
  const bottomLeft = { x: topLeft.x, y: topLeft.y + node.dimension.height };
  const bottomRight = { x: topRight.x, y: bottomLeft.y };

  return (
    intersectionOfLineSegments(lineSegment, [topLeft, topRight]) ??
    intersectionOfLineSegments(lineSegment, [topRight, bottomRight]) ??
    intersectionOfLineSegments(lineSegment, [bottomLeft, bottomRight]) ??
    intersectionOfLineSegments(lineSegment, [topLeft, bottomLeft])
  );
}
