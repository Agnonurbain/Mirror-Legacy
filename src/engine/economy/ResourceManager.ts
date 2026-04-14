export class ResourceManager {
  wealth: number;
  prestige: number;
  destiny: number;

  // Track changes for re-renders
  isDirty: boolean = false;

  constructor(wealth: number = 0, prestige: number = 0, destiny: number = 0) {
    this.wealth = wealth;
    this.prestige = prestige;
    this.destiny = destiny;
  }

  addWealth(amount: number): void {
    this.wealth += amount;
    this.isDirty = true;
  }

  spendWealth(amount: number): boolean {
    if (this.wealth >= amount) {
      this.wealth -= amount;
      this.isDirty = true;
      return true;
    }
    return false;
  }

  addPrestige(amount: number): void {
    this.prestige += amount;
    this.isDirty = true;
  }

  spendPrestige(amount: number): boolean {
    if (this.prestige >= amount) {
      this.prestige -= amount;
      this.isDirty = true;
      return true;
    }
    return false;
  }

  addDestiny(amount: number): void {
    this.destiny += amount;
    this.isDirty = true;
  }

  spendDestiny(amount: number): boolean {
    if (this.destiny >= amount) {
      this.destiny -= amount;
      this.isDirty = true;
      return true;
    }
    return false;
  }

  clone(): ResourceManager {
    const clone = new ResourceManager(this.wealth, this.prestige, this.destiny);
    clone.isDirty = false;
    return clone;
  }
}
