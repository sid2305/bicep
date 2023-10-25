export interface Position {
  x: number;
  y: number;
}

export interface Dimension {
  width: number;
  height: number;
}

export type Point = Position;

export type LineSegment = [Point, Point];
