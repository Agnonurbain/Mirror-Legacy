export type CultivationRealm = 'Mortel' | 'Embryonnaire' | 'Raffinement du Qi' | 'Fondation' | 'Manoir Pourpre' | "Noyau d'Or" | 'Embryon Dao' | 'Immortel';
export type TaskType = 'Repos' | 'Culture' | 'Récolte' | 'Patrouille' | 'Alchimie' | 'Forge' | 'Étude';

export type ItemType = 'Pill' | 'Equipment' | 'Treasure' | 'Fragment';

export interface Item {
  id: string;
  name: string;
  type: ItemType;
  description: string;
  rarity: 'Commun' | 'Rare' | 'Épique' | 'Légendaire';
  effect: {
    type: 'Breakthrough' | 'Lifespan' | 'Power' | 'Stability';
    value: number;
  };
  quantity: number;
}

export interface Character {
  id: string;
  firstName: string;
  lastName: string;
  age: number;
  gender: 'Male' | 'Female';
  maxLifespan: number;
  generation: number;
  realm: CultivationRealm;
  spiritualRoot: number; // 0-100
  mentalStability: number; // 0-100
  currentTask: TaskType;
  isAlive: boolean;
  parents: string[]; // IDs
  children: string[]; // IDs
  spouse?: string; // ID
  portraitSeed: string; // Used for picsum or avatar generation
  traits: string[];
  backstory?: string;
  skills: {
    alchemy: number;
    forging: number;
    farming: number;
    combat: number;
  };
  breakthroughProgress: number; // 0-100
  studiedManuals: string[]; // IDs of mastered manuals
  assignedManualId?: string; // Manual currently being studied
  isProtectedByMirror?: boolean; // Bouclier Ancestral
}

export interface Sect {
  id: string;
  name: string;
  relation: 'Hostile' | 'Neutre' | 'Amical' | 'Allié';
  power: 'Faible' | 'Moyenne' | 'Élevée' | 'Légendaire';
  description: string;
  reputation: number; // -100 to 100
}

export interface Facility {
  id: string;
  name: string;
  level: number;
  description: string;
  cost: number;
  type: 'Cultivation' | 'Wealth' | 'Defense' | 'Prestige';
  bonus: number; // Percentage or flat value
}

export interface WorldEvent {
  id: string;
  name: string;
  description: string;
  duration: number; // Years remaining
  type: 'Positive' | 'Negative' | 'Neutral';
  effects: {
    cultivationSpeed?: number; // Multiplier
    wealthGain?: number; // Multiplier
    prestigeGain?: number; // Multiplier
    dangerLevel?: number; // Flat increase to risk
  };
}

export interface Manual {
  id: string;
  name: string;
  description: string;
  rarity: 'Commun' | 'Rare' | 'Épique' | 'Légendaire';
  type: 'Culture' | 'Combat' | 'Alchimie' | 'Forge';
  difficulty: number;
  bonus: number; // Multiplier or flat bonus
  requirements: {
    realm: CultivationRealm;
    spiritualRoot: number;
  };
}

export interface SpiritualBeast {
  id: string;
  name: string;
  species: string;
  element: 'Feu' | 'Eau' | 'Bois' | 'Métal' | 'Terre' | 'Foudre' | 'Vent' | 'Lumière' | 'Ténèbres';
  rarity: 'Commun' | 'Rare' | 'Épique' | 'Légendaire' | 'Mythique';
  level: number;
  experience: number;
  bonus: {
    type: 'Cultivation' | 'Wealth' | 'Combat' | 'Alchemy' | 'Forging' | 'Prestige';
    value: number;
  };
  assignedTo?: string; // ID of the member it's assigned to, or undefined for clan-wide
}

export interface MarketItem {
  id: string;
  type: 'Item' | 'Manual' | 'Beast';
  item?: Item;
  manual?: Manual;
  beast?: SpiritualBeast;
  cost: number;
}

export interface Clan {
  name: string;
  motto: string;
  wealth: number;
  prestige: number;
  destiny: number;
  currentYear: number;
  tier: number;
  members: Character[];
  facilities: Facility[];
  worldStatus: string;
  worldEvents: WorldEvent[];
  inventory: Item[];
  sects: Sect[];
  qiDensity: number; // 0-100, affects cultivation speed
  manuals: Manual[];
  mirrorPower: number; // 0-100
  beasts: SpiritualBeast[];
  destinyUpgrades: string[]; // IDs of unlocked destiny upgrades
  marketItems?: MarketItem[]; // Items currently available in the market
}

export interface GameEvent {
  id: string;
  year: number;
  type: 'Info' | 'Success' | 'Warning' | 'Danger';
  message: string;
}

export interface Expedition {
  id: string;
  name: string;
  description: string;
  difficulty: number;
  minRealm: CultivationRealm;
  rewardType: 'Wealth' | 'Prestige' | 'Item' | 'Destiny' | 'Fragment';
  baseReward: number;
  risk: number; // 0-100
}

export interface ActiveExpedition {
  expeditionId: string;
  leaderId: string;
  memberIds: string[];
  startTime: number;
  duration: number; // in milliseconds or turns
}

export interface Recipe {
  id: string;
  name: string;
  description: string;
  ingredients: { itemId: string; quantity: number }[];
  resultItemId: string;
  minSkill: number;
}
