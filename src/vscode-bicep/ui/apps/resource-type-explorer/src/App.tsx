// import ReactLogo from "./assets/react.svg?react";
// import ViteLogo from "/vite.svg";
import "./App.css";
import { AccordionGroup } from "./components/Accordion/AccordionGroup";
import { Accordion } from "./components/Accordion/Accordion";
import { AccordionHeader } from "./components/Accordion/AccordionHeader";
import { AccordionPanel } from "./components/Accordion/AccordionPanel";
// import { SvgIcon } from "./components/Icons";
import { AzureIcon } from "./components/Icons/AzureIcon";
import { List } from "./components/List/List";

const resourceTypeCatalog = {
  "Microsoft.Compute": [
    "Microsoft.Compute/virtualMachines",
    "Microsoft.Compute/virtualMachineScaleSets",
  ],
  "Microsoft.Web": ["Microsoft.Web/serverfarms", "Microsoft.Web/sites"],
};

function App() {
  return (
    <section className="App">
      <h2>collapsible</h2>
      <AccordionGroup>
        {Object.entries(resourceTypeCatalog).map(
          ([provider, resourceTypes], i) => (
            <Accordion key={i}>
              <AccordionHeader>{provider}</AccordionHeader>
              <AccordionPanel>
                <List
                  items={resourceTypes}
                  renderItem={(resourceType) => (
                    <span>
                    <AzureIcon resourceType={resourceType} />
                    {resourceType}
                    </span>
                  )}
                />
                {/* <SvgIcon iconPath="azure/compute/10021-icon-service-Virtual-Machine" /> */}
                {/* <AzureIcon resourceType={`/virtualMachines`} /> */}
                {/* Lorem ipsum dolor sit amet consectetur adipisicing elit. Eos
                quod explicabo, nam sapiente id nostrum ex, ab numquam,
                doloremque aspernatur quisquam illo! Officiis explicabo laborum
                incidunt corrupti provident esse eligendi. */}
              </AccordionPanel>
            </Accordion>
          ),
        )}
      </AccordionGroup>
    </section>
  );
}

export default App;
