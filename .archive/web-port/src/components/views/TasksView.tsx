import React, { useState } from 'react';
import { Character, TaskType, Manual } from '../../types/game';
import { REALMS } from '../../constants';
import { Scroll, Pickaxe, Shield, FlaskConical, Hammer, Activity, ChevronRight, X, BookOpen, UserCog } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';
import { Avatar } from '../ui/Avatar';

interface TasksViewProps {
  members: Character[];
  manuals: Manual[];
  onAutoAssign: () => void;
  onUpdateTask: (memberId: string, task: TaskType, manualId?: string) => void;
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

export const TasksView: React.FC<TasksViewProps> = ({ members, manuals, onAutoAssign, onUpdateTask }) => {
  const [reassigningMember, setReassigningMember] = useState<Character | null>(null);
  const [selectingManualFor, setSelectingManualFor] = useState<Character | null>(null);

  const tasks = [
    { id: 'Culture' as TaskType, icon: Activity, color: 'text-purple-400', bg: 'bg-purple-900/20', border: 'border-purple-900/30', skill: 'combat' as const },
    { id: 'Étude' as TaskType, icon: BookOpen, color: 'text-amber-400', bg: 'bg-amber-900/20', border: 'border-amber-900/30', skill: null },
    { id: 'Récolte' as TaskType, icon: Pickaxe, color: 'text-emerald-400', bg: 'bg-emerald-900/20', border: 'border-emerald-900/30', skill: 'farming' as const },
    { id: 'Patrouille' as TaskType, icon: Shield, color: 'text-red-400', bg: 'bg-red-900/20', border: 'border-red-900/30', skill: 'combat' as const },
    { id: 'Alchimie' as TaskType, icon: FlaskConical, color: 'text-blue-400', bg: 'bg-blue-900/20', border: 'border-blue-900/30', skill: 'alchemy' as const },
    { id: 'Forge' as TaskType, icon: Hammer, color: 'text-orange-400', bg: 'bg-orange-900/20', border: 'border-orange-900/30', skill: 'forging' as const },
    { id: 'Repos' as TaskType, icon: Scroll, color: 'text-stone-400', bg: 'bg-stone-900/20', border: 'border-stone-900/30', skill: null },
  ];

  const handleReassign = (task: TaskType) => {
    if (reassigningMember) {
      if (task === 'Étude') {
        setSelectingManualFor(reassigningMember);
        setReassigningMember(null);
      } else {
        onUpdateTask(reassigningMember.id, task);
        setReassigningMember(null);
      }
    }
  };

  const handleManualSelect = (manualId: string) => {
    if (selectingManualFor) {
      onUpdateTask(selectingManualFor.id, 'Étude', manualId);
      setSelectingManualFor(null);
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
            <UserCog className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Décrets du Clan</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Assignation des Tâches</p>
          </div>
        </div>
        
        <button 
          onClick={onAutoAssign}
          className="relative z-10 bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white px-8 py-4 rounded-xl font-serif font-bold text-lg transition-all shadow-lg active:scale-95 border border-amber-400/20 flex items-center gap-3"
        >
          <Scroll size={20} />
          Auto-Assignation
        </button>
      </motion.div>

      {/* Tasks Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-8 relative z-10">
        {tasks.map(task => {
          const assignedMembers = members.filter(m => m.isAlive && m.currentTask === task.id);
          const Icon = task.icon;

          return (
            <motion.div 
              key={task.id} 
              variants={itemVariants}
              className={`bg-[#1a120f] border rounded-3xl p-6 shadow-xl relative overflow-hidden group transition-all ${task.border} hover:border-amber-500/50`}
            >
              {/* Decorative Background */}
              <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${task.bg} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
              <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                <Icon size={150} />
              </div>

              <div className="flex items-center justify-between mb-6 pb-4 border-b border-amber-900/20 relative z-10">
                <div className="flex items-center gap-4">
                  <div className={`w-12 h-12 rounded-2xl bg-black/50 border ${task.border} shadow-inner flex items-center justify-center`}>
                    <Icon size={24} className={task.color} />
                  </div>
                  <h3 className="text-2xl font-calligraphy text-amber-100">{task.id}</h3>
                </div>
                <div className="bg-black/40 px-3 py-1.5 rounded-lg border border-amber-900/30 flex items-center gap-2">
                  <span className="text-lg font-mono text-amber-500">{assignedMembers.length}</span>
                  <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono">Disciples</span>
                </div>
              </div>

              <div className="space-y-3 relative z-10 min-h-[150px]">
                {assignedMembers.length > 0 ? (
                  <AnimatePresence>
                    {assignedMembers.map(member => (
                      <motion.div 
                        key={member.id} 
                        initial={{ opacity: 0, x: -20 }}
                        animate={{ opacity: 1, x: 0 }}
                        exit={{ opacity: 0, scale: 0.9 }}
                        className="flex items-center justify-between p-3 bg-black/30 rounded-xl border border-amber-900/20 hover:border-amber-500/30 transition-all group/item"
                      >
                        <div className="flex items-center gap-3">
                          <Avatar 
                            seed={member.portraitSeed} 
                            name={member.firstName} 
                            realm={member.realm}
                            size="sm"
                            className="border border-amber-900/50"
                          />
                          <div>
                            <p className="text-sm font-serif font-bold text-amber-100/90 group-hover/item:text-amber-400 transition-colors">
                              {member.lastName} {member.firstName}
                            </p>
                            <div className="flex items-center gap-2 mt-0.5">
                              <span className="text-[9px] font-mono text-amber-700/70 uppercase tracking-widest">
                                {member.realm}
                              </span>
                              {task.id === 'Étude' && member.assignedManualId && (
                                <span className="text-[9px] font-mono text-amber-500/70 uppercase tracking-widest bg-amber-900/20 px-1.5 py-0.5 rounded border border-amber-900/30">
                                  {manuals.find(m => m.id === member.assignedManualId)?.name || 'Manuel'}
                                </span>
                              )}
                            </div>
                          </div>
                        </div>
                        
                        <button 
                          onClick={() => setReassigningMember(member)}
                          className="p-2 rounded-lg bg-amber-900/20 text-amber-500 hover:bg-amber-500 hover:text-white transition-colors border border-amber-900/30 opacity-0 group-hover/item:opacity-100"
                          title="Réassigner"
                        >
                          <ChevronRight size={16} />
                        </button>
                      </motion.div>
                    ))}
                  </AnimatePresence>
                ) : (
                  <div className="h-full flex items-center justify-center">
                    <p className="text-sm font-serif italic text-amber-700/40 text-center">
                      Aucun disciple assigné
                    </p>
                  </div>
                )}
              </div>
            </motion.div>
          );
        })}
      </div>

      {/* Reassign Modal */}
      <AnimatePresence>
        {reassigningMember && (
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
              className="bg-[#1a120f] border border-amber-900/50 rounded-3xl p-8 max-w-md w-full shadow-2xl relative"
            >
              <button 
                onClick={() => setReassigningMember(null)}
                className="absolute top-6 right-6 text-amber-700/50 hover:text-amber-500 transition-colors"
              >
                <X size={24} />
              </button>
              
              <h3 className="text-3xl font-calligraphy text-amber-500 mb-2">Nouveau Décret</h3>
              <p className="text-sm text-amber-100/60 font-serif mb-8">
                Assigner une nouvelle tâche à <span className="text-amber-400 font-bold">{reassigningMember.lastName} {reassigningMember.firstName}</span>
              </p>
              
              <div className="grid grid-cols-2 gap-3">
                {tasks.map(task => {
                  const Icon = task.icon;
                  return (
                    <button
                      key={task.id}
                      onClick={() => handleReassign(task.id)}
                      className={`p-4 rounded-xl border flex flex-col items-center gap-3 transition-all ${
                        reassigningMember.currentTask === task.id
                          ? 'bg-amber-900/40 border-amber-500 text-amber-400'
                          : 'bg-black/40 border-amber-900/30 text-amber-100/70 hover:bg-amber-900/20 hover:border-amber-500/50 hover:text-amber-400'
                      }`}
                    >
                      <Icon size={24} className={reassigningMember.currentTask === task.id ? task.color : 'opacity-70'} />
                      <span className="font-serif font-bold">{task.id}</span>
                    </button>
                  );
                })}
              </div>
            </motion.div>
          </motion.div>
        )}

        {/* Manual Selection Modal */}
        {selectingManualFor && (
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
                onClick={() => setSelectingManualFor(null)}
                className="absolute top-6 right-6 text-amber-700/50 hover:text-amber-500 transition-colors"
              >
                <X size={24} />
              </button>
              
              <h3 className="text-3xl font-calligraphy text-amber-500 mb-2">Pavillon des Écritures</h3>
              <p className="text-sm text-amber-100/60 font-serif mb-8">
                Choisir un manuel pour <span className="text-amber-400 font-bold">{selectingManualFor.lastName} {selectingManualFor.firstName}</span>
              </p>
              
              <div className="overflow-y-auto pr-2 custom-scrollbar space-y-3">
                {manuals.length === 0 ? (
                  <p className="text-center text-amber-700/50 font-serif italic py-8">
                    La bibliothèque de la secte est vide.
                  </p>
                ) : (
                  manuals.map(manual => (
                    <button
                      key={manual.id}
                      onClick={() => handleManualSelect(manual.id)}
                      className="w-full text-left p-4 rounded-xl border border-amber-900/30 bg-black/40 hover:bg-amber-900/20 hover:border-amber-500/50 transition-all flex items-center gap-4 group"
                    >
                      <div className="w-12 h-12 rounded-lg bg-amber-900/30 border border-amber-500/30 flex items-center justify-center shrink-0">
                        <BookOpen size={20} className="text-amber-500" />
                      </div>
                      <div className="flex-1">
                        <h4 className="font-serif font-bold text-amber-100 group-hover:text-amber-400 transition-colors">{manual.name}</h4>
                        <p className="text-xs text-amber-100/60 mt-1">{manual.description}</p>
                      </div>
                      <div className="text-right">
                        <span className="text-[10px] uppercase tracking-widest font-mono text-amber-700/70 block mb-1">Multiplicateur</span>
                        <span className="text-sm font-bold text-jade-400">x{manual.multiplier}</span>
                      </div>
                    </button>
                  ))
                )}
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};
