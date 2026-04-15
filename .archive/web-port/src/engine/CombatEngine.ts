import { Clan, Character, Expedition, SpiritualBeast } from '../types/game';
import { CombatState, Combatant, Position, CombatLogEntry, CellType } from '../types/combat';
import { REALM_CONFIG } from '../constants';

export class CombatEngine {
  static initializeCombat(
    clan: Clan,
    expedition: Expedition,
    leader: Character,
    members: Character[]
  ): CombatState {
    const width = 10;
    const height = 10;
    const grid: { x: number; y: number; type: CellType }[][] = [];

    // Generate Grid with some obstacles
    for (let y = 0; y < height; y++) {
      grid[y] = [];
      for (let x = 0; x < width; x++) {
        const isObstacle = Math.random() < 0.15 && x > 1 && x < width - 2;
        grid[y][x] = { x, y, type: isObstacle ? 'Obstacle' : 'Empty' };
      }
    }

    const combatants: Combatant[] = [];
    const playerTeam = [leader, ...members];

    // Clan-wide beast combat bonus
    let clanBeastCombatBonus = 0;
    (clan.beasts || []).forEach(b => {
      if (!b.assignedTo && b.bonus.type === 'Combat') {
        clanBeastCombatBonus += b.bonus.value;
      }
    });

    // Add Player Team
    playerTeam.forEach((char, index) => {
      let basePower = REALM_CONFIG[char.realm].power;
      const memberBeast = (clan.beasts || []).find(b => b.assignedTo === char.id);
      if (memberBeast?.bonus.type === 'Combat') {
        basePower *= 1 + (memberBeast.bonus.value / 100);
      }
      basePower *= 1 + (clanBeastCombatBonus / 100);

      const hp = 100 + (char.skills.combat * 5) + (basePower / 10);
      
      combatants.push({
        id: char.id,
        name: char.firstName,
        team: 'Player',
        hp: Math.floor(hp),
        maxHp: Math.floor(hp),
        ap: 2,
        maxAp: 2,
        position: { x: 0, y: 2 + index * 2 }, // Spread out on the left
        attack: Math.floor(10 + char.skills.combat + (basePower / 50)),
        defense: Math.floor(5 + (char.skills.combat / 2)),
        speed: Math.floor(10 + Math.random() * 5),
        range: 1,
        isDead: false,
        portraitSeed: char.portraitSeed
      });
    });

    // Add Enemies based on Expedition Difficulty
    const enemyCount = Math.max(1, Math.min(5, Math.floor(expedition.difficulty / 500) + 1));
    const enemyPower = expedition.difficulty / enemyCount;

    for (let i = 0; i < enemyCount; i++) {
      const hp = 80 + (enemyPower / 5);
      combatants.push({
        id: `enemy_${i}`,
        name: `Bête Démoniaque ${i + 1}`,
        team: 'Enemy',
        hp: Math.floor(hp),
        maxHp: Math.floor(hp),
        ap: 2,
        maxAp: 2,
        position: { x: width - 1, y: 2 + i * 2 }, // Spread out on the right
        attack: Math.floor(8 + (enemyPower / 40)),
        defense: Math.floor(3 + (enemyPower / 80)),
        speed: Math.floor(8 + Math.random() * 5),
        range: 1,
        isDead: false,
        isBeast: true
      });
    }

    // Sort by speed for turn order
    combatants.sort((a, b) => b.speed - a.speed);
    const turnOrder = combatants.map(c => c.id);

    return {
      id: `combat_${Date.now()}`,
      expeditionId: expedition.id,
      grid,
      width,
      height,
      combatants,
      turnOrder,
      currentTurnIndex: 0,
      round: 1,
      logs: [{ id: 'log_0', message: `Le combat commence dans ${expedition.name}!`, type: 'info' }],
      status: 'ongoing'
    };
  }

  static getDistance(p1: Position, p2: Position): number {
    return Math.abs(p1.x - p2.x) + Math.abs(p1.y - p2.y);
  }

  static getPath(state: CombatState, start: Position, end: Position): Position[] {
    // Simple BFS for pathfinding
    const queue: { pos: Position; path: Position[] }[] = [{ pos: start, path: [] }];
    const visited = new Set<string>();
    visited.add(`${start.x},${start.y}`);

    const isOccupied = (x: number, y: number) => {
      return state.combatants.some(c => !c.isDead && c.position.x === x && c.position.y === y);
    };

    while (queue.length > 0) {
      const { pos, path } = queue.shift()!;
      
      if (pos.x === end.x && pos.y === end.y) {
        return path;
      }

      const neighbors = [
        { x: pos.x + 1, y: pos.y },
        { x: pos.x - 1, y: pos.y },
        { x: pos.x, y: pos.y + 1 },
        { x: pos.x, y: pos.y - 1 }
      ];

      for (const n of neighbors) {
        if (n.x >= 0 && n.x < state.width && n.y >= 0 && n.y < state.height) {
          if (state.grid[n.y][n.x].type !== 'Obstacle' && !isOccupied(n.x, n.y)) {
            const key = `${n.x},${n.y}`;
            if (!visited.has(key)) {
              visited.add(key);
              queue.push({ pos: n, path: [...path, n] });
            }
          }
        }
      }
    }
    return [];
  }

  static moveCombatant(state: CombatState, combatantId: string, target: Position): CombatState {
    const newState = { ...state, combatants: [...state.combatants], logs: [...state.logs] };
    const combatantIndex = newState.combatants.findIndex(c => c.id === combatantId);
    if (combatantIndex === -1) return state;

    const combatant = { ...newState.combatants[combatantIndex] };
    
    // Check AP
    if (combatant.ap < 1) return state;

    const path = this.getPath(state, combatant.position, target);
    if (path.length === 0 || path.length > 4) { // Max movement range per AP is 4
      return state;
    }

    combatant.position = target;
    combatant.ap -= 1;
    newState.combatants[combatantIndex] = combatant;

    newState.logs.push({
      id: `log_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
      message: `${combatant.name} se déplace.`,
      type: 'move'
    });

    return newState;
  }

  static attackCombatant(state: CombatState, attackerId: string, targetId: string): CombatState {
    const newState = { ...state, combatants: [...state.combatants], logs: [...state.logs] };
    const attackerIdx = newState.combatants.findIndex(c => c.id === attackerId);
    const targetIdx = newState.combatants.findIndex(c => c.id === targetId);
    
    if (attackerIdx === -1 || targetIdx === -1) return state;

    const attacker = { ...newState.combatants[attackerIdx] };
    const target = { ...newState.combatants[targetIdx] };

    if (attacker.ap < 1 || attacker.isDead || target.isDead) return state;
    
    if (this.getDistance(attacker.position, target.position) > attacker.range) return state;

    attacker.ap -= 1;
    
    // Calculate damage
    const damage = Math.max(1, attacker.attack - Math.floor(target.defense / 2) + Math.floor(Math.random() * 5));
    target.hp -= damage;

    newState.logs.push({
      id: `log_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
      message: `${attacker.name} attaque ${target.name} et inflige ${damage} dégâts!`,
      type: 'damage'
    });

    if (target.hp <= 0) {
      target.hp = 0;
      target.isDead = true;
      newState.logs.push({
        id: `log_${Date.now()}_death_${Math.random().toString(36).substr(2, 5)}`,
        message: `${target.name} a été vaincu!`,
        type: 'death'
      });
    }

    newState.combatants[attackerIdx] = attacker;
    newState.combatants[targetIdx] = target;

    return this.checkWinCondition(newState);
  }

  static endTurn(state: CombatState): CombatState {
    const newState = { ...state, combatants: [...state.combatants], logs: [...state.logs] };
    
    let nextIndex = (newState.currentTurnIndex + 1) % newState.turnOrder.length;
    let nextCombatantId = newState.turnOrder[nextIndex];
    let nextCombatant = newState.combatants.find(c => c.id === nextCombatantId);

    // Skip dead combatants
    let loopCount = 0;
    while (nextCombatant?.isDead && loopCount < newState.turnOrder.length) {
      nextIndex = (nextIndex + 1) % newState.turnOrder.length;
      nextCombatantId = newState.turnOrder[nextIndex];
      nextCombatant = newState.combatants.find(c => c.id === nextCombatantId);
      loopCount++;
    }

    if (nextIndex < newState.currentTurnIndex) {
      newState.round += 1;
      newState.logs.push({
        id: `log_${Date.now()}_round_${Math.random().toString(36).substr(2, 5)}`,
        message: `--- Début du Round ${newState.round} ---`,
        type: 'info'
      });
    }

    newState.currentTurnIndex = nextIndex;

    // Reset AP for the new active combatant
    const activeIdx = newState.combatants.findIndex(c => c.id === nextCombatantId);
    if (activeIdx !== -1) {
      newState.combatants[activeIdx] = {
        ...newState.combatants[activeIdx],
        ap: newState.combatants[activeIdx].maxAp
      };
    }

    return newState;
  }

  static checkWinCondition(state: CombatState): CombatState {
    const alivePlayers = state.combatants.filter(c => c.team === 'Player' && !c.isDead);
    const aliveEnemies = state.combatants.filter(c => c.team === 'Enemy' && !c.isDead);

    if (aliveEnemies.length === 0) {
      return { ...state, status: 'victory' };
    } else if (alivePlayers.length === 0) {
      return { ...state, status: 'defeat' };
    }
    return state;
  }

  static processAITurn(state: CombatState): CombatState {
    let currentState = state;
    const activeId = currentState.turnOrder[currentState.currentTurnIndex];
    const activeCombatant = currentState.combatants.find(c => c.id === activeId);

    if (!activeCombatant || activeCombatant.team === 'Player' || activeCombatant.isDead) {
      return currentState;
    }

    // AI Logic: Find closest player
    const alivePlayers = currentState.combatants.filter(c => c.team === 'Player' && !c.isDead);
    if (alivePlayers.length === 0) return this.endTurn(currentState);

    // Simple AI: Move towards closest, attack if in range
    while (currentState.combatants.find(c => c.id === activeId)!.ap > 0 && currentState.status === 'ongoing') {
      const currentAI = currentState.combatants.find(c => c.id === activeId)!;
      
      // Find closest target
      let closestTarget = alivePlayers[0];
      let minDistance = this.getDistance(currentAI.position, closestTarget.position);
      
      for (const p of alivePlayers) {
        const dist = this.getDistance(currentAI.position, p.position);
        if (dist < minDistance) {
          closestTarget = p;
          minDistance = dist;
        }
      }

      if (minDistance <= currentAI.range) {
        // Attack
        currentState = this.attackCombatant(currentState, currentAI.id, closestTarget.id);
      } else {
        // Move towards target
        // Find adjacent cell to target that is closest to AI
        const neighbors = [
          { x: closestTarget.position.x + 1, y: closestTarget.position.y },
          { x: closestTarget.position.x - 1, y: closestTarget.position.y },
          { x: closestTarget.position.x, y: closestTarget.position.y + 1 },
          { x: closestTarget.position.x, y: closestTarget.position.y - 1 }
        ];

        let bestMove = currentAI.position;
        let bestDist = minDistance;

        for (const n of neighbors) {
          if (n.x >= 0 && n.x < currentState.width && n.y >= 0 && n.y < currentState.height) {
            const isOccupied = currentState.combatants.some(c => !c.isDead && c.position.x === n.x && c.position.y === n.y);
            if (currentState.grid[n.y][n.x].type !== 'Obstacle' && !isOccupied) {
              const distToAI = this.getDistance(currentAI.position, n);
              if (distToAI < bestDist) {
                // Check if reachable
                const path = this.getPath(currentState, currentAI.position, n);
                if (path.length > 0 && path.length <= 4) {
                  bestMove = n;
                  bestDist = distToAI;
                }
              }
            }
          }
        }

        if (bestMove.x !== currentAI.position.x || bestMove.y !== currentAI.position.y) {
          currentState = this.moveCombatant(currentState, currentAI.id, bestMove);
        } else {
          // Can't move or attack, end turn
          break;
        }
      }
    }

    if (currentState.status === 'ongoing') {
      return this.endTurn(currentState);
    }
    return currentState;
  }
}
