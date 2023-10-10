import { useAzureSvg } from "./use-azure-svg";
import styled from "styled-components";

interface AzureIconProps {
  resourceType: string;
  size: number;
}

const $SvgIcon = styled.div<{ $size: number; }>`
  width: ${(props) => props.$size}px;
  height: ${(props) => props.$size}px;
  text-align: center;
`;

export function AzureIcon({ resourceType, size }: AzureIconProps) {
  const { loading, AzureSvg } = useAzureSvg(resourceType);

  return (
    <$SvgIcon $size={size}>
      {!loading && <AzureSvg width="100%" height="100%" />}
    </$SvgIcon>
  );
}
