import { createContext } from "react";

interface AccordionItemContextType {
  active: boolean;
  toggleActive(): void;
}

export const AccordionItemContext = createContext<AccordionItemContextType | undefined>(
  undefined
);

export const AccordionItemProvider = AccordionItemContext.Provider;

export const AccordionItemConsumer = AccordionItemContext.Consumer;
