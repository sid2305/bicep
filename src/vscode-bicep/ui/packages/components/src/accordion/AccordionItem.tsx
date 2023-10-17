import { PropsWithChildren, useEffect, useRef, useState } from "react";
import styled from "styled-components";
import { useAccordion } from "./use-accordion";
import { AccordionItemId } from "./types";
import { randomUUID } from "crypto";
import { AccordionItemProvider } from "./accordion-item-context";

type AccordionItemProps = PropsWithChildren<{
  itemId?: AccordionItemId;
}>;

const $AccordionItem = styled.div`
  overflow: hidden;
  margin-bottom: 20px;
  background-color: #fff;
`;

export function AccordionItem({ itemId, children }: AccordionItemProps) {
  const itemIdRef = useRef<AccordionItemId>(itemId ?? randomUUID());
  const { activeItemId, setActiveItemId } = useAccordion();
  const [active, setActive] = useState(activeItemId === itemId);

  useEffect(() => {
    active && setActiveItemId(itemIdRef.current);
  }, [active, setActiveItemId]);

  useEffect(() => {
    setActive(itemIdRef.current === activeItemId);
  }, [activeItemId]);

  return (
    <AccordionItemProvider value={{
      active,
      toggleActive: () => setActive(!active)
    }}>
      <$AccordionItem>{children}</$AccordionItem>;
    </AccordionItemProvider>
  );
}
