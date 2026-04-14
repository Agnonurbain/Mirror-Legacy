import { Character, CultivationRealm, TaskType } from '../../types/game';
import { REALM_CONFIG } from '../../constants';
import { CultivationSystem } from '../systems/CultivationSystem';
import { MentalStabilitySystem } from '../systems/MentalStabilitySystem';

export class CharacterObject implements Character {
  id: string;
  firstName: string;
  lastName: string;
  gender: 'Male' | 'Female';
  generation: number;
  age: number;
  maxLifespan: number;
  spiritualRoot: number;
  realm: CultivationRealm;
  isAlive: boolean;
  parents: string[];
  children: string[];
  spouse?: string;
  currentTask: TaskType;
  skills: {
    alchemy: number;
    forging: number;
    farming: number;
    combat: number;
  };
  studiedManuals: string[];
  traits: string[];
  mentalStability: number;
  portraitSeed: string;
  backstory?: string;
  assignedManualId?: string;
  breakthroughProgress: number;
  isProtectedByMirror?: boolean;
  
  // Track if the character changed this year to trigger re-renders
  isDirty: boolean = false;

  constructor(data: Character & { assignedManualId?: string }) {
    this.id = data.id;
    this.firstName = data.firstName;
    this.lastName = data.lastName;
    this.gender = data.gender;
    this.generation = data.generation;
    this.age = data.age;
    this.maxLifespan = data.maxLifespan;
    this.spiritualRoot = data.spiritualRoot;
    this.realm = data.realm;
    this.isAlive = data.isAlive;
    this.parents = data.parents || [];
    this.children = data.children || [];
    this.spouse = data.spouse;
    this.currentTask = data.currentTask;
    this.skills = data.skills || { alchemy: 0, forging: 0, farming: 0, combat: 0 };
    this.studiedManuals = data.studiedManuals || [];
    this.traits = data.traits || [];
    this.mentalStability = data.mentalStability ?? 100;
    this.portraitSeed = data.portraitSeed;
    this.backstory = data.backstory;
    this.assignedManualId = data.assignedManualId;
    this.breakthroughProgress = data.breakthroughProgress || 0;
    this.isProtectedByMirror = data.isProtectedByMirror;
  }

  updateAge(): void {
    if (!this.isAlive) return;
    this.age += 1;
    this.isDirty = true;
    
    if (this.age >= this.maxLifespan) {
      this.die();
    }
  }

  applyDamage(amount: number): void {
    if (!this.isAlive) return;
    this.maxLifespan -= amount;
    this.isDirty = true;
    if (this.age >= this.maxLifespan) {
      this.die();
    }
  }

  modifyStability(amount: number): void {
    if (!this.isAlive) return;
    this.mentalStability = Math.max(0, Math.min(100, this.mentalStability + amount));
    this.isDirty = true;
  }

  die(): void {
    this.isAlive = false;
    this.currentTask = 'Repos';
    this.isDirty = true;
  }

  clone(): CharacterObject {
    const clone = new CharacterObject(this.toData());
    clone.isDirty = false;
    return clone;
  }

  toData(): Character {
    return {
      id: this.id,
      firstName: this.firstName,
      lastName: this.lastName,
      gender: this.gender,
      generation: this.generation,
      age: this.age,
      maxLifespan: this.maxLifespan,
      spiritualRoot: this.spiritualRoot,
      realm: this.realm,
      isAlive: this.isAlive,
      parents: this.parents,
      children: this.children,
      spouse: this.spouse,
      currentTask: this.currentTask,
      skills: { ...this.skills },
      studiedManuals: [...this.studiedManuals],
      traits: [...this.traits],
      mentalStability: this.mentalStability,
      portraitSeed: this.portraitSeed,
      backstory: this.backstory,
      assignedManualId: this.assignedManualId,
      breakthroughProgress: this.breakthroughProgress,
      isProtectedByMirror: this.isProtectedByMirror
    };
  }
}
