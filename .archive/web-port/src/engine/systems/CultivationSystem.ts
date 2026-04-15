import { Character, CultivationRealm } from '../../types/game';

export const REALM_REQUIREMENTS: Record<CultivationRealm, { minRoot: number; minAge: number; baseRisk: number; baseLifespan: [number, number] }> = {
  'Mortel': { minRoot: 0, minAge: 0, baseRisk: 0, baseLifespan: [60, 70] },
  'Embryonnaire': { minRoot: 0, minAge: 0, baseRisk: 0, baseLifespan: [70, 80] },
  'Raffinement du Qi': { minRoot: 10, minAge: 12, baseRisk: 5, baseLifespan: [100, 120] },
  'Fondation': { minRoot: 30, minAge: 17, baseRisk: 15, baseLifespan: [250, 300] },
  'Manoir Pourpre': { minRoot: 50, minAge: 37, baseRisk: 30, baseLifespan: [500, 800] },
  "Noyau d'Or": { minRoot: 70, minAge: 137, baseRisk: 50, baseLifespan: [1500, 2000] },
  'Embryon Dao': { minRoot: 90, minAge: 637, baseRisk: 70, baseLifespan: [10000, 10000] },
  'Immortel': { minRoot: 100, minAge: 1000, baseRisk: 90, baseLifespan: [99999, 99999] }
};

export class CultivationSystem {
  static getBaseLifespan(realm: CultivationRealm): number {
    const [min, max] = REALM_REQUIREMENTS[realm].baseLifespan;
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  static canAttemptBreakthrough(character: Character, nextRealm: CultivationRealm): boolean {
    const req = REALM_REQUIREMENTS[nextRealm];
    if (!req) return false;
    
    if (character.spiritualRoot < req.minRoot) return false;
    if (character.age < req.minAge) return false;
    
    return true;
  }
}
