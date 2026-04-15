import React, { useState } from 'react';
import { Clan, Manual } from '../../types/game';
import { Book, Scroll, Sparkles, Shield, Sword, FlaskConical, Hammer, GitMerge, Hexagon } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';

interface ManualsViewProps {
  clan: Clan;
  onDeduceManual: (manualId1: string, manualId2: string) => void;
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

export const ManualsView: React.FC<ManualsViewProps> = ({ clan, onDeduceManual }) => {
  const manuals = clan.manuals || [];
  const [deductionMode, setDeductionMode] = useState(false);
  const [selectedManuals, setSelectedManuals] = useState<string[]>([]);

  const handleSelectManual = (id: string) => {
    if (!deductionMode) return;
    if (selectedManuals.includes(id)) {
      setSelectedManuals(selectedManuals.filter(mId => mId !== id));
    } else if (selectedManuals.length < 2) {
      setSelectedManuals([...selectedManuals, id]);
    }
  };

  const handleDeduce = () => {
    if (selectedManuals.length === 2) {
      onDeduceManual(selectedManuals[0], selectedManuals[1]);
      setSelectedManuals([]);
      setDeductionMode(false);
    }
  };

  const getIcon = (type: Manual['type']) => {
    switch (type) {
      case 'Culture': return <Sparkles size={24} className="text-blue-400" />;
      case 'Combat': return <Sword size={24} className="text-red-400" />;
      case 'Alchimie': return <FlaskConical size={24} className="text-emerald-400" />;
      case 'Forge': return <Hammer size={24} className="text-orange-400" />;
      default: return <Book size={24} className="text-amber-400" />;
    }
  };

  const getRarityColor = (rarity: Manual['rarity']) => {
    switch (rarity) {
      case 'Légendaire': return 'text-amber-400 border-amber-500/50 shadow-[0_0_15px_rgba(251,191,36,0.3)]';
      case 'Épique': return 'text-purple-400 border-purple-500/50 shadow-[0_0_15px_rgba(168,85,247,0.3)]';
      case 'Rare': return 'text-blue-400 border-blue-500/50 shadow-[0_0_15px_rgba(59,130,246,0.3)]';
      default: return 'text-stone-400 border-stone-500/50';
    }
  };

  const getRarityBg = (rarity: Manual['rarity']) => {
    switch (rarity) {
      case 'Légendaire': return 'bg-amber-900/20';
      case 'Épique': return 'bg-purple-900/20';
      case 'Rare': return 'bg-blue-900/20';
      default: return 'bg-stone-900/20';
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
            <Book className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Bibliothèque des Sutras</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Savoir Ancestral du Clan</p>
          </div>
        </div>
        
        <div className="flex flex-col md:flex-row items-center gap-6 relative z-10">
          <button
            onClick={() => {
              setDeductionMode(!deductionMode);
              setSelectedManuals([]);
            }}
            className={`flex items-center gap-2 px-6 py-3 rounded-xl text-xs font-bold uppercase tracking-widest transition-all shadow-lg ${
              deductionMode 
                ? 'bg-amber-500 text-black border border-amber-400 shadow-[0_0_20px_rgba(245,158,11,0.4)]' 
                : 'bg-black/60 text-amber-500 border border-amber-900/50 hover:bg-amber-900/30 hover:border-amber-500/50'
            }`}
          >
            <GitMerge size={16} />
            Mode Déduction
          </button>
          
          <div className="flex flex-col items-center md:items-end bg-black/40 p-4 rounded-2xl border border-amber-900/50 shadow-inner">
            <span className="text-xs text-amber-700/70 uppercase tracking-[0.3em] font-mono mb-1 flex items-center gap-2">
              <Scroll size={14} className="text-amber-500" />
              Collection
            </span>
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-mono text-amber-400 drop-shadow-[0_0_10px_rgba(251,191,36,0.5)]">
                {manuals.length}
              </span>
              <span className="text-xs text-amber-700/50 font-serif italic">Manuels</span>
            </div>
          </div>
        </div>
      </motion.div>

      {/* Deduction Mode Banner */}
      <AnimatePresence>
        {deductionMode && (
          <motion.div 
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="bg-gradient-to-r from-amber-900/40 via-amber-800/20 to-amber-900/40 border border-amber-500/30 rounded-2xl p-6 mb-8 flex flex-col md:flex-row items-center justify-between gap-6 shadow-[0_0_30px_rgba(245,158,11,0.1)] relative z-10"
          >
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-full bg-amber-500/20 flex items-center justify-center border border-amber-500/50">
                <GitMerge className="text-amber-400" size={24} />
              </div>
              <div>
                <h3 className="text-xl font-serif font-bold text-amber-100">Fusion de Manuels</h3>
                <p className="text-sm text-amber-100/60 font-serif italic">Sélectionnez deux manuels pour tenter d'en déduire un nouveau.</p>
              </div>
            </div>
            
            <div className="flex items-center gap-6">
              <div className="flex items-center gap-3">
                <div className={`w-14 h-14 rounded-xl border-2 flex items-center justify-center transition-all ${selectedManuals[0] ? 'border-amber-500 bg-amber-900/30 shadow-[0_0_15px_rgba(245,158,11,0.3)]' : 'border-amber-900/30 bg-black/40 border-dashed'}`}>
                  {selectedManuals[0] ? <Book className="text-amber-400" size={24} /> : <span className="text-amber-700/30 font-mono">1</span>}
                </div>
                <span className="text-amber-700/50 font-bold">+</span>
                <div className={`w-14 h-14 rounded-xl border-2 flex items-center justify-center transition-all ${selectedManuals[1] ? 'border-amber-500 bg-amber-900/30 shadow-[0_0_15px_rgba(245,158,11,0.3)]' : 'border-amber-900/30 bg-black/40 border-dashed'}`}>
                  {selectedManuals[1] ? <Book className="text-amber-400" size={24} /> : <span className="text-amber-700/30 font-mono">2</span>}
                </div>
              </div>
              
              <button
                onClick={handleDeduce}
                disabled={selectedManuals.length !== 2}
                className={`px-6 py-3 rounded-xl font-bold uppercase tracking-widest text-sm transition-all ${
                  selectedManuals.length === 2
                    ? 'bg-amber-500 text-black hover:bg-amber-400 shadow-[0_0_20px_rgba(245,158,11,0.4)]'
                    : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
                }`}
              >
                Déduire
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {manuals.length === 0 ? (
        <motion.div variants={itemVariants} className="bg-[#1a120f] border border-amber-900/30 rounded-3xl p-16 text-center relative overflow-hidden shadow-xl">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          <div className="relative z-10 flex flex-col items-center">
            <div className="w-24 h-24 rounded-full bg-black/50 border border-amber-900/50 flex items-center justify-center mb-6">
              <Book className="w-12 h-12 text-amber-700/30" />
            </div>
            <h3 className="text-3xl font-calligraphy text-amber-500 mb-4">Bibliothèque Vide</h3>
            <p className="text-amber-100/50 font-serif italic text-lg max-w-md">
              Le clan ne possède aucun manuel. Explorez le monde ou achetez-en au marché pour enrichir votre savoir.
            </p>
          </div>
        </motion.div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8 relative z-10">
          {manuals.map(manual => {
            const isSelected = selectedManuals.includes(manual.id);
            const isSelectable = deductionMode && (isSelected || selectedManuals.length < 2);
            
            return (
              <motion.div 
                key={manual.id} 
                variants={itemVariants}
                whileHover={!deductionMode ? { y: -5, scale: 1.02 } : {}}
                onClick={() => isSelectable && handleSelectManual(manual.id)}
                className={`bg-[#1a120f] border rounded-3xl p-6 flex flex-col justify-between transition-all shadow-xl relative overflow-hidden group ${
                  isSelected 
                    ? 'border-amber-500 shadow-[0_0_30px_rgba(245,158,11,0.2)]' 
                    : deductionMode && !isSelectable
                      ? 'border-amber-900/10 opacity-50 grayscale'
                      : deductionMode
                        ? 'border-amber-900/50 hover:border-amber-400 cursor-pointer'
                        : 'border-amber-900/30 hover:border-amber-500/50'
                }`}
              >
                {/* Decorative Elements */}
                <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${getRarityBg(manual.rarity)} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
                <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                  <Hexagon size={120} />
                </div>

                {isSelected && (
                  <div className="absolute top-4 right-4 w-6 h-6 rounded-full bg-amber-500 flex items-center justify-center shadow-[0_0_10px_rgba(245,158,11,0.5)] z-20">
                    <div className="w-2 h-2 bg-black rounded-full" />
                  </div>
                )}

                <div className="relative z-10">
                  <div className="flex items-start justify-between mb-6">
                    <div className={`w-14 h-14 rounded-2xl bg-black/50 border shadow-inner flex items-center justify-center ${getRarityColor(manual.rarity)}`}>
                      {getIcon(manual.type)}
                    </div>
                    <div className={`px-3 py-1.5 rounded-lg border flex items-center gap-2 ${getRarityBg(manual.rarity)} ${getRarityColor(manual.rarity)}`}>
                      <span className="text-[9px] uppercase tracking-widest font-mono font-bold">
                        {manual.rarity}
                      </span>
                    </div>
                  </div>
                  
                  <h3 className="text-xl font-serif font-bold mb-2 text-amber-100 group-hover:text-amber-400 transition-colors">
                    {manual.name}
                  </h3>
                  
                  <div className="flex items-center gap-2 mb-4">
                    <span className="text-[10px] uppercase tracking-[0.2em] font-mono text-amber-700/70 bg-black/40 px-2 py-1 rounded border border-amber-900/30">
                      {manual.type}
                    </span>
                    <span className="text-[10px] uppercase tracking-[0.2em] font-mono text-amber-700/70 bg-black/40 px-2 py-1 rounded border border-amber-900/30">
                      Niv. {manual.level}
                    </span>
                  </div>
                  
                  <p className="text-sm text-amber-100/60 leading-relaxed mb-6 min-h-[60px] font-serif italic border-l-2 border-amber-900/30 pl-3">
                    {manual.description}
                  </p>
                </div>

                <div className="pt-4 border-t border-amber-900/20 flex items-center justify-between relative z-10">
                  <div className="flex items-center gap-2">
                    <Shield size={14} className="text-amber-700/50" />
                    <span className="text-xs font-mono text-amber-500">
                      +{manual.bonus.value} {manual.bonus.type}
                    </span>
                  </div>
                  {manual.statRequirements && (
                    <div className="flex items-center gap-2 text-[10px] text-amber-700/50 font-mono uppercase tracking-widest">
                      Req: {Object.entries(manual.statRequirements).map(([stat, val]) => `${stat.substring(0,3)} ${val}`).join(', ')}
                    </div>
                  )}
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </motion.div>
  );
};
