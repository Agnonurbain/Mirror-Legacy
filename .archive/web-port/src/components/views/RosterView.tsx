import React, { useState, useMemo } from 'react';
import { Character, Item, TaskType, Manual } from '../../types/game';
import { Search, Filter, Shield, Activity, Leaf, User, Heart, ArrowUpDown, BookOpen, ScrollText } from 'lucide-react';
import { CharacterModal } from '../CharacterModal';
import { Avatar } from '../ui/Avatar';
import { motion, AnimatePresence } from 'motion/react';
import { REALMS } from '../../constants';
import { Tooltip } from '../ui/Tooltip';

interface RosterViewProps {
  members: Character[];
  onUseItem: (memberId: string, itemId: string) => void;
  onUpdateTask: (memberId: string, task: TaskType, manualId?: string) => void;
  onUseDivinePower: (powerId: string, targetId: string) => void;
  onArrangeMarriage: (memberId: string) => void;
  inventory: Item[];
  manuals: Manual[];
  mirrorPower: number;
}

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.05 }
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

export const RosterView: React.FC<RosterViewProps> = ({ members, onUseItem, onUpdateTask, onUseDivinePower, onArrangeMarriage, inventory, manuals, mirrorPower }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRealm, setFilterRealm] = useState('All');
  const [filterStatus, setFilterStatus] = useState<'all' | 'alive' | 'dead'>('alive');
  const [sortBy, setSortBy] = useState<'realm' | 'age' | 'root' | 'name'>('realm');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [selectedMember, setSelectedMember] = useState<Character | null>(null);

  const filteredMembers = useMemo(() => {
    let result = members.filter(m => {
      const matchesSearch = `${m.lastName} ${m.firstName}`.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesRealm = filterRealm === 'All' || m.realm === filterRealm;
      const matchesStatus = filterStatus === 'all' || (filterStatus === 'alive' ? m.isAlive : !m.isAlive);
      return matchesSearch && matchesRealm && matchesStatus;
    });

    result.sort((a, b) => {
      let comparison = 0;
      switch (sortBy) {
        case 'realm':
          comparison = REALMS.indexOf(a.realm) - REALMS.indexOf(b.realm);
          if (comparison === 0) {
            comparison = a.breakthroughProgress - b.breakthroughProgress;
          }
          break;
        case 'age':
          comparison = a.age - b.age;
          break;
        case 'root':
          comparison = a.spiritualRoot - b.spiritualRoot;
          break;
        case 'name':
          comparison = `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`);
          break;
      }
      return sortOrder === 'asc' ? comparison : -comparison;
    });

    return result;
  }, [members, searchTerm, filterRealm, filterStatus, sortBy, sortOrder]);

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen"
    >
      {/* Header Section */}
      <div className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-6 md:p-8 shadow-xl mb-8 relative overflow-hidden">
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-50" />
        
        <div className="relative z-10 flex flex-col xl:flex-row justify-between items-start xl:items-center gap-8">
          <div className="flex items-center gap-6">
            <div className="w-16 h-16 rounded-full bg-amber-900/20 flex items-center justify-center border border-amber-500/30 shadow-[0_0_15px_rgba(217,119,6,0.2)]">
              <ScrollText size={32} className="text-amber-500" />
            </div>
            <div>
              <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Registre du Clan</h2>
              <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Liste des Cultivateurs</p>
            </div>
          </div>
          
          <div className="flex flex-col md:flex-row gap-4 w-full xl:w-auto">
            <div className="flex flex-1 md:flex-none items-center gap-4">
              <div className="relative flex-1 md:w-64">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-amber-700/50" size={16} />
                <input
                  type="text"
                  placeholder="Rechercher un disciple..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full bg-black/50 border border-amber-900/50 rounded-full py-2.5 pl-12 pr-6 text-sm font-mono text-amber-500 placeholder:text-amber-700/30 focus:outline-none focus:border-amber-500/50 transition-colors shadow-inner"
                />
              </div>
              
              <div className="relative">
                <Filter className="absolute left-4 top-1/2 -translate-y-1/2 text-amber-700/50" size={16} />
                <select
                  value={filterRealm}
                  onChange={(e) => setFilterRealm(e.target.value)}
                  className="appearance-none bg-black/50 border border-amber-900/50 rounded-full py-2.5 pl-12 pr-10 text-sm font-mono text-amber-500 focus:outline-none focus:border-amber-500/50 transition-colors cursor-pointer min-w-[160px] shadow-inner"
                >
                  <option value="All" className="bg-[#140d0a]">Tous Royaumes</option>
                  {REALMS.map(realm => (
                    <option key={realm} value={realm} className="bg-[#140d0a]">{realm}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex items-center gap-4 justify-between md:justify-end">
              <div className="flex bg-black/50 rounded-full p-1 border border-amber-900/50 shadow-inner">
                <button 
                  onClick={() => setFilterStatus('alive')}
                  className={`px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-widest transition-all ${filterStatus === 'alive' ? 'bg-jade-900/40 text-jade-400 shadow-md' : 'text-amber-700/50 hover:text-amber-500'}`}
                >
                  Vivants
                </button>
                <button 
                  onClick={() => setFilterStatus('dead')}
                  className={`px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-widest transition-all ${filterStatus === 'dead' ? 'bg-red-900/40 text-red-400 shadow-md' : 'text-amber-700/50 hover:text-amber-500'}`}
                >
                  Défunts
                </button>
                <button 
                  onClick={() => setFilterStatus('all')}
                  className={`px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-widest transition-all ${filterStatus === 'all' ? 'bg-amber-900/40 text-amber-400 shadow-md' : 'text-amber-700/50 hover:text-amber-500'}`}
                >
                  Tous
                </button>
              </div>

              <div className="flex items-center gap-2 bg-black/50 rounded-full p-1 border border-amber-900/50 shadow-inner">
                <select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value as any)}
                  className="appearance-none bg-transparent py-1.5 pl-4 pr-2 text-xs font-bold uppercase tracking-widest text-amber-500 focus:outline-none cursor-pointer"
                >
                  <option value="realm" className="bg-[#140d0a]">Trier par Royaume</option>
                  <option value="root" className="bg-[#140d0a]">Trier par Racine</option>
                  <option value="age" className="bg-[#140d0a]">Trier par Âge</option>
                  <option value="name" className="bg-[#140d0a]">Trier par Nom</option>
                </select>
                <button 
                  onClick={() => setSortOrder(prev => prev === 'asc' ? 'desc' : 'asc')}
                  className="p-1.5 rounded-full hover:bg-amber-900/30 transition-colors text-amber-500"
                  title={sortOrder === 'asc' ? 'Croissant' : 'Décroissant'}
                >
                  <ArrowUpDown size={14} className={sortOrder === 'desc' ? 'text-amber-400' : 'opacity-50'} />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Grid Section */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 gap-6">
        <AnimatePresence mode="popLayout">
          {filteredMembers.map(member => (
            <motion.div 
              key={member.id} 
              layout
              variants={itemVariants}
              initial="hidden"
              animate="visible"
              exit="hidden"
              whileHover={{ y: -4, scale: 1.02 }}
              onClick={() => setSelectedMember(member)}
              className={`bg-[#1a120f] border border-amber-900/30 rounded-2xl p-6 relative overflow-hidden group hover:border-amber-500/50 transition-all cursor-pointer shadow-lg ${!member.isAlive ? 'opacity-70 grayscale' : ''}`}
            >
              {/* Card Background Texture */}
              <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
              
              {!member.isAlive && (
                <div className="absolute top-4 right-4 vertical-text text-[10px] uppercase tracking-[0.5em] text-red-500/50 font-mono font-bold">
                  Défunt
                </div>
              )}
              
              <div className="flex items-start gap-5 relative z-10">
                <div className="relative">
                  <Avatar 
                    seed={member.portraitSeed} 
                    name={member.firstName} 
                    realm={member.realm}
                    size="lg"
                    className={`border-2 ${member.isAlive ? 'border-amber-900/50' : 'border-stone-800'} shadow-md`}
                  />
                  {member.isAlive && (
                    <div className="absolute -bottom-2 -right-2 w-6 h-6 rounded-full bg-[#1a120f] border border-amber-900/50 flex items-center justify-center">
                      <div className={`w-3 h-3 rounded-full ${
                        member.currentTask === 'Culture' ? 'bg-gold-500 shadow-[0_0_8px_rgba(245,158,11,0.6)]' :
                        member.currentTask === 'Patrouille' ? 'bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.6)]' :
                        member.currentTask === 'Repos' ? 'bg-stone-500' :
                        'bg-jade-500 shadow-[0_0_8px_rgba(34,197,94,0.6)]'
                      }`} />
                    </div>
                  )}
                </div>
                
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className={`text-xl font-calligraphy truncate ${
                      member.isAlive ? 'text-amber-100 group-hover:text-amber-400' : 'text-stone-500 line-through'
                    } transition-colors`}>
                      {member.lastName} {member.firstName}
                    </h3>
                    {member.spouse && <Heart size={14} className="text-red-500/60 fill-red-500/20" />}
                  </div>
                  
                  <p className="text-[10px] text-amber-700/60 uppercase tracking-widest mb-3 font-mono">
                    Gen {member.generation} • {member.gender === 'Male' ? 'Yang ♂' : 'Yin ♀'} • {member.age} ans
                  </p>
                  
                  <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-amber-900/20 border border-amber-900/30">
                    <Activity size={12} className={member.isAlive ? "text-amber-500" : "text-stone-500"} />
                    <span className={`text-[10px] font-bold uppercase tracking-widest ${member.isAlive ? "text-amber-400" : "text-stone-500"}`}>
                      {member.realm}
                    </span>
                  </div>
                  
                  {member.isAlive && (
                    <div className="mt-4 space-y-1.5">
                      <div className="flex justify-between text-[9px] uppercase tracking-[0.2em] text-amber-700/70 font-mono">
                        <span>Percée</span>
                        <span className="text-amber-500">{Math.floor(member.breakthroughProgress)}%</span>
                      </div>
                      <div className="h-1 bg-black/50 rounded-full overflow-hidden border border-amber-900/30">
                        <motion.div 
                          initial={{ width: 0 }}
                          animate={{ width: `${member.breakthroughProgress}%` }}
                          className="h-full bg-gradient-to-r from-amber-600 to-amber-400 shadow-[0_0_10px_rgba(245,158,11,0.5)] transition-all duration-1000"
                        />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="mt-6 grid grid-cols-2 gap-3 relative z-10">
                <Tooltip content={
                  <div className="space-y-1">
                    <p className="font-bold text-jade-400">Racine Spirituelle</p>
                    <p className="text-stone-400 text-xs">Détermine la vitesse d'absorption du Qi et les chances de percée.</p>
                  </div>
                } className="w-full">
                  <div className="w-full p-2.5 border border-amber-900/20 rounded-xl bg-black/20 hover:bg-black/40 transition-colors flex flex-col items-center justify-center">
                    <div className="flex items-center gap-1.5 mb-1 text-amber-700/60">
                      <Leaf size={12} className="text-jade-500" />
                      <span className="text-[9px] uppercase tracking-widest font-mono">Racine</span>
                    </div>
                    <span className="text-sm font-mono text-amber-100/80">{member.spiritualRoot}</span>
                  </div>
                </Tooltip>
                
                <Tooltip content={
                  <div className="space-y-1">
                    <p className="font-bold text-blue-400">Stabilité Mentale</p>
                    <p className="text-stone-400 text-xs">L'état d'esprit du disciple. Une faible stabilité augmente les risques d'échec.</p>
                  </div>
                } className="w-full">
                  <div className="w-full p-2.5 border border-amber-900/20 rounded-xl bg-black/20 hover:bg-black/40 transition-colors flex flex-col items-center justify-center">
                    <div className="flex items-center gap-1.5 mb-1 text-amber-700/60">
                      <Shield size={12} className="text-blue-400" />
                      <span className="text-[9px] uppercase tracking-widest font-mono">Esprit</span>
                    </div>
                    <span className="text-sm font-mono text-amber-100/80">{member.mentalStability}</span>
                  </div>
                </Tooltip>
              </div>

              <div className="mt-5 pt-4 border-t border-amber-900/20 flex items-center justify-between relative z-10">
                <span className="text-[9px] text-amber-700/50 uppercase tracking-[0.2em] font-mono">Tâche Actuelle</span>
                <span className={`text-[10px] font-bold px-4 py-1.5 rounded-lg border font-mono uppercase tracking-wider ${
                  member.currentTask === 'Culture' ? 'bg-amber-900/20 border-amber-500/50 text-amber-400' :
                  member.currentTask === 'Patrouille' ? 'bg-red-900/20 border-red-500/50 text-red-400' :
                  member.currentTask === 'Repos' ? 'bg-stone-900/20 border-stone-500/50 text-stone-400' :
                  'bg-jade-900/20 border-jade-500/50 text-jade-400'
                }`}>
                  {member.currentTask}
                </span>
              </div>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>

      {selectedMember && (
        <CharacterModal 
          member={selectedMember} 
          onClose={() => setSelectedMember(null)} 
          allMembers={members}
          onUseItem={onUseItem}
          onUpdateTask={onUpdateTask}
          onUseDivinePower={onUseDivinePower}
          onArrangeMarriage={onArrangeMarriage}
          inventory={inventory}
          manuals={manuals}
          mirrorPower={mirrorPower}
        />
      )}
    </motion.div>
  );
};
