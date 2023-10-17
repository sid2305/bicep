import { PropsWithChildren } from "react";
import { AnimatePresence, motion } from "framer-motion";
import styled from "styled-components";
import { useAccordionItem } from "./use-accordion-item";

const $AccordionContent = styled(motion.div)`
  padding: 20px;
`;

export function AccordionContent({ children }: PropsWithChildren) {
  const { active } = useAccordionItem();

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
          <$AccordionContent>{children}</$AccordionContent>
        </motion.section>
      )}
    </AnimatePresence>
  );
}
