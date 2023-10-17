import { PropsWithChildren } from "react";
import styled  from "styled-components";
import { useAccordionItem } from "./use-accordion-item";

const $AccordionItemHeader = styled.div`
  cursor: pointer;
`;

export function AccordionItemHeader({ children }: PropsWithChildren) {
  const { toggleActive } = useAccordionItem();

  return (
    <$AccordionItemHeader onClick={toggleActive}>
      {children}
    </$AccordionItemHeader>
  );
}
