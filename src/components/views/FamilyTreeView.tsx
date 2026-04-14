import React from 'react';
import { Character } from '../../types/game';
import { Crown, Skull, Heart, Search } from 'lucide-react';
import { Avatar } from '../ui/Avatar';
import { motion } from 'motion/react';

interface FamilyTreeViewProps {
  members: Character[];
  onArrangeMarriage: (memberId: string) => void;
}

const containerVariants = {
  hidden: { opacity: 0, scaleY: 0 },
  visible: {
    opacity: 1,
    scaleY: 1,
    transition: {
      duration: 0.8,
      ease: [0.16, 1, 0.3, 1],
      staggerChildren: 0.1,
      delayChildren: 0.4
    }
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

export const FamilyTreeView: React.FC<FamilyTreeViewProps> = ({ members, onArrangeMarriage }) => {
  const [searchQuery, setSearchQuery] = React.useState<string>('');
  
  const generations = Array.from(new Set(members.map(m => m.generation))).sort((a: number, b: number) => a - b);

  const displayedGenerations = searchQuery.trim() === '' 
    ? generations 
    : searchQuery
        .split(',')
        .map(s => s.trim())
        .filter(s => s !== '')
        .map(Number)
        .filter(n => !isNaN(n) && generations.includes(n));

  return (
    <div className="p-4 md:p-8 max-w-6xl mx-auto min-h-screen flex flex-col items-center">
      
      {/* Search Bar */}
      <div className="w-full max-w-md mb-8 relative z-30">
        <div className="relative w-full">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-amber-700/50" size={18} />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Filtrer par génération (ex: 0, 1, 2...)"
            className="w-full bg-[#1a1510]/80 border border-amber-900/50 rounded-full py-3 pl-12 pr-4 text-sm font-serif text-amber-500 placeholder:text-amber-700/50 focus:outline-none focus:border-amber-500/60 transition-colors shadow-xl backdrop-blur-sm"
          />
        </div>
      </div>

      {/* The Silk Scroll */}
      <div className="w-full relative flex flex-col items-center">
        
        {/* Top Roller */}
        <div className="w-full h-10 bg-gradient-to-b from-[#3e2723] via-[#5d4037] to-[#3e2723] rounded-full shadow-[0_10px_20px_rgba(0,0,0,0.5)] border border-[#1e100a] relative z-20 flex items-center justify-center">
          <div className="absolute -left-3 md:-left-6 top-1/2 -translate-y-1/2 w-3 md:w-6 h-7 md:h-8 bg-gradient-to-b from-[#2a1a14] to-[#4e342e] rounded-l-full border-y border-l border-[#1e100a] shadow-inner" />
          <div className="absolute -right-3 md:-right-6 top-1/2 -translate-y-1/2 w-3 md:w-6 h-7 md:h-8 bg-gradient-to-b from-[#2a1a14] to-[#4e342e] rounded-r-full border-y border-r border-[#1e100a] shadow-inner" />
          <div className="w-1/3 h-2 bg-[#1e100a]/30 rounded-full" />
        </div>

        {/* Scroll Body */}
        <motion.div 
          initial="hidden"
          animate="visible"
          variants={containerVariants}
          className="w-[96%] md:w-[92%] bg-[#241c15] relative shadow-2xl border-x border-[#3e2723] origin-top"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%233e2723' fill-opacity='0.1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`
          }}
        >
          {/* Inner Silk Border */}
          <div className="absolute inset-2 md:inset-4 border-2 border-double border-amber-900/40 pointer-events-none" />
          <div className="absolute inset-3 md:inset-5 border border-amber-900/20 pointer-events-none" />

          <div className="py-16 px-4 md:px-12 flex flex-col items-center gap-24 relative z-10 min-h-[400px]">
            
            {/* Title */}
            <div className="text-center mb-8">
              <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-600 tracking-[0.3em] uppercase" style={{ writingMode: 'vertical-rl', textOrientation: 'upright' }}>
                Lignée
              </h2>
              <div className="mt-6 w-1 h-24 bg-gradient-to-b from-amber-600/0 via-amber-600/50 to-amber-600/0 mx-auto" />
            </div>

            {displayedGenerations.length === 0 ? (
              <div className="text-center py-12 opacity-50 font-serif italic text-amber-700">
                Aucune archive trouvée.
              </div>
            ) : (
              displayedGenerations.map((gen, index) => {
                const genMembers = members.filter(m => m.generation === gen);
                return (
                  <div key={gen} className="w-full flex flex-col items-center relative">
                    
                    {/* Generation Divider */}
                    {index > 0 && (
                      <div className="absolute -top-16 left-1/2 -translate-x-1/2 w-px h-12 bg-amber-900/50" />
                    )}

                    {/* Generation Label */}
                    <div className="flex items-center gap-4 mb-12">
                      <div className="h-px w-12 md:w-32 bg-gradient-to-r from-transparent to-amber-900/50" />
                      <h3 className="text-xl md:text-2xl font-calligraphy text-amber-700 tracking-[0.2em] uppercase">
                        Génération {gen}
                      </h3>
                      <div className="h-px w-12 md:w-32 bg-gradient-to-l from-transparent to-amber-900/50" />
                    </div>
                    
                    {/* Members Grid */}
                    <div className="flex flex-wrap justify-center gap-6 md:gap-10 w-full">
                      {genMembers.map(member => (
                        <motion.div 
                          key={member.id} 
                          variants={itemVariants}
                          whileHover={{ scale: 1.05, y: -5 }}
                          className={`relative group transition-all duration-300 w-40 md:w-48 flex flex-col items-center ${
                            !member.isAlive ? 'opacity-60 grayscale-[0.5]' : ''
                          }`}
                        >
                          {/* Tablette Ancestrale Background */}
                          <div className="absolute inset-0 bg-gradient-to-b from-[#1e1510] to-[#140d0a] shadow-[0_5px_15px_rgba(0,0,0,0.5)] border border-amber-900/40 rounded-t-sm rounded-b-md" />
                          
                          {/* Tablette Top Decoration */}
                          <div className="absolute top-0 left-1/2 -translate-x-1/2 w-3/4 h-2 bg-gradient-to-b from-amber-900/50 to-transparent rounded-t-sm" />

                          <div className="relative z-10 flex flex-col items-center p-4 w-full">
                            
                            {/* Status Icons */}
                            <div className="absolute top-2 right-2 flex flex-col gap-1">
                              {gen === 0 && member.isAlive && (
                                <Crown className="text-amber-500 drop-shadow-md" size={16} />
                              )}
                              {!member.isAlive && (
                                <Skull className="text-red-900/80 drop-shadow-md" size={16} />
                              )}
                              {member.isAlive && member.spouse && (
                                <Heart className="text-red-800/80 drop-shadow-md" size={14} fill="currentColor" />
                              )}
                            </div>

                            <Avatar 
                              seed={member.portraitSeed} 
                              name={member.firstName} 
                              realm={member.realm}
                              size="md"
                              className={`mb-4 border-2 border-amber-900/30 shadow-inner ${!member.isAlive ? 'sepia-[.5]' : ''}`}
                            />

                            {/* Name */}
                            <div className="flex flex-col items-center justify-center min-h-[60px] mb-2">
                              <h4 className={`text-lg md:text-xl font-calligraphy text-center leading-tight ${
                                member.isAlive ? 'text-amber-500' : 'text-amber-700/50'
                              }`}>
                                {member.lastName}<br/>{member.firstName}
                              </h4>
                            </div>

                            <div className="w-full h-px bg-amber-900/30 my-2" />

                            <p className="text-[10px] text-amber-700/70 font-mono uppercase tracking-widest text-center">
                              {member.realm}
                            </p>
                            <p className="text-[10px] text-amber-700/50 font-mono uppercase tracking-widest text-center mt-1">
                              {member.age} Ans
                            </p>

                            {member.isAlive && member.age >= 18 && !member.spouse && (
                              <button
                                onClick={() => onArrangeMarriage(member.id)}
                                className="mt-4 px-3 py-1.5 bg-amber-900/20 text-amber-600 border border-amber-900/40 rounded text-[9px] uppercase tracking-widest hover:bg-amber-900/40 hover:text-amber-400 transition-colors opacity-0 group-hover:opacity-100 w-full"
                              >
                                Mariage
                              </button>
                            )}
                          </div>
                        </motion.div>
                      ))}
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </motion.div>

        {/* Bottom Roller */}
        <div className="w-full h-10 bg-gradient-to-b from-[#3e2723] via-[#5d4037] to-[#3e2723] rounded-full shadow-[0_10px_20px_rgba(0,0,0,0.5)] border border-[#1e100a] relative z-20 flex items-center justify-center">
          <div className="absolute -left-3 md:-left-6 top-1/2 -translate-y-1/2 w-3 md:w-6 h-7 md:h-8 bg-gradient-to-b from-[#2a1a14] to-[#4e342e] rounded-l-full border-y border-l border-[#1e100a] shadow-inner" />
          <div className="absolute -right-3 md:-right-6 top-1/2 -translate-y-1/2 w-3 md:w-6 h-7 md:h-8 bg-gradient-to-b from-[#2a1a14] to-[#4e342e] rounded-r-full border-y border-r border-[#1e100a] shadow-inner" />
          <div className="w-1/3 h-2 bg-[#1e100a]/30 rounded-full" />
        </div>

      </div>
    </div>
  );
};
