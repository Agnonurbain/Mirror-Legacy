import { Character, GameEvent } from '../../types/game';

export class MentalStabilitySystem {
  static applyAnnualModifiers(character: Character, currentYear: number): GameEvent[] {
    const events: GameEvent[] = [];
    
    if (!character.isAlive) return events;

    // Base modifiers
    if (character.currentTask === 'Culture') {
      character.mentalStability = Math.min(100, character.mentalStability + 2);
    }

    // Threshold checks
    if (character.mentalStability <= 0) {
      character.isAlive = false;
      events.push({
        id: Math.random().toString(36).substr(2, 9),
        year: currentYear,
        type: 'Danger',
        message: `[Déviation de Qi] L'esprit de ${character.firstName} a sombré dans le chaos. Il/Elle a péri dans une explosion de Qi.`
      });
    } else if (character.mentalStability < 15) {
      if (Math.random() < 0.5) { // 50% chance per year
        character.isAlive = false;
        events.push({
          id: Math.random().toString(36).substr(2, 9),
          year: currentYear,
          type: 'Danger',
          message: `[Déviation de Qi] ${character.firstName} n'a pas pu contrôler ses démons intérieurs et a péri.`
        });
      }
    } else if (character.mentalStability < 30) {
      if (Math.random() < 0.1) { // 10% chance per year
        character.isAlive = false;
        events.push({
          id: Math.random().toString(36).substr(2, 9),
          year: currentYear,
          type: 'Danger',
          message: `[Trahison] Dévoré(e) par ses démons intérieurs, ${character.firstName} a trahi le clan et s'est enfui(e) dans la nuit.`
        });
      }
    } else if (character.mentalStability < 50) {
      if (Math.random() < 0.2) { // Unpredictable events
        character.mentalStability = Math.max(0, character.mentalStability - 10);
        events.push({
          id: Math.random().toString(36).substr(2, 9),
          year: currentYear,
          type: 'Warning',
          message: `[Démons du Cœur] ${character.firstName} souffre d'hallucinations et blesse un disciple. Sa culture stagne.`
        });
      }
    }

    return events;
  }
}
