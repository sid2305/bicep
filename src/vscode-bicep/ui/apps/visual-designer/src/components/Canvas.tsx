import { DragEvent } from "react";
import styled from "styled-components";
import usePanZoom from "../hooks/usePanZoom";

import Graph from "./Graph";
import { store } from "../store";

const $Canvas = styled.div`
  position: absolute;
  left: 0px;
  top: 0px;
  right: 0px;
  bottom: 0px;
  overflow: hidden;
`;

let nodeId = 0;

export default function Canvas() {
  const canvasRef = usePanZoom();
  const nodes = store.use.nodes();
  const scale = store.use.scale();
  const position = store.use.position();
  const addNode = store.use.addNode();

  function handleDragOver(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    const fullyQualifiedResourceType = event.dataTransfer.getData("text");

    addNode(`${fullyQualifiedResourceType}/${nodeId++}`, {
      // x: event.clientX * scale - position.x,
      // y: event.clientY * scale - position.y,
      // x: event.clientX * scale - position.x,
      // y: event.clientY * scale - position.y,
      x: (event.clientX - position.x / scale),
      y: (event.clientY - position.y / scale),
    });

    console.log("node A", nodes["A"].position);
    console.log("scale", scale);
    console.log("position", position);
    console.log("absolute position", { x: position.x / scale, y: position.y / scale });
    console.log("new node", event.clientX, event.clientY);
    console.log(
      (event.clientX - position.x / scale),
      (event.clientY - position.y / scale),
    );
  }

  return (
    <$Canvas ref={canvasRef} onDragOver={handleDragOver} onDrop={handleDrop}>
      <Graph />
    </$Canvas>
  );
}
