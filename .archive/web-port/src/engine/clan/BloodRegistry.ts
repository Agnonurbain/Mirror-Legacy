import { CharacterObject } from './CharacterObject';

export class BloodRegistry {
  private members: Map<string, CharacterObject> = new Map();
  private deathHistory: CharacterObject[] = [];

  constructor(initialMembers: CharacterObject[] = []) {
    initialMembers.forEach(m => this.members.set(m.id, m));
  }

  addMember(member: CharacterObject): void {
    this.members.set(member.id, member);
  }

  getMember(id: string): CharacterObject | undefined {
    return this.members.get(id);
  }

  getAllMembers(): CharacterObject[] {
    return Array.from(this.members.values());
  }

  getLivingMembers(): CharacterObject[] {
    return this.getAllMembers().filter(m => m.isAlive);
  }

  getDeadMembers(): CharacterObject[] {
    return this.getAllMembers().filter(m => !m.isAlive);
  }

  recordDeath(member: CharacterObject): void {
    if (!this.deathHistory.find(m => m.id === member.id)) {
      this.deathHistory.push(member);
    }
  }

  getDeathHistory(): CharacterObject[] {
    return [...this.deathHistory];
  }

  getParents(memberId: string): CharacterObject[] {
    const member = this.getMember(memberId);
    if (!member) return [];
    return member.parents.map(id => this.getMember(id)).filter((m): m is CharacterObject => m !== undefined);
  }

  getChildren(memberId: string): CharacterObject[] {
    const member = this.getMember(memberId);
    if (!member) return [];
    return member.children.map(id => this.getMember(id)).filter((m): m is CharacterObject => m !== undefined);
  }

  getSpouse(memberId: string): CharacterObject | undefined {
    const member = this.getMember(memberId);
    if (!member || !member.spouse) return undefined;
    return this.getMember(member.spouse);
  }
}
