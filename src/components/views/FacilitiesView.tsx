import React from 'react';
import { Facility, Clan } from '../../types/game';
import { Building2, ArrowUpCircle, Zap, Coins, Shield, Sparkles, Flame, Wind, Droplets, Mountain } from 'lucide-react';
import { motion } from 'motion/react';

interface FacilitiesViewProps {
  clan: Clan;
  onUpgrade: (facilityId: string) => void;
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

export const FacilitiesView: React.FC<FacilitiesViewProps> = ({ clan, onUpgrade }) => {
  const getIcon = (type: string) => {
    switch (type) {
      case 'Cultivation': return <Zap className="text-purple-400" size={24} />;
      case 'Wealth': return <Coins className="text-amber-400" size={24} />;
      case 'Defense': return <Shield className="text-blue-400" size={24} />;
      case 'Prestige': return <Sparkles className="text-jade-400" size={24} />;
      default: return <Building2 className="text-stone-400" size={24} />;
    }
  };

  const getBgColor = (type: string) => {
    switch (type) {
      case 'Cultivation': return 'from-purple-900/20 to-transparent border-purple-900/30';
      case 'Wealth': return 'from-amber-900/20 to-transparent border-amber-900/30';
      case 'Defense': return 'from-blue-900/20 to-transparent border-blue-900/30';
      case 'Prestige': return 'from-jade-900/20 to-transparent border-jade-900/30';
      default: return 'from-stone-900/20 to-transparent border-stone-900/30';
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
            <Building2 className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Pavillons du Clan</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Infrastructures de la Secte</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-amber-900/50 shadow-inner">
          <span className="text-xs text-amber-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <Coins size={14} className="text-amber-500" />
            Trésorerie
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-amber-400 drop-shadow-[0_0_10px_rgba(251,191,36,0.5)]">
              {clan.wealth.toLocaleString()}
            </span>
            <span className="text-sm text-amber-700/50 font-serif italic">Pierres d'Esprit</span>
          </div>
        </div>
      </motion.div>

      {/* Facilities Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 relative z-10">
        {clan.facilities.map((facility) => (
          <motion.div 
            key={facility.id} 
            variants={itemVariants}
            whileHover={{ y: -5, scale: 1.01 }}
            className={`bg-[#1a120f] border rounded-3xl p-8 flex flex-col justify-between transition-all shadow-xl relative overflow-hidden group bg-gradient-to-br ${getBgColor(facility.type)}`}
          >
            {/* Decorative Elements */}
            <div className="absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl from-white/5 to-transparent rounded-bl-full pointer-events-none" />
            <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
              {facility.type === 'Cultivation' ? <Flame size={150} /> :
               facility.type === 'Wealth' ? <Coins size={150} /> :
               facility.type === 'Defense' ? <Shield size={150} /> :
               <Sparkles size={150} />}
            </div>

            <div className="relative z-10">
              <div className="flex items-start justify-between mb-8">
                <div className="flex items-center gap-5">
                  <div className="w-16 h-16 bg-black/50 rounded-2xl border border-amber-900/30 shadow-inner flex items-center justify-center group-hover:border-amber-500/50 transition-colors">
                    {getIcon(facility.type)}
                  </div>
                  <div>
                    <h3 className="text-2xl font-calligraphy text-amber-100 group-hover:text-amber-400 transition-colors">{facility.name}</h3>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="text-[10px] text-amber-700/70 font-mono uppercase tracking-[0.2em] bg-black/40 px-2 py-0.5 rounded border border-amber-900/30">
                        Rang {facility.level}
                      </span>
                    </div>
                  </div>
                </div>
                
                <div className="text-right bg-black/40 px-4 py-2 rounded-xl border border-amber-900/30">
                  <span className="text-[9px] text-amber-700/60 uppercase tracking-widest block mb-1 font-mono">Bonus Actuel</span>
                  <span className="text-xl font-mono text-jade-400 drop-shadow-[0_0_8px_rgba(52,211,153,0.4)]">
                    {facility.type === 'Wealth' ? `+${facility.bonus}` : `+${facility.bonus}%`}
                  </span>
                </div>
              </div>
              
              <p className="text-amber-100/60 text-sm leading-relaxed mb-8 font-serif italic border-l-2 border-amber-900/30 pl-4">
                "{facility.description}"
              </p>
            </div>

            <div className="pt-6 border-t border-amber-900/20 flex items-center justify-between relative z-10">
              <div className="flex flex-col">
                <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono mb-1">Coût d'Élévation</span>
                <div className="flex items-baseline gap-1.5">
                  <span className={`text-xl font-mono ${clan.wealth >= facility.cost ? 'text-amber-400' : 'text-red-400'}`}>
                    {facility.cost.toLocaleString()}
                  </span>
                  <span className="text-[10px] text-amber-700/40 uppercase tracking-widest">Pierres</span>
                </div>
              </div>
              
              <button
                onClick={() => onUpgrade(facility.id)}
                disabled={clan.wealth < facility.cost}
                className={`flex items-center gap-2 px-6 py-3 rounded-xl text-sm font-bold uppercase tracking-widest transition-all shadow-lg ${
                  clan.wealth >= facility.cost
                    ? 'bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white border border-amber-400/20 active:scale-95'
                    : 'bg-stone-900/50 text-stone-500 border border-stone-800 cursor-not-allowed'
                }`}
              >
                <ArrowUpCircle size={18} className={clan.wealth >= facility.cost ? 'animate-bounce' : ''} />
                <span>Élever</span>
              </button>
            </div>
          </motion.div>
        ))}
      </div>
    </motion.div>
  );
};
