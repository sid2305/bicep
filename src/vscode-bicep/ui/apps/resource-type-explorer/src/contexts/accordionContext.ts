import { createContext, useContext } from "react";

interface AccordionContextType {
  active: boolean;
  index: number;
  activate(index: number): void;
}

const AccordionContext = createContext<
  AccordionContextType | undefined
>(undefined);

export const AccordionProvider = AccordionContext.Provider;

export const AccordionConsumer = AccordionContext.Consumer;

export function useAccordion() {
  const context = useContext(AccordionContext);

  if (context === undefined) {
    throw new Error(
      "useAccordion must be used within an AccordionContext.Provider",
    );
  }
  
  return context;
}
