import React, { useEffect, useState } from 'react';
import { Clan, GameEvent } from '../../types/game';
import { REALM_CONFIG } from '../../constants';
import { Shield, Users, Sword, Activity, Sparkles, Globe, Zap, Wind, BookOpen, ScrollText, Play, Hourglass, Search, Mountain } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';

interface OverviewViewProps {
  clan: Clan;
  events: GameEvent[];
  onAdvanceYears: (years: number) => void;
  isAdvancing: boolean;
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

export const OverviewView: React.FC<OverviewViewProps> = ({ clan, events, onAdvanceYears, isAdvancing }) => {
  const [showLightning, setShowLightning] = useState(false);
  const [cyclesToAdvance, setCyclesToAdvance] = useState(1);
  const [eventSearchQuery, setEventSearchQuery] = useState('');
  const livingMembers = clan.members.filter(m => m.isAlive);
  
  const highestRealm = livingMembers.length > 0 ? livingMembers.reduce((highest, current) => {
    const realms = ['Mortel', 'Embryonnaire', 'Raffinement du Qi', 'Fondation', 'Manoir Pourpre', "Noyau d'Or", 'Embryon Dao', 'Immortel'];
    return realms.indexOf(current.realm) > realms.indexOf(highest) ? current.realm : highest;
  }, 'Mortel') : 'Aucun';

  useEffect(() => {
    const lastEvent = events[0];
    if (lastEvent && (lastEvent.message.includes('Tribulation') || lastEvent.message.includes('Foudre'))) {
      setShowLightning(true);
      const timer = setTimeout(() => setShowLightning(false), 2000);
      return () => clearTimeout(timer);
    }
  }, [events]);

  const filteredEvents = events.filter(event => {
    if (!eventSearchQuery) return true;
    const query = eventSearchQuery.toLowerCase();
    return event.message.toLowerCase().includes(query) || event.year.toString().includes(query);
  });

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen relative"
    >
      {/* Tribulation Lightning Effect */}
      <AnimatePresence>
        {showLightning && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 pointer-events-none overflow-hidden"
          >
            <div className="lightning absolute top-0 left-1/4 w-px h-full bg-blue-400 shadow-[0_0_20px_#60a5fa]" />
            <div className="lightning absolute top-0 left-1/2 w-px h-full bg-blue-400 shadow-[0_0_20px_#60a5fa] delay-100" />
            <div className="lightning absolute top-0 left-3/4 w-px h-full bg-blue-400 shadow-[0_0_20px_#60a5fa] delay-300" />
            <div className="absolute inset-0 bg-blue-400/10 animate-pulse" />
          </motion.div>
        )}
      </AnimatePresence>

      <div className="grid grid-cols-1 xl:grid-cols-12 gap-8 relative z-10">
        
        {/* Left Column: Sect Mountain Visual & Time Controls */}
        <div className="xl:col-span-5 flex flex-col gap-8">
          
          {/* Sect Mountain Visual */}
          <motion.div variants={itemVariants} className="relative rounded-3xl overflow-hidden border border-amber-900/30 shadow-2xl bg-[#0a0a0a] min-h-[400px] xl:min-h-[500px] flex flex-col items-center justify-center group">
            {/* Mountain Background Art (CSS) */}
            <div className="absolute inset-0 opacity-40 mix-blend-screen pointer-events-none"
                 style={{
                   backgroundImage: `radial-gradient(circle at 50% 100%, #3e2723 0%, transparent 60%), radial-gradient(circle at 50% 50%, #1a1510 0%, #000 100%)`
                 }} />
            <div className="absolute bottom-0 w-full h-1/2 bg-gradient-to-t from-[#1a100c] to-transparent z-10 pointer-events-none" />
            
            {/* Abstract Mountains */}
            <div className="absolute bottom-0 left-1/2 -translate-x-1/2 w-[150%] h-[60%] opacity-20 pointer-events-none">
              <svg viewBox="0 0 100 100" preserveAspectRatio="none" className="w-full h-full fill-amber-700">
                <polygon points="0,100 20,40 40,80 50,20 70,70 100,30 100,100" />
              </svg>
            </div>
            <div className="absolute bottom-0 left-1/2 -translate-x-1/2 w-[120%] h-[40%] opacity-30 pointer-events-none">
              <svg viewBox="0 0 100 100" preserveAspectRatio="none" className="w-full h-full fill-amber-900">
                <polygon points="0,100 30,50 50,80 70,40 100,60 100,100" />
              </svg>
            </div>

            {/* Clouds */}
            <div className="absolute inset-0 cloud-motif opacity-30 animate-qi-flow pointer-events-none" />

            {/* Content */}
            <div className="relative z-20 flex flex-col items-center text-center p-8">
              <div className="w-24 h-24 rounded-full bg-gradient-to-br from-amber-900/40 to-black border border-amber-500/30 flex items-center justify-center mb-8 shadow-[0_0_30px_rgba(217,119,6,0.2)] group-hover:shadow-[0_0_50px_rgba(217,119,6,0.4)] transition-shadow duration-700">
                <Mountain className="text-amber-500" size={40} />
              </div>
              
              <h1 className="text-6xl md:text-8xl font-calligraphy text-amber-500 drop-shadow-lg mb-4 tracking-wider">
                {clan.name}
              </h1>
              
              <div className="flex items-center justify-center gap-4 mb-8 w-full max-w-md">
                <div className="h-px flex-1 bg-gradient-to-r from-transparent to-amber-500/50" />
                <p className="font-serif italic text-lg md:text-xl text-amber-100/70 text-center px-4">
                  "{clan.motto}"
                </p>
                <div className="h-px flex-1 bg-gradient-to-l from-transparent to-amber-500/50" />
              </div>

              <div className="inline-flex items-center gap-3 px-6 py-2 rounded-full bg-black/60 border border-amber-900/50 backdrop-blur-md">
                <span className="w-2 h-2 rounded-full bg-jade-500 animate-pulse shadow-[0_0_10px_rgba(74,222,128,0.5)]" />
                <span className="font-mono text-sm text-amber-500 uppercase tracking-widest">
                  {clan.worldStatus}
                </span>
              </div>
            </div>
          </motion.div>

          {/* Time Controls */}
          <motion.div variants={itemVariants} className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-6 md:p-8 shadow-xl relative overflow-hidden">
            <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
            
            <div className="relative z-10 flex flex-col gap-6">
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 rounded-full bg-amber-900/20 flex items-center justify-center border border-amber-500/30">
                  <Hourglass size={24} className="text-amber-500" />
                </div>
                <div>
                  <h3 className="text-2xl font-calligraphy text-amber-500">Méditation</h3>
                  <p className="text-xs text-amber-700/70 font-mono uppercase tracking-widest">Avancer le temps</p>
                </div>
              </div>
              
              <div className="flex flex-col sm:flex-row items-center gap-4">
                <div className="flex bg-black/50 rounded-xl border border-amber-900/30 p-1.5 w-full sm:w-auto justify-center">
                  {[1, 5, 10, 50].map(val => (
                    <button
                      key={val}
                      onClick={() => setCyclesToAdvance(val)}
                      disabled={isAdvancing}
                      className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${cyclesToAdvance === val ? 'bg-amber-700 text-white shadow-md' : 'text-amber-700/50 hover:text-amber-500 hover:bg-amber-900/20'} disabled:opacity-50`}
                    >
                      {val}
                    </button>
                  ))}
                  <div className="h-8 w-px bg-amber-900/30 mx-2 self-center" />
                  <div className="relative flex items-center">
                    <input
                      type="text"
                      value={cyclesToAdvance || ''}
                      onChange={(e) => {
                        const val = e.target.value.replace(/\D/g, '');
                        if (val === '') setCyclesToAdvance(0);
                        else {
                          const num = parseInt(val, 10);
                          setCyclesToAdvance(Math.min(Math.max(num, 1), 1000));
                        }
                      }}
                      disabled={isAdvancing}
                      placeholder="Cycles"
                      className="w-20 bg-transparent border-none py-2 px-2 text-sm font-mono text-amber-500 placeholder:text-amber-700/30 focus:outline-none text-center"
                    />
                  </div>
                </div>
                
                <button
                  onClick={() => onAdvanceYears(Math.max(1, cyclesToAdvance))}
                  disabled={isAdvancing || cyclesToAdvance < 1}
                  className="w-full sm:w-auto bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white px-8 py-3.5 rounded-xl flex items-center justify-center gap-3 font-serif font-bold text-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-lg active:scale-95 border border-amber-400/20 group"
                >
                  {isAdvancing ? (
                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  ) : (
                    <Play size={18} fill="currentColor" className="group-hover:scale-110 transition-transform" />
                  )}
                  <span>{isAdvancing ? 'En cours...' : 'Avancer'}</span>
                </button>
              </div>
            </div>
          </motion.div>
        </div>

        {/* Right Column: Stats & Chronicles */}
        <div className="xl:col-span-7 flex flex-col gap-8 h-full">
          
          {/* Stats Grid */}
          <motion.div variants={itemVariants} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {[
              { label: 'Disciples', value: livingMembers.length, icon: Users, color: 'text-jade-500', bg: 'bg-jade-950/20', border: 'border-jade-900/30' },
              { label: 'Sommet', value: highestRealm, icon: Activity, color: 'text-purple-400', bg: 'bg-purple-950/20', border: 'border-purple-900/30' },
              { label: 'Puissance', value: livingMembers.reduce((sum, m) => sum + REALM_CONFIG[m.realm].power, 0).toLocaleString(), icon: Sword, color: 'text-red-400', bg: 'bg-red-950/20', border: 'border-red-900/30' },
              { label: 'Destinée', value: clan.destiny, icon: Sparkles, color: 'text-amber-400', bg: 'bg-amber-950/20', border: 'border-amber-900/30' },
            ].map((stat, i) => (
              <div key={i} className={`p-6 rounded-2xl border ${stat.border} ${stat.bg} flex flex-col items-center text-center relative overflow-hidden group`}>
                <div className="absolute inset-0 bg-gradient-to-b from-white/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
                <stat.icon size={20} className={`${stat.color} mb-3 opacity-70`} />
                <span className={`text-2xl md:text-3xl font-serif font-bold ${stat.color} mb-1`}>{stat.value}</span>
                <span className="text-[10px] uppercase tracking-widest text-white/40">{stat.label}</span>
              </div>
            ))}
          </motion.div>

          {/* Chronicles */}
          <motion.div variants={itemVariants} className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-6 md:p-8 shadow-xl flex-1 flex flex-col min-h-[500px]">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
              <div className="flex items-center gap-4">
                <ScrollText className="text-amber-500" size={28} />
                <h2 className="text-3xl font-calligraphy text-amber-500">Annales du Clan</h2>
              </div>
              
              <div className="relative w-full sm:w-64">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-amber-700/50" size={16} />
                <input
                  type="text"
                  value={eventSearchQuery}
                  onChange={(e) => setEventSearchQuery(e.target.value)}
                  placeholder="Rechercher..."
                  className="w-full bg-black/50 border border-amber-900/50 rounded-full py-2 pl-10 pr-4 text-sm font-mono text-amber-500 placeholder:text-amber-700/30 focus:outline-none focus:border-amber-500/50 transition-colors"
                />
              </div>
            </div>
            
            <div className="flex-1 overflow-y-auto pr-4 custom-scrollbar relative">
              <div className="absolute left-[15px] top-0 bottom-0 w-px bg-gradient-to-b from-amber-900/50 via-amber-900/20 to-transparent" />
              
              {filteredEvents.length === 0 ? (
                <div className="pl-12 py-12 text-center opacity-50 font-serif italic text-amber-700">
                  Aucun événement ne correspond à votre recherche.
                </div>
              ) : (
                <AnimatePresence mode="popLayout">
                  {filteredEvents.slice(0, 50).map((event) => (
                    <motion.div 
                      key={event.id}
                      layout
                      initial={{ opacity: 0, x: -20 }}
                      animate={{ opacity: 1, x: 0 }}
                      exit={{ opacity: 0, scale: 0.9 }}
                      className="pl-12 py-4 relative group"
                    >
                      <div className={`absolute left-[11px] top-[26px] w-2.5 h-2.5 rounded-full border-2 border-[#140d0a] z-10 ${
                        event.type === 'Success' ? 'bg-jade-500 shadow-[0_0_10px_rgba(34,197,94,0.5)]' :
                        event.type === 'Danger' ? 'bg-red-500 shadow-[0_0_10px_rgba(239,68,68,0.5)]' :
                        'bg-amber-500 shadow-[0_0_10px_rgba(245,158,11,0.5)]'
                      }`} />
                      
                      <div className="bg-black/30 border border-amber-900/20 rounded-xl p-5 hover:border-amber-900/50 transition-colors">
                        <div className="flex items-center gap-4 mb-2">
                          <span className="text-[10px] font-mono text-amber-700/70 uppercase tracking-widest bg-amber-900/10 px-2 py-0.5 rounded">
                            An {event.year}
                          </span>
                        </div>
                        <p className="text-amber-100/80 font-serif text-lg leading-relaxed">
                          {event.message}
                        </p>
                      </div>
                    </motion.div>
                  ))}
                </AnimatePresence>
              )}
            </div>
          </motion.div>

        </div>
      </div>
    </motion.div>
  );
};
