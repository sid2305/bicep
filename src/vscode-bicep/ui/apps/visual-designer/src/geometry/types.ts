export interface Position {
  x: number;
  y: number;
}

export interface TwoDimensionalShape {
  width: number;
  height: number;
}

export type LineSegment = [Position, Position];

export interface Arc {
  center: Position;
  radius: number;
  startAngle: number;
  endAngle: number;
}

export interface Rectangle extends TwoDimensionalShape {
  center: Position;
}

export interface RoundedRectangle extends Rectangle {
  cornerRadius: number;
}
