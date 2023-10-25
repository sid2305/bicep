import styled from "styled-components";

import Node from "./Node";
import { store } from "../store";

const $Graph = styled.div.attrs<{
  $x: number;
  $y: number;
  $scale: number;
}>(({ $x, $y, $scale }) => ({
  style: {
    transform: `translate(${$x}px,${$y}px) scale(${$scale})`,
  },
}))`
  transform-origin: 0 0;
  /* position: relative; */
  height: 100px;
  width: 100px;
`;

export default function Graph() {
  const { x, y } = store.use.position();
  const scale = store.use.scale();
  const nodes = store.use.nodes();
  const edges = store.use.edges();
  const dimension = store.use.dimension();

  return (
    <$Graph $x={x} $y={y} $scale={scale}>
      {Object.values(nodes).map((node) => (
        <Node key={node.id} {...node} />
      ))}
      <svg width={dimension.width} height={dimension.height}>
        <g>
          {Object.values(edges).map((edge) => (
            <line
              key={edge.id}
              style={{
                stroke: "black",
                strokeWidth: 2,
              }}
              x1={edge.sourceIntersection.x}
              y1={edge.sourceIntersection.y}
              x2={edge.targetIntersection.x}
              y2={edge.targetIntersection.y}
            />
          ))}
        </g>
      </svg>
    </$Graph>
  );
}
