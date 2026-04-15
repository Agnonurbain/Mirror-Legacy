import { ClanManager } from '../clan/ClanManager';
import { GameEvent, Character, Item, Manual, SpiritualBeast, WorldEvent, MarketItem } from '../../types/game';
import { REALMS, REALM_CONFIG, FIRST_NAMES, TRAITS } from '../../constants';
import { BreakthroughSystem } from '../systems/BreakthroughSystem';
import { GeneticSystem } from '../systems/GeneticSystem';
import { MentalStabilitySystem } from '../systems/MentalStabilitySystem';
import { CultivationSystem } from '../systems/CultivationSystem';

export class TimeManager {
  static async advanceYear(clanManager: ClanManager): Promise<GameEvent[]> {
    const events: GameEvent[] = [];
    clanManager.currentYear += 1;
    const currentYear = clanManager.currentYear;
    let mirrorPowerGain = 0.2; // Passive regeneration
    
    // 0. Update Qi Density
    const baseQi = 20;
    const facilityQi = clanManager.facilities.find(f => f.type === 'Cultivation')?.bonus || 0;
    const worldQiModifier = clanManager.worldStatus === 'Marée de Qi' ? 1.5 : clanManager.worldStatus === 'Sécheresse Spirituelle' ? 0.5 : 1;
    clanManager.qiDensity = Math.min(100, Math.max(0, (baseQi + facilityQi) * worldQiModifier));

    // 1. Process Ongoing World Events
    clanManager.worldEvents = (clanManager.worldEvents || [])
      .map(we => ({ ...we, duration: we.duration - 1 }))
      .filter(we => we.duration > 0);

    const worldModifiers = {
      cultivation: clanManager.worldEvents.reduce((acc, we) => acc * (we.effects.cultivationSpeed || 1), 1) * (1 + clanManager.qiDensity / 100),
      wealth: clanManager.worldEvents.reduce((acc, we) => acc * (we.effects.wealthGain || 1), 1),
      prestige: clanManager.worldEvents.reduce((acc, we) => acc * (we.effects.prestigeGain || 1), 1),
      danger: clanManager.worldEvents.reduce((acc, we) => acc + (we.effects.dangerLevel || 0), 0),
    };

    // 2. Resource Gathering & Costs
    let annualWealth = 0;
    const livingMembers = clanManager.bloodRegistry.getLivingMembers();
    
    // Beast bonuses
    let beastWealthBonus = 0;
    let beastPrestigeBonus = 0;
    let beastCultivationBonus = 0;
    let beastCombatBonus = 0;
    let beastAlchemyBonus = 0;

    (clanManager.beasts || []).forEach(beast => {
      if (!beast.assignedTo) {
        if (beast.bonus.type === 'Wealth') beastWealthBonus += beast.bonus.value;
        if (beast.bonus.type === 'Prestige') beastPrestigeBonus += beast.bonus.value;
        if (beast.bonus.type === 'Cultivation') beastCultivationBonus += beast.bonus.value;
        if (beast.bonus.type === 'Combat') beastCombatBonus += beast.bonus.value;
        if (beast.bonus.type === 'Alchemy') beastAlchemyBonus += beast.bonus.value;
      }
    });

    const wealthBonus = clanManager.facilities.find(f => f.type === 'Wealth')?.bonus || 0;
    annualWealth += (wealthBonus + beastWealthBonus) * worldModifiers.wealth;

    livingMembers.forEach(m => {
      if (m.currentTask === 'Récolte') annualWealth += (50 + (m.spiritualRoot / 2)) * worldModifiers.wealth;
      
      if (m.currentTask === 'Alchimie') {
        if (Math.random() < 0.3) {
          const pill: Item = {
            id: `item_${Math.random().toString(36).substr(2, 9)}`,
            name: 'Pilule de Percée',
            type: 'Pill',
            description: 'Augmente les chances de percée de 20% pour une tentative.',
            rarity: 'Rare',
            effect: { type: 'Breakthrough', value: 20 },
            quantity: 1
          };
          clanManager.addItem(pill);
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `${m.lastName} ${m.firstName} a raffiné une Pilule de Percée !` });
        }
        annualWealth += 20 * worldModifiers.wealth;
      }

      if (m.currentTask === 'Forge') {
        if (Math.random() < 0.2) {
          const equip: Item = {
            id: `item_${Math.random().toString(36).substr(2, 9)}`,
            name: 'Épée de Jade',
            type: 'Equipment',
            description: 'Augmente la puissance martiale de 50.',
            rarity: 'Commun',
            effect: { type: 'Power', value: 50 },
            quantity: 1
          };
          clanManager.addItem(equip);
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `${m.lastName} ${m.firstName} a forgé une Épée de Jade !` });
        }
        annualWealth += 30 * worldModifiers.wealth;
      }

      if (Math.random() < 0.02) {
        const prefixes = ['Sutra', 'Art', 'Technique', 'Livre', 'Secret', 'Voie', 'Parchemin', 'Mantra', 'Manuel'];
        const suffixes = ['Céleste', 'du Vide', 'Ancestral', 'Divin', 'des Étoiles', 'du Dragon', 'des Ombres', 'Éternel', 'de Glace', 'Pourpre', 'de l\'Alchimiste', 'de l\'Épée'];
        let baseName = `${prefixes[Math.floor(Math.random() * prefixes.length)]} ${suffixes[Math.floor(Math.random() * suffixes.length)]}`;
        
        let finalName = baseName;
        let counter = 1;
        while (clanManager.manuals.some(man => man.name === finalName)) {
          counter++;
          finalName = `${baseName} (Vol. ${counter})`;
        }

        const manual: Manual = {
          id: `manual_${Math.random().toString(36).substr(2, 9)}`,
          name: finalName,
          description: "Un manuel ancien contenant des secrets de culture.",
          rarity: Math.random() > 0.8 ? 'Épique' : 'Rare',
          type: 'Culture',
          difficulty: Math.floor(Math.random() * 50) + 10,
          bonus: 1.2,
          requirements: { realm: 'Raffinement du Qi', spiritualRoot: 50 }
        };
        clanManager.manuals.push(manual);
        events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `Découverte ! ${m.firstName} a trouvé un manuel ancien : ${manual.name}.` });
      }

      annualWealth -= 10; // Maintenance cost
    });

    // 3. Process Members
    let prestigeChange = beastPrestigeBonus;
    livingMembers.forEach(member => {
      member.updateAge();
      
      const effectiveMaxLifespan = member.maxLifespan + (clanManager.destinyUpgrades.includes('destiny_lifespan_1') ? 10 : 0);
      if (member.age > effectiveMaxLifespan) {
        member.die();
        events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Danger', message: `${member.lastName} ${member.firstName} est décédé de vieillesse à l'âge de ${member.age} ans.` });
        
        const realmIndex = REALMS.indexOf(member.realm);
        if (realmIndex >= 2) {
          const legacyDestiny = realmIndex * 10;
          clanManager.resourceManager.addDestiny(legacyDestiny);
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Info', message: `Héritage : L'énergie spirituelle de ${member.firstName} retourne à la terre, augmentant la destinée du clan de ${legacyDestiny}.` });

          if (Math.random() < 0.3 + (realmIndex * 0.1)) {
            const manual: Manual = {
              id: `man_${Math.random().toString(36).substr(2, 9)}`,
              name: `Héritage de ${member.firstName}`,
              description: `Un manuel d'héritage laissé par ${member.firstName} avant son trépas.`,
              rarity: realmIndex >= 4 ? 'Épique' : (realmIndex >= 3 ? 'Rare' : 'Commun'),
              type: 'Culture',
              difficulty: realmIndex * 15,
              bonus: 1 + (realmIndex * 0.1),
              requirements: { realm: REALMS[Math.max(0, realmIndex - 1)], spiritualRoot: 30 }
            };
            clanManager.manuals.push(manual);
            events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `Héritage : Avant de s'éteindre, ${member.firstName} a laissé derrière lui le manuel "${manual.name}".` });
          }
        }
        return;
      }

      const mentalEvents = MentalStabilitySystem.applyAnnualModifiers(member, currentYear);
      events.push(...mentalEvents);
      
      if (!member.isAlive) return;

      let cultivationBonus = 1 + ((clanManager.facilities.find(f => f.type === 'Cultivation')?.bonus || 0) / 100);
      if (clanManager.destinyUpgrades.includes('destiny_cultivation_1')) cultivationBonus += 0.1;
      const destinyBoost = 1 + (clanManager.resourceManager.destiny / 1000);
      let worldModifier = worldModifiers.cultivation;
      if (clanManager.worldStatus === 'Marée de Qi') worldModifier *= 1.2;
      if (clanManager.worldStatus === 'Sécheresse Spirituelle') worldModifier *= 0.8;

      const traitModifiers = {
        cultivation: member.traits.includes('Génie') ? 1.5 : 1,
        efficiency: member.traits.includes('Diligent') ? 1.2 : 1,
        breakthrough: member.traits.includes('Calme') ? 1.1 : 1,
        combat: member.traits.includes('Brave') ? 1.2 : 1,
        alchemy: member.traits.includes('Maître Alchimiste') ? 1.5 : 1,
      };

      const manualModifiers = { cultivation: 1, combat: 1, alchemy: 1, forging: 1 };
      (member.studiedManuals || []).forEach(manualId => {
        const manual = clanManager.manuals.find(m => m.id === manualId);
        if (manual) {
          if (manual.type === 'Culture') manualModifiers.cultivation *= manual.bonus;
          if (manual.type === 'Combat') manualModifiers.combat *= manual.bonus;
          if (manual.type === 'Alchimie') manualModifiers.alchemy *= manual.bonus;
          if (manual.type === 'Forge') manualModifiers.forging *= manual.bonus;
        }
      });

      const assignedBeast = (clanManager.beasts || []).find(b => b.assignedTo === member.id);
      const personalBeastModifiers = {
        cultivation: assignedBeast?.bonus.type === 'Cultivation' ? 1 + (assignedBeast.bonus.value / 100) : 1,
        combat: assignedBeast?.bonus.type === 'Combat' ? 1 + (assignedBeast.bonus.value / 100) : 1,
        alchemy: assignedBeast?.bonus.type === 'Alchemy' ? 1 + (assignedBeast.bonus.value / 100) : 1,
      };

      if (member.currentTask === 'Culture') {
        const baseGain = REALM_CONFIG[member.realm].baseProgress;
        const currentRealmIndex = REALMS.indexOf(member.realm);
        
        let resourceMultiplier = cultivationBonus * manualModifiers.cultivation * personalBeastModifiers.cultivation;
        let destinyMultiplier = destinyBoost;

        if (currentRealmIndex >= REALMS.indexOf('Embryon Dao')) {
           resourceMultiplier = 1; 
           destinyMultiplier = 1 + (clanManager.resourceManager.destiny / 500);
        } else if (currentRealmIndex >= REALMS.indexOf("Noyau d'Or")) {
           destinyMultiplier = 1 + (clanManager.resourceManager.destiny / 750);
        } else {
           destinyMultiplier = 1 + (clanManager.resourceManager.destiny / 2000);
        }

        const progressGain = baseGain * (member.spiritualRoot / 50) * resourceMultiplier * worldModifier * destinyMultiplier * traitModifiers.cultivation;
        member.breakthroughProgress = Math.min(100, member.breakthroughProgress + progressGain);
        member.skills.combat = Math.min(100, member.skills.combat + 0.2 * traitModifiers.combat * manualModifiers.combat * personalBeastModifiers.combat);
        
        if (member.breakthroughProgress >= 100) {
          const currentRealmIndex = REALMS.indexOf(member.realm);
          if (currentRealmIndex < REALMS.length - 1) {
            const nextRealm = REALMS[currentRealmIndex + 1];
            const isHighRealm = currentRealmIndex >= 4;
            let tribulationSuccess = true;
            if (isHighRealm) {
              if (member.isProtectedByMirror) {
                tribulationSuccess = true;
                member.isProtectedByMirror = false;
                events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `TRIBULATION SURMONTÉE ! Le Bouclier Ancestral a protégé ${member.firstName} de la foudre céleste !` });
              } else {
                const tribulationChance = 0.7 + (member.mentalStability / 500);
                tribulationSuccess = Math.random() < tribulationChance;
                if (!tribulationSuccess) {
                  member.die();
                  events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Danger', message: `TRIBULATION CÉLESTE ! ${member.firstName} a été frappé par la foudre divine lors de son ascension vers ${nextRealm} et a péri.` });

                  const realmIndex = REALMS.indexOf(member.realm);
                  if (realmIndex >= 2) {
                    const legacyDestiny = realmIndex * 10;
                    clanManager.resourceManager.addDestiny(legacyDestiny);
                    events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Info', message: `Héritage : L'énergie spirituelle de ${member.firstName} retourne à la terre, augmentant la destinée du clan de ${legacyDestiny}.` });

                    if (Math.random() < 0.3 + (realmIndex * 0.1)) {
                      const manual: Manual = {
                        id: `man_${Math.random().toString(36).substr(2, 9)}`,
                        name: `Héritage de ${member.firstName}`,
                        description: `Un manuel d'héritage laissé par ${member.firstName} avant son trépas.`,
                        rarity: realmIndex >= 4 ? 'Épique' : (realmIndex >= 3 ? 'Rare' : 'Commun'),
                        type: 'Culture',
                        difficulty: realmIndex * 15,
                        bonus: 1 + (realmIndex * 0.1),
                        requirements: { realm: REALMS[Math.max(0, realmIndex - 1)], spiritualRoot: 30 }
                      };
                      clanManager.manuals.push(manual);
                      events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `Héritage : Avant de s'éteindre, ${member.firstName} a laissé derrière lui le manuel "${manual.name}".` });
                    }
                  }
                  return;
                } else {
                  events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `TRIBULATION SURMONTÉE ! ${member.firstName} a survécu à la foudre céleste !` });
                }
              }
            }

            let destinyBoostForBreakthrough = destinyBoost;
            if (clanManager.destinyUpgrades.includes('destiny_breakthrough_1')) destinyBoostForBreakthrough += 0.05;

            const breakthroughAttempt = BreakthroughSystem.attemptBreakthrough(
              member, nextRealm, false, false, member.isProtectedByMirror || false,
              destinyBoostForBreakthrough, cultivationBonus, worldModifier, traitModifiers.breakthrough
            );

            const breakthroughEvents = BreakthroughSystem.applyBreakthroughResult(member, nextRealm, breakthroughAttempt.result, currentYear);
            events.push(...breakthroughEvents);

            if (breakthroughAttempt.result === 'Success') {
              if (nextRealm === "Noyau d'Or") clanManager.resourceManager.addDestiny(5);
              if (nextRealm === "Embryon Dao") clanManager.resourceManager.addDestiny(20);
              if (nextRealm === "Immortel") clanManager.resourceManager.addDestiny(100);
              mirrorPowerGain += 5;
            } else {
              if (member.isProtectedByMirror) member.isProtectedByMirror = false;
            }
          }
        }
      } else if (member.currentTask === 'Alchimie') {
        member.skills.alchemy = Math.min(100, member.skills.alchemy + 1 * traitModifiers.alchemy);
        annualWealth += 50 * (member.skills.alchemy / 20) * worldModifiers.wealth * traitModifiers.efficiency;
      } else if (member.currentTask === 'Récolte') {
        member.skills.farming = Math.min(100, member.skills.farming + 1);
        annualWealth += 100 * (member.skills.farming / 20) * worldModifiers.wealth * traitModifiers.efficiency;
      } else if (member.currentTask === 'Patrouille') {
        member.skills.combat = Math.min(100, member.skills.combat + 1 * traitModifiers.combat);
        prestigeChange += 1 * worldModifiers.prestige * traitModifiers.efficiency;
      } else if (member.currentTask === 'Forge') {
        member.skills.forging = Math.min(100, member.skills.forging + 1 * manualModifiers.forging);
        annualWealth += 70 * (member.skills.forging / 20) * worldModifiers.wealth * traitModifiers.efficiency;
      } else if (member.currentTask === 'Étude') {
        if (member.assignedManualId) {
          const manual = clanManager.manuals.find(m => m.id === member.assignedManualId);
          if (manual) {
            const baseYears = manual.rarity === 'Commun' ? 2 : manual.rarity === 'Rare' ? 4 : manual.rarity === 'Épique' ? 8 : 15;
            const studySpeed = (member.spiritualRoot / 50) * traitModifiers.cultivation;
            const finishChance = studySpeed / baseYears;
            if (Math.random() < finishChance) {
              if (!member.studiedManuals) member.studiedManuals = [];
              member.studiedManuals.push(manual.id);
              member.assignedManualId = undefined;
              member.currentTask = 'Culture';
              events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `${member.firstName} a fini d'étudier le manuel : ${manual.name} !` });
            }
          } else {
            member.assignedManualId = undefined;
            member.currentTask = 'Repos';
          }
        } else {
          member.currentTask = 'Repos';
        }
      }

      if (Math.random() < 0.05) {
        member.modifyStability(Math.random() > 0.5 ? 5 : -5);
      }
    });

    // 4. Marriages
    const singleMembers = livingMembers.filter(m => m.age >= 18 && !m.spouse);
    if (singleMembers.length >= 2 && Math.random() < 0.1) {
      const m1 = singleMembers[Math.floor(Math.random() * singleMembers.length)];
      const m2 = singleMembers.find(m => m.id !== m1.id && m.lastName !== m1.lastName);
      if (m2) {
        m1.spouse = m2.id;
        m2.spouse = m1.id;
        events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `Célébration ! ${m1.lastName} ${m1.firstName} et ${m2.lastName} ${m2.firstName} se sont unis par les liens du mariage.` });
      }
    }

    // 5. Births
    const couples = livingMembers.filter(m => m.spouse && m.age >= 20 && m.age <= 100);
    for (const parent1 of couples) {
      if (parent1.id < (parent1.spouse || '')) {
        const parent2 = livingMembers.find(m => m.id === parent1.spouse);
        if (parent2) {
          const realmIndex1 = REALMS.indexOf(parent1.realm);
          const realmIndex2 = REALMS.indexOf(parent2.realm);
          const maxRealm = Math.max(realmIndex1, realmIndex2);
          
          let birthChance = 0.15 - (maxRealm * 0.015);
          if (parent1.age > 60 || parent2.age > 60) birthChance -= 0.05;
          birthChance = Math.max(0.01, birthChance);

          if (Math.random() < birthChance) {
            const childId = `char_${currentYear}_${Math.random().toString(36).substr(2, 5)}`;
            const firstName = FIRST_NAMES[Math.floor(Math.random() * FIRST_NAMES.length)];
            const gender = Math.random() > 0.5 ? 'Male' : 'Female';
            
            const possibleTraits = [...new Set([...(parent1.traits || []), ...(parent2.traits || [])])];
            let childTraits = possibleTraits.filter(() => Math.random() > 0.5);
            if (Math.random() < 0.05) childTraits.push(TRAITS[Math.floor(Math.random() * TRAITS.length)]);
            
            let childRoot = GeneticSystem.calculateSpiritualRoot(parent1, parent2);
            if (clanManager.destinyUpgrades.includes('destiny_birth_1')) childRoot = Math.min(100, childRoot + 10);

            const childData: Character = {
              id: childId,
              firstName,
              lastName: parent1.lastName,
              age: 0,
              gender,
              maxLifespan: CultivationSystem.getBaseLifespan('Mortel'),
              generation: Math.max(parent1.generation, parent2.generation) + 1,
              realm: 'Mortel',
              spiritualRoot: childRoot,
              mentalStability: 100,
              currentTask: 'Repos',
              isAlive: true,
              parents: [parent1.id, parent2.id],
              children: [],
              spouse: undefined,
              portraitSeed: `${gender.toLowerCase()}-${Math.random().toString(36).substr(2, 9)}`,
              traits: [...new Set(childTraits)],
              skills: { alchemy: 0, forging: 0, farming: 0, combat: 0 },
              breakthroughProgress: 0,
              studiedManuals: []
            };
            
            parent1.children.push(childId);
            parent2.children.push(childId);
            clanManager.addMember(childData);
            
            events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `Une nouvelle vie s'éveille ! ${childData.lastName} ${childData.firstName} est né(e) de l'union de ${parent1.firstName} et ${parent2.firstName}. (Racine Spirituelle: ${childRoot})` });
          }
        }
      }
    }

    // 6. Global Random Events & World Status
    if (clanManager.worldStatus === 'Guerre des Sectes' && Math.random() < 0.2) {
      const wealthLoss = 500 + Math.floor(Math.random() * 1000);
      annualWealth -= wealthLoss;
      events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Danger', message: `[Monde] La guerre des sectes a perturbé nos routes commerciales. Perte de ${wealthLoss} Or.` });
    }

    if (Math.random() < 0.2) {
      const worldSituations: WorldEvent[] = [
        { id: `we_${Math.random().toString(36).substr(2, 9)}`, name: 'Éruption de Qi Souterrain', description: 'Une veine de Qi a éclaté, saturant l\'air d\'énergie spirituelle.', duration: 3, type: 'Positive', effects: { cultivationSpeed: 1.5 } },
        { id: `we_${Math.random().toString(36).substr(2, 9)}`, name: 'Peste Spirituelle', description: 'Une maladie mystérieuse affaiblit les cultivateurs et flétrit les herbes.', duration: 4, type: 'Negative', effects: { cultivationSpeed: 0.7, wealthGain: 0.5, dangerLevel: 10 } },
        { id: `we_${Math.random().toString(36).substr(2, 9)}`, name: 'Âge d\'Or du Commerce', description: 'Les routes sont sûres et les marchands affluent.', duration: 5, type: 'Positive', effects: { wealthGain: 1.8, prestigeGain: 1.5 } },
        { id: `we_${Math.random().toString(36).substr(2, 9)}`, name: 'Ombre du Démon', description: 'Un ancien démon s\'est réveillé, semant la terreur.', duration: 6, type: 'Negative', effects: { dangerLevel: 30, prestigeGain: 0.5 } }
      ];
      const newEvent = worldSituations[Math.floor(Math.random() * worldSituations.length)];
      clanManager.worldEvents.push(newEvent);
      events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: newEvent.type === 'Positive' ? 'Success' : 'Danger', message: `[Événement Mondial] ${newEvent.name} : ${newEvent.description}` });
    }

    if (Math.random() < 0.15) {
      const worldEventsList = [
        { status: 'Marée de Qi', message: "Une marée de Qi déferle sur la région, facilitant la culture.", type: 'Success' as const, prestige: 0 },
        { status: 'Guerre des Sectes', message: "Les sectes voisines sont en guerre. Le chaos règne.", type: 'Danger' as const, prestige: -5 },
        { status: 'Foire Immortelle', message: "Une foire immortelle se tient à proximité. Notre prestige grandit.", type: 'Info' as const, prestige: 10 },
        { status: 'Sécheresse Spirituelle', message: "Le Qi ambiant s'amincit. La culture devient ardue.", type: 'Warning' as const, prestige: 0 },
        { status: 'Paix Relative', message: "Le monde est calme. Une période de stabilité commence.", type: 'Info' as const, prestige: 2 }
      ];
      const selectedEvent = worldEventsList[Math.floor(Math.random() * worldEventsList.length)];
      clanManager.worldStatus = selectedEvent.status;
      prestigeChange += selectedEvent.prestige;
      events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: selectedEvent.type, message: `[Monde] ${selectedEvent.message}` });
    }

    // 7. Tier Upgrades
    const prestigeThresholds = [500, 1500, 5000, 15000];
    if (clanManager.tier <= prestigeThresholds.length && (clanManager.resourceManager.prestige + prestigeChange) >= prestigeThresholds[clanManager.tier - 1]) {
      clanManager.tier += 1;
      events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `Le clan a atteint le Tier ${clanManager.tier} ! Notre influence grandit.` });
    }

    // 8. Sect AI & World Interactions
    const powerLevels = ['Faible', 'Moyenne', 'Élevée', 'Légendaire'] as const;
    clanManager.sects.forEach((sect, index) => {
      if (Math.random() < 0.05) {
        const currentIdx = powerLevels.indexOf(sect.power);
        if (currentIdx < powerLevels.length - 1) {
          sect.power = powerLevels[currentIdx + 1];
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Info', message: `[Rumeur] La secte ${sect.name} a gagné en puissance et est désormais considérée comme ${sect.power}.` });
        }
      } else if (Math.random() < 0.05) {
        const currentIdx = powerLevels.indexOf(sect.power);
        if (currentIdx > 0) {
          sect.power = powerLevels[currentIdx - 1];
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Info', message: `[Rumeur] La secte ${sect.name} a subi des revers et sa puissance a chuté à ${sect.power}.` });
        }
      }

      if (sect.relation === 'Hostile' && Math.random() < 0.15) {
        const raidPower = sect.power === 'Faible' ? 50 : sect.power === 'Moyenne' ? 150 : sect.power === 'Élevée' ? 400 : 1000;
        const clanDefense = clanManager.facilities.find(f => f.type === 'Defense')?.bonus || 0;
        const totalCombatSkill = clanManager.bloodRegistry.getLivingMembers().reduce((acc, m) => acc + m.skills.combat, 0);
        const defensePower = clanDefense + (totalCombatSkill / 2);
        
        if (defensePower < raidPower) {
          const wealthLost = Math.floor(clanManager.resourceManager.wealth * 0.1);
          const prestigeLost = 50;
          annualWealth -= wealthLost;
          prestigeChange -= prestigeLost;
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Danger', message: `[Secte] La secte ${sect.name} a lancé un raid sur notre domaine ! Nos défenses ont été submergées. Perte de ${wealthLost} Or et ${prestigeLost} Prestige.` });
          
          const aliveMembers = clanManager.bloodRegistry.getLivingMembers();
          if (aliveMembers.length > 0 && Math.random() < 0.3) {
             const victim = aliveMembers[Math.floor(Math.random() * aliveMembers.length)];
             victim.modifyStability(-30);
             events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Danger', message: `[Secte] ${victim.firstName} a été gravement blessé(e) lors de l'attaque de ${sect.name}.` });
          }
        } else {
          prestigeChange += 20;
          events.push({ id: Math.random().toString(36).substr(2, 9), year: currentYear, type: 'Success', message: `[Secte] Nous avons repoussé une attaque de la secte ${sect.name} ! (+20 Prestige)` });
        }
      }
    });

    // Apply resource changes
    if (annualWealth > 0) clanManager.resourceManager.addWealth(annualWealth);
    else if (annualWealth < 0) clanManager.resourceManager.spendWealth(Math.abs(annualWealth));
    
    if (prestigeChange > 0) clanManager.resourceManager.addPrestige(prestigeChange);
    else if (prestigeChange < 0) clanManager.resourceManager.spendPrestige(Math.abs(prestigeChange));

    clanManager.mirrorPower = Math.min(100, clanManager.mirrorPower + mirrorPowerGain);
    clanManager.marketItems = TimeManager.generateMarketItems(clanManager.tier);

    return events;
  }

  static generateMarketItems(tier: number): MarketItem[] {
    const items: MarketItem[] = [];
    const count = 3 + Math.floor(Math.random() * 3);
    
    for (let i = 0; i < count; i++) {
      const roll = Math.random();
      const id = `market_${Math.random().toString(36).substr(2, 9)}`;
      
      if (roll < 0.5) {
        // Pill or Equipment
        const isPill = Math.random() > 0.5;
        const item: Item = isPill ? {
          id: `item_${Math.random().toString(36).substr(2, 9)}`,
          name: 'Pilule de Percée',
          type: 'Pill',
          description: 'Augmente les chances de percée de 20% pour une tentative.',
          rarity: 'Rare',
          effect: { type: 'Breakthrough', value: 20 },
          quantity: 1
        } : {
          id: `item_${Math.random().toString(36).substr(2, 9)}`,
          name: 'Épée de Jade',
          type: 'Equipment',
          description: 'Augmente la puissance martiale de 50.',
          rarity: 'Commun',
          effect: { type: 'Power', value: 50 },
          quantity: 1
        };
        items.push({ id, type: 'Item', item, cost: 200 + Math.floor(Math.random() * 300) });
      } else if (roll < 0.8) {
        // Manual
        const prefixes = ['Sutra', 'Art', 'Technique', 'Livre', 'Secret', 'Voie'];
        const suffixes = ['Céleste', 'du Vide', 'Ancestral', 'Divin', 'des Étoiles', 'du Dragon'];
        const name = `${prefixes[Math.floor(Math.random() * prefixes.length)]} ${suffixes[Math.floor(Math.random() * suffixes.length)]}`;
        const manual: Manual = {
          id: `man_${Math.random().toString(36).substr(2, 9)}`,
          name,
          description: "Un manuel ancien trouvé sur le marché.",
          rarity: Math.random() > 0.8 ? 'Épique' : 'Rare',
          type: 'Culture',
          difficulty: 20 + Math.floor(Math.random() * 30),
          bonus: 1.2 + (Math.random() * 0.3),
          requirements: { realm: 'Raffinement du Qi', spiritualRoot: 40 }
        };
        items.push({ id, type: 'Manual', manual, cost: 500 + Math.floor(Math.random() * 500) });
      } else {
        // Beast
        const speciesList = ['Tigre', 'Grue', 'Tortue', 'Serpent', 'Loup'];
        const elements = ['Feu', 'Eau', 'Bois', 'Métal', 'Terre'] as const;
        const beast: SpiritualBeast = {
          id: `beast_${Math.random().toString(36).substr(2, 9)}`,
          name: `${speciesList[Math.floor(Math.random() * speciesList.length)]} Spirituel`,
          species: speciesList[Math.floor(Math.random() * speciesList.length)],
          element: elements[Math.floor(Math.random() * elements.length)],
          rarity: 'Rare',
          level: 1,
          experience: 0,
          bonus: { type: 'Cultivation', value: 5 + Math.floor(Math.random() * 10) }
        };
        items.push({ id, type: 'Beast', beast, cost: 1000 + Math.floor(Math.random() * 1000) });
      }
    }
    return items;
  }
}

