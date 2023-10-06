import { Children, PropsWithChildren, useState } from "react";
import { AccordionProvider } from "../../contexts/accordionContext";

export function AccordionGroup({ children }: PropsWithChildren) {
  const [activeIndex, setActiveIndex] = useState(-1);

  return Children.map(children, (child, index) => {
    const active = index === activeIndex;

    function activate(index: number) {
      setActiveIndex(() => (index === activeIndex ? -1 : index));
    }

    return (
      <AccordionProvider value={{ active, index, activate }}>
        {child}
      </AccordionProvider>
    );
  });
}
