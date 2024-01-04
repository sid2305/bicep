import styled from "styled-components";

import { Node } from "./Node";
import { Edge } from "./Edge";
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
  height: 0;
  width: 0;
`;

const $Svg = styled.svg`
  overflow: visible;
`;

export function Graph() {
  const { x, y } = store.use.graph().position;
  const scale = store.use.graph().scale;
  const nodes = store.use.graph().nodes;
  const edges = store.use.graph().edges;

  return (
    <$Graph $x={x} $y={y} $scale={scale}>
      {Object.values(nodes).map((node) => (
        <Node key={node.id} {...node}>
          {node.id}
        </Node>
      ))}
      <$Svg>
        <g>
          {Object.values(edges).map((edge) => (
            <Edge key={edge.id} {...edge} />
          ))}
        </g>
      </$Svg>
    </$Graph>
  );
}
