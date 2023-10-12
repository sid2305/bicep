import { PropsWithChildren } from "react";
import { useAccordion } from "../../contexts/accordionContext";
import { AnimatePresence, motion } from "framer-motion";
import styled from "styled-components";

const $ContentContainer = styled(motion.div)`
  padding: 20px;
`;

export function AccordionPanel({ children }: PropsWithChildren) {
  const { active } = useAccordion();

  return (
    <AnimatePresence initial={false}>
      {active && (
        <motion.section
          key="content"
          initial="collapsed"
          animate="open"
          exit="collapsed"
          variants={{
            open: { height: "auto" },
            collapsed: { height: 0 },
          }}
          transition={{ type: "spring", duration: 0.4, bounce: 0 }}
        >
          <$ContentContainer>{children}</$ContentContainer>
        </motion.section>
      )}
    </AnimatePresence>
  );
}
