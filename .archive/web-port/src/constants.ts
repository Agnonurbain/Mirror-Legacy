import { CultivationRealm, TaskType } from './types/game';

export const REALMS: CultivationRealm[] = [
  'Mortel',
  'Embryonnaire',
  'Raffinement du Qi',
  'Fondation',
  'Manoir Pourpre',
  "Noyau d'Or",
  'Embryon Dao',
  'Immortel'
];

export const REALM_CONFIG: Record<CultivationRealm, { maxAge: number; breakthroughChance: number; power: number; baseProgress: number; minRoot: number; baseRisk: number }> = {
  'Mortel': { maxAge: 70, breakthroughChance: 0.8, power: 1, baseProgress: 10, minRoot: 0, baseRisk: 0 },
  'Embryonnaire': { maxAge: 80, breakthroughChance: 0.6, power: 5, baseProgress: 4, minRoot: 0, baseRisk: 0 },
  'Raffinement du Qi': { maxAge: 120, breakthroughChance: 0.4, power: 20, baseProgress: 2, minRoot: 10, baseRisk: 5 },
  'Fondation': { maxAge: 300, breakthroughChance: 0.2, power: 100, baseProgress: 1, minRoot: 30, baseRisk: 15 },
  'Manoir Pourpre': { maxAge: 800, breakthroughChance: 0.1, power: 500, baseProgress: 0.5, minRoot: 50, baseRisk: 30 },
  "Noyau d'Or": { maxAge: 2000, breakthroughChance: 0.05, power: 2500, baseProgress: 0.2, minRoot: 70, baseRisk: 50 },
  'Embryon Dao': { maxAge: 10000, breakthroughChance: 0.02, power: 10000, baseProgress: 0.1, minRoot: 90, baseRisk: 70 },
  'Immortel': { maxAge: 999999, breakthroughChance: 0, power: 100000, baseProgress: 0, minRoot: 100, baseRisk: 90 },
};

export const TASKS: TaskType[] = [
  'Culture',
  'Récolte',
  'Patrouille',
  'Alchimie',
  'Forge',
  'Étude',
  'Repos'
];

export const TRAITS = [
  'Génie',
  'Brave',
  'Calme',
  'Curieux',
  'Diligent',
  'Chanceux',
  'Maître Alchimiste',
  'Inflexible'
];

export const MALE_FIRST_NAMES = ['Feng', 'Dong', 'Ming', 'Hao', 'Chen', 'Bo', 'An', 'Jian', 'Long', 'Wei', 'Li', 'Jun', 'Lei', 'Qiang', 'Tao', 'Wen', 'Zhen'];
export const FEMALE_FIRST_NAMES = ['Xue', 'Yue', 'Mei', 'Lan', 'Zhi', 'Ruo', 'Ling', 'Yan', 'Fang', 'Jia', 'Hui', 'Qing', 'Hua', 'Xin'];
export const LAST_NAMES = ['Lin', 'Ye', 'Xiao', 'Chu', 'Han', 'Zhang', 'Wang', 'Li', 'Liu', 'Chen', 'Yang', 'Zhao', 'Huang', 'Zhou', 'Wu'];

export const FIRST_NAMES = [...MALE_FIRST_NAMES, ...FEMALE_FIRST_NAMES];

export interface DestinyUpgrade {
  id: string;
  name: string;
  description: string;
  cost: number;
  icon: string;
  type: 'cultivation' | 'birth' | 'breakthrough' | 'lifespan' | 'combat';
}

export const DESTINY_UPGRADES: DestinyUpgrade[] = [
  {
    id: 'destiny_cultivation_1',
    name: 'Bénédiction Céleste',
    description: '+10% Vitesse de culture globale pour tous les membres du clan.',
    cost: 100,
    icon: 'Sparkles',
    type: 'cultivation'
  },
  {
    id: 'destiny_birth_1',
    name: 'Racines Profondes',
    description: 'Les nouveaux nés ont +10 Racine Spirituelle minimum.',
    cost: 200,
    icon: 'Leaf',
    type: 'birth'
  },
  {
    id: 'destiny_breakthrough_1',
    name: 'Aura de Chance',
    description: '+5% de chance de succès lors de toutes les percées.',
    cost: 150,
    icon: 'Star',
    type: 'breakthrough'
  },
  {
    id: 'destiny_lifespan_1',
    name: 'Longévité Accrue',
    description: '+10 ans d\'espérance de vie de base pour tous les membres.',
    cost: 300,
    icon: 'Heart',
    type: 'lifespan'
  },
  {
    id: 'destiny_combat_1',
    name: 'Esprit Martial',
    description: '+10% Puissance de combat lors des expéditions.',
    cost: 250,
    icon: 'Sword',
    type: 'combat'
  }
];
