import { useEffect, useRef } from "react";
import { D3ZoomEvent, zoom } from "d3-zoom";
import { select } from "d3-selection";
import { store } from "../store";

export default function usePanZoom<T extends Element = HTMLDivElement>() {
  const containerRef = useRef<T>(null);
  const translateTo = store.use.translateTo();
  const scaleTo = store.use.scaleTo();

  useEffect(() => {
    if (containerRef.current) {
      const selection = select(containerRef.current as Element);

      const { width, height } = containerRef.current.getBoundingClientRect();

      const zoomBehavior = zoom()
        .scaleExtent([1 / 4, 4])
        .extent([
          [0, 0],
          [width, height],
        ])
        .wheelDelta(
          (event) => (-event.deltaY * (event.deltaMode ? 120 : 1)) / 1000,
        )
        .on("zoom", (event: D3ZoomEvent<Element, unknown>) => {
          const { x, y, k } = event.transform;
          translateTo({ x, y });
          scaleTo(k);
        });

      selection.call(zoomBehavior);

      return () => {
        zoomBehavior.on("zoom", null);
      };
    }
  }, [translateTo, scaleTo]);
  
  return containerRef;
}
