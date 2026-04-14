import { Clan, Character, Item, Manual, SpiritualBeast, Sect, Facility, WorldEvent, MarketItem, TaskType } from '../../types/game';
import { BloodRegistry } from './BloodRegistry';
import { CharacterObject } from './CharacterObject';
import { ResourceManager } from '../economy/ResourceManager';

export class ClanManager {
  name: string;
  motto: string;
  currentYear: number;
  tier: number;
  
  bloodRegistry: BloodRegistry;
  resourceManager: ResourceManager;
  
  facilities: Facility[];
  worldStatus: string;
  worldEvents: WorldEvent[];
  inventory: Item[];
  sects: Sect[];
  qiDensity: number;
  manuals: Manual[];
  mirrorPower: number;
  beasts: SpiritualBeast[];
  destinyUpgrades: string[];
  marketItems?: MarketItem[];

  constructor(clanData: Clan) {
    this.name = clanData.name;
    this.motto = clanData.motto;
    this.currentYear = clanData.currentYear;
    this.tier = clanData.tier;
    
    const characterObjects = clanData.members.map(m => new CharacterObject(m));
    this.bloodRegistry = new BloodRegistry(characterObjects);
    
    this.resourceManager = new ResourceManager(
      clanData.wealth,
      clanData.prestige,
      clanData.destiny
    );
    
    this.facilities = [...clanData.facilities];
    this.worldStatus = clanData.worldStatus;
    this.worldEvents = [...clanData.worldEvents];
    this.inventory = [...clanData.inventory];
    this.sects = [...(clanData.sects || [])];
    this.qiDensity = clanData.qiDensity;
    this.manuals = [...clanData.manuals];
    this.mirrorPower = clanData.mirrorPower;
    this.beasts = [...(clanData.beasts || [])];
    this.destinyUpgrades = [...(clanData.destinyUpgrades || [])];
    this.marketItems = clanData.marketItems ? [...clanData.marketItems] : undefined;
  }

  addMember(memberData: Character): void {
    const charObj = new CharacterObject(memberData);
    this.bloodRegistry.addMember(charObj);
  }

  removeMember(memberId: string): void {
    const member = this.bloodRegistry.getMember(memberId);
    if (member) {
      member.die();
      this.bloodRegistry.recordDeath(member);
    }
  }

  updateTask(memberId: string, task: TaskType, manualId?: string): void {
    const member = this.bloodRegistry.getMember(memberId);
    if (member) {
      member.currentTask = task;
      if (manualId !== undefined) {
        (member as any).assignedManualId = manualId; // Handle extra properties if needed
      }
    }
  }

  autoAssignTasks(): void {
    this.bloodRegistry.getLivingMembers().forEach(member => {
      let task: TaskType = 'Culture';
      if (member.skills.alchemy > 20) task = 'Alchimie';
      else if (member.skills.forging > 20) task = 'Forge';
      else if (member.skills.farming > 20) task = 'Récolte';
      else if (member.spiritualRoot > 60) task = 'Culture';
      else task = 'Patrouille';
      
      member.currentTask = task;
    });
  }

  advanceYear(): void {
    this.currentYear += 1;
  }

  addItem(item: Item): void {
    const existing = this.inventory.find(i => i.name === item.name);
    if (existing) {
      existing.quantity += item.quantity;
    } else {
      this.inventory.push(item);
    }
  }

  removeItem(itemId: string, quantity: number = 1): boolean {
    const index = this.inventory.findIndex(i => i.id === itemId);
    if (index !== -1) {
      const item = this.inventory[index];
      if (item.quantity >= quantity) {
        item.quantity -= quantity;
        if (item.quantity <= 0) {
          this.inventory.splice(index, 1);
        }
        return true;
      }
    }
    return false;
  }

  addManual(manual: Manual): void {
    this.manuals.push(manual);
  }

  removeManual(manualId: string): void {
    this.manuals = this.manuals.filter(m => m.id !== manualId);
  }

  addBeast(beast: SpiritualBeast): void {
    this.beasts.push(beast);
  }

  removeBeast(beastId: string): void {
    this.beasts = this.beasts.filter(b => b.id !== beastId);
  }

  toData(): Clan {
    return {
      name: this.name,
      motto: this.motto,
      wealth: this.resourceManager.wealth,
      prestige: this.resourceManager.prestige,
      destiny: this.resourceManager.destiny,
      currentYear: this.currentYear,
      tier: this.tier,
      members: this.bloodRegistry.getAllMembers().map(m => m.toData()),
      facilities: [...this.facilities],
      worldStatus: this.worldStatus,
      worldEvents: [...this.worldEvents],
      inventory: [...this.inventory],
      sects: [...this.sects],
      qiDensity: this.qiDensity,
      manuals: [...this.manuals],
      mirrorPower: this.mirrorPower,
      beasts: [...this.beasts],
      destinyUpgrades: [...this.destinyUpgrades],
      marketItems: this.marketItems ? [...this.marketItems] : undefined
    };
  }
}
