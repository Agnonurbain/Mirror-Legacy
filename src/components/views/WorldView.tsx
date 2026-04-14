import React from 'react';
import { Clan, Sect, GameEvent, WorldEvent } from '../../types/game';
import { Globe, Shield, Sword, Heart, Users, ExternalLink, Timer, TrendingUp, TrendingDown, AlertTriangle, Hexagon, Mountain, Cloud } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';

interface WorldViewProps {
  clan: Clan;
  events: GameEvent[];
  onInteract: (sectId: string, action: 'Diplomacy' | 'Tribute' | 'Challenge') => void;
  onUseDivinePower: (powerId: string, targetId: string) => void;
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

export const WorldView: React.FC<WorldViewProps> = ({ clan, events, onInteract, onUseDivinePower }) => {
  const getRelationColor = (relation: string) => {
    switch (relation) {
      case 'Hostile': return 'text-red-500';
      case 'Neutre': return 'text-stone-400';
      case 'Amical': return 'text-jade-400';
      case 'Allié': return 'text-blue-400';
      default: return 'text-stone-400';
    }
  };

  const getPowerColor = (power: string) => {
    switch (power) {
      case 'Faible': return 'text-stone-500';
      case 'Moyenne': return 'text-blue-400';
      case 'Élevée': return 'text-purple-400';
      case 'Légendaire': return 'text-amber-400';
      default: return 'text-stone-400';
    }
  };

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen relative"
    >
      {/* Background Ambience */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/stardust.png')] opacity-20 pointer-events-none mix-blend-screen" />
      <div className="absolute inset-0 bg-gradient-to-b from-transparent via-[#140d0a]/50 to-[#0a0a0a] pointer-events-none" />

      {/* Header Section */}
      <motion.div 
        variants={itemVariants}
        className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-8 md:p-12 shadow-2xl mb-12 relative overflow-hidden group"
      >
        {/* Decorative Background */}
        <div className="absolute inset-0 opacity-30 mix-blend-screen pointer-events-none"
             style={{
               backgroundImage: `radial-gradient(circle at 80% 20%, #3e2723 0%, transparent 50%), radial-gradient(circle at 20% 80%, #1a1510 0%, #000 100%)`
             }} />
        <div className="absolute top-0 right-0 w-1/2 h-full bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiNkOTc3MDYiIGZpbGwtb3BhY2l0eT0iMC4wNSIvPjwvc3ZnPg==')] opacity-50" />
        
        <div className="relative z-10 flex flex-col md:flex-row items-center gap-10">
          <div className="w-32 h-32 rounded-full bg-gradient-to-br from-amber-900/40 to-black border border-amber-500/30 flex items-center justify-center shrink-0 shadow-[0_0_30px_rgba(217,119,6,0.2)] group-hover:shadow-[0_0_50px_rgba(217,119,6,0.4)] transition-shadow duration-700 relative">
            <div className="absolute inset-0 rounded-full border border-amber-500/20 animate-[spin_10s_linear_infinite]" />
            <div className="absolute inset-2 rounded-full border border-amber-500/10 animate-[spin_15s_linear_infinite_reverse]" />
            <Globe className="text-amber-500" size={48} />
          </div>
          
          <div className="flex-1 text-center md:text-left">
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 mb-4 drop-shadow-md">Le Monde Spirituel</h2>
            
            <div className="inline-flex items-center gap-3 px-6 py-2 rounded-full bg-black/60 border border-amber-900/50 backdrop-blur-md mb-6">
              <span className="w-2 h-2 rounded-full bg-jade-500 animate-pulse shadow-[0_0_10px_rgba(74,222,128,0.5)]" />
              <span className="font-mono text-sm text-amber-500 uppercase tracking-widest">
                {clan.worldStatus}
              </span>
            </div>
            
            <p className="text-amber-100/70 max-w-3xl leading-relaxed font-serif italic text-lg md:text-xl">
              "Le monde de la cultivation est vaste et en perpétuel changement. Les tensions entre les grandes sectes 
              influencent directement la difficulté de votre ascension et les opportunités de commerce."
            </p>
          </div>
        </div>
      </motion.div>

      {/* Ongoing World Events */}
      {clan.worldEvents && clan.worldEvents.length > 0 && (
        <div className="mb-12">
          <h3 className="text-2xl font-calligraphy text-amber-500 mb-6 flex items-center gap-3">
            <Cloud className="text-amber-700" size={24} />
            Événements Mondiaux
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 relative z-10">
            {clan.worldEvents.map((event) => (
              <motion.div 
                key={event.id} 
                variants={itemVariants}
                whileHover={{ y: -4, scale: 1.01 }}
                className={`bg-[#1a120f] border rounded-2xl p-6 flex items-start gap-6 shadow-lg transition-all ${
                  event.type === 'Positive' ? 'border-jade-900/40 hover:border-jade-500/50' :
                  event.type === 'Negative' ? 'border-red-900/40 hover:border-red-500/50' :
                  'border-amber-900/30 hover:border-amber-500/50'
                }`}
              >
                <div className={`p-4 rounded-2xl flex items-center justify-center shrink-0 ${
                  event.type === 'Positive' ? 'bg-jade-900/20 text-jade-400 border border-jade-500/30' :
                  event.type === 'Negative' ? 'bg-red-900/20 text-red-500 border border-red-500/30' :
                  'bg-amber-900/20 text-amber-500 border border-amber-500/30'
                }`}>
                  {event.type === 'Positive' ? <TrendingUp size={24} /> : 
                   event.type === 'Negative' ? <TrendingDown size={24} /> : 
                   <AlertTriangle size={24} />}
                </div>
                
                <div className="flex-1">
                  <div className="flex justify-between items-start mb-2">
                    <h4 className="font-serif font-bold text-lg text-amber-100">{event.name}</h4>
                    <div className="flex items-center gap-1.5 text-[10px] font-mono uppercase tracking-widest text-amber-700/70 bg-black/40 px-3 py-1 rounded-full border border-amber-900/30">
                      <Timer size={12} />
                      <span>{event.duration} cycles restants</span>
                    </div>
                  </div>
                  <p className="text-sm text-amber-100/60 leading-relaxed font-serif">{event.description}</p>
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      )}

      {/* Sects Grid */}
      <div>
        <h3 className="text-2xl font-calligraphy text-amber-500 mb-6 flex items-center gap-3">
          <Mountain className="text-amber-700" size={24} />
          Sectes Connues
        </h3>
        <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-8">
          {clan.sects.map(sect => (
            <motion.div 
              key={sect.id} 
              variants={itemVariants}
              whileHover={{ y: -5 }}
              className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-8 relative overflow-hidden group hover:border-amber-500/50 transition-all shadow-xl"
            >
              {/* Card Background Texture */}
              <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
              
              <div className="flex justify-between items-start mb-6 relative z-10">
                <div className="flex items-center gap-4">
                  <div className="w-14 h-14 rounded-2xl bg-black/50 border border-amber-900/50 flex items-center justify-center shadow-inner">
                    <Hexagon size={28} className="text-amber-700/50" />
                  </div>
                  <div>
                    <h3 className="text-2xl font-calligraphy text-amber-100 group-hover:text-amber-400 transition-colors">{sect.name}</h3>
                    <p className="text-xs font-mono text-amber-700/70 uppercase tracking-widest mt-1">Secte Extérieure</p>
                  </div>
                </div>
                
                <div className="flex flex-col items-end gap-2">
                  <div className={`text-xs font-bold uppercase tracking-widest px-3 py-1 rounded-full border bg-black/40 ${
                    sect.relation === 'Hostile' ? 'text-red-500 border-red-900/50' :
                    sect.relation === 'Amical' ? 'text-jade-400 border-jade-900/50' :
                    sect.relation === 'Allié' ? 'text-blue-400 border-blue-900/50' :
                    'text-stone-400 border-stone-800'
                  }`}>
                    {sect.relation}
                  </div>
                  <div className="text-[10px] font-mono text-amber-700/50 uppercase tracking-widest">
                    Faveur: {sect.favor}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4 mb-8 relative z-10">
                <div className="bg-black/30 border border-amber-900/20 rounded-xl p-4 flex items-center gap-3">
                  <Sword size={16} className="text-amber-700/50" />
                  <div>
                    <p className="text-[9px] uppercase tracking-widest font-mono text-amber-700/60 mb-1">Puissance</p>
                    <p className={`text-sm font-bold font-serif ${getPowerColor(sect.power)}`}>{sect.power}</p>
                  </div>
                </div>
                <div className="bg-black/30 border border-amber-900/20 rounded-xl p-4 flex items-center gap-3">
                  <Users size={16} className="text-amber-700/50" />
                  <div>
                    <p className="text-[9px] uppercase tracking-widest font-mono text-amber-700/60 mb-1">Disciples</p>
                    <p className="text-sm font-bold font-serif text-amber-100/80">{sect.estimatedMembers}</p>
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap gap-3 relative z-10">
                <button 
                  onClick={() => onInteract(sect.id, 'Diplomacy')}
                  className="flex-1 bg-amber-900/20 hover:bg-amber-900/40 border border-amber-500/30 text-amber-400 px-4 py-2.5 rounded-xl text-xs font-bold uppercase tracking-widest transition-all flex items-center justify-center gap-2"
                >
                  <Heart size={14} />
                  Diplomatie
                </button>
                <button 
                  onClick={() => onInteract(sect.id, 'Tribute')}
                  className="flex-1 bg-black/40 hover:bg-black/60 border border-amber-900/50 text-amber-100/70 hover:text-amber-400 px-4 py-2.5 rounded-xl text-xs font-bold uppercase tracking-widest transition-all flex items-center justify-center gap-2"
                >
                  <ExternalLink size={14} />
                  Tribut
                </button>
                <button 
                  onClick={() => onInteract(sect.id, 'Challenge')}
                  className="flex-1 bg-red-900/20 hover:bg-red-900/40 border border-red-500/30 text-red-500 px-4 py-2.5 rounded-xl text-xs font-bold uppercase tracking-widest transition-all flex items-center justify-center gap-2"
                >
                  <Shield size={14} />
                  Défi
                </button>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </motion.div>
  );
};
