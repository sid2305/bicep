// import ReactLogo from "./assets/react.svg?react";
// import ViteLogo from "/vite.svg";
import "./App.css";
import { Accordion, AzureIcon, List } from "@vscode-bicep/ui-components";

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
      <Accordion>
        {Object.entries(resourceTypeCatalog).map(
          ([provider, resourceTypes], i) => (
            <Accordion.Item key={i}>
              <Accordion.Header>{provider}</Accordion.Header>
              <Accordion.Content>
                <List>
                  {resourceTypes.map((resourceType, j) => (
                    <List.Item key={j}>
                      <AzureIcon resourceType={resourceType} size={20} />
                      <span>{resourceType.split('/')[1]}</span>
                    </List.Item>
                  ))}
                </List>
              </Accordion.Content>
            </Accordion.Item>
          ),
        )}
      </Accordion>
    </section>
  );
}

export default App;
