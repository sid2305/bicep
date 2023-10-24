import { AzureIcon, List } from "@vscode-bicep/ui-components";
import styled from "styled-components";

interface ResourceTypeListItemProps {
  resourceProvider: string;
  resourceType: string;
}

const $ResourceTypeListItem = styled(List.Item)`
  height: 22px;
  line-height: 22px;
  gap: 6px;
`;

export function ResourceTypeListItem({
  resourceProvider,
  resourceType,
}: ResourceTypeListItemProps) {
  return (
    <$ResourceTypeListItem>
      <AzureIcon
        resourceType={`${resourceProvider}/${resourceType}`}
        size={16}
      />
      <span>{resourceType}</span>
    </$ResourceTypeListItem>
  );
}
