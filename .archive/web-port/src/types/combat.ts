export type Team = 'Player' | 'Enemy';

export interface Position {
  x: number;
  y: number;
}

export interface Combatant {
  id: string;
  name: string;
  team: Team;
  hp: number;
  maxHp: number;
  ap: number;
  maxAp: number;
  position: Position;
  attack: number;
  defense: number;
  speed: number; // Determines turn order
  range: number;
  isDead: boolean;
  portraitSeed?: string;
  isBeast?: boolean;
}

export type CellType = 'Empty' | 'Obstacle';

export interface CombatCell {
  x: number;
  y: number;
  type: CellType;
}

export interface CombatLogEntry {
  id: string;
  message: string;
  type: 'damage' | 'heal' | 'move' | 'info' | 'death';
}

export interface CombatState {
  id: string;
  expeditionId: string;
  grid: CombatCell[][];
  width: number;
  height: number;
  combatants: Combatant[];
  turnOrder: string[]; // Array of combatant IDs
  currentTurnIndex: number;
  round: number;
  logs: CombatLogEntry[];
  status: 'ongoing' | 'victory' | 'defeat';
}
