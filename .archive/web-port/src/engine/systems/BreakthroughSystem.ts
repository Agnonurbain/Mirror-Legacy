import { Character, CultivationRealm, GameEvent } from '../../types/game';
import { REALM_REQUIREMENTS, CultivationSystem } from './CultivationSystem';

export type BreakthroughResult = 'Success' | 'Minor Fail' | 'Major Fail' | 'Catastrophic';

export class BreakthroughSystem {
  static attemptBreakthrough(
    character: Character, 
    nextRealm: CultivationRealm, 
    hasPill: boolean, 
    hasMentor: boolean, 
    isProtectedByMirror: boolean,
    destinyBoost: number = 1,
    cultivationBonus: number = 1,
    worldModifier: number = 1,
    traitModifier: number = 1
  ): { result: BreakthroughResult; events: GameEvent[] } {
    const events: GameEvent[] = [];
    const req = REALM_REQUIREMENTS[nextRealm];
    
    if (!req) return { result: 'Minor Fail', events };

    // Base Success = 100 - Base Risk
    let successChance = 100 - req.baseRisk;

    // Modifiers
    // +1% per 5 points of Spiritual Root above minimum
    const rootBonus = Math.max(0, Math.floor((character.spiritualRoot - req.minRoot) / 5));
    successChance += rootBonus;

    if (hasPill) successChance += 10;
    if (hasMentor) successChance += 10; // Average mentor bonus
    if (isProtectedByMirror) successChance += 30;
    
    // -20% if Mental Stability < 50
    if (character.mentalStability < 50) successChance -= 20;
    
    // -10% if Age > 80% of max lifespan
    if (character.age > character.maxLifespan * 0.8) successChance -= 10;

    // Apply external multipliers (destiny, traits, world)
    // Convert chance to a 0-1 ratio for multiplication, then back to percentage
    let chanceRatio = successChance / 100;
    chanceRatio = chanceRatio * destinyBoost * cultivationBonus * worldModifier * traitModifier;
    successChance = chanceRatio * 100;

    // Clamp between 1 and 99
    successChance = Math.max(1, Math.min(99, successChance));

    const roll = Math.random() * 100;

    if (roll <= successChance) {
      return { result: 'Success', events };
    } else {
      // Failure logic
      // Determine severity based on how badly they failed
      const failMargin = roll - successChance;
      
      if (failMargin > 40) {
        return { result: 'Catastrophic', events };
      } else if (failMargin > 20) {
        return { result: 'Major Fail', events };
      } else {
        return { result: 'Minor Fail', events };
      }
    }
  }

  static applyBreakthroughResult(character: Character, nextRealm: CultivationRealm, result: BreakthroughResult, currentYear: number): GameEvent[] {
    const events: GameEvent[] = [];
    
    if (result === 'Success') {
      character.realm = nextRealm;
      character.maxLifespan = CultivationSystem.getBaseLifespan(nextRealm);
      character.breakthroughProgress = 0;
      character.mentalStability = Math.min(100, character.mentalStability + 10);
      
      events.push({
        id: Math.random().toString(36).substr(2, 9),
        year: currentYear,
        type: 'Success',
        message: `${character.lastName} ${character.firstName} a percé vers le royaume ${nextRealm} ! (+10 Stabilité)`
      });
    } else if (result === 'Minor Fail') {
      character.breakthroughProgress = 50;
      character.mentalStability = Math.max(0, character.mentalStability - 10);
      events.push({
        id: Math.random().toString(36).substr(2, 9),
        year: currentYear,
        type: 'Warning',
        message: `${character.lastName} ${character.firstName} a échoué son ascension vers ${nextRealm}. Son esprit est ébranlé (-10 Stabilité).`
      });
    } else if (result === 'Major Fail') {
      character.breakthroughProgress = 20;
      character.mentalStability = Math.max(0, character.mentalStability - 20);
      // TODO: Add Dao Wound
      events.push({
        id: Math.random().toString(36).substr(2, 9),
        year: currentYear,
        type: 'Danger',
        message: `Échec majeur ! ${character.lastName} ${character.firstName} a subi une grave blessure spirituelle en tentant de percer vers ${nextRealm} (-20 Stabilité).`
      });
    } else if (result === 'Catastrophic') {
      character.breakthroughProgress = 0;
      character.mentalStability = Math.max(0, character.mentalStability - 50);
      
      if (Math.random() < 0.5) {
        character.isAlive = false;
        events.push({
          id: Math.random().toString(36).substr(2, 9),
          year: currentYear,
          type: 'Danger',
          message: `DÉVIATION DE QI ! ${character.lastName} ${character.firstName} a péri dans une explosion d'énergie en tentant de percer vers ${nextRealm}.`
        });
      } else {
        events.push({
          id: Math.random().toString(36).substr(2, 9),
          year: currentYear,
          type: 'Danger',
          message: `DÉVIATION DE QI ! ${character.lastName} ${character.firstName} a survécu de justesse à son échec vers ${nextRealm}, mais son esprit est brisé.`
        });
      }
    }

    return events;
  }
}
