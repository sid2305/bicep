import { PropsWithChildren } from "react";
import styled from "styled-components";

const $List = styled.ul`
  list-style-type: none;
  margin: 0;
  padding: 0;
`;

export function List({ children }: PropsWithChildren) {
  return <$List>{children}</$List>;
}
