import { Clan, GameEvent, Character, Facility, Expedition, Recipe } from '../types/game';

export const mockExpeditions: Expedition[] = [
  {
    id: 'exp_1',
    name: 'Chasse aux Bêtes Démoniaques',
    description: 'Traquez les bêtes qui menacent les villages voisins pour gagner du prestige et des ressources.',
    difficulty: 100,
    minRealm: 'Raffinement du Qi',
    rewardType: 'Wealth',
    baseReward: 500,
    risk: 20
  },
  {
    id: 'exp_2',
    name: 'Exploration de Ruines Anciennes',
    description: 'Explorez une grotte scellée par un ancien cultivateur. Risqué mais potentiellement très lucratif.',
    difficulty: 500,
    minRealm: 'Fondation',
    rewardType: 'Item',
    baseReward: 1,
    risk: 50
  },
  {
    id: 'exp_3',
    name: 'Raid sur un Camp de Bandits',
    description: 'Éliminez les bandits qui pillent les caravanes spirituelles.',
    difficulty: 200,
    minRealm: 'Raffinement du Qi',
    rewardType: 'Prestige',
    baseReward: 50,
    risk: 30
  },
  {
    id: 'exp_4',
    name: 'Quête de l\'Herbe de Longévité',
    description: 'Cherchez une herbe rare dans les montagnes interdites pour prolonger la vie des aînés.',
    difficulty: 1000,
    minRealm: 'Manoir Pourpre',
    rewardType: 'Destiny',
    baseReward: 10,
    risk: 70
  },
  {
    id: 'exp_5',
    name: 'Fouille du Pavillon Oublié',
    description: 'Infiltrez les ruines d\'une ancienne secte pour y dérober des fragments de techniques perdues.',
    difficulty: 300,
    minRealm: 'Fondation',
    rewardType: 'Fragment',
    baseReward: 1,
    risk: 40
  }
];

export const mockCharacters: Character[] = [
  {
    id: 'char_1',
    firstName: 'Feng',
    lastName: 'Lin',
    age: 42,
    gender: 'Male',
    maxLifespan: 150,
    generation: 1,
    realm: 'Raffinement du Qi',
    spiritualRoot: 85,
    mentalStability: 78,
    currentTask: 'Culture',
    isAlive: true,
    parents: [],
    children: ['char_2', 'char_3'],
    spouse: 'char_4',
    portraitSeed: 'male-elder-wise',
    traits: ['Génie', 'Inflexible'],
    skills: { alchemy: 20, forging: 10, farming: 5, combat: 80 },
    breakthroughProgress: 45,
    studiedManuals: [],
  },
  {
    id: 'char_4',
    firstName: 'Yue',
    lastName: 'Su',
    age: 38,
    gender: 'Female',
    maxLifespan: 100,
    generation: 1,
    realm: 'Embryonnaire',
    spiritualRoot: 60,
    mentalStability: 90,
    currentTask: 'Alchimie',
    isAlive: true,
    parents: [],
    children: ['char_2', 'char_3'],
    spouse: 'char_1',
    portraitSeed: 'female-elder-woman',
    traits: ['Maître Alchimiste'],
    skills: { alchemy: 95, forging: 15, farming: 40, combat: 30 },
    breakthroughProgress: 12,
    studiedManuals: [],
  },
  {
    id: 'char_2',
    firstName: 'Dong',
    lastName: 'Lin',
    age: 20,
    gender: 'Male',
    maxLifespan: 80,
    generation: 2,
    realm: 'Mortel',
    spiritualRoot: 92,
    mentalStability: 65,
    currentTask: 'Patrouille',
    isAlive: true,
    parents: ['char_1', 'char_4'],
    children: ['char_5'],
    portraitSeed: 'male-young-warrior',
    traits: ['Brave', 'Forgeron'],
    skills: { alchemy: 5, forging: 25, farming: 10, combat: 65 },
    breakthroughProgress: 78,
    studiedManuals: [],
  },
  {
    id: 'char_3',
    firstName: 'Xue',
    lastName: 'Lin',
    age: 18,
    gender: 'Female',
    maxLifespan: 80,
    generation: 2,
    realm: 'Mortel',
    spiritualRoot: 75,
    mentalStability: 88,
    currentTask: 'Culture',
    isAlive: true,
    parents: ['char_1', 'char_4'],
    children: [],
    portraitSeed: 'female-young-woman',
    traits: ['Calme'],
    skills: { alchemy: 10, forging: 5, farming: 55, combat: 20 },
    breakthroughProgress: 92,
    studiedManuals: [],
  },
  {
    id: 'char_5',
    firstName: 'Ming',
    lastName: 'Lin',
    age: 2,
    gender: 'Male',
    maxLifespan: 80,
    generation: 3,
    realm: 'Mortel',
    spiritualRoot: 45,
    mentalStability: 50,
    currentTask: 'Récolte',
    isAlive: true,
    parents: ['char_2'],
    children: [],
    portraitSeed: 'male-boy',
    traits: ['Curieux'],
    skills: { alchemy: 0, forging: 0, farming: 0, combat: 5 },
    breakthroughProgress: 5,
    studiedManuals: [],
  },
  {
    id: 'char_6',
    firstName: 'Hao',
    lastName: 'Lin',
    age: 85,
    gender: 'Male',
    maxLifespan: 250,
    generation: 0,
    realm: 'Fondation',
    spiritualRoot: 55,
    mentalStability: 0,
    currentTask: 'Repos',
    isAlive: false,
    parents: [],
    children: ['char_1'],
    portraitSeed: 'male-ancestor',
    traits: ['Légendaire'],
    skills: { alchemy: 100, forging: 100, farming: 100, combat: 100 },
    breakthroughProgress: 0,
    studiedManuals: [],
  }
];

export const mockRecipes: Recipe[] = [
  {
    id: 'rec_1',
    name: 'Pilule de Fondation',
    description: 'Aide à franchir le royaume de Fondation.',
    ingredients: [{ itemId: 'item_herb_1', quantity: 3 }, { itemId: 'item_spirit_stone', quantity: 100 }],
    resultItemId: 'item_pill_fondation',
    minSkill: 30
  },
  {
    id: 'rec_2',
    name: 'Élixir de Longévité',
    description: 'Augmente l\'espérance de vie de 50 ans.',
    ingredients: [{ itemId: 'item_herb_2', quantity: 1 }, { itemId: 'item_spirit_stone', quantity: 500 }],
    resultItemId: 'item_pill_longevity',
    minSkill: 60
  }
];

export const mockForgeRecipes: Recipe[] = [
  {
    id: 'forge_1',
    name: 'Épée de Fer Froid',
    description: 'Une épée de base imprégnée de Qi, augmente la puissance d\'attaque.',
    ingredients: [{ itemId: 'item_ore_1', quantity: 5 }, { itemId: 'item_spirit_stone', quantity: 200 }],
    resultItemId: 'item_sword_iron',
    minSkill: 20
  },
  {
    id: 'forge_2',
    name: 'Armure d\'Écailles de Wyrm',
    description: 'Une armure résistante qui protège contre les attaques spirituelles.',
    ingredients: [{ itemId: 'item_ore_2', quantity: 3 }, { itemId: 'item_spirit_stone', quantity: 800 }],
    resultItemId: 'item_armor_wyrm',
    minSkill: 50
  },
  {
    id: 'forge_3',
    name: 'Talisman de Foudre',
    description: 'Un artefact à usage unique libérant la foudre céleste.',
    ingredients: [{ itemId: 'item_fragment_lightning', quantity: 2 }, { itemId: 'item_spirit_stone', quantity: 500 }],
    resultItemId: 'item_talisman_lightning',
    minSkill: 70
  }
];

export const mockFacilities: Facility[] = [
  {
    id: 'fac_1',
    name: 'Source de Qi de Montagne',
    level: 1,
    description: 'Une source naturelle qui concentre le Qi ambiant, accélérant la culture.',
    cost: 5000,
    type: 'Cultivation',
    bonus: 10,
  },
  {
    id: 'fac_2',
    name: 'Champs de Ginseng Spirituel',
    level: 1,
    description: 'Des terres fertiles pour cultiver des herbes médicinales précieuses.',
    cost: 3000,
    type: 'Wealth',
    bonus: 500,
  },
  {
    id: 'fac_3',
    name: 'Mur de Protection en Pierre de Jade',
    level: 1,
    description: 'Un mur renforcé de jade pour protéger le clan des bêtes démoniaques.',
    cost: 4000,
    type: 'Defense',
    bonus: 5,
  }
];

export const mockClan: Clan = {
  name: 'Dragon d\'Azur',
  motto: 'S\'élevant à travers les cieux, inflexible face à la terre.',
  wealth: 1500,
  prestige: 50,
  destiny: 10,
  currentYear: 1,
  tier: 1,
  qiDensity: 1.0,
  mirrorPower: 10,
  beasts: [],
  destinyUpgrades: [],
  manuals: [
    {
      id: 'man_1',
      name: 'Respiration du Dragon d\'Azur',
      description: 'Une technique de base pour harmoniser le Qi avec le souffle du dragon.',
      rarity: 'Commun',
      type: 'Culture',
      difficulty: 10,
      bonus: 1.2,
      requirements: { realm: 'Mortel', spiritualRoot: 20 }
    },
    {
      id: 'man_2',
      name: 'Art de l\'Épée de Fer',
      description: 'Techniques fondamentales de combat à l\'épée.',
      rarity: 'Commun',
      type: 'Combat',
      difficulty: 15,
      bonus: 1.3,
      requirements: { realm: 'Mortel', spiritualRoot: 10 }
    }
  ],
  members: mockCharacters,
  facilities: mockFacilities,
  worldStatus: 'Paix Relative',
  worldEvents: [],
  inventory: [
    {
      id: 'item_herb_1',
      name: 'Herbe Spirituelle',
      type: 'Treasure',
      description: 'Une herbe commune imprégnée de Qi.',
      rarity: 'Commun',
      effect: { type: 'Stability', value: 5 },
      quantity: 15
    },
    {
      id: 'item_herb_2',
      name: 'Lotus de Sang',
      type: 'Treasure',
      description: 'Une herbe rare poussant dans les lieux de grand combat.',
      rarity: 'Rare',
      effect: { type: 'Power', value: 20 },
      quantity: 2
    },
    {
      id: 'item_spirit_stone',
      name: 'Pierre d\'Esprit',
      type: 'Treasure',
      description: 'La monnaie de base du monde des cultivateurs.',
      rarity: 'Commun',
      effect: { type: 'Power', value: 1 },
      quantity: 1000
    }
  ],
  sects: [
    { id: 'sect_1', name: 'Secte de l\'Épée Céleste', relation: 'Neutre', power: 'Élevée', description: 'Une secte ancienne spécialisée dans l\'art de l\'épée.', reputation: 0 },
    { id: 'sect_2', name: 'Pavillon des Nuages Pourpres', relation: 'Amical', power: 'Moyenne', description: 'Experts en alchimie et en herbes spirituelles.', reputation: 40 },
    { id: 'sect_3', name: 'Temple du Lotus Noir', relation: 'Hostile', power: 'Élevée', description: 'Une organisation mystérieuse pratiquant des arts interdits.', reputation: -60 },
    { id: 'sect_4', name: 'Clan de la Terre Ferme', relation: 'Neutre', power: 'Faible', description: 'Un clan local axé sur la défense et l\'agriculture.', reputation: 10 },
  ],
};

export const mockEvents: GameEvent[] = [
  { id: 'evt_1', year: 1, type: 'Info', message: 'L\'an 1 commence. Le clan du Dragon d\'Azur s\'établit dans la vallée.' },
];
