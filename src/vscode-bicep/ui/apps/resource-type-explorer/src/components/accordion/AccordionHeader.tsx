import { PropsWithChildren } from "react";
import { useAccordion } from "../../contexts/accordionContext";
import styled, { css } from "styled-components";
import { motion } from "framer-motion";

const $AccordionHeader = styled(motion.div)<{ $active: boolean }>`
  padding: 20px;
  cursor: pointer;
  transition: background-color 0.15s ease-in-out;
  &:hover {
    background-color: #ffd700;
  }

  ${(props) =>
    props.$active &&
    css`
      background-color: #ffd700;
    `}
`;

export function AccordionHeader({ children }: PropsWithChildren) {
  const { active, index, activate } = useAccordion();

  return (
    <$AccordionHeader $active={active} onClick={() => activate(index)}>
      {children}
    </$AccordionHeader>
  );
}
