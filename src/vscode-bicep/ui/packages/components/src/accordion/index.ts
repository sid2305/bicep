import { Accordion as AccordionComponent } from "./Accordion";
import { AccordionItem } from "./AccordionItem";
import { AccordionItemHeader } from "./AccordionItemHeader";
import { AccordionItemContent } from "./AccordionItemContent";
import { useAccordionItem } from "./use-accordion-item";

const Accordion = Object.assign(AccordionComponent, {
  Item: AccordionItem,
  ItemHeader: AccordionItemHeader,
  ItemContent: AccordionItemContent,
});

export { Accordion, useAccordionItem };
