import { PropsWithChildren } from "react";
import { styled } from "styled-components";

const $AccordionItem = styled.div`
  border-radius: 4px;
  overflow: hidden;
  margin-bottom: 20px;
  background-color: #fff;
`;

export function Accordion({ children }: PropsWithChildren) {
  return <$AccordionItem>{children}</$AccordionItem>
}
