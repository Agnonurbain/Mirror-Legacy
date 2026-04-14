import { create } from 'zustand';
import { Clan, GameEvent, Character, TaskType, Item, Manual, SpiritualBeast } from '../types/game';
import { GameManager } from '../engine/core/GameManager';
import { mockClan, mockEvents } from '../data/mockData';

interface GameState {
  clan: Clan;
  events: GameEvent[];
  isAdvancing: boolean;
  
  // Actions
  setClan: (clan: Clan) => void;
  setEvents: (events: GameEvent[]) => void;
  advanceYears: (years: number) => Promise<void>;
  updateTask: (memberId: string, task: TaskType) => void;
  // ... other actions can be added here
}

export const useGameStore = create<GameState>((set, get) => ({
  clan: JSON.parse(JSON.stringify(mockClan)),
  events: mockEvents,
  isAdvancing: false,

  setClan: (clan) => {
    GameManager.getInstance().initialize(clan, get().events);
    set({ clan });
  },
  setEvents: (events) => {
    GameManager.getInstance().events = events;
    set({ events });
  },

  advanceYears: async (years: number) => {
    set({ isAdvancing: true });
    
    const gm = GameManager.getInstance();
    if (!gm.clanManager) {
      gm.initialize(get().clan, get().events);
    }

    for (let i = 0; i < years; i++) {
      const { newClan, newEvents } = await gm.advanceYear();
      
      set({ clan: newClan, events: newEvents });

      if (years > 1 && i < years - 1) {
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    }

    set({ isAdvancing: false });
  },

  updateTask: (memberId: string, task: TaskType) => {
    set((state) => ({
      clan: {
        ...state.clan,
        members: state.clan.members.map(m => 
          m.id === memberId ? { ...m, currentTask: task } : m
        )
      }
    }));
  }
}));
