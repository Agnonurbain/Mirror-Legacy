import React, { useState } from 'react';
import { Clan, Item, Character } from '../../types/game';
import { Package, Sparkles, Zap, Shield, Heart, Info, User, ChevronRight, X, ScrollText } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';
import { Avatar } from '../ui/Avatar';

interface InventoryViewProps {
  clan: Clan;
  onUseItem: (memberId: string, itemId: string) => void;
}

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 }
  }
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: {
    y: 0,
    opacity: 1,
    transition: { type: "spring", stiffness: 100 }
  }
};

export const InventoryView: React.FC<InventoryViewProps> = ({ clan, onUseItem }) => {
  const [selectedItem, setSelectedItem] = useState<Item | null>(null);

  const categories: { type: Item['type']; icon: any; color: string; bg: string; border: string; label: string }[] = [
    { type: 'Pill', icon: Zap, color: 'text-purple-400', bg: 'bg-purple-900/20', border: 'border-purple-900/30', label: 'Pilules Spirituelles' },
    { type: 'Equipment', icon: Shield, color: 'text-blue-400', bg: 'bg-blue-900/20', border: 'border-blue-900/30', label: 'Équipements' },
    { type: 'Treasure', icon: Sparkles, color: 'text-amber-400', bg: 'bg-amber-900/20', border: 'border-amber-900/30', label: 'Trésors Rares' },
  ];

  const handleUse = (memberId: string) => {
    if (selectedItem) {
      onUseItem(memberId, selectedItem.id);
      setSelectedItem(null);
    }
  };

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen relative"
    >
      {/* Background Texture */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
      
      {/* Header Section */}
      <motion.div 
        variants={itemVariants}
        className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-8 md:p-12 shadow-2xl mb-12 relative overflow-hidden flex flex-col md:flex-row items-center justify-between gap-8"
      >
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-50" />
        
        <div className="flex items-center gap-6 relative z-10">
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-amber-900/40 to-black border border-amber-500/30 flex items-center justify-center shadow-[0_0_20px_rgba(217,119,6,0.2)]">
            <Package className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Trésorerie du Clan</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Inventaire des Artefacts</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-amber-900/50 shadow-inner">
          <span className="text-xs text-amber-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <ScrollText size={14} className="text-amber-500" />
            Capacité
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-amber-400 drop-shadow-[0_0_10px_rgba(251,191,36,0.5)]">
              {clan.inventory.reduce((acc, item) => acc + item.quantity, 0)}
            </span>
            <span className="text-sm text-amber-700/50 font-serif italic">Objets</span>
          </div>
        </div>
      </motion.div>

      {/* Inventory Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 relative z-10">
        {categories.map(cat => {
          const items = clan.inventory.filter(i => i.type === cat.type);
          const Icon = cat.icon;

          return (
            <motion.div 
              key={cat.type} 
              variants={itemVariants}
              className={`bg-[#1a120f] border rounded-3xl p-6 shadow-xl relative overflow-hidden group transition-all ${cat.border} hover:border-amber-500/50`}
            >
              {/* Decorative Background */}
              <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${cat.bg} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
              <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                <Icon size={150} />
              </div>

              <div className="flex items-center justify-between mb-6 pb-4 border-b border-amber-900/20 relative z-10">
                <div className="flex items-center gap-4">
                  <div className={`w-12 h-12 rounded-2xl bg-black/50 border ${cat.border} shadow-inner flex items-center justify-center`}>
                    <Icon size={24} className={cat.color} />
                  </div>
                  <h3 className="text-2xl font-calligraphy text-amber-100">{cat.label}</h3>
                </div>
                <div className="bg-black/40 px-3 py-1.5 rounded-lg border border-amber-900/30 flex items-center gap-2">
                  <span className="text-lg font-mono text-amber-500">{items.length}</span>
                  <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono">Types</span>
                </div>
              </div>

              <div className="space-y-4 relative z-10 min-h-[200px]">
                {items.length > 0 ? (
                  <AnimatePresence>
                    {items.map(item => (
                      <motion.div 
                        key={item.id} 
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="p-4 bg-black/30 rounded-2xl border border-amber-900/20 hover:border-amber-500/30 transition-all group/item"
                      >
                        <div className="flex justify-between items-start mb-2">
                          <h4 className="text-lg font-serif font-bold text-amber-100/90 group-hover/item:text-amber-400 transition-colors">
                            {item.name}
                          </h4>
                          <span className="text-xs font-mono text-amber-500 bg-amber-900/20 px-2.5 py-1 rounded-lg border border-amber-900/30 shadow-[0_0_10px_rgba(245,158,11,0.2)]">
                            x{item.quantity}
                          </span>
                        </div>
                        
                        <p className="text-xs text-amber-100/60 mb-4 leading-relaxed font-serif italic border-l-2 border-amber-900/30 pl-3">
                          {item.description}
                        </p>
                        
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <div className={`text-[9px] uppercase tracking-widest font-mono px-2 py-1 rounded border ${
                              item.rarity === 'Commun' ? 'text-stone-400 border-stone-800 bg-stone-900/20' :
                              item.rarity === 'Rare' ? 'text-blue-400 border-blue-900/30 bg-blue-900/20' :
                              item.rarity === 'Épique' ? 'text-purple-400 border-purple-900/30 bg-purple-900/20' :
                              'text-amber-400 border-amber-900/30 bg-amber-900/20'
                            }`}>
                              {item.rarity}
                            </div>
                            <div className="flex items-center gap-1.5 text-[9px] text-amber-700/60 uppercase tracking-widest font-mono bg-black/40 px-2 py-1 rounded border border-amber-900/30">
                              <Info size={10} className="text-amber-500/50" />
                              <span>{item.effect.type}: +{item.effect.value}</span>
                            </div>
                          </div>
                          
                          <button
                            onClick={() => setSelectedItem(item)}
                            className="p-2 rounded-lg bg-amber-900/20 text-amber-500 hover:bg-amber-500 hover:text-white transition-colors border border-amber-900/30 opacity-0 group-hover/item:opacity-100"
                            title="Utiliser"
                          >
                            <ChevronRight size={16} />
                          </button>
                        </div>
                      </motion.div>
                    ))}
                  </AnimatePresence>
                ) : (
                  <div className="h-full flex items-center justify-center">
                    <p className="text-sm font-serif italic text-amber-700/40 text-center">
                      Aucun objet de ce type
                    </p>
                  </div>
                )}
              </div>
            </motion.div>
          );
        })}
      </div>

      {/* Use Item Modal */}
      <AnimatePresence>
        {selectedItem && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm"
          >
            <motion.div 
              initial={{ scale: 0.9, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.9, opacity: 0 }}
              className="bg-[#1a120f] border border-amber-900/50 rounded-3xl p-8 max-w-2xl w-full shadow-2xl relative max-h-[80vh] flex flex-col"
            >
              <button 
                onClick={() => setSelectedItem(null)}
                className="absolute top-6 right-6 text-amber-700/50 hover:text-amber-500 transition-colors"
              >
                <X size={24} />
              </button>
              
              <div className="flex items-center gap-4 mb-2">
                <div className="w-10 h-10 rounded-xl bg-black/50 border border-amber-900/50 shadow-inner flex items-center justify-center">
                  <Package size={20} className="text-amber-500" />
                </div>
                <h3 className="text-3xl font-calligraphy text-amber-500">Utiliser un Objet</h3>
              </div>
              
              <p className="text-sm text-amber-100/60 font-serif mb-8 ml-14">
                Sélectionnez un disciple pour utiliser <span className="text-amber-400 font-bold">{selectedItem.name}</span>
              </p>
              
              <div className="overflow-y-auto pr-2 custom-scrollbar space-y-3">
                {clan.members.filter(m => m.isAlive).map(member => (
                  <button
                    key={member.id}
                    onClick={() => handleUse(member.id)}
                    className="w-full text-left p-4 rounded-xl border border-amber-900/30 bg-black/40 hover:bg-amber-900/20 hover:border-amber-500/50 transition-all flex items-center gap-4 group"
                  >
                    <Avatar 
                      seed={member.portraitSeed} 
                      name={member.firstName} 
                      realm={member.realm}
                      size="sm"
                      className="border border-amber-900/50"
                    />
                    <div className="flex-1">
                      <h4 className="font-serif font-bold text-amber-100 group-hover:text-amber-400 transition-colors">
                        {member.lastName} {member.firstName}
                      </h4>
                      <p className="text-[10px] text-amber-700/70 font-mono uppercase tracking-widest mt-1">
                        {member.realm} • Racine: {member.spiritualRoot}
                      </p>
                    </div>
                    <ChevronRight size={20} className="text-amber-700/30 group-hover:text-amber-500 transition-colors" />
                  </button>
                ))}
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};
