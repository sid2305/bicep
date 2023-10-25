import { Dimension } from "./types";

export interface NodeConfig {
  cornerRadius: number;
  container: {
    padding: number;
  };
  leaf: {
    dimension: Dimension;
  };
}

export const nodeConfig: NodeConfig = {
  cornerRadius: 10,
  container: {
    padding: 20,
  },
  leaf: {
    dimension: {
      width: 100,
      height: 100,
    },
  },
};
