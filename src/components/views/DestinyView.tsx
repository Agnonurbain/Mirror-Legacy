import React from 'react';
import { Clan } from '../../types/game';
import { DESTINY_UPGRADES, DestinyUpgrade } from '../../constants';
import { Sparkles, Leaf, Star, Heart, Sword, Lock, Unlock, Hexagon } from 'lucide-react';
import { motion } from 'motion/react';

interface DestinyViewProps {
  clan: Clan;
  onPurchaseUpgrade: (upgradeId: string) => void;
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

export const DestinyView: React.FC<DestinyViewProps> = ({ clan, onPurchaseUpgrade }) => {
  const getIcon = (iconName: string) => {
    switch (iconName) {
      case 'Sparkles': return <Sparkles className="text-blue-400" size={28} />;
      case 'Leaf': return <Leaf className="text-emerald-400" size={28} />;
      case 'Star': return <Star className="text-amber-400" size={28} />;
      case 'Heart': return <Heart className="text-red-400" size={28} />;
      case 'Sword': return <Sword className="text-purple-400" size={28} />;
      default: return <Sparkles className="text-amber-500" size={28} />;
    }
  };

  const getIconBg = (iconName: string) => {
    switch (iconName) {
      case 'Sparkles': return 'bg-blue-900/20 border-blue-900/50';
      case 'Leaf': return 'bg-emerald-900/20 border-emerald-900/50';
      case 'Star': return 'bg-amber-900/20 border-amber-900/50';
      case 'Heart': return 'bg-red-900/20 border-red-900/50';
      case 'Sword': return 'bg-purple-900/20 border-purple-900/50';
      default: return 'bg-amber-900/20 border-amber-900/50';
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
        <div className="absolute top-0 right-0 w-64 h-64 bg-amber-900/10 rounded-full blur-3xl pointer-events-none" />
        
        <div className="flex items-center gap-6 relative z-10">
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-amber-900/40 to-black border border-amber-500/30 flex items-center justify-center shadow-[0_0_20px_rgba(217,119,6,0.2)]">
            <Sparkles className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Arbre de Destinée</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Bénédictions Ancestrales</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-amber-900/50 shadow-inner">
          <span className="text-xs text-amber-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <Star size={14} className="text-amber-500" />
            Points de Destinée
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-amber-400 drop-shadow-[0_0_10px_rgba(251,191,36,0.5)]">
              {clan.destiny.toLocaleString()}
            </span>
            <span className="text-sm text-amber-700/50 font-serif italic">DP</span>
          </div>
        </div>
      </motion.div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8 relative z-10">
        {DESTINY_UPGRADES.map((upgrade: DestinyUpgrade) => {
          const isUnlocked = clan.destinyUpgrades?.includes(upgrade.id);
          const canAfford = clan.destiny >= upgrade.cost;

          return (
            <motion.div 
              key={upgrade.id} 
              variants={itemVariants}
              whileHover={{ y: -5, scale: 1.02 }}
              className={`bg-[#1a120f] border rounded-3xl p-6 flex flex-col justify-between transition-all shadow-xl relative overflow-hidden group ${
                isUnlocked 
                  ? 'border-amber-500/50 shadow-[0_0_30px_rgba(245,158,11,0.15)] bg-gradient-to-b from-amber-900/10 to-transparent' 
                  : 'border-amber-900/30 hover:border-amber-500/50'
              }`}
            >
              {/* Decorative Elements */}
              <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${isUnlocked ? 'from-amber-500/20' : 'from-amber-900/20'} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
              <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                <Hexagon size={120} />
              </div>

              <div className="relative z-10">
                <div className="flex items-start justify-between mb-6">
                  <div className={`w-14 h-14 rounded-2xl bg-black/50 border shadow-inner flex items-center justify-center ${getIconBg(upgrade.icon)} ${isUnlocked ? 'shadow-[0_0_15px_rgba(245,158,11,0.2)]' : ''}`}>
                    {getIcon(upgrade.icon)}
                  </div>
                  <div className={`px-3 py-1.5 rounded-lg border flex items-center gap-2 ${
                    isUnlocked 
                      ? 'bg-amber-900/30 border-amber-500/50 text-amber-400' 
                      : 'bg-black/40 border-amber-900/30 text-amber-700/50'
                  }`}>
                    {isUnlocked ? (
                      <>
                        <Unlock size={12} />
                        <span className="text-[9px] uppercase tracking-widest font-mono font-bold">Acquis</span>
                      </>
                    ) : (
                      <>
                        <Lock size={12} />
                        <span className="text-[9px] uppercase tracking-widest font-mono font-bold">Verrouillé</span>
                      </>
                    )}
                  </div>
                </div>
                
                <h3 className={`text-xl font-serif font-bold mb-3 transition-colors ${
                  isUnlocked ? 'text-amber-400 drop-shadow-[0_0_5px_rgba(245,158,11,0.5)]' : 'text-amber-100 group-hover:text-amber-400'
                }`}>
                  {upgrade.name}
                </h3>
                
                <p className="text-sm text-amber-100/60 leading-relaxed mb-6 min-h-[60px] font-serif italic border-l-2 border-amber-900/30 pl-3">
                  {upgrade.description}
                </p>
              </div>

              <div className="pt-6 border-t border-amber-900/20 flex items-center justify-between relative z-10">
                <div className="flex flex-col">
                  <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono mb-1">Coût</span>
                  <div className="flex items-baseline gap-1.5">
                    <span className={`text-xl font-mono ${isUnlocked ? 'text-amber-700/50 line-through' : canAfford ? 'text-amber-400' : 'text-red-400'}`}>
                      {upgrade.cost.toLocaleString()}
                    </span>
                    <span className="text-[10px] text-amber-700/40 uppercase tracking-widest">DP</span>
                  </div>
                </div>
                
                {!isUnlocked && (
                  <button
                    onClick={() => onPurchaseUpgrade(upgrade.id)}
                    disabled={!canAfford}
                    className={`flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-bold uppercase tracking-widest transition-all shadow-lg ${
                      canAfford
                        ? 'bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white border border-amber-400/20 active:scale-95'
                        : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
                    }`}
                  >
                    <Sparkles size={16} />
                    <span>Éveiller</span>
                  </button>
                )}
              </div>
            </motion.div>
          );
        })}
      </div>
    </motion.div>
  );
};
