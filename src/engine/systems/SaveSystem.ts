import { Clan, GameEvent } from '../../types/game';
import { mockClan, mockEvents } from '../../data/mockData';

export class SaveSystem {
  static readonly CLAN_SAVE_KEY = 'clan_save';
  static readonly EVENTS_SAVE_KEY = 'events_save';

  static loadClan(): Clan {
    const saved = localStorage.getItem(this.CLAN_SAVE_KEY);
    let loadedClan = saved ? JSON.parse(saved) : null;
    
    if (!loadedClan || typeof loadedClan !== 'object') {
      loadedClan = JSON.parse(JSON.stringify(mockClan));
    }
    
    // Migration: Ensure all members have skills, worldEvents exists, and studiedManuals exists
    if (loadedClan) {
      if (!loadedClan.members) {
        loadedClan.members = [];
      }
      if (loadedClan.members) {
        // Deduplicate members by ID
        const uniqueMembers = new Map();
        loadedClan.members.forEach((m: any) => {
          if (!uniqueMembers.has(m.id)) {
            uniqueMembers.set(m.id, {
              ...m,
              skills: m.skills || { alchemy: 0, forging: 0, farming: 0, combat: 0 },
              studiedManuals: m.studiedManuals || [],
              traits: m.traits || [],
              parents: m.parents || []
            });
          }
        });
        loadedClan.members = Array.from(uniqueMembers.values());
      }
      loadedClan.worldEvents = loadedClan.worldEvents || [];
      loadedClan.manuals = loadedClan.manuals || [];
      loadedClan.qiDensity = loadedClan.qiDensity || 1.0;
      loadedClan.mirrorPower = loadedClan.mirrorPower || 0;
      loadedClan.inventory = loadedClan.inventory || [];
      loadedClan.beasts = loadedClan.beasts || [];
      loadedClan.sects = loadedClan.sects || [];
      loadedClan.facilities = loadedClan.facilities || [];
      loadedClan.destinyUpgrades = loadedClan.destinyUpgrades || [];
      loadedClan.wealth = loadedClan.wealth || 0;
      loadedClan.prestige = loadedClan.prestige || 0;
      loadedClan.destiny = loadedClan.destiny || 0;
      loadedClan.currentYear = loadedClan.currentYear || 1;
      loadedClan.tier = loadedClan.tier || 1;
      loadedClan.worldStatus = loadedClan.worldStatus || 'Paix Relative';
    }
    
    return loadedClan;
  }

  static loadEvents(): GameEvent[] {
    const saved = localStorage.getItem(this.EVENTS_SAVE_KEY);
    const parsed = saved ? JSON.parse(saved) : null;
    return Array.isArray(parsed) ? parsed : mockEvents;
  }

  static save(clan: Clan, events: GameEvent[]) {
    localStorage.setItem(this.CLAN_SAVE_KEY, JSON.stringify(clan));
    localStorage.setItem(this.EVENTS_SAVE_KEY, JSON.stringify(events));
  }

  static reset() {
    localStorage.removeItem(this.CLAN_SAVE_KEY);
    localStorage.removeItem(this.EVENTS_SAVE_KEY);
  }

  static exportSave(clan: Clan, events: GameEvent[]) {
    const saveData = { clan, events };
    const blob = new Blob([JSON.stringify(saveData)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `legacy_mirror_save_annee_${clan.currentYear}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
