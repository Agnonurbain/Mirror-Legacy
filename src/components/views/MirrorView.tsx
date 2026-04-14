import React, { useState } from 'react';
import { Clan, Character, Sect } from '../../types/game';
import { Sparkles, Shield, Zap, Skull, ChevronRight, X, Scroll, GitMerge, Eye, Hexagon } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';
import { Avatar } from '../ui/Avatar';
import { REALMS } from '../../constants';

interface MirrorViewProps {
  clan: Clan;
  onUseDivinePower: (powerId: string, targetId: string) => void;
  onDeduceFromFragments: (fragmentIds: string[]) => void;
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

export const MirrorView: React.FC<MirrorViewProps> = ({ clan, onUseDivinePower, onDeduceFromFragments }) => {
  const [selectedPower, setSelectedPower] = useState<string | null>(null);
  const [selectedFragments, setSelectedFragments] = useState<string[]>([]);
  
  const hasReachedPurpleMansion = clan.members.some(m => REALMS.indexOf(m.realm) >= 4);

  const powers = [
    {
      id: 'heal',
      name: 'Impulsion de Qi',
      cost: 35,
      icon: Sparkles,
      color: 'text-cyan-400',
      bg: 'bg-cyan-900/20',
      border: 'border-cyan-900/30',
      description: 'Restaure la stabilité mentale et booste la progression de culture d\'un disciple.',
      targetType: 'member',
      unlocked: true
    },
    {
      id: 'protect',
      name: 'Bouclier Ancestral',
      cost: 50,
      icon: Shield,
      color: 'text-amber-400',
      bg: 'bg-amber-900/20',
      border: 'border-amber-900/30',
      description: 'Protège un disciple lors de sa prochaine percée, annulant les risques de blessure de Dao.',
      targetType: 'member',
      unlocked: true
    },
    {
      id: 'awaken',
      name: 'Éveil Forcé',
      cost: 90,
      icon: Zap,
      color: 'text-purple-400',
      bg: 'bg-purple-900/20',
      border: 'border-purple-900/30',
      description: 'Révèle le potentiel caché d\'un disciple, augmentant sa Racine Spirituelle et lui conférant le trait Génie.',
      targetType: 'member',
      unlocked: true
    },
    {
      id: 'punish',
      name: 'Jugement du Miroir',
      cost: 100,
      icon: Skull,
      color: 'text-red-500',
      bg: 'bg-red-900/20',
      border: 'border-red-900/30',
      description: 'Frappe une secte ennemie pour réduire sa puissance, ou exécute un traître du clan.',
      targetType: 'both', // member or sect
      unlocked: true
    },
    {
      id: 'vision',
      name: 'Visionnaire',
      cost: 85,
      icon: Eye,
      color: 'text-emerald-400',
      bg: 'bg-emerald-900/20',
      border: 'border-emerald-900/30',
      description: 'Permet au miroir de voir l\'avenir 10 cycles à l\'avance.',
      targetType: 'mirror',
      unlocked: hasReachedPurpleMansion
    }
  ];

  const handleCast = (targetId: string) => {
    if (selectedPower) {
      onUseDivinePower(selectedPower, targetId);
      setSelectedPower(null);
    }
  };

  const handleFragmentToggle = (id: string) => {
    setSelectedFragments(prev => 
      prev.includes(id) ? prev.filter(f => f !== id) : [...prev, id]
    );
  };

  const handleDeduce = () => {
    if (selectedFragments.length >= 3) {
      onDeduceFromFragments(selectedFragments);
      setSelectedFragments([]);
    }
  };

  const activePower = powers.find(p => p.id === selectedPower);
  const fragments = clan.inventory?.filter(i => i.type === 'Fragment') || [];

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
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-cyan-900/40 to-black border border-cyan-500/30 flex items-center justify-center shadow-[0_0_30px_rgba(6,182,212,0.2)] relative group">
            <div className="absolute inset-0 rounded-full border border-cyan-500/20 animate-[spin_10s_linear_infinite]" />
            <div className="absolute inset-2 rounded-full border border-cyan-500/10 animate-[spin_15s_linear_infinite_reverse]" />
            <Hexagon className="text-cyan-400" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-cyan-400 drop-shadow-md">Miroir du Destin</h2>
            <p className="text-sm text-cyan-700/70 font-mono uppercase tracking-[0.2em] mt-2">Artefact Ancestral</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-cyan-900/50 shadow-inner">
          <span className="text-xs text-cyan-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <Sparkles size={14} className="text-cyan-400" />
            Énergie Divine
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-cyan-400 drop-shadow-[0_0_10px_rgba(34,211,238,0.5)]">
              {clan.mirrorPower}
            </span>
            <span className="text-sm text-cyan-700/50 font-serif italic">Unités</span>
          </div>
        </div>
      </motion.div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-8 relative z-10">
        
        {/* Divine Powers */}
        <motion.div variants={itemVariants} className="bg-[#1a120f] border border-amber-900/30 rounded-3xl p-8 shadow-xl relative overflow-hidden">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          
          <h3 className="text-2xl font-calligraphy text-amber-500 mb-6 flex items-center gap-3 relative z-10">
            <Zap className="text-amber-700" size={24} />
            Interventions Divines
          </h3>
          
          <div className="space-y-4 relative z-10">
            {powers.map(power => {
              const Icon = power.icon;
              const canAfford = clan.mirrorPower >= power.cost;
              const isSelected = selectedPower === power.id;

              return (
                <div 
                  key={power.id}
                  className={`border rounded-2xl p-5 transition-all relative overflow-hidden group ${
                    !power.unlocked ? 'bg-black/60 border-stone-800 opacity-50' :
                    isSelected ? `bg-black/60 ${power.border} shadow-[0_0_20px_rgba(0,0,0,0.5)]` :
                    `bg-black/40 border-amber-900/20 hover:border-amber-500/30`
                  }`}
                >
                  {/* Power Background Glow */}
                  {isSelected && (
                    <div className={`absolute inset-0 bg-gradient-to-r ${power.bg} to-transparent opacity-20 pointer-events-none`} />
                  )}

                  <div className="flex items-start justify-between relative z-10">
                    <div className="flex gap-4">
                      <div className={`w-12 h-12 rounded-xl bg-black/50 border shadow-inner flex items-center justify-center shrink-0 ${
                        !power.unlocked ? 'border-stone-800 text-stone-600' :
                        `${power.border} ${power.color}`
                      }`}>
                        <Icon size={24} />
                      </div>
                      <div>
                        <div className="flex items-center gap-3">
                          <h4 className={`text-xl font-calligraphy ${!power.unlocked ? 'text-stone-500' : 'text-amber-100'}`}>
                            {power.name}
                          </h4>
                          {!power.unlocked && (
                            <span className="text-[9px] uppercase tracking-widest font-mono text-stone-500 bg-stone-900/50 px-2 py-0.5 rounded border border-stone-800">
                              Verrouillé
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-amber-100/60 font-serif mt-1 leading-relaxed">
                          {power.description}
                        </p>
                      </div>
                    </div>
                    
                    <div className="flex flex-col items-end gap-3 shrink-0 ml-4">
                      <div className={`flex items-center gap-1.5 text-sm font-mono font-bold ${
                        !power.unlocked ? 'text-stone-600' :
                        canAfford ? 'text-cyan-400' : 'text-red-500'
                      }`}>
                        <Sparkles size={14} />
                        {power.cost}
                      </div>
                      
                      {power.unlocked && (
                        <button
                          onClick={() => {
                            if (power.targetType === 'mirror') {
                              onUseDivinePower(power.id, 'mirror');
                            } else {
                              setSelectedPower(isSelected ? null : power.id);
                            }
                          }}
                          disabled={!canAfford}
                          className={`px-4 py-2 rounded-xl text-xs font-bold uppercase tracking-widest transition-all ${
                            !canAfford ? 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed' :
                            isSelected ? 'bg-amber-500 text-black shadow-[0_0_15px_rgba(245,158,11,0.4)]' :
                            'bg-amber-900/20 text-amber-500 border border-amber-900/50 hover:bg-amber-900/40 hover:border-amber-500/50'
                          }`}
                        >
                          {power.targetType === 'mirror' ? 'Activer' : isSelected ? 'Annuler' : 'Préparer'}
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </motion.div>

        {/* Deduced Fragments */}
        <motion.div variants={itemVariants} className="bg-[#1a120f] border border-amber-900/30 rounded-3xl p-8 shadow-xl relative overflow-hidden flex flex-col">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          
          <div className="flex items-center justify-between mb-6 relative z-10">
            <h3 className="text-2xl font-calligraphy text-amber-500 flex items-center gap-3">
              <Scroll className="text-amber-700" size={24} />
              Fragments de Compréhension
            </h3>
            <span className="text-xs font-mono text-amber-700/70 uppercase tracking-widest bg-black/40 px-3 py-1.5 rounded-lg border border-amber-900/30">
              {fragments.length} Fragments
            </span>
          </div>
          
          <p className="text-amber-100/60 font-serif italic mb-6 relative z-10">
            Combinez 3 fragments pour tenter de déduire un nouveau manuel de culture ou une technique martiale.
          </p>

          <div className="flex-1 overflow-y-auto pr-2 custom-scrollbar space-y-3 relative z-10 min-h-[300px]">
            {fragments.length === 0 ? (
              <div className="h-full flex items-center justify-center">
                <p className="text-amber-700/40 font-serif italic text-center">
                  Aucun fragment en votre possession.
                </p>
              </div>
            ) : (
              fragments.map(fragment => {
                const isSelected = selectedFragments.includes(fragment.id);
                return (
                  <div 
                    key={fragment.id}
                    onClick={() => handleFragmentToggle(fragment.id)}
                    className={`p-4 rounded-xl border cursor-pointer transition-all flex items-start gap-4 group ${
                      isSelected 
                        ? 'bg-amber-900/30 border-amber-500 shadow-[0_0_15px_rgba(245,158,11,0.2)]' 
                        : 'bg-black/40 border-amber-900/20 hover:border-amber-500/30 hover:bg-black/60'
                    }`}
                  >
                    <div className={`w-10 h-10 rounded-lg border flex items-center justify-center shrink-0 transition-colors ${
                      isSelected ? 'bg-amber-500/20 border-amber-500 text-amber-400' : 'bg-black/50 border-amber-900/50 text-amber-700/50 group-hover:text-amber-500/50'
                    }`}>
                      <Scroll size={20} />
                    </div>
                    <div>
                      <h4 className={`font-serif font-bold transition-colors ${isSelected ? 'text-amber-400' : 'text-amber-100 group-hover:text-amber-200'}`}>
                        {fragment.name}
                      </h4>
                      <p className="text-xs text-amber-100/60 mt-1">{fragment.description}</p>
                      <span className="inline-block mt-2 text-[9px] uppercase tracking-widest font-mono text-amber-700/70 bg-black/50 px-2 py-0.5 rounded border border-amber-900/30">
                        {fragment.type}
                      </span>
                    </div>
                  </div>
                );
              })
            )}
          </div>

          <div className="mt-6 pt-6 border-t border-amber-900/30 flex items-center justify-between relative z-10">
            <div className="flex items-center gap-2">
              {[0, 1, 2].map(i => (
                <div key={i} className={`w-3 h-3 rounded-full border transition-colors ${
                  i < selectedFragments.length ? 'bg-amber-500 border-amber-400 shadow-[0_0_8px_rgba(245,158,11,0.5)]' : 'bg-black/50 border-amber-900/50'
                }`} />
              ))}
              <span className="text-[10px] font-mono text-amber-700/70 uppercase tracking-widest ml-2">
                Sélectionnés
              </span>
            </div>
            
            <button
              onClick={handleDeduce}
              disabled={selectedFragments.length < 3}
              className={`flex items-center gap-2 px-6 py-3 rounded-xl text-sm font-bold uppercase tracking-widest transition-all shadow-lg ${
                selectedFragments.length >= 3
                  ? 'bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white border border-amber-400/20 active:scale-95'
                  : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
              }`}
            >
              <GitMerge size={18} />
              Déduire
            </button>
          </div>
        </motion.div>
      </div>

      {/* Target Selection Modal */}
      <AnimatePresence>
        {selectedPower && activePower && activePower.targetType !== 'mirror' && (
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
                onClick={() => setSelectedPower(null)}
                className="absolute top-6 right-6 text-amber-700/50 hover:text-amber-500 transition-colors"
              >
                <X size={24} />
              </button>
              
              <div className="flex items-center gap-4 mb-2">
                <div className={`w-10 h-10 rounded-xl bg-black/50 border shadow-inner flex items-center justify-center ${activePower.border} ${activePower.color}`}>
                  <activePower.icon size={20} />
                </div>
                <h3 className="text-3xl font-calligraphy text-amber-500">Cible du Pouvoir</h3>
              </div>
              
              <p className="text-sm text-amber-100/60 font-serif mb-8 ml-14">
                Sélectionnez la cible pour <span className="text-amber-400 font-bold">{activePower.name}</span>
              </p>
              
              <div className="overflow-y-auto pr-2 custom-scrollbar space-y-3">
                {(activePower.targetType === 'member' || activePower.targetType === 'both') && clan.members.filter(m => m.isAlive).map(member => (
                  <button
                    key={member.id}
                    onClick={() => handleCast(member.id)}
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

                {(activePower.targetType === 'sect' || activePower.targetType === 'both') && clan.sects.map(sect => (
                  <button
                    key={sect.id}
                    onClick={() => handleCast(sect.id)}
                    className="w-full text-left p-4 rounded-xl border border-amber-900/30 bg-black/40 hover:bg-amber-900/20 hover:border-amber-500/50 transition-all flex items-center gap-4 group"
                  >
                    <div className="w-10 h-10 rounded-lg bg-black/50 border border-amber-900/50 flex items-center justify-center shrink-0">
                      <Hexagon size={20} className="text-amber-700/50" />
                    </div>
                    <div className="flex-1">
                      <h4 className="font-serif font-bold text-amber-100 group-hover:text-amber-400 transition-colors">
                        {sect.name}
                      </h4>
                      <p className="text-[10px] text-amber-700/70 font-mono uppercase tracking-widest mt-1">
                        Puissance: {sect.power} • Relation: {sect.relation}
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
