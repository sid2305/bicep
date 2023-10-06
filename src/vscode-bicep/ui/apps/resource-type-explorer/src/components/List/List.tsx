import { ReactNode } from "react";
import styled from "styled-components";

interface ListProps<T> {
  items: T[];
  renderItem: (item: T) => ReactNode;
}

const $List = styled.ul`
  list-style-type: none;
  margin: 0;
  padding: 0;
`;

export function List<T>({ items, renderItem }: ListProps<T>) {
  return (
    <$List>
      {items.map((item, i) => (
        <li key={i}>{renderItem(item)}</li>
      ))}
    </$List>
  );
}
