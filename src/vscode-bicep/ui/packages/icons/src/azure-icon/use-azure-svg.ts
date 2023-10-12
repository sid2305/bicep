import { useEffect, useRef, useState } from "react";
import ResourceSvg from "../assets/icons/azure/custom/resource.svg?react";

type SvgComponent = typeof ResourceSvg;

const svgImportsByPath = import.meta.glob<SvgComponent>(
  `../assets/icons/azure/**/*.svg`,
  {
    query: "react",
    import: "default",
  },
);

const svgPathsByResourceType: Record<string, string> = {
  "microsoft.compute/virtualmachines":
    "compute/10021-icon-service-Virtual-Machine",
  "microsoft.compute/virtualmachinescalesets":
    "compute/10034-icon-service-VM-Scale-Sets",

  "microsoft.network/networkinterfaces":
    "networking/10080-icon-service-Network-Interfaces",
  "microsoft.network/loadbalancers":
    "networking/10062-icon-service-Load-Balancers",

  "microsoft.web/serverfarms":
    "app-services/00046-icon-service-App-Service-Plans",
  "microsoft.web/sites": "app-services/10035-icon-service-App-Services",
};

async function tryImportAzureSvg(
  resourceType: string,
): Promise<SvgComponent | undefined> {
  resourceType = resourceType.toLowerCase();

  if (resourceType in svgPathsByResourceType) {
    const svgPath = svgPathsByResourceType[resourceType];
    const svgImport = svgImportsByPath[`../assets/icons/azure/${svgPath}.svg`];

    return svgImport?.();
  }

  return undefined;
}

export function useAzureSvg(
  resourceType: string,
):
  | { loading: true; AzureSvg: undefined }
  | { loading: false; AzureSvg: SvgComponent } {
  const svgRef = useRef<SvgComponent>(ResourceSvg);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);

    const loadIcon = async () => {
      try {
        svgRef.current = (await tryImportAzureSvg(resourceType)) ?? ResourceSvg;
      } catch (err) {
        console.log(err);
      } finally {
        setLoading(false);
      }
    };

    loadIcon();
  }, [resourceType]);

  return loading
    ? { loading, AzureSvg: undefined }
    : { loading, AzureSvg: svgRef.current };
}
