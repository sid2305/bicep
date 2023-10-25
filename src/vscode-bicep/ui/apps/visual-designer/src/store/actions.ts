import { nodeConfig } from "../core/configs";
import { centerOfBox, intersectionOfLineSegments } from "../core/math";
import { LineSegment } from "../core/types";
import {
  GraphActions,
  Setter,
  GraphModel,
  NodeModel,
  EdgeModel,
} from "./types";

function* enumerateAncestors(graph: GraphModel, node: NodeModel) {
  while (node.parentId) {
    node = graph.nodes[node.parentId];
    yield node;
  }
}

function* enumerableChildren(graph: GraphModel, node: NodeModel) {
  for (const childId of node.childIds ?? []) {
    yield graph.nodes[childId];
  }
}

function* enumerateDescendants(
  graph: GraphModel,
  node: NodeModel,
): Generator<NodeModel> {
  for (const childId of node.childIds ?? []) {
    const child = graph.nodes[childId];

    yield child;
    yield* enumerateDescendants(graph, child);
  }
}

function repositionEdges(graph: GraphModel, node: NodeModel) {
  if (node.edgeIds) {
    for (const edgeId of node.edgeIds) {
      const edge = graph.edges[edgeId];
      updateIntersections(graph, edge);
    }
  }
}

function updateIntersections(graph: GraphModel, edge: EdgeModel) {
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

function findIntersection(lineSegment: LineSegment, node: NodeModel) {
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

export const createGraphActions = (set: Setter): GraphActions => ({
  translateTo: (position) =>
    set((graph) => {
      graph.position = position;
    }),
  scaleTo: (scale) =>
    set((graph) => {
      graph.scale = scale;
    }),

  moveNode: (nodeId, dx, dy) =>
    set((graph) => {
      const node = graph.nodes[nodeId];

      dx = dx / graph.scale;
      dy = dy / graph.scale;

      if (dx === 0 && dy === 0) {
        return;
      }

      // Move nodes and descendants.
      node.position.x += dx;
      node.position.y += dy;

      repositionEdges(graph, node);

      for (const descendant of enumerateDescendants(graph, node)) {
        descendant.position.x += dx;
        descendant.position.y += dy;

        repositionEdges(graph, descendant);
      }

      // Calculate positions and dimensions of ancestor.
      for (const ancestor of enumerateAncestors(graph, node)) {
        let newX = Infinity;
        let newY = Infinity;

        for (const child of enumerableChildren(graph, ancestor)) {
          newX = Math.min(newX, child.position.x);
          newY = Math.min(newY, child.position.y);
        }

        let newWidth = 0;
        let newHeight = 0;

        for (const child of enumerableChildren(graph, ancestor)) {
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

        repositionEdges(graph, ancestor);
      }
    }),

  addNode: (nodeId, { x, y }) =>
    set((graph) => {
      graph.nodes[nodeId] = {
        id: nodeId,
        position: { x, y },
        zIndex: 0,
        dimension: nodeConfig.leaf.dimension,
      };
    }),
});
