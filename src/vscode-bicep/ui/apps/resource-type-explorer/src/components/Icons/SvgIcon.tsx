import { FC, SVGProps } from "react";
import styled from "styled-components";

interface SvgIconProps {
  width: number;
  height: number;
  SvgComponent: FC<SVGProps<SVGSVGElement>>;
}

const $SvgIcon = styled.div<{ $width: number; $height: number }>`
  width: ${(props) => props.$width}px;
  height: ${(props) => props.$height}px;
  text-align: center;
`;

export function SvgIcon({ width, height, SvgComponent }: SvgIconProps) {
  return (
    <$SvgIcon $width={width} $height={height}>
      <SvgComponent width="100%" height="100%" />
    </$SvgIcon>
  );
}
