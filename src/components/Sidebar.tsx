import React, { useRef } from 'react';
import { Home, Users, Network, Scroll, Swords, Settings, Shield, Building2, Globe, Package, Beaker, Book, Moon, Sun, PawPrint, Sparkles, Anvil, Download, Upload, Star, ShoppingCart } from 'lucide-react';
import { Clan } from '../types/game';

interface SidebarProps {
  currentView: string;
  setCurrentView: (view: string) => void;
  clan: Clan;
  onReset: () => void;
  isDarkMode: boolean;
  toggleDarkMode: () => void;
  onExport: () => void;
  onImport: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ currentView, setCurrentView, clan, onReset, isDarkMode, toggleDarkMode, onExport, onImport }) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const navItems = [
    { id: 'overview', label: 'Chronique', icon: Home },
    { id: 'mirror', label: 'Le Miroir', icon: Sparkles },
    { id: 'roster', label: 'Disciples', icon: Users },
    { id: 'family-tree', label: 'Lignée', icon: Network },
    { id: 'facilities', label: 'Pavillons', icon: Building2 },
    { id: 'inventory', label: 'Trésors', icon: Package },
    { id: 'manuals', label: 'Bibliothèque', icon: Book },
    { id: 'beasts', label: 'Bêtes Spirituelles', icon: PawPrint },
    { id: 'world', label: 'Le Monde', icon: Globe },
    { id: 'alchemy', label: 'Alchimie', icon: Beaker },
    { id: 'forge', label: 'Forge', icon: Anvil },
    { id: 'tasks', label: 'Décrets', icon: Scroll },
    { id: 'combat', label: 'Guerres', icon: Swords },
    { id: 'destiny', label: 'Destinée', icon: Star },
    { id: 'market', label: 'Marché', icon: ShoppingCart },
  ];

  return (
    <div className="w-64 bg-[#0a0a0a] border-r border-amber-900/30 flex flex-col h-full relative z-20 shadow-[4px_0_24px_rgba(0,0,0,0.5)] overflow-hidden">
      {/* Background Texture */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
      
      {/* Decorative Edge */}
      <div className="absolute top-0 right-0 w-px h-full bg-gradient-to-b from-transparent via-amber-500/50 to-transparent" />
      
      <div className="p-8 relative z-10 border-b border-amber-900/20 bg-black/40">
        <div className="absolute -left-2 top-8 [writing-mode:vertical-rl] text-[10px] tracking-[0.5em] text-amber-500 opacity-30 uppercase font-mono transform rotate-180">
          Mirror Chronicles
        </div>
        <h1 className="text-3xl font-calligraphy text-amber-500 drop-shadow-[0_0_10px_rgba(245,158,11,0.3)] mb-2">
          {clan.name || "Mirror Chronicles"}
        </h1>
        <div className="flex items-center gap-3">
          <span className="text-[9px] opacity-60 uppercase tracking-[0.2em] font-mono text-amber-700/70">Voie de l'Ascension</span>
          <div className="h-px flex-1 bg-amber-900/30" />
          <span className="text-[10px] font-bold text-amber-500 font-mono bg-amber-900/20 px-2 py-0.5 rounded border border-amber-900/50">T{clan.tier}</span>
        </div>
      </div>

      <nav className="flex-1 px-4 py-6 space-y-1.5 overflow-y-auto custom-scrollbar relative z-10">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = currentView === item.id;
          return (
            <button
              key={item.id}
              onClick={() => setCurrentView(item.id)}
              className={`w-full flex items-center group relative py-3 px-4 transition-all duration-300 rounded-xl border ${
                isActive
                  ? 'bg-gradient-to-r from-amber-900/40 to-black border-amber-500/50 shadow-[0_0_15px_rgba(245,158,11,0.15)]'
                  : 'border-transparent hover:border-amber-900/30 hover:bg-black/40 text-amber-100/60 hover:text-amber-100'
              }`}
            >
              <Icon 
                size={18} 
                className={`mr-3 transition-all duration-500 ${isActive ? 'text-amber-400 scale-110 drop-shadow-[0_0_5px_rgba(251,191,36,0.5)]' : 'text-amber-700/50 group-hover:text-amber-500 group-hover:scale-110'}`} 
              />
              <span className={`text-sm tracking-[0.15em] font-serif transition-all duration-300 ${
                isActive ? 'text-amber-400 font-bold translate-x-1' : 'group-hover:translate-x-1'
              }`}>
                {item.label}
              </span>
              {isActive && (
                <div className="absolute right-3 top-1/2 -translate-y-1/2 w-1.5 h-1.5 bg-amber-400 rounded-full shadow-[0_0_8px_rgba(251,191,36,0.8)]" />
              )}
            </button>
          );
        })}
      </nav>

      <div className="p-6 border-t border-amber-900/20 bg-black/40 relative z-10">
        <div className="flex items-center justify-between mb-4">
          <button
            onClick={toggleDarkMode}
            className="p-2 rounded-lg bg-black/50 border border-amber-900/30 text-amber-700/50 hover:text-amber-500 hover:border-amber-500/50 transition-all"
            title="Thème"
          >
            {isDarkMode ? <Sun size={18} /> : <Moon size={18} />}
          </button>
          
          <div className="flex gap-2">
            <button
              onClick={onExport}
              className="p-2 rounded-lg bg-black/50 border border-amber-900/30 text-amber-700/50 hover:text-amber-500 hover:border-amber-500/50 transition-all"
              title="Exporter Sauvegarde"
            >
              <Download size={18} />
            </button>
            
            <button
              onClick={() => fileInputRef.current?.click()}
              className="p-2 rounded-lg bg-black/50 border border-amber-900/30 text-amber-700/50 hover:text-amber-500 hover:border-amber-500/50 transition-all"
              title="Importer Sauvegarde"
            >
              <Upload size={18} />
            </button>
            <input
              type="file"
              ref={fileInputRef}
              onChange={onImport}
              accept=".json"
              className="hidden"
            />
          </div>
        </div>
        
        <button
          onClick={onReset}
          className="w-full flex items-center justify-center gap-2 py-2.5 rounded-xl border border-red-900/30 bg-red-950/20 text-red-400 hover:bg-red-900/40 hover:border-red-500/50 transition-all text-xs font-bold uppercase tracking-widest"
        >
          <Settings size={14} />
          <span>Réinitialiser</span>
        </button>
      </div>
    </div>
  );
};
