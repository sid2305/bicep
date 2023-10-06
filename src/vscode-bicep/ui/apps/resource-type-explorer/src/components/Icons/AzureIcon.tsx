import { SvgIcon } from "./SvgIcon";
import { useAzureSvg } from "../../hooks/useAzureSvg";

interface AzureIconProps {
  resourceType: string;
}

export function AzureIcon({ resourceType }: AzureIconProps) {
  const { AzureSvg } = useAzureSvg(resourceType);

  return <SvgIcon width={20} height={20} SvgComponent={AzureSvg} />;
}
