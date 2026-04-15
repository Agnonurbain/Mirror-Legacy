import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { CombatState, Combatant, Position } from '../../types/combat';
import { CombatEngine } from '../../engine/CombatEngine';
import { Shield, Sword, Heart, Zap, Skull, ArrowRight, Hexagon, Crosshair, Footprints } from 'lucide-react';

interface CombatViewProps {
  initialState: CombatState;
  onCombatEnd: (result: 'victory' | 'defeat', finalState: CombatState) => void;
}

export const CombatView: React.FC<CombatViewProps> = ({ initialState, onCombatEnd }) => {
  const [combatState, setCombatState] = useState<CombatState>(initialState);
  const [selectedCell, setSelectedCell] = useState<Position | null>(null);

  const activeCombatantId = combatState.turnOrder[combatState.currentTurnIndex];
  const activeCombatant = combatState.combatants.find(c => c.id === activeCombatantId);
  const isPlayerTurn = activeCombatant?.team === 'Player' && !activeCombatant.isDead;

  useEffect(() => {
    if (combatState.status !== 'ongoing') {
      const timer = setTimeout(() => onCombatEnd(combatState.status as 'victory' | 'defeat', combatState), 2000);
      return () => clearTimeout(timer);
    }

    if (!isPlayerTurn && activeCombatant && !activeCombatant.isDead) {
      // AI Turn
      const timer = setTimeout(() => {
        const newState = CombatEngine.processAITurn(combatState);
        setCombatState(newState);
      }, 1000);
      return () => clearTimeout(timer);
    } else if (activeCombatant?.isDead) {
      // Skip dead combatant
      setCombatState(CombatEngine.endTurn(combatState));
    }
  }, [combatState, isPlayerTurn, activeCombatant, onCombatEnd]);

  const handleCellClick = (x: number, y: number) => {
    if (!isPlayerTurn || combatState.status !== 'ongoing') return;

    const targetPos = { x, y };
    const occupant = combatState.combatants.find(c => !c.isDead && c.position.x === x && c.position.y === y);

    if (occupant) {
      if (occupant.team === 'Enemy') {
        // Attack
        if (activeCombatant && activeCombatant.ap > 0) {
          const dist = CombatEngine.getDistance(activeCombatant.position, targetPos);
          if (dist <= activeCombatant.range) {
            const newState = CombatEngine.attackCombatant(combatState, activeCombatant.id, occupant.id);
            setCombatState(newState);
          }
        }
      }
    } else {
      // Move
      if (activeCombatant && activeCombatant.ap > 0) {
        const path = CombatEngine.getPath(combatState, activeCombatant.position, targetPos);
        if (path.length > 0 && path.length <= 4) {
          const newState = CombatEngine.moveCombatant(combatState, activeCombatant.id, targetPos);
          setCombatState(newState);
        }
      }
    }
    setSelectedCell(null);
  };

  const handleEndTurn = () => {
    if (isPlayerTurn && combatState.status === 'ongoing') {
      setCombatState(CombatEngine.endTurn(combatState));
      setSelectedCell(null);
    }
  };

  const renderGrid = () => {
    const cells = [];
    for (let y = 0; y < combatState.height; y++) {
      for (let x = 0; x < combatState.width; x++) {
        const cell = combatState.grid[y][x];
        const occupant = combatState.combatants.find(c => !c.isDead && c.position.x === x && c.position.y === y);
        
        let isReachable = false;
        let isAttackable = false;

        if (isPlayerTurn && activeCombatant && !occupant && cell.type !== 'Obstacle') {
          const path = CombatEngine.getPath(combatState, activeCombatant.position, { x, y });
          isReachable = path.length > 0 && path.length <= 4;
        }

        if (isPlayerTurn && activeCombatant && occupant?.team === 'Enemy') {
          const dist = CombatEngine.getDistance(activeCombatant.position, { x, y });
          isAttackable = dist <= activeCombatant.range;
        }

        cells.push(
          <div
            key={`${x}-${y}`}
            onClick={() => handleCellClick(x, y)}
            className={`w-12 h-12 sm:w-16 sm:h-16 border border-amber-900/20 relative flex items-center justify-center transition-all ${
              cell.type === 'Obstacle' ? 'bg-[#1a120f] border-amber-900/50' :
              isAttackable ? 'bg-red-900/20 cursor-crosshair hover:bg-red-900/40 border-red-500/30' :
              isReachable ? 'bg-jade-900/20 cursor-pointer hover:bg-jade-900/40 border-jade-500/30' :
              'bg-black/40 hover:bg-black/60'
            }`}
          >
            {/* Cell Texture */}
            <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
            
            {cell.type === 'Obstacle' && (
              <div className="absolute inset-0 flex items-center justify-center opacity-30">
                <Hexagon className="text-amber-700" size={24} />
              </div>
            )}
            
            {isAttackable && (
              <div className="absolute inset-0 flex items-center justify-center opacity-20 pointer-events-none">
                <Crosshair className="text-red-500" size={24} />
              </div>
            )}
            
            {isReachable && !isAttackable && (
              <div className="absolute inset-0 flex items-center justify-center opacity-20 pointer-events-none">
                <Footprints className="text-jade-500" size={24} />
              </div>
            )}

            <AnimatePresence>
              {occupant && (
                <motion.div
                  initial={{ scale: 0, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  exit={{ scale: 0, opacity: 0 }}
                  className={`w-10 h-10 sm:w-12 sm:h-12 rounded-full flex items-center justify-center relative z-10 shadow-lg border-2 ${
                    occupant.team === 'Player' ? 'bg-gradient-to-br from-amber-700 to-amber-900 border-amber-500' : 'bg-gradient-to-br from-red-800 to-red-950 border-red-500'
                  } ${occupant.id === activeCombatantId ? 'ring-4 ring-white/20 animate-pulse' : ''}`}
                >
                  <span className="text-xs sm:text-sm font-bold text-white drop-shadow-md">{occupant.name.charAt(0)}</span>
                  
                  {/* Health Bar */}
                  <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 w-8 h-1.5 bg-black/80 rounded-full overflow-hidden border border-amber-900/50">
                    <div 
                      className={`h-full transition-all ${occupant.team === 'Player' ? 'bg-jade-500' : 'bg-red-500'}`}
                      style={{ width: `${(occupant.hp / occupant.maxHp) * 100}%` }}
                    />
                  </div>
                  
                  {/* Status Effects */}
                  {occupant.statusEffects.length > 0 && (
                    <div className="absolute -top-2 -right-2 flex gap-0.5">
                      {occupant.statusEffects.map((effect, i) => (
                        <div key={i} className={`w-3 h-3 rounded-full border border-black ${
                          effect.type === 'Poison' ? 'bg-green-500' :
                          effect.type === 'Burn' ? 'bg-orange-500' :
                          effect.type === 'Stun' ? 'bg-yellow-500' :
                          'bg-blue-500'
                        }`} title={`${effect.type} (${effect.duration})`} />
                      ))}
                    </div>
                  )}
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        );
      }
    }
    return cells;
  };

  return (
    <div className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen relative flex flex-col">
      {/* Background Texture */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
      <div className="absolute inset-0 bg-gradient-to-b from-transparent via-[#140d0a]/50 to-[#0a0a0a] pointer-events-none" />

      {/* Header */}
      <div className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-6 md:p-8 shadow-2xl mb-8 relative overflow-hidden flex flex-col md:flex-row items-center justify-between gap-6 z-10">
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-50" />
        
        <div className="flex items-center gap-6 relative z-10">
          <div className="w-16 h-16 rounded-full bg-gradient-to-br from-red-900/40 to-black border border-red-500/30 flex items-center justify-center shadow-[0_0_20px_rgba(239,68,68,0.2)]">
            <Sword className="text-red-500" size={32} />
          </div>
          <div>
            <h2 className="text-3xl md:text-4xl font-calligraphy text-amber-500 drop-shadow-md">Combat Spirituel</h2>
            <p className="text-xs text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-1">Tour {combatState.turn}</p>
          </div>
        </div>

        <div className="flex items-center gap-4 relative z-10">
          <div className="bg-black/40 px-6 py-3 rounded-xl border border-amber-900/50 flex items-center gap-3">
            <span className="text-xs text-amber-700/70 uppercase tracking-widest font-mono">Actif:</span>
            <span className={`font-serif font-bold ${activeCombatant?.team === 'Player' ? 'text-amber-400' : 'text-red-400'}`}>
              {activeCombatant?.name || '...'}
            </span>
          </div>
          <button
            onClick={handleEndTurn}
            disabled={!isPlayerTurn || combatState.status !== 'ongoing'}
            className="bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white px-6 py-3 rounded-xl font-serif font-bold text-sm transition-all shadow-lg active:scale-95 border border-amber-400/20 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            Fin de Tour
            <ArrowRight size={16} />
          </button>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-8 flex-1 relative z-10">
        {/* Combat Log */}
        <div className="w-full lg:w-80 bg-[#1a120f] border border-amber-900/30 rounded-3xl p-6 shadow-xl flex flex-col h-[400px] lg:h-auto order-2 lg:order-1 relative overflow-hidden">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          
          <h3 className="text-xl font-calligraphy text-amber-500 mb-4 border-b border-amber-900/30 pb-2 relative z-10">Journal de Combat</h3>
          <div className="flex-1 overflow-y-auto pr-2 custom-scrollbar space-y-3 relative z-10">
            {combatState.logs.map((entry, i) => (
              <motion.div 
                key={i}
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                className="text-sm font-serif text-amber-100/80 bg-black/30 p-3 rounded-xl border border-amber-900/20"
              >
                {entry}
              </motion.div>
            ))}
          </div>
        </div>

        {/* Grid */}
        <div className="flex-1 flex items-center justify-center bg-[#140d0a] border border-amber-900/30 rounded-3xl p-8 shadow-2xl order-1 lg:order-2 relative overflow-hidden">
          <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-20" />
          
          <div 
            className="grid gap-1 relative z-10 bg-black/60 p-4 rounded-2xl border border-amber-900/50 shadow-[0_0_30px_rgba(0,0,0,0.8)]"
            style={{ 
              gridTemplateColumns: `repeat(${combatState.width}, minmax(0, 1fr))`,
              gridTemplateRows: `repeat(${combatState.height}, minmax(0, 1fr))`
            }}
          >
            {renderGrid()}
          </div>
        </div>

        {/* Combatant Info */}
        <div className="w-full lg:w-80 space-y-4 order-3 relative z-10">
          {combatState.combatants.map(combatant => (
            <div 
              key={combatant.id}
              className={`bg-[#1a120f] border rounded-2xl p-4 transition-all shadow-lg relative overflow-hidden ${
                combatant.id === activeCombatantId ? 'border-amber-500/50 shadow-[0_0_15px_rgba(245,158,11,0.2)]' : 'border-amber-900/30'
              } ${combatant.isDead ? 'opacity-50 grayscale' : ''}`}
            >
              <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
              
              <div className="flex items-center justify-between mb-3 relative z-10">
                <div className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center text-white font-bold text-xs shadow-inner ${
                    combatant.team === 'Player' ? 'bg-gradient-to-br from-amber-700 to-amber-900 border border-amber-500' : 'bg-gradient-to-br from-red-800 to-red-950 border border-red-500'
                  }`}>
                    {combatant.name.charAt(0)}
                  </div>
                  <span className={`font-serif font-bold ${combatant.team === 'Player' ? 'text-amber-100' : 'text-red-100'}`}>
                    {combatant.name}
                  </span>
                </div>
                {combatant.isDead && <Skull size={16} className="text-red-500" />}
              </div>

              <div className="space-y-2 relative z-10">
                <div className="flex items-center justify-between text-xs font-mono">
                  <span className="text-amber-700/70 uppercase tracking-widest flex items-center gap-1"><Heart size={10} className="text-red-400"/> PV</span>
                  <span className="text-amber-100">{combatant.hp} / {combatant.maxHp}</span>
                </div>
                <div className="h-1.5 bg-black/60 rounded-full overflow-hidden border border-amber-900/30">
                  <div 
                    className={`h-full transition-all ${combatant.team === 'Player' ? 'bg-jade-500' : 'bg-red-500'}`}
                    style={{ width: `${(combatant.hp / combatant.maxHp) * 100}%` }}
                  />
                </div>

                <div className="flex items-center justify-between text-xs font-mono mt-2">
                  <span className="text-amber-700/70 uppercase tracking-widest flex items-center gap-1"><Zap size={10} className="text-amber-400"/> PA</span>
                  <span className="text-amber-500">{combatant.ap} / {combatant.maxAp}</span>
                </div>
                <div className="h-1.5 bg-black/60 rounded-full overflow-hidden border border-amber-900/30">
                  <div 
                    className="h-full bg-amber-500 transition-all"
                    style={{ width: `${(combatant.ap / combatant.maxAp) * 100}%` }}
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* End Screen Overlay */}
      <AnimatePresence>
        {combatState.status !== 'ongoing' && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="absolute inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-md"
          >
            <motion.div 
              initial={{ scale: 0.9, y: 20 }}
              animate={{ scale: 1, y: 0 }}
              className={`bg-[#1a120f] border rounded-3xl p-12 text-center max-w-md w-full shadow-2xl relative overflow-hidden ${
                combatState.status === 'victory' ? 'border-jade-500/50 shadow-[0_0_50px_rgba(34,197,94,0.2)]' : 'border-red-500/50 shadow-[0_0_50px_rgba(239,68,68,0.2)]'
              }`}
            >
              <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
              
              <div className="relative z-10">
                <h2 className={`text-6xl font-calligraphy mb-4 drop-shadow-lg ${
                  combatState.status === 'victory' ? 'text-jade-400' : 'text-red-500'
                }`}>
                  {combatState.status === 'victory' ? 'Victoire' : 'Défaite'}
                </h2>
                <p className="text-amber-100/70 font-serif italic text-lg">
                  {combatState.status === 'victory' 
                    ? "Vos disciples ont triomphé de leurs adversaires."
                    : "Vos disciples ont été vaincus au combat."}
                </p>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};
