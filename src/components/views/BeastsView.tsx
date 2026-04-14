import React from 'react';
import { Clan, SpiritualBeast, Character } from '../../types/game';
import { PawPrint, Star, Zap, Shield, Coins, Activity, Sparkles, Hexagon } from 'lucide-react';
import { motion } from 'motion/react';
import { Avatar } from '../ui/Avatar';

interface BeastsViewProps {
  clan: Clan;
  onFeedBeast: (beastId: string) => void;
  onAssignBeast: (beastId: string, memberId?: string) => void;
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

export const BeastsView: React.FC<BeastsViewProps> = ({ clan, onFeedBeast, onAssignBeast }) => {
  const getElementColor = (element: string) => {
    switch (element) {
      case 'Feu': return 'text-red-500';
      case 'Eau': return 'text-blue-400';
      case 'Bois': return 'text-emerald-500';
      case 'Métal': return 'text-zinc-300';
      case 'Terre': return 'text-amber-600';
      case 'Foudre': return 'text-purple-400';
      case 'Vent': return 'text-teal-300';
      case 'Lumière': return 'text-yellow-300';
      case 'Ténèbres': return 'text-indigo-900';
      default: return 'text-zinc-400';
    }
  };

  const getRarityColor = (rarity: string) => {
    switch (rarity) {
      case 'Commun': return 'text-stone-400 border-stone-500/50';
      case 'Rare': return 'text-blue-400 border-blue-500/50 shadow-[0_0_15px_rgba(59,130,246,0.3)]';
      case 'Épique': return 'text-purple-400 border-purple-500/50 shadow-[0_0_15px_rgba(168,85,247,0.3)]';
      case 'Légendaire': return 'text-amber-400 border-amber-500/50 shadow-[0_0_15px_rgba(251,191,36,0.3)]';
      case 'Mythique': return 'text-red-500 border-red-500/50 shadow-[0_0_15px_rgba(239,68,68,0.3)]';
      default: return 'text-stone-400 border-stone-500/50';
    }
  };

  const getRarityBg = (rarity: string) => {
    switch (rarity) {
      case 'Commun': return 'bg-stone-900/20';
      case 'Rare': return 'bg-blue-900/20';
      case 'Épique': return 'bg-purple-900/20';
      case 'Légendaire': return 'bg-amber-900/20';
      case 'Mythique': return 'bg-red-900/20';
      default: return 'bg-stone-900/20';
    }
  };

  const getBonusIcon = (type: string) => {
    switch (type) {
      case 'Cultivation': return <Activity size={16} />;
      case 'Wealth': return <Coins size={16} />;
      case 'Combat': return <Shield size={16} />;
      case 'Alchemy': return <Sparkles size={16} />;
      case 'Prestige': return <Star size={16} />;
      default: return <Zap size={16} />;
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
            <PawPrint className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Pavillon des Bêtes</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Créatures Spirituelles Apprivoisées</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-amber-900/50 shadow-inner">
          <span className="text-xs text-amber-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <Star size={14} className="text-amber-500" />
            Bêtes Apprivoisées
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-amber-400 drop-shadow-[0_0_10px_rgba(251,191,36,0.5)]">
              {clan.beasts?.length || 0}
            </span>
            <span className="text-sm text-amber-700/50 font-serif italic">Créatures</span>
          </div>
        </div>
      </motion.div>

      {(!clan.beasts || clan.beasts.length === 0) ? (
        <motion.div variants={itemVariants} className="bg-[#1a120f] border border-amber-900/30 rounded-3xl p-16 text-center relative overflow-hidden shadow-xl">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          <div className="relative z-10 flex flex-col items-center">
            <div className="w-24 h-24 rounded-full bg-black/50 border border-amber-900/50 flex items-center justify-center mb-6">
              <PawPrint className="w-12 h-12 text-amber-700/30" />
            </div>
            <h3 className="text-3xl font-calligraphy text-amber-500 mb-4">Aucune Bête Spirituelle</h3>
            <p className="text-amber-100/50 font-serif italic text-lg max-w-md">
              Explorez le monde ou attendez des événements propices pour apprivoiser des créatures mystiques.
            </p>
          </div>
        </motion.div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-8 relative z-10">
          {clan.beasts.map(beast => {
            const assignedMember = beast.assignedTo ? clan.members.find(m => m.id === beast.assignedTo) : null;
            
            return (
              <motion.div 
                key={beast.id} 
                variants={itemVariants}
                whileHover={{ y: -5, scale: 1.02 }}
                className="bg-[#1a120f] border border-amber-900/30 rounded-3xl p-6 flex flex-col justify-between transition-all shadow-xl relative overflow-hidden group hover:border-amber-500/50"
              >
                {/* Decorative Elements */}
                <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${getRarityBg(beast.rarity)} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
                <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                  <Hexagon size={120} />
                </div>

                <div className="relative z-10">
                  <div className="flex items-start justify-between mb-6">
                    <div className={`w-14 h-14 rounded-2xl bg-black/50 border shadow-inner flex items-center justify-center ${getRarityColor(beast.rarity)}`}>
                      <PawPrint size={28} />
                    </div>
                    <div className={`px-3 py-1.5 rounded-lg border flex items-center gap-2 ${getRarityBg(beast.rarity)} ${getRarityColor(beast.rarity)}`}>
                      <span className="text-[9px] uppercase tracking-widest font-mono font-bold">
                        {beast.rarity}
                      </span>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="text-xl font-serif font-bold text-amber-100 group-hover:text-amber-400 transition-colors">
                      {beast.name}
                    </h3>
                    <span className={`text-xs font-mono font-bold ${getElementColor(beast.element)}`}>
                      {beast.element}
                    </span>
                  </div>
                  
                  <div className="flex items-center gap-2 mb-4">
                    <span className="text-[10px] uppercase tracking-[0.2em] font-mono text-amber-700/70 bg-black/40 px-2 py-1 rounded border border-amber-900/30">
                      Niv. {beast.level}
                    </span>
                    <span className="text-[10px] uppercase tracking-[0.2em] font-mono text-amber-700/70 bg-black/40 px-2 py-1 rounded border border-amber-900/30">
                      Loyauté: {beast.loyalty}/100
                    </span>
                  </div>
                  
                  <div className="bg-black/40 p-3 rounded-xl border border-amber-900/20 mb-6">
                    <div className="flex items-center gap-2 text-amber-400 mb-1">
                      {getBonusIcon(beast.bonus.type)}
                      <span className="text-xs font-mono font-bold uppercase tracking-widest">{beast.bonus.type}</span>
                    </div>
                    <span className="text-sm font-serif text-amber-100/80">+{beast.bonus.value} au clan</span>
                  </div>
                </div>

                <div className="pt-4 border-t border-amber-900/20 space-y-4 relative z-10">
                  <div className="flex items-center justify-between">
                    <div className="flex flex-col">
                      <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono mb-1">Nourrir</span>
                      <div className="flex items-baseline gap-1.5">
                        <span className={`text-sm font-mono ${clan.wealth >= 50 ? 'text-amber-400' : 'text-red-400'}`}>
                          50
                        </span>
                        <span className="text-[9px] text-amber-700/40 uppercase tracking-widest">Pierres</span>
                      </div>
                    </div>
                    <button
                      onClick={() => onFeedBeast(beast.id)}
                      disabled={clan.wealth < 50 || beast.loyalty >= 100}
                      className={`px-4 py-2 rounded-xl text-xs font-bold uppercase tracking-widest transition-all ${
                        clan.wealth >= 50 && beast.loyalty < 100
                          ? 'bg-amber-900/30 text-amber-400 hover:bg-amber-500 hover:text-black border border-amber-500/30'
                          : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
                      }`}
                    >
                      Nourrir
                    </button>
                  </div>

                  <div className="pt-4 border-t border-amber-900/10">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono">Maître</span>
                      {assignedMember && (
                        <button
                          onClick={() => onAssignBeast(beast.id, undefined)}
                          className="text-[9px] text-red-400 hover:text-red-300 uppercase tracking-widest font-mono"
                        >
                          Retirer
                        </button>
                      )}
                    </div>
                    
                    {assignedMember ? (
                      <div className="flex items-center gap-3 p-2 bg-black/40 rounded-xl border border-amber-900/20">
                        <Avatar seed={assignedMember.portraitSeed} name={assignedMember.firstName} realm={assignedMember.realm} size="sm" className="border border-amber-900/50" />
                        <div>
                          <span className="text-sm font-serif font-bold text-amber-100/90 block">
                            {assignedMember.lastName} {assignedMember.firstName}
                          </span>
                          <span className="text-[9px] font-mono uppercase tracking-widest text-amber-700/70">
                            {assignedMember.realm}
                          </span>
                        </div>
                      </div>
                    ) : (
                      <select
                        onChange={(e) => onAssignBeast(beast.id, e.target.value)}
                        className="w-full bg-black/50 border border-amber-900/30 rounded-xl px-3 py-2 text-sm font-serif text-amber-100/80 focus:outline-none focus:border-amber-500/50"
                        defaultValue=""
                      >
                        <option value="" disabled>Assigner à un disciple...</option>
                        {clan.members.filter(m => m.isAlive).map(m => (
                          <option key={m.id} value={m.id}>
                            {m.lastName} {m.firstName} ({m.realm})
                          </option>
                        ))}
                      </select>
                    )}
                  </div>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </motion.div>
  );
};
