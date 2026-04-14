import { Clan, GameEvent, Expedition, Item, SpiritualBeast, Manual, Character, MarketItem } from '../../types/game';
import { CombatState } from '../../types/combat';
import { ClanManager } from '../clan/ClanManager';
import { TimeManager } from './TimeManager';
import { mockExpeditions, mockRecipes, mockForgeRecipes } from '../../data/mockData';
import { REALM_CONFIG, REALMS, FIRST_NAMES, MALE_FIRST_NAMES, FEMALE_FIRST_NAMES, LAST_NAMES, TRAITS } from '../../constants';
import { CultivationSystem } from '../systems/CultivationSystem';
import { GeneticSystem } from '../systems/GeneticSystem';

export class GameManager {
  private static instance: GameManager;
  
  clanManager: ClanManager | null = null;
  events: GameEvent[] = [];
  
  private constructor() {}

  static getInstance(): GameManager {
    if (!GameManager.instance) {
      GameManager.instance = new GameManager();
    }
    return GameManager.instance;
  }

  initialize(clanData: Clan, initialEvents: GameEvent[]): void {
    this.clanManager = new ClanManager(clanData);
    this.events = [...initialEvents];
  }

  async advanceYear(): Promise<{ newClan: Clan; newEvents: GameEvent[] }> {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    
    const newEvents = await TimeManager.advanceYear(this.clanManager);
    this.events = [...newEvents, ...this.events].slice(0, 50);
    
    return {
      newClan: this.clanManager.toData(),
      newEvents: this.events
    };
  }

  // --- Combat & Expeditions ---
  
  resolveCombat(combatState: CombatState): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const expedition = mockExpeditions.find(e => e.id === combatState.expeditionId);
    if (!expedition) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Expédition invalide.' } };

    const success = combatState.status === 'victory';
    
    let wealthGain = 0;
    let prestigeGain = 0;
    let destinyGain = 0;
    let itemGain: Item | null = null;
    let beastGain: SpiritualBeast | null = null;
    let message = '';

    if (success) {
      if (expedition.rewardType === 'Wealth') wealthGain = expedition.baseReward + Math.floor(Math.random() * 200);
      if (expedition.rewardType === 'Prestige') prestigeGain = expedition.baseReward + Math.floor(Math.random() * 20);
      if (expedition.rewardType === 'Destiny') destinyGain = expedition.baseReward;
      if (expedition.rewardType === 'Item') {
        itemGain = {
          id: `item_${Math.random().toString(36).substr(2, 9)}`,
          name: 'Trésor Ancien',
          type: 'Treasure',
          description: 'Un artefact mystérieux trouvé dans des ruines.',
          rarity: 'Épique',
          effect: { type: 'Power', value: 100 },
          quantity: 1
        };
      }
      
      if (expedition.rewardType === 'Fragment') {
        itemGain = {
          id: `frag_${Math.random().toString(36).substr(2, 9)}`,
          name: 'Fragment de Technique',
          type: 'Fragment',
          description: 'Un morceau de parchemin ancien contenant des bribes de savoir martial ou spirituel.',
          rarity: Math.random() > 0.8 ? 'Rare' : 'Commun',
          effect: { type: 'Power', value: 0 },
          quantity: 1
        };
      }
      
      if (Math.random() < 0.15) {
        const elements = ['Feu', 'Eau', 'Bois', 'Métal', 'Terre', 'Foudre', 'Vent', 'Lumière', 'Ténèbres'] as const;
        const rarities = ['Commun', 'Rare', 'Épique', 'Légendaire', 'Mythique'] as const;
        const bonusTypes = ['Cultivation', 'Wealth', 'Combat', 'Alchemy', 'Prestige'] as const;
        const speciesList = ['Loup', 'Tigre', 'Grue', 'Tortue', 'Serpent', 'Renard', 'Aigle', 'Dragon Mineur', 'Qilin'];
        
        const rarityIdx = Math.floor(Math.random() * Math.random() * rarities.length);
        const rarity = rarities[rarityIdx];
        const species = speciesList[Math.floor(Math.random() * speciesList.length)];
        
        beastGain = {
          id: `beast_${Math.random().toString(36).substr(2, 9)}`,
          name: `${species} ${elements[Math.floor(Math.random() * elements.length)]}`,
          species,
          element: elements[Math.floor(Math.random() * elements.length)],
          rarity,
          level: 1,
          experience: 0,
          bonus: {
            type: bonusTypes[Math.floor(Math.random() * bonusTypes.length)],
            value: 5 + (rarityIdx * 5)
          }
        };
      }
      
      message = `L'expédition "${expedition.name}" est un succès total !`;
      if (beastGain) {
        message += ` L'équipe a apprivoisé une bête spirituelle : ${beastGain.name} !`;
      }
    } else {
      wealthGain = -100;
      prestigeGain = -10;
      message = `L'expédition "${expedition.name}" a échoué. L'équipe a dû battre en retraite.`;
    }

    clan.bloodRegistry.getAllMembers().forEach(m => {
      const combatant = combatState.combatants.find(c => c.id === m.id);
      if (combatant) {
        const hpLost = combatant.maxHp - combatant.hp;
        if (hpLost > 0) {
          m.mentalStability = Math.max(0, m.mentalStability - Math.floor(hpLost / 5));
        }
      }
    });

    if (wealthGain > 0) clan.resourceManager.addWealth(wealthGain);
    else if (wealthGain < 0) clan.resourceManager.spendWealth(-wealthGain);
    
    if (prestigeGain > 0) clan.resourceManager.addPrestige(prestigeGain);
    else if (prestigeGain < 0) clan.resourceManager.spendPrestige(-prestigeGain);
    
    if (destinyGain > 0) clan.resourceManager.addDestiny(destinyGain);

    if (itemGain) clan.addItem(itemGain);
    if (beastGain) clan.addBeast(beastGain);

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: success ? 'Success' : 'Danger',
      message
    };
    
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  // --- Sects ---
  
  interactWithSect(sectId: string, action: 'Diplomacy' | 'Tribute' | 'Challenge'): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const sect = clan.sects.find(s => s.id === sectId);
    if (!sect) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Secte non trouvée.' } };

    let wealthChange = 0;
    let prestigeChange = 0;
    let reputationChange = 0;
    let message = '';
    let type: GameEvent['type'] = 'Info';

    if (action === 'Diplomacy') {
      if (clan.resourceManager.spendWealth(500)) {
        reputationChange = 10 + Math.floor(Math.random() * 15);
        message = `Nos diplomates ont visité la ${sect.name}. Les relations se sont améliorées.`;
        type = 'Success';
      } else {
        message = "Pas assez de Spirit Stones pour la diplomatie.";
        type = 'Warning';
      }
    } else if (action === 'Tribute') {
      if (clan.resourceManager.spendWealth(2000)) {
        reputationChange = 30 + Math.floor(Math.random() * 20);
        prestigeChange = -5;
        message = `Nous avons envoyé un tribut généreux à la ${sect.name}. Ils nous voient d'un bon œil.`;
        type = 'Success';
      } else {
        message = "Pas assez de Spirit Stones pour un tribut.";
        type = 'Warning';
      }
    } else if (action === 'Challenge') {
      let power = clan.bloodRegistry.getLivingMembers().reduce((sum, m) => {
        let memberPower = REALM_CONFIG[m.realm].power;
        const memberBeast = (clan.beasts || []).find(b => b.assignedTo === m.id);
        if (memberBeast?.bonus.type === 'Combat') {
          memberPower *= 1 + (memberBeast.bonus.value / 100);
        }
        return sum + memberPower;
      }, 0);

      let clanBeastCombatBonus = 0;
      (clan.beasts || []).forEach(b => {
        if (!b.assignedTo && b.bonus.type === 'Combat') {
          clanBeastCombatBonus += b.bonus.value;
        }
      });
      power *= 1 + (clanBeastCombatBonus / 100);

      const success = Math.random() * power > (sect.power === 'Légendaire' ? 10000 : sect.power === 'Élevée' ? 5000 : 2000);
      
      if (success) {
        prestigeChange = 50;
        reputationChange = -40;
        message = `Nos guerriers ont défié et vaincu les disciples de la ${sect.name} ! Notre prestige explose.`;
        type = 'Success';
      } else {
        prestigeChange = -20;
        reputationChange = -20;
        message = `Notre défi à la ${sect.name} s'est soldé par une défaite humiliante.`;
        type = 'Danger';
      }
    }

    if (prestigeChange > 0) clan.resourceManager.addPrestige(prestigeChange);
    else if (prestigeChange < 0) clan.resourceManager.spendPrestige(-prestigeChange);

    const newRep = Math.max(-100, Math.min(100, sect.reputation + reputationChange));
    let newRelation = sect.relation;
    if (newRep <= -50) newRelation = 'Hostile';
    else if (newRep >= 50) newRelation = 'Allié';
    else if (newRep >= 20) newRelation = 'Amical';
    else newRelation = 'Neutre';
    
    sect.reputation = newRep;
    sect.relation = newRelation;

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type,
      message
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  // --- Crafting & Forging ---
  
  craftItem(recipeId: string, alchemistId: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const recipe = mockRecipes.find(r => r.id === recipeId);
    const alchemist = clan.bloodRegistry.getMember(alchemistId);
    if (!recipe || !alchemist) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Recette ou alchimiste invalide.' } };

    const hasIngredients = recipe.ingredients.every(ing => {
      const item = clan.inventory.find(i => i.id === ing.itemId);
      return item && item.quantity >= ing.quantity;
    });

    if (!hasIngredients) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Ingrédients insuffisants.' } };

    recipe.ingredients.forEach(ing => clan.removeItem(ing.itemId, ing.quantity));

    const assignedBeast = (clan.beasts || []).find(b => b.assignedTo === alchemist.id);
    const beastAlchemyBonus = assignedBeast?.bonus.type === 'Alchemy' ? assignedBeast.bonus.value : 0;
    
    const successChance = Math.min(0.95, (alchemist.skills.alchemy + beastAlchemyBonus) / Math.max(1, recipe.minSkill));
    const success = Math.random() < successChance;

    if (success) {
      const newItem: Item = {
        id: `item_${Math.random().toString(36).substr(2, 9)}`,
        name: recipe.name,
        type: 'Pill',
        description: recipe.description,
        rarity: 'Rare',
        effect: { type: 'Breakthrough', value: 10 },
        quantity: 1
      };
      clan.addItem(newItem);
      alchemist.skills.alchemy = Math.min(100, alchemist.skills.alchemy + 2);
      
      const event: GameEvent = {
        id: Math.random().toString(36).substr(2, 9),
        year: clan.currentYear,
        type: 'Success',
        message: `Succès ! ${alchemist.firstName} a raffiné ${recipe.name}.`
      };
      this.events.unshift(event);
      return { newClan: clan.toData(), event };
    } else {
      alchemist.skills.alchemy = Math.min(100, alchemist.skills.alchemy + 1);
      const event: GameEvent = {
        id: Math.random().toString(36).substr(2, 9),
        year: clan.currentYear,
        type: 'Danger',
        message: `Échec. ${alchemist.firstName} a raté la concoction de ${recipe.name}, perdant les ingrédients.`
      };
      this.events.unshift(event);
      return { newClan: clan.toData(), event };
    }
  }

  forgeArtifact(recipeId: string, smithId: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const recipe = mockForgeRecipes.find(r => r.id === recipeId);
    const smith = clan.bloodRegistry.getMember(smithId);
    if (!recipe || !smith) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Recette ou forgeron invalide.' } };

    const hasIngredients = recipe.ingredients.every(ing => {
      const item = clan.inventory.find(i => i.id === ing.itemId);
      return item && item.quantity >= ing.quantity;
    });

    if (!hasIngredients) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Ingrédients insuffisants.' } };

    recipe.ingredients.forEach(ing => clan.removeItem(ing.itemId, ing.quantity));

    const assignedBeast = (clan.beasts || []).find(b => b.assignedTo === smith.id);
    const beastForgeBonus = assignedBeast?.bonus.type === 'Forging' ? assignedBeast.bonus.value : 0;
    
    const successChance = Math.min(0.95, (smith.skills.forging + beastForgeBonus) / Math.max(1, recipe.minSkill));
    const success = Math.random() < successChance;

    if (success) {
      const newItem: Item = {
        id: `item_${Math.random().toString(36).substr(2, 9)}`,
        name: recipe.name,
        type: 'Equipment',
        description: recipe.description,
        rarity: 'Rare',
        effect: { type: 'Power', value: 10 },
        quantity: 1
      };
      clan.addItem(newItem);
      smith.skills.forging = Math.min(100, smith.skills.forging + 2);
      
      const event: GameEvent = {
        id: Math.random().toString(36).substr(2, 9),
        year: clan.currentYear,
        type: 'Success',
        message: `Succès ! ${smith.firstName} a forgé ${recipe.name}.`
      };
      this.events.unshift(event);
      return { newClan: clan.toData(), event };
    } else {
      smith.skills.forging = Math.min(100, smith.skills.forging + 1);
      const event: GameEvent = {
        id: Math.random().toString(36).substr(2, 9),
        year: clan.currentYear,
        type: 'Danger',
        message: `Échec. ${smith.firstName} a raté la forge de ${recipe.name}, perdant les matériaux.`
      };
      this.events.unshift(event);
      return { newClan: clan.toData(), event };
    }
  }

  // --- Divine Powers ---
  
  async useDivinePower(powerId: string, targetId: string): Promise<{ newClan: Clan; event: GameEvent }> {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    
    let cost = 0;
    let message = '';
    let type: GameEvent['type'] = 'Success';

    if (powerId === 'heal') {
      cost = 20;
      if (clan.mirrorPower < cost) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Énergie divine insuffisante.' } };
      const target = clan.bloodRegistry.getMember(targetId);
      if (target) {
        target.mentalStability = 100;
        message = `L'esprit de ${target.firstName} a été purifié par la lumière divine.`;
      }
    } else if (powerId === 'protect') {
      cost = 50;
      if (clan.mirrorPower < cost) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Énergie divine insuffisante.' } };
      const target = clan.bloodRegistry.getMember(targetId);
      if (target) {
        target.isProtectedByMirror = true;
        message = `${target.firstName} est maintenant protégé(e) par le Bouclier Ancestral contre la prochaine tribulation.`;
      }
    } else if (powerId === 'enlighten') {
      cost = 80;
      if (clan.mirrorPower < cost) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Énergie divine insuffisante.' } };
      const target = clan.bloodRegistry.getMember(targetId);
      if (target) {
        target.breakthroughProgress = 100;
        message = `${target.firstName} a reçu l'Illumination Divine et est prêt(e) pour une percée !`;
      }
    } else if (powerId === 'bless_land') {
      cost = 100;
      if (clan.mirrorPower < cost) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Énergie divine insuffisante.' } };
      clan.worldStatus = 'Marée de Qi';
      message = `Les terres du clan ont été bénies. Une Marée de Qi déferle sur le domaine !`;
    }

    if (message) {
      clan.mirrorPower -= cost;
      const event: GameEvent = {
        id: Math.random().toString(36).substr(2, 9),
        year: clan.currentYear,
        type,
        message
      };
      this.events.unshift(event);
      return { newClan: clan.toData(), event };
    }

    return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Pouvoir inconnu.' } };
  }

  deduceFromFragments(fragmentIds: string[]): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const costPower = fragmentIds.length * 10;
    
    if (clan.mirrorPower < costPower) {
      return {
        newClan: clan.toData(),
        event: { id: Math.random().toString(36).substr(2, 9), year: clan.currentYear, type: 'Danger', message: `Énergie Divine insuffisante pour la déduction (Requis: ${costPower}).` }
      };
    }

    let totalRarityScore = 0;
    for (const fragId of fragmentIds) {
      const item = clan.inventory.find(i => i.id === fragId && i.type === 'Fragment');
      if (!item) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Fragment introuvable.' } };
      
      if (item.rarity === 'Commun') totalRarityScore += 1;
      else if (item.rarity === 'Rare') totalRarityScore += 2;
      else if (item.rarity === 'Épique') totalRarityScore += 3;
      else if (item.rarity === 'Légendaire') totalRarityScore += 4;

      clan.removeItem(fragId, 1);
    }

    const rarities = ['Commun', 'Rare', 'Épique', 'Légendaire'];
    const types: Manual['type'][] = ['Culture', 'Combat', 'Alchimie', 'Forge'];
    
    let newRarityIdx = 0;
    if (totalRarityScore >= 10) newRarityIdx = 3;
    else if (totalRarityScore >= 6) newRarityIdx = 2;
    else if (totalRarityScore >= 3) newRarityIdx = 1;

    if (Math.random() < 0.2 && newRarityIdx < 3) newRarityIdx++;

    const newRarity = rarities[newRarityIdx] as Manual['rarity'];
    const newType = types[Math.floor(Math.random() * types.length)];
    
    const prefixes = ['Sutra', 'Art', 'Technique', 'Livre', 'Secret', 'Voie', 'Parchemin', 'Mantra'];
    const suffixes = ['Céleste', 'du Vide', 'Ancestral', 'Divin', 'des Étoiles', 'du Dragon', 'des Ombres', 'Éternel'];
    const newName = `${prefixes[Math.floor(Math.random() * prefixes.length)]} ${suffixes[Math.floor(Math.random() * suffixes.length)]}`;

    const newBonus = 1 + (newRarityIdx * 0.2) + (fragmentIds.length * 0.05);
    const newDifficulty = 10 + (newRarityIdx * 20);
    const newRealmReq = REALMS[Math.min(REALMS.length - 1, newRarityIdx + 1)];

    const newManual: Manual = {
      id: `man_${Math.random().toString(36).substr(2, 9)}`,
      name: newName,
      description: `Une technique déduite par le Miroir de Bronze à partir de ${fragmentIds.length} fragments.`,
      rarity: newRarity,
      type: newType,
      difficulty: newDifficulty,
      bonus: newBonus,
      requirements: { realm: newRealmReq, spiritualRoot: 20 + (newRarityIdx * 15) }
    };

    clan.mirrorPower -= costPower;
    clan.addManual(newManual);

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Success',
      message: `Déduction réussie : Le Miroir a synthétisé les fragments pour créer le manuel [${newRarity}] ${newName} !`
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  deduceManual(manualId1: string, manualId2: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const costDestiny = 100;

    if (clan.resourceManager.destiny < costDestiny) {
      return {
        newClan: clan.toData(),
        event: { id: Math.random().toString(36).substr(2, 9), year: clan.currentYear, type: 'Danger', message: `Destinée insuffisante pour la déduction (Requis: ${costDestiny}).` }
      };
    }

    const m1 = clan.manuals.find(m => m.id === manualId1);
    const m2 = clan.manuals.find(m => m.id === manualId2);

    if (!m1 || !m2) {
      return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Manuels introuvables.' } };
    }

    const rarities = ['Commun', 'Rare', 'Épique', 'Légendaire'];
    const r1Idx = rarities.indexOf(m1.rarity);
    const r2Idx = rarities.indexOf(m2.rarity);
    const maxRarityIdx = Math.max(r1Idx, r2Idx);
    
    let newRarityIdx = maxRarityIdx;
    if (Math.random() < 0.3 && newRarityIdx < rarities.length - 1) {
      newRarityIdx += 1;
    }

    const newRarity = rarities[newRarityIdx] as Manual['rarity'];
    const newType = Math.random() > 0.5 ? m1.type : m2.type;
    
    const prefixes = ['Sutra', 'Art', 'Technique', 'Livre', 'Secret', 'Voie'];
    const suffixes = ['Céleste', 'du Vide', 'Ancestral', 'Divin', 'des Étoiles', 'du Dragon'];
    const newName = `${prefixes[Math.floor(Math.random() * prefixes.length)]} ${suffixes[Math.floor(Math.random() * suffixes.length)]}`;

    const newBonus = Math.max(m1.bonus, m2.bonus) + (newRarityIdx * 0.2) + 0.1;
    const newDifficulty = Math.max(m1.difficulty, m2.difficulty) + 10;
    
    const realmReqIdx = Math.max(REALMS.indexOf(m1.requirements.realm), REALMS.indexOf(m2.requirements.realm));
    const newRealmReq = REALMS[Math.min(REALMS.length - 1, realmReqIdx + (newRarityIdx > maxRarityIdx ? 1 : 0))];

    const newManual: Manual = {
      id: `man_${Math.random().toString(36).substr(2, 9)}`,
      name: newName,
      description: `Une technique déduite de la fusion de ${m1.name} et ${m2.name}.`,
      rarity: newRarity,
      type: newType,
      difficulty: newDifficulty,
      bonus: Number(newBonus.toFixed(2)),
      requirements: {
        realm: newRealmReq,
        spiritualRoot: Math.max(m1.requirements.spiritualRoot, m2.requirements.spiritualRoot) + 5
      }
    };

    clan.removeManual(manualId1);
    clan.removeManual(manualId2);
    clan.addManual(newManual);

    clan.bloodRegistry.getAllMembers().forEach(newMember => {
      if (newMember.assignedManualId === manualId1 || newMember.assignedManualId === manualId2) {
        newMember.assignedManualId = undefined;
        newMember.currentTask = 'Repos';
      }
      if (newMember.studiedManuals) {
        newMember.studiedManuals = newMember.studiedManuals.filter(id => id !== manualId1 && id !== manualId2);
      }
    });

    clan.resourceManager.spendDestiny(costDestiny);

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Success',
      message: `Déduction réussie ! Vous avez sacrifié deux manuels pour créer la technique ${newRarity} : "${newManual.name}".`
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  // --- Characters ---
  
  arrangeMarriage(memberId: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const m1 = clan.bloodRegistry.getMember(memberId);
    if (!m1 || !m1.isAlive) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Membre invalide.' } };

    if (m1.spouse) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Warning', message: 'Déjà marié.' } };

    const cost = 500;
    if (!clan.resourceManager.spendWealth(cost)) {
      return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Warning', message: `Pas assez de Spirit Stones pour organiser un mariage (Requis: ${cost}).` } };
    }

    const gender = m1.gender === 'Male' ? 'Female' : 'Male';
    const firstName = gender === 'Male' 
      ? MALE_FIRST_NAMES[Math.floor(Math.random() * MALE_FIRST_NAMES.length)]
      : FEMALE_FIRST_NAMES[Math.floor(Math.random() * FEMALE_FIRST_NAMES.length)];
    
    const spouse: Character = {
      id: `char_spouse_${Math.random().toString(36).substr(2, 9)}`,
      firstName,
      lastName: LAST_NAMES[Math.floor(Math.random() * LAST_NAMES.length)],
      age: m1.age + Math.floor(Math.random() * 10) - 5,
      gender,
      maxLifespan: CultivationSystem.getBaseLifespan('Mortel'),
      generation: m1.generation,
      realm: 'Mortel',
      spiritualRoot: Math.max(10, m1.spiritualRoot - 20 + Math.floor(Math.random() * 40)),
      mentalStability: 100,
      currentTask: 'Repos',
      isAlive: true,
      parents: [],
      children: [],
      spouse: m1.id,
      portraitSeed: `${gender.toLowerCase()}-${Math.random().toString(36).substr(2, 9)}`,
      traits: [TRAITS[Math.floor(Math.random() * TRAITS.length)]],
      skills: { alchemy: 0, forging: 0, farming: 0, combat: 0 },
      breakthroughProgress: 0,
      studiedManuals: []
    };

    m1.spouse = spouse.id;
    clan.addMember(spouse);
    clan.resourceManager.addPrestige(20);

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Success',
      message: `Mariage arrangé ! ${m1.firstName} a épousé ${spouse.firstName} ${spouse.lastName}. Le clan gagne 20 Prestige.`
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  // --- Beasts ---
  
  feedBeast(beastId: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const beast = clan.beasts.find(b => b.id === beastId);
    if (!beast) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Bête introuvable.' } };

    const cost = beast.level * 50;
    if (!clan.resourceManager.spendWealth(cost)) {
      return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Warning', message: `Pas assez de Spirit Stones pour nourrir ${beast.name} (Requis: ${cost}).` } };
    }

    beast.experience += 50;
    let message = `${beast.name} a été nourri et gagne 50 XP.`;

    if (beast.experience >= beast.level * 100) {
      beast.experience -= beast.level * 100;
      beast.level += 1;
      beast.bonus.value += 2;
      message = `${beast.name} monte au niveau ${beast.level} ! Son bonus passe à ${beast.bonus.value}.`;
    }

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Success',
      message
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  assignBeast(beastId: string, memberId?: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const beast = clan.beasts.find(b => b.id === beastId);
    if (!beast) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Bête introuvable.' } };

    beast.assignedTo = memberId;
    
    let message = memberId 
      ? `${beast.name} a été assigné à ${clan.bloodRegistry.getMember(memberId)?.firstName}.`
      : `${beast.name} garde maintenant le domaine du clan.`;

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Info',
      message
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }

  // --- Market ---
  
  buyMarketItem(itemId: string): { newClan: Clan; event: GameEvent } {
    if (!this.clanManager) throw new Error("GameManager not initialized");
    const clan = this.clanManager;
    const marketItem = clan.marketItems?.find(i => i.id === itemId);
    
    if (!marketItem) return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Danger', message: 'Objet introuvable sur le marché.' } };
    
    if (!clan.resourceManager.spendWealth(marketItem.cost)) {
      return { newClan: clan.toData(), event: { id: 'err', year: clan.currentYear, type: 'Warning', message: `Fonds insuffisants. Requis: ${marketItem.cost} Spirit Stones.` } };
    }

    let message = '';
    if (marketItem.type === 'Item' && marketItem.item) {
      clan.addItem(marketItem.item);
      message = `Vous avez acheté ${marketItem.item.name}.`;
    } else if (marketItem.type === 'Manual' && marketItem.manual) {
      clan.addManual(marketItem.manual);
      message = `Vous avez acheté le manuel ${marketItem.manual.name}.`;
    } else if (marketItem.type === 'Beast' && marketItem.beast) {
      clan.addBeast(marketItem.beast);
      message = `Vous avez acquis la bête spirituelle ${marketItem.beast.name}.`;
    }

    if (clan.marketItems) {
      clan.marketItems = clan.marketItems.filter(i => i.id !== itemId);
    }

    const event: GameEvent = {
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Success',
      message
    };
    this.events.unshift(event);
    return { newClan: clan.toData(), event };
  }
}
