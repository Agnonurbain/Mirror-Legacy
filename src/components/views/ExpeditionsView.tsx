import React, { useState } from 'react';
import { Swords, ShieldAlert, Skull, Trophy, Users, User, ChevronRight, Info, Zap, MessageSquare, Gift, Sword, History, Target, Hexagon } from 'lucide-react';
import { GameEvent, Clan, Expedition, Character } from '../../types/game';
import { mockExpeditions } from '../../data/mockData';
import { REALMS } from '../../constants';
import { Avatar } from '../ui/Avatar';
import { motion, AnimatePresence } from 'motion/react';

interface ExpeditionsViewProps {
  clan: Clan;
  events: GameEvent[];
  onDispatch: (expeditionId: string, leaderId: string, memberIds: string[]) => void;
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

export const ExpeditionsView: React.FC<ExpeditionsViewProps> = ({ clan, events, onDispatch }) => {
  const [selectedExpedition, setSelectedExpedition] = useState<Expedition | null>(null);
  const [leaderId, setLeaderId] = useState<string>('');
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);

  const combatEvents = events.filter(e => e.type === 'Danger' || e.type === 'Success' || e.type === 'Warning');
  const livingMembers = clan.members.filter(m => m.isAlive);

  const handleToggleMember = (id: string) => {
    if (selectedMemberIds.includes(id)) {
      setSelectedMemberIds(prev => prev.filter(mid => mid !== id));
    } else {
      if (selectedMemberIds.length < 3) {
        setSelectedMemberIds(prev => [...prev, id]);
      }
    }
  };

  const canStart = selectedExpedition && leaderId;

  const handleStart = () => {
    if (canStart) {
      onDispatch(selectedExpedition.id, leaderId, selectedMemberIds);
      setSelectedExpedition(null);
      setLeaderId('');
      setSelectedMemberIds([]);
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
        <div className="absolute top-0 right-0 w-64 h-64 bg-red-900/10 rounded-full blur-3xl pointer-events-none" />
        
        <div className="flex items-center gap-6 relative z-10">
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-red-900/40 to-black border border-red-500/30 flex items-center justify-center shadow-[0_0_20px_rgba(239,68,68,0.2)]">
            <Swords className="text-red-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-red-500 drop-shadow-md">Guerres & Expéditions</h2>
            <p className="text-sm text-red-700/70 font-mono uppercase tracking-[0.2em] mt-2">Conquêtes du Clan</p>
          </div>
        </div>
        
        <div className="flex items-center gap-3 bg-black/40 px-6 py-3 rounded-2xl border border-amber-900/50 shadow-inner relative z-10">
          <ShieldAlert size={20} className="text-amber-500" />
          <span className="text-lg font-serif text-amber-100/80">Défense : Rang {clan.tier}</span>
        </div>
      </motion.div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 relative z-10">
        {/* Expedition Selection */}
        <div className="lg:col-span-2 space-y-8">
          <div className="flex items-center gap-4 mb-6">
            <Target className="text-red-500" size={24} />
            <h3 className="text-2xl font-calligraphy text-red-500">Missions de Secte</h3>
            <div className="flex-1 h-px bg-gradient-to-r from-red-900/50 to-transparent" />
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {mockExpeditions.map(exp => (
              <motion.button
                variants={itemVariants}
                whileHover={{ y: -5, scale: 1.02 }}
                key={exp.id}
                onClick={() => setSelectedExpedition(exp)}
                className={`text-left p-6 rounded-3xl border transition-all group relative overflow-hidden ${
                  selectedExpedition?.id === exp.id 
                    ? 'bg-gradient-to-b from-red-900/20 to-black border-red-500/50 shadow-[0_0_30px_rgba(239,68,68,0.15)]' 
                    : 'bg-[#1a120f] border-amber-900/30 hover:border-red-500/50 shadow-xl'
                }`}
              >
                {/* Decorative Elements */}
                <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${selectedExpedition?.id === exp.id ? 'from-red-500/20' : 'from-amber-900/20'} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
                <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                  <Hexagon size={120} />
                </div>

                <div className="flex justify-between items-start mb-4 relative z-10">
                  <h3 className={`text-xl font-serif font-bold transition-colors ${selectedExpedition?.id === exp.id ? 'text-red-400 drop-shadow-[0_0_5px_rgba(239,68,68,0.5)]' : 'text-amber-100 group-hover:text-red-400'}`}>{exp.name}</h3>
                  <span className={`text-[9px] uppercase tracking-[0.2em] px-3 py-1.5 rounded-xl border font-mono font-bold ${
                    exp.difficulty > 500 ? 'bg-red-900/30 text-red-400 border-red-500/50' :
                    exp.difficulty > 200 ? 'bg-amber-900/30 text-amber-400 border-amber-500/50' :
                    'bg-emerald-900/30 text-emerald-400 border-emerald-500/50'
                  }`}>
                    Difficulté {exp.difficulty}
                  </span>
                </div>
                
                <p className="text-sm text-amber-100/60 mb-6 line-clamp-2 font-serif italic relative z-10 border-l-2 border-amber-900/30 pl-3">"{exp.description}"</p>
                
                <div className="flex items-center gap-6 text-[10px] font-mono text-amber-700/60 uppercase tracking-widest relative z-10 pt-4 border-t border-amber-900/20">
                  <span className="flex items-center gap-2"><Zap size={14} className="text-amber-500" /> Min: {exp.minRealm}</span>
                  <span className="flex items-center gap-2"><Trophy size={14} className="text-emerald-400" /> {exp.rewardType}</span>
                </div>
              </motion.button>
            ))}
          </div>

          <AnimatePresence>
            {selectedExpedition && (
              <motion.div 
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-8 shadow-2xl relative overflow-hidden"
              >
                <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-30" />
                
                <div className="flex items-center justify-between relative z-10 mb-8 border-b border-amber-900/30 pb-6">
                  <div>
                    <h3 className="text-2xl font-calligraphy text-red-500 drop-shadow-md">Préparation: {selectedExpedition.name}</h3>
                    <p className="text-sm text-amber-700/70 font-mono uppercase tracking-widest mt-1">Sélection de l'Escouade</p>
                  </div>
                  <button
                    onClick={handleStart}
                    disabled={!canStart}
                    className={`flex items-center gap-2 px-8 py-4 rounded-xl text-sm font-bold uppercase tracking-widest transition-all shadow-lg ${
                      canStart
                        ? 'bg-gradient-to-r from-red-700 to-red-600 hover:from-red-600 hover:to-red-500 text-white border border-red-400/20 active:scale-95'
                        : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
                    }`}
                  >
                    <Sword size={18} />
                    <span>Lancer l'Expédition</span>
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-8 relative z-10">
                  {/* Leader Selection */}
                  <div className="space-y-4">
                    <h4 className="text-sm font-mono text-amber-500 uppercase tracking-widest flex items-center gap-2">
                      <User size={16} /> Chef d'Expédition (Requis)
                    </h4>
                    <div className="bg-black/40 border border-amber-900/30 rounded-2xl p-4 max-h-64 overflow-y-auto custom-scrollbar space-y-2 shadow-inner">
                      {livingMembers.map(member => {
                        const isLeader = leaderId === member.id;
                        const isSelected = selectedMemberIds.includes(member.id);
                        const canBeLeader = !isSelected;
                        
                        return (
                          <button
                            key={`leader-${member.id}`}
                            onClick={() => canBeLeader && setLeaderId(isLeader ? '' : member.id)}
                            disabled={!canBeLeader}
                            className={`w-full flex items-center justify-between p-3 rounded-xl border transition-all ${
                              isLeader 
                                ? 'bg-amber-900/30 border-amber-500/50 shadow-[0_0_10px_rgba(245,158,11,0.2)]' 
                                : !canBeLeader
                                ? 'bg-stone-900/20 border-stone-800/50 opacity-50 cursor-not-allowed'
                                : 'bg-[#1a120f] border-amber-900/20 hover:border-amber-500/30'
                            }`}
                          >
                            <div className="flex items-center gap-3">
                              <Avatar seed={member.id} className="w-10 h-10 rounded-lg border border-amber-900/50" />
                              <div className="text-left">
                                <p className={`font-serif font-bold ${isLeader ? 'text-amber-400' : 'text-amber-100'}`}>{member.firstName} {member.lastName}</p>
                                <p className="text-[10px] font-mono text-amber-700/70 uppercase tracking-widest">{member.realm}</p>
                              </div>
                            </div>
                            {isLeader && <div className="w-2 h-2 rounded-full bg-amber-400 shadow-[0_0_8px_rgba(251,191,36,0.8)]" />}
                          </button>
                        );
                      })}
                    </div>
                  </div>

                  {/* Members Selection */}
                  <div className="space-y-4">
                    <h4 className="text-sm font-mono text-amber-500 uppercase tracking-widest flex items-center gap-2">
                      <Users size={16} /> Escouade ({selectedMemberIds.length}/3)
                    </h4>
                    <div className="bg-black/40 border border-amber-900/30 rounded-2xl p-4 max-h-64 overflow-y-auto custom-scrollbar space-y-2 shadow-inner">
                      {livingMembers.map(member => {
                        const isSelected = selectedMemberIds.includes(member.id);
                        const isLeader = leaderId === member.id;
                        const canBeSelected = !isLeader && (isSelected || selectedMemberIds.length < 3);
                        
                        return (
                          <button
                            key={`member-${member.id}`}
                            onClick={() => canBeSelected && handleToggleMember(member.id)}
                            disabled={!canBeSelected}
                            className={`w-full flex items-center justify-between p-3 rounded-xl border transition-all ${
                              isSelected 
                                ? 'bg-red-900/20 border-red-500/50 shadow-[0_0_10px_rgba(239,68,68,0.2)]' 
                                : !canBeSelected
                                ? 'bg-stone-900/20 border-stone-800/50 opacity-50 cursor-not-allowed'
                                : 'bg-[#1a120f] border-amber-900/20 hover:border-red-500/30'
                            }`}
                          >
                            <div className="flex items-center gap-3">
                              <Avatar seed={member.id} className="w-10 h-10 rounded-lg border border-amber-900/50" />
                              <div className="text-left">
                                <p className={`font-serif font-bold ${isSelected ? 'text-red-400' : 'text-amber-100'}`}>{member.firstName} {member.lastName}</p>
                                <p className="text-[10px] font-mono text-amber-700/70 uppercase tracking-widest">{member.realm}</p>
                              </div>
                            </div>
                            {isSelected && <div className="w-2 h-2 rounded-full bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.8)]" />}
                          </button>
                        );
                      })}
                    </div>
                  </div>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Combat History Sidebar */}
        <div className="flex flex-col h-full max-h-[calc(100vh-12rem)]">
          <div className="flex items-center gap-4 mb-6 shrink-0">
            <History className="text-amber-500" size={24} />
            <h3 className="text-2xl font-calligraphy text-amber-500">Chroniques de Guerre</h3>
            <div className="flex-1 h-px bg-gradient-to-r from-amber-900/50 to-transparent" />
          </div>
          
          <div className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-6 shadow-xl relative overflow-hidden flex-1 flex flex-col min-h-[400px]">
            <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-30" />
            
            <div className="flex-1 overflow-y-auto custom-scrollbar pr-2 space-y-4 relative z-10">
              {combatEvents.length === 0 ? (
                <div className="h-full flex flex-col items-center justify-center text-amber-700/50 space-y-4">
                  <ShieldAlert size={48} className="opacity-20" />
                  <p className="font-serif italic text-center text-sm">Aucun conflit n'a encore été enregistré dans les annales du clan.</p>
                </div>
              ) : (
                combatEvents.map((event, index) => (
                  <motion.div 
                    initial={{ opacity: 0, x: 20 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: index * 0.1 }}
                    key={event.id} 
                    className={`p-4 rounded-2xl border ${
                      event.type === 'Danger' ? 'bg-red-900/10 border-red-900/30' :
                      event.type === 'Success' ? 'bg-emerald-900/10 border-emerald-900/30' :
                      'bg-amber-900/10 border-amber-900/30'
                    }`}
                  >
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-[10px] font-mono text-amber-700/70 uppercase tracking-widest">An {event.year}</span>
                      {event.type === 'Danger' && <Skull size={14} className="text-red-500" />}
                      {event.type === 'Success' && <Trophy size={14} className="text-emerald-500" />}
                      {event.type === 'Warning' && <ShieldAlert size={14} className="text-amber-500" />}
                    </div>
                    <p className={`text-sm font-serif leading-relaxed ${
                      event.type === 'Danger' ? 'text-red-200' :
                      event.type === 'Success' ? 'text-emerald-200' :
                      'text-amber-200'
                    }`}>
                      {event.message}
                    </p>
                  </motion.div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    </motion.div>
  );
};
