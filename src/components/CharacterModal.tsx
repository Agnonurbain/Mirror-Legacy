import React from 'react';
import { Character, Item, TaskType, Manual } from '../types/game';
import { REALMS, TASKS } from '../constants';
import { X, Heart, Shield, Leaf, Activity, User, Users, Package, Zap, Scroll, Info, BookOpen, Hexagon } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';
import { Avatar } from './ui/Avatar';
import { Tooltip } from './ui/Tooltip';

const TRAIT_DESCRIPTIONS: Record<string, string> = {
  'Génie': '+50% Vitesse de Culture',
  'Diligent': '+20% Efficacité des Tâches',
  'Brave': '+20% Puissance de Combat',
  'Calme': '+10% Chance de Percée',
  'Maître Alchimiste': '+50% Gain de Compétence Alchimie',
  'Chanceux': '+5% Événements Positifs',
  'Inflexible': '+10% Stabilité Mentale',
  'Curieux': '+10% Gain de Compétence Global',
  'Légendaire': '+100% Vitesse de Culture & Destinée',
};

interface CharacterModalProps {
  member: Character;
  onClose: () => void;
  allMembers: Character[];
  onUseItem?: (memberId: string, itemId: string) => void;
  onUpdateTask?: (memberId: string, task: TaskType, manualId?: string) => void;
  onUseDivinePower?: (powerId: string, targetId: string) => void;
  onArrangeMarriage?: (memberId: string) => void;
  inventory?: Item[];
  manuals?: Manual[];
  mirrorPower?: number;
}

export const CharacterModal: React.FC<CharacterModalProps> = ({ member, onClose, allMembers, onUseItem, onUpdateTask, onUseDivinePower, onArrangeMarriage, inventory, manuals, mirrorPower = 0 }) => {
  const [selectingManual, setSelectingManual] = React.useState(false);
  const parents = allMembers.filter(m => member.parents.includes(m.id));
  const children = allMembers.filter(m => member.children.includes(m.id));
  const spouse = allMembers.find(m => m.id === member.spouse);

  const usableItems = inventory?.filter(i => i.type === 'Pill') || [];

  return (
    <motion.div 
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-ink/90 backdrop-blur-md"
      onClick={onClose}
    >
      <motion.div 
        initial={{ scale: 0.9, opacity: 0, y: 20 }}
        animate={{ scale: 1, opacity: 1, y: 0 }}
        exit={{ scale: 0.9, opacity: 0, y: 20 }}
        className="bg-ink border border-paper/10 rounded-[3rem] w-full max-w-3xl overflow-hidden shadow-2xl relative bagua-bg max-h-[90vh] overflow-y-auto custom-scrollbar"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Qi Particles */}
        {[...Array(15)].map((_, i) => (
          <motion.div 
            key={i} 
            initial={{ y: 0, opacity: 0 }}
            animate={{ 
              y: -800, 
              opacity: [0, 0.3, 0],
              x: (Math.random() - 0.5) * 200
            }}
            transition={{ 
              duration: 4 + Math.random() * 4, 
              repeat: Infinity,
              delay: Math.random() * 4
            }}
            className="absolute bottom-0 w-1 h-1 rounded-full pointer-events-none"
            style={{ 
              left: `${Math.random() * 100}%`,
              background: i % 2 === 0 ? '#4ade80' : '#fbbf24',
            }}
          />
        ))}
        
        {/* Decorative background elements */}
        <div className="absolute top-0 left-0 w-full h-32 bg-gradient-to-b from-paper/5 to-transparent" />
        <div className="absolute top-12 right-12 vertical-text text-[10px] tracking-[0.8em] opacity-10 uppercase font-sans">
          Fiche de Culture
        </div>

        <div className="p-12 relative">
          <button 
            onClick={onClose}
            className="absolute top-8 right-8 p-3 bg-paper/5 hover:bg-paper/10 opacity-40 hover:opacity-100 transition-all z-50 rounded-full"
          >
            <X size={18} />
          </button>

          <div className="flex flex-col md:flex-row gap-12">
            {/* Portrait Section */}
            <div className="flex flex-col items-center space-y-6">
              <Avatar 
                seed={member.portraitSeed} 
                name={member.firstName} 
                realm={member.realm}
                size="xl"
                className={!member.isAlive ? 'grayscale sepia-[.3] opacity-80' : ''}
              />
              
              <div className="text-center">
                <div className="jade-text font-calligraphy text-2xl mb-1">{member.realm}</div>
                <div className="text-[10px] uppercase tracking-[0.3em] opacity-30">Royaume Actuel</div>
              </div>

              {member.isAlive && (
                <div className="w-full space-y-2">
                  <div className="flex justify-between text-[10px] uppercase tracking-widest opacity-40">
                    <span>Ascension</span>
                    <span className="gold-text">{Math.floor(member.breakthroughProgress)}%</span>
                  </div>
                  <div className="h-1 bg-paper/5 rounded-full overflow-hidden">
                    <motion.div 
                      initial={{ width: 0 }}
                      animate={{ width: `${member.breakthroughProgress}%` }}
                      className="h-full bg-gold-500 shadow-[0_0_10px_rgba(245,158,11,0.5)] transition-all duration-1000"
                    />
                  </div>
                </div>
              )}
            </div>

            {/* Info Section */}
            <div className="flex-1 space-y-8">
              <div>
                <h2 className="text-5xl font-calligraphy text-gold-500 mb-2">
                  {member.lastName} {member.firstName}
                </h2>
                <p className="opacity-40 font-serif italic">
                  Génération {member.generation} • {member.gender === 'Male' ? 'Yang (♂)' : 'Yin (♀)'} • {member.isAlive ? `Âge ${member.age}/${member.maxLifespan}` : member.gender === 'Male' ? 'Décédé' : 'Décédée'}
                </p>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <Tooltip content={
                  <div className="space-y-1">
                    <p className="font-bold text-jade-400">Racine Spirituelle</p>
                    <p className="text-stone-400">Détermine la vitesse d'absorption du Qi et les chances de percée. Une racine élevée accélère grandement la culture.</p>
                  </div>
                } position="bottom">
                  <div className="paper-card p-4 text-center h-full cursor-help">
                    <Leaf size={14} className="jade-text mx-auto mb-2 opacity-50" />
                    <div className="text-xl font-mono font-bold jade-text">{member.spiritualRoot}</div>
                    <div className="text-[8px] uppercase tracking-widest opacity-20">Racine</div>
                  </div>
                </Tooltip>

                <Tooltip content={
                  <div className="space-y-1">
                    <p className="font-bold text-blue-400">Stabilité Mentale</p>
                    <p className="text-stone-400">L'état d'esprit du disciple. Une faible stabilité augmente les risques d'échec lors des percées et peut mener à la folie.</p>
                  </div>
                } position="bottom">
                  <div className="paper-card p-4 text-center h-full cursor-help">
                    <Shield size={14} className="text-blue-400 mx-auto mb-2 opacity-50" />
                    <div className="text-xl font-mono font-bold text-blue-400">{member.mentalStability}</div>
                    <div className="text-[8px] uppercase tracking-widest opacity-20">Stabilité</div>
                  </div>
                </Tooltip>

                <Tooltip content={
                  <div className="space-y-1">
                    <p className="font-bold text-cinnabar">Vitalité (Espérance de vie)</p>
                    <p className="text-stone-400">Le nombre d'années restantes avant que le disciple ne décède de vieillesse. Augmente avec chaque percée majeure.</p>
                  </div>
                } position="bottom">
                  <div className="paper-card p-4 text-center h-full cursor-help">
                    <Heart size={14} className="text-cinnabar mx-auto mb-2 opacity-50" />
                    <div className="text-xl font-mono font-bold text-cinnabar">{Math.max(0, member.maxLifespan - member.age)}</div>
                    <div className="text-[8px] uppercase tracking-widest opacity-20">Vitalité</div>
                  </div>
                </Tooltip>
              </div>

              {/* Backstory */}
              {member.backstory && (
                <div className="space-y-3">
                  <h4 className="text-[10px] uppercase tracking-[0.3em] opacity-30 font-bold">Histoire d'Origine</h4>
                  <p className="text-sm text-stone-300 italic leading-relaxed border-l-2 border-gold-500/20 pl-4 py-1">
                    "{member.backstory}"
                  </p>
                </div>
              )}

              {/* Traits */}
              {member.traits && member.traits.length > 0 && (
                <div className="space-y-3">
                  <h4 className="text-[10px] uppercase tracking-[0.3em] opacity-30 font-bold">Traits de Caractère</h4>
                  <div className="flex flex-wrap gap-3">
                    {member.traits.map((trait, idx) => (
                      <Tooltip key={idx} content={TRAIT_DESCRIPTIONS[trait] || 'Effet mystérieux...'} position="bottom">
                        <span className="px-3 py-1 bg-gold-500/5 text-gold-400 border border-gold-500/20 rounded-full text-[10px] font-medium cursor-help flex items-center gap-2">
                          {trait}
                          <Info size={10} className="opacity-30" />
                        </span>
                      </Tooltip>
                    ))}
                  </div>
                </div>
              )}

              {/* Skills & Manuals */}
              <div className="grid grid-cols-2 gap-8">
                <div className="space-y-4">
                  <h4 className="text-[10px] uppercase tracking-[0.3em] opacity-30 font-bold">Compétences de Voie</h4>
                  <div className="grid grid-cols-1 gap-4">
                    {[
                      { label: 'Alchimie', value: member.skills?.alchemy || 0, color: 'bg-jade-500' },
                      { label: 'Forge', value: member.skills?.forging || 0, color: 'bg-orange-500' },
                      { label: 'Agriculture', value: member.skills?.farming || 0, color: 'bg-emerald-500' },
                      { label: 'Combat', value: member.skills?.combat || 0, color: 'bg-cinnabar' },
                    ].map(skill => (
                      <div key={skill.label} className="space-y-2">
                        <div className="flex justify-between text-[10px] tracking-widest">
                          <span className="opacity-40">{skill.label}</span>
                          <span className="opacity-60 font-mono">{Math.floor(skill.value)}</span>
                        </div>
                        <div className="h-0.5 bg-paper/5 rounded-full overflow-hidden">
                          <div 
                            className={`h-full ${skill.color} opacity-60 transition-all duration-1000`}
                            style={{ width: `${skill.value}%` }}
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="space-y-4">
                  <h4 className="text-[10px] uppercase tracking-[0.3em] opacity-30 font-bold">Manuels Maîtrisés</h4>
                  <div className="flex flex-wrap gap-2">
                    {member.studiedManuals && member.studiedManuals.length > 0 ? (
                      member.studiedManuals.map(manualId => {
                        const manual = manuals?.find(m => m.id === manualId);
                        return (
                          <div key={manualId} className="px-3 py-1 bg-gold-500/10 border border-gold-500/20 rounded-lg text-[10px] text-gold-500 flex items-center gap-2">
                            <BookOpen size={10} />
                            {manual?.name || 'Manuel Inconnu'}
                          </div>
                        );
                      })
                    ) : (
                      <p className="text-[10px] opacity-20 italic">Aucun manuel maîtrisé</p>
                    )}
                  </div>
                </div>
              </div>

              {/* Family Section */}
              <div className="pt-6 border-t border-paper/5">
                <h4 className="text-[10px] uppercase tracking-[0.3em] opacity-30 font-bold mb-4 flex items-center gap-2">
                  <Heart size={12} />
                  Famille & Liens
                </h4>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <span className="text-[9px] uppercase tracking-widest opacity-40">Conjoint(e)</span>
                    {spouse ? (
                      <div className="flex items-center gap-2 p-2 bg-paper/5 rounded-lg border border-paper/10">
                        <Avatar seed={spouse.portraitSeed} name={spouse.firstName} realm={spouse.realm} size="sm" />
                        <div className="flex flex-col">
                          <span className="text-xs font-bold text-paper">{spouse.lastName} {spouse.firstName}</span>
                          <span className="text-[9px] opacity-50">{spouse.realm}</span>
                        </div>
                      </div>
                    ) : (
                      <div className="flex flex-col gap-2">
                        <span className="text-xs italic opacity-30">Célibataire</span>
                        {member.isAlive && member.age >= 18 && onArrangeMarriage && (
                          <button
                            onClick={() => onArrangeMarriage(member.id)}
                            className="px-3 py-1.5 bg-gold-500/10 text-gold-500 border border-gold-500/20 rounded-lg text-[10px] uppercase tracking-widest hover:bg-gold-500/20 transition-colors"
                          >
                            Arranger un Mariage
                          </button>
                        )}
                      </div>
                    )}
                  </div>
                  <div className="space-y-2">
                    <span className="text-[9px] uppercase tracking-widest opacity-40">Enfants ({children.length})</span>
                    {children.length > 0 ? (
                      <div className="flex flex-wrap gap-1">
                        {children.slice(0, 4).map(child => (
                          <div key={child.id} className="relative group/child">
                            <Avatar seed={child.portraitSeed} name={child.firstName} realm={child.realm} size="sm" />
                            <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 w-max px-2 py-1 bg-ink border border-paper/10 rounded text-[9px] opacity-0 group-hover/child:opacity-100 transition-opacity pointer-events-none z-50">
                              {child.firstName}
                            </div>
                          </div>
                        ))}
                        {children.length > 4 && (
                          <div className="w-8 h-8 rounded-full bg-paper/5 border border-paper/10 flex items-center justify-center text-[10px] opacity-50">
                            +{children.length - 4}
                          </div>
                        )}
                      </div>
                    ) : (
                      <span className="text-xs italic opacity-30 block">Aucun enfant</span>
                    )}
                  </div>
                </div>
              </div>

              {/* Tasks Selection */}
              {member.isAlive && (
                <div className="pt-6 border-t border-paper/5">
                  <div className="flex flex-wrap gap-2">
                    {TASKS.map(task => (
                      <button
                        key={task}
                        onClick={() => {
                          if (task === 'Étude') {
                            setSelectingManual(true);
                          } else {
                            onUpdateTask?.(member.id, task);
                          }
                        }}
                        className={`px-4 py-2 rounded-full text-[10px] font-bold uppercase tracking-widest transition-all border ${
                          member.currentTask === task
                            ? 'bg-gold-500 border-gold-400 text-ink shadow-[0_0_15px_rgba(245,158,11,0.3)]'
                            : 'bg-paper/5 border-paper/10 opacity-40 hover:opacity-80 hover:border-paper/20'
                        }`}
                      >
                        {task}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* Divine Intervention */}
              {member.isAlive && onUseDivinePower && (
                <div className="pt-6 border-t border-cyan-500/10">
                  <h4 className="text-[10px] uppercase tracking-[0.3em] font-bold text-cyan-400 mb-4 flex items-center gap-2">
                    <Hexagon size={12} />
                    Intervention Divine
                  </h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => onUseDivinePower('heal', member.id)}
                      disabled={mirrorPower < 20}
                      className="p-3 bg-cyan-900/10 border border-cyan-500/20 rounded-xl hover:bg-cyan-500/10 hover:border-cyan-400/50 transition-all text-left group disabled:opacity-30 disabled:cursor-not-allowed"
                    >
                      <div className="flex justify-between items-center mb-1">
                        <span className="font-serif font-bold text-cyan-300 group-hover:text-cyan-200">Guérison Spirituelle</span>
                        <span className="text-[10px] font-mono text-cyan-400/70">20 MP</span>
                      </div>
                      <p className="text-[9px] opacity-60 text-cyan-100/60">Restaure la stabilité mentale et booste la percée.</p>
                    </button>

                    <button
                      onClick={() => onUseDivinePower('protect', member.id)}
                      disabled={mirrorPower < 50 || member.isProtectedByMirror}
                      className="p-3 bg-cyan-900/10 border border-cyan-500/20 rounded-xl hover:bg-cyan-500/10 hover:border-cyan-400/50 transition-all text-left group disabled:opacity-30 disabled:cursor-not-allowed relative overflow-hidden"
                    >
                      {member.isProtectedByMirror && (
                        <div className="absolute inset-0 bg-cyan-500/5 flex items-center justify-center backdrop-blur-[1px]">
                          <span className="text-[10px] font-bold uppercase tracking-widest text-cyan-300 rotate-[-15deg] border border-cyan-400/30 px-2 py-1 rounded">Actif</span>
                        </div>
                      )}
                      <div className="flex justify-between items-center mb-1">
                        <span className="font-serif font-bold text-cyan-300 group-hover:text-cyan-200">Protection du Miroir</span>
                        <span className="text-[10px] font-mono text-cyan-400/70">50 MP</span>
                      </div>
                      <p className="text-[9px] opacity-60 text-cyan-100/60">Protège contre la mort ou la folie lors d'une percée.</p>
                    </button>

                    <button
                      onClick={() => onUseDivinePower('awaken', member.id)}
                      disabled={mirrorPower < 100}
                      className="p-3 bg-cyan-900/10 border border-cyan-500/20 rounded-xl hover:bg-cyan-500/10 hover:border-cyan-400/50 transition-all text-left group disabled:opacity-30 disabled:cursor-not-allowed"
                    >
                      <div className="flex justify-between items-center mb-1">
                        <span className="font-serif font-bold text-cyan-300 group-hover:text-cyan-200">Éveil du Potentiel</span>
                        <span className="text-[10px] font-mono text-cyan-400/70">100 MP</span>
                      </div>
                      <p className="text-[9px] opacity-60 text-cyan-100/60">Améliore la racine spirituelle et peut donner le trait Génie.</p>
                    </button>

                    <button
                      onClick={() => onUseDivinePower('punish', member.id)}
                      disabled={mirrorPower < 30}
                      className="p-3 bg-red-900/10 border border-red-500/20 rounded-xl hover:bg-red-500/10 hover:border-red-400/50 transition-all text-left group disabled:opacity-30 disabled:cursor-not-allowed"
                    >
                      <div className="flex justify-between items-center mb-1">
                        <span className="font-serif font-bold text-red-400 group-hover:text-red-300">Châtiment Divin</span>
                        <span className="text-[10px] font-mono text-red-400/70">30 MP</span>
                      </div>
                      <p className="text-[9px] opacity-60 text-red-100/60">Blesse gravement ou tue le membre ciblé.</p>
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Manual Selection Modal (Nested) */}
        <AnimatePresence>
          {selectingManual && (
            <motion.div 
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 z-[110] bg-ink/95 backdrop-blur-xl flex items-center justify-center p-8"
            >
              <div className="w-full max-w-md space-y-8">
                <div className="text-center">
                  <BookOpen size={48} className="mx-auto text-gold-500 mb-4 opacity-50" />
                  <h3 className="text-3xl font-calligraphy text-gold-500">Choisir un Manuel</h3>
                  <p className="text-[10px] uppercase tracking-[0.3em] opacity-30 mt-2">Étude pour {member.firstName}</p>
                </div>

                <div className="space-y-3 max-h-[400px] overflow-y-auto pr-2 custom-scrollbar">
                  {manuals?.filter(m => {
                    const meetsRealm = REALMS.indexOf(member.realm) >= REALMS.indexOf(m.requirements.realm);
                    const meetsRoot = member.spiritualRoot >= m.requirements.spiritualRoot;
                    const alreadyStudied = member.studiedManuals.includes(m.id);
                    return meetsRealm && meetsRoot && !alreadyStudied;
                  }).map(manual => (
                    <button
                      key={manual.id}
                      onClick={() => {
                        onUpdateTask?.(member.id, 'Étude', manual.id);
                        setSelectingManual(false);
                      }}
                      className="w-full p-4 bg-paper/5 border border-paper/10 rounded-2xl hover:border-gold-500/50 hover:bg-gold-500/5 transition-all text-left group"
                    >
                      <div className="flex justify-between items-start mb-2">
                        <h4 className="font-serif font-bold text-paper group-hover:text-gold-500 transition-colors">{manual.name}</h4>
                        <span className="text-[10px] font-mono text-gold-500/50">{manual.type}</span>
                      </div>
                      <p className="text-[10px] opacity-40 line-clamp-2 mb-3">{manual.description}</p>
                      <div className="flex gap-4">
                        <div className="text-[8px] uppercase tracking-widest opacity-30">
                          Difficulté: <span className="text-gold-500">{manual.difficulty}</span>
                        </div>
                      </div>
                    </button>
                  ))}
                  {manuals?.filter(m => {
                    const meetsRealm = REALMS.indexOf(member.realm) >= REALMS.indexOf(m.requirements.realm);
                    const meetsRoot = member.spiritualRoot >= m.requirements.spiritualRoot;
                    const alreadyStudied = member.studiedManuals.includes(m.id);
                    return meetsRealm && meetsRoot && !alreadyStudied;
                  }).length === 0 && (
                    <div className="text-center py-12 opacity-30 italic text-sm">
                      Aucun manuel disponible pour ce niveau.
                    </div>
                  )}
                </div>

                <button 
                  onClick={() => setSelectingManual(false)}
                  className="w-full py-4 text-[10px] font-bold uppercase tracking-[0.3em] opacity-40 hover:opacity-100 transition-all"
                >
                  Annuler
                </button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </motion.div>
  );
};
