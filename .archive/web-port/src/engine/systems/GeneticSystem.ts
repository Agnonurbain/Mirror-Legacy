import { Character } from '../../types/game';

export class GeneticSystem {
  /**
   * Calculates the Spiritual Root of a child based on parents.
   * Formula: (Father.Root + Mother.Root) / 2 + Random(-15, +15)
   * Clamped between 1 and 100.
   */
  static calculateSpiritualRoot(parent1: Character, parent2: Character): number {
    const avgRoot = (parent1.spiritualRoot + parent2.spiritualRoot) / 2;
    const mutation = (Math.random() * 30) - 15; // -15 to +15 variance
    return Math.min(100, Math.max(1, Math.floor(avgRoot + mutation)));
  }

  /**
   * Determines the quality of the Spiritual Root.
   */
  static getRootQuality(root: number): string {
    if (root <= 20) return 'Déchets';
    if (root <= 40) return 'Médiocre';
    if (root <= 60) return 'Moyen';
    if (root <= 80) return 'Excellent';
    if (root <= 95) return 'Génie';
    return 'Prodige Céleste';
  }
}
