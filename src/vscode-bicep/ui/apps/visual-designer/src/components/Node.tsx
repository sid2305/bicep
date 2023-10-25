import styled from "styled-components";
import useDrag from "../hooks/useDrag";
import { NodeModel } from "../store/types";
import { Dimension, Position } from "../core/types";

const $Node = styled.div.attrs<{
  $zIndex: number;
  $position: Position;
  $dimension: Dimension;
}>(({ $zIndex, $position, $dimension }) => ({
  style: {
    transform: `translate(${$position.x}px, ${$position.y}px)`,
    zIndex: $zIndex,
    width: `${$dimension.width}px`,
    height: `${$dimension.height}px`,
  },
}))`
  cursor: default;
  display: flex;
  justify-content: center;
  align-items: center;
  transform-origin: 0 0;
  position: absolute;
  background: #1f1f1f1f;
  border-style: solid;
  border-color: black;
`;

export default function Node({ id, zIndex, position, dimension }: NodeModel) {
  const nodeRef = useDrag(id);

  return (
    <$Node
      ref={nodeRef}
      $zIndex={zIndex}
      $position={position}
      $dimension={dimension}
    >
      {id}
    </$Node>
  );
}
